using UnityEngine;

public class DebugHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterState characterState;
    [SerializeField] private CharacterMovement characterMovement;
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private PlayerMovementIntent movementIntent;
    [SerializeField] private ParkourDetector parkourDetector;

    [Header("Debug Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;

    private bool showDebug = true;

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showDebug = !showDebug;
        }
    }

    private void OnGUI()
    {
        if (!showDebug)
        {
            return;
        }

        GUILayout.BeginArea(
            new Rect(20f, 20f, 350f, 500f)
        );

        GUILayout.Label("PLAYER DEBUG");
        GUILayout.Label("------------------------------");

        if (characterState != null)
        {
            GUILayout.Label(
                $"State: {characterState.CurrentState}"
            );
        }

        if (characterMovement != null)
        {
            GUILayout.Label(
                $"Speed: {characterMovement.CurrentSpeed:F2}"
            );

            GUILayout.Label(
                $"Vertical Velocity: {characterMovement.VerticalVelocity:F2}"
            );
        }

        GUILayout.Space(5f);

        GUILayout.Label("INPUT");
        GUILayout.Label("------------------------------");

        if (inputHandler != null)
        {
            GUILayout.Label(
                $"Move: {inputHandler.MoveInput}"
            );

            GUILayout.Label(
                $"Sprint: {inputHandler.SprintHeld}"
            );

            GUILayout.Label(
                $"Jump: {inputHandler.JumpPressed}"
            );
        }

        GUILayout.Space(5f);

        GUILayout.Label("MOVEMENT INTENT");
        GUILayout.Label("------------------------------");

        if (movementIntent != null)
        {
            GUILayout.Label(
                $"Direction: {movementIntent.MovementDirection}"
            );

            GUILayout.Label(
                $"Facing: {movementIntent.FacingDirection}"
            );

            GUILayout.Label(
                $"Sprinting: {movementIntent.Sprinting}"
            );
        }

        GUILayout.Space(5f);

        GUILayout.Label("PARKOUR DETECTION");
        GUILayout.Label("------------------------------");

        if (parkourDetector != null)
        {
            GUILayout.Label(
                $"Obstacle Detected: {parkourDetector.ObstacleDetected}"
            );

            GUILayout.Label(
                $"Obstacle Height: {parkourDetector.ObstacleHeight:F2}"
            );
        }
        GUILayout.EndArea();
    }
}