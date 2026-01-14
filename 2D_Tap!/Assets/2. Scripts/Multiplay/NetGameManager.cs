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

    // 일반 변수 사용
    public NetSumoBody player1;
    public NetSumoBody player2;

    private bool p1Rematch = false;
    private bool p2Rematch = false;

    private void Awake()
    {
        // 씬 전환 시 중복 방지
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // 변수 강제 초기화
            player1 = null;
            player2 = null;
            p1Rematch = false;
            p2Rematch = false;
            currentNetState.Value = GameState.Intro;

            // ★ [핵심] 1초 뒤부터 탐색 시작 (로딩 시간 벌어주기)
            StartCoroutine(FindPlayersRoutine());
        }
    }

    // ★ [좀비 탐색기] 두 명이 다 구해질 때까지 0.5초마다 계속 찾음
    IEnumerator FindPlayersRoutine()
    {
        Debug.Log("[GM] 플레이어 탐색 시작...");

        while (player1 == null || player2 == null)
        {
            // 씬에 있는 모든 스모 선수 찾기
            NetSumoBody[] bodies = FindObjectsOfType<NetSumoBody>();

            foreach (var body in bodies)
            {
                RegisterPlayer(body);
            }

            // 아직 다 못 찾았으면 로그 띄우고 0.5초 대기
            if (player1 == null || player2 == null)
            {
                // Debug.Log($"[GM] 찾는 중... 현재 발견된 몸체: {bodies.Length}개 / P1:{(player1!=null)} P2:{(player2!=null)}");
                yield return new WaitForSeconds(0.5f);
            }
        }

        // 반복문 탈출 = 두 명 다 찾음
        Debug.Log("[GM] 두 명 모두 등록 완료! 인트로 시퀀스 진입.");

        // 인트로 시작
        StartCoroutine(ServerIntroSequence());
    }

    public void RegisterPlayer(NetSumoBody newBody)
    {
        if (newBody == player1 || newBody == player2) return;

        // 주인 확인해서 등록
        if (newBody.IsOwnedByServer)
        {
            player1 = newBody;
            newBody.name = "Player 1 (Host)";
            ResetPos(newBody, -startDistance);
        }
        else
        {
            player2 = newBody;
            newBody.name = "Player 2 (Client)";
            ResetPos(newBody, startDistance);
        }
    }

    void ResetPos(NetSumoBody body, float xPos)
    {
        // 물리적 위치와 네트워크 목표 위치 동시 설정
        body.transform.position = new Vector3(xPos, 0, 0);
        body.netTargetPos.Value = new Vector3(xPos, 0, 0);
    }

    IEnumerator ServerIntroSequence()
    {
        // 상태 확실히 Intro로 고정
        currentNetState.Value = GameState.Intro;

        // 서로 연결 (중요)
        player1.opponent = player2;
        player2.opponent = player1;

        yield return new WaitForSeconds(1.0f);

        Vector3 p1Target = new Vector3(-standDistance, 0, 0);
        Vector3 p2Target = new Vector3(standDistance, 0, 0);

        // 이동 연출
        while (Vector3.Distance(player1.netTargetPos.Value, p1Target) > 0.1f)
        {
            if (player1 == null || player2 == null) yield break; // 중간에 나가면 중단

            player1.netTargetPos.Value = Vector3.MoveTowards(player1.netTargetPos.Value, p1Target, Time.deltaTime * introSpeed);
            player2.netTargetPos.Value = Vector3.MoveTowards(player2.netTargetPos.Value, p2Target, Time.deltaTime * introSpeed);
            yield return null;
        }

        // 위치 보정
        player1.netTargetPos.Value = p1Target;
        player2.netTargetPos.Value = p2Target;

        yield return new WaitForSeconds(0.5f);

        // 게임 시작!
        currentNetState.Value = GameState.Playing;
        Debug.Log("FIGHT!");
    }

    private void Update()
    {
        if (!IsServer) return;

        // 게임 중일 때만 승패 판정
        if (currentNetState.Value == GameState.Playing && player1 != null && player2 != null)
        {
            if (player1.transform.position.x <= -fallLineX) EndGame(player2.OwnerClientId);
            else if (player2.transform.position.x >= fallLineX) EndGame(player1.OwnerClientId);
        }
    }

    private void EndGame(ulong winnerId)
    {
        if (currentNetState.Value == GameState.GameOver) return;
        currentNetState.Value = GameState.GameOver;
        GameOverClientRpc(winnerId);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void GameOverClientRpc(ulong winnerId)
    {
        bool amIWinner = (NetworkManager.Singleton.LocalClientId == winnerId);
        if (UIManager.Instance != null) UIManager.Instance.ShowResult(amIWinner);
    }

    public void PlayerRequestRematch(ulong clientId)
    {
        if (!IsServer) return;

        if (player1 != null && clientId == player1.OwnerClientId) p1Rematch = true;
        if (player2 != null && clientId == player2.OwnerClientId) p2Rematch = true;

        Debug.Log($"재시작 투표: P1({p1Rematch}) / P2({p2Rematch})");

        if (p1Rematch && p2Rematch)
        {
            RestartGame();
        }
    }

    public void RestartGame()
    {
        if (IsServer)
        {
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
        // 클라이언트 동기화 보정
        if (player1 == null || player2 == null)
        {
            NetSumoBody[] bodies = FindObjectsOfType<NetSumoBody>();
            foreach (var body in bodies)
            {
                if (body.IsOwnedByServer) player1 = body;
                else player2 = body;
            }
        }

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