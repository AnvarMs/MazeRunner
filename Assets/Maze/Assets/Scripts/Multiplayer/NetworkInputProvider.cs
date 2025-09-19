using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;

public class NetworkInputProvider : MonoBehaviour
{
    private PlayerControls controls;
    private Vector2 moveInput;
    private Vector2 lookInput;

    private void Awake()
    {
        controls = new PlayerControls();
        
        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        controls.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        controls.Player.Look.canceled += ctx => lookInput = Vector2.zero;
    }

    private void OnEnable()
    {
        controls?.Player.Enable();
    }

    private void OnDisable()
    {
        controls?.Player.Disable();
    }

    // This is called by Fusion to collect input
    public void OnInput(NetworkRunner runner, NetworkInputData input)
    {
        input.MovementInput = moveInput;
        input.LookInput = lookInput;
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInputData input)
    {
        // Handle missing input
    }

    private void OnDestroy()
    {
        controls?.Dispose();
    }
}
