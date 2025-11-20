using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads input from InputSystem and broadcasts event if there's any this frame
/// </summary>
public class PlayerInput : MonoBehaviour {
    // STRING LITERALS
    private const string MoveActionName = "Move";

    // REFERENCES
    private InputAction moveAction;
    
    // STATE
    private Vector2 currentMovementInput;
    private bool isMovementInputOngoing;
    
    // DELEGATES
    public delegate void PlayerInputReceived(Vector2 input);
    public static event PlayerInputReceived OnPlayerInputReceived;
    
    private void OnEnable() {
        moveAction = InputSystem.actions.FindAction(MoveActionName);
        moveAction.started += OnStartedMovementInput;
        moveAction.canceled += OnStoppedMovementInput;
    }

    private void OnDisable() {
        moveAction.started -= OnStartedMovementInput;
        moveAction.canceled -= OnStoppedMovementInput;
    }
    
    private void OnStartedMovementInput(InputAction.CallbackContext _) => isMovementInputOngoing = true;

    private void OnStoppedMovementInput(InputAction.CallbackContext _) => isMovementInputOngoing = false;
    
    private void Update() {
        if (!isMovementInputOngoing) return;
        
        currentMovementInput = moveAction.ReadValue<Vector2>();

        // Sanitize input. If we have nearly identical input in two axes at the same time, set both to zero.
        // This could cause trouble with joystick input, but this prototype expects only keyboard input.
        if (Mathf.Abs(currentMovementInput.x) > Mathf.Abs(currentMovementInput.y)) 
            currentMovementInput.y = 0;
        else if (Mathf.Abs(currentMovementInput.x) < Mathf.Abs(currentMovementInput.y))
            currentMovementInput.x = 0;
        else if (Mathf.Approximately(currentMovementInput.x, currentMovementInput.y))
            currentMovementInput = Vector2.zero;
        
        // check to broadcast input
        if(currentMovementInput.magnitude > 0)
            OnPlayerInputReceived?.Invoke(currentMovementInput);
    }
}
