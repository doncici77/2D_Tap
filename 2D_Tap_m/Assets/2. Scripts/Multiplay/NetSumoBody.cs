using UnityEngine;
using Unity.Netcode;

public class NetSumoBody : NetworkBehaviour
{
    [Header("References")]
    public NetSumoBody opponent;

    [Header("Visual Settings")]
    public SpriteRenderer bodyRenderer;
    public CharacterDatabase characterDB; // ★ DB 연결 필수!

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

        // 2. 초기화 (이미 들어있는 값으로 업데이트)
        UpdateSprite(netSkinId.Value);

        // 3. 주인(Owner)인 경우 스킨 정보 전송
        if (IsOwner)
        {
            int mySavedId = PlayerPrefs.GetInt("MyCharacterID", 0);
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

    [Rpc(SendTo.Server)]
    private void SubmitSkinRpc(int skinId)
    {
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

    // ============================================
    // ★ [추가] 사운드 재생 함수 (로컬에서만 들리면 됨)
    // ============================================
    public void PlaySuccessSound()
    {
        // 현재 내 스킨 ID (netSkinId.Value)에 맞는 소리 재생
        AudioClip clip = (characterDB != null) ? characterDB.GetSuccessSound(netSkinId.Value) : null;

        if (clip != null)
            SoundManager.Instance.PlayDirectSFX(clip);
        else
            SoundManager.Instance.PlaySFX(SFX.Success);
    }

    public void PlayFailSound()
    {
        AudioClip clip = (characterDB != null) ? characterDB.GetFailSound(netSkinId.Value) : null;

        if (clip != null)
            SoundManager.Instance.PlayDirectSFX(clip);
        else
            SoundManager.Instance.PlaySFX(SFX.Fail);
    }

    // ============================================

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

    // --- 물리 로직 ---

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