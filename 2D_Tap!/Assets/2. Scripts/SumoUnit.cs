using UnityEngine;
using System.Collections;

public class SumoUnit : MonoBehaviour
{
    public enum State { Idle, Charging, Moving, Stunned }

    [Header("Base Settings")]
    public SumoUnit opponent;
    public float moveSpeed = 10f;
    public float attackCooldown = 0.5f;

    [Header("Game Rule")]
    [Range(0f, 1f)]
    public float successThreshold = 0.5f;

    [Header("Direction Fix")]
    public bool invertPushDirection = false;
    public bool followOpponent = true;

    [Header("Status")]
    public State currentState = State.Idle;
    protected Vector3 targetPosition;

    protected virtual void Start()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;

        // 인트로 애니메이션을 위해 초기 위치는 Transform 값 그대로 두거나 GameManager에 맡김
        targetPosition = transform.position;
    }

    protected virtual void Update()
    {

        if (GameManager.Instance.currentState == GameManager.GameState.Intro)
        {
            // 인트로 중에는 targetPosition을 현재 위치로 계속 동기화해서
            // Lerp가 튀지 않게 함
            targetPosition = transform.position;
            return;
        }

        if (GameManager.Instance.currentState == GameManager.GameState.GameOver) return;

        // 평소 이동
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
    }

    public void PerformAttack(float gauge)
    {
        // 게임 중이 아니면 공격 불가
        if (GameManager.Instance.currentState != GameManager.GameState.Playing) return;

        currentState = State.Moving;

        bool isSuccess = (gauge >= successThreshold);

        if (isSuccess)
        {
            int pushStep = 1;
            int direction = (transform.position.x < opponent.transform.position.x) ? 1 : -1;
            if (invertPushDirection) direction *= -1;

            if (opponent != null) opponent.GetPushed(direction, pushStep);
            if (followOpponent) MoveSelf(direction, pushStep);
        }

        StartCoroutine(AttackCooldownRoutine());
    }

    public void GetPushed(int direction, int steps)
    {
        float distance = steps * GameManager.Instance.tileSize;
        targetPosition += new Vector3(direction * distance, 0, 0);
    }

    public void MoveSelf(int direction, int steps)
    {
        float distance = steps * GameManager.Instance.tileSize;
        targetPosition += new Vector3(direction * distance, 0, 0);
    }

    protected virtual IEnumerator AttackCooldownRoutine()
    {
        yield return new WaitForSeconds(attackCooldown);
        // Playing 상태일 때만 복귀
        if (GameManager.Instance.currentState == GameManager.GameState.Playing)
            OnAttackFinished();
    }

    protected virtual void OnAttackFinished()
    {
        currentState = State.Idle;
    }
}