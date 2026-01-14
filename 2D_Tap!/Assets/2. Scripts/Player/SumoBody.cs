using UnityEngine;

public class SumoBody : MonoBehaviour
{
    [Header("References")]
    public SumoBody opponent; // 상대방 몸체

    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float tileSize = 1.5f;

    // 외부(Controller)에서 내 상태를 알기 위한 변수
    public bool IsMoving { get; private set; }
    private Vector3 targetPosition;

    private void Start()
    {
        // 시작할 때 내 위치 고정
        targetPosition = transform.position;
    }

    private void Update()
    {
        // 인트로 중일 때는 강제로 위치 동기화 (Lerp 튐 방지)
        if (GameManager.Instance.currentState == GameManager.GameState.Intro)
        {
            targetPosition = transform.position;
            return;
        }

        // 1. 목표 지점까지 부드럽게 이동 (Lerp)
        // 리지드바디 없이 그냥 좌표를 옮깁니다.
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);

        // 2. 거의 도착했는지 확인 (0.05f 오차 범위)
        if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            // 완전히 도착한 걸로 처리 (미세 떨림 방지)
            transform.position = targetPosition;
            IsMoving = false;
        }
        else
        {
            IsMoving = true;
        }
    }

    // 공격 명령을 수행하는 함수 (Controller가 호출)
    public void PushOpponent()
    {
        // 1. 나는 앞으로 1칸 전진
        MoveStep(1);

        // 2. 상대방은 뒤로 1칸 밀려남
        if (opponent != null)
        {
            opponent.GetPushed();
        }
    }

    // 내가 스스로 앞으로 움직일 때
    public void MoveStep(int steps)
    {
        // 상대방이 있는 방향 계산 (오른쪽이든 왼쪽이든 알아서 구함)
        // 상대 x좌표 - 내 x좌표의 부호(+1 or -1)를 구함
        float direction = Mathf.Sign(opponent.transform.position.x - transform.position.x);

        // 목표 위치 갱신
        targetPosition += new Vector3(direction * steps * tileSize, 0, 0);
        IsMoving = true;
    }

    // 상대에 의해 밀려날 때
    public void GetPushed()
    {
        // 내 x좌표 - 상대 x좌표 = 내가 밀려날 방향
        float direction = Mathf.Sign(transform.position.x - opponent.transform.position.x);

        targetPosition += new Vector3(direction * tileSize, 0, 0);
        IsMoving = true;
    }
}