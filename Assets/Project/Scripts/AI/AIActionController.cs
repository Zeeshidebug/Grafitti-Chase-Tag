using UnityEngine;

/// <summary>
/// Polls the current parkour candidate every frame and starts Vault as soon
/// as ParkourCandidateEvaluator reports an open initiation window.
/// Keep slower Utility AI decision ticks separate from this per-frame action check.
/// </summary>
public class AIActionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParkourCandidateEvaluator parkourCandidateEvaluator;
    [SerializeField] private VaultExecutor vaultExecutor;
    [SerializeField] private CharacterState characterState;
    [SerializeField] private AIMovementIntent movementIntent;

    [Header("Retry")]
    [Tooltip("Delay before retrying if VaultExecutor rejects a valid candidate, e.g. because stamina is insufficient.")]
    [SerializeField, Min(0f)] private float failedStartRetryCooldown = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool logDecisions;

    private float retryCooldownTimer;

    private void Awake()
    {
        if (parkourCandidateEvaluator == null)
            parkourCandidateEvaluator = GetComponent<ParkourCandidateEvaluator>();

        if (vaultExecutor == null)
            vaultExecutor = GetComponent<VaultExecutor>();

        if (characterState == null)
            characterState = GetComponent<CharacterState>();

        if (movementIntent == null)
            movementIntent = GetComponent<AIMovementIntent>();
    }

    private void Start()
    {
        if (parkourCandidateEvaluator == null ||
            vaultExecutor == null ||
            characterState == null ||
            movementIntent == null)
        {
            Debug.LogError(
                "AIActionController requires ParkourCandidateEvaluator, VaultExecutor, CharacterState, and AIMovementIntent.",
                this);

            enabled = false;
        }
    }

    private void Update()
    {
        if (vaultExecutor.IsExecuting)
            return;

        retryCooldownTimer = Mathf.Max(
            0f,
            retryCooldownTimer - Time.deltaTime);

        if (characterState.CurrentState != LocomotionState.Grounded)
            return;

        if (!movementIntent.Sprinting)
            return;

        if (retryCooldownTimer > 0f)
            return;

        // IMPORTANT: this check intentionally runs every frame.
        // Do not put it behind UtilityAIController's slower decision interval.
        TryInitiateVault();
    }

    private void TryInitiateVault()
    {
        if (!parkourCandidateEvaluator.TryGetVaultInitiationCandidate(
                out VaultCandidate candidate))
            return;

        if (!vaultExecutor.TryExecute(candidate))
        {
            // Do not latch the obstacle when execution fails. The AI may retry
            // after this short cooldown, for example after stamina recovers.
            retryCooldownTimer = failedStartRetryCooldown;

            LogDecision(
                $"Vault start failed. Candidate result={candidate.Result}");
            return;
        }

        // Latch only after the executor successfully acquires movement ownership.
        parkourCandidateEvaluator.NotifyVaultStarted();

        LogDecision(
            $"Vault started. Arc={candidate.RequiredArcHeight:F2}, " +
            $"landing={candidate.LandingPosition}");
    }

    private void LogDecision(string message)
    {
        if (logDecisions)
            Debug.Log($"AI Action | {message}", this);
    }
}
