using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
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

    // ★ [추가] 콤보 시스템 변수
    [Header("Combo System")]
    private int comboCount = 0;
    public float comboBonus = 0.2f; // 콤보당 20% 파워 증가
    public float maxComboPower = 3.0f; // 최대 3배

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
                // ★ [추가] 콤보 성공 로직
                comboCount++;

                // UI 갱신
                if (SingleUIManager.Instance != null)
                    SingleUIManager.Instance.UpdateComboText(comboCount);

                SuccessAttack();
            }
            else
            {
                // ★ [추가] 콤보 실패 (초기화)
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
        // ★ [수정] 콤보 파워 계산 후 전달
        float power = 1.0f + ((comboCount - 1) * comboBonus);
        power = Mathf.Min(power, maxComboPower);

        myBody.PushOpponent(power); // 파워 실어서 밀기!

        currentSpeed = Mathf.Min(currentSpeed + speedStep, maxGaugeSpeed);
        currentThreshold = Mathf.Min(currentThreshold + thresholdStep, maxThreshold);

        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator FailRoutine()
    {
        state = State.Cooldown;
        Debug.Log("Miss! 패널티 적용");

        ResetDifficulty(); // 여기서 콤보도 같이 초기화됨

        yield return new WaitForSeconds(attackCooldown);
        StartCharging();
    }

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

        // ★ [추가] 난이도 리셋될 때 콤보도 초기화
        comboCount = 0;
        if (SingleUIManager.Instance != null)
            SingleUIManager.Instance.UpdateComboText(0);

        Debug.Log("플레이어 난이도 & 콤보 초기화됨");
    }
}