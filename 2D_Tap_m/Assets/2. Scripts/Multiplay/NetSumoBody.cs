using UnityEngine;
using Unity.Netcode;
using System.Collections; // ★ [추가] 코루틴(IEnumerator) 쓰려면 이게 꼭 필요합니다!

public class NetSumoBody : NetworkBehaviour
{
    [Header("References")]
    public NetSumoBody opponent;

    [Header("Visual Settings")]
    public SpriteRenderer bodyRenderer;
    public CharacterDatabase characterDB;

    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float tileSize = 1.5f;

    // ★ [추가] 쫀득한 애니메이션 설정값
    [Header("Animation Settings")]
    public float squashDuration = 0.15f; // 애니메이션 시간
    public Vector3 attackScale = new Vector3(1.3f, 0.8f, 1f); // 늘어날 크기 (X는 뚱뚱, Y는 납작)

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
        netSkinId.OnValueChanged += OnSkinIdChanged;
        UpdateSprite(netSkinId.Value);

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
    // 사운드 재생 함수
    // ============================================
    public void PlaySuccessSound()
    {
        AudioClip clip = (characterDB != null) ? characterDB.GetSuccessSound(netSkinId.Value) : null;
        if (clip != null) SoundManager.Instance.PlayDirectSFX(clip);
        else SoundManager.Instance.PlaySFX(SFX.Success);
    }

    public void PlayFailSound()
    {
        AudioClip clip = (characterDB != null) ? characterDB.GetFailSound(netSkinId.Value) : null;
        if (clip != null) SoundManager.Instance.PlayDirectSFX(clip);
        else SoundManager.Instance.PlaySFX(SFX.Fail);
    }

    // ============================================
    // ★ [추가] 쫀득한 공격 애니메이션 실행 함수
    // ============================================
    public void PlayAttackAnim()
    {
        // 이미 움직이고 있어도 강제로 멈추고 새로 시작 (반응성 향상)
        StopAllCoroutines();
        StartCoroutine(SquashRoutine());
    }

    private IEnumerator SquashRoutine()
    {
        // 렌더러가 붙은 오브젝트(보통 자기 자신)를 변형합니다.
        Transform targetTr = bodyRenderer.transform;
        Vector3 originalScale = Vector3.one; // 원래 크기 (1,1,1)

        float timer = 0f;

        // 1. 찌그러지기 (가는 과정)
        while (timer < squashDuration / 2)
        {
            timer += Time.deltaTime;
            float t = timer / (squashDuration / 2);
            targetTr.localScale = Vector3.Lerp(originalScale, attackScale, t);
            yield return null;
        }

        // 2. 돌아오기 (오는 과정)
        timer = 0f;
        while (timer < squashDuration / 2)
        {
            timer += Time.deltaTime;
            float t = timer / (squashDuration / 2);
            targetTr.localScale = Vector3.Lerp(attackScale, originalScale, t);
            yield return null;
        }

        // 3. 확실하게 원상복구
        targetTr.localScale = originalScale;
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
        if (opponent != null) opponent.GetPushedServer(powerMultiplier);
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