using UnityEngine;
using UnityEngine.UI;

public class AutoSoundBinder : MonoBehaviour
{
    void Start()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        Debug.Log($"[Binder] 발견된 버튼 개수: {buttons.Length}"); // ★ 버튼을 찾긴 했는지 확인

        foreach (Button btn in buttons)
        {
            if (btn.GetComponent<NoClickSound>() != null) continue;

            // ★ 람다식 안에 로그 추가
            btn.onClick.AddListener(() => {
                Debug.Log($"[Binder] 버튼 눌림: {btn.gameObject.name}"); // ★ 버튼 누를 때 이게 뜨나 확인
                SoundManager.Instance.PlaySFX(SFX.Click);
            });
        }
    }
}
