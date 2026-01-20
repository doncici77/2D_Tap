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
        // 1. 시작하면 무조건 로그인 패널 켜기 (재로그인 느낌)
        if (loginPanel != null) loginPanel.SetActive(true);

        // 2. 버튼은 일단 숨김 (자동 접속 시도 중)
        if (guestLoginButton != null) guestLoginButton.SetActive(false);
        if (googleLoginButton != null) googleLoginButton.SetActive(false);

#if UNITY_EDITOR
        Debug.Log("에디터 환경: 게스트 모드 자동 진입");
        Invoke("LoginAsGuest", 1.0f); // 1초 뒤 입장 (로그인하는 척)
#else
        // 안드로이드 설정
        PlayGamesPlatform.DebugLogEnabled = true;
        PlayGamesPlatform.Activate();

        // ★ [변경점] "이미 로그인 됐나?" 확인 안 함! 무조건 로그인 시도!
        SignIn();
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

            // ★ 로그인 성공 시에만 패널 닫기
            if (loginPanel != null) loginPanel.SetActive(false);
        }
        else
        {
            if (logText != null) logText.text = "Login failed. Select login method.";

            // 실패 시 버튼 띄우기
            if (guestLoginButton != null) guestLoginButton.SetActive(true);
            if (googleLoginButton != null) googleLoginButton.SetActive(true);
        }
    }

    public void LoginAsGuest()
    {
        string guestName = "Guest_" + Random.Range(1000, 9999);
        PlayerPrefs.SetString("PlayerName", guestName);

        if (loginPanel != null) loginPanel.SetActive(false);
    }

    // ★ [추가됨] 앱이 꺼질 때 강제로 로그아웃 (정보 삭제)
    private void OnApplicationQuit()
    {
#if !UNITY_EDITOR
        // 구글 로그아웃 처리
        PlayGamesPlatform.Instance.SignOut();
        Debug.Log("앱 종료: 로그아웃 완료");
#endif
    }
}