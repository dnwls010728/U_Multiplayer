using Unity.Cinemachine;
using UnityEngine;


public class PlayerCharacter : CharacterBase
{
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private InputReader _input;

    private Vector3 _movement;
    
    private void Update()
    {
        if (!IsOwner) return;
        
        _movement = new Vector3(_input.Direction.x, 0, _input.Direction.y);

        if (_movement.magnitude > 0f)
        {
            var rotation = Quaternion.LookRotation(_movement);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, 720f * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        if (_movement.magnitude > 0f)
        {
            var velocity = _movement * 5f;
            _rb.linearVelocity = new Vector3(velocity.x, _rb.linearVelocity.y, velocity.z);
        }
        else
        {
            _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
        }
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