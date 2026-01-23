using Firebase.Auth;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System.Collections;
using TMPro;
using UnityEngine;
// ★ 로컬라이제이션 네임스페이스 추가
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class GoogleManager : MonoBehaviour
{
    public static GoogleManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI logText;
    public TextMeshProUGUI userIdText;
    public GameObject loginPanel;
    public GameObject guestLoginButton;
    public GameObject googleLoginButton;

    // ★ 테이블 이름 정의
    private const string TableName = "Table";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 1. UI 초기화
        if (loginPanel != null) loginPanel.SetActive(true);
        if (guestLoginButton != null) guestLoginButton.SetActive(false);
        if (googleLoginButton != null) googleLoginButton.SetActive(false);

        // 2. GPGS 활성화
        PlayGamesPlatform.DebugLogEnabled = true;
        PlayGamesPlatform.Activate();

        // ★ [핵심] 이미 로그인된 세션인지 확인
        if (DataManager.Instance != null && DataManager.Instance.IsLoggedIn)
        {
            Debug.Log("[System] 이미 로그인된 세션입니다. (로그인 화면 스킵)");

            string lastType = PlayerPrefs.GetString("LastLoginType", "");
            if (userIdText != null)
            {
                if (lastType == "Guest")
                {
                    // "ID: Guest" -> 로컬라이징 적용
                    userIdText.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, "auth_id_guest");
                }
                else if (PlayGamesPlatform.Instance.IsAuthenticated())
                {
                    // "ID: {0}" -> 로컬라이징 적용 (이름 대입)
                    string userName = PlayGamesPlatform.Instance.GetUserDisplayName();
                    userIdText.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, "auth_id_user", new object[] { userName });
                }
            }

            if (loginPanel != null) loginPanel.SetActive(false);
            return;
        }

        string lastLoginType = PlayerPrefs.GetString("LastLoginType", "");

        // 구글이었을 때만 자동 로그인
        if (lastLoginType == "Google")
        {
            if (PlayGamesPlatform.Instance.IsAuthenticated())
            {
                if (userIdText != null)
                {
                    string userName = PlayGamesPlatform.Instance.GetUserDisplayName();
                    userIdText.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, "auth_id_user", new object[] { userName });
                }
                StartCoroutine(TryFirebaseLogin());
            }
            else
            {
                SignIn();
            }
        }
        else
        {
            // 게스트였거나 기록 없으면 대기
            SetLoginButtons(true);
            if (logText != null)
            {
                // "Welcome! Please Login." -> 로컬라이징 적용
                logText.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, "auth_welcome");
            }
        }
    }

    public void SignIn()
    {
        if (logText != null)
        {
            // "Logging in..." -> 로컬라이징 적용
            logText.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, "auth_logging_in");
        }
        SetLoginButtons(false);
        PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
    }

    internal void ProcessAuthentication(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
            string name = PlayGamesPlatform.Instance.GetUserDisplayName();
            Debug.Log($"[System] GPGS 로그인 성공! (유저명: {name})");
            StartCoroutine(TryFirebaseLogin());
        }
        else
        {
            Debug.LogError($"[System] GPGS 로그인 실패 (Status: {status})");
            if (logText != null)
            {
                // "Login Failed. Try Again." -> 로컬라이징 적용
                logText.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, "auth_login_failed");
            }
            SetLoginButtons(true);
        }
    }

    public void LoginAsGuest()
    {
        string guestName = PlayerPrefs.GetString("GuestID", "");
        if (string.IsNullOrEmpty(guestName))
        {
            guestName = "Guest_" + Random.Range(1000, 9999);
            PlayerPrefs.SetString("GuestID", guestName);
        }

        PlayerPrefs.SetString("LastLoginType", "Guest");
        PlayerPrefs.Save();

        if (DataManager.Instance != null)
            DataManager.Instance.OnLoginSuccess(false);

        if (userIdText != null)
        {
            // "ID: Guest" -> 로컬라이징 적용
            userIdText.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, "auth_id_guest");
        }

        Debug.Log($"[System] 게스트 로그인 완료: {guestName}");
        if (loginPanel != null) loginPanel.SetActive(false);
    }

    IEnumerator TryFirebaseLogin()
    {
        if (logText != null)
        {
            // "Linking Firebase..." -> 로컬라이징 적용
            logText.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, "auth_linking");
        }

        string authCode = "";
        bool isFinished = false;

        PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
        {
            authCode = code;
            isFinished = true;
        });

        yield return new WaitUntil(() => isFinished);

        if (string.IsNullOrEmpty(authCode))
        {
            Debug.LogError("[System] 인증 코드 실패");
            if (logText != null)
            {
                // "Login Error" -> 로컬라이징 적용
                logText.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, "auth_error");
            }
            SetLoginButtons(true);
            yield break;
        }

        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        Credential credential = PlayGamesAuthProvider.GetCredential(authCode);
        var task = auth.SignInWithCredentialAsync(credential);

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsCanceled || task.IsFaulted)
        {
            Debug.LogError("[System] 파이어베이스 로그인 실패");
            if (logText != null)
            {
                // "Login Failed" -> 로컬라이징 적용
                logText.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, "auth_login_failed");
            }
            SetLoginButtons(true);
        }
        else
        {
            // 성공
            FirebaseUser newUser = task.Result;
            Debug.Log($"[System] 파이어베이스 접속 완료. UID: {newUser.UserId}");

            if (userIdText != null)
            {
                // "ID: {0}" -> 로컬라이징 적용
                string userName = PlayGamesPlatform.Instance.GetUserDisplayName();
                userIdText.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, "auth_id_user", new object[] { userName });
            }

            PlayerPrefs.SetString("LastLoginType", "Google");
            PlayerPrefs.Save();

            if (DataManager.Instance != null)
                DataManager.Instance.OnLoginSuccess(true);

            if (loginPanel != null) loginPanel.SetActive(false);
        }
    }

    private void SetLoginButtons(bool isActive)
    {
        if (guestLoginButton != null) guestLoginButton.SetActive(isActive);
        if (googleLoginButton != null) googleLoginButton.SetActive(isActive);
    }

    public void OnClickLogOut()
    {
        Debug.Log("[System] 로그아웃 진행...");

        if (DataManager.Instance != null)
            DataManager.Instance.IsLoggedIn = false;

        FirebaseAuth.DefaultInstance.SignOut();
        PlayerPrefs.DeleteKey("LastLoginType");
        PlayerPrefs.Save();

        if (DataManager.Instance != null)
            DataManager.Instance.currentCharacterID = 0;

        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}