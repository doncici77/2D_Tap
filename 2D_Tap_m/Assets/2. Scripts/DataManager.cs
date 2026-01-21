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
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // ★ 주의: 본인의 DB 주소가 맞는지 꼭 확인하세요!
                dbReference = FirebaseDatabase.GetInstance("https://just-tap-7bb95-default-rtdb.firebaseio.com/").RootReference;
                isFirebaseReady = true;
                Debug.Log("[System] 파이어베이스 연결 성공!"); // ★ 태그 추가
            }
            else
            {
                Debug.LogError($"[System] 파이어베이스 연결 실패: {dependencyStatus}"); // ★ 태그 추가
            }
        });
    }

    public void OnLoginSuccess(bool isGoogleUser)
    {
        if (isGoogleUser)
        {
            LoadDataFromFirebase();
        }
        else
        {
            Debug.Log("[System] 게스트 로그인: 캐릭터를 기본값(0)으로 설정합니다."); // ★ 태그 추가
            UpdateLocalCharacter(0);
        }
    }

    public void LoadDataFromFirebase()
    {
        if (!isFirebaseReady || !PlayGamesPlatform.Instance.IsAuthenticated()) return;

        string userId = PlayGamesPlatform.Instance.GetUserId();

        // ★ [디버깅용 로그] 함수가 호출되었는지 확인
        Debug.Log($"[System] 데이터 로드 시도... UserID: {userId}");

        dbReference.Child("users").Child(userId).Child("characterId").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    int dbCharIndex = int.Parse(snapshot.Value.ToString());
                    Debug.Log($"[System] DB 로드 완료! 내 캐릭터: {dbCharIndex}"); // ★ 태그 추가
                    UpdateLocalCharacter(dbCharIndex);
                }
                else
                {
                    Debug.Log("[System] 신규 유저입니다. 캐릭터 0번으로 시작."); // ★ 태그 추가
                    SaveCharacterToFirebase(0);
                    UpdateLocalCharacter(0);
                }
            }
            else
            {
                Debug.LogError($"[System] DB 읽기 실패: {task.Exception}"); // ★ 에러 로그 추가
            }
        });
    }

    public void SaveCharacter(int newCharIndex)
    {
        UpdateLocalCharacter(newCharIndex);
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
        Debug.Log($"[System] DB 저장 완료: {charIndex}"); // ★ 태그 추가
    }
}