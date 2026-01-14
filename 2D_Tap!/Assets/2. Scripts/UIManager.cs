using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Controls")]
    public Button actionButton;
    public Button restartButton;

    [Header("Gauge Display")]
    public Slider powerGaugeSlider;
    public Image sliderFillImage;
    public Color normalColor = Color.white;
    public Color successColor = Color.green;

    [Header("Game Info")]
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI distanceText; // 여기에 상태 텍스트 표시
    public GameObject resultPanel;

    [Header("Reference")]
    public PlayerUnit playerUnit;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        resultPanel.SetActive(false);

        if (actionButton != null && playerUnit != null)
            actionButton.onClick.AddListener(() => playerUnit.OnActionBtnPressed());

        if (restartButton != null)
            restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
    }

    void Update()
    {
        if (GameManager.Instance.currentState != GameManager.GameState.Playing)
        {
            powerGaugeSlider.value = 0f;
            // 게임 중이 아닐 땐 상태 텍스트 숨기기 or 초기화
            if (distanceText != null) distanceText.text = "";
            return;
        }

        if (playerUnit == null) return;

        // 1. 게이지 표시
        if (playerUnit.currentState == SumoUnit.State.Charging)
        {
            float gauge = playerUnit.currentGaugeValue;
            powerGaugeSlider.value = gauge;

            // 플레이어의 현재 난이도 기준을 가져와서 색상 변경
            sliderFillImage.color = (gauge >= playerUnit.currentSuccessThreshold) ? successColor : normalColor;
        }
        else
        {
            powerGaugeSlider.value = 0f;
            sliderFillImage.color = normalColor;
        }

        // 2. [변경됨] 전투 상태 텍스트 표시 (숫자 X -> 영어 텍스트 O)
        UpdateBattleStatus();
    }

    void UpdateBattleStatus()
    {
        if (distanceText == null) return;

        // GameManager에서 현재 상태 텍스트(영어) 가져오기
        string status = GameManager.Instance.GetBattleStatusText();
        distanceText.text = status;

        // 텍스트 내용에 따라 색상 변경
        switch (status)
        {
            case "DANGER!!":
                distanceText.color = Color.red;         // 위험: 빨강
                // 폰트 크기를 키우거나 흔들리는 효과를 추가해도 좋음
                break;
            case "FINISH HIM!":
                distanceText.color = new Color(1f, 0.8f, 0f); // 마무리: 금색/노랑
                break;
            case "ADVANTAGE":
                distanceText.color = Color.cyan;        // 우세: 청록
                break;
            case "DEFENSE!":
                distanceText.color = new Color(1f, 0.5f, 0f); // 수비: 주황
                break;
            case "EQUAL":
                distanceText.color = Color.white;       // 대등: 흰색
                break;
        }
    }

    public void ShowResult(bool playerWin)
    {
        resultPanel.SetActive(true);
        if (actionButton != null) actionButton.gameObject.SetActive(false);

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
}