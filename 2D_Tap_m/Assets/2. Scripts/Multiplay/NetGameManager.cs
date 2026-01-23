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

    public NetworkVariable<int> p1Score = new NetworkVariable<int>(0);
    public NetworkVariable<int> p2Score = new NetworkVariable<int>(0);

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
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        // ★ [추가] 누가 나갔는지 감시하는 이벤트 연결
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

        if (IsServer)
        {
            player1 = null;
            player2 = null;
            p1Rematch = false;
            p2Rematch = false;
            currentNetState.Value = GameState.Intro;

            StartCoroutine(FindPlayersRoutine());
        }
    }

    // ★ [추가] 이벤트 연결 해제 (메모리 누수 방지)
    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }

    // ★★★ [핵심] 누군가 연결이 끊기면 호출되는 함수 ★★★
    private void OnClientDisconnect(ulong clientId)
    {
        // 1. 내가 서버(호스트)인 경우: 상대방(클라이언트)이 나갔는지 체크
        if (IsServer)
        {
            // 게임 도중(Playing)인데 누군가 나갔다면?
            if (currentNetState.Value == GameState.Playing)
            {
                // 나간 사람이 Player 2라면 -> Player 1(나)의 승리
                if (player2 != null && clientId == player2.OwnerClientId)
                {
                    Debug.Log(">>> 상대방(Player 2) 탈주! Player 1 부전승!");
                    EndGame(player1.OwnerClientId);
                }
                // 나간 사람이 Player 1이라면 -> Player 2 승리 (보통 호스트가 나가면 방이 터지지만 예외처리)
                else if (player1 != null && clientId == player1.OwnerClientId)
                {
                    Debug.Log(">>> Player 1 탈주! Player 2 부전승!");
                    EndGame(player2.OwnerClientId);
                }
            }
            else if (currentNetState.Value == GameState.GameOver)
            {
                // 남은 사람에게 "상대방이 나갔습니다" 알림 후 타이틀로 보내기
                // 혹은 "혼자 남았습니다" UI 띄우기
                Debug.Log("결과창에서 상대방 탈주! 방을 깹니다.");
                ShutdownRoomClientRpc(); // 모두 타이틀로 보냄
            }
        }
        // 2. 내가 클라이언트인 경우: 호스트(서버)가 나갔는지 체크
        else
        {
            // 서버(ID: 0)가 연결을 끊었다면 -> 방이 폭파된 것임
            if (clientId == NetworkManager.ServerClientId)
            {
                Debug.Log(">>> 호스트 연결 끊김. 타이틀로 이동.");
                GoToTitle(); // 타이틀 화면으로 강제 이동
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ShutdownRoomClientRpc()
    {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene(titleSceneName);
    }

    IEnumerator FindPlayersRoutine()
    {
        Debug.Log("[GM] 플레이어 탐색 시작...");

        while (player1 == null || player2 == null)
        {
            NetSumoBody[] bodies = FindObjectsByType<NetSumoBody>(FindObjectsSortMode.None);

            foreach (var body in bodies)
            {
                RegisterPlayer(body);
            }

            if (player1 == null || player2 == null)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        Debug.Log("[GM] 두 명 모두 등록 완료! 인트로 시퀀스 진입.");
        StartCoroutine(ServerIntroSequence());
    }

    public void RegisterPlayer(NetSumoBody newBody)
    {
        if (newBody == player1 || newBody == player2) return;

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
        body.transform.position = new Vector3(xPos, 0, 0);
        body.netTargetPos.Value = new Vector3(xPos, 0, 0);
    }

    IEnumerator ServerIntroSequence()
    {
        currentNetState.Value = GameState.Intro;

        player1.opponent = player2;
        player2.opponent = player1;

        yield return new WaitForSeconds(1.0f);

        Vector3 p1Target = new Vector3(-standDistance, 0, 0);
        Vector3 p2Target = new Vector3(standDistance, 0, 0);

        while (Vector3.Distance(player1.netTargetPos.Value, p1Target) > 0.1f)
        {
            if (player1 == null || player2 == null) yield break;

            player1.netTargetPos.Value = Vector3.MoveTowards(player1.netTargetPos.Value, p1Target, Time.deltaTime * introSpeed);
            player2.netTargetPos.Value = Vector3.MoveTowards(player2.netTargetPos.Value, p2Target, Time.deltaTime * introSpeed);
            yield return null;
        }

        player1.netTargetPos.Value = p1Target;
        player2.netTargetPos.Value = p2Target;

        yield return new WaitForSeconds(0.5f);

        currentNetState.Value = GameState.Playing;
        Debug.Log("FIGHT!");
    }

    private void Update()
    {
        if (!IsServer) return;

        if (currentNetState.Value == GameState.Playing && player1 != null && player2 != null)
        {
            if (player1.transform.position.x <= -fallLineX) EndGame(player2.OwnerClientId);
            else if (player2.transform.position.x >= fallLineX) EndGame(player1.OwnerClientId);
        }
    }

    // ★ [수정] 탈주 시에도 호출되어야 하므로 public으로 두거나 내부 호출
    private void EndGame(ulong winnerId)
    {
        if (currentNetState.Value == GameState.GameOver) return;
        currentNetState.Value = GameState.GameOver;
        GameOverClientRpc(winnerId);

        if (winnerId == player1.OwnerClientId) p1Score.Value++;
        else p2Score.Value++;

        // UI에 점수 표시하라고 Rpc 보내기
        UpdateScoreClientRpc(p1Score.Value, p2Score.Value);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void GameOverClientRpc(ulong winnerId)
    {
        bool amIWinner = (NetworkManager.Singleton.LocalClientId == winnerId);
        if (MultiUIManager.Instance != null)
        {
            // 결과창 띄우기
            MultiUIManager.Instance.ShowResult(amIWinner);

            // 만약 승리했는데 이유가 '탈주'라면 텍스트를 좀 바꿔주면 좋음 (선택사항)
            // 지금은 그냥 심플하게 승/패만 띄웁니다.
        }
    }

    public void PlayerRequestRematch(ulong clientId)
    {
        if (!IsServer) return;

        if (player1 != null && clientId == player1.OwnerClientId) p1Rematch = true;
        if (player2 != null && clientId == player2.OwnerClientId) p2Rematch = true;

        // ★ 추가: 누가 눌렀는지 모두에게 알려줌 (UI 갱신용)
        UpdateRematchStateClientRpc(p1Rematch, p2Rematch);

        if (p1Rematch && p2Rematch)
        {
            // 잠시 대기 후 시작 (바로 시작하면 UI 볼 틈이 없음)
            StartCoroutine(DelayedRestart());
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateRematchStateClientRpc(bool p1Ready, bool p2Ready)
    {
        if (MultiUIManager.Instance != null)
        {
            MultiUIManager.Instance.UpdateRematchUI(p1Ready, p2Ready);
        }
    }

    IEnumerator DelayedRestart()
    {
        yield return new WaitForSeconds(1.0f); // 1초 뒤 시작
        RestartGame();
    }

    public void RestartGame()
    {
        if (!IsServer) return;

        Debug.Log("[GM] 소프트 리셋 실행");

        p1Rematch = false;
        p2Rematch = false;

        // 플레이어 상태 리셋
        if (player1 != null && player1.GetComponent<NetPlayerController>() != null)
            player1.GetComponent<NetPlayerController>().ResetPlayerStateRpc();

        if (player2 != null && player2.GetComponent<NetPlayerController>() != null)
            player2.GetComponent<NetPlayerController>().ResetPlayerStateRpc();

        // 위치 리셋
        if (player1 != null) ResetPos(player1, -startDistance);
        if (player2 != null) ResetPos(player2, startDistance);

        ResetUIClientRpc();
        StopAllCoroutines();
        StartCoroutine(ServerIntroSequence());
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ResetUIClientRpc()
    {
        if (MultiUIManager.Instance != null)
        {
            MultiUIManager.Instance.HideResult();
        }
    }

    public void GoToTitle()
    {
        // 네트워크 연결 종료 후 씬 이동
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene(titleSceneName);
    }

    public string GetBattleStatusKey()
    {
        if (player1 == null || player2 == null)
        {
            NetSumoBody[] bodies = FindObjectsByType<NetSumoBody>(FindObjectsSortMode.None);
            foreach (var body in bodies)
            {
                if (body.IsOwnedByServer) player1 = body;
                else player2 = body;
            }
        }

        if (player1 == null || player2 == null) return "status_waiting"; // WAITING...

        ulong myId = NetworkManager.Singleton.LocalClientId;
        NetSumoBody me = (player1.OwnerClientId == myId) ? player1 : player2;
        NetSumoBody enemy = (me == player1) ? player2 : player1;

        if (me == null || enemy == null) return "";

        float myDist = Mathf.Abs(me.transform.position.x);
        float enemyDist = Mathf.Abs(enemy.transform.position.x);
        float dangerZone = fallLineX * 0.7f;

        if (myDist > dangerZone) return "status_danger";    // DANGER!!
        if (enemyDist > dangerZone) return "status_finish"; // FINISH HIM!
        if (Mathf.Abs(myDist - enemyDist) < 1.0f) return "status_equal"; // EQUAL
        else if (enemyDist > myDist) return "status_advantage"; // ADVANTAGE
        else return "status_defense"; // DEFENSE!
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateScoreClientRpc(int s1, int s2)
    {
        if (MultiUIManager.Instance != null)
        {
            MultiUIManager.Instance.UpdateScoreUI(s1, s2);
        }
    }
}