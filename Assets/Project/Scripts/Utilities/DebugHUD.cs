using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class DebugHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterState characterState;
    [SerializeField] private CharacterMovement characterMovement;
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private PlayerMovementIntent movementIntent;
    [SerializeField] private ParkourDetector parkourDetector;
    [SerializeField] private ParkourCandidateEvaluator parkourEvaluator;

    private bool showDebug = true;

    private enum DebugPage
    {
        Player,
        Parkour
    }

    private DebugPage currentPage = DebugPage.Player;

    private void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        if (inputHandler.DebugLog)
        {
            showDebug = !showDebug;
        }

        if (!showDebug)
        {
            return;
        }

        if (inputHandler.DebugNext)
        {
            NextPage();
        }

        if (inputHandler.DebugPrevious)
        {
            PreviousPage();
        }
    }

    private void NextPage()
    {
        int next =
            (int)currentPage + 1;

        if (next >= System.Enum.GetValues(
            typeof(DebugPage)).Length)
        {
            next = 0;
        }

        currentPage = (DebugPage)next;
    }

    private void PreviousPage()
    {
        int previous =
            (int)currentPage - 1;

        if (previous < 0)
        {
            previous =
                System.Enum.GetValues(
                    typeof(DebugPage)).Length - 1;
        }

        currentPage = (DebugPage)previous;
    }

    private void OnGUI()
    {
        if (!showDebug)
        {
            return;
        }

        GUILayout.BeginArea(
            new Rect(20f, 20f, 350f, 1000f)
        );

        GUILayout.Label(
            $"DEBUG — {currentPage.ToString().ToUpper()}"
        );

        GUILayout.Label(
            "------------------------------"
        );

        switch (currentPage)
        {
            case DebugPage.Player:
                DrawPlayerPage();
                break;

            case DebugPage.Parkour:
                DrawParkourPage();
                break;
        }

        GUILayout.EndArea();
    }

    private void DrawPlayerPage()
    {
        if (characterState != null)
        {
            GUILayout.Label(
                $"State: {characterState.CurrentState}"
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

        GUILayout.Label("MOVEMENT");
        GUILayout.Label("------------------------------");

        if (characterMovement != null)
        {
            GUILayout.Label(
                $"Speed: {characterMovement.CurrentSpeed:F2}"
            );

            GUILayout.Label(
                $"Vertical Velocity: " +
                $"{characterMovement.VerticalVelocity:F2}"
            );
        }

        GUILayout.Space(5f);

        GUILayout.Label("MOVEMENT INTENT");
        GUILayout.Label("------------------------------");

        if (movementIntent != null)
        {
            GUILayout.Label(
                $"Direction: " +
                $"{movementIntent.MovementDirection}"
            );

            GUILayout.Label(
                $"Facing: " +
                $"{movementIntent.FacingDirection}"
            );

            GUILayout.Label(
                $"Sprinting: " +
                $"{movementIntent.Sprinting}"
            );
        }
    }

    private void DrawParkourPage()
    {
        GUILayout.Label("DETECTION");
        GUILayout.Label("------------------------------");

        if (parkourDetector != null)
        {
            GUILayout.Label(
                $"Obstacle: " +
                $"{parkourDetector.ObstacleDetected}"
            );

            if (parkourDetector.ObstacleDetected)
            {
                ParkourObstacleData obstacle =
                    parkourDetector.CurrentObstacle;

                GUILayout.Label(
                    $"Distance: {obstacle.Distance:F2}"
                );

                GUILayout.Label(
                    $"Height: {obstacle.Height:F2}"
                );

                GUILayout.Label(
                    $"Normal: {obstacle.SurfaceNormal}"
                );

                GUILayout.Label(
                    $"Top: {obstacle.TopPosition}"
                );
            }
        }

        GUILayout.Space(5f);

        GUILayout.Label("CANDIDATE");
        GUILayout.Label("------------------------------");

        if (parkourEvaluator != null)
        {
            ParkourCandidate candidate =
                parkourEvaluator.CurrentCandidate;

            if (candidate != null)
            {
                GUILayout.Label(
                    $"Vault: {candidate.CanVault}"
                );

                GUILayout.Label(
                    $"Vault Result: {candidate.VaultResult}"
                );

                GUILayout.Label(
                    $"Clearance: {candidate.HasClearance}"
                );

                GUILayout.Label(
                    $"Landing Space: {candidate.HasLandingSpace}"
                );

                GUILayout.Label(
                    $"Climb: {candidate.CanClimb}"
                );
            }
            else
            {
                GUILayout.Label(
                    "No Candidate"
                );
            }
        }
    }
}