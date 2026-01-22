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
    public Button hostButton;       // 방 만들기 버튼
    public Button refreshButton;    // 새로고침 버튼
    public GameObject lobbyPanel;   // 로비 패널
    public Transform roomListContent; // 방 목록 들어갈 자리
    public RoomItem roomItemPrefab;   // 방 아이템 프리팹
    public TMP_InputField roomNameInput; // 방 이름 입력
    public GameObject loadingPanel;   // 로딩 패널

    // ★ [추가] 현재 방의 ID를 저장할 변수 (어디서든 접근 가능하게 static으로)
    public static string CurrentLobbyId;

    // 상태 메시지 텍스트 (UI에는 영어가 출력됨)
    public TextMeshProUGUI statusText;

    private void Start()
    {
        // 1. 시작 시 버튼 잠금
        SetButtonsState(false);

        hostButton.onClick.AddListener(CreateLobby);
        refreshButton.onClick.AddListener(RefreshLobbyList);

        // 2. 재시작 감지 (이미 게임 중이면 로비 숨김)
        if (NetworkManager.Singleton != null &&
           (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient))
        {
            Debug.Log("[Lobby] 재시작 감지됨! 로비 UI를 숨깁니다.");
            lobbyPanel.SetActive(false);
        }
        else
        {
            Debug.Log("[Lobby] 게임 시작. 로비 UI 활성화.");
            lobbyPanel.SetActive(true);

            // 3. 인터넷 체크 및 로그인 시도
            if (CheckInternetConnection())
            {
                Authenticate();
            }
            else
            {
                // UI: 영어 출력
                UpdateStatus("Please check your internet connection.");
            }
        }
    }

    // 인터넷 연결 확인 함수
    bool CheckInternetConnection()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogError("인터넷 연결이 없습니다.");
            return false;
        }
        return true;
    }

    void SetButtonsState(bool interactable)
    {
        hostButton.interactable = interactable;
        refreshButton.interactable = interactable;
    }

    // 상태 메시지 업데이트 (UI용)
    void UpdateStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        // 로그에는 어떤 영어 메시지가 떴는지 기록
        Debug.Log($"[Status UI] {msg}");
    }

    async void Authenticate()
    {
        UpdateStatus("Connecting to server..."); // UI: 서버 연결 중

        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            Debug.Log($"[Lobby] 로그인 성공! ID: {AuthenticationService.Instance.PlayerId}");
            UpdateStatus("Ready"); // UI: 준비 완료

            SetButtonsState(true);
            RefreshLobbyList(); // 로그인 되면 목록 한번 갱신
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Lobby] 로그인 실패: {e}");
            UpdateStatus("Connection failed. Please restart."); // UI: 연결 실패
            SetButtonsState(false);
        }
    }

    public async void CreateLobby()
    {
        // 인터넷 재확인
        if (!CheckInternetConnection())
        {
            UpdateStatus("No internet connection."); // UI: 인터넷 없음
            return;
        }

        if (loadingPanel != null) loadingPanel.SetActive(true);
        SetButtonsState(false);
        UpdateStatus("Creating room..."); // UI: 방 생성 중

        string startingSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        try
        {
            // 1. 릴레이(중계 서버) 할당
            Debug.Log("[Lobby] Relay 할당 요청...");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != startingSceneName)
                throw new System.Exception("씬 변경됨 (방 생성 취소)");

            string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData
            );

            // 2. 로비 생성
            string lobbyName = (roomNameInput != null && roomNameInput.text.Length > 0) ? roomNameInput.text : "My Room";

            CreateLobbyOptions options = new CreateLobbyOptions();
            options.Data = new Dictionary<string, DataObject> {
                { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
            };

            Debug.Log($"[Lobby] 로비 생성 요청: {lobbyName}");
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 2, options);

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != startingSceneName)
                throw new System.Exception("씬 변경됨 (방 생성 취소)");

            Debug.Log($"[Lobby] 방 생성 완료: {lobby.Name}");
            // ★ [추가] 방이 만들어지면 ID를 저장해둡니다!
            CurrentLobbyId = lobby.Id;
            UpdateStatus("Room created! Starting game..."); // UI: 방 생성됨

            NetworkManager.Singleton.StartHost();

            lobbyPanel.SetActive(false);
            loadingPanel.SetActive(false);

            // 하트비트(방 유지 신호) 시작
            StartCoroutine(HeartbeatLobbyCoroutine(lobby.Id, 15));
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby] 로비 서비스 에러: {e}");
            UpdateStatus($"Failed to create room: {e.Reason}"); // UI: 실패 이유 출력

            if (loadingPanel != null) loadingPanel.SetActive(false);
            SetButtonsState(true);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Lobby] 방 생성 실패: {e.Message}");
            UpdateStatus("An error occurred."); // UI: 오류 발생

            if (loadingPanel != null) loadingPanel.SetActive(false);
            SetButtonsState(true);
        }
    }

    public async void RefreshLobbyList()
    {
        if (!CheckInternetConnection())
        {
            UpdateStatus("No internet connection.");
            return;
        }

        // 버튼이 없으면 아예 시작도 안 함
        if (refreshButton == null) return;

        refreshButton.interactable = false; // 버튼 비활성화
        UpdateStatus("Refreshing list...");

        try
        {
            // 기존 목록 삭제
            foreach (Transform child in roomListContent) Destroy(child.gameObject);

            // 검색 조건 설정
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 10;
            options.Filters = new List<QueryFilter> {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);

            Debug.Log($"[Lobby] 방 {response.Results.Count}개 발견됨");

            if (response.Results.Count == 0)
            {
                UpdateStatus("No rooms found.");
            }
            else
            {
                UpdateStatus($"Found {response.Results.Count} room(s).");
                foreach (Lobby lobby in response.Results)
                {
                    // 방 목록 생성 중에도 버튼이 파괴되었는지 체크 (선택 사항이지만 안전함)
                    if (roomListContent == null) return;

                    RoomItem newItem = Instantiate(roomItemPrefab, roomListContent);
                    newItem.Setup(lobby, this);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Lobby] 목록 갱신 실패: {e.Message}");
            UpdateStatus("Failed to load room list.");
        }

        // ★ [수정] 작업이 끝난 후, 버튼이 살아있는지 확인하고 켭니다.
        if (refreshButton != null)
        {
            refreshButton.interactable = true;
        }
    }

    public async void JoinRoom(Lobby lobby)
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);
        UpdateStatus("Joining room..."); // UI: 입장 중
        SetButtonsState(false);

        try
        {
            Debug.Log($"[Lobby] 방 입장 시도: {lobby.Name}");
            Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);
            string relayCode = joinedLobby.Data["RelayCode"].Value;

            Debug.Log("[Lobby] 릴레이 코드 획득, 연결 시도...");
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, allocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();

            CurrentLobbyId = lobby.Id;
            lobbyPanel.SetActive(false);
            if (loadingPanel != null) loadingPanel.SetActive(false);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Lobby] 입장 실패: {e.Message}");
            UpdateStatus("Failed to join room."); // UI: 입장 실패

            if (loadingPanel != null) loadingPanel.SetActive(false);
            SetButtonsState(true);
            RefreshLobbyList();
        }
    }

    System.Collections.IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);
        while (true) { LobbyService.Instance.SendHeartbeatPingAsync(lobbyId); yield return delay; }
    }
}