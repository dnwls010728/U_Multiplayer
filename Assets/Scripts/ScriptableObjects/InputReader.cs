using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static InputSystem_Actions;

[CreateAssetMenu(fileName = "InputReader", menuName = "ScriptableObjects/InputReader")]
public class InputReader : ScriptableObject, IPlayerActions
{
    public event UnityAction<Vector2> Move = delegate { };
    public event UnityAction Interact = delegate { };
    
    private InputSystem_Actions _inputActions;
    
    public Vector2 Direction => _inputActions.Player.Move.ReadValue<Vector2>();

    private void OnEnable()
    {
        if (_inputActions is null)
        {
            _inputActions = new InputSystem_Actions();
            _inputActions.Player.SetCallbacks(this);
        }
        
        _inputActions.Enable();
    }
    
    private void OnDisable()
    {
        _inputActions.Disable();
    }
    
    public void OnMove(InputAction.CallbackContext context)
    {
        Move?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed) Interact?.Invoke();
    }
}
