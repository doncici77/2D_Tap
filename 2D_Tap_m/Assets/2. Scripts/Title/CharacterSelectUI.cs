using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CharacterSelectUI : MonoBehaviour
{
    [Header("Data Source")]
    public CharacterDatabase characterDB;

    [Header("Popup Settings")]
    public GameObject popupPanel;   // 캐릭터 선택 팝업창 (Panel)
    public Button openPopupButton;  // 타이틀 화면에 있는 '캐릭터 변경' 버튼
    public Button closePopupButton; // 팝업창 닫기 버튼 (X 버튼 등)

    [Header("Generation Settings")]
    public Transform buttonContainer; // 팝업 안의 버튼들이 생성될 부모 (Grid)
    public GameObject buttonPrefab;   // 복제해서 사용할 버튼 프리팹

    [Header("Preview (Optional)")]
    public Image mainPreviewImage;    // 팝업 내에서 크게 보여줄 미리보기 이미지 (없어도 됨)

    private List<Button> generatedButtons = new List<Button>();
    private int currentSelection = 0;

    void Start()
    {
        // 1. 저장된 정보 불러오기
        currentSelection = PlayerPrefs.GetInt("MyCharacterID", 0);

        // 2. 버튼 동적 생성
        GenerateButtons();

        // 3. 버튼 이벤트 연결 (열기/닫기)
        if (openPopupButton != null)
            openPopupButton.onClick.AddListener(OpenPopup);

        if (closePopupButton != null)
            closePopupButton.onClick.AddListener(ClosePopup);

        // 4. 초기 상태 적용
        ClosePopup();
        UpdateUI();
    }

    public void OpenPopup()
    {
        if (popupPanel != null) popupPanel.SetActive(true);
        UpdateUI();
    }

    public void ClosePopup()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
    }

    void GenerateButtons()
    {
        foreach (Transform child in buttonContainer) Destroy(child.gameObject);
        generatedButtons.Clear();

        if (characterDB == null) return;

        for (int i = 0; i < characterDB.Count; i++)
        {
            int index = i;

            GameObject newBtnObj = Instantiate(buttonPrefab, buttonContainer);
            Button btn = newBtnObj.GetComponent<Button>();
            Image btnImage = newBtnObj.GetComponent<Image>();

            if (btnImage != null)
            {
                btnImage.sprite = characterDB.GetSkin(index);
                btnImage.preserveAspect = true;
            }

            // 클릭 시 로직 연결
            btn.onClick.AddListener(() => OnSelectCharacter(index));

            generatedButtons.Add(btn);
        }
    }

    // ★ [수정됨] 클릭 시 광고 여부를 판단하는 함수
    void OnSelectCharacter(int index)
    {
        // 이미 선택된 캐릭터를 또 누르면 무시 (선택사항)
        if (index == currentSelection) return;

        // ★ 조건: 4번째 캐릭터(인덱스 3) 이상이면 광고 보여주기
        if (index >= 3)
        {
            Debug.Log($"[Ad] {index}번 캐릭터 선택 시도 -> 광고 요청");

            if (AdMobManager.Instance != null)
            {
                // 광고를 보여주고, 닫으면(콜백) ConfirmSelection을 실행해라!
                AdMobManager.Instance.ShowAd(() =>
                {
                    ConfirmSelection(index);
                });
            }
            else
            {
                // 광고 매니저가 없으면(테스트 등) 그냥 선택
                Debug.LogWarning("AdMobManager가 없습니다. 광고 없이 진행합니다.");
                ConfirmSelection(index);
            }
        }
        else
        {
            // 0, 1, 2번 캐릭터는 그냥 바로 선택
            ConfirmSelection(index);
        }
    }

    // ★ [추가됨] 실제로 저장하고 UI 바꾸는 진짜 선택 함수
    // (광고를 다 보고 닫았거나, 무료 캐릭터일 때 호출됨)
    void ConfirmSelection(int index)
    {
        currentSelection = index;

        // 저장
        PlayerPrefs.SetInt("MyCharacterID", currentSelection);
        PlayerPrefs.Save();

        Debug.Log($"캐릭터 {index}번 선택 완료 및 저장됨!");
        UpdateUI();
    }

    void UpdateUI()
    {
        // 팝업 내 큰 이미지 갱신
        if (mainPreviewImage != null && characterDB != null)
            mainPreviewImage.sprite = characterDB.GetSkin(currentSelection);

        // 버튼 활성화/비활성화 상태 갱신
        for (int i = 0; i < generatedButtons.Count; i++)
        {
            generatedButtons[i].interactable = (i != currentSelection);
        }
    }
}