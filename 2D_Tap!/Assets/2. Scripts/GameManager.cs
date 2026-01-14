using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum GameState { Intro, Playing, GameOver }
    public GameState currentState = GameState.Intro;
    public bool IsGameOver => currentState == GameState.GameOver;

    [Header("Round Settings")]
    public int currentRound = 1; // 현재 라운드

    [Header("Scene Settings")]
    public string titleSceneName = "Title";

    [Header("References")]
    public Transform playerTransform;
    public Transform aiTransform;

    // ★ [추가] 컨트롤러에 직접 명령을 내리기 위해 참조 추가
    public PlayerController playerController;
    public AIController aiController;

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
        currentRound = 1;
        StartCoroutine(IntroSequence());
    }

    // 게임 시작 및 라운드 재시작 시 호출되는 연출
    IEnumerator IntroSequence()
    {
        currentState = GameState.Intro;

        // ★ [추가] 라운드 시작 알림
        Debug.Log($"=== Round {currentRound} Start! ===");
        if (UIManager.Instance != null) UIManager.Instance.UpdateRoundText(currentRound);

        // 1. 위치 초기화
        playerTransform.position = new Vector3(-startDistance, playerTransform.position.y, 0);
        aiTransform.position = new Vector3(startDistance, aiTransform.position.y, 0);

        yield return new WaitForSeconds(0.5f);

        // 2. 중앙으로 이동
        Vector3 targetPos = new Vector3(-standDistance, playerTransform.position.y, 0);

        while (Vector3.Distance(playerTransform.position, targetPos) > 0.1f)
        {
            playerTransform.position = Vector3.MoveTowards(playerTransform.position,
                new Vector3(-standDistance, playerTransform.position.y, 0), Time.deltaTime * introSpeed);

            aiTransform.position = Vector3.MoveTowards(aiTransform.position,
                new Vector3(standDistance, aiTransform.position.y, 0), Time.deltaTime * introSpeed);

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
            GameOver(false); // 플레이어 낙사 (패배)
        }
        else if (Mathf.Abs(aiTransform.position.x) >= fallLineX)
        {
            GameOver(true); // AI 낙사 (승리)
        }
    }

    void GameOver(bool playerWin)
    {
        if (playerWin)
        {
            // ★ [승리] 다음 라운드 준비
            Debug.Log("Player Win! Next Round...");
            StartCoroutine(PrepareNextRound());
        }
        else
        {
            // ★ [패배] 진짜 게임 오버
            currentState = GameState.GameOver;
            UIManager.Instance.ShowResult(false); // 패배 UI 표시
        }
    }

    // ★ [추가] 다음 라운드로 넘어가는 로직
    IEnumerator PrepareNextRound()
    {
        currentState = GameState.GameOver; // 잠시 조작 불가
        UIManager.Instance.ShowResult(true);

        // 승리 메시지 잠깐 보여주기 (선택사항)
        yield return new WaitForSeconds(2.0f);

        // 1. 라운드 수 증가
        currentRound++;

        // 2. 플레이어 스탯 리셋 (초기화)
        if (playerController != null) playerController.ResetDifficulty();

        // 3. AI 난이도 강화 (생각하는 시간 단축)
        if (aiController != null) aiController.LevelUpAI();

        UIManager.Instance.HideResult();

        // 4. 인트로 다시 시작 (위치 리셋 포함됨)
        StartCoroutine(IntroSequence());
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToTitle()
    {
        SceneManager.LoadScene(titleSceneName);
    }

    public string GetBattleStatusText()
    {
        float playerDist = Mathf.Abs(playerTransform.position.x);
        float aiDist = Mathf.Abs(aiTransform.position.x);
        float dangerZone = fallLineX * 0.7f;

        if (playerDist > dangerZone) return "DANGER!!";
        if (aiDist > dangerZone) return "FINISH HIM!";
        if (Mathf.Abs(playerDist - aiDist) < 1.0f) return "EQUAL";
        else if (aiDist > playerDist) return "ADVANTAGE";
        else return "DEFENSE!";
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(-fallLineX, -5, 0), new Vector3(-fallLineX, 5, 0));
        Gizmos.DrawLine(new Vector3(fallLineX, -5, 0), new Vector3(fallLineX, 5, 0));
    }
}