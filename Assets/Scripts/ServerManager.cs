using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Unity.Multiplayer;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerManager : Singleton<ServerManager>
{
    private string _ip = "127.0.0.1";
    private string _serverName = "";
    private string _description = "";
    private string _region = "KR";
    
    private int _maxPlayers = 4;
    
    private ushort _port = 7777;
    private ushort _queryPort = 8888; // 현재 사용되지 않음

    private Lobby _lobby;

    private CancellationTokenSource _cts;
    
    private readonly HashSet<ulong> _pending = new ();
    private readonly Dictionary<ulong, Task> _timeouts = new ();
    
    private async void Start()
    {
        var role = MultiplayerRolesManager.ActiveMultiplayerRoleMask;
        if (role == MultiplayerRoleFlags.Client)
        {
            SceneManager.LoadScene("ClientScene");
            return;
        }
        
        SetTargetFrameRate();
        ParseCommandLineArgs();
        
        var networkManager = NetworkManager.Singleton;
        
        var transport = networkManager.GetComponent<UnityTransport>();
        transport.SetConnectionData("0.0.0.0", _port);
        
        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        networkManager.ConnectionApprovalCallback += ApprovalCheck;

        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            
            Debug.Log("Signed in as: " + AuthenticationService.Instance.PlayerId);

            if (!networkManager.StartServer())
            {
                Debug.LogError("Failed to start server");
                return;
            }
            
            var status = networkManager.SceneManager.LoadScene("WorldScene", LoadSceneMode.Single);
            if (status != SceneEventProgressStatus.Started)
            {
                Debug.LogError("Failed to load scene: " + status);
                return;
            }
            
            // 개선 필요
            var options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new ()
                {
                    ["ip"] = new DataObject(DataObject.VisibilityOptions.Public, _ip),
                    ["port"] = new DataObject(DataObject.VisibilityOptions.Public, _port.ToString()),
                    ["queryPort"] = new DataObject(DataObject.VisibilityOptions.Public, _queryPort.ToString()),
                    ["description"] = new DataObject(DataObject.VisibilityOptions.Public, _description),
                    ["region"] = new DataObject(DataObject.VisibilityOptions.Public, _region, DataObject.IndexOptions.S1),
                    ["build"] = new DataObject(DataObject.VisibilityOptions.Public, Application.version, DataObject.IndexOptions.S2),
                    ["players"] = new DataObject(DataObject.VisibilityOptions.Public, "0")
                }
            };
            
            _lobby = await LobbyService.Instance.CreateLobbyAsync(_serverName, _maxPlayers, options);
                
            _cts = new CancellationTokenSource();
            Heartbeat(_cts.Token).Forget();
                
            Debug.Log("Server is ready.");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void SetTargetFrameRate()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    private void ParseCommandLineArgs()
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length; ++i)
        {
            switch (args[i])
            {
                case "--ip" when (i + 1) < args.Length:
                    _ip = args[i + 1];
                    break;
                case "--serverName" when (i + 1) < args.Length:
                    _serverName = args[i + 1];
                    break;
                case "--description" when (i + 1) < args.Length:
                    _description = args[i + 1];
                    break;
                case "--region" when (i + 1) < args.Length:
                    _region = args[i + 1].ToLower();
                    break;
                case "--maxPlayers" when (i + 1) < args.Length:
                    _maxPlayers = int.Parse(args[i + 1]);
                    break;
                case "--port" when (i + 1) < args.Length:
                    _port = ushort.Parse(args[i + 1]);
                    break;
                case "--queryPort" when (i + 1) < args.Length:
                    _queryPort = ushort.Parse(args[i + 1]);
                    break;
            }
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (_pending.Remove(clientId))
            _timeouts.Remove(clientId);
        
        Debug.Log($"Client {clientId} connected.");
        _ = FlushPlayersAsync();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (_pending.Remove(clientId))
            _timeouts.Remove(clientId);
        
        Debug.Log($"Client {clientId} disconnected.");
        _ = FlushPlayersAsync();
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
        var end = Time.time + seconds;
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
}
