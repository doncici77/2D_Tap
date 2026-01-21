using Firebase; // FirebaseException 처리를 위해 추가
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
    public TextMeshProUGUI userIdText; // ★ [추가] 유저 아이디 띄울 텍스트  
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
        // 1. 일단 패널은 켜두고, 버튼은 상황 봐서 켭니다.
        if (loginPanel != null) loginPanel.SetActive(true);
        if (guestLoginButton != null) guestLoginButton.SetActive(false);
        if (googleLoginButton != null) googleLoginButton.SetActive(false);

        // 2. GPGS 초기화
        PlayGamesPlatform.DebugLogEnabled = true;
        PlayGamesPlatform.Activate();

        // 3. 마지막 로그인 기록 확인
        string lastLoginType = PlayerPrefs.GetString("LastLoginType", "");
        Debug.Log($"[System] 저장된 로그인 타입: {(string.IsNullOrEmpty(lastLoginType) ? "없음" : lastLoginType)}");

        // ★ [핵심 변경] "구글"일 때만 자동 로그인 시도.
        // 그 외(게스트, 처음 접속 등)에는 버튼 띄우고 대기.
        if (lastLoginType == "Google")
        {
            Debug.Log("[System] 기존 구글 유저 감지 -> 자동 로그인 시도");

            // GPGS가 이미 인증된 상태인지 체크 (보통 앱 재시작시 false일 확률 높음 -> SignIn 호출)
            if (PlayGamesPlatform.Instance.IsAuthenticated())
            {
                if (userIdText != null)
                    userIdText.text = "ID: " + PlayGamesPlatform.Instance.GetUserDisplayName();
                StartCoroutine(TryFirebaseLogin());
            }
            else
            {
                // 구글 기록이 있으면 바로 구글 로그인 창을 띄웁니다 (Silent Login)
                SignIn();
            }
        }
        else
        {
            // 구글 기록이 없거나, 마지막이 게스트였으면 -> "로그인 화면에서 대기"
            Debug.Log("[System] 자동 로그인 조건 아님 -> 로그인 버튼 활성화");

            SetLoginButtons(true);
            if (logText != null) logText.text = "Welcome! Please Login.";
        }
    }

    public void SignIn()
    {
        if (logText != null) logText.text = "Logging in...";
        SetLoginButtons(false);

        Debug.Log("[System] GPGS 인증(Authenticate) 요청...");
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

        if (DataManager.Instance != null)
            DataManager.Instance.OnLoginSuccess(false);

        // ★ [추가] UI에 "Guest" 표시
        if (userIdText != null)
            userIdText.text = "ID: Guest";

        Debug.Log($"[System] 게스트 로그인 완료: {guestName}");
        if (loginPanel != null) loginPanel.SetActive(false);
    }

    IEnumerator TryFirebaseLogin()
    {
        if (logText != null) logText.text = "Linking Firebase...";
        Debug.Log("[System] 1단계: 구글 서버 인증 코드 요청 중...");

        string authCode = "";
        bool isFinished = false;

        PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
        {
            authCode = code;
            isFinished = true;
        });

        yield return new WaitUntil(() => isFinished);

        // ★ [디버그] 인증 코드 획득 여부 확인
        if (string.IsNullOrEmpty(authCode))
        {
            Debug.LogError("[System] 1단계 실패: 인증 코드가 비어있습니다. (Web Client ID 불일치 가능성)");
            if (logText != null) logText.text = "Auth Code Error";
            SetLoginButtons(true);
            yield break;
        }

        Debug.Log($"[System] 1단계 성공: 인증 코드 획득 완료! (앞 5자리: {authCode.Substring(0, Mathf.Min(authCode.Length, 5))}...)");
        Debug.Log("[System] 2단계: 파이어베이스 자격증명(Credential) 생성 중...");

        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        Credential credential = PlayGamesAuthProvider.GetCredential(authCode);

        Debug.Log("[System] 3단계: 파이어베이스 로그인(SignIn) 시도...");
        var task = auth.SignInWithCredentialAsync(credential);

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsCanceled)
        {
            Debug.LogError("[System] 3단계 실패: 로그인이 취소되었습니다.");
            if (logText != null) logText.text = "Login Canceled";
            SetLoginButtons(true);
        }
        else if (task.IsFaulted)
        {
            // ★ [핵심] 에러 내용을 뜯어서 깔끔하게 보여줍니다.
            // AggregateException 안쪽에 있는 진짜 범인(FirebaseException)을 꺼냅니다.
            FirebaseException firebaseEx = task.Exception.GetBaseException() as FirebaseException;

            if (firebaseEx != null)
            {
                // 파이어베이스가 주는 정확한 에러 코드 (예: InvalidCredential, UserDisabled 등)
                Debug.LogError($"[System] 3단계 실패 (Firebase Error): {firebaseEx.Message} [Code: {firebaseEx.ErrorCode}]");
                if (logText != null) logText.text = $"Error: {firebaseEx.ErrorCode}";
            }
            else
            {
                // 그 외 일반 에러 (네트워크 등)
                Debug.LogError($"[System] 3단계 실패 (Unknown Error): {task.Exception.Message}");
                if (logText != null) logText.text = "Login Error";
            }

            SetLoginButtons(true);
        }
        else
        {
            // 성공
            FirebaseUser newUser = task.Result;
            Debug.Log($"[System] 모든 단계 성공! 파이어베이스 접속 완료. (UID: {newUser.UserId})");

            // ★ [추가] UI에 "구글 닉네임" 표시
            if (userIdText != null)
            {
                // 방법 A: 구글 닉네임 (예: 박광호) 보여주기 - 추천!
                userIdText.text = "ID: " + PlayGamesPlatform.Instance.GetUserDisplayName();

                // 방법 B: 만약 이메일이나 고유 ID를 보여주고 싶다면 아래 주석 해제
                // userIdText.text = newUser.UserId; 
            }

            PlayerPrefs.SetString("LastLoginType", "Google");
            PlayerPrefs.Save();

            if (DataManager.Instance != null)
                DataManager.Instance.OnLoginSuccess(true);

            if (loginPanel != null) loginPanel.SetActive(false);
        }
    }

    // 버튼 끄고 켜는 코드 중복 제거용 함수
    private void SetLoginButtons(bool isActive)
    {
        if (guestLoginButton != null) guestLoginButton.SetActive(isActive);
        if (googleLoginButton != null) googleLoginButton.SetActive(isActive);
    }

    public void OnClickLogOut()
    {
        Debug.Log("[System] 로그아웃 진행...");

        FirebaseAuth.DefaultInstance.SignOut();
        PlayerPrefs.DeleteKey("LastLoginType");
        PlayerPrefs.Save();

        if (DataManager.Instance != null)
            DataManager.Instance.currentCharacterID = 0;

        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}