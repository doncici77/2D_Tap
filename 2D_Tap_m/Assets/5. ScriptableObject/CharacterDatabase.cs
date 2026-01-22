using UnityEngine;

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Scriptable Objects/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    // 1. 캐릭터 하나가 가질 정보들을 묶습니다.
    [System.Serializable]
    public struct CharacterData
    {
        public string name;           // 캐릭터 이름 (에디터 구별용)
        public Sprite skin;           // 스킨 이미지
        public AudioClip successClip; // 이 캐릭터만의 성공 사운드
        public AudioClip failClip;    // 이 캐릭터만의 실패 사운드
    }

    [Header("Character List")]
    // 2. 이제 스프라이트 배열이 아니라, 데이터 묶음의 배열입니다.
    public CharacterData[] characters;

    // ==========================================
    // 데이터 가져오는 헬퍼 함수들
    // ==========================================

    // 스킨 가져오기
    public Sprite GetSkin(int index)
    {
        if (IsValidIndex(index)) return characters[index].skin;
        return (characters != null && characters.Length > 0) ? characters[0].skin : null;
    }

    // 성공 사운드 가져오기
    public AudioClip GetSuccessSound(int index)
    {
        if (IsValidIndex(index)) return characters[index].successClip;
        return null; // 없으면 소리 안 남
    }

    // 실패 사운드 가져오기
    public AudioClip GetFailSound(int index)
    {
        if (IsValidIndex(index)) return characters[index].failClip;
        return null;
    }

    // 인덱스 유효성 검사 (중복 코드 제거)
    private bool IsValidIndex(int index)
    {
        return characters != null && index >= 0 && index < characters.Length;
    }

    public int Count => characters != null ? characters.Length : 0;
}