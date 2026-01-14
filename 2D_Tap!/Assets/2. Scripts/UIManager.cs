using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems; // ★ [필수] 이게 있어야 터치 즉시 반응 구현 가능

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Controls")]
    public GameObject actionButtonObj; // ★ [변경] Button 대신 GameObject로 받아도 됨 (EventTrigger를 붙일 거라)
    public Button restartButton;
    public Button titleButton;

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
    public PlayerController playerController;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        HideResult();

        // ★ [핵심] 버튼을 "누르는 순간" 반응하도록 설정하는 함수 호출
        if (actionButtonObj != null && playerController != null)
        {
            SetupRapidButton(actionButtonObj);
        }

        if (restartButton != null)
            restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());

        if (titleButton != null)
            titleButton.onClick.AddListener(() => GameManager.Instance.GoToTitle());

        UpdateRoundText(1);
    }

    // ★ [추가된 함수] 버튼에 "즉시 반응" 기능을 심어주는 로직
    void SetupRapidButton(GameObject btnObj)
    {
        // 1. 이미 있는 EventTrigger를 가져오거나 없으면 추가
        EventTrigger trigger = btnObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btnObj.AddComponent<EventTrigger>();

        // 2. "PointerDown" (누르는 순간) 이벤트 생성
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerDown;

        // 3. 실행할 함수 연결 (PlayerController의 TryAction)
        entry.callback.AddListener((data) => { playerController.TryAction(); });

        // 4. 트리거에 등록
        trigger.triggers.Add(entry);
    }

    void Update()
    {
        if (GameManager.Instance.currentState != GameManager.GameState.Playing)
        {
            powerGaugeSlider.value = 0f;
            if (distanceText != null) distanceText.text = "";
            return;
        }

        if (playerController == null) return;

        if (playerController.IsCharging)
        {
            float gauge = playerController.CurrentGaugeValue;
            float threshold = playerController.CurrentThreshold;

            powerGaugeSlider.value = gauge;
            sliderFillImage.color = (gauge >= threshold) ? successColor : normalColor;
            UpdateSuccessZoneVisual(threshold);
        }
        else
        {
            powerGaugeSlider.value = 0f;
            sliderFillImage.color = normalColor;
        }

        UpdateBattleStatus();
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
        string status = GameManager.Instance.GetBattleStatusText();
        distanceText.text = status;

        switch (status)
        {
            case "DANGER!!": distanceText.color = Color.red; break;
            case "FINISH HIM!": distanceText.color = new Color(1f, 0.8f, 0f); break;
            case "ADVANTAGE": distanceText.color = Color.cyan; break;
            case "DEFENSE!": distanceText.color = new Color(1f, 0.5f, 0f); break;
            case "EQUAL": distanceText.color = Color.white; break;
        }
    }

    public void ShowResult(bool playerWin)
    {
        resultPanel.SetActive(true);
        if (actionButtonObj != null) actionButtonObj.SetActive(false);

        if (playerWin)
        {
            resultText.text = "VICTORY!";
            resultText.color = Color.green;
            if (restartButton != null) restartButton.gameObject.SetActive(false);
            if (titleButton != null) titleButton.gameObject.SetActive(false);
        }
        else
        {
            resultText.text = "DEFEAT...";
            resultText.color = Color.red;
            if (restartButton != null) restartButton.gameObject.SetActive(true);
            if (titleButton != null) titleButton.gameObject.SetActive(true);
        }
    }

    public void HideResult()
    {
        resultPanel.SetActive(false);
        if (actionButtonObj != null) actionButtonObj.SetActive(true);
    }
}