using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CharacterSelectUI : MonoBehaviour
{
    [Header("Data Source")]
    public CharacterDatabase characterDB;

    [Header("Popup Settings")]
    public GameObject popupPanel;     // 캐릭터 선택창 전체
    public GameObject adConfirmPopup; // "광고 볼래?" 팝업
    public GameObject noInternetPopup; // ★ [추가] "인터넷 연결해라" 경고 팝업

    [Header("Buttons")]
    public Button openPopupButton;
    public Button closePopupButton;

    public Button adYesButton;
    public Button adNoButton;

    // ★ [추가] 인터넷 경고 팝업 닫기 버튼 (확인 버튼)
    public Button noInternetOkButton;

    [Header("Generation Settings")]
    public Transform buttonContainer;
    public GameObject buttonPrefab;
    public Image mainPreviewImage;

    private List<Button> generatedButtons = new List<Button>();
    private int currentSelection = 0;
    private int pendingCharacterIndex = -1;

    void Start()
    {
        currentSelection = PlayerPrefs.GetInt("MyCharacterID", 0);
        GenerateButtons();

        if (openPopupButton != null) openPopupButton.onClick.AddListener(OpenPopup);
        if (closePopupButton != null) closePopupButton.onClick.AddListener(ClosePopup);

        if (adYesButton != null) adYesButton.onClick.AddListener(OnAdYesClicked);
        if (adNoButton != null) adNoButton.onClick.AddListener(OnAdNoClicked);

        // ★ [추가] 인터넷 경고창 닫기 버튼 연결
        if (noInternetOkButton != null) noInternetOkButton.onClick.AddListener(CloseNoInternetPopup);

        // 초기화
        if (popupPanel != null) popupPanel.SetActive(false);
        if (adConfirmPopup != null) adConfirmPopup.SetActive(false);
        if (noInternetPopup != null) noInternetPopup.SetActive(false);

        UpdateUI();
    }

    public void OpenPopup() { if (popupPanel != null) popupPanel.SetActive(true); UpdateUI(); }
    public void ClosePopup() { if (popupPanel != null) popupPanel.SetActive(false); }

    // ★ [추가] 경고창 닫기 함수
    public void CloseNoInternetPopup() { if (noInternetPopup != null) noInternetPopup.SetActive(false); }

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

            btn.onClick.AddListener(() => OnSelectCharacter(index));
            generatedButtons.Add(btn);
        }
    }

    void OnSelectCharacter(int index)
    {
        if (index == currentSelection) return;

        // 4번째(3번 인덱스) 이상이면 -> 광고 필요
        if (index >= 3)
        {
            // ★ [핵심] 인터넷 연결 상태 확인
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                // 인터넷이 없음 -> 경고창 띄우고 종료
                Debug.Log("인터넷 연결이 없습니다.");
                if (noInternetPopup != null) noInternetPopup.SetActive(true);
                return;
            }

            // 인터넷 있음 -> 정상 진행
            pendingCharacterIndex = index;
            if (adConfirmPopup != null) adConfirmPopup.SetActive(true);
        }
        else
        {
            // 무료 캐릭터는 인터넷 없어도 OK
            ConfirmSelection(index);
        }
    }

    void OnAdYesClicked()
    {
        if (adConfirmPopup != null) adConfirmPopup.SetActive(false);

        // 광고 재생 전 한 번 더 체크해도 좋음 (선택사항)
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            if (noInternetPopup != null) noInternetPopup.SetActive(true);
            return;
        }

        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.ShowAd(() =>
            {
                ConfirmSelection(pendingCharacterIndex);
            });
        }
        else
        {
            ConfirmSelection(pendingCharacterIndex);
        }
    }

    void OnAdNoClicked()
    {
        if (adConfirmPopup != null) adConfirmPopup.SetActive(false);
        pendingCharacterIndex = -1;
    }

    void ConfirmSelection(int index)
    {
        // 1. 현재 선택된 캐릭터 변수 업데이트
        currentSelection = index;

        // 2. 저장 로직 (매니저에게 위임)
        if (DataManager.Instance != null)
        {
            // 매니저가 있으면: 로컬 + 파이어베이스 둘 다 알아서 저장해줌
            DataManager.Instance.SaveCharacter(index);
        }
        else
        {
            // 매니저가 없을 때(비상용): 로컬에만 직접 저장
            PlayerPrefs.SetInt("MyCharacterID", index);
            PlayerPrefs.Save();
        }

        // 3. UI 갱신
        UpdateUI();
    }

    void UpdateUI()
    {
        if (mainPreviewImage != null && characterDB != null)
            mainPreviewImage.sprite = characterDB.GetSkin(currentSelection);

        for (int i = 0; i < generatedButtons.Count; i++)
        {
            generatedButtons[i].interactable = (i != currentSelection);
        }
    }
}