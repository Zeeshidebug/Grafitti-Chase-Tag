using Unity.VisualScripting;
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
    [SerializeField] private VaultExecutor vaultExecutor;
    [SerializeField] private VaultEvaluator vaultEvaluator;
    [SerializeField] private ClimbExecutor climbExecutor;
    [SerializeField] private ClimbEvaluator climbEvaluator;
    [SerializeField] private StaminaSystem staminaSystem;
    [SerializeField] private SlideExecutor slideExecutor;
    [SerializeField] private PoleSpinEvaluator poleSpinEvaluator;
    [SerializeField] private PoleSpinExecutor poleSpinExecutor;
    [SerializeField] private BuffNerfController buffNerfController;
    [SerializeField] private RoundController roundController;
    [SerializeField] private GameController gameController;
    [SerializeField] private RoleController aiRole;
    [SerializeField] private RoleController playerRole;
    [SerializeField] private ScoreController scoreController;

    private bool showDebug = true;

    private enum DebugPage
    {
        Player,
        Obstacle,
        Parkour,
        BuffNerf,
        Round
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

            case DebugPage.Obstacle:
                DrawObstaclePage();
                break;

            case DebugPage.Parkour:
                DrawParkourPage();
                break;

            case DebugPage.BuffNerf:
                DrawBuffNerfPage();
                break;

            case DebugPage.Round:
                DrawRoundPage();
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

            GUILayout.Label(
            $"Interaction: {inputHandler.Interaction}"
            );

            GUILayout.Label(
            $"Interaction Held: {inputHandler.InteractionHeld}"
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

            GUILayout.Label(
            $"Stamina: {staminaSystem.CurrentStamina:F2}"
            );

            GUILayout.Label(
            $"Is Exhausted: {staminaSystem.IsExhausted}"
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

    private void DrawObstaclePage()
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

        if (vaultEvaluator != null)
        {
            VaultCandidate candidate =
                vaultEvaluator.Evaluate(
                    parkourDetector.CurrentObstacle
                );

            if (candidate != null)
            {
                GUILayout.Label(
                    $"Vault: {candidate.IsValid}"
                );

                GUILayout.Label(
                    $"Vault Result: {candidate.Result}"
                );

                GUILayout.Label(
                    $"Clearance: {candidate.HasClearance}"
                );

                GUILayout.Label(
                    $"Landing Space: {candidate.HasLandingSpace}"
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

    private void DrawParkourPage()
    {
        GUILayout.Label("ACTION - SLIDING");
        GUILayout.Label("------------------------------");

        if (slideExecutor != null)
        {
            GUILayout.Label(
                $"SLIDING: {slideExecutor.IsExecuting}"
            );

            if (slideExecutor.IsExecuting)
            {
                GUILayout.Label(
                    $"Progress: {slideExecutor.Progress:F2}"
                );

                GUILayout.Label(
                    $"Speed Multiplier: {slideExecutor.GetCurrentSpeedMultiplier():F2}"
                );
            }
        }

        GUILayout.Space(5f);
        GUILayout.Label("ACTION - CLIMBING");
        GUILayout.Label("------------------------------");

        if (climbEvaluator != null)
        {
            ClimbCandidate candidate =
                climbEvaluator.Evaluate(
                    parkourDetector.CurrentObstacle
                );

            if (candidate != null)
            {
                GUILayout.Label(
                    $"Climb: {candidate.IsValid}"
                );

                GUILayout.Label(
                    $"Climb Result: {candidate.Result}"
                );
            }
            else
            {
                GUILayout.Label(
                    "No Candidate"
                );
            }
        }

        GUILayout.Space(5f);
        GUILayout.Label("ACTION - POLE SPINNING");
        GUILayout.Label("------------------------------");

        if (poleSpinEvaluator != null)
        {
            PoleSpinCandidate candidate =
                poleSpinEvaluator.Evaluate(
                    parkourDetector.CurrentObstacle
                );

            if (candidate != null)
            {
                GUILayout.Label(
                    $"Pole Spin: {candidate.IsValid}"
                );

                GUILayout.Label(
                    $"Pole Spin Result: {candidate.Result}"
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

    private void DrawBuffNerfPage()
    {
        GUILayout.Label("ACTIVE BUFFS/NERFS");
        GUILayout.Label("------------------------------");

        if (buffNerfController != null)
        {
            foreach (var activeEffect in buffNerfController.ActiveEffects)
            {
                GUILayout.Label(
                    $"Effect: {activeEffect.EffectData.name}, " +
                    $"Type: {activeEffect.EffectData.effectType}, " +
                    $"Stat: {activeEffect.EffectData.statType}, " +
                    $"Multiplier: {activeEffect.EffectData.multiplier:F2}, " +
                    $"Remaining Duration: {activeEffect.RemainingDuration:F2}"
                );
            }
        }
    }

    private void DrawRoundPage()
    {
        GUILayout.Label("ROUND SYSTEMS");
        GUILayout.Label("------------------------------");

        if (roundController != null)
        {
            GUILayout.Label(
                $"Round timer: {roundController.RemainingTime:F2}"
            );

            GUILayout.Label(
                $"Round Status: {roundController.CurrentState}"
            );

            GUILayout.Label(
                $"Current Player Role: {playerRole.CurrentRole}"
            );

            GUILayout.Label(
                $"Current AI Role: {aiRole.CurrentRole}"
            );
        }

        GUILayout.Label("GAME SYSTEM");
        GUILayout.Label("------------------------------");

        if (gameController != null)
        {
            GUILayout.Label(
                $"Current Round: {gameController.CurrentRound}"
            );

            GUILayout.Label(
                $"Game Status: {gameController.Status}"
            );
            GUILayout.Label(
                $"Current Score; Player: {scoreController.PlayerScore} , AI: {scoreController.AIScore} "
            );
        }
    }
}