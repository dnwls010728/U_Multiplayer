using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

public class ServerManager : Singleton<ServerManager>
{
    private ISession _session;
    
    private string _ip = "127.0.0.1";
    private string _hostname = "";
    private string _description = "";
    private string _region = "";
    
    private ushort _port = 7777;
    
    private int _maxPlayers = 4;
    
    void Start()
    {
        var args = System.Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length; ++i)
        {
            if (args[i] == "-ip" && (i + 1 < args.Length))
                _ip = args[i + 1];
            else if (args[i] == "-port" && (i + 1 < args.Length))
                ushort.TryParse(args[i + 1], out _port);
            else if (args[i] == "-hostname" && (i + 1 < args.Length))
                _hostname = args[i + 1];
            else if (args[i] == "-maxPlayers" && (i + 1 < args.Length))
                int.TryParse(args[i + 1], out _maxPlayers);
            else if (args[i] == "-description" && (i + 1 < args.Length))
                _description = args[i + 1];
            else if (args[i] == "-region" && (i + 1 < args.Length))
                _region = args[i + 1];
        }
        
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData("0.0.0.0", _port);
        
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientChanged;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientChanged;

        _ = BootAsync();
    }

    private async Task BootAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();
            
            AuthenticationService.Instance.SignedIn += OnSignedIn;
            if (AuthenticationService.Instance.IsSignedIn)
            {
                OnSignedIn();
                return;
            }
            
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void OnSignedIn()
    {
        if (!NetworkManager.Singleton.StartServer())
            Debug.LogError("Failed to start server.");
    }
    
    private async void OnServerStarted()
    {
        try
        {
            MultiplayerService.Instance.SessionAdded += OnSessionAdded;
            _session = await MultiplayerService.Instance.CreateSessionAsync(new SessionOptions()
            {
                Name = _hostname,
                MaxPlayers = _maxPlayers,
                IsPrivate = false,
                SessionProperties = new()
                {
                    { "ip", new SessionProperty(_ip) },
                    { "port", new SessionProperty(_port.ToString()) },
                    { "description", new SessionProperty(_description) },
                    { "region", new SessionProperty(_region) },
                    { "build", new SessionProperty(Application.version) },
                    { "players", new SessionProperty("0") }
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
    
    private void OnSessionAdded(ISession session)
    {
        Debug.Log("Server is ready.");
        _ = UpdatePlayersPropertyAsync();
    }

    private void OnClientChanged(ulong clientId)
    {
        _ = UpdatePlayersPropertyAsync();
    }

    private async Task UpdatePlayersPropertyAsync()
    {
        if (_session is null) return;
        
        var current = NetworkManager.Singleton.ConnectedClientsIds.Count;
        Debug.Log($"Updating players property: {current}");

        var host = _session.AsHost();
        host.SetProperty("players", new SessionProperty(current.ToString()));
        await host.SavePropertiesAsync();
    }
}
