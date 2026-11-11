using UnityEngine;

/// <summary>
/// Chooses the first feasible parkour action for the obstacle detected ahead.
/// </summary>
public class AIActionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParkourCandidateEvaluator parkourCandidateEvaluator;
    [SerializeField] private ParkourDetector parkourDetector;
    [SerializeField] private VaultExecutor vaultExecutor;
    [SerializeField] private SlideExecutor slideExecutor;
    [SerializeField] private ClimbEvaluator climbEvaluator;
    [SerializeField] private ClimbExecutor climbExecutor;
    [SerializeField] private CharacterMovement characterMovement;
    [SerializeField] private CharacterState characterState;
    [SerializeField] private AIMovementIntent movementIntent;
    [SerializeField] private AINavMeshDriver navMeshDriver;

    [Header("Parkour Alignment")]
    [Tooltip("AI waits until its forward direction is within this angle of the obstacle face before acting.")]
    [SerializeField, Range(1f, 45f)] private float vaultFacingAngleTolerance = 8f;

    [Header("Jump + Climb")]
    [SerializeField, Min(0.1f)] private float jumpClimbWaitTimeout = 1.5f;
    [SerializeField, Min(0.1f)] private float jumpRecoveryTimeout = 3f;

    [Header("Retry")]
    [Tooltip("Delay before retrying if every feasible action executor rejects the request, e.g. because stamina is insufficient.")]
    [SerializeField, Min(0f)] private float failedStartRetryCooldown = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool logDecisions;

    private float retryCooldownTimer;
    private bool isAligningForAction;
    private ClimbCandidate pendingJumpClimbCandidate;
    private float jumpClimbElapsed;
    private bool jumpClimbSawAirborne;
    private bool jumpActionInProgress;
    private bool jumpActionSawAirborne;
    private float jumpActionElapsed;

    private void Awake()
    {
        if (parkourCandidateEvaluator == null)
            parkourCandidateEvaluator = GetComponent<ParkourCandidateEvaluator>();

        if (parkourDetector == null)
            parkourDetector = GetComponent<ParkourDetector>();

        if (vaultExecutor == null)
            vaultExecutor = GetComponent<VaultExecutor>();

        if (slideExecutor == null)
            slideExecutor = GetComponent<SlideExecutor>();

        if (climbEvaluator == null)
            climbEvaluator = GetComponent<ClimbEvaluator>();

        if (climbExecutor == null)
            climbExecutor = GetComponent<ClimbExecutor>();

        if (characterMovement == null)
            characterMovement = GetComponent<CharacterMovement>();

        if (characterState == null)
            characterState = GetComponent<CharacterState>();

        if (movementIntent == null)
            movementIntent = GetComponent<AIMovementIntent>();

        if (navMeshDriver == null)
            navMeshDriver = GetComponent<AINavMeshDriver>();
    }

    public void ResetForSpawn()
    {
        isAligningForAction = false;
        pendingJumpClimbCandidate = null;
        jumpActionInProgress = false;
        retryCooldownTimer = 0f;
    }

    private void Start()
    {
        if (parkourCandidateEvaluator == null ||
            parkourDetector == null ||
            vaultExecutor == null ||
            slideExecutor == null ||
            climbEvaluator == null ||
            climbExecutor == null ||
            characterMovement == null ||
            characterState == null ||
            movementIntent == null ||
            navMeshDriver == null)
        {
            Debug.LogError(
                "AIActionController requires the parkour detector/evaluators, action executors, CharacterMovement, CharacterState, AIMovementIntent, and AINavMeshDriver.",
                this);

            enabled = false;
        }
    }

    private void Update()
    {
        if (GameplayPauseController.IsPaused)
            return;

        if (vaultExecutor.IsExecuting ||
            slideExecutor.IsExecuting ||
            climbExecutor.IsExecuting)
        {
            return;
        }

        if (pendingJumpClimbCandidate != null)
        {
            UpdateJumpClimbSequence();
            return;
        }

        if (jumpActionInProgress)
        {
            UpdateJumpRecovery();
            if (jumpActionInProgress)
                return;
        }

        // A timed parkour executor may finish before the CharacterController
        // lands. Don't arbitrate another action while the NavMesh proxy is
        // waiting for the driver's deferred post-landing recovery.
        if (navMeshDriver.IsParkourTraversalActive)
            return;

        // The exit owns movement until landing. An edge detected as another
        // obstacle must not start an alignment that turns the character back.
        if (navMeshDriver.IsExitingParkourSurface)
        {
            EndParkourAlignment();
            return;
        }

        retryCooldownTimer = Mathf.Max(
            0f,
            retryCooldownTimer - Time.deltaTime);

        if (characterState.CurrentState != LocomotionState.Grounded)
        {
            EndParkourAlignment();
            return;
        }

        if (retryCooldownTimer > 0f)
        {
            EndParkourAlignment();
            return;
        }

        if (!parkourCandidateEvaluator.TryGetCurrentObstacle(
                out ParkourObstacleData obstacle))
        {
            EndParkourAlignment();
            return;
        }

        Vector3 actionDirection = GetObstacleTravelDirection(obstacle);

        // Priority 1: Vault, but only when sprinting and the trajectory is valid.
        if (movementIntent.Sprinting &&
            parkourCandidateEvaluator.TryGetVaultInitiationCandidate(
                out VaultCandidate vaultCandidate))
        {
            if (AlignAndWait(vaultCandidate.TravelDirection))
                return;

            if (vaultExecutor.TryExecute(vaultCandidate))
            {
                navMeshDriver.SetParkourMovementIntent(
                    vaultCandidate.TravelDirection);
                parkourCandidateEvaluator.NotifyActionStarted(
                    obstacle.ObstacleCollider);

                if (logDecisions)
                    LogDecision(
                        $"Vault started. Direction={vaultCandidate.TravelDirection}, " +
                        $"Arc={vaultCandidate.RequiredArcHeight:F2}, " +
                        $"landing={vaultCandidate.LandingPosition}");
                return;
            }

            if (logDecisions)
                LogDecision(
                    $"Vault start failed. Candidate result={vaultCandidate.Result}");
        }

        // Priority 2: Slide only if the executor's reduced capsule clears the
        // complete path underneath and beyond the detected obstacle.
        if (movementIntent.Sprinting &&
            slideExecutor.CanPassUnderObstacle(
                obstacle,
                actionDirection,
                parkourDetector.ParkourLayer))
        {
            if (AlignAndWait(actionDirection))
                return;

            if (slideExecutor.TryExecute(actionDirection))
            {
                navMeshDriver.SetParkourMovementIntent(actionDirection);
                parkourCandidateEvaluator.NotifyActionStarted(
                    obstacle.ObstacleCollider);
                LogDecision("Slide started through the detected clearance.");
                return;
            }

            LogDecision("Slide start failed; checking the next action.");
        }

        // Priority 3: Climb a ledge that is above the jump range. Taller ledges
        // use Jump first, then Climb from the jump apex.
        ClimbCandidate climbCandidate = climbEvaluator.EvaluateForAI(
            obstacle,
            characterMovement.JumpHeight);

        if (climbCandidate.IsValid)
        {
            if (AlignAndWait(actionDirection))
                return;

            if (climbCandidate.RequiresJump)
            {
                if (TryStartJump(obstacle, climbCandidate))
                    return;

                LogDecision("Jump + Climb could not start; checking the next action.");
            }
            else if (climbExecutor.TryExecute(climbCandidate))
            {
                navMeshDriver.SetParkourMovementIntent(actionDirection);
                parkourCandidateEvaluator.NotifyActionStarted(
                    obstacle.ObstacleCollider);
                if (logDecisions)
                    LogDecision($"Climb started. Obstacle height={obstacle.Height:F2}.");
                return;
            }
            else
            {
                LogDecision("Climb start failed; checking the next action.");
            }
        }

        // Priority 4: Jump over a ledge within the configured jump reach.
        if (climbEvaluator.IsJumpCandidate(
                obstacle,
                characterMovement.JumpHeight))
        {
            if (AlignAndWait(actionDirection))
                return;

            if (TryStartJump(obstacle, null))
                return;

            LogDecision("Jump start failed.");
        }

        EndParkourAlignment();
        retryCooldownTimer = failedStartRetryCooldown;
    }

    private bool TryStartJump(
        ParkourObstacleData obstacle,
        ClimbCandidate climbAfterJump)
    {
        if (!characterMovement.TryJump())
            return false;

        // Alignment stops horizontal movement. Restore forward traversal intent
        // before pausing the NavMesh driver, so the jump can clear the obstacle.
        navMeshDriver.SetParkourMovementIntent(
            GetObstacleTravelDirection(obstacle));
        navMeshDriver.BeginParkourTraversal();
        parkourCandidateEvaluator.NotifyActionStarted(
            obstacle.ObstacleCollider);
        jumpActionInProgress = true;
        jumpActionSawAirborne = false;
        jumpActionElapsed = 0f;

        if (climbAfterJump != null)
        {
            pendingJumpClimbCandidate = climbAfterJump;
            jumpClimbElapsed = 0f;
            jumpClimbSawAirborne = false;
            if (logDecisions)
                LogDecision(
                    $"Jump started before Climb. Obstacle height={obstacle.Height:F2}, " +
                    $"top={climbAfterJump.TopPosition}.");
        }
        else if (logDecisions)
        {
            LogDecision($"Jump started. Obstacle height={obstacle.Height:F2}.");
        }

        return true;
    }

    private void UpdateJumpClimbSequence()
    {
        jumpClimbElapsed += Time.deltaTime;

        if (characterState.CurrentState == LocomotionState.Airborne)
        {
            jumpClimbSawAirborne = true;

            if (characterMovement.VerticalVelocity <= 0f)
            {
                ClimbCandidate candidate = pendingJumpClimbCandidate;
                pendingJumpClimbCandidate = null;

                if (climbExecutor.TryExecute(candidate))
                {
                    jumpActionInProgress = false;
                    parkourCandidateEvaluator.NotifyActionStarted(
                        candidate.Obstacle != null
                            ? candidate.Obstacle.ObstacleCollider
                            : null);
                    LogDecision("Jump reached its apex; Climb started.");
                }
                else
                {
                    retryCooldownTimer = failedStartRetryCooldown;
                    LogDecision("Climb after Jump failed to start.");
                }

                return;
            }
        }
        else if (jumpClimbSawAirborne &&
                 characterState.CurrentState == LocomotionState.Grounded)
        {
            pendingJumpClimbCandidate = null;
            retryCooldownTimer = failedStartRetryCooldown;
            LogDecision("Jump landed before reaching the Climb phase.");
            return;
        }

        if (jumpClimbElapsed >= jumpClimbWaitTimeout)
        {
            pendingJumpClimbCandidate = null;
            retryCooldownTimer = failedStartRetryCooldown;
            LogDecision("Jump + Climb timed out before the Climb phase.");
        }
    }

    private void UpdateJumpRecovery()
    {
        jumpActionElapsed += Time.deltaTime;

        if (characterState.CurrentState == LocomotionState.Airborne)
        {
            jumpActionSawAirborne = true;
            return;
        }

        if (characterState.CurrentState == LocomotionState.Grounded &&
            (jumpActionSawAirborne || jumpActionElapsed >= jumpRecoveryTimeout))
        {
            jumpActionInProgress = false;

            if (!navMeshDriver.RecoverAfterParkour(2f) && logDecisions)
            {
                LogDecision(
                    $"Jump NavMesh recovery failed at {transform.position}.");
            }
        }
    }

    private bool AlignAndWait(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
            return false;

        direction.Normalize();
        float facingAngle = Vector3.Angle(transform.forward, direction);
        if (facingAngle <= vaultFacingAngleTolerance)
        {
            EndParkourAlignment();
            return false;
        }

        if (!isAligningForAction && logDecisions)
        {
            LogDecision(
                $"Aligning to obstacle face. Turn={facingAngle:F1} degrees");
        }

        navMeshDriver.BeginParkourAlignment(direction);
        isAligningForAction = true;
        return true;
    }

    private Vector3 GetObstacleTravelDirection(ParkourObstacleData obstacle)
    {
        Vector3 direction = -obstacle.SurfaceNormal;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            direction = transform.forward;

        return direction.normalized;
    }

    private void EndParkourAlignment()
    {
        if (!isAligningForAction)
            return;

        navMeshDriver.EndParkourAlignment();
        isAligningForAction = false;
    }

    private void LogDecision(string message)
    {
        if (logDecisions)
            Debug.Log($"AI Action | {message}", this);
    }
}
