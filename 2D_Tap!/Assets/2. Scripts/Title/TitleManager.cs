using UnityEngine;
using UnityEngine.SceneManagement; // 씬 이동을 위해 필수

public class TitleManager : MonoBehaviour
{
    [Header("Settings")]
    public string gameSceneName = "GameScene"; // 이동할 게임 씬 이름 (정확히 적어야 함)

    // 시작 버튼에 연결할 함수
    public void OnStartBtnClick()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // 종료 버튼에 연결할 함수
    public void OnExitBtnClick()
    {
        Debug.Log("게임 종료 (에디터에서는 안 꺼짐)");
        Application.Quit();
    }
}