using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems; // UI 예외 처리를 위해 필요

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

    // 상태 관리
    private enum State { Idle, Charging, Cooldown }
    private State state = State.Idle;

    // UI에서 내가 차징 중인지 알 수 있게 해주는 프로퍼티
    public bool IsCharging => state == State.Charging;

    // 게이지 관련
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
        // 1. 게이지 계산
        if (state == State.Charging)
        {
            float timePassed = Time.time - chargeStartTime;
            CurrentGaugeValue = Mathf.PingPong(timePassed * currentSpeed, 1f);
        }
    }

    public void TryAction()
    {
        if (GameManager.Instance.IsGameOver) return;

        // ★ [삭제] 이 줄을 지워야 밀려나는 중에도 반격 가능!
        // if (myBody.IsMoving) return; 

        // 공격 쿨타임(게이지 충전) 상태만 체크하면 됨
        if (state == State.Charging)
        {
            if (CurrentGaugeValue >= currentThreshold)
            {
                SuccessAttack();
            }
            else
            {
                StartCoroutine(FailRoutine());
            }
        }
        else if (state == State.Idle)
        {
            StartCharging();
        }
    }

    // (혹시 모를 기존 연결 끊김 방지용 - TryAction을 부름)
    public void OnActionBtnPressed()
    {
        TryAction();
    }

    private void SuccessAttack()
    {
        myBody.PushOpponent();

        currentSpeed = Mathf.Min(currentSpeed + speedStep, maxGaugeSpeed);
        currentThreshold = Mathf.Min(currentThreshold + thresholdStep, maxThreshold);

        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator FailRoutine()
    {
        state = State.Cooldown;
        Debug.Log("Miss! 패널티 적용");

        ResetDifficulty();

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
        Debug.Log("플레이어 난이도 초기화됨");
    }
}