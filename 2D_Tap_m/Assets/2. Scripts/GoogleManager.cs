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

    public GameObject guestLoginButton;
    public GameObject googleLoginButton;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (loginPanel != null) loginPanel.SetActive(true);
        if (guestLoginButton != null) guestLoginButton.SetActive(false);
        if (googleLoginButton != null) googleLoginButton.SetActive(false);

#if UNITY_EDITOR
        Invoke("LoginAsGuest", 0.5f);
#else
        PlayGamesPlatform.DebugLogEnabled = true;
        PlayGamesPlatform.Activate();

        // 1. 이미 구글에 연결되어 있다면? -> 바로 통과
        if (PlayGamesPlatform.Instance.IsAuthenticated())
        {
            Debug.Log("이미 구글 로그인 상태입니다.");
            if (loginPanel != null) loginPanel.SetActive(false);
            return;
        }

        // 2. 연결 안 됨 -> "마지막에 뭘로 로그인했었지?" 확인
        string lastLoginType = PlayerPrefs.GetString("LastLoginType", "");

        if (lastLoginType == "Guest")
        {
            // "아, 저번에 게스트였네? 그럼 바로 게스트로 입장!"
            LoginAsGuest();
        }
        else
        {
            // "구글이었거나 처음이네? 구글 로그인 시도!"
            SignIn();
        }
#endif
    }

    public void SignIn()
    {
        if (logText != null) logText.text = "Logging in...";
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

            // ★ [저장] 나 구글 로그인 성공했어! 기억해!
            PlayerPrefs.SetString("LastLoginType", "Google");
            PlayerPrefs.Save();

            if (loginPanel != null) loginPanel.SetActive(false);
        }
        else
        {
            if (logText != null) logText.text = "Login Failed. Choose Mode.";

            // 실패 시 버튼 두 개 띄우기
            if (guestLoginButton != null) guestLoginButton.SetActive(true);
            if (googleLoginButton != null) googleLoginButton.SetActive(true);
        }
    }

    public void LoginAsGuest()
    {
        // 1. 게스트 ID 불러오기 or 생성
        string guestName = PlayerPrefs.GetString("GuestID", "");
        if (string.IsNullOrEmpty(guestName))
        {
            guestName = "Guest_" + Random.Range(1000, 9999);
            PlayerPrefs.SetString("GuestID", guestName);
        }

        // ★ [저장] 나 게스트로 로그인했어! 기억해!
        PlayerPrefs.SetString("LastLoginType", "Guest");
        PlayerPrefs.Save();

        Debug.Log($"Guest Login: {guestName}");

        // 2. 패널 닫기 (바로 통과)
        if (loginPanel != null) loginPanel.SetActive(false);
    }
}