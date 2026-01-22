using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // ... (변수들은 그대로 유지) ...
    [Header("Connect Body")]
    public SumoBody myBody;

    [Header("Game Settings")]
    public float attackCooldown = 0.5f;

    [Header("Difficulty (Dynamic)")]
    public float initialGaugeSpeed = 3f;
    public float speedStep = 0.5f;
    public float maxGaugeSpeed = 10f;

    [Range(0f, 1f)] public float initialThreshold = 0.5f;
    [Range(0f, 0.1f)] public float thresholdStep = 0.05f;
    [Range(0f, 0.98f)] public float maxThreshold = 0.95f;

    [Header("Combo System")]
    private int comboCount = 0;
    public float comboBonus = 0.2f;

    private enum State { Idle, Charging, Cooldown }
    private State state = State.Idle;
    public bool IsCharging => state == State.Charging;

    private float currentSpeed;
    private float currentThreshold;
    public float CurrentGaugeValue { get; private set; }
    public float CurrentThreshold => currentThreshold;
    private float chargeStartTime;

    private void Start()
    {
        ResetDifficulty();
        StartCharging();
    }

    private void Update()
    {
        if (state == State.Charging)
        {
            float timePassed = Time.time - chargeStartTime;
            CurrentGaugeValue = Mathf.PingPong(timePassed * currentSpeed, 1f);
        }
    }

    public void TryAction()
    {
        if (GameManager.Instance.IsGameOver) return;

        if (state == State.Charging)
        {
            if (CurrentGaugeValue >= currentThreshold)
            {
                comboCount++;
                if (SingleUIManager.Instance != null)
                    SingleUIManager.Instance.UpdateComboText(comboCount);

                SuccessAttack();
            }
            else
            {
                comboCount = 0;
                if (SingleUIManager.Instance != null)
                    SingleUIManager.Instance.UpdateComboText(0);

                StartCoroutine(FailRoutine());
            }
        }
        else if (state == State.Idle)
        {
            StartCharging();
        }
    }

    public void OnActionBtnPressed()
    {
        TryAction();
    }

    private void SuccessAttack()
    {
        float power = 1.0f + ((comboCount - 1) * comboBonus);
        myBody.PushOpponent(power);

        currentSpeed = Mathf.Min(currentSpeed + speedStep, maxGaugeSpeed);
        currentThreshold = Mathf.Min(currentThreshold + thresholdStep, maxThreshold);

        // ★ [수정] SoundManager 직접 호출 -> myBody에게 요청
        // (캐릭터 고유 소리가 있으면 그거 틀고, 없으면 기본 소리 틈)
        if (myBody != null) myBody.PlaySuccessSound();

        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator FailRoutine()
    {
        // ★ [수정] SoundManager 직접 호출 -> myBody에게 요청
        if (myBody != null) myBody.PlayFailSound();

        state = State.Cooldown;
        Debug.Log("Miss! 패널티 적용");

        ResetDifficulty();

        yield return new WaitForSeconds(attackCooldown);
        StartCharging();
    }

    // ... (나머지 코드는 그대로) ...
    private IEnumerator CooldownRoutine()
    {
        state = State.Cooldown;
        yield return new WaitForSeconds(attackCooldown);
        StartCharging();
    }

    private void StartCharging()
    {
        state = State.Charging;
        chargeStartTime = Time.time;
        CurrentGaugeValue = 0f;
    }

    public void ResetDifficulty()
    {
        currentSpeed = initialGaugeSpeed;
        currentThreshold = initialThreshold;
        comboCount = 0;

        if (SingleUIManager.Instance != null)
            SingleUIManager.Instance.UpdateComboText(0);

        Debug.Log("플레이어 난이도 & 콤보 초기화됨");
    }
}