using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum GameState { Intro, Playing, GameOver }
    public GameState currentState = GameState.Intro;

    // AI가 참조할 수 있도록 연결
    public bool IsGameOver => currentState == GameState.GameOver;

    [Header("References")]
    public Transform playerTransform;
    public Transform aiTransform;

    [Header("Map Settings")]
    public float tileSize = 1.5f;
    public float fallLineX = 8.0f;

    [Header("Intro Settings")]
    public float startDistance = 10f;
    public float standDistance = 1.5f;
    public float introSpeed = 15f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        StartCoroutine(IntroSequence());
    }

    IEnumerator IntroSequence()
    {
        currentState = GameState.Intro;

        // 1. 시작 위치 강제 이동
        playerTransform.position = new Vector3(-startDistance, playerTransform.position.y, 0);
        aiTransform.position = new Vector3(startDistance, aiTransform.position.y, 0);

        yield return new WaitForSeconds(0.5f);

        // 2. 중앙으로 돌진 (Y축 오류 방지 적용됨)
        Vector3 targetPos = new Vector3(-standDistance, playerTransform.position.y, 0);

        while (Vector3.Distance(playerTransform.position, targetPos) > 0.1f)
        {
            playerTransform.position = Vector3.MoveTowards(playerTransform.position,
                new Vector3(-standDistance, playerTransform.position.y, 0), Time.deltaTime * introSpeed);

            aiTransform.position = Vector3.MoveTowards(aiTransform.position,
                new Vector3(standDistance, aiTransform.position.y, 0), Time.deltaTime * introSpeed);

            // [중요] 이동 중 Y축 변화에 대응하여 목표 타겟 갱신 (무한루프 방지)
            targetPos.y = playerTransform.position.y;

            yield return null;
        }

        // 3. 위치 보정
        playerTransform.position = new Vector3(-standDistance, playerTransform.position.y, 0);
        aiTransform.position = new Vector3(standDistance, aiTransform.position.y, 0);

        // 4. 게임 시작
        Debug.Log("FIGHT!");
        currentState = GameState.Playing;
    }

    void Update()
    {
        if (currentState != GameState.Playing) return;

        // 패배 체크
        if (Mathf.Abs(playerTransform.position.x) >= fallLineX)
        {
            GameOver(false); // 플레이어 패배
        }
        else if (Mathf.Abs(aiTransform.position.x) >= fallLineX)
        {
            GameOver(true); // AI 패배 (플레이어 승리)
        }
    }

    void GameOver(bool playerWin)
    {
        currentState = GameState.GameOver;
        UIManager.Instance.ShowResult(playerWin);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ★ [추가됨] 전투 상황을 영어 텍스트로 반환하는 함수 ★
    public string GetBattleStatusText()
    {
        float playerDist = Mathf.Abs(playerTransform.position.x);
        float aiDist = Mathf.Abs(aiTransform.position.x);
        float dangerZone = fallLineX * 0.7f; // 70% 이상 밀리면 위험구역

        // 1. 내가 위험할 때
        if (playerDist > dangerZone) return "DANGER!!";

        // 2. 적이 위험할 때 (마무리 기회)
        if (aiDist > dangerZone) return "FINISH HIM!";

        // 3. 중앙 힘싸움 단계
        if (Mathf.Abs(playerDist - aiDist) < 1.0f) return "EQUAL";
        else if (aiDist > playerDist) return "ADVANTAGE"; // 내가 미세하게 우세
        else return "DEFENSE!"; // 내가 미세하게 밀림
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(-fallLineX, -5, 0), new Vector3(-fallLineX, 5, 0));
        Gizmos.DrawLine(new Vector3(fallLineX, -5, 0), new Vector3(fallLineX, 5, 0));

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(new Vector3(-standDistance, 0, 0), 0.5f);
        Gizmos.DrawWireSphere(new Vector3(standDistance, 0, 0), 0.5f);
    }
}