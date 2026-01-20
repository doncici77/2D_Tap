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

    [Header("Difficulty Scaling")]
    public float aiPower = 1.0f; // ★ [추가] AI의 미는 힘 (기본 1배)

    private float limitMinTime = 0.3f;
    private float limitMaxTime = 0.8f;

    private void Start()
    {
        StartCoroutine(AIRoutine());
    }

    public void LevelUpAI()
    {
        // 1. 생각하는 시간 단축 (더 빨라짐)
        minThinkTime = Mathf.Max(minThinkTime * 0.9f, limitMinTime);
        maxThinkTime = Mathf.Max(maxThinkTime * 0.9f, limitMaxTime);

        // 2. ★ [추가] 라운드가 지날수록 AI 힘도 조금씩 세짐 (예: 10% 증가)
        aiPower += 0.1f;

        // 3. 성공률도 조금 올리기 (최대 95%까지)
        successRate = Mathf.Min(successRate + 0.02f, 0.95f);

        Debug.Log($"AI 레벨업! 속도:{minThinkTime}~{maxThinkTime}, 파워:{aiPower}, 확률:{successRate}");
    }

    IEnumerator AIRoutine()
    {
        while (true)
        {
            // ★ [수정] 게임오버뿐만 아니라 '인트로' 중일 때도 대기해야 함
            if (GameManager.Instance.currentState != GameManager.GameState.Playing)
            {
                yield return null;
                continue;
            }

            // 1. 생각하기
            yield return new WaitForSeconds(Random.Range(minThinkTime, maxThinkTime));

            // 2. 생각 끝난 후에도 게임 중인지 다시 체크 (생각하는 동안 게임이 끝날 수도 있음)
            if (GameManager.Instance.currentState == GameManager.GameState.Playing)
            {
                TryAttack();
            }
        }
    }

    void TryAttack()
    {
        // 성공 확률 체크
        if (Random.value < successRate)
        {
            // ★ [수정] 변경된 함수에 맞춰서 파워값(aiPower)을 넣어줍니다.
            // AI는 콤보가 없으므로 설정된 고정 파워로 공격합니다.
            myBody.PushOpponent(aiPower);
        }
        else
        {
            Debug.Log("AI Miss!");
            // (선택 사항) AI가 실수했을 때 이모티콘 같은 걸 띄워도 좋습니다.
        }
    }
}