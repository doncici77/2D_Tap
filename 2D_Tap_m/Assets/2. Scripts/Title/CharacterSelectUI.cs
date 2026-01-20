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

        // 4. 초기 상태 적용 (시작할 땐 팝업 닫고, 버튼 이미지 갱신)
        ClosePopup();
        UpdateUI();
    }

    // 팝업 열기
    public void OpenPopup()
    {
        if (popupPanel != null) popupPanel.SetActive(true);
        UpdateUI(); // 열 때 UI 한 번 갱신
    }

    // 팝업 닫기
    public void ClosePopup()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
    }

    void GenerateButtons()
    {
        // 기존 버튼 삭제 (중복 방지)
        foreach (Transform child in buttonContainer) Destroy(child.gameObject);
        generatedButtons.Clear();

        // characterSprites.Length 대신 DB 사용
        if (characterDB == null) return;

        // 스프라이트 개수만큼 버튼 생성
        for (int i = 0; i < characterDB.Count; i++)
        {
            int index = i; // 클로저 문제 해결용 로컬 변수

            GameObject newBtnObj = Instantiate(buttonPrefab, buttonContainer);
            Button btn = newBtnObj.GetComponent<Button>();
            Image btnImage = newBtnObj.GetComponent<Image>();

            // 버튼 이미지 설정
            if (btnImage != null)
            {
                btnImage.sprite = characterDB.GetSkin(index); // ★ 변경점
                btnImage.preserveAspect = true;
            }

            // 클릭 시 "캐릭터 선택" 함수 실행
            btn.onClick.AddListener(() => OnSelectCharacter(index));

            generatedButtons.Add(btn);
        }
    }

    void OnSelectCharacter(int index)
    {
        currentSelection = index;

        // 저장 (매우 중요)
        PlayerPrefs.SetInt("MyCharacterID", currentSelection);
        PlayerPrefs.Save();

        Debug.Log($"캐릭터 {index}번 선택됨!");
        UpdateUI();
    }

    void UpdateUI()
    {
        if (mainPreviewImage != null)
            mainPreviewImage.sprite = characterDB.GetSkin(currentSelection); // ★ 변경점

        // 3. 팝업 내 리스트 버튼 상태 갱신 (선택된 것만 어둡게 처리 등)
        for (int i = 0; i < generatedButtons.Count; i++)
        {
            // 선택된 버튼은 클릭 못하게 막아서 "선택됨" 표시
            generatedButtons[i].interactable = (i != currentSelection);
        }
    }
}