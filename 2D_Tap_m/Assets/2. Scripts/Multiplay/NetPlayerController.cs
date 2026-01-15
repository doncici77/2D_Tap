using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetPlayerController : NetworkBehaviour
{
    [Header("Connect Body")]
    public NetSumoBody myBody;

    [Header("Game Settings")]
    public float attackCooldown = 0.5f;

    [Header("Difficulty")]
    public float initialGaugeSpeed = 3f;
    public float speedStep = 0.5f;
    public float maxGaugeSpeed = 10f;
    [Range(0f, 1f)] public float initialThreshold = 0.5f;
    [Range(0f, 0.1f)] public float thresholdStep = 0.05f;
    [Range(0f, 0.98f)] public float maxThreshold = 0.95f;

    // ★ [추가] 콤보 관련 변수
    [Header("Combo System")]
    private int comboCount = 0; // 현재 콤보
    public float comboBonus = 0.2f; // 콤보당 추가 파워 (예: 1콤보당 20% 쎄짐)
    public float maxComboPower = 3.0f; // 최대 3배까지만 강해짐 (밸런스 조절)

    private enum State { Idle, Charging, Cooldown }
    private State state = State.Idle;
    public bool IsCharging => state == State.Charging;

    private float currentSpeed;
    private float currentThreshold;
    public float CurrentGaugeValue { get; private set; }
    public float CurrentThreshold => currentThreshold;
    private float chargeStartTime;

    public override void OnNetworkSpawn()
    {
        // ★ 내 캐릭터일 때만 실행
        if (IsOwner)
        {
            ResetDifficulty();
            StartCharging();

            // UI 매니저 연결
            if (MultiUIManager.Instance != null)
            {
                MultiUIManager.Instance.SetNetPlayer(this);
            }

            // 서버(Host)는 기본 카메라(앞면)를 쓰고, 클라이언트(Client)는 뒷면 카메라를 씀
            if (!IsServer)
            {
                AdjustCameraForClient();
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    // ★ 오브젝트가 사라질 때 이벤트 연결 해제 (메모리 누수 방지)
    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    // ★ 씬 로드가 끝나면 호출되는 함수
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsOwner)
        {
            ResetDifficulty();
            StartCharging();

            // UI 매니저 연결
            if (MultiUIManager.Instance != null)
            {
                MultiUIManager.Instance.SetNetPlayer(this);
            }
        }

        // 내가 주인이고, 서버가 아니라면(클라이언트라면) 카메라 조정
        if (IsOwner && !IsServer)
        {
            // 조금 더 확실하게 하기 위해 약간의 딜레이 후 실행
            StartCoroutine(DelayedCameraAdjust());
        }
    }

    // 로딩 직후 카메라가 덮어씌워지는 것을 방지하기 위한 0.1초 딜레이
    IEnumerator DelayedCameraAdjust()
    {
        yield return null; // 한 프레임 대기
        AdjustCameraForClient();
    }

    // ★ 카메라를 반대편(Z축 뒤쪽)으로 옮기고 180도 돌려서 찍는 함수
    void AdjustCameraForClient()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0, 0, 10); // Z를 -10에서 10으로 변경
            mainCam.transform.rotation = Quaternion.Euler(0, 180, 0); // Y축 180도 회전
        }
        else
        {
            // 혹시 카메라를 못 찾으면 0.1초 뒤에 다시 시도 (안전장치)
            Invoke(nameof(AdjustCameraForClient), 0.1f);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        // 게임 시작 전/후에는 게이지 멈춤
        if (NetGameManager.Instance != null &&
            NetGameManager.Instance.currentNetState.Value != NetGameManager.GameState.Playing)
            return;

        if (state == State.Charging)
        {
            float timePassed = Time.time - chargeStartTime;
            CurrentGaugeValue = Mathf.PingPong(timePassed * currentSpeed, 1f);
        }
    }

    // ★ [수정] TryAction에서 콤보 계산
    public void TryAction()
    {
        if (!IsOwner) return;

        if (NetGameManager.Instance != null &&
            NetGameManager.Instance.currentNetState.Value != NetGameManager.GameState.Playing) return;

        if (state == State.Charging)
        {
            if (CurrentGaugeValue >= CurrentThreshold) // 성공!
            {
                comboCount++; // 콤보 증가

                // UI에 콤보 표시 (멀티 매니저가 있다면)
                if (MultiUIManager.Instance != null)
                    MultiUIManager.Instance.UpdateComboText(comboCount);

                // 서버에 공격 요청 (현재 콤보 수치에 따른 파워 계산)
                float power = 1.0f + ((comboCount - 1) * comboBonus);
                power = Mathf.Min(power, maxComboPower); // 최대치 제한

                RequestAttackServerRpc(power); // 파워를 실어서 보냄
                SuccessAttackLocal();
            }
            else // 실패!
            {
                comboCount = 0; // 콤보 초기화
                if (MultiUIManager.Instance != null)
                    MultiUIManager.Instance.UpdateComboText(0); // UI도 리셋

                StartCoroutine(FailRoutine());
            }
        }
        else if (state == State.Idle)
        {
            StartCharging();
        }
    }

    // ★ [수정] 서버로 파워값 전달
    [ServerRpc]
    void RequestAttackServerRpc(float power)
    {
        if (myBody != null)
        {
            // 몸체에게 계산된 파워로 밀라고 명령
            myBody.PushOpponentServer(power);
        }
    }

    // ★ [추가] 리셋될 때 콤보도 초기화
    [Rpc(SendTo.Owner)]
    public void ResetPlayerStateRpc()
    {
        ResetDifficulty();
        state = State.Idle;
        CurrentGaugeValue = 0f;

        // 콤보 초기화
        comboCount = 0;
        if (MultiUIManager.Instance != null) MultiUIManager.Instance.UpdateComboText(0);

        StartCharging();
        if (!IsServer) AdjustCameraForClient();
    }

    // ★ [추가] 클라이언트 -> 서버로 재시작 요청을 전달하는 중계 RPC
    [ServerRpc] // 기본값(RequireOwnership = true) 사용. 내 캐릭터니까 가능!
    public void SendRematchRequestServerRpc()
    {
        // 서버에서 실행됨
        if (NetGameManager.Instance != null)
        {
            // 매니저에게 "나(OwnerClientId) 재시작할래"라고 보고
            NetGameManager.Instance.PlayerRequestRematch(this.OwnerClientId);
        }
    }

    private void SuccessAttackLocal()
    {
        currentSpeed = Mathf.Min(currentSpeed + speedStep, maxGaugeSpeed);
        currentThreshold = Mathf.Min(currentThreshold + thresholdStep, maxThreshold);
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator FailRoutine()
    {
        state = State.Cooldown;
        Debug.Log("Miss!");
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
    }
}