using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

public class ClientManager : Singleton<ClientManager>
{
    [SerializeField] private GameObject _serverItemPrefab;
    [SerializeField] private GameObject _contentParent;
    
    private IList<GameObject> _serverItems = new List<GameObject>();
    private IList<ISessionInfo> _sessionInfos;
    
    private CancellationTokenSource _cts;
    
    void Start()
    {
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
        Debug.Log($"Signed in as {AuthenticationService.Instance.PlayerId}");
        RefreshServerList();
        
        _cts = new CancellationTokenSource();
        PollServerListLoopAsync(_cts.Token).Forget();
    }
    private async Task UpdateSessions()
    {
        var results = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions());
        _sessionInfos = results.Sessions;
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

            if (_sessionInfos is null) return;

            foreach (var sessionInfo in _sessionInfos)
            {
                var itemPrefab = Instantiate(_serverItemPrefab, _contentParent.transform);
                if (itemPrefab.TryGetComponent<ServerItem>(out var serverItem))
                {
                    serverItem.SetSession(sessionInfo);
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

    private void OnSelectServer(ISessionInfo sessionInfo)
    {
        string ip = sessionInfo.Properties["ip"].Value;
        ushort port = ushort.Parse(sessionInfo.Properties["port"].Value);
        
        Debug.Log($"{ip}:{port}");
        
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, port);
        
        if (!NetworkManager.Singleton.StartClient())
            Debug.LogError("Failed to start client.");
    }
    
    private async UniTaskVoid PollServerListLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken: token);
            RefreshServerList();
        }
    }
}
