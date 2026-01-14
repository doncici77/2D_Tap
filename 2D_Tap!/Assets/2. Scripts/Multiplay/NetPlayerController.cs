using UnityEngine;
using Unity.Netcode;
using System.Collections;

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
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetNetPlayer(this);
            }

            // ★★★ [핵심 기능 추가] 내가 클라이언트(2P)라면 카메라를 반대편으로 이동! ★★★
            // 서버(Host)는 기본 카메라(앞면)를 쓰고, 클라이언트(Client)는 뒷면 카메라를 씀
            if (!IsServer)
            {
                AdjustCameraForClient();
            }
        }
    }

    // ★ 카메라를 반대편(Z축 뒤쪽)으로 옮기고 180도 돌려서 찍는 함수
    void AdjustCameraForClient()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            // 원래 카메라: (0, 10, -10) / 회전 (50, 0, 0)
            // 반전 카메라: (0, 10, 10)  / 회전 (50, 180, 0) -> 뒤에서 앞으로 찍음

            mainCam.transform.position = new Vector3(0, 10, 10); // Z를 -10에서 10으로 변경
            mainCam.transform.rotation = Quaternion.Euler(50, 180, 0); // Y축 180도 회전
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

    public void TryAction()
    {
        if (!IsOwner) return;

        // Playing 상태가 아니면 공격 불가
        if (NetGameManager.Instance != null &&
            NetGameManager.Instance.currentNetState.Value != NetGameManager.GameState.Playing)
        {
            return;
        }

        if (state == State.Charging)
        {
            if (CurrentGaugeValue >= currentThreshold)
            {
                RequestAttackServerRpc();
                SuccessAttackLocal();
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

    [ServerRpc]
    void RequestAttackServerRpc()
    {
        if (myBody != null)
        {
            myBody.PushOpponentServer();
        }
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