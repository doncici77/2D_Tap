using UnityEngine;

// 우클릭 > Create 메뉴에 이 항목이 생깁니다.
[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Scriptable Objects/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    [Header("Character List")]
    // 나중에 이름이나 능력치도 넣고 싶으면 구조체(Struct)나 클래스로 바꿔도 됩니다.
    // 지금은 심플하게 이미지만 관리합시다.
    public Sprite[] skins;

    // 인덱스로 스프라이트를 안전하게 가져오는 헬퍼 함수
    public Sprite GetSkin(int index)
    {
        if (skins == null || skins.Length == 0) return null;

        // 범위 벗어나면 0번이나 null 리턴
        if (index < 0 || index >= skins.Length) return skins[0];

        return skins[index];
    }

    // 전체 개수 반환
    public int Count => skins != null ? skins.Length : 0;
}