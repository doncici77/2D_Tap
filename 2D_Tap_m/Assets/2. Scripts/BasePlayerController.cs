using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public abstract class BasePlayerController : NetworkBehaviour, IController
{
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
    public float comboBonus = 0.2f;
    public float maxComboPower = 3.0f;

    // Events for Decoupling
    public event Action<int> OnComboChanged;
    public event Action<float, float> OnSuccess; // power, currentGauge
    public event Action OnFail;
    public event Action OnStateChanged;

    [Header("Connect Body")]
    [SerializeField] protected GameObject bodyObject;
    protected IBody myBody;

    protected enum State { Idle, Charging, Cooldown }
    protected State currentState = State.Idle;
    
    public bool IsCharging => currentState == State.Charging;
    public float CurrentGaugeValue { get; protected set; }
    public float CurrentThreshold { get; protected set; }
    public int ComboCount { get; protected set; }

    protected float currentSpeed;
    protected float chargeStartTime;

    protected virtual void Awake()
    {
        if (bodyObject != null)
        {
            myBody = bodyObject.GetComponent<IBody>();
        }
    }

    protected virtual void Start()
    {
        ResetDifficulty();
        StartCharging();
        
        // Initialize basic body interactions
        OnSuccess += (power, gauge) => {
            if (myBody != null) {
                myBody.PushOpponent(power);
                myBody.PlaySuccessSound();
                myBody.PlayAttackAnim();
            }
        };

        OnFail += () => {
            if (myBody != null) myBody.PlayFailSound();
        };
    }

    protected virtual void Update()
    {
        if (currentState == State.Charging)
        {
            float timePassed = Time.time - chargeStartTime;
            CurrentGaugeValue = Mathf.PingPong(timePassed * currentSpeed, 1f);
        }
    }

    public virtual void TryAction()
    {
        if (!CanPerformAction()) return;

        if (currentState == State.Charging)
        {
            if (CurrentGaugeValue >= CurrentThreshold)
            {
                HandleSuccess();
            }
            else
            {
                HandleFail();
            }
        }
        else if (currentState == State.Idle)
        {
            StartCharging();
        }
    }

    protected abstract bool CanPerformAction();

    protected virtual void HandleSuccess()
    {
        ComboCount++;
        OnComboChanged?.Invoke(ComboCount);

        float power = Mathf.Min(1.0f + ((ComboCount - 1) * comboBonus), maxComboPower);
        
        currentSpeed = Mathf.Min(currentSpeed + speedStep, maxGaugeSpeed);
        CurrentThreshold = Mathf.Min(CurrentThreshold + thresholdStep, maxThreshold);

        OnSuccess?.Invoke(power, CurrentGaugeValue);
        StartCoroutine(CooldownRoutine());
    }

    protected virtual void HandleFail()
    {
        ComboCount = 0;
        OnComboChanged?.Invoke(0);
        
        OnFail?.Invoke();
        ResetDifficulty();
        StartCoroutine(CooldownRoutine());
    }

    protected IEnumerator CooldownRoutine()
    {
        currentState = State.Cooldown;
        OnStateChanged?.Invoke();
        yield return new WaitForSeconds(attackCooldown);
        StartCharging();
    }

    protected void StartCharging()
    {
        currentState = State.Charging;
        chargeStartTime = Time.time;
        CurrentGaugeValue = 0f;
        OnStateChanged?.Invoke();
    }

    public virtual void ResetDifficulty()
    {
        currentSpeed = initialGaugeSpeed;
        CurrentThreshold = initialThreshold;
        ComboCount = 0;
        OnComboChanged?.Invoke(0);
    }
}
