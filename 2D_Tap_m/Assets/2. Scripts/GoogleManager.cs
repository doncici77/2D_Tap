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

        // ★ [변경] v11 버전은 설정 코드가 필요 없습니다.
        // 그냥 활성화(Activate)만 하면, 아까 Android Setup에서 입력한 ID를 알아서 씁니다.
        PlayGamesPlatform.DebugLogEnabled = true;
        PlayGamesPlatform.Activate();

#if UNITY_EDITOR
        Invoke("LoginAsGuest", 0.5f);
#else
        // 1. 이미 구글에 연결되어 있다면? -> 바로 파이어베이스 연동 시도
        if (PlayGamesPlatform.Instance.IsAuthenticated())
        {
            Debug.Log("이미 구글 로그인 상태입니다.");
            StartCoroutine(TryFirebaseLogin()); 
            return;
        }

        // 2. 연결 안 됨 -> 마지막 로그인 방식 확인
        string lastLoginType = PlayerPrefs.GetString("LastLoginType", "");

        if (lastLoginType == "Guest")
        {
            LoginAsGuest();
        }
        else
        {
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

            // 구글 로그인 성공 후 -> 파이어베이스 로그인 시도
            StartCoroutine(TryFirebaseLogin());
        }
        else
        {
            if (logText != null) logText.text = "Login Failed. Choose Mode.";
            if (guestLoginButton != null) guestLoginButton.SetActive(true);
            if (googleLoginButton != null) googleLoginButton.SetActive(true);
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

        // 게스트는 파이어베이스 로그인 없이 바로 데이터 처리 (false)
        if (DataManager.Instance != null)
            DataManager.Instance.OnLoginSuccess(false);

        Debug.Log($"Guest Login: {guestName}");

        if (loginPanel != null) loginPanel.SetActive(false);
    }

    IEnumerator TryFirebaseLogin()
    {
        if (logText != null) logText.text = "Linking Firebase...";

        string authCode = "";

        // ★ [핵심] v11에서는 이 함수가 서버 인증 코드를 받아옵니다.
        // forceRefreshToken: true로 해야 코드를 확실하게 받아옵니다.
        bool isFinished = false;
        PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
        {
            authCode = code;
            isFinished = true;
        });

        yield return new WaitUntil(() => isFinished);

        if (string.IsNullOrEmpty(authCode))
        {
            Debug.LogError("인증 코드를 못 가져왔습니다. (Android Setup에 Web Client ID 넣었는지 확인!)");
            if (logText != null) logText.text = "Auth Code Error";

            if (guestLoginButton != null) guestLoginButton.SetActive(true);
            if (googleLoginButton != null) googleLoginButton.SetActive(true);
            yield break;
        }

        // 파이어베이스 자격증 생성 및 로그인
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        Credential credential = PlayGamesAuthProvider.GetCredential(authCode);

        var task = auth.SignInWithCredentialAsync(credential);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsCanceled || task.IsFaulted)
        {
            Debug.LogError("파이어베이스 로그인 실패: " + task.Exception);
            if (logText != null) logText.text = "Firebase Login Failed";

            if (guestLoginButton != null) guestLoginButton.SetActive(true);
            if (googleLoginButton != null) googleLoginButton.SetActive(true);
        }
        else
        {
            // 성공!
            FirebaseUser newUser = task.Result;
            Debug.Log($"파이어베이스 로그인 완료! User ID: {newUser.UserId}");

            PlayerPrefs.SetString("LastLoginType", "Google");
            PlayerPrefs.Save();

            // 구글 유저 모드(true)로 데이터 로드
            if (DataManager.Instance != null)
                DataManager.Instance.OnLoginSuccess(true);

            if (loginPanel != null) loginPanel.SetActive(false);
        }
    }

    public void OnClickLogOut()
    {
        Debug.Log("로그아웃 시도...");

        // 1. 파이어베이스 로그아웃 (이게 진짜 게임 로그아웃)
        FirebaseAuth.DefaultInstance.SignOut();

        // 2. 자동 로그인 기록 삭제
        PlayerPrefs.DeleteKey("LastLoginType");
        PlayerPrefs.Save();

        // 3. 데이터 초기화
        if (DataManager.Instance != null)
            DataManager.Instance.currentCharacterID = 0;

        // 4. 게임 재시작 (로그인 화면으로 돌아가기)
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}