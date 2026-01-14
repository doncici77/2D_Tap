using UnityEngine;
using System.Collections;

public class AIController : MonoBehaviour
{
    [Header("Connect Body")]
    public SumoBody myBody;

    [Header("AI Stats")]
    public float minThinkTime = 1.0f;
    public float maxThinkTime = 2.5f;
    [Range(0, 1)] public float successRate = 0.8f;

    private float limitMinTime = 0.3f;
    private float limitMaxTime = 0.8f;

    private void Start()
    {
        StartCoroutine(AIRoutine());
    }

    public void LevelUpAI()
    {
        minThinkTime = Mathf.Max(minThinkTime * 0.8f, limitMinTime);
        maxThinkTime = Mathf.Max(maxThinkTime * 0.8f, limitMaxTime);
    }

    IEnumerator AIRoutine()
    {
        while (true)
        {
            if (GameManager.Instance.IsGameOver)
            {
                yield return null;
                continue;
            }

            // 1. 생각하기
            yield return new WaitForSeconds(Random.Range(minThinkTime, maxThinkTime));

            // ★ [변경] IsMoving 체크나 대기 없이 바로 공격 시도!
            // 게임 오버만 아니면 무조건 밀어붙임
            if (!GameManager.Instance.IsGameOver)
            {
                TryAttack();
            }
        }
    }

    void TryAttack()
    {
        if (Random.value < successRate)
        {
            myBody.PushOpponent();
        }
        else
        {
            Debug.Log("AI Miss!");
        }
    }
}