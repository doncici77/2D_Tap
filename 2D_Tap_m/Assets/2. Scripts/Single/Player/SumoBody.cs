using UnityEngine;

public class SumoBody : MonoBehaviour
{
    [Header("References")]
    public SumoBody opponent;

    [Header("Visual Settings")]
    public SpriteRenderer bodyRenderer;
    public CharacterDatabase characterDB; // ★ 여기에 DB가 연결되어 있어야 합니다!

    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float tileSize = 1.5f;

    public bool IsMoving { get; private set; }
    private Vector3 targetPosition;

    // ★ [추가] 현재 내 캐릭터가 몇 번인지 기억하는 변수
    private int currentSkinIndex = 0;

    private void Awake()
    {
        if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        targetPosition = transform.position;

        if (characterDB != null)
        {
            if (gameObject.CompareTag("Player"))
            {
                // ★ [수정] 지역변수가 아니라 멤버변수에 저장
                currentSkinIndex = PlayerPrefs.GetInt("MyCharacterID", 0);
                ChangeSkin(currentSkinIndex);
            }
        }
    }

    public void ChangeSkin(int index)
    {
        if (characterDB != null && bodyRenderer != null)
        {
            bodyRenderer.sprite = characterDB.GetSkin(index);
        }
    }

    // ============================================
    // ★ [추가] 캐릭터 고유 사운드 재생 함수들
    // ============================================

    public void PlaySuccessSound()
    {
        // 1. DB에서 내 캐릭터(index)의 성공 소리를 가져옴
        AudioClip clip = (characterDB != null) ? characterDB.GetSuccessSound(currentSkinIndex) : null;

        if (clip != null)
        {
            // 2. 고유 소리가 있으면 그거 재생 (Direct)
            SoundManager.Instance.PlayDirectSFX(clip);
        }
        else
        {
            // 3. 없으면 기본 공용 소리 재생 (Enum)
            SoundManager.Instance.PlaySFX(SFX.Success);
        }
    }

    public void PlayFailSound()
    {
        AudioClip clip = (characterDB != null) ? characterDB.GetFailSound(currentSkinIndex) : null;

        if (clip != null)
        {
            SoundManager.Instance.PlayDirectSFX(clip);
        }
        else
        {
            SoundManager.Instance.PlaySFX(SFX.Fail);
        }
    }

    // ... (아래 Update, PushOpponent 등 나머지 코드는 기존과 동일) ...
    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentState == GameManager.GameState.Intro)
        {
            targetPosition = transform.position;
            return;
        }

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);

        if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            transform.position = targetPosition;
            IsMoving = false;
        }
        else IsMoving = true;
    }

    public void PushOpponent(float powerMultiplier)
    {
        MoveStep(1, powerMultiplier);
        if (opponent != null) opponent.GetPushed(powerMultiplier);
    }

    public void MoveStep(int steps, float multiplier)
    {
        if (opponent == null) return;
        float direction = Mathf.Sign(opponent.transform.position.x - transform.position.x);
        float distance = tileSize * multiplier;
        targetPosition += new Vector3(direction * steps * distance, 0, 0);
        IsMoving = true;
    }

    public void GetPushed(float multiplier)
    {
        if (opponent == null) return;
        float direction = Mathf.Sign(transform.position.x - opponent.transform.position.x);
        float distance = tileSize * multiplier;
        targetPosition += new Vector3(direction * distance, 0, 0);
        IsMoving = true;
    }
}