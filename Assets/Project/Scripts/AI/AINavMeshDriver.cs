
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AINavMeshDriver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AIMovementIntent movementIntent;
    [SerializeField] private ParkourDetector parkourDetector;
    [SerializeField] private CharacterState characterState;
    [SerializeField] private CharacterController characterController;

    [Header("Navigation")]
    [SerializeField] private float stoppingDistance = 1.5f;
    [SerializeField, Min(0f)] private float preciseStoppingDistance = 0.1f;
    [SerializeField] private float repathInterval = 0.2f;

    [Header("Agent Proxy")]
    [SerializeField] private LayerMask agentProxyObstacleLayer;
    [SerializeField, Min(0.1f)] private float agentProxySampleRadius = 1.5f;
    [SerializeField, Min(0f)] private float agentProxyGroundCheckDistance = 0.25f;

    [Header("Movement")]
    private bool sprintRequested;

    private NavMeshAgent agent;
    private float repathTimer;
    private bool hasDestination;
    private Vector3 destination;
    private bool isVaultAlignmentActive;
    private Vector3 vaultAlignmentDirection;
    public bool SprintRequested => sprintRequested;
    public bool IsOnParkourSurface { get; private set; }

    private void Awake()
    {
        if (parkourDetector == null)
            parkourDetector = GetComponent<ParkourDetector>();

        if (characterState == null)
            characterState = GetComponent<CharacterState>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        agent = GetComponent<NavMeshAgent>();

        // NavMesh computes paths; CharacterMovement moves the character.
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.stoppingDistance = stoppingDistance;
    }

    private void Start()
    {
        if (movementIntent == null)
        {
            Debug.LogError(
                "AINavMeshDriver requires Movement Intent",
                this
            );

            enabled = false;
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError(
                "AI NavMeshAgent is not positioned on a baked NavMesh.",
                this
            );

            enabled = false;
        }
    }

    private void Update()
    {
        // The vault executor owns physical movement while traversing an obstacle.
        // Keep the NavMesh simulation at its takeoff position until landing.
        if (characterState != null &&
            characterState.CurrentState == LocomotionState.Vaulting)
        {
            return;
        }

        if (isVaultAlignmentActive)
        {
            movementIntent.SetMovementIntent(
                Vector3.zero,
                vaultAlignmentDirection,
                sprintRequested);

            if (parkourDetector != null)
                parkourDetector.SetObstacleDetectionDirection(vaultAlignmentDirection);

            return;
        }

        if (!SyncAgentProxyPosition())
        {
            if (!IsOnParkourSurface)
                StopNavigationIntent();
            return;
        }

        if (IsOnParkourSurface)
            return;

        if (!agent.isOnNavMesh)
        {
            StopNavigationIntent();
            return;
        }

        if (!hasDestination)
        {
            StopNavigationIntent();
            return;
        }

        repathTimer -= Time.deltaTime;

        if (repathTimer <= 0f)
        {
            if (agent.SetDestination(destination))
                repathTimer = repathInterval;
        }

        if (agent.pathPending ||
            !agent.hasPath ||
            agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            StopNavigationIntent();
            return;
        }

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            StopNavigationIntent();
            return;
        }

        Vector3 direction = agent.steeringTarget - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            StopNavigationIntent();
            return;
        }

        direction.Normalize();

        movementIntent.SetMovementIntent(
            direction,
            direction,
            sprintRequested
        );

        if (parkourDetector != null)
        {
            parkourDetector.SetObstacleDetectionDirection(direction);
        }
    }

    public void SetDestination(Vector3 newDestination)
    {
        SetDestination(newDestination, stoppingDistance);
    }

    public void SetPreciseDestination(Vector3 newDestination)
    {
        SetDestination(newDestination, preciseStoppingDistance);
    }

    public void BeginVaultAlignment(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
            return;

        vaultAlignmentDirection = direction.normalized;
        isVaultAlignmentActive = true;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        movementIntent.SetMovementIntent(
            Vector3.zero,
            vaultAlignmentDirection,
            sprintRequested);

        if (parkourDetector != null)
            parkourDetector.SetObstacleDetectionDirection(vaultAlignmentDirection);
    }

    public void EndVaultAlignment()
    {
        isVaultAlignmentActive = false;
        vaultAlignmentDirection = Vector3.zero;
        repathTimer = 0f;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = false;
    }

    private void SetDestination(Vector3 newDestination, float arrivalDistance)
    {
        destination = newDestination;
        hasDestination = true;

        if (agent != null)
            agent.stoppingDistance = Mathf.Max(0f, arrivalDistance);
    }

    public void ClearDestination()
    {
        EndVaultAlignment();
        hasDestination = false;
        sprintRequested = false;

        if (agent != null)
        {
            if (agent.isOnNavMesh)
                agent.ResetPath();

            agent.stoppingDistance = stoppingDistance;
        }

        if (movementIntent != null)
            StopNavigationIntent();
    }

    private void OnDisable()
    {
        EndVaultAlignment();
        if (movementIntent != null)
            StopNavigationIntent();
        sprintRequested = false;
    }

    public void SetSprintIntent(bool shouldSprint)
    {
        sprintRequested = shouldSprint;
    }

    public void SetParkourMovementIntent(Vector3 direction)
    {
        if (isVaultAlignmentActive)
        {
            movementIntent.SetMovementIntent(
                Vector3.zero,
                vaultAlignmentDirection,
                sprintRequested);
            return;
        }

        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            StopNavigationIntent();
            return;
        }

        direction.Normalize();
        movementIntent.SetMovementIntent(direction, direction, sprintRequested);
        if (parkourDetector != null)
            parkourDetector.SetObstacleDetectionDirection(direction);
    }

    public void BeginParkourTraversal()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    public bool RecoverAfterParkour(float sampleRadius)
    {
        if (agent == null || !agent.enabled || !gameObject.activeInHierarchy)
            return false;

        bool synced = SyncAgentProxyPosition(sampleRadius);
        agent.ResetPath();
        repathTimer = 0f;

        if (movementIntent != null)
            movementIntent.StopMovement();

        if (synced)
            agent.isStopped = false;

        return synced;
    }

    private bool SyncAgentProxyPosition(float sampleRadius = -1f)
    {
        bool standingOnObstacle = false;
        if (characterController != null && characterController.isGrounded)
        {
            Vector3 rayOrigin = transform.position + Vector3.up * 0.05f;
            standingOnObstacle = Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit groundHit,
                agentProxyGroundCheckDistance,
                agentProxyObstacleLayer,
                QueryTriggerInteraction.Ignore);
        }

        IsOnParkourSurface = standingOnObstacle;

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return false;

        Vector3 sampleOrigin = transform.position;
        if (standingOnObstacle)
            sampleOrigin.y = agent.nextPosition.y;

        if (!NavMesh.SamplePosition(
                sampleOrigin,
                out NavMeshHit hit,
                sampleRadius > 0f ? sampleRadius : agentProxySampleRadius,
                agent.areaMask))
        {
            agent.isStopped = true;
            return false;
        }

        float proxyRadius = Mathf.Max(0.05f, agent.radius * 0.9f);
        float proxyHeight = Mathf.Max(proxyRadius * 2f, agent.height * 0.9f);
        Vector3 capsuleBottom = hit.position + Vector3.up * proxyRadius;
        Vector3 capsuleTop = hit.position + Vector3.up * (proxyHeight - proxyRadius);

        if (Physics.CheckCapsule(
                capsuleBottom,
                capsuleTop,
                proxyRadius,
                agentProxyObstacleLayer,
                QueryTriggerInteraction.Ignore))
        {
            agent.isStopped = true;
            return false;
        }

        agent.nextPosition = hit.position;
        if (agent.isStopped && !standingOnObstacle)
        {
            agent.ResetPath();
            agent.isStopped = false;
            repathTimer = 0f;
        }

        return true;
    }

    private void StopNavigationIntent()
    {
        if (movementIntent != null)
            movementIntent.StopMovement();

        if (parkourDetector != null)
            parkourDetector.ClearObstacleDetectionDirection();
    }
}
