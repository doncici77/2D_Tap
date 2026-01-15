using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies.Models;

public class RoomItem : MonoBehaviour
{
    public TextMeshProUGUI roomNameText;
    public Button joinButton;

    private Lobby lobby;
    private LobbyManager manager;

    public void Setup(Lobby _lobby, LobbyManager _manager)
    {
        lobby = _lobby;
        manager = _manager;

        // 텍스트 표시: "방제목 (1/2)"
        roomNameText.text = $"{lobby.Name} ({lobby.Players.Count}/{lobby.MaxPlayers})";

        joinButton.onClick.AddListener(() => {
            manager.JoinRoom(lobby); // 클릭하면 입장 시도
        });
    }
}