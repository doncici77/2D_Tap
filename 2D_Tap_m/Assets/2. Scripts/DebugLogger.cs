using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DebugLogger : MonoBehaviour
{
    [Header("UI References")]
    public Text logText; // 로그가 찍힐 텍스트
    public ScrollRect scrollRect;   // 스크롤 뷰 (로그가 길어지면 내려보게)

    // 스크립트가 켜질 때 로그 수집 시작
    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    // 꺼질 때 중단
    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (logText == null) return;

        // 색상 구분: 에러는 빨간색, 경고는 노란색, 일반은 흰색
        string color = "white";
        if (type == LogType.Error || type == LogType.Exception) color = "red";
        else if (type == LogType.Warning) color = "yellow";

        // 로그 메시지 조립
        string newLog = $"<color={color}>[{System.DateTime.Now:HH:mm:ss}] {logString}</color>\n";

        // 에러면 스택트레이스(어디서 에러났는지 위치)도 추가
        if (type == LogType.Exception || type == LogType.Error)
        {
            newLog += $"<size=80%><color=#ffcccc>{stackTrace}</color></size>\n";
        }

        // 텍스트 추가
        logText.text += newLog;

        // 자동으로 스크롤 맨 아래로 내리기
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    // 로그 지우기 버튼용 함수
    public void ClearLog()
    {
        logText.text = "";
    }
}