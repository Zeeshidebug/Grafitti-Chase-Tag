using UnityEngine;
using UnityEngine.AI;

public class VaultExecutor : MonoBehaviour
{
    [Header("Vault")]
    [SerializeField] private float vaultDuration = 0.6f;

    [Header("Stamina")]
    [SerializeField] private float staminaCostRate = 0.3f;

    [SerializeField] private Animator animator;
    [SerializeField] private StaminaHUDController staminaHUDController;

    [Header("Debug")]
    [SerializeField] private bool logVaultDiagnostics = true;
    [SerializeField] private float navMeshSampleRadius = 2f;

    private CharacterController characterController;
    private CharacterState characterState;
    private StaminaSystem staminaSystem;
    private NavMeshAgent navMeshAgent;
    private AINavMeshDriver aiNavMeshDriver;

    private bool isExecuting;
    private float elapsedTime;
    private float currentArcHeight;
    private Vector3 startPosition;
    private Vector3 landingPosition;
    private Vector3 landingSurfacePosition;

    public bool IsExecuting => isExecuting;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        characterState = GetComponent<CharacterState>();
        staminaSystem = GetComponent<StaminaSystem>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        aiNavMeshDriver = GetComponent<AINavMeshDriver>();
    }

    public bool TryExecute(VaultCandidate candidate)
    {
        if (candidate == null || !candidate.IsValid || isExecuting)
            return false;

        if (characterController == null || characterState == null || staminaSystem == null)
        {
            Debug.LogError(
                "VaultExecutor requires CharacterController, CharacterState, and StaminaSystem.",
                this);
            return false;
        }

        if (vaultDuration <= 0f)
        {
            Debug.LogError("Vault duration must be greater than zero.", this);
            return false;
        }

        if (float.IsNaN(candidate.RequiredArcHeight) ||
            float.IsInfinity(candidate.RequiredArcHeight) ||
            candidate.RequiredArcHeight < 0f)
        {
            Debug.LogError(
                $"Vault candidate has an invalid arc amplitude: {candidate.RequiredArcHeight}.",
                this);
            return false;
        }

        float staminaCost = staminaSystem.MaxStamina * staminaCostRate;
        if (!staminaSystem.TryConsume(staminaCost))
        {
            if (staminaHUDController != null)
                staminaHUDController.NotifyInsufficientStamina();
            return false;
        }

        startPosition = transform.position;
        landingPosition = candidate.LandingPosition;
        landingSurfacePosition = candidate.LandingSurfacePosition;
        elapsedTime = 0f;
        currentArcHeight = candidate.RequiredArcHeight;

        if (aiNavMeshDriver != null)
            aiNavMeshDriver.BeginParkourTraversal();

        if (logVaultDiagnostics)
        {
            int areaMask = navMeshAgent != null ? navMeshAgent.areaMask : NavMesh.AllAreas;
            bool surfaceOnNavMesh = NavMesh.SamplePosition(
                landingSurfacePosition,
                out NavMeshHit surfaceHit,
                navMeshSampleRadius,
                areaMask);

            Debug.Log(
                $"VAULT START | rootStart={startPosition}, " +
                $"landingSurface={landingSurfacePosition}, rootLanding={landingPosition}, " +
                $"requiredArc={currentArcHeight:F2}, " +
                $"obstacleT={candidate.ObstacleTime:F3}, " +
                $"surfaceSampleSuccess={surfaceOnNavMesh}, " +
                $"sampledSurface={(surfaceOnNavMesh ? surfaceHit.position.ToString() : "N/A")}, " +
                $"agentOnNavMesh={(navMeshAgent != null && navMeshAgent.isOnNavMesh)}, " +
                $"agentNextPosition={(navMeshAgent != null ? navMeshAgent.nextPosition.ToString() : "No Agent")}",
                this);
        }

        isExecuting = true;
        characterState.SetState(LocomotionState.Vaulting);

        if (animator != null)
            animator.SetTrigger("Vault");

        return true;
    }

    public void CancelForSpawn()
    {
        isExecuting = false;
    }

    private void Update()
    {
        if (!isExecuting || GameplayPauseController.IsPaused)
            return;

        ExecuteVault();
    }

    private void ExecuteVault()
    {
        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / Mathf.Max(vaultDuration, 0.01f));

        Vector3 targetPosition = Vector3.Lerp(startPosition, landingPosition, t);
        float arc = Mathf.Sin(t * Mathf.PI) * currentArcHeight;
        targetPosition += Vector3.up * arc;

        Vector3 movement = targetPosition - transform.position;
        CollisionFlags collisionFlags = characterController.Move(movement);

        bool blockedBySideOrCeiling =
            (collisionFlags & (CollisionFlags.Sides | CollisionFlags.Above)) != 0;

        if (logVaultDiagnostics && blockedBySideOrCeiling)
        {
            Debug.LogWarning(
                $"VAULT COLLISION | t={t:F2}, flags={collisionFlags}, " +
                $"requestedMove={movement}, actualPosition={transform.position}",
                this);
        }

        if (t >= 1f)
            FinishVault();
    }

    private void FinishVault()
    {
        isExecuting = false;
        characterState.SetState(LocomotionState.Grounded);

        if (aiNavMeshDriver != null &&
            !aiNavMeshDriver.RecoverAfterParkour(navMeshSampleRadius))
        {
            Debug.LogWarning(
                $"VAULT NAVMESH RECOVERY FAILED | position={transform.position}, " +
                $"landingSurface={landingSurfacePosition}, agentOnNavMesh={(navMeshAgent != null && navMeshAgent.isOnNavMesh)}",
                this);
        }

        if (!logVaultDiagnostics)
            return;

        int areaMask = navMeshAgent != null ? navMeshAgent.areaMask : NavMesh.AllAreas;
        bool transformOnNavMesh = NavMesh.SamplePosition(
            transform.position,
            out NavMeshHit transformHit,
            navMeshSampleRadius,
            areaMask);

        float agentTransformDelta = navMeshAgent != null
            ? Vector3.Distance(transform.position, navMeshAgent.nextPosition)
            : -1f;

        Debug.LogWarning(
            $"VAULT END | rootActual={transform.position}, rootTarget={landingPosition}, " +
            $"landingSurface={landingSurfacePosition}, rootError={Vector3.Distance(transform.position, landingPosition):F3}, " +
            $"controllerGrounded={characterController.isGrounded}, " +
            $"transformSampleSuccess={transformOnNavMesh}, " +
            $"sampledTransform={(transformOnNavMesh ? transformHit.position.ToString() : "N/A")}, " +
            $"agentOnNavMesh={(navMeshAgent != null && navMeshAgent.isOnNavMesh)}, " +
            $"agentNextPosition={(navMeshAgent != null ? navMeshAgent.nextPosition.ToString() : "No Agent")}, " +
            $"transform-agent delta={agentTransformDelta:F3}, state={characterState.CurrentState}",
            this);
    }
}
