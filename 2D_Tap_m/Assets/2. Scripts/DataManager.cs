using Firebase;
using Firebase.Database;
using Firebase.Extensions; // 람다식 사용을 위해 필요
using GooglePlayGames;
using System;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    private DatabaseReference dbReference;
    private bool isFirebaseReady = false;

    // 현재 내 캐릭터 ID (메모리)
    public int currentCharacterID = 0;

    public event Action<int> OnCharacterDataLoaded;

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
                // (복사한 주소를 아래 큰따옴표 안에 넣으세요)
                dbReference = FirebaseDatabase.GetInstance("https://just-tap-7bb95-default-rtdb.firebaseio.com/").RootReference; // 예시 주소임! 본인 것 넣기
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
            Debug.Log("게스트 로그인: 캐릭터를 기본값(0)으로 설정합니다.");
            UpdateLocalCharacter(0); // ★ 로직 분리함
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
                    int dbCharIndex = int.Parse(snapshot.Value.ToString());
                    Debug.Log($"DB 로드 완료! 내 캐릭터: {dbCharIndex}");

                    // ★ DB 데이터로 업데이트 및 UI 알림
                    UpdateLocalCharacter(dbCharIndex);
                }
                else // 저장된 게 없다 (첫 접속)
                {
                    Debug.Log("신규 유저입니다. 캐릭터 0번으로 시작.");
                    SaveCharacterToFirebase(0);
                    UpdateLocalCharacter(0);
                }
            }
        });
    }

    // =========================================================
    // ★ 3. 캐릭터 변경 시 저장 (Write)
    // =========================================================
    public void SaveCharacter(int newCharIndex)
    {
        // 1. 로컬 업데이트 및 저장
        UpdateLocalCharacter(newCharIndex);

        // 2. 구글 유저만 DB에 저장
        if (PlayGamesPlatform.Instance.IsAuthenticated())
        {
            SaveCharacterToFirebase(newCharIndex);
        }
    }

    // ★ 중복 코드를 줄이고 이벤트를 호출하는 내부 함수
    private void UpdateLocalCharacter(int index)
    {
        currentCharacterID = index;
        PlayerPrefs.SetInt("MyCharacterID", index);
        PlayerPrefs.Save();

        // ★ [핵심] "데이터 바뀜! 듣고 있는 UI들은 화면 갱신해라!" 라고 방송
        OnCharacterDataLoaded?.Invoke(index);
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