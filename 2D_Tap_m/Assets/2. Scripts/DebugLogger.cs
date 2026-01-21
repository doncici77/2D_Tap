using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DebugLogger : MonoBehaviour
{
    [Header("UI References")]
    public Text logText; // 로그가 찍힐 텍스트
    public ScrollRect scrollRect;   // 스크롤 뷰 (로그가 길어지면 내려보게)

    void OnEnable() { Application.logMessageReceived += HandleLog; }
    void OnDisable() { Application.logMessageReceived -= HandleLog; }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (logText == null) return;

        // ★ [핵심 필터링] "[System]" 태그가 붙은 로그만 통과시킵니다.
        // (단, 빨간색 시스템 에러(Exception)는 중요하니까 무조건 보여줍니다)
        if (!logString.StartsWith("[System]") && type != LogType.Exception)
        {
            return;
        }

        // 보기 좋게 "[System]" 글자는 떼고 출력 (선택 사항)
        string cleanLog = logString.Replace("[System] ", "");

        string color = "white";
        if (type == LogType.Error || type == LogType.Exception) color = "red";
        else if (type == LogType.Warning) color = "yellow";

        string newLog = $"<color={color}>[{System.DateTime.Now:HH:mm:ss}] {cleanLog}</color>\n";

        if (type == LogType.Exception || type == LogType.Error)
        {
            newLog += $"<size=70%><color=#ffcccc>{stackTrace}</color></size>\n";
        }

        logText.text += newLog;

        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public void ClearLog()
    {
        logText.text = "";
    }
}