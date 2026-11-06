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
    [SerializeField] private UtilityAIController aiController;
    [SerializeField] private StaminaUtilityEvaluator staminaEvaluator;
    [SerializeField] private AINavMeshDriver navMeshDriver;
    [SerializeField] private bool showUtilityGraphs = true;
    [SerializeField] private float graphWidth = 260f;
    [SerializeField] private float graphHeight = 110f;
    [SerializeField] private float graphMargin = 20f;

    private bool showDebug = true;

    private enum DebugPage
    {
        Player,
        Obstacle,
        Parkour,
        BuffNerf,
        Round,
        AI,
        AIToken
    }

    private DebugPage currentPage = DebugPage.AIToken;

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
            new Rect(20f, 20f, 900f, 1000f)
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

            case DebugPage.AI:
                DrawAIPage();
                break;

            case DebugPage.AIToken:
                DrawAITokenPage();
                break;
        }

        GUILayout.EndArea();

        if (currentPage == DebugPage.AI)
            DrawUtilityGraphsOverlay();
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

    private void DrawAIPage()
    {
        // GUILayout.BeginHorizontal();

        // LEFT COLUMN: AI information
        // GUILayout.BeginVertical(GUILayout.Width(340f));

        GUILayout.Label("AI DECISION");
        GUILayout.Label("------------------------------");

        if (aiController == null)
        {
            GUILayout.Label("AI Controller: Not Assigned");
            return;
        }

        GUILayout.Label($"Role: {aiController.DebugRole}");
        GUILayout.Label($"Sprinting: {navMeshDriver.SprintRequested}");
        GUILayout.Label($"Stamina Ratio: {staminaEvaluator.SprintUtility:F2}");
        GUILayout.Label($"Player Distance: {aiController.DebugPlayerDistance:F2}");
        GUILayout.Label(
            $"Navigation: {(navMeshDriver.IsProxyDetached ? "Proxy" : "Synced")} | " +
            $"Parkour Exit: {navMeshDriver.IsExitingParkourSurface}");

        GUILayout.Space(2f);
        GUILayout.Label("UTILITY SCORES");
        GUILayout.Label("------------------------------");

        GUILayout.Label($"Chase Score: {aiController.DebugChaseScore:F2}");
        GUILayout.Label($"Best Evade Score: {aiController.DebugBestEvadeScore:F2}");
        GUILayout.Label($"Selected Score: {aiController.DebugSelectedScore:F2}");
        GUILayout.Label($"Selected Distance: {aiController.DebugSelectedDistance:F2}");
        GUILayout.Label($"Selected Path Length: {aiController.DebugSelectedPathLength:F2}");
        GUILayout.Label($"Selected Path Quality: {aiController.DebugSelectedPathQuality:F2}");

        GUILayout.Space(2f);
        GUILayout.Label("CANDIDATES");
        GUILayout.Label("------------------------------");

        GUILayout.Label($"Valid: {aiController.DebugCandidateCount}");
        GUILayout.Label($"Eligible: {aiController.DebugEligibleCount}");

        foreach (string candidate in aiController.DebugCandidateLines)
        {
            GUILayout.Label(candidate);
        }

        GUILayout.Space(2f);
        GUILayout.Label("SELECTED DECISION");
        GUILayout.Label("------------------------------");

        GUILayout.Label($"Decision: {aiController.DebugSelectedDecision}");
        GUILayout.Label($"Destination: {aiController.DebugDestination}");
    }

    private void DrawAITokenPage()
    {

        GUILayout.Space(4f);
        GUILayout.Label("TOKEN AWARENESS");
        GUILayout.Label("------------------------------");
        GUILayout.Label(
            $"Scanned: {aiController.DebugTokenScannedCount} | " +
            $"Viable: {aiController.DebugTokenViableCount}");
        GUILayout.Label(
            $"Best: {aiController.DebugBestTokenName} " +
            $"({aiController.DebugBestTokenScore:F2})");
        GUILayout.Label($"Target: {aiController.DebugCurrentTokenTarget}");
        GUILayout.Label($"Target lock: {aiController.DebugTokenTargetLock}");
        GUILayout.Label($"Status: {aiController.DebugTokenStatus}");
        GUILayout.Label($"Decision: {aiController.DebugTokenDecision}");

        foreach (string candidate in aiController.DebugTokenCandidateLines)
            GUILayout.Label(candidate);
    }



    private void DrawUtilityCurveGraph(
        string title,
        AnimationCurve curve,
        float graphWidth = 280f,
        float graphHeight = 110f)
    {
        GUILayout.Space(5f);
        GUILayout.Label(title);

        if (curve == null)
        {
            GUILayout.Label("Curve not assigned.");
            return;
        }

        Rect graphRect = GUILayoutUtility.GetRect(
            graphWidth,
            graphHeight
        );

        // Reserve space for axis labels.
        graphRect.x += 24f;
        graphRect.width -= 32f;
        graphRect.y += 6f;
        graphRect.height -= 22f;

        Color previousColor = GUI.color;

        // Graph background.
        GUI.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        GUI.DrawTexture(graphRect, Texture2D.whiteTexture);

        // Grid.
        GUI.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);

        for (int i = 0; i <= 4; i++)
        {
            float x = graphRect.x + graphRect.width * i / 4f;
            float y = graphRect.y + graphRect.height * i / 4f;

            DrawGraphLine(
                new Vector2(x, graphRect.y),
                new Vector2(x, graphRect.y + graphRect.height),
                GUI.color,
                1f
            );

            DrawGraphLine(
                new Vector2(graphRect.x, y),
                new Vector2(graphRect.x + graphRect.width, y),
                GUI.color,
                1f
            );
        }

        // Draw the AnimationCurve.
        GUI.color = Color.green;

        const int segments = 60;

        Vector2 previousPoint = GetCurvePoint(
            curve, graphRect, 0f
        );

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;

            Vector2 currentPoint = GetCurvePoint(
                curve, graphRect, t
            );

            DrawGraphLine(
                previousPoint,
                currentPoint,
                GUI.color,
                2f
            );

            previousPoint = currentPoint;
        }

        GUI.color = previousColor;

        // Axis labels.
        GUI.Label(
            new Rect(graphRect.x - 27f, graphRect.y - 7f, 25f, 18f),
            "1.0"
        );

        GUI.Label(
            new Rect(graphRect.x - 27f,
                graphRect.y + graphRect.height / 2f - 7f,
                25f, 18f),
            "0.5"
        );

        GUI.Label(
            new Rect(graphRect.x - 20f,
                graphRect.y + graphRect.height - 8f,
                20f, 18f),
            "0"
        );

        GUI.Label(
            new Rect(graphRect.x - 2f,
                graphRect.y + graphRect.height + 2f,
                45f, 18f),
            "Near"
        );

        GUI.Label(
            new Rect(graphRect.x + graphRect.width - 38f,
                graphRect.y + graphRect.height + 2f,
                45f, 18f),
            "Far"
        );
    }

    private Vector2 GetCurvePoint(
        AnimationCurve curve,
        Rect graphRect,
        float normalizedDistance)
    {
        float score = Mathf.Clamp01(
            curve.Evaluate(normalizedDistance)
        );

        return new Vector2(
            graphRect.x + normalizedDistance * graphRect.width,
            graphRect.y + (1f - score) * graphRect.height
        );
    }

    private void DrawGraphLine(
        Vector2 start,
        Vector2 end,
        Color color,
        float thickness)
    {
        Vector2 difference = end - start;
        float length = difference.magnitude;

        if (length <= 0.001f)
            return;

        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;

        float angle = Mathf.Atan2(
            difference.y,
            difference.x
        ) * Mathf.Rad2Deg;

        GUI.color = color;

        GUIUtility.RotateAroundPivot(angle, start);

        GUI.DrawTexture(
            new Rect(
                start.x,
                start.y - thickness * 0.5f,
                length,
                thickness
            ),
            Texture2D.whiteTexture
        );

        GUI.matrix = previousMatrix;
        GUI.color = previousColor;
    }

    private void DrawUtilityGraphsOverlay()
    {
        if (!showUtilityGraphs || aiController == null)
            return;

        float width = Mathf.Min(graphWidth, Screen.width - 40f);
        float height = graphHeight;

        float x = Screen.width - width - graphMargin;
        float firstY = Screen.height - (height * 2f) - 75f;

        GUILayout.BeginArea(
            new Rect(x, firstY, width + 10f, height * 2f + 70f)
        );

        DrawUtilityCurveGraph(
            "Chaser Utility",
            aiController.DebugChaserDistanceCurve,
            width,
            height
        );

        DrawUtilityCurveGraph(
            "Evader Utility",
            aiController.DebugEvaderDistanceCurve,
            width,
            height
        );

        DrawUtilityCurveGraph(
            "Sprint Utility",
            staminaEvaluator.SprintUtilityCurve,
            width,
            height
        );

        GUILayout.EndArea();
    }

}
