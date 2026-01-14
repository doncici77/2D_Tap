using UnityEngine;
using Unity.Netcode; // 필수
using UnityEngine.UI;

public class NetworkUI : MonoBehaviour
{
    public Button hostButton;
    public Button clientButton;

    void Start()
    {
        // 방장(서버+플레이어)으로 시작
        hostButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
        });

        // 참가자(손님)로 시작
        clientButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
        });
    }
}