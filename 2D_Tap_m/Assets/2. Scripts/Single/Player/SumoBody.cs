using UnityEngine;
using System.Collections; // ★ [추가] 코루틴 사용을 위해 필수

public class SumoBody : MonoBehaviour
{
    [Header("References")]
    public SumoBody opponent;

    [Header("Visual Settings")]
    public SpriteRenderer bodyRenderer;
    public CharacterDatabase characterDB;

    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float tileSize = 1.5f;

    // ★ [추가] 쫀득한 애니메이션 설정값
    [Header("Animation Settings")]
    public float squashDuration = 0.15f; // 애니메이션 시간
    public Vector3 attackScale = new Vector3(1.3f, 0.8f, 1f); // 늘어날 크기 (X는 뚱뚱, Y는 납작)

    public bool IsMoving { get; private set; }
    private Vector3 targetPosition;
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
    // 사운드 재생 함수들
    // ============================================
    public void PlaySuccessSound()
    {
        AudioClip clip = (characterDB != null) ? characterDB.GetSuccessSound(currentSkinIndex) : null;
        if (clip != null) SoundManager.Instance.PlayDirectSFX(clip);
        else SoundManager.Instance.PlaySFX(SFX.Success);
    }

    public void PlayFailSound()
    {
        AudioClip clip = (characterDB != null) ? characterDB.GetFailSound(currentSkinIndex) : null;
        if (clip != null) SoundManager.Instance.PlayDirectSFX(clip);
        else SoundManager.Instance.PlaySFX(SFX.Fail);
    }

    // ============================================
    // ★ [추가] 쫀득한 공격 애니메이션 실행 함수
    // ============================================
    public void PlayAttackAnim()
    {
        // 이미 움직이고 있어도 강제로 멈추고 새로 시작 (반응성 향상)
        StopAllCoroutines();
        StartCoroutine(SquashRoutine());
    }

    private IEnumerator SquashRoutine()
    {
        // 렌더러가 붙은 오브젝트(이미지)만 변형
        Transform targetTr = bodyRenderer.transform;
        Vector3 originalScale = gameObject.transform.localScale;
        float timer = 0f;

        // 1. 찌그러지기 (가는 과정)
        while (timer < squashDuration / 2)
        {
            timer += Time.deltaTime;
            float t = timer / (squashDuration / 2);
            targetTr.localScale = Vector3.Lerp(originalScale, attackScale, t);
            yield return null;
        }

        // 2. 돌아오기 (오는 과정)
        timer = 0f;
        while (timer < squashDuration / 2)
        {
            timer += Time.deltaTime;
            float t = timer / (squashDuration / 2);
            targetTr.localScale = Vector3.Lerp(attackScale, originalScale, t);
            yield return null;
        }

        // 3. 확실하게 원상복구
        targetTr.localScale = originalScale;
    }
    // ============================================

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