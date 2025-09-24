using Unity.Multiplayer;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    void Start()
    {
        var role = MultiplayerRolesManager.ActiveMultiplayerRoleMask;
        
        Debug.Log($"Starting game with role: {role}");

        if (role == MultiplayerRoleFlags.Server)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            SceneManager.LoadScene("ServerScene");
        }
        else if (role == MultiplayerRoleFlags.Client)
        {
            SceneManager.LoadScene("ClientScene");
        }
    }
}