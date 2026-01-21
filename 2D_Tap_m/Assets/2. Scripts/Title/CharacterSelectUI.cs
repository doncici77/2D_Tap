using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CharacterSelectUI : MonoBehaviour
{
    [Header("Data Source")]
    public CharacterDatabase characterDB;

    [Header("Popup Settings")]
    public GameObject popupPanel;
    public GameObject adConfirmPopup;
    public GameObject noInternetPopup;

    [Header("Buttons")]
    public Button openPopupButton;
    public Button closePopupButton;
    public Button adYesButton;
    public Button adNoButton;
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
        // 1. 일단 로컬 저장된 정보로 UI 초기화
        currentSelection = PlayerPrefs.GetInt("MyCharacterID", 0);
        GenerateButtons();

        if (openPopupButton != null) openPopupButton.onClick.AddListener(OpenPopup);
        if (closePopupButton != null) closePopupButton.onClick.AddListener(ClosePopup);
        if (adYesButton != null) adYesButton.onClick.AddListener(OnAdYesClicked);
        if (adNoButton != null) adNoButton.onClick.AddListener(OnAdNoClicked);
        if (noInternetOkButton != null) noInternetOkButton.onClick.AddListener(CloseNoInternetPopup);

        if (popupPanel != null) popupPanel.SetActive(false);
        if (adConfirmPopup != null) adConfirmPopup.SetActive(false);
        if (noInternetPopup != null) noInternetPopup.SetActive(false);

        UpdateUI();

        // ★ [추가] 매니저한테 "데이터 바뀌면 나한테 알려줘(RefreshUI)" 라고 등록
        if (DataManager.Instance != null)
        {
            DataManager.Instance.OnCharacterDataLoaded += RefreshUI;
        }
    }

    // ★ [추가] UI가 파괴될 때 구독 해제 (메모리 누수 방지)
    void OnDestroy()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.OnCharacterDataLoaded -= RefreshUI;
        }
    }

    // ★ [추가] 매니저가 부르는 함수 (외부에서 데이터 로드 완료 시 실행됨)
    void RefreshUI(int newIndex)
    {
        currentSelection = newIndex;
        UpdateUI(); // 화면 갱신!
    }

    public void OpenPopup() { if (popupPanel != null) popupPanel.SetActive(true); UpdateUI(); }
    public void ClosePopup() { if (popupPanel != null) popupPanel.SetActive(false); }
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

        if (index >= 3)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                if (noInternetPopup != null) noInternetPopup.SetActive(true);
                return;
            }

            pendingCharacterIndex = index;
            if (adConfirmPopup != null) adConfirmPopup.SetActive(true);
        }
        else
        {
            ConfirmSelection(index);
        }
    }

    void OnAdYesClicked()
    {
        if (adConfirmPopup != null) adConfirmPopup.SetActive(false);

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            if (noInternetPopup != null) noInternetPopup.SetActive(true);
            return;
        }

        // (AdMob 부분은 그대로 유지)
        ConfirmSelection(pendingCharacterIndex);
    }

    void OnAdNoClicked()
    {
        if (adConfirmPopup != null) adConfirmPopup.SetActive(false);
        pendingCharacterIndex = -1;
    }

    void ConfirmSelection(int index)
    {
        currentSelection = index;

        if (DataManager.Instance != null)
        {
            DataManager.Instance.SaveCharacter(index);
        }
        else
        {
            PlayerPrefs.SetInt("MyCharacterID", index);
            PlayerPrefs.Save();
        }

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