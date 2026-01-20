using UnityEngine;

public class SumoBody : MonoBehaviour
{
    [Header("References")]
    public SumoBody opponent; // 상대방 몸체

    [Header("Visual Settings")]
    public SpriteRenderer bodyRenderer; // 캐릭터의 SpriteRenderer 연결
    public CharacterDatabase characterDB; // ★ 스크립터블 오브젝트 연결

    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float tileSize = 1.5f;

    // 외부(Controller)에서 내 상태를 알기 위한 변수
    public bool IsMoving { get; private set; }
    private Vector3 targetPosition;

    private void Awake()
    {
        // 렌더러가 연결 안 되어 있으면 자동으로 찾기
        if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        targetPosition = transform.position;

        // ★ [추가] 태그를 확인해서 스킨 적용하기
        if (characterDB != null)
        {
            if (gameObject.CompareTag("Player"))
            {
                // 플레이어라면: 저장된(선택한) 캐릭터 번호 불러오기
                int mySkinId = PlayerPrefs.GetInt("MyCharacterID", 0);
                ChangeSkin(mySkinId);
            }
        }
    }

    // ★ [추가] 실제 이미지를 DB에서 가져와 교체하는 함수
    public void ChangeSkin(int index)
    {
        if (characterDB != null && bodyRenderer != null)
        {
            bodyRenderer.sprite = characterDB.GetSkin(index);
        }
    }

    private void Update()
    {
        // 싱글 게임 매니저가 있다면 인트로 상태 체크 (없으면 그냥 패스)
        if (GameManager.Instance != null && GameManager.Instance.currentState == GameManager.GameState.Intro)
        {
            targetPosition = transform.position;
            return;
        }

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);

        if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            transform.position = targetPosition;
            IsMoving = false;
        }
        else
        {
            IsMoving = true;
        }
    }

    // 힘(powerMultiplier)을 받아서 공격
    public void PushOpponent(float powerMultiplier)
    {
        // 1. 나는 힘만큼 전진
        MoveStep(1, powerMultiplier);

        // 2. 상대방은 밀려남
        if (opponent != null)
        {
            opponent.GetPushed(powerMultiplier);
        }
    }

    // 배율 적용
    public void MoveStep(int steps, float multiplier)
    {
        // 상대방이 없으면 그냥 앞(오른쪽/왼쪽) 판별이 어려우므로 예외처리 가능
        if (opponent == null) return;

        float direction = Mathf.Sign(opponent.transform.position.x - transform.position.x);

        // 이동 거리 = 기본크기 * 배율
        float distance = tileSize * multiplier;

        targetPosition += new Vector3(direction * steps * distance, 0, 0);
        IsMoving = true;
    }

    // 밀려날 때도 배율 적용
    public void GetPushed(float multiplier)
    {
        if (opponent == null) return;

        float direction = Mathf.Sign(transform.position.x - opponent.transform.position.x);

        // 강하게 맞으면 더 멀리 밀려남
        float distance = tileSize * multiplier;

        targetPosition += new Vector3(direction * distance, 0, 0);
        IsMoving = true;
    }
}