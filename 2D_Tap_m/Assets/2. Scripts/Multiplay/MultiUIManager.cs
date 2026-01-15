using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Unity.Netcode; // 멀티용 필수

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
    public TextMeshProUGUI roundText;
    public GameObject resultPanel;

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
        UpdateRoundText(1);

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

    void GoToTitle()
    {
        if (NetGameManager.Instance != null) NetGameManager.Instance.GoToTitle();
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
        string status = NetGameManager.Instance.GetBattleStatusText();
        if (!string.IsNullOrEmpty(status)) distanceText.text = status;

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

        if (playerWin) { resultText.text = "VICTORY!"; resultText.color = Color.green; }
        else { resultText.text = "DEFEAT..."; resultText.color = Color.red; }
    }

    public void HideResult()
    {
        resultPanel.SetActive(false);
        if (actionButtonObj != null) actionButtonObj.SetActive(true);
    }

    public void UpdateRoundText(int round)
    {
        if (roundText != null) roundText.text = $"ROUND {round}";
    }
}