using Unity.Multiplayer;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    private ISession _session;
    
    async void Start()
    {
        var role = MultiplayerRolesManager.ActiveMultiplayerRoleMask;

        if (role == MultiplayerRoleFlags.Server)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length; ++i)
            {
                Debug.Log(args[i]);
            }
            
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData("0.0.0.0", 7777);
            NetworkManager.Singleton.StartServer();
            
            _session = await MultiplayerService.Instance.CreateSessionAsync(new SessionOptions()
            {
                Name = "Test Session",
                MaxPlayers = 4,
                IsPrivate = false,
                SessionProperties = new()
                {
                    { "ip", new SessionProperty("") },
                    { "port", new SessionProperty("") },
                    { "region", new SessionProperty("") },
                    { "build", new SessionProperty(Application.version) },
                }
            });
            
            Debug.Log("Starting as Server");
        }
        else if (role == MultiplayerRoleFlags.Client)
        {
            Debug.Log("Starting as Client");
        }
    }
}