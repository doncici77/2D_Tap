using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;

public class NetGameManager : NetworkBehaviour
{
    public static NetGameManager Instance;

    public enum GameState { Intro, Playing, GameOver }

    public NetworkVariable<GameState> currentNetState = new NetworkVariable<GameState>(
        GameState.Intro, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Game Settings")]
    public float fallLineX = 8.0f;
    public string titleSceneName = "TitleScene";

    [Header("Intro Settings")]
    public float startDistance = 8f;
    public float standDistance = 1.5f;
    public float introSpeed = 5f;

    public NetSumoBody player1;
    public NetSumoBody player2;

    private bool p1Rematch = false;
    private bool p2Rematch = false;

    private void Awake()
    {
        // 싱글톤 재설정 (씬이 다시 로드되면 새로운 Instance가 되어야 함)
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    // ★ [중요] 씬이 로드되고 네트워크가 준비되면 변수를 깨끗하게 초기화
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            player1 = null;
            player2 = null;
            p1Rematch = false;
            p2Rematch = false;
            currentNetState.Value = GameState.Intro;
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        // ★ [안전장치] 혹시 플레이어가 태어났는데 등록이 안 됐을까봐 매 프레임 검사 (Race Condition 방지)
        if (player1 == null || player2 == null)
        {
            FindAndRegisterPlayers();
        }

        // 게임 중일 때만 승패 판정
        if (currentNetState.Value == GameState.Playing && player1 != null && player2 != null)
        {
            if (player1.transform.position.x <= -fallLineX) EndGame(player2.OwnerClientId);
            else if (player2.transform.position.x >= fallLineX) EndGame(player1.OwnerClientId);
        }
    }

    // ★ [추가] 놓친 플레이어 찾기 (재시작 시 타이밍 이슈 해결용)
    private void FindAndRegisterPlayers()
    {
        // 씬에 있는 모든 NetSumoBody를 찾음
        NetSumoBody[] bodies = FindObjectsOfType<NetSumoBody>();

        foreach (var body in bodies)
        {
            // 아직 등록되지 않은 플레이어라면 등록 시도
            if (body != player1 && body != player2)
            {
                RegisterPlayer(body);
            }
        }
    }

    public void RegisterPlayer(NetSumoBody newBody)
    {
        // 이미 등록된 애면 무시
        if (newBody == player1 || newBody == player2) return;

        // 1. Player 1 등록 (호스트)
        if (player1 == null)
        {
            // 보통 호스트가 먼저 생성되지만, 확실히 하기 위해 OwnerClientId로 체크해도 좋음
            player1 = newBody;
            newBody.name = "Player 1 (Host)";
            // 위치 강제 설정 (재시작 시 중요)
            newBody.transform.position = new Vector3(-startDistance, 0, 0);
            newBody.netTargetPos.Value = new Vector3(-startDistance, 0, 0);
        }
        // 2. Player 2 등록 (클라이언트)
        else if (player2 == null)
        {
            player2 = newBody;
            newBody.name = "Player 2 (Client)";
            newBody.transform.position = new Vector3(startDistance, 0, 0);
            newBody.netTargetPos.Value = new Vector3(startDistance, 0, 0);

            // 서로 연결
            player1.opponent = player2;
            player2.opponent = player1;

            Debug.Log("=== 두 명 입장 완료! 인트로 시작 ===");
            StartCoroutine(ServerIntroSequence());
        }
    }

    IEnumerator ServerIntroSequence()
    {
        // 재시작 변수 확실히 초기화
        p1Rematch = false;
        p2Rematch = false;

        // 상태 초기화
        currentNetState.Value = GameState.Intro;

        yield return new WaitForSeconds(1.0f);

        Vector3 p1Target = new Vector3(-standDistance, 0, 0);
        Vector3 p2Target = new Vector3(standDistance, 0, 0);

        // 플레이어가 없을 경우를 대비한 null 체크 추가
        if (player1 != null && player2 != null)
        {
            while (Vector3.Distance(player1.netTargetPos.Value, p1Target) > 0.1f)
            {
                if (player1 == null || player2 == null) yield break; // 도중 나감 방지

                player1.netTargetPos.Value = Vector3.MoveTowards(player1.netTargetPos.Value, p1Target, Time.deltaTime * introSpeed);
                player2.netTargetPos.Value = Vector3.MoveTowards(player2.netTargetPos.Value, p2Target, Time.deltaTime * introSpeed);
                yield return null;
            }

            player1.netTargetPos.Value = p1Target;
            player2.netTargetPos.Value = p2Target;
        }

        yield return new WaitForSeconds(0.5f);
        currentNetState.Value = GameState.Playing;
    }

    private void EndGame(ulong winnerId)
    {
        // 중복 호출 방지
        if (currentNetState.Value == GameState.GameOver) return;

        currentNetState.Value = GameState.GameOver;
        GameOverClientRpc(winnerId);
    }

    [ClientRpc]
    private void GameOverClientRpc(ulong winnerId)
    {
        bool amIWinner = (NetworkManager.Singleton.LocalClientId == winnerId);
        if (UIManager.Instance != null) UIManager.Instance.ShowResult(amIWinner);
    }

    public void PlayerRequestRematch(ulong clientId)
    {
        // 서버가 아니면 실행 불가 (안전장치)
        if (!IsServer) return;

        // 누가 보냈는지 확인
        if (player1 != null && clientId == player1.OwnerClientId) p1Rematch = true;
        if (player2 != null && clientId == player2.OwnerClientId) p2Rematch = true;

        Debug.Log($"재시작 투표 현황: P1({p1Rematch}) / P2({p2Rematch})");

        // 둘 다 투표했으면 재시작
        if (p1Rematch && p2Rematch)
        {
            RestartGame();
        }
    }

    public void RestartGame()
    {
        if (IsServer)
        {
            // 씬을 다시 로드하면 NetworkManager가 자동으로 플레이어를 다시 스폰함
            NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }
    }

    public void GoToTitle()
    {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene(titleSceneName);
    }

    public string GetBattleStatusText()
    {
        if (player1 == null || player2 == null) return "WAITING...";

        ulong myId = NetworkManager.Singleton.LocalClientId;
        NetSumoBody me = (player1.OwnerClientId == myId) ? player1 : player2;
        NetSumoBody enemy = (me == player1) ? player2 : player1;

        if (me == null || enemy == null) return "";

        float myDist = Mathf.Abs(me.transform.position.x);
        float enemyDist = Mathf.Abs(enemy.transform.position.x);
        float dangerZone = fallLineX * 0.7f;

        if (myDist > dangerZone) return "DANGER!!";
        if (enemyDist > dangerZone) return "FINISH HIM!";
        if (Mathf.Abs(myDist - enemyDist) < 1.0f) return "EQUAL";
        else if (enemyDist > myDist) return "ADVANTAGE";
        else return "DEFENSE!";
    }
}