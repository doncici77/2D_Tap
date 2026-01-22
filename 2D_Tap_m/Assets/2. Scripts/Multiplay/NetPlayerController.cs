using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetPlayerController : NetworkBehaviour
{
    [Header("Connect Body")]
    public NetSumoBody myBody; // ★ 여기에 연결되어 있어야 함!

    [Header("Game Settings")]
    public float attackCooldown = 0.5f;

    [Header("Difficulty")]
    public float initialGaugeSpeed = 3f;
    public float speedStep = 0.5f;
    public float maxGaugeSpeed = 10f;
    [Range(0f, 1f)] public float initialThreshold = 0.5f;
    [Range(0f, 0.1f)] public float thresholdStep = 0.05f;
    [Range(0f, 0.98f)] public float maxThreshold = 0.95f;

    [Header("Combo System")]
    private int comboCount = 0;
    public float comboBonus = 0.2f;
    public float maxComboPower = 3.0f;

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
        if (IsOwner)
        {
            ResetDifficulty();
            StartCharging();

            if (MultiUIManager.Instance != null)
            {
                MultiUIManager.Instance.SetNetPlayer(this);
            }

            if (!IsServer)
            {
                AdjustCameraForClient();
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsOwner)
        {
            ResetDifficulty();
            StartCharging();

            if (MultiUIManager.Instance != null)
            {
                MultiUIManager.Instance.SetNetPlayer(this);
            }
        }

        if (IsOwner && !IsServer)
        {
            StartCoroutine(DelayedCameraAdjust());
        }
    }

    IEnumerator DelayedCameraAdjust()
    {
        yield return null;
        AdjustCameraForClient();
    }

    void AdjustCameraForClient()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0, 0, 10);
            mainCam.transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        else
        {
            Invoke(nameof(AdjustCameraForClient), 0.1f);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

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

        if (NetGameManager.Instance != null &&
            NetGameManager.Instance.currentNetState.Value != NetGameManager.GameState.Playing) return;

        if (state == State.Charging)
        {
            if (CurrentGaugeValue >= CurrentThreshold) // 성공!
            {
                comboCount++;

                if (MultiUIManager.Instance != null)
                    MultiUIManager.Instance.UpdateComboText(comboCount);

                float power = 1.0f + ((comboCount - 1) * comboBonus);
                power = Mathf.Min(power, maxComboPower);

                RequestAttackServerRpc(power);
                SuccessAttackLocal();
            }
            else // 실패!
            {
                comboCount = 0;
                if (MultiUIManager.Instance != null)
                    MultiUIManager.Instance.UpdateComboText(0);

                StartCoroutine(FailRoutine());
            }
        }
        else if (state == State.Idle)
        {
            StartCharging();
        }
    }

    [Rpc(SendTo.Server)]
    void RequestAttackServerRpc(float power)
    {
        if (myBody != null)
        {
            myBody.PushOpponentServer(power);
        }
    }

    [Rpc(SendTo.Owner)]
    public void ResetPlayerStateRpc()
    {
        ResetDifficulty();
        state = State.Idle;
        CurrentGaugeValue = 0f;
        comboCount = 0;
        if (MultiUIManager.Instance != null) MultiUIManager.Instance.UpdateComboText(0);

        StartCharging();
        if (!IsServer) AdjustCameraForClient();
    }

    [Rpc(SendTo.Server)]
    public void SendRematchRequestServerRpc()
    {
        if (NetGameManager.Instance != null)
        {
            NetGameManager.Instance.PlayerRequestRematch(this.OwnerClientId);
        }
    }

    private void SuccessAttackLocal()
    {
        currentSpeed = Mathf.Min(currentSpeed + speedStep, maxGaugeSpeed);
        currentThreshold = Mathf.Min(currentThreshold + thresholdStep, maxThreshold);

        // ★ [수정] myBody에게 소리 재생 요청
        if (myBody != null) myBody.PlaySuccessSound();
        else SoundManager.Instance.PlaySFX(SFX.Success);

        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator FailRoutine()
    {
        // ★ [수정] myBody에게 소리 재생 요청
        if (myBody != null) myBody.PlayFailSound();
        else SoundManager.Instance.PlaySFX(SFX.Fail);

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