using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionManager : Singleton<SessionManager>
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private GameObject _gameStatePrefab;
    
    [SerializeField]
    private string _joinCode;
    
    [field: SerializeField]
    public WorldState WorldState { get; set; }
    
    public string JoinCode => _joinCode;
    
    public async void OpenSession()
    {
        try
        {
            if (NetworkManager.Singleton.IsListening)
            {
                Debug.LogWarning("Already hosting a session.");
                return;
            }
            
            NetworkManager.Singleton.ConnectionApprovalCallback -= Approval;
            NetworkManager.Singleton.ConnectionApprovalCallback += Approval;
            
            var allocation = await RelayService.Instance.CreateAllocationAsync(4);
            _joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);
            
            if (!NetworkManager.Singleton.StartHost())
            {
                Debug.LogError("Failed to start host");
                return;
            }

            NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
            NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            
            var status = NetworkManager.Singleton.SceneManager.LoadScene("WorldScene", LoadSceneMode.Single);
            if (status != SceneEventProgressStatus.Started)
            {
                Debug.LogError($"Failed to load scene: {status}");
                NetworkManager.Singleton.Shutdown();
                return;
            }
            
            Debug.Log($"Host started. Join code: {_joinCode}");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
    
    public async void JoinSession(string joinCode)
    {
        try
        {
            if (NetworkManager.Singleton.IsListening)
            {
                Debug.LogWarning("Already in a session.");
                return;
            }
            
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(joinAllocation.RelayServer.IpV4, (ushort)joinAllocation.RelayServer.Port, joinAllocation.AllocationIdBytes, joinAllocation.Key, joinAllocation.ConnectionData, joinAllocation.HostConnectionData);
            
            if (!NetworkManager.Singleton.StartClient())
            {
                Debug.LogError("Failed to start client");
                return;
            }
            
            Debug.Log("Client started and joined session.");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
    
    private void Approval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = false;
    }

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (sceneEvent.SceneName != "WorldScene") return;

        if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
        {
            var go = Instantiate(_gameStatePrefab);
            if (go.TryGetComponent(out NetworkObject no))
                no.Spawn(true);
        }
        
        if (sceneEvent.SceneEventType == SceneEventType.LoadComplete)
            TrySpawnPlayer(sceneEvent.ClientId);
    }

    private void TrySpawnPlayer(ulong clientId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;
        if (client.PlayerObject is not null) return;
        
        var go = Instantiate(_playerPrefab, new Vector3(0f, 1f), Quaternion.identity);
        if (go.TryGetComponent(out NetworkObject no))
            no.SpawnAsPlayerObject(clientId, true);
    }
}
