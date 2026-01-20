using UnityEngine;
using Unity.Netcode;

public class NetSumoBody : NetworkBehaviour
{
    [Header("References")]
    public NetSumoBody opponent;

    [Header("Visual Settings")]
    public SpriteRenderer bodyRenderer;
    public CharacterDatabase characterDB; // 스크립터블 오브젝트 연결 필수

    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float tileSize = 1.5f;

    // 스킨 ID 동기화 변수
    public NetworkVariable<int> netSkinId = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<Vector3> netTargetPos = new NetworkVariable<Vector3>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool IsMoving { get; private set; }

    private void Awake()
    {
        if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        // 1. 이벤트 구독
        netSkinId.OnValueChanged += OnSkinIdChanged;

        // 2. 초기화
        UpdateSprite(netSkinId.Value);

        // 3. 주인(Owner)인 경우 스킨 정보 전송
        if (IsOwner)
        {
            int mySavedId = PlayerPrefs.GetInt("MyCharacterID", 0);
            // ★ [수정] 이름도 Rpc로 통일하고 호출
            SubmitSkinRpc(mySavedId);
        }

        if (IsServer)
        {
            netTargetPos.Value = transform.position;
            if (NetGameManager.Instance != null) NetGameManager.Instance.RegisterPlayer(this);
        }
    }

    public override void OnNetworkDespawn()
    {
        netSkinId.OnValueChanged -= OnSkinIdChanged;
    }

    // ★ [핵심 수정] ServerRpc -> Rpc(SendTo.Server)
    // 이름도 'ServerRpc' 접미사 대신 그냥 'Rpc'로 끝내는 것이 최신 관례입니다.
    [Rpc(SendTo.Server)]
    private void SubmitSkinRpc(int skinId)
    {
        // 서버에서 실행됨
        netSkinId.Value = skinId;
    }

    private void OnSkinIdChanged(int oldVal, int newVal)
    {
        UpdateSprite(newVal);
    }

    private void UpdateSprite(int id)
    {
        if (characterDB != null && bodyRenderer != null)
        {
            bodyRenderer.sprite = characterDB.GetSkin(id);
        }
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, netTargetPos.Value, Time.deltaTime * moveSpeed);

        if (Vector3.Distance(transform.position, netTargetPos.Value) < 0.05f)
        {
            transform.position = netTargetPos.Value;
            IsMoving = false;
        }
        else IsMoving = true;
    }

    // --- 물리 로직 (이 함수들은 RPC가 아니라 서버 로직이므로 속성 불필요) ---

    public void PushOpponentServer(float powerMultiplier)
    {
        MoveStepServer(1, powerMultiplier);
        if (opponent != null)
        {
            opponent.GetPushedServer(powerMultiplier);
        }
    }

    public void MoveStepServer(int directionSign, float multiplier)
    {
        if (opponent == null) return;

        float direction = Mathf.Sign(opponent.transform.position.x - transform.position.x);
        float distance = tileSize * multiplier;

        netTargetPos.Value += new Vector3(direction * directionSign * distance, 0, 0);
        IsMoving = true;
    }

    public void GetPushedServer(float multiplier)
    {
        if (opponent == null) return;

        float direction = Mathf.Sign(transform.position.x - opponent.transform.position.x);
        float distance = tileSize * multiplier;

        netTargetPos.Value += new Vector3(direction * distance, 0, 0);
        IsMoving = true;
    }
}