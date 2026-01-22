using System.Collections.Generic;
using UnityEngine;
using System; // Enum 처리를 위해 필요

public enum SFX
{
    Click,      // 클릭
    Success,    // 성공
    Fail,       // 실패
    Attack,     // 공격
    Win,       // 승리
    Lose      // 패배
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
    public class SfxData // struct 대신 class로 변경 (인스펙터 수정 용이성)
    {
        [HideInInspector] public string name; // 인스펙터에서 보기 편하게 이름 표시용
        public SFX sfxType;
        public AudioClip clip;
    }

    [Header("SFX List (자동으로 관리됩니다)")]
    public List<SfxData> sfxList = new List<SfxData>();

    // 내부 검색용 딕셔너리
    private Dictionary<SFX, AudioClip> sfxDictionary = new Dictionary<SFX, AudioClip>();
    private AudioSource bgmPlayer;
    private AudioSource sfxPlayer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        bgmPlayer = gameObject.AddComponent<AudioSource>();
        bgmPlayer.loop = true;

        sfxPlayer = gameObject.AddComponent<AudioSource>();
        sfxPlayer.loop = false;

        // 리스트 데이터를 딕셔너리로 변환
        foreach (var data in sfxList)
        {
            if (!sfxDictionary.ContainsKey(data.sfxType))
            {
                sfxDictionary.Add(data.sfxType, data.clip);
            }
        }
    }

    private void Start()
    {
        SetBGMVolume(globalBgmVolume);
        SetSFXVolume(globalSfxVolume);
    }

    // ============================================
    // ★ [핵심] 에디터 자동 동기화 코드
    // ============================================
    private void OnValidate()
    {
        // 1. 현재 Enum 목록을 다 가져옵니다.
        var enumValues = Enum.GetValues(typeof(SFX));

        // 2. 리스트가 비어있거나 개수가 다르면 (Enum이 추가/삭제되면) 다시 맞춥니다.
        if (sfxList.Count != enumValues.Length)
        {
            List<SfxData> newList = new List<SfxData>();

            // 기존에 설정해둔 클립들은 백업해둡니다. (삭제 방지)
            Dictionary<SFX, AudioClip> backup = new Dictionary<SFX, AudioClip>();
            foreach (var data in sfxList)
            {
                if (!backup.ContainsKey(data.sfxType))
                    backup.Add(data.sfxType, data.clip);
            }

            // Enum 순서대로 리스트를 다시 만듭니다.
            foreach (SFX sfx in enumValues)
            {
                SfxData newData = new SfxData();
                newData.sfxType = sfx;
                newData.name = sfx.ToString(); // 인스펙터 이름표 달기

                // 백업된 클립이 있으면 복구, 없으면 빈칸
                if (backup.ContainsKey(sfx))
                    newData.clip = backup[sfx];

                newList.Add(newData);
            }

            sfxList = newList;
        }
        else
        {
            // 개수는 맞는데 이름표(Label)만 갱신이 필요할 때
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

    // ... (이하 PlaySFX, PlayBGM 등 함수들은 그대로 두세요) ...
    public void PlaySFX(SFX type)
    {
        if (sfxDictionary.TryGetValue(type, out AudioClip clip))
        {
            if (clip != null) sfxPlayer.PlayOneShot(clip, globalSfxVolume);
        }
    }

    // BGM 관련 함수 생략 (위의 코드와 동일) ...
    public void PlayLobbyBGM() => PlayBGM(lobbyBGM);
    public void PlayInGameBGM() => PlayBGM(inGameBGM);
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || (bgmPlayer.clip == clip && bgmPlayer.isPlaying)) return;
        bgmPlayer.clip = clip;
        bgmPlayer.Play();
    }
    public void StopBGM() => bgmPlayer.Stop();
    public void SetBGMVolume(float volume) { globalBgmVolume = volume; bgmPlayer.volume = globalBgmVolume; }
    public void SetSFXVolume(float volume) { globalSfxVolume = volume; }
}