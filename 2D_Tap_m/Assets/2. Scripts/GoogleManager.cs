using Firebase.Auth;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System.Collections;
using TMPro;
using UnityEngine;

public class GoogleManager : MonoBehaviour
{
    public static GoogleManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI logText;
    public TextMeshProUGUI userIdText;
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
        // 1. UI 초기화
        if (loginPanel != null) loginPanel.SetActive(true);
        if (guestLoginButton != null) guestLoginButton.SetActive(false);
        if (googleLoginButton != null) googleLoginButton.SetActive(false);

        // 2. GPGS 활성화
        PlayGamesPlatform.DebugLogEnabled = true;
        PlayGamesPlatform.Activate();

        // ★ [핵심] 이미 로그인된 세션인지 확인 (게임하다 타이틀로 돌아온 경우)
        if (DataManager.Instance != null && DataManager.Instance.IsLoggedIn)
        {
            Debug.Log("[System] 이미 로그인된 세션입니다. (로그인 화면 스킵)");

            // 닉네임 복구
            string lastType = PlayerPrefs.GetString("LastLoginType", "");
            if (userIdText != null)
            {
                if (lastType == "Guest")
                {
                    userIdText.text = "ID: Guest";
                }
                else if (PlayGamesPlatform.Instance.IsAuthenticated())
                {
                    userIdText.text = "ID: " + PlayGamesPlatform.Instance.GetUserDisplayName();
                }
            }

            // 패널 끄고 종료
            if (loginPanel != null) loginPanel.SetActive(false);
            return;
        }

        // --- 여기서부터는 앱을 처음 켰거나 로그아웃 상태일 때 ---

        string lastLoginType = PlayerPrefs.GetString("LastLoginType", "");
        Debug.Log($"[System] 저장된 로그인 타입: {(string.IsNullOrEmpty(lastLoginType) ? "없음" : lastLoginType)}");

        // 구글이었을 때만 자동 로그인
        if (lastLoginType == "Google")
        {
            Debug.Log("[System] 기존 구글 유저 감지 -> 자동 로그인 시도");
            if (PlayGamesPlatform.Instance.IsAuthenticated())
            {
                if (userIdText != null)
                    userIdText.text = "ID: " + PlayGamesPlatform.Instance.GetUserDisplayName();
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
            Debug.Log("[System] 자동 로그인 조건 아님 -> 로그인 버튼 활성화");
            SetLoginButtons(true);
            if (logText != null) logText.text = "Welcome! Please Login.";
        }
    }

    public void SignIn()
    {
        if (logText != null) logText.text = "Logging in...";
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
            if (logText != null) logText.text = "Login Failed. Try Again.";
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

        // DataManager에게 로그인 성공 알림 (여기서 IsLoggedIn = true가 됨)
        if (DataManager.Instance != null)
            DataManager.Instance.OnLoginSuccess(false);

        if (userIdText != null)
            userIdText.text = "ID: Guest";

        Debug.Log($"[System] 게스트 로그인 완료: {guestName}");
        if (loginPanel != null) loginPanel.SetActive(false);
    }

    IEnumerator TryFirebaseLogin()
    {
        if (logText != null) logText.text = "Linking Firebase...";
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
            if (logText != null) logText.text = "Auth Code Error";
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
            if (logText != null) logText.text = "Login Failed";
            SetLoginButtons(true);
        }
        else
        {
            // 성공
            FirebaseUser newUser = task.Result;
            Debug.Log($"[System] 파이어베이스 접속 완료. UID: {newUser.UserId}");

            if (userIdText != null)
                userIdText.text = "ID: " + PlayGamesPlatform.Instance.GetUserDisplayName();

            PlayerPrefs.SetString("LastLoginType", "Google");
            PlayerPrefs.Save();

            // DataManager에게 로그인 성공 알림 (여기서 IsLoggedIn = true가 됨)
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

        // ★ 로그아웃 시 세션 플래그 해제
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