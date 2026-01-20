using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using TMPro;

public class GoogleManager : MonoBehaviour
{
    public static GoogleManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI logText;
    public GameObject loginPanel;

    public GameObject guestLoginButton;  // 게스트 입장 버튼
    public GameObject googleLoginButton; // 구글 로그인(재시도) 버튼

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 1. 시작 시 버튼 숨기기
        if (guestLoginButton != null) guestLoginButton.SetActive(false);
        if (googleLoginButton != null) googleLoginButton.SetActive(false);

        // 안드로이드 폰에서만 실행
        // 설정(Configuration) 코드 삭제됨 -> 바로 Activate만 하면 끝!
        PlayGamesPlatform.DebugLogEnabled = true;
        PlayGamesPlatform.Activate();

        SignIn();
    }

    public void SignIn()
    {
        if (logText != null) logText.text = "Logging in...";

        // 재시도 시 버튼 다시 숨기기
        if (guestLoginButton != null) guestLoginButton.SetActive(false);
        if (googleLoginButton != null) googleLoginButton.SetActive(false);

        PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
    }

    internal void ProcessAuthentication(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
            string name = PlayGamesPlatform.Instance.GetUserDisplayName();
            Debug.Log($"Google Login Success: {name}");

            // 성공하면 패널 닫기
            if (loginPanel != null) loginPanel.SetActive(false);
        }
        else
        {
            // 실패 시 메시지 변경
            if (logText != null) logText.text = "Login failed. Select login method.";

            // ★ 실패했으니 유저에게 선택권을 줌 (둘 다 켜기)
            if (guestLoginButton != null) guestLoginButton.SetActive(true);
            if (googleLoginButton != null) googleLoginButton.SetActive(true);
        }
    }

    public void LoginAsGuest()
    {
        string guestName = "Guest_" + Random.Range(1000, 9999);
        Debug.Log($"Guest Login: {guestName}");

        PlayerPrefs.SetString("PlayerName", guestName);

        // 게스트 입장 시 패널 닫기
        if (loginPanel != null) loginPanel.SetActive(false);
    }
}