using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MultiUIManager : MonoBehaviour
{
    public static MultiUIManager Instance;

    [Header("Controls")]
    public GameObject actionButtonObj;
    public Button restartButton;
    public Button titleButton;
    public Button multiExitButton; // 멀티 전용 나가기 버튼

    [Header("Gauge Display")]
    public Slider powerGaugeSlider;
    public Image sliderFillImage;
    public RectTransform successZoneRect;
    public Color normalColor = Color.white;
    public Color successColor = Color.green;

    [Header("Game Info")]
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI distanceText;
    public GameObject resultPanel;

    [Header("Combo Display")]
    public TextMeshProUGUI comboText; // ★ 콤보 텍스트 (Inspector에서 연결 필요!)

    [Header("Score Info")] // ★ [추가] 점수 표시용 텍스트
    public TextMeshProUGUI p1ScoreText;
    public TextMeshProUGUI p2ScoreText;

    [Header("Rematch Info")] // ★ [추가] 재경기 상태 표시 텍스트
    public TextMeshProUGUI rematchStatusText;

    // 멀티 플레이어 참조
    public NetPlayerController netPlayer;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(Instance.gameObject);
        Instance = this;
    }

    // NetPlayerController에서 호출해주는 함수
    public void SetNetPlayer(NetPlayerController controller)
    {
        netPlayer = controller;
        SetupRapidButton(); // 플레이어 연결되면 버튼 기능 활성화
        Debug.Log($"UI: 멀티 플레이어 연결됨 - {controller.name}");
    }

    void Start()
    {
        HideResult();

        // 재시작 버튼 (재대결 요청)
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(() => {
                if (netPlayer != null)
                {
                    netPlayer.SendRematchRequestServerRpc();
                    HideResult();
                    if (distanceText != null) distanceText.text = "WAITING FOR OPPONENT...";
                    UpdateBattleStatus();
                }
            });
        }

        // 타이틀 이동
        if (titleButton != null)
            titleButton.onClick.AddListener(() => GoToTitle());

        // 대기 중 나가기
        if (multiExitButton != null)
            multiExitButton.onClick.AddListener(() => GoToTitle());
    }

    async void GoToTitle()
    {
        // 1. 내가 호스트인지 확인
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            // 저장해둔 Lobby ID가 있다면 방 삭제 시도
            if (!string.IsNullOrEmpty(LobbyManager.CurrentLobbyId))
            {
                try
                {
                    Debug.Log("[MultiUI] 호스트가 방을 삭제합니다...");
                    await LobbyService.Instance.DeleteLobbyAsync(LobbyManager.CurrentLobbyId);
                    LobbyManager.CurrentLobbyId = null; // ID 초기화
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"방 삭제 실패: {e.Message}");
                }
            }
        }

        // 2. 네트워크 연결 종료 (호스트면 서버 닫힘, 클라면 연결 끊김)
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // 3. 타이틀 씬으로 이동
        // (NetGameManager가 파괴되지 않도록 설정되어 있다면 여기서 씬 이동만 해도 됨)
        // 하지만 확실하게 하기 위해 SceneManager 직접 사용 권장
        SceneManager.LoadScene("Title");
    }

    void Update()
    {
        if (NetGameManager.Instance == null) return;

        // 플레이어 놓쳤으면 다시 찾기
        if (netPlayer == null) FindMyNetPlayer();

        bool isPlaying = (NetGameManager.Instance.currentNetState.Value == NetGameManager.GameState.Playing);

        if (!isPlaying)
        {
            powerGaugeSlider.value = 0f;
            UpdateBattleStatus();
            return;
        }

        if (netPlayer != null)
        {
            UpdateGauge(netPlayer.IsCharging, netPlayer.CurrentGaugeValue, netPlayer.CurrentThreshold);
        }
        UpdateBattleStatus();
    }

    // 게이지 그리는 함수 (싱글과 똑같지만 복사해서 독립적으로 사용)
    void UpdateGauge(bool isCharging, float gauge, float threshold)
    {
        if (isCharging)
        {
            powerGaugeSlider.value = gauge;
            sliderFillImage.color = (gauge >= threshold) ? successColor : normalColor;
            if (successZoneRect != null)
            {
                successZoneRect.anchorMin = new Vector2(threshold, 0f);
                successZoneRect.anchorMax = new Vector2(1f, 1f);
                successZoneRect.offsetMin = Vector2.zero;
                successZoneRect.offsetMax = Vector2.zero;
            }
        }
        else
        {
            powerGaugeSlider.value = 0f;
            sliderFillImage.color = normalColor;
        }
    }

    void FindMyNetPlayer()
    {
        var players = FindObjectsByType<NetPlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p.IsOwner)
            {
                SetNetPlayer(p);
                break;
            }
        }
    }

    void SetupRapidButton()
    {
        if (actionButtonObj == null) return;

        EventTrigger trigger = actionButtonObj.GetComponent<EventTrigger>();
        if (trigger != null) Destroy(trigger);

        trigger = actionButtonObj.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerDown;
        entry.callback.AddListener((data) => {
            if (netPlayer != null) netPlayer.TryAction();
        });
        trigger.triggers.Add(entry);
    }

    void UpdateBattleStatus()
    {
        if (distanceText == null) return;

        // 1. 텍스트 갱신 (가장 먼저 실행!)
        string statusKey = NetGameManager.Instance.GetBattleStatusKey();

        if (!string.IsNullOrEmpty(statusKey))
        {
            // Localization 테이블 이름이 "UITable"이라고 가정
            string localizedText = LocalizationSettings.StringDatabase.GetLocalizedString("UITable", statusKey);
            distanceText.text = localizedText;
        }
        else
        {
            distanceText.text = "";
        }

        // 2. 버튼 표시 여부 판단
        bool isWaiting = (distanceText.text == "WAITING FOR OPPONENT..." || distanceText.text == "WAITING...");

        if (multiExitButton != null)
        {
            multiExitButton.gameObject.SetActive(isWaiting);
        }
    }

    public void ShowResult(bool playerWin)
    {
        resultPanel.SetActive(true);
        if (actionButtonObj != null) actionButtonObj.SetActive(false);
        if (titleButton != null) titleButton.gameObject.SetActive(true);

        // 멀티는 재대결 버튼 보여줌
        if (restartButton != null) restartButton.gameObject.SetActive(true);

        if (playerWin) { SoundManager.Instance.PlaySFX(SFX.Win); resultText.text = "VICTORY!"; resultText.color = Color.green; }
        else { SoundManager.Instance.PlaySFX(SFX.Lose); resultText.text = "DEFEAT..."; resultText.color = Color.red; }
    }

    public void HideResult()
    {
        resultPanel.SetActive(false);
        if (actionButtonObj != null) actionButtonObj.SetActive(true);

        // 버튼 다시 켜주기
        if (restartButton != null) restartButton.interactable = true;
        if (rematchStatusText != null) rematchStatusText.text = "";
    }

    // ★ [추가] 콤보 텍스트 갱신 함수
    public void UpdateComboText(int combo)
    {
        if (comboText == null) return;

        if (combo > 1) // 2콤보 이상일 때만 표시
        {
            comboText.gameObject.SetActive(true);
            comboText.text = $"{combo} COMBO!";
        }
        else
        {
            comboText.gameObject.SetActive(false); // 0~1콤보일 땐 숨김
        }
    }

    // 점수 업데이트 함수 (ME / ENEMY 표시 추가)
    public void UpdateScoreUI(int s1, int s2)
    {
        // 1. 현재 네트워크 매니저가 없으면 중단 (안전장치)
        if (NetworkManager.Singleton == null) return;

        // 2. 내가 호스트인지 확인
        bool amIHost = NetworkManager.Singleton.IsServer;

        // 3. 라벨 결정 로직
        // - 내가 호스트라면? P1이 '나(ME)', P2가 '적(ENEMY)'
        // - 내가 게스트라면? P1이 '적(ENEMY)', P2가 '나(ME)'
        string p1Label = amIHost ? "ME" : "ENEMY";
        string p2Label = amIHost ? "ENEMY" : "ME";

        // 4. 텍스트 갱신 (이름: 점수 형태)
        if (p1ScoreText != null) p1ScoreText.text = $"{p1Label}: {s1}";
        if (p2ScoreText != null) p2ScoreText.text = $"{p2Label}: {s2}";
    }

    // ★ [추가] 재경기 UI 업데이트 (누가 수락했는지 버튼과 텍스트 제어)
    public void UpdateRematchUI(bool p1Ready, bool p2Ready)
    {
        bool isServer = NetworkManager.Singleton.IsServer;
        bool amIReady = isServer ? p1Ready : p2Ready;
        bool opponentReady = isServer ? p2Ready : p1Ready;

        // 1. 내가 눌렀으면 버튼 비활성화 (중복 클릭 방지)
        if (amIReady)
        {
            restartButton.interactable = false;
            if (rematchStatusText != null) rematchStatusText.text = "Waiting for Opponent...";
        }

        // 2. 상대방도 눌렀다면?
        if (opponentReady)
        {
            if (rematchStatusText != null) rematchStatusText.text = "Opponent wants Rematch!";
        }

        // 3. 둘 다 눌렀으면? (어차피 게임 재시작되면서 UI 초기화됨)
        if (p1Ready && p2Ready)
        {
            if (rematchStatusText != null) rematchStatusText.text = "Game Restarting...";
        }
    }
}