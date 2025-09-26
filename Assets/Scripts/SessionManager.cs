using System;
using Sirenix.OdinInspector;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionManager : Singleton<SessionManager>
{
    [SerializeField] [ReadOnly]
    private string _joinCode;
    
    public string JoinCode => _joinCode;
    
    [Button]
    public async void OpenSession()
    {
        try
        {
            if (NetworkManager.Singleton.IsListening)
            {
                Debug.LogWarning("Already hosting a session.");
                return;
            }
            
            var allocation = await RelayService.Instance.CreateAllocationAsync(4);
            _joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);
            
            if (!NetworkManager.Singleton.StartHost())
            {
                Debug.LogError("Failed to start host");
                return;
            }

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
    
    [Button]
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
}
