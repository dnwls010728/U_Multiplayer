using Unity.Cinemachine;
using UnityEngine;


public class PlayerCharacter : CharacterBase
{
    [SerializeField] private CharacterController _controller;
    [SerializeField] private InputReader _input;
    
    private void Update()
    {
        if (!IsOwner) return;
        
        var direction = new Vector3(_input.Direction.x, 0, _input.Direction.y).normalized;
        if (direction.magnitude > .1f)
        {
            var rotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, 720f * Time.deltaTime);
            // transform.LookAt(transform.position + direction);
            
            _controller.Move(direction * (5f * Time.deltaTime));
        }
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (!IsOwner) return;

        var vcam = FindFirstObjectByType<CinemachineCamera>();
        if (vcam is null) return;
        
        vcam.Follow = transform;
        // vcam.LookAt = transform;
    }
}