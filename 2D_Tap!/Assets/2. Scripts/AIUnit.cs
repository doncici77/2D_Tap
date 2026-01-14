using UnityEngine;
using System.Collections;

// SumoUnit을 상속받음
public class AIUnit : SumoUnit
{
    protected override void Start()
    {
        base.Start(); // 부모의 초기화 실행
        StartCoroutine(AIRoutine());
    }

    IEnumerator AIRoutine()
    {
        while (!GameManager.Instance.IsGameOver)
        {
            // 생각하는 시간 (랜덤)
            yield return new WaitForSeconds(Random.Range(1.0f, 2.5f));

            if (currentState != State.Moving && !GameManager.Instance.IsGameOver)
            {
                // AI는 게이지를 확률로 결정
                float aiGauge = Random.Range(0.4f, 0.95f);

                PerformAttack(aiGauge); // 부모의 공격 함수 호출
            }
        }
    }

    // AI는 공격 끝나면 Idle 상태 유지 (루틴이 알아서 처리)
    protected override void OnAttackFinished()
    {
        currentState = State.Idle;
    }
}