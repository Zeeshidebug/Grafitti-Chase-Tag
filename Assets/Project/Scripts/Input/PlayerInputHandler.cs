using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public bool SprintHeld { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool DebugLog { get; private set; }
    public bool DebugNext { get; private set; }
    public bool DebugPrevious { get; private set; }
    public bool Interaction { get; private set; }
    public Vector2 LookInput { get; private set; }

    private InputSystem_Actions inputActions;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        MoveInput = inputActions.Player.Move.ReadValue<Vector2>();
        SprintHeld = inputActions.Player.Sprint.IsPressed();
        JumpPressed = inputActions.Player.Jump.WasPressedThisFrame();
        LookInput = inputActions.Player.Look.ReadValue<Vector2>();
        DebugLog = inputActions.Player.DebugMenu.WasPressedThisFrame();
        DebugNext = inputActions.Player.DebugNext.WasPressedThisFrame();
        DebugPrevious = inputActions.Player.DebugPrevious.WasPressedThisFrame();
        Interaction = inputActions.Player.Interaction.WasPressedThisFrame();
    }
}