using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    [Header("Buttons")]
    public Button bgmButton;
    public Button sfxButton;
    public Button closeButton; // 닫기(X) 버튼

    // 색상 정의 (원하는 색으로 바꾸셔도 됩니다)
    private Color soundOnColor = Color.white; // 켜짐: 흰색 (원본 색상)
    private Color soundOffColor = new Color(1f, 0.3f, 0.3f); // 꺼짐: 약간 연한 빨간색

    private void Start()
    {
        // 버튼 기능 연결
        bgmButton.onClick.AddListener(OnClickBGM);
        sfxButton.onClick.AddListener(OnClickSFX);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        // 초기 상태 반영
        RefreshUI();
    }

    // 패널이 켜질 때마다 상태 최신화
    private void OnEnable()
    {
        RefreshUI();
    }

    public void OnClickBGM()
    {
        SoundManager.Instance.ToggleBGM();
        RefreshUI(); // 상태가 바뀌었으니 UI 갱신
    }

    public void OnClickSFX()
    {
        SoundManager.Instance.ToggleSFX();
        RefreshUI(); // 상태가 바뀌었으니 UI 갱신
    }

    // 패널 닫기 함수 (X버튼 용)
    public void ClosePanel()
    {
        // 효과음 추가 가능: SoundManager.Instance.PlaySFX(SFX.Click);
        settingsPanel.SetActive(false);
    }

    // ★ [핵심] 상태에 따라 버튼 색깔 바꾸기
    private void RefreshUI()
    {
        if (SoundManager.Instance == null) return;

        // BGM 버튼 색상 변경
        // SoundManager의 상태가 true(켜짐)면 흰색, false(꺼짐)면 빨간색 적용
        if (SoundManager.Instance.IsBgmOn)
            bgmButton.image.color = soundOnColor;
        else
            bgmButton.image.color = soundOffColor;

        // SFX 버튼 색상 변경
        if (SoundManager.Instance.IsSfxOn)
            sfxButton.image.color = soundOnColor;
        else
            sfxButton.image.color = soundOffColor;
    }

    public GameObject settingsPanel; // ★ 인스펙터에서 SettingsPanel 오브젝트 연결

    // 설정 버튼(톱니바퀴)에 연결할 함수
    public void OpenSettings()
    {
        settingsPanel.SetActive(true); // 켜기!
    }
}