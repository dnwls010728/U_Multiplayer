using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;


public class PlayerCharacter : CharacterBase
{
    [SerializeField] private SkinnedMeshRenderer _renderer;
    [SerializeField] private Rigidbody _rigid;
    [SerializeField] private Animator _animator;
    [SerializeField] private InputReader _input;
    [SerializeField] private LayerMask _interactableMask;

    private Vector3 _movement;
    private float _moveSpeed = 1.5f;
    private MaterialPropertyBlock _mpb;

    private void Awake()
    {
        _mpb = new();
    }
    
    private void Update()
    {
        if (!IsOwner) return;
        
        _movement = new Vector3(_input.Direction.x, 0, _input.Direction.y);

        if (_movement.magnitude > 0f)
        {
            var rotation = Quaternion.LookRotation(_movement);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, 300f * Time.deltaTime);
        }
        
        var velocity = _rigid.linearVelocity;
        var velocityXZ = new Vector3(velocity.x, 0f, velocity.z);
        var speed = velocityXZ.magnitude;
        speed = Mathf.Clamp01(speed / 5f);
        
        float direction = Vector3.SignedAngle(transform.forward, velocity.normalized, Vector3.up);
        direction = Mathf.Clamp(direction / 180f, -1f, 1f);
        
        _animator.SetFloat(Animator.StringToHash("Direction"), direction);
        _animator.SetFloat(Animator.StringToHash("Speed"), speed);
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;
        
        var eou = _animator.GetFloat(Animator.StringToHash("EOU"));
        var eov = _animator.GetFloat(Animator.StringToHash("EOV"));
        var mou = _animator.GetFloat(Animator.StringToHash("MOU"));
        var mov = _animator.GetFloat(Animator.StringToHash("MOV"));
        
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(Shader.PropertyToID("_EOU"), eou);
        _mpb.SetFloat(Shader.PropertyToID("_EOV"), eov);
        _mpb.SetFloat(Shader.PropertyToID("_MOU"), mou);
        _mpb.SetFloat(Shader.PropertyToID("_MOV"), mov);
        _renderer.SetPropertyBlock(_mpb);
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        if (_movement.magnitude > 0f)
        {
            var velocity = _movement * _moveSpeed;
            _rigid.linearVelocity = new Vector3(velocity.x, _rigid.linearVelocity.y, velocity.z);
        }
        else
        {
            _rigid.linearVelocity = new Vector3(0f, _rigid.linearVelocity.y, 0f);
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

        _input.Interact += OnInteract;
        _input.SprintKeyDown += OnSprintKeyDown;
        _input.SprintKeyUp += OnSprintKeyUp;
    }

    public void OnInteract()
    {
        var hasHit = Physics.Raycast(transform.position, transform.forward, out var hit, 1.5f, _interactableMask);
        if (!hasHit) return;
        
        if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
            interactable.OnInteract();
    }

    public void OnSprintKeyDown() => _moveSpeed = 5f;
    
    public void OnSprintKeyUp() => _moveSpeed = 1.5f;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
    }
}