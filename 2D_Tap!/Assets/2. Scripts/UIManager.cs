using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

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
    public TextMeshProUGUI roundText;
    public GameObject resultPanel;

    [Header("Reference")]
    public PlayerController singlePlayer;
    public NetPlayerController netPlayer;

    private void Awake()
    {
        // 씬이 재로딩될 때마다 새로운 Instance가 되어야 함
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;
    }

    public void SetNetPlayer(NetPlayerController controller)
    {
        netPlayer = controller;
        singlePlayer = null;
        if (actionButtonObj != null) SetupRapidButton(actionButtonObj);
        Debug.Log($"UI: NetPlayer 연결됨 ({controller.name})");
    }

    void Start()
    {
        HideResult();

        // 버튼 초기화
        if (actionButtonObj != null) SetupRapidButton(actionButtonObj);

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(() => {
                // 1. 멀티플레이: 내 캐릭터를 통해 요청 전송
                if (NetGameManager.Instance != null && netPlayer != null)
                {
                    // ★ [수정] 매니저 직접 호출 X -> 내 플레이어 통해 호출 O
                    netPlayer.SendRematchRequestServerRpc();

                    HideResult();
                    if (distanceText != null) distanceText.text = "WAITING FOR OPPONENT...";
                }
                // 2. 싱글플레이: 바로 재시작
                else if (GameManager.Instance != null)
                {
                    GameManager.Instance.RestartGame();
                }
            });
        }

        if (titleButton != null)
        {
            titleButton.onClick.AddListener(() => {
                if (NetGameManager.Instance != null) NetGameManager.Instance.GoToTitle();
                else if (GameManager.Instance != null) GameManager.Instance.GoToTitle();
            });
        }

        if (multiExitButton != null)
        {
            multiExitButton.onClick.AddListener(() => {
                if (NetGameManager.Instance != null) NetGameManager.Instance.GoToTitle();
                else if (GameManager.Instance != null) GameManager.Instance.GoToTitle();
            });
        }

        UpdateRoundText(1);
    }

    void SetupRapidButton(GameObject btnObj)
    {
        EventTrigger trigger = btnObj.GetComponent<EventTrigger>();
        if (trigger != null) Destroy(trigger);
        trigger = btnObj.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerDown;

        entry.callback.AddListener((data) => {
            if (netPlayer != null) netPlayer.TryAction();
            else if (singlePlayer != null) singlePlayer.TryAction();
            else Debug.LogWarning("연결된 플레이어가 없습니다! (재연결 시도 중...)");
        });
        trigger.triggers.Add(entry);
    }

    void Update()
    {
        bool isPlaying = false;

        // 1. 멀티플레이 모드
        if (NetGameManager.Instance != null)
        {
            isPlaying = (NetGameManager.Instance.currentNetState.Value == NetGameManager.GameState.Playing);

            // ★★★ [핵심 수정] 재시작 후 연결이 끊겼다면 다시 찾기 (멀티)
            if (netPlayer == null)
            {
                var players = FindObjectsByType<NetPlayerController>(FindObjectsSortMode.None);
                foreach (var p in players)
                {
                    // "내 캐릭터(IsOwner)"를 찾아서 연결
                    if (p.IsOwner)
                    {
                        SetNetPlayer(p);
                        break;
                    }
                }
            }
        }
        // 2. 싱글플레이 모드
        else if (GameManager.Instance != null)
        {
            isPlaying = (GameManager.Instance.currentState == GameManager.GameState.Playing);

            // ★ [핵심 수정] 재시작 후 연결이 끊겼다면 다시 찾기 (싱글)
            if (singlePlayer == null)
            {
                if (GameManager.Instance.playerController != null)
                    singlePlayer = GameManager.Instance.playerController;
                else
                    singlePlayer = FindFirstObjectByType<PlayerController>();
            }
        }

        // 게임 중이 아니면 게이지 끄기
        if (!isPlaying)
        {
            powerGaugeSlider.value = 0f;
            UpdateBattleStatus();
            return;
        }

        // 게이지 갱신
        if (netPlayer != null)
            UpdateGauge(netPlayer.IsCharging, netPlayer.CurrentGaugeValue, netPlayer.CurrentThreshold);
        else if (singlePlayer != null)
            UpdateGauge(singlePlayer.IsCharging, singlePlayer.CurrentGaugeValue, singlePlayer.CurrentThreshold);
        else
        {
            powerGaugeSlider.value = 0f;
            sliderFillImage.color = normalColor;
        }

        UpdateBattleStatus();
    }

    void UpdateGauge(bool isCharging, float gauge, float threshold)
    {
        if (isCharging)
        {
            powerGaugeSlider.value = gauge;
            sliderFillImage.color = (gauge >= threshold) ? successColor : normalColor;
            UpdateSuccessZoneVisual(threshold);
        }
        else
        {
            powerGaugeSlider.value = 0f;
            sliderFillImage.color = normalColor;
        }
    }

    void UpdateSuccessZoneVisual(float threshold)
    {
        if (successZoneRect == null) return;
        successZoneRect.anchorMin = new Vector2(threshold, 0f);
        successZoneRect.anchorMax = new Vector2(1f, 1f);
        successZoneRect.offsetMin = Vector2.zero;
        successZoneRect.offsetMax = Vector2.zero;
    }

    public void UpdateRoundText(int round)
    {
        if (roundText != null) roundText.text = $"ROUND {round}";
    }

    void UpdateBattleStatus()
    {
        if (distanceText == null) return;
        if (distanceText.text == "WAITING FOR OPPONENT...")
        {
            if(NetGameManager.Instance != null)
            {
                multiExitButton.gameObject.SetActive(true);
            }
            return;
        }

        string status = "";
        if (NetGameManager.Instance != null) status = NetGameManager.Instance.GetBattleStatusText();
        else if (GameManager.Instance != null) status = GameManager.Instance.GetBattleStatusText();

        if (!string.IsNullOrEmpty(status)) distanceText.text = status;
        if (NetGameManager.Instance != null)
        {
            multiExitButton.gameObject.SetActive(false);
        }
        switch (distanceText.text)
        {
            case "DANGER!!": distanceText.color = Color.red; break;
            case "FINISH HIM!": distanceText.color = new Color(1f, 0.8f, 0f); break;
            case "ADVANTAGE": distanceText.color = Color.cyan; break;
            case "DEFENSE!": distanceText.color = new Color(1f, 0.5f, 0f); break;
            case "EQUAL": distanceText.color = Color.white; break;
            case "WAITING...":
                distanceText.color = Color.white;
                if (NetGameManager.Instance != null)
                {
                    multiExitButton.gameObject.SetActive(true);
                }
                break;
            default: distanceText.color = Color.white; break;
        }
    }

    public void ShowResult(bool playerWin)
    {
        resultPanel.SetActive(true);
        if (actionButtonObj != null) actionButtonObj.SetActive(false);

        if (restartButton != null) restartButton.gameObject.SetActive(true);
        if (titleButton != null) titleButton.gameObject.SetActive(true);

        if (playerWin)
        {
            resultText.text = "VICTORY!";
            resultText.color = Color.green;
        }
        else
        {
            resultText.text = "DEFEAT...";
            resultText.color = Color.red;
        }
    }

    public void HideResult()
    {
        resultPanel.SetActive(false);
        if (actionButtonObj != null) actionButtonObj.SetActive(true);
    }
}