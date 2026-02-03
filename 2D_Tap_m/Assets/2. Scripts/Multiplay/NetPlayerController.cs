using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetPlayerController : BasePlayerController
{
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            ResetDifficulty();
            StartCharging();

            if (MultiUIManager.Instance != null)
                MultiUIManager.Instance.SetNetPlayer(this);

            if (!IsServer) AdjustCameraForClient();

            SceneManager.sceneLoaded += OnSceneLoaded;

            // UI 연결
            OnComboChanged += (combo) => {
                if (MultiUIManager.Instance != null) MultiUIManager.Instance.UpdateComboText(combo);
            };

            // 네트워크 공격 요청 추가 (기본 동작 외 추가 로직)
            OnSuccess += (power, gauge) => {
                if (IsOwner) {
                    RequestAttackServerRpc(power);
                }
            };
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
                MultiUIManager.Instance.SetNetPlayer(this);
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

    protected override void Update()
    {
        if (!IsOwner) return;
        base.Update();
    }

    protected override bool CanPerformAction()
    {
        if (!IsOwner) return false;
        return NetGameManager.Instance != null && 
               NetGameManager.Instance.currentNetState.Value == NetGameManager.GameState.Playing;
    }

    [Rpc(SendTo.Server)]
    void RequestAttackServerRpc(float power)
    {
        if (myBody != null)
        {
            myBody.PushOpponent(power);
        }
    }

    [Rpc(SendTo.Owner)]
    public void ResetPlayerStateRpc()
    {
        ResetDifficulty();
        currentState = State.Idle;
        CurrentGaugeValue = 0f;
        
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
}