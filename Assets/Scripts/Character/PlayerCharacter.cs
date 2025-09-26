using Unity.Cinemachine;
using UnityEngine;


public class PlayerCharacter : CharacterBase
{
    private InputSystem_Actions _actions;

    private void Awake()
    {
        if (!IsOwner) return;
        _actions = new();
    }
    
    private void Update()
    {
        if (!IsOwner) return;
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (!IsOwner) return;

        var vcam = FindFirstObjectByType<CinemachineCamera>();
        if (vcam is null) return;
        
        vcam.Follow = transform;
        vcam.LookAt = transform;
    }
}