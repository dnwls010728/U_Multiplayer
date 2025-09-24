using TMPro;
using Unity.Services.Lobbies.Models;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ServerItem : MonoBehaviour, ISelectHandler
{
    [SerializeField] private TextMeshProUGUI _serverNameText;
    [SerializeField] private TextMeshProUGUI _serverPlayersText;
    
    private Lobby _lobby;
    
    private UnityEvent<Lobby> _selectEvent;
    
    public void OnSelect(BaseEventData eventData)
    {
        _selectEvent?.Invoke(_lobby);
    }

    public void AddSelectListener(UnityAction<Lobby> action)
    {
        _selectEvent ??= new UnityEvent<Lobby>();
        _selectEvent.AddListener(action);
    }
    
    public void SetSession(Lobby lobby)
    {
        _lobby = lobby;
        SetServerName(_lobby.Name);
        var currentPlayers = int.Parse(_lobby.Data["players"].Value);
        SetPlayers(currentPlayers, _lobby.MaxPlayers);
    }
    
    private void SetServerName(string serverName)
    {
        _serverNameText.text = serverName;
    }
    
    private void SetPlayers(int currentPlayers, int maxPlayers)
    {
        _serverPlayersText.text = $"{currentPlayers}/{maxPlayers}";
    }
}
