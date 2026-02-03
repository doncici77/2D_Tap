using UnityEngine;
using System.Collections;

public class PlayerController : BasePlayerController
{
    protected override void Start()
    {
        base.Start();
        
        // UI 연결 (이벤트 기반)
        OnComboChanged += (combo) => {
            if (SingleUIManager.Instance != null) SingleUIManager.Instance.UpdateComboText(combo);
        };
    }

    protected override bool CanPerformAction()
    {
        return GameManager.Instance != null && !GameManager.Instance.IsGameOver;
    }

    // 기존의 OnActionBtnPressed는 그대로 유지 (UI 하위 호환)
    public void OnActionBtnPressed()
    {
        TryAction();
    }
}