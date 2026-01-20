using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class SingleUIManager : MonoBehaviour
{
    public static SingleUIManager Instance;

    [Header("Controls")]
    public GameObject actionButtonObj;
    public Button titleButton;
    // 싱글은 재시작 버튼이 필요 없으면 아예 변수에서 빼거나, 숨겨둡니다.
    public Button restartButton;

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

    // 싱글 플레이어 참조
    public PlayerController singlePlayer;

    // ★ [추가] 콤보 텍스트 (Inspector 연결 필요!)
    [Header("Combo Display")]
    public TextMeshProUGUI comboText;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(Instance.gameObject);
        Instance = this;
    }

    void Start()
    {
        HideResult();
        UpdateRoundText(1);

        // 플레이어 찾기
        FindSinglePlayer();

        // 버튼 세팅 (싱글 로직)
        SetupRapidButton();

        // 타이틀 버튼
        if (titleButton != null)
        {
            titleButton.onClick.AddListener(() => {
                if (GameManager.Instance != null) GameManager.Instance.GoToTitle();
            });
        }

        // 싱글은 재시작 버튼 숨김 (혹은 바로 재시작 기능 연결)
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(false);
            restartButton.onClick.AddListener(() => {
                if (GameManager.Instance != null) GameManager.Instance.RestartGame();
            });
        }

        // 시작 시 콤보 텍스트 숨김
        UpdateComboText(0);
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        if (singlePlayer == null) FindSinglePlayer();

        bool isPlaying = (GameManager.Instance.currentState == GameManager.GameState.Playing);

        if (!isPlaying)
        {
            powerGaugeSlider.value = 0f;
            UpdateStatusText(); // 상태 텍스트 갱신
            return;
        }

        // 게이지 업데이트
        if (singlePlayer != null)
        {
            UpdateGauge(singlePlayer.IsCharging, singlePlayer.CurrentGaugeValue, singlePlayer.CurrentThreshold);
        }

        UpdateStatusText();
    }

    // 게이지 그리는 함수 (독립적으로 구현)
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

    void FindSinglePlayer()
    {
        if (GameManager.Instance.playerController != null)
            singlePlayer = GameManager.Instance.playerController;
        else
            singlePlayer = FindFirstObjectByType<PlayerController>();
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
            if (singlePlayer != null) singlePlayer.TryAction();
        });
        trigger.triggers.Add(entry);
    }

    void UpdateStatusText()
    {
        if (distanceText == null) return;
        string status = GameManager.Instance.GetBattleStatusText();
        if (!string.IsNullOrEmpty(status)) distanceText.text = status;
    }

    public void ShowResult(bool playerWin)
    {
        resultPanel.SetActive(true);
        if (actionButtonObj != null) actionButtonObj.SetActive(false);
        if (titleButton != null) titleButton.gameObject.SetActive(true);

        // 싱글은 재시작 버튼 안 보이게 확실히 처리
        if (restartButton != null) restartButton.gameObject.SetActive(false);

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

    // ★ [추가] 콤보 텍스트 업데이트 함수
    public void UpdateComboText(int combo)
    {
        if (comboText == null) return;

        if (combo > 1) // 2콤보 이상일 때 표시
        {
            comboText.gameObject.SetActive(true);
            comboText.text = $"{combo} COMBO!";
        }
        else
        {
            comboText.gameObject.SetActive(false);
        }
    }
}