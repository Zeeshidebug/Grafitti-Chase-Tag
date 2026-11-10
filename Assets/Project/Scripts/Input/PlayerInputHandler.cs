using UnityEngine;

[DefaultExecutionOrder(-200)]
public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public bool SprintHeld { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool DebugLog { get; private set; }
    public bool DebugNext { get; private set; }
    public bool DebugPrevious { get; private set; }
    public bool Interaction { get; private set; }
    public bool InteractionHeld { get; private set; }
    public bool IsTagging { get; private set; }

    private InputSystem_Actions inputActions;

    public bool PausePressed => inputActions.Player.Pause.WasPressedThisFrame();
    public bool FreeCursorHeld => inputActions.Player.FreeCursor.IsPressed();

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

    private void OnDestroy()
    {
        inputActions.Dispose();
    }

    private void Update()
    {
        var player = inputActions.Player;
        bool playing = !GameplayPauseController.IsPaused;
        bool canInteract = playing && !FreeCursorHeld;
        MoveInput = playing ? player.Move.ReadValue<Vector2>() : Vector2.zero;
        SprintHeld = playing && player.Sprint.IsPressed();
        JumpPressed = playing && player.Jump.WasPressedThisFrame();
        DebugLog = inputActions.Player.DebugMenu.WasPressedThisFrame();
        DebugNext = inputActions.Player.DebugNext.WasPressedThisFrame();
        DebugPrevious = inputActions.Player.DebugPrevious.WasPressedThisFrame();
        Interaction = canInteract && player.Interaction.WasPressedThisFrame();
        InteractionHeld = canInteract && player.Interaction.IsPressed();
        IsTagging = canInteract && player.Tagging.WasPressedThisFrame();
    }
}
