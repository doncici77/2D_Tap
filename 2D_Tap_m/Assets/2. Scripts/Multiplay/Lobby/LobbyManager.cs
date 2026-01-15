using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using System.Collections.Generic;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    [Header("UI References")]
    public Button hostButton;      // 방 만들기 버튼
    public Button refreshButton;   // 목록 새로고침 버튼
    public GameObject lobbyPanel;  // 로비 전체 UI
    public Transform roomListContent; // Scroll View의 Content
    public RoomItem roomItemPrefab;   // 방 버튼 프리팹
    public TMP_InputField roomNameInput; // (선택) 방 제목 입력칸
    public GameObject loadingPanel;

    private void Start()
    {
        hostButton.onClick.AddListener(CreateLobby);
        refreshButton.onClick.AddListener(RefreshLobbyList);

        // ★★★ [핵심 수정] 재시작 감지 로직 추가 ★★★
        // NetworkManager가 있고, 이미 연결된 상태(Host 또는 Client)라면?
        // -> 이것은 "게임 중 재시작" 상황입니다. UI를 숨깁니다.
        if (NetworkManager.Singleton != null &&
           (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient))
        {
            Debug.Log("[Lobby] 재시작 감지됨! 로비 UI를 숨깁니다.");
            lobbyPanel.SetActive(false); // UI 끄기
        }
        else
        {
            // 연결 안 된 상태라면? -> "게임을 처음 켠" 상황입니다.
            Debug.Log("[Lobby] 게임 시작. 로비 UI를 켭니다.");
            lobbyPanel.SetActive(true); // UI 켜기
            Authenticate(); // 로그인 시도
        }
    }

    async void Authenticate()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"[Lobby] 로그인 완료! ID: {AuthenticationService.Instance.PlayerId}");
        }
    }

    public async void CreateLobby()
    {
        // ★ 1. 로딩 패널 켜기 (화면 전체를 막아서 클릭 방지)
        if (loadingPanel != null) loadingPanel.SetActive(true);

        // 혹시 모르니 버튼도 꺼둠
        hostButton.interactable = false;

        string startingSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        try
        {
            Debug.Log("[Lobby] 방 생성 중...");

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);

            // 씬 변경 체크
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != startingSceneName)
                throw new System.Exception("씬 변경됨");

            string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData
            );

            string lobbyName = (roomNameInput != null && roomNameInput.text.Length > 0) ? roomNameInput.text : "My Room";

            CreateLobbyOptions options = new CreateLobbyOptions();
            options.Data = new Dictionary<string, DataObject> {
                { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 2, options);

            // 씬 변경 체크 2
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != startingSceneName)
                throw new System.Exception("씬 변경됨");

            Debug.Log($"[Lobby] 방 생성됨: {lobby.Name}");

            NetworkManager.Singleton.StartHost();

            // ★ 성공하면 로비 UI 전체가 꺼지므로 LoadingPanel도 자연스럽게 사라짐
            lobbyPanel.SetActive(false); 
            loadingPanel.SetActive(false);
            StartCoroutine(HeartbeatLobbyCoroutine(lobby.Id, 15));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"방 만들기 실패/취소: {e.Message}");

            // ★ 2. 실패했을 때는 반드시 로딩 패널을 꺼줘야 유저가 다시 조작 가능
            if (loadingPanel != null) loadingPanel.SetActive(false);
            hostButton.interactable = true;
        }
    }

    public async void RefreshLobbyList()
    {
        try
        {
            foreach (Transform child in roomListContent) Destroy(child.gameObject); // 기존 목록 삭제

            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 10;
            options.Filters = new List<QueryFilter> {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
            Debug.Log($"[Lobby] 방 {response.Results.Count}개 발견");

            foreach (Lobby lobby in response.Results)
            {
                RoomItem newItem = Instantiate(roomItemPrefab, roomListContent);
                newItem.Setup(lobby, this);
            }
        }
        catch (System.Exception e) { Debug.LogError($"목록 갱신 실패: {e.Message}"); }
    }

    public async void JoinRoom(Lobby lobby)
    {
        try
        {
            Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);
            string relayCode = joinedLobby.Data["RelayCode"].Value;

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, allocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
            lobbyPanel.SetActive(false);
        }
        catch (System.Exception e) { Debug.LogError($"입장 실패: {e.Message}"); }
    }

    System.Collections.IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);
        while (true) { LobbyService.Instance.SendHeartbeatPingAsync(lobbyId); yield return delay; }
    }
}