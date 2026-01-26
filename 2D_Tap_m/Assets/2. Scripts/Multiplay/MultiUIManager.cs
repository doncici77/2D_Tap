using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class MultiUIManager : MonoBehaviour
{
    public static MultiUIManager Instance;

    [Header("Controls")]
    public GameObject actionButtonObj;
    public Button restartButton;
    public Button titleButton;
    public Button multiExitButton;

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
    public TextMeshProUGUI comboText;

    [Header("Score Info")]
    public TextMeshProUGUI p1ScoreText;
    public TextMeshProUGUI p2ScoreText;

    [Header("Rematch Info")]
    public TextMeshProUGUI rematchStatusText;

    public NetPlayerController netPlayer;

    // ★ 로컬라이제이션 테이블 이름 (유니티 에셋에서 만든 테이블 이름과 똑같아야 함)
    private const string TableName = "Table";

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(Instance.gameObject);
        Instance = this;
    }

    public void SetNetPlayer(NetPlayerController controller)
    {
        netPlayer = controller;
        SetupRapidButton();
        Debug.Log($"UI: 멀티 플레이어 연결됨 - {controller.name}");
    }

    void Start()
    {
        HideResult();

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(() => {
                if (netPlayer != null)
                {
                    netPlayer.SendRematchRequestServerRpc();
                    HideResult();

                    // ★ [수정] 대기 메시지 로컬라이징 적용
                    if (distanceText != null)
                    {
                        // "상대방 기다리는 중..." 키값 호출
                        distanceText.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, "ui_waiting_opponent");
                    }
                    UpdateBattleStatus();
                }
            });
        }

        if (titleButton != null)
            titleButton.onClick.AddListener(() => GoToTitle());

        if (multiExitButton != null)
            multiExitButton.onClick.AddListener(() => GoToTitle());
    }

    async void GoToTitle()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            if (!string.IsNullOrEmpty(LobbyManager.CurrentLobbyId))
            {
                try
                {
                    await LobbyService.Instance.DeleteLobbyAsync(LobbyManager.CurrentLobbyId);
                    LobbyManager.CurrentLobbyId = null;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"방 삭제 실패: {e.Message}");
                }
            }
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene("Title");
    }

    void Update()
    {
        if (NetGameManager.Instance == null) return;

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

        string statusKey = NetGameManager.Instance.GetBattleStatusKey();

        // 1. 재경기 대기 중인지 확인 (rematchStatusText에 글자가 있으면 대기 중인 것으로 간주)
        bool isRematchWaiting = false;
        if (rematchStatusText != null && !string.IsNullOrEmpty(rematchStatusText.text))
        {
            isRematchWaiting = true;
        }

        // 2. 텍스트 업데이트 (재경기 대기 중이면 NetGameManager 값 무시하고 기존 텍스트 유지)
        if (!isRematchWaiting && !string.IsNullOrEmpty(statusKey))
        {
            distanceText.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, statusKey);
        }
        else if (string.IsNullOrEmpty(statusKey) && !isRematchWaiting)
        {
            distanceText.text = "";
        }

        // 3. 버튼 활성화 로직
        // - ui_waiting (서버 접속 대기)
        // - ui_waiting_opponent (상대 접속 대기)
        // - isRematchWaiting (재경기 수락 대기)
        // 이 중 하나라도 해당되면 나가기 버튼을 보여줍니다.
        bool isWaiting = (statusKey == "ui_waiting" || statusKey == "ui_waiting_opponent" || isRematchWaiting || statusKey == "status_waiting");

        if (multiExitButton != null)
        {
            multiExitButton.gameObject.SetActive(isWaiting);
        }
    }

    // ★ [수정] 결과창 로컬라이징
    public void ShowResult(bool playerWin)
    {
        resultPanel.SetActive(true);
        if (actionButtonObj != null) actionButtonObj.SetActive(false);
        if (titleButton != null) titleButton.gameObject.SetActive(true);
        if (restartButton != null) restartButton.gameObject.SetActive(true);

        if (playerWin)
        {
            SoundManager.Instance.PlaySFX(SFX.Win);
            // "YOU WIN" -> 번역된 승리 텍스트
            resultText.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, "ui_win");
            resultText.color = Color.green;
        }
        else
        {
            SoundManager.Instance.PlaySFX(SFX.Lose);
            // "YOU LOSE" -> 번역된 패배 텍스트
            resultText.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, "ui_lose");
            resultText.color = Color.red;
        }
    }

    public void HideResult()
    {
        resultPanel.SetActive(false);
        if (actionButtonObj != null) actionButtonObj.SetActive(true);
        if (restartButton != null) restartButton.interactable = true;
        if (rematchStatusText != null) rematchStatusText.text = "";
    }

    // ★ [수정] 콤보 텍스트 로컬라이징 (Smart String 사용)
    public void UpdateComboText(int combo)
    {
        if (comboText == null) return;

        if (combo > 1)
        {
            comboText.gameObject.SetActive(true);

            // ★ [수정] combo(int)를 new object[] { combo }로 감싸서 전달해야 함
            comboText.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, "ui_combo", new object[] { combo });
        }
        else
        {
            comboText.gameObject.SetActive(false);
        }
    }

    // ★ [수정] 점수판 로컬라이징
    public void UpdateScoreUI(int s1, int s2)
    {
        if (NetworkManager.Singleton == null) return;

        bool amIHost = NetworkManager.Singleton.IsServer;

        // "나", "적" 텍스트 가져오기
        string meText = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, "ui_me");
        string enemyText = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, "ui_enemy");

        string p1Label = amIHost ? meText : enemyText;
        string p2Label = amIHost ? enemyText : meText;

        if (p1ScoreText != null) p1ScoreText.text = $"{p1Label}: {s1}";
        if (p2ScoreText != null) p2ScoreText.text = $"{p2Label}: {s2}";
    }

    // ★ [수정] 재경기 UI 로컬라이징
    public void UpdateRematchUI(bool p1Ready, bool p2Ready)
    {
        bool isServer = NetworkManager.Singleton.IsServer;
        bool amIReady = isServer ? p1Ready : p2Ready;
        bool opponentReady = isServer ? p2Ready : p1Ready;

        if (amIReady)
        {
            restartButton.interactable = false;
            if (rematchStatusText != null)
                rematchStatusText.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, "rematch_wait");
        }

        if (opponentReady)
        {
            if (rematchStatusText != null)
                rematchStatusText.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, "rematch_request");
        }

        if (p1Ready && p2Ready)
        {
            if (rematchStatusText != null)
                rematchStatusText.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, "rematch_restart");
        }
    }
}