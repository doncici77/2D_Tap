using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Volume Settings (0 ~ 1)")]
    [Range(0f, 1f)] public float globalBgmVolume = 1f;
    [Range(0f, 1f)] public float globalSfxVolume = 1f;

    [Header("Registered Clips (Inspector에서 할당)")]
    public AudioClip lobbyBGM;  // 로비 배경음
    public AudioClip inGameBGM; // 인게임 배경음
    public AudioClip buttonClickSFX; // 버튼 클릭음
    public AudioClip successSFX; // 성공 효과음
    public AudioClip failSFX;    // 실패 효과음

    // 내부에서 사용할 오디오 소스들
    private AudioSource bgmPlayer;
    private AudioSource sfxPlayer;

    private void Awake()
    {
        // 1. 싱글톤 설정 (씬 이동해도 파괴 안 됨)
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

        // 2. 오디오 소스 컴포넌트 자동 생성
        // BGM용: 반복 재생 필요
        bgmPlayer = gameObject.AddComponent<AudioSource>();
        bgmPlayer.loop = true;
        bgmPlayer.playOnAwake = false;

        // SFX용: 중첩 재생 필요 (PlayOneShot 사용)
        sfxPlayer = gameObject.AddComponent<AudioSource>();
        sfxPlayer.loop = false;
        sfxPlayer.playOnAwake = false;
    }

    private void Start()
    {
        // 시작하자마자 볼륨 적용
        SetBGMVolume(globalBgmVolume);
        SetSFXVolume(globalSfxVolume);
    }

    // ============================================
    // 🎵 BGM 관련 함수
    // ============================================

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;

        // 이미 같은 음악이 나오고 있다면 다시 재생하지 않음 (씬 전환 시 끊김 방지)
        if (bgmPlayer.clip == clip && bgmPlayer.isPlaying) return;

        bgmPlayer.clip = clip;
        bgmPlayer.Play();
    }

    // 이름으로 편하게 재생하기 위한 오버로딩 (필요하면 추가)
    public void PlayLobbyBGM() => PlayBGM(lobbyBGM);
    public void PlayInGameBGM() => PlayBGM(inGameBGM);

    public void StopBGM()
    {
        bgmPlayer.Stop();
    }

    public void SetBGMVolume(float volume)
    {
        globalBgmVolume = Mathf.Clamp01(volume);
        bgmPlayer.volume = globalBgmVolume;
    }

    // ============================================
    // 🔊 SFX (효과음) 관련 함수
    // ============================================

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        // PlayOneShot: 소리가 겹쳐도 끊기지 않고 위에 덮어씌워 재생됨 (효과음 전용)
        sfxPlayer.PlayOneShot(clip, globalSfxVolume);
    }

    // 자주 쓰는 효과음 단축 함수
    public void PlayClickSFX() => PlaySFX(buttonClickSFX);

    public void SetSFXVolume(float volume)
    {
        globalSfxVolume = Mathf.Clamp01(volume);
        // SFX는 PlayOneShot을 쓸 때 볼륨을 넘겨주므로, 변수값만 바꿔두면 됨
    }
}