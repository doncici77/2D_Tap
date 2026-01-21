using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using GooglePlayGames;
using System;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;
    private DatabaseReference dbReference;
    private bool isFirebaseReady = false;

    public int currentCharacterID = 0;
    public event Action<int> OnCharacterDataLoaded;

    // ★ [추가] 로그인 상태 유지용 플래그 (앱 껐다 켜면 false로 자동 초기화됨)
    public bool IsLoggedIn { get; set; } = false;

    void Awake()
    {
        // 싱글톤 패턴 (이 오브젝트는 씬이 바뀌어도 파괴되지 않음)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // ★ 주의: 본인의 DB 주소가 맞는지 꼭 확인하세요!
                dbReference = FirebaseDatabase.GetInstance("https://just-tap-7bb95-default-rtdb.firebaseio.com/").RootReference;
                isFirebaseReady = true;
                Debug.Log("[System] 파이어베이스 연결 성공!");
            }
            else
            {
                Debug.LogError($"[System] 파이어베이스 연결 실패: {dependencyStatus}");
            }
        });
    }

    // ★ 로그인 성공 시 호출되는 함수 (GoogleManager에서 호출)
    public void OnLoginSuccess(bool isGoogleUser)
    {
        // 1. 로그인 성공 도장 찍기!
        IsLoggedIn = true;

        // 2. 유저 타입에 따라 데이터 로드 분기
        if (isGoogleUser)
        {
            LoadDataFromFirebase();
        }
        else
        {
            Debug.Log("[System] 게스트 로그인: 캐릭터를 기본값(0)으로 설정합니다.");
            UpdateLocalCharacter(0);
        }
    }

    public void LoadDataFromFirebase()
    {
        if (!isFirebaseReady || !PlayGamesPlatform.Instance.IsAuthenticated()) return;

        string userId = PlayGamesPlatform.Instance.GetUserId();
        Debug.Log($"[System] 데이터 로드 시도... UserID: {userId}");

        dbReference.Child("users").Child(userId).Child("characterId").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    int dbCharIndex = int.Parse(snapshot.Value.ToString());
                    Debug.Log($"[System] DB 로드 완료! 내 캐릭터: {dbCharIndex}");
                    UpdateLocalCharacter(dbCharIndex);
                }
                else
                {
                    Debug.Log("[System] 신규 유저입니다. 캐릭터 0번으로 시작.");
                    SaveCharacterToFirebase(0);
                    UpdateLocalCharacter(0);
                }
            }
            else
            {
                Debug.LogError($"[System] DB 읽기 실패: {task.Exception}");
            }
        });
    }

    public void SaveCharacter(int newCharIndex)
    {
        UpdateLocalCharacter(newCharIndex);

        // 구글 로그인 상태일 때만 클라우드에 저장
        if (PlayGamesPlatform.Instance.IsAuthenticated())
        {
            SaveCharacterToFirebase(newCharIndex);
        }
    }

    private void UpdateLocalCharacter(int index)
    {
        currentCharacterID = index;
        PlayerPrefs.SetInt("MyCharacterID", index);
        PlayerPrefs.Save();
        OnCharacterDataLoaded?.Invoke(index);
    }

    private void SaveCharacterToFirebase(int charIndex)
    {
        if (!isFirebaseReady) return;
        string userId = PlayGamesPlatform.Instance.GetUserId();
        dbReference.Child("users").Child(userId).Child("characterId").SetValueAsync(charIndex);
        Debug.Log($"[System] DB 저장 완료: {charIndex}");
    }
}