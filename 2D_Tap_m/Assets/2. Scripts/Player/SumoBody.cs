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
        targetPosition = transform.position;
    }

    private void Update()
    {
        if (GameManager.Instance.currentState == GameManager.GameState.Intro)
        {
            targetPosition = transform.position;
            return;
        }

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);

        if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            transform.position = targetPosition;
            IsMoving = false;
        }
        else
        {
            IsMoving = true;
        }
    }

    // ★ [수정] 힘(powerMultiplier)을 받아서 공격
    public void PushOpponent(float powerMultiplier)
    {
        // 1. 나는 힘만큼 전진
        MoveStep(1, powerMultiplier);

        // 2. 상대방은 밀려남
        if (opponent != null)
        {
            opponent.GetPushed(powerMultiplier);
        }
    }

    // ★ [수정] 배율 적용
    public void MoveStep(int steps, float multiplier)
    {
        float direction = Mathf.Sign(opponent.transform.position.x - transform.position.x);

        // 이동 거리 = 기본크기 * 배율
        float distance = tileSize * multiplier;

        targetPosition += new Vector3(direction * steps * distance, 0, 0);
        IsMoving = true;
    }

    // ★ [수정] 밀려날 때도 배율 적용 (AI가 나를 밀 때도 사용됨)
    public void GetPushed(float multiplier)
    {
        float direction = Mathf.Sign(transform.position.x - opponent.transform.position.x);

        // 강하게 맞으면 더 멀리 밀려남
        float distance = tileSize * multiplier;

        targetPosition += new Vector3(direction * distance, 0, 0);
        IsMoving = true;
    }
}