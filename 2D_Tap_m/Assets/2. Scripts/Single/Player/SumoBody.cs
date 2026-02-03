using UnityEngine;
using System.Collections;

public class SumoBody : MonoBehaviour, IBody
{
    [Header("References")]
    public SumoBody opponent;

    [Header("Visual Settings")]
    public SpriteRenderer bodyRenderer;
    public CharacterDatabase characterDB;

    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float tileSize = 1.5f;

    [Header("Animation Settings")]
    public float squashDuration = 0.15f;
    public Vector3 attackScale = new Vector3(1.3f, 0.8f, 1f);

    public bool IsMoving { get; private set; }
    private Vector3 targetPosition;
    private int currentSkinIndex = 0;
    private Coroutine attackRoutine;

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

    public void PlayAttackAnim()
    {
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        attackRoutine = StartCoroutine(SquashRoutine());
    }

    private IEnumerator SquashRoutine()
    {
        Transform targetTr = bodyRenderer.transform;
        Vector3 originalScale = gameObject.transform.localScale;
        float timer = 0f;

        while (timer < squashDuration / 2)
        {
            timer += Time.deltaTime;
            float t = timer / (squashDuration / 2);
            targetTr.localScale = Vector3.Lerp(originalScale, attackScale, t);
            yield return null;
        }

        timer = 0f;
        while (timer < squashDuration / 2)
        {
            timer += Time.deltaTime;
            float t = timer / (squashDuration / 2);
            targetTr.localScale = Vector3.Lerp(attackScale, originalScale, t);
            yield return null;
        }

        targetTr.localScale = originalScale;
        attackRoutine = null;
    }

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
