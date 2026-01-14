using UnityEngine;

// SumoUnit을 상속받음
public class PlayerUnit : SumoUnit
{
    [Header("Speed Settings")]
    public float initialGaugeSpeed = 3f;    // 시작 속도
    public float speedStep = 0.5f;          // 턴마다 빨라질 속도
    public float maxGaugeSpeed = 10f;       // 속도 최대 제한

    [Header("Difficulty Settings")]
    [Range(0f, 1f)] public float initialThreshold = 0.5f; // 시작 성공 범위 (0.7 이상이면 성공)
    [Range(0f, 0.1f)] public float thresholdStep = 0.05f; // 턴마다 좁아질 범위 (0.05씩 어려워짐)
    [Range(0f, 0.98f)] public float maxThreshold = 0.95f; // 최대 난이도 (0.95 이상 맞춰야 함)

    // [중요] 외부(UI)에서 이 값을 참조해서 게이지 색을 바꿔야 함
    public float currentSuccessThreshold;

    // 내부 변수
    private float currentSpeed;
    public float currentGaugeValue = 0f;
    private float chargeStartTime;

    protected override void Start()
    {
        base.Start();

        // 게임 시작 시 난이도 초기화
        ResetDifficulty();
        StartCharging();
    }

    protected override void Update()
    {
        base.Update();

        // 플레이어 전용: 게이지 핑퐁 계산
        if (currentState == State.Charging)
        {
            float timePassed = Time.time - chargeStartTime;
            currentGaugeValue = Mathf.PingPong(timePassed * currentSpeed, 1f);
        }
    }

    // UI 버튼 연결용 함수
    public void OnActionBtnPressed()
    {
        if (GameManager.Instance.IsGameOver) return;

        if (currentState == State.Idle)
        {
            StartCharging();
        }
        else if (currentState == State.Charging)
        {
            // [수정] 성공 범위 안에 들어왔는지 체크!
            if (currentGaugeValue >= currentSuccessThreshold)
            {
                // 성공! -> 공격 수행
                PerformAttack(currentGaugeValue);
            }
            else
            {
                // 실패! (빗나감) -> 난이도 초기화 및 다시 차징
                Debug.Log("Miss! 난이도 초기화");
                ResetDifficulty();
                StartCharging(); // 공격 모션 없이 바로 다시 게이지 시작
            }
        }
    }

    public void StartCharging()
    {
        currentState = State.Charging;
        chargeStartTime = Time.time;
        currentGaugeValue = 0f;
    }

    // 공격이 성공적으로 끝났을 때만 호출됨
    protected override void OnAttackFinished()
    {
        // 1. 속도 증가
        currentSpeed += speedStep;
        if (currentSpeed > maxGaugeSpeed) currentSpeed = maxGaugeSpeed;

        // 2. 성공 범위 좁히기 (난이도 상승)
        // 숫자가 커질수록 게이지 끝부분만 맞춰야 하므로 어려워짐
        currentSuccessThreshold += thresholdStep;
        if (currentSuccessThreshold > maxThreshold) currentSuccessThreshold = maxThreshold;

        StartCharging();
    }

    // [추가] 난이도(속도, 범위)를 초기 상태로 되돌리는 함수
    private void ResetDifficulty()
    {
        currentSpeed = initialGaugeSpeed;
        currentSuccessThreshold = initialThreshold;
    }
}