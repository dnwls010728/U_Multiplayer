using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ServerItem : MonoBehaviour, ISelectHandler
{
    [SerializeField] private TextMeshProUGUI _serverNameText;
    [SerializeField] private TextMeshProUGUI _serverPlayersText;
    
    private ISessionInfo _sessionInfo;
    
    private UnityEvent<ISessionInfo> _selectEvent;
    
    public void OnSelect(BaseEventData eventData)
    {
        _selectEvent?.Invoke(_sessionInfo);
    }

    public void AddSelectListener(UnityAction<ISessionInfo> action)
    {
        _selectEvent ??= new UnityEvent<ISessionInfo>();
        _selectEvent.AddListener(action);
    }
    
    public void SetSession(ISessionInfo sessionInfo)
    {
        _sessionInfo = sessionInfo;
        SetServerName(_sessionInfo.Name);
        var currentPlayers = int.Parse(_sessionInfo.Properties["players"].Value);
        SetPlayers(currentPlayers, _sessionInfo.MaxPlayers);
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
