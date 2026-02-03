using UnityEngine;
using System.Collections;

public class AIController : MonoBehaviour, IController
{
    [Header("Connect Body")]
    [SerializeField] private GameObject bodyObject;
    private IBody myBody;

    [Header("AI Stats")]
    public float minThinkTime = 1.0f;
    public float maxThinkTime = 2.5f;
    [Range(0, 1)] public float successRate = 0.8f;

    [Header("Difficulty Scaling")]
    public float aiPower = 1.0f;

    private float limitMinTime = 0.3f;
    private float limitMaxTime = 0.8f;

    private void Awake()
    {
        if (bodyObject != null)
        {
            myBody = bodyObject.GetComponent<IBody>();
        }
    }

    private void Start()
    {
        StartCoroutine(AIRoutine());
    }

    public void LevelUpAI()
    {
        minThinkTime = Mathf.Max(minThinkTime * 0.9f, limitMinTime);
        maxThinkTime = Mathf.Max(maxThinkTime * 0.9f, limitMaxTime);
        aiPower += 0.1f;
        successRate = Mathf.Min(successRate + 0.02f, 0.95f);
        Debug.Log($"AI 레벨업! 속도:{minThinkTime}~{maxThinkTime}, 파워:{aiPower}, 확률:{successRate}");
    }

    IEnumerator AIRoutine()
    {
        while (true)
        {
            if (GameManager.Instance.currentState != GameManager.GameState.Playing)
            {
                yield return null;
                continue;
            }

            yield return new WaitForSeconds(Random.Range(minThinkTime, maxThinkTime));

            if (GameManager.Instance.currentState == GameManager.GameState.Playing)
            {
                TryAction();
            }
        }
    }

    public void TryAction()
    {
        TryAttack();
    }

    private void TryAttack()
    {
        if (Random.value < successRate)
        {
            if (myBody != null)
            {
                myBody.PushOpponent(aiPower);
                myBody.PlaySuccessSound();
                myBody.PlayAttackAnim();
            }
        }
        else
        {
            if (myBody != null) myBody.PlayFailSound();
            Debug.Log("AI Miss!");
        }
    }
}
