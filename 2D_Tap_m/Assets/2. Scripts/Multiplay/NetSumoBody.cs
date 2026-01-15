using UnityEngine;
using Unity.Netcode;

public class NetSumoBody : NetworkBehaviour
{
    [Header("References")]
    public NetSumoBody opponent;

    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float tileSize = 1.5f; // 기본 1칸 크기

    public NetworkVariable<Vector3> netTargetPos = new NetworkVariable<Vector3>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool IsMoving { get; private set; }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            netTargetPos.Value = transform.position;
            if (NetGameManager.Instance != null) NetGameManager.Instance.RegisterPlayer(this);
        }
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, netTargetPos.Value, Time.deltaTime * moveSpeed);

        if (Vector3.Distance(transform.position, netTargetPos.Value) < 0.05f)
        {
            transform.position = netTargetPos.Value;
            IsMoving = false;
        }
        else IsMoving = true;
    }

    // ★ [수정] 이제 '힘(powerMultiplier)'을 인자로 받습니다.
    public void PushOpponentServer(float powerMultiplier)
    {
        // 1. 나는 힘만큼 전진
        MoveStepServer(1, powerMultiplier);

        // 2. 상대는 밀려남
        if (opponent != null)
        {
            opponent.GetPushedServer(powerMultiplier);
        }
    }

    // ★ [수정] multiplier 적용
    public void MoveStepServer(int directionSign, float multiplier)
    {
        if (opponent == null) return;

        // 상대방 쪽으로 이동 (방향 계산)
        float direction = Mathf.Sign(opponent.transform.position.x - transform.position.x);

        // 이동 거리 = 기본타일크기 * 배율
        float distance = tileSize * multiplier;

        // 방향 * (부호: 전진1, 후진-1) * 거리
        netTargetPos.Value += new Vector3(direction * directionSign * distance, 0, 0);
        IsMoving = true;
    }

    // ★ [수정] 밀려날 때도 배율 적용
    public void GetPushedServer(float multiplier)
    {
        if (opponent == null) return;

        float direction = Mathf.Sign(transform.position.x - opponent.transform.position.x);
        float distance = tileSize * multiplier; // 강하게 맞으면 더 멀리 밀려남

        netTargetPos.Value += new Vector3(direction * distance, 0, 0);
        IsMoving = true;
    }
}