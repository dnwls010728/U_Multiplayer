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

public class ClientManager : Singleton<ClientManager>
{
    [SerializeField] private GameObject _serverItemPrefab;
    [SerializeField] private GameObject _contentParent;
    
    private IList<GameObject> _serverItems = new List<GameObject>();
    private IList<ISessionInfo> _sessionInfos;
    
    private IList<Lobby> _lobbies;
    
    private CancellationTokenSource _cts;
    
    private async void Start()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        
        try
        {
            await UnityServices.InitializeAsync();
            
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            
            Debug.Log($"Signed in as {AuthenticationService.Instance.PlayerId}");
            RefreshServerList();
        
            _cts = new CancellationTokenSource();
            PollServerListLoopAsync(_cts.Token).Forget();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
    
    private async Task UpdateSessions()
    {
        var options = new QueryLobbiesOptions
        {
            Count = 25,
            Filters = new ()
            {
                new (QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                new (QueryFilter.FieldOptions.S2, Application.version, QueryFilter.OpOptions.EQ),
                new (QueryFilter.FieldOptions.S1, "kr", QueryFilter.OpOptions.EQ)
            },
            Order = new ()
            {
                new (false, QueryOrder.FieldOptions.Created)
            }
        };

        var results = await LobbyService.Instance.QueryLobbiesAsync(options);
        _lobbies = results.Results;
    }

    private async void RefreshServerList()
    {
        try
        {
            await UpdateSessions();
            
            foreach (var serverItem in _serverItems)
            {
                Destroy(serverItem);
            }

            if (_lobbies is null) return;

            foreach (var lobby in _lobbies)
            {
                var itemPrefab = Instantiate(_serverItemPrefab, _contentParent.transform);
                if (itemPrefab.TryGetComponent<ServerItem>(out var serverItem))
                {
                    serverItem.SetSession(lobby);
                    serverItem.AddSelectListener(OnSelectServer);
                }
                
                _serverItems.Add(itemPrefab);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void OnSelectServer(Lobby lobby)
    {
        string ip = lobby.Data["ip"].Value;
        ushort port = ushort.Parse(lobby.Data["port"].Value);
        
        Debug.Log($"{ip}:{port}");
        
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, port);

        if (!NetworkManager.Singleton.StartClient())
        {
            Debug.LogError("Failed to start client.");
            return;
        }
        
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
    
    private async UniTaskVoid PollServerListLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken: token);
            RefreshServerList();
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (clientId != NetworkManager.Singleton.LocalClientId) return;
        
        var reason = NetworkManager.Singleton.DisconnectReason;
        if (string.IsNullOrEmpty(reason))
            reason = "Disconnected by host.";
        
        Debug.Log($"Disconnected from server. Reason: {reason}");
    }
}
