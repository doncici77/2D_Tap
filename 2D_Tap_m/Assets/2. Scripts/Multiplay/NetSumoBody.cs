using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class NetSumoBody : NetworkBehaviour, IBody
{
    [Header("References")]
    public NetSumoBody opponent;

    [Header("Visual Settings")]
    public SpriteRenderer bodyRenderer;
    public CharacterDatabase characterDB;

    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float tileSize = 1.5f;

    [Header("Animation Settings")]
    public float squashDuration = 0.15f;
    public Vector3 attackScale = new Vector3(1.3f, 0.8f, 1f);

    public NetworkVariable<Vector3> netTargetPos = new NetworkVariable<Vector3>(
        Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private int currentSkinIndex = 0;
    private Coroutine attackRoutine;

    private void Awake()
    {
        if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            currentSkinIndex = PlayerPrefs.GetInt("MyCharacterID", 0);
            ChangeSkinServerRpc(currentSkinIndex);
        }
    }

    [ServerRpc]
    private void ChangeSkinServerRpc(int index)
    {
        currentSkinIndex = index;
        ChangeSkinClientRpc(index);
    }

    [ClientRpc]
    private void ChangeSkinClientRpc(int index)
    {
        if (characterDB != null && bodyRenderer != null)
        {
            bodyRenderer.sprite = characterDB.GetSkin(index);
        }
    }

    public void PlaySuccessSound()
    {
        PlaySuccessSoundClientRpc();
    }

    [ClientRpc]
    private void PlaySuccessSoundClientRpc()
    {
        AudioClip clip = (characterDB != null) ? characterDB.GetSuccessSound(currentSkinIndex) : null;
        if (clip != null) SoundManager.Instance.PlayDirectSFX(clip);
        else SoundManager.Instance.PlaySFX(SFX.Success);
    }

    public void PlayFailSound()
    {
        PlayFailSoundClientRpc();
    }

    [ClientRpc]
    private void PlayFailSoundClientRpc()
    {
        AudioClip clip = (characterDB != null) ? characterDB.GetFailSound(currentSkinIndex) : null;
        if (clip != null) SoundManager.Instance.PlayDirectSFX(clip);
        else SoundManager.Instance.PlaySFX(SFX.Fail);
    }

    public void PlayAttackAnim()
    {
        PlayAttackAnimClientRpc();
    }

    [ClientRpc]
    private void PlayAttackAnimClientRpc()
    {
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        attackRoutine = StartCoroutine(SquashRoutine());
    }

    private IEnumerator SquashRoutine()
    {
        Transform targetTr = bodyRenderer.transform;
        Vector3 originalScale = transform.localScale;
        float timer = 0f;

        while (timer < squashDuration / 2)
        {
            timer += Time.deltaTime;
            float t = timer / (squashDuration / 2);
            targetTr.localScale = Vector3.Lerp(originalScale, attackScale, t);
            yield return null;
        }

        timer = 0f;
        while (timer < squashDuration / 2)
        {
            timer += Time.deltaTime;
            float t = timer / (squashDuration / 2);
            targetTr.localScale = Vector3.Lerp(attackScale, originalScale, t);
            yield return null;
        }

        targetTr.localScale = originalScale;
        attackRoutine = null;
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, netTargetPos.Value, Time.deltaTime * moveSpeed);
    }

    public void PushOpponent(float powerMultiplier)
    {
        if (IsServer)
        {
            PushOpponentServer(powerMultiplier);
        }
    }

    public void PushOpponentServer(float powerMultiplier)
    {
        if (!IsServer) return;
        
        MoveStep(1, powerMultiplier);
        if (opponent != null) opponent.GetPushed(powerMultiplier);
    }

    public void MoveStep(int steps, float multiplier)
    {
        if (!IsServer || opponent == null) return;
        float direction = Mathf.Sign(opponent.transform.position.x - transform.position.x);
        float distance = tileSize * multiplier;
        netTargetPos.Value += new Vector3(direction * steps * distance, 0, 0);
    }

    public void GetPushed(float multiplier)
    {
        if (!IsServer || opponent == null) return;
        float direction = Mathf.Sign(transform.position.x - opponent.transform.position.x);
        float distance = tileSize * multiplier;
        netTargetPos.Value += new Vector3(direction * distance, 0, 0);
    }
}
