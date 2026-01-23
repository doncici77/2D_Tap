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
using UnityEngine.Localization.Settings;

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

    // ★ 현재 방의 ID를 저장할 변수
    public static string CurrentLobbyId;

    // 상태 메시지 텍스트
    public TextMeshProUGUI statusText;

    // ★ 테이블 이름 정의
    private const string TableName = "Table";

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
                // UI: "인터넷 연결 확인해주세요"
                UpdateStatusKey("lobby_check_internet");
            }
        }

        SoundManager.Instance.PlayBGM(SoundManager.Instance.inGameBGM);
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

    // ★ [수정] 키(Key)를 받아서 번역된 텍스트 출력하는 함수 (일반 텍스트)
    void UpdateStatusKey(string key)
    {
        if (statusText != null)
        {
            statusText.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, key);
        }
    }

    // ★ [추가] 인자(Available Rooms 등 숫자)가 있는 번역 텍스트 출력용 함수
    void UpdateStatusKey(string key, object[] args)
    {
        if (statusText != null)
        {
            statusText.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, key, args);
        }
    }

    async void Authenticate()
    {
        UpdateStatusKey("lobby_connecting"); // "서버 연결 중..."

        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            Debug.Log($"[Lobby] 로그인 성공! ID: {AuthenticationService.Instance.PlayerId}");
            UpdateStatusKey("lobby_ready"); // "준비 완료"

            SetButtonsState(true);
            RefreshLobbyList(); // 로그인 되면 목록 한번 갱신
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Lobby] 로그인 실패: {e}");
            UpdateStatusKey("lobby_connect_fail"); // "연결 실패"
            SetButtonsState(false);
        }
    }

    public async void CreateLobby()
    {
        // 인터넷 재확인
        if (!CheckInternetConnection())
        {
            UpdateStatusKey("lobby_no_internet"); // "인터넷 없음"
            return;
        }

        if (loadingPanel != null) loadingPanel.SetActive(true);
        SetButtonsState(false);
        UpdateStatusKey("lobby_creating_room"); // "방 생성 중..."

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
            CurrentLobbyId = lobby.Id;

            UpdateStatusKey("lobby_room_created"); // "방 생성 완료! 게임 시작..."

            NetworkManager.Singleton.StartHost();

            lobbyPanel.SetActive(false);
            loadingPanel.SetActive(false);

            // 하트비트(방 유지 신호) 시작
            StartCoroutine(HeartbeatLobbyCoroutine(lobby.Id, 15));
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby] 로비 서비스 에러: {e}");
            // "방 생성 실패: {이유}"
            UpdateStatusKey("lobby_create_fail", new object[] { e.Reason });

            if (loadingPanel != null) loadingPanel.SetActive(false);
            SetButtonsState(true);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Lobby] 방 생성 실패: {e.Message}");
            UpdateStatusKey("lobby_error"); // "오류가 발생했습니다."

            if (loadingPanel != null) loadingPanel.SetActive(false);
            SetButtonsState(true);
        }
    }

    public async void RefreshLobbyList()
    {
        if (!CheckInternetConnection())
        {
            UpdateStatusKey("lobby_no_internet");
            return;
        }

        // 버튼이 없으면 아예 시작도 안 함
        if (refreshButton == null) return;

        refreshButton.interactable = false; // 버튼 비활성화
        UpdateStatusKey("lobby_refreshing"); // "목록 갱신 중..."

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
                UpdateStatusKey("lobby_no_rooms"); // "방이 없습니다."
            }
            else
            {
                // "방 {0}개를 찾았습니다."
                UpdateStatusKey("lobby_found_rooms", new object[] { response.Results.Count });

                foreach (Lobby lobby in response.Results)
                {
                    if (roomListContent == null) return;

                    RoomItem newItem = Instantiate(roomItemPrefab, roomListContent);
                    newItem.Setup(lobby, this);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Lobby] 목록 갱신 실패: {e.Message}");
            UpdateStatusKey("lobby_load_fail"); // "목록 불러오기 실패."
        }

        if (refreshButton != null)
        {
            refreshButton.interactable = true;
        }
    }

    public async void JoinRoom(Lobby lobby)
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);
        UpdateStatusKey("lobby_joining"); // "방 입장 중..."
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
            UpdateStatusKey("lobby_join_fail"); // "방 입장 실패."

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