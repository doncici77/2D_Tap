using System.Collections.Generic;
using UnityEngine;
using System;

public enum SFX
{
    Click,
    Success,
    Fail,
    Win,
    Lose
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Volume Settings (0 ~ 1)")]
    [Range(0f, 1f)] public float globalBgmVolume = 1f;
    [Range(0f, 1f)] public float globalSfxVolume = 1f;

    [Header("BGM Clips")]
    public AudioClip lobbyBGM;
    public AudioClip inGameBGM;

    [System.Serializable]
    public class SfxData
    {
        [HideInInspector] public string name;
        public SFX sfxType;
        public AudioClip clip;
    }

    [Header("SFX List (자동으로 관리됩니다)")]
    public List<SfxData> sfxList = new List<SfxData>();

    private Dictionary<SFX, AudioClip> sfxDictionary = new Dictionary<SFX, AudioClip>();
    private AudioSource bgmPlayer;
    private AudioSource sfxPlayer;

    // ★ [추가됨] 소리 On/Off 상태를 저장할 변수
    public bool IsBgmOn { get; private set; } = true;
    public bool IsSfxOn { get; private set; } = true;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        bgmPlayer = gameObject.AddComponent<AudioSource>();
        bgmPlayer.loop = true;

        sfxPlayer = gameObject.AddComponent<AudioSource>();
        sfxPlayer.loop = false;

        foreach (var data in sfxList)
        {
            if (!sfxDictionary.ContainsKey(data.sfxType))
                sfxDictionary.Add(data.sfxType, data.clip);
        }
    }

    private void Start()
    {
        // ★ [추가됨] 저장된 설정 불러오기 (없으면 1=켜짐)
        IsBgmOn = PlayerPrefs.GetInt("BgmOn", 1) == 1;
        IsSfxOn = PlayerPrefs.GetInt("SfxOn", 1) == 1;

        // ★ [수정됨] 시작할 때 On/Off 상태에 맞춰 볼륨 적용
        bgmPlayer.volume = IsBgmOn ? globalBgmVolume : 0f;
    }

    // ============================================
    // ★ [추가됨] 세팅 UI에서 호출할 토글 함수들
    // ============================================
    public void ToggleBGM()
    {
        IsBgmOn = !IsBgmOn; // 상태 뒤집기 (True <-> False)

        // 실제 소리 끄거나 켜기
        bgmPlayer.volume = IsBgmOn ? globalBgmVolume : 0f;

        // 저장 (1: 켜짐, 0: 꺼짐)
        PlayerPrefs.SetInt("BgmOn", IsBgmOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleSFX()
    {
        IsSfxOn = !IsSfxOn;

        // SFX는 재생할 때마다 확인하므로 여기서는 저장만 하면 됨
        PlayerPrefs.SetInt("SfxOn", IsSfxOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ============================================
    // 기존 재생 함수들 (SFX 부분 수정됨)
    // ============================================

    public void PlaySFX(SFX type)
    {
        // ★ [수정됨] SFX가 꺼져있으면 아예 실행 안 함
        if (!IsSfxOn) return;

        if (sfxDictionary.TryGetValue(type, out AudioClip clip))
        {
            if (clip != null) sfxPlayer.PlayOneShot(clip, globalSfxVolume);
        }
    }

    public void PlayDirectSFX(AudioClip clip)
    {
        // ★ [수정됨] SFX가 꺼져있으면 실행 안 함
        if (!IsSfxOn || clip == null) return;

        sfxPlayer.PlayOneShot(clip, globalSfxVolume);
    }

    // OnValidate는 그대로 둡니다 (너무 길어서 생략, 기존 코드 유지하세요)
    private void OnValidate()
    {
        var enumValues = Enum.GetValues(typeof(SFX));
        if (sfxList.Count != enumValues.Length)
        {
            List<SfxData> newList = new List<SfxData>();
            Dictionary<SFX, AudioClip> backup = new Dictionary<SFX, AudioClip>();
            foreach (var data in sfxList) { if (!backup.ContainsKey(data.sfxType)) backup.Add(data.sfxType, data.clip); }
            foreach (SFX sfx in enumValues)
            {
                SfxData newData = new SfxData();
                newData.sfxType = sfx;
                newData.name = sfx.ToString();
                if (backup.ContainsKey(sfx)) newData.clip = backup[sfx];
                newList.Add(newData);
            }
            sfxList = newList;
        }
        else
        {
            for (int i = 0; i < sfxList.Count; i++)
            {
                if (i < enumValues.Length)
                {
                    sfxList[i].name = enumValues.GetValue(i).ToString();
                    sfxList[i].sfxType = (SFX)enumValues.GetValue(i);
                }
            }
        }
    }

    // BGM 관련 함수들
    public void PlayLobbyBGM() => PlayBGM(lobbyBGM);
    public void PlayInGameBGM() => PlayBGM(inGameBGM);
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || (bgmPlayer.clip == clip && bgmPlayer.isPlaying)) return;
        bgmPlayer.clip = clip;
        bgmPlayer.Play();
        // ★ [수정됨] BGM 재생 시작할 때도 음소거 상태 체크
        bgmPlayer.volume = IsBgmOn ? globalBgmVolume : 0f;
    }
    public void StopBGM() => bgmPlayer.Stop();

    // 볼륨 조절 함수도 상태 체크하도록 수정
    public void SetBGMVolume(float volume)
    {
        globalBgmVolume = volume;
        if (IsBgmOn) bgmPlayer.volume = globalBgmVolume;
    }
    public void SetSFXVolume(float volume) { globalSfxVolume = volume; }
}