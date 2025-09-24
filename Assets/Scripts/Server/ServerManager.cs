using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Multiplayer;
using UnityEngine;

public class ServerManager : Singleton<ServerManager>
{
    private ISession _session;
    private Lobby _lobby;
    
    private string _ip = "127.0.0.1";
    private string _hostname = "";
    private string _description = "";
    private string _region = "";
    
    private ushort _port = 7777;
    
    private int _maxPlayers = 4;
    
    private CancellationTokenSource _cts;
    
    private readonly HashSet<ulong> _pending = new ();
    private readonly Dictionary<ulong, Task> _timeouts = new ();
    
    void Start()
    {
        var args = Environment.GetCommandLineArgs();
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
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;

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
            var options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new ()
                {
                    ["ip"] = new DataObject(DataObject.VisibilityOptions.Public, _ip),
                    ["port"] = new DataObject(DataObject.VisibilityOptions.Public, _port.ToString()),
                    ["description"] = new DataObject(DataObject.VisibilityOptions.Public, _description),
                    ["region"] = new DataObject(DataObject.VisibilityOptions.Public, _region, DataObject.IndexOptions.S1),
                    ["build"] = new DataObject(DataObject.VisibilityOptions.Public, Application.version, DataObject.IndexOptions.S2),
                    ["players"] = new DataObject(DataObject.VisibilityOptions.Public, "0")
                }
            };
            
            _lobby = await LobbyService.Instance.CreateLobbyAsync(_hostname, _maxPlayers, options);
            Debug.Log("Server is ready.");
            
            _cts = new CancellationTokenSource();
            Heartbeat(_cts.Token).Forget();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private async UniTaskVoid Heartbeat(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(_lobby.Id);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Lobby Heartbeat failed: {e.Message}");
            }
            
            await UniTask.Delay(TimeSpan.FromSeconds(15), cancellationToken: token);
        }
    }

    private void OnClientChanged(ulong clientId)
    {
        if (_pending.Remove(clientId))
            _timeouts.Remove(clientId);
        
        _ = FlushPlayersAsync();
    }

    private async Task FlushPlayersAsync()
    {
        try
        {
            var current = NetworkManager.Singleton.ConnectedClientsIds.Count;
            var options = new UpdateLobbyOptions
            {
                Data = new ()
                {
                    ["players"] = new DataObject(DataObject.VisibilityOptions.Public, current.ToString())
                }
            };
            
            _lobby = await LobbyService.Instance.UpdateLobbyAsync(_lobby.Id, options);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        var current = NetworkManager.Singleton.ConnectedClientsIds.Count;
        var pending = _pending.Count;
        
        bool isFull = (current + pending) >= _maxPlayers;
        if (isFull)
        {
            response.Approved = false;
            response.Reason = "Server is full.";
            return;
        }
        
        if (!_pending.Add(request.ClientNetworkId))
        {
            response.Approved = false;
            response.Reason = "Client is already pending.";
            return;
        }
        
        _timeouts[request.ClientNetworkId] = StartTimeout(request.ClientNetworkId, 5f);
        
        response.Approved = true;
        response.Pending = false;
    }

    private async Task StartTimeout(ulong clientId, float seconds)
    {
        float end = Time.time + seconds;
        while (Time.time < end)
        {
            if (!_pending.Contains(clientId)) return;
            await Task.Yield();
        }
        
        if (_pending.Remove(clientId))
        {
            _timeouts.Remove(clientId);
            Debug.LogWarning($"Client {clientId} timed out.");
        }
    }
}
