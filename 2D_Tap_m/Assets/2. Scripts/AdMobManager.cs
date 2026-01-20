using UnityEngine;
using GoogleMobileAds.Api;
using System;

public class AdMobManager : MonoBehaviour
{
    public static AdMobManager Instance;

    // 테스트용 ID (출시 전엔 무조건 이걸로 테스트해야 정지 안 당함)
    // 안드로이드 전면 광고 테스트 ID: ca-app-pub-3940256099942544/1033173712
    private string adUnitId = "ca-app-pub-3940256099942544/1033173712";

    private InterstitialAd interstitialAd;

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
        // SDK 초기화
        MobileAds.Initialize(initStatus => {
            LoadInterstitialAd(); // 초기화 끝나면 광고 미리 로드해두기
        });
    }

    // 광고 로드하기 (미리 불러놔야 렉이 없음)
    public void LoadInterstitialAd()
    {
        // 기존 광고 있으면 정리
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        var adRequest = new AdRequest();

        InterstitialAd.Load(adUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("광고 로드 실패: " + error);
                return;
            }

            interstitialAd = ad;
            // 광고 닫았을 때 다음 광고 미리 로드하는 이벤트 연결
            interstitialAd.OnAdFullScreenContentClosed += LoadInterstitialAd;
        });
    }

    // 광고 보여주기 (성공 시 실행할 행동을 Action으로 받음)
    public void ShowAd(Action onAdClosed)
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            // 광고 닫히면 실행할 함수 임시 연결
            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                onAdClosed?.Invoke(); // "광고 다 봤으니 이제 캐릭터 선택해줘" 실행
            };

            interstitialAd.Show();
        }
        else
        {
            Debug.Log("준비된 광고가 없어서 그냥 통과시켜줍니다.");
            onAdClosed?.Invoke(); // 광고 없으면 그냥 바로 실행
            LoadInterstitialAd(); // 다시 로드 시도
        }
    }
}