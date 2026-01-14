using UnityEngine;
using Unity.Netcode; // 넷코드 필수

public class NetSumoBody : NetworkBehaviour
{
    [Header("References")]
    public NetSumoBody opponent; // 상대방 (나중에 GameManager가 연결해줌)

    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float tileSize = 1.5f;

    // ★ [핵심] 네트워크 변수 (서버만 수정 가능, 모두가 읽기 가능)
    // 목표 위치(TargetPosition)를 서버가 관리합니다.
    public NetworkVariable<Vector3> netTargetPos = new NetworkVariable<Vector3>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool IsMoving { get; private set; }

    // ★ [여기가 핵심] 캐릭터가 생성될 때 실행되는 함수
    public override void OnNetworkSpawn()
    {
        // 서버에서만 실행 (등록은 서버가 관리하므로)
        if (IsServer)
        {
            // 1. 초기 위치 설정
            netTargetPos.Value = transform.position;

            // 2. ★ [호출 위치] 매니저에게 나를 등록해달라고 요청!
            if (NetGameManager.Instance != null)
            {
                NetGameManager.Instance.RegisterPlayer(this);
            }
            else
            {
                Debug.LogError("NetGameManager가 씬에 없습니다!");
            }
        }
    }

    private void Update()
    {
        // [모든 클라이언트 공통]
        // netTargetPos.Value가 서버에 의해 바뀌면, 내 화면의 캐릭터를 그쪽으로 이동시킴
        transform.position = Vector3.Lerp(transform.position, netTargetPos.Value, Time.deltaTime * moveSpeed);

        // 도착 여부 체크
        if (Vector3.Distance(transform.position, netTargetPos.Value) < 0.05f)
        {
            transform.position = netTargetPos.Value;
            IsMoving = false;
        }
        else
        {
            IsMoving = true;
        }
    }

    // ★ [서버 전용] 실제 이동 로직 (Controller가 호출함)
    public void PushOpponentServer()
    {
        // 1. 나 전진 (서버에서만 값 변경 가능)
        MoveStepServer(1);

        // 2. 상대 후퇴
        if (opponent != null)
        {
            opponent.GetPushedServer();
        }
    }

    // 내가 움직일 때
    public void MoveStepServer(int steps)
    {
        if (opponent == null) return;

        // 상대방 방향 계산
        float direction = Mathf.Sign(opponent.transform.position.x - transform.position.x);

        // 네트워크 변수 값 수정 -> 모든 클라이언트에 자동 전파됨
        netTargetPos.Value += new Vector3(direction * steps * tileSize, 0, 0);
        IsMoving = true;
    }

    // 밀려날 때
    public void GetPushedServer()
    {
        if (opponent == null) return;

        // 밀려날 방향 계산
        float direction = Mathf.Sign(transform.position.x - opponent.transform.position.x);

        netTargetPos.Value += new Vector3(direction * tileSize, 0, 0);
        IsMoving = true;
    }
}