using UnityEngine;
using GooglePlayGames;
using Firebase;
using Firebase.Database;
using Firebase.Extensions; // 람다식 사용을 위해 필요

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    private DatabaseReference dbReference;
    private bool isFirebaseReady = false;

    // 현재 내 캐릭터 ID (메모리)
    public int currentCharacterID = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    void Start()
    {
        // 1. 파이어베이스 초기화 (비동기)
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // 연결 성공! DB 참조 가져오기
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                isFirebaseReady = true;
                Debug.Log("파이어베이스 연결 성공!");
            }
            else
            {
                Debug.LogError($"파이어베이스 연결 실패: {dependencyStatus}");
            }
        });
    }

    // =========================================================
    // ★ 1. 로그인 성공 시 호출하는 함수 (분기점)
    // =========================================================
    public void OnLoginSuccess(bool isGoogleUser)
    {
        if (isGoogleUser)
        {
            // 구글 유저 -> DB에서 데이터 불러오기
            LoadDataFromFirebase();
        }
        else
        {
            // 게스트 유저 -> 캐릭터 0번으로 초기화 (저장 안 함)
            Debug.Log("게스트 로그인: 캐릭터를 기본값(0)으로 설정합니다.");
            currentCharacterID = 0;

            // 로컬(PlayerPrefs)에도 0번으로 덮어씌움
            PlayerPrefs.SetInt("MyCharacterID", 0);
            PlayerPrefs.Save();
        }
    }

    // =========================================================
    // ★ 2. 구글 유저 데이터 불러오기 (Read)
    // =========================================================
    public void LoadDataFromFirebase()
    {
        if (!isFirebaseReady || !PlayGamesPlatform.Instance.IsAuthenticated()) return;

        string userId = PlayGamesPlatform.Instance.GetUserId(); // 구글 ID

        // users -> (내ID) -> characterId 값 읽어오기
        dbReference.Child("users").Child(userId).Child("characterId").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists) // 저장된 데이터가 있다!
                {
                    // DB에서 가져온 값으로 설정
                    int dbCharIndex = int.Parse(snapshot.Value.ToString());
                    currentCharacterID = dbCharIndex;

                    // 로컬에도 동기화
                    PlayerPrefs.SetInt("MyCharacterID", dbCharIndex);
                    PlayerPrefs.Save();

                    Debug.Log($"DB 로드 완료! 내 캐릭터: {dbCharIndex}");
                }
                else // 저장된 게 없다 (첫 접속)
                {
                    Debug.Log("신규 유저입니다. 캐릭터 0번으로 시작.");
                    SaveCharacterToFirebase(0); // 0번으로 DB 생성
                }
            }
        });
    }

    // =========================================================
    // ★ 3. 캐릭터 변경 시 저장 (Write)
    // =========================================================
    public void SaveCharacter(int newCharIndex)
    {
        // 1. 일단 메모리랑 로컬에 반영
        currentCharacterID = newCharIndex;
        PlayerPrefs.SetInt("MyCharacterID", newCharIndex);
        PlayerPrefs.Save();

        // 2. 구글 유저만 DB에 저장
        if (PlayGamesPlatform.Instance.IsAuthenticated())
        {
            SaveCharacterToFirebase(newCharIndex);
        }
    }

    private void SaveCharacterToFirebase(int charIndex)
    {
        if (!isFirebaseReady) return;

        string userId = PlayGamesPlatform.Instance.GetUserId();

        // users -> (내ID) -> characterId 에 값 저장
        dbReference.Child("users").Child(userId).Child("characterId").SetValueAsync(charIndex);
        Debug.Log($"DB 저장 완료: {charIndex}");
    }
}