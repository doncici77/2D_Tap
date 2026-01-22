using UnityEngine;
using UnityEngine.SceneManagement; // 씬 이동을 위해 필수

public class TitleManager : MonoBehaviour
{
    [Header("Settings")]
    public string gameSceneName = "GameScene"; // 이동할 게임 씬 이름 (정확히 적어야 함)
    public string multiSceneName = "MultiScene"; // 이동할 게임 씬 이름 (정확히 적어야 함)

    private void Start()
    {
        // 모바일에서도 60프레임 목표로 설정
        Application.targetFrameRate = 60;
        // 수직 동기화 끄기 (모바일에서 프레임 제한과 충돌 방지)
        QualitySettings.vSyncCount = 0;

        SoundManager.Instance.PlayBGM(SoundManager.Instance.lobbyBGM);
    }

    // 시작 버튼에 연결할 함수
    public void OnStartBtnClick()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnMultiBtnClick()
    {
        SceneManager.LoadScene(multiSceneName);
    }
}