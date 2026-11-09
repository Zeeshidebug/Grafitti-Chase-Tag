
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
    private bool isParkourAlignmentActive;
    private Vector3 parkourAlignmentDirection;
    private Vector3 movementDirectionBeforeAlignment;
    private bool isParkourTraversalActive;
    private bool parkourRecoveryPending;
    private float pendingRecoverySampleRadius;
    private bool proxySyncFailed;
    private NavMeshPath characterPath;
    private NavMeshQueryFilter characterPathFilter;
    private Vector3[] characterPathCorners;
    private int characterPathCornerCount;
    private int characterPathCornerIndex;
    private bool characterPathValid;
    private bool pathUsesCharacterOrigin;
    private Vector3 parkourTravelDirection;
    private int navigationOriginFrame = -1;
    private Vector3 navigationOriginFeet;
    private Vector3 sampledNavigationOrigin;
    private bool navigationOriginFound;
    private NavMeshPath surfaceConnectionPath;
    private float nextSurfaceConnectionCheckTime;
    public bool SprintRequested => sprintRequested;
    public bool IsParkourTraversalActive => isParkourTraversalActive;
    public bool IsOnParkourSurface { get; private set; }
    public bool IsExitingParkourSurface { get; private set; }
    public bool IsProxyDetached => isParkourTraversalActive || IsOnParkourSurface ||
        IsExitingParkourSurface || proxySyncFailed ||
        (characterController != null && !characterController.isGrounded);
    public Vector3 NavigationOrigin => TryGetNavigationOrigin(out Vector3 origin)
        ? origin : transform.position;
    public string DebugNavigationStatus { get; private set; } = "Starting";

    private void Awake()
    {
        characterPath = new NavMeshPath();

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
        // Planning continues while an executor owns physical movement.
        UpdateNavigationPath();

        if (isParkourTraversalActive)
        {
            DebugNavigationStatus = parkourRecoveryPending
                ? "Character planning; waiting for landing" : "Character planning; action executing";
            if (parkourRecoveryPending && IsCharacterGrounded())
                CompleteParkourRecovery(pendingRecoverySampleRadius);

            return;
        }

        // Parkour executors own physical movement while traversing an obstacle.
        if (characterState != null &&
            (characterState.CurrentState == LocomotionState.Vaulting ||
             characterState.CurrentState == LocomotionState.Climbing))
        {
            return;
        }

        if (isParkourAlignmentActive)
        {
            movementIntent.SetMovementIntent(
                Vector3.zero,
                parkourAlignmentDirection,
                sprintRequested);

            if (parkourDetector != null)
                parkourDetector.SetObstacleDetectionDirection(parkourAlignmentDirection);

            return;
        }

        // Keep the character moving through the air after leaving a parkour
        // surface. The NavMesh proxy must wait for landing before resyncing.
        if (characterController != null && !characterController.isGrounded)
        {
            DebugNavigationStatus = IsExitingParkourSurface
                ? "Exiting disconnected parkour surface; waiting for landing"
                : "Character planning; airborne movement preserved";
            if (IsExitingParkourSurface)
                ContinueParkourTravel();

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            return;
        }

        proxySyncFailed = !SyncAgentProxyPosition();

        if (IsProxyDetached != pathUsesCharacterOrigin)
            UpdateNavigationPath();

        if (IsProxyDetached)
        {
            FollowCharacterPath();
            return;
        }

        parkourTravelDirection = Vector3.zero;

        if (!agent.enabled || !agent.isOnNavMesh)
        {
            DebugNavigationStatus = "Proxy off NavMesh; cannot calculate path";
            StopNavigationIntent();
            return;
        }

        if (!hasDestination)
        {
            DebugNavigationStatus = "No destination";
            StopNavigationIntent();
            return;
        }

        if (agent.pathPending ||
            !agent.hasPath ||
            agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            DebugNavigationStatus = "Waiting for complete path";
            StopNavigationIntent();
            return;
        }

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            DebugNavigationStatus = "Destination reached";
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
        DebugNavigationStatus = "Following path";

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

    private void UpdateNavigationPath()
    {
        // A complete path on an isolated obstacle top cannot take us off it.
        // Finish the physical traversal before selecting another local route.
        if (IsExitingParkourSurface)
            return;

        bool useCharacterOrigin = IsProxyDetached;
        if (pathUsesCharacterOrigin != useCharacterOrigin)
        {
            pathUsesCharacterOrigin = useCharacterOrigin;
            characterPathValid = false;
            repathTimer = 0f;
            if (useCharacterOrigin && agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

        if (agent == null || !agent.enabled || !hasDestination)
            return;

        repathTimer -= Time.deltaTime;
        if (repathTimer > 0f)
            return;

        if (useCharacterOrigin)
        {
            // Script reload during Play Mode can clear this native object
            // without calling Awake again.
            characterPath ??= new NavMeshPath();
            characterPathValid = TryCalculateNavigationPath(destination, characterPath) &&
                characterPath.status == NavMeshPathStatus.PathComplete;
            characterPathCornerCount = characterPathValid
                ? NavMeshPathUtility.ReadCorners(characterPath, ref characterPathCorners) : 0;
            // Corner zero is the sampled start, which may be below/behind the
            // character. Never steer back to that projected start point.
            characterPathCornerIndex = characterPathCornerCount > 1 ? 1 : 0;
        }
        else if (agent.isOnNavMesh && !agent.pathPending)
        {
            agent.SetDestination(destination);
        }
        repathTimer = repathInterval;
    }

    public bool TryGetNavigationOrigin(out Vector3 origin)
    {
        origin = transform.position;
        if (agent == null || !agent.enabled)
            return false;

        if (!IsProxyDetached)
        {
            if (!agent.isOnNavMesh)
                return false;
            origin = agent.nextPosition;
            return true;
        }

        Vector3 feet = transform.position +
            CharacterControllerGeometry.GetWorldBottomOffset(characterController);
        if (navigationOriginFrame != Time.frameCount || navigationOriginFeet != feet)
        {
            navigationOriginFrame = Time.frameCount;
            navigationOriginFeet = feet;
            var filter = new NavMeshQueryFilter
            {
                agentTypeID = agent.agentTypeID,
                areaMask = agent.areaMask
            };
            navigationOriginFound = NavMesh.SamplePosition(
                feet, out NavMeshHit hit, agentProxySampleRadius, filter);

            // Obstacles may have no NavMesh on top. Project the CHARACTER'S XZ
            // onto the proxy's navigation level; do not reuse the proxy's XZ.
            if (!navigationOriginFound && agent.isOnNavMesh)
            {
                Vector3 projectedFeet = feet;
                projectedFeet.y = agent.nextPosition.y;
                navigationOriginFound = NavMesh.SamplePosition(
                    projectedFeet, out hit, agentProxySampleRadius, filter);
            }

            sampledNavigationOrigin = navigationOriginFound ? hit.position : feet;
        }

        origin = sampledNavigationOrigin;
        return navigationOriginFound;
    }

    public bool TryCalculateNavigationPath(Vector3 target, NavMeshPath path)
    {
        if (!TryGetNavigationOrigin(out Vector3 origin))
            return false;

        if (!IsProxyDetached)
            return agent.CalculatePath(target, path);

        characterPathFilter.agentTypeID = agent.agentTypeID;
        characterPathFilter.areaMask = agent.areaMask;
        for (int area = 0; area < 32; area++)
            characterPathFilter.SetAreaCost(area, agent.GetAreaCost(area));

        return NavMesh.CalculatePath(origin, target, characterPathFilter, path);
    }

    private void FollowCharacterPath()
    {
        if (IsExitingParkourSurface)
        {
            DebugNavigationStatus = "Exiting disconnected parkour surface";
            ContinueParkourTravel();
            return;
        }

        if (!hasDestination || !characterPathValid || characterPathCornerCount == 0)
        {
            DebugNavigationStatus = "No character path; continuing traversal and retrying";
            ContinueParkourTravel();
            return;
        }

        while (characterPathCornerIndex < characterPathCornerCount)
        {
            Vector3 corner = characterPathCorners[characterPathCornerIndex];
            Vector3 direction = corner - transform.position;
            direction.y = 0f;
            bool lastCorner = characterPathCornerIndex == characterPathCornerCount - 1;
            float arrivalDistance = lastCorner
                ? Mathf.Max(0.05f, agent.stoppingDistance) : Mathf.Max(0.1f, agent.radius);

            if (direction.sqrMagnitude > arrivalDistance * arrivalDistance)
            {
                direction.Normalize();
                movementIntent.SetMovementIntent(direction, direction, sprintRequested);
                if (parkourDetector != null)
                    parkourDetector.SetObstacleDetectionDirection(direction);
                DebugNavigationStatus = "Following path from character";
                return;
            }

            if (lastCorner)
            {
                Vector3 feet = transform.position +
                    CharacterControllerGeometry.GetWorldBottomOffset(characterController);
                if (Mathf.Abs(feet.y - corner.y) <= agentProxyGroundCheckDistance)
                {
                    DebugNavigationStatus = "Character reached destination";
                    StopNavigationIntent();
                }
                else
                {
                    DebugNavigationStatus = "Above destination; continuing traversal";
                    ContinueParkourTravel();
                }
                return;
            }

            characterPathCornerIndex++;
        }
    }

    private void ContinueParkourTravel()
    {
        if (parkourTravelDirection.sqrMagnitude > 0.001f)
        {
            movementIntent.SetMovementIntent(
                parkourTravelDirection, parkourTravelDirection, sprintRequested);
            if (parkourDetector != null)
                parkourDetector.SetObstacleDetectionDirection(parkourTravelDirection);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && IsExitingParkourSurface)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, parkourTravelDirection * 3f);
            return;
        }

        if (!Application.isPlaying || !IsProxyDetached || !characterPathValid ||
            characterPathCornerCount == 0)
            return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(characterPathCorners[0], 0.15f);
        for (int i = 1; i < characterPathCornerCount; i++)
            Gizmos.DrawLine(characterPathCorners[i - 1], characterPathCorners[i]);

        if (characterPathCornerIndex < characterPathCornerCount)
            Gizmos.DrawLine(transform.position, characterPathCorners[characterPathCornerIndex]);
    }

    public void SetDestination(Vector3 newDestination)
    {
        SetDestination(newDestination, stoppingDistance);
    }

    public void SetPreciseDestination(Vector3 newDestination)
    {
        SetDestination(newDestination, preciseStoppingDistance);
    }

    public void BeginParkourAlignment(Vector3 direction)
    {
        if (IsExitingParkourSurface)
            return;

        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
            return;

        if (!isParkourAlignmentActive && movementIntent != null)
        {
            movementDirectionBeforeAlignment =
                movementIntent.MovementDirection;
        }

        parkourAlignmentDirection = direction.normalized;
        isParkourAlignmentActive = true;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        movementIntent.SetMovementIntent(
            Vector3.zero,
            parkourAlignmentDirection,
            sprintRequested);

        if (parkourDetector != null)
            parkourDetector.SetObstacleDetectionDirection(parkourAlignmentDirection);
    }

    public void EndParkourAlignment()
    {
        if (!isParkourAlignmentActive)
            return;

        Vector3 alignedDirection = parkourAlignmentDirection;
        isParkourAlignmentActive = false;
        parkourAlignmentDirection = Vector3.zero;
        repathTimer = 0f;

        if (movementIntent != null)
        {
            movementIntent.SetMovementIntent(
                movementDirectionBeforeAlignment,
                alignedDirection,
                sprintRequested);
        }

        movementDirectionBeforeAlignment = Vector3.zero;

        if (IsExitingParkourSurface)
            ContinueParkourTravel();

        if (agent != null && agent.enabled && agent.isOnNavMesh && !IsProxyDetached)
            agent.isStopped = false;
    }

    private void SetDestination(Vector3 newDestination, float arrivalDistance)
    {
        if (!hasDestination || (destination - newDestination).sqrMagnitude > 0.0001f)
        {
            characterPathValid = false;
            repathTimer = 0f;
        }
        destination = newDestination;
        hasDestination = true;

        if (agent != null)
            agent.stoppingDistance = Mathf.Max(0f, arrivalDistance);
    }

    public void ClearDestination()
    {
        EndParkourAlignment();
        SetParkourSurfaceExit(false);
        nextSurfaceConnectionCheckTime = 0f;
        hasDestination = false;
        characterPathValid = false;
        parkourTravelDirection = Vector3.zero;
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

    public void ResetForSpawn(Vector3 feet)
    {
        ClearDestination();
        isParkourTraversalActive = false;
        parkourRecoveryPending = false;
        IsOnParkourSurface = false;
        navigationOriginFrame = -1;
        repathTimer = 0f;
        movementIntent.SetMovementIntent(Vector3.zero, transform.forward);
        movementIntent.SetJumpPressed(false);

        Vector3 rootPosition = transform.position;
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };
        proxySyncFailed = !NavMesh.SamplePosition(feet, out NavMeshHit hit, agentProxySampleRadius, filter)
            || !agent.Warp(hit.position);
        transform.position = rootPosition;
        if (proxySyncFailed)
        {
            Debug.LogWarning($"No NavMesh at spawn point {feet}.", this);
            return;
        }

        agent.ResetPath();
        agent.velocity = Vector3.zero;
        agent.isStopped = false;
    }

    private void OnDisable()
    {
        EndParkourAlignment();
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
        if (isParkourAlignmentActive)
        {
            movementIntent.SetMovementIntent(
                Vector3.zero,
                parkourAlignmentDirection,
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
        parkourTravelDirection = direction;
        movementIntent.SetMovementIntent(direction, direction, sprintRequested);
        if (parkourDetector != null)
            parkourDetector.SetObstacleDetectionDirection(direction);
    }

    public void BeginParkourTraversal()
    {
        SetParkourSurfaceExit(false);
        nextSurfaceConnectionCheckTime = 0f;
        parkourTravelDirection = movementIntent.MovementDirection;
        if (parkourTravelDirection.sqrMagnitude <= 0.001f)
            parkourTravelDirection = transform.forward;
        parkourTravelDirection.y = 0f;
        parkourTravelDirection.Normalize();
        parkourRecoveryPending = false;
        isParkourTraversalActive = true;

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    public bool RecoverAfterParkour(float sampleRadius)
    {
        if (agent == null || !agent.enabled || !gameObject.activeInHierarchy)
        {
            parkourRecoveryPending = false;
            isParkourTraversalActive = false;
            return false;
        }

        // Executors can finish their timed trajectory while the character is
        // still falling. Keep the proxy paused and leave movement intent alone
        // until the CharacterController confirms a real landing.
        if (!IsCharacterGrounded())
        {
            pendingRecoverySampleRadius = sampleRadius;
            parkourRecoveryPending = true;
            isParkourTraversalActive = true;
            return true;
        }

        return CompleteParkourRecovery(sampleRadius);
    }

    private bool CompleteParkourRecovery(float sampleRadius)
    {
        parkourRecoveryPending = false;
        bool synced = SyncAgentProxyPosition(sampleRadius);
        proxySyncFailed = !synced;
        if (agent.isOnNavMesh)
            agent.ResetPath();
        repathTimer = 0f;

        if (movementIntent != null && synced && !IsOnParkourSurface)
            movementIntent.StopMovement();

        if (synced)
            agent.isStopped = IsOnParkourSurface;

        isParkourTraversalActive = false;
        return synced || IsOnParkourSurface;
    }

    private bool IsCharacterGrounded()
    {
        return characterController != null && characterController.isGrounded;
    }

    private bool SyncAgentProxyPosition(float sampleRadius = -1f)
    {
        Vector3 feetPosition = transform.position +
            CharacterControllerGeometry.GetWorldBottomOffset(characterController);
        bool hasGroundSupport = false;
        bool standingOnObstacle = false;
        if (characterController != null && characterController.isGrounded)
        {
            float probeOffset = Mathf.Max(0.05f, characterController.skinWidth * 2f);
            Vector3 rayOrigin = feetPosition + Vector3.up * probeOffset;
            // Require an actual surface under the feet. At an edge the capsule
            // can still report grounded while this ray no longer hits anything.
            hasGroundSupport = Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit groundHit,
                agentProxyGroundCheckDistance + probeOffset,
                Physics.DefaultRaycastLayers & ~(1 << gameObject.layer),
                QueryTriggerInteraction.Ignore);
            standingOnObstacle = hasGroundSupport &&
                (agentProxyObstacleLayer.value & (1 << groundHit.collider.gameObject.layer)) != 0;
        }

        IsOnParkourSurface = standingOnObstacle;

        if (agent == null || !agent.enabled)
            return false;

        float proxySampleRadius = sampleRadius > 0f
            ? sampleRadius
            : agentProxySampleRadius;

        if (hasGroundSupport && !standingOnObstacle &&
            TryReattachProxyAtLanding(feetPosition, proxySampleRadius))
        {
            SetParkourSurfaceExit(false);
            nextSurfaceConnectionCheckTime = 0f;
            return true;
        }

        if (!agent.isOnNavMesh)
            return false;

        // A missed edge ray or an unsafe landing sample must not release the
        // exit direction. Keep it until the proxy really reconnects at landing.
        if (!standingOnObstacle)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            return false;
        }

        UpdateParkourSurfaceExit(feetPosition);
        Vector3 sampleOrigin = feetPosition;
        sampleOrigin.y = agent.nextPosition.y;

        if (!TryFindSafeProxyPosition(
                sampleOrigin,
                proxySampleRadius,
                out NavMeshHit hit))
        {
            agent.isStopped = true;
            return false;
        }

        agent.nextPosition = hit.position;
        // nextPosition is constrained by connectivity; assignment alone does
        // not prove that the proxy reached a different NavMesh floor/island.
        if (Vector3.Distance(agent.nextPosition, hit.position) > proxySampleRadius)
        {
            agent.isStopped = true;
            return false;
        }

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        return true;
    }

    private bool TryReattachProxyAtLanding(Vector3 feetPosition, float sampleRadius)
    {
        // A nearby sample on another floor or beyond a wall is not a landing.
        float landingRadius = Mathf.Min(sampleRadius, Mathf.Max(0.1f, agent.radius));
        float heightTolerance = agentProxyGroundCheckDistance + characterController.skinWidth * 2f;
        if (!TrySampleSafeProxyPosition(feetPosition, landingRadius, out NavMeshHit hit) ||
            Mathf.Abs(hit.position.y - feetPosition.y) > heightTolerance)
            return false;

        bool wasDetached = IsProxyDetached;
        if (agent.isOnNavMesh)
            agent.nextPosition = hit.position;

        // nextPosition cannot cross disconnected polygons. Once the character
        // has landed on clear NavMesh, rebind the proxy there instead of leaving
        // it at the old edge. Preserve the CharacterController's physical pose.
        bool rebound = false;
        if (!IsProxyAtLanding(hit.position, heightTolerance))
        {
            Vector3 characterPosition = transform.position;
            bool warped = agent.Warp(hit.position);
            transform.position = characterPosition;
            if (!warped || !IsProxyAtLanding(hit.position, heightTolerance))
                return false;

            rebound = true;
        }

        if (wasDetached || rebound || agent.isStopped)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            characterPathValid = false;
            navigationOriginFrame = -1;
            repathTimer = 0f;
        }

        agent.isStopped = false;
        return true;
    }

    private bool IsProxyAtLanding(Vector3 landingPosition, float heightTolerance)
    {
        if (!agent.isOnNavMesh)
            return false;

        Vector3 delta = agent.nextPosition - landingPosition;
        // The agent's configured base offset is not a failure to reconnect.
        if (Mathf.Abs(delta.y) > Mathf.Abs(agent.baseOffset) + heightTolerance)
            return false;

        delta.y = 0f;
        float horizontalTolerance = Mathf.Max(0.05f, characterController.skinWidth);
        return delta.sqrMagnitude <= horizontalTolerance * horizontalTolerance;
    }

    private void UpdateParkourSurfaceExit(Vector3 feetPosition)
    {
        if (IsExitingParkourSurface || Time.time < nextSurfaceConnectionCheckTime ||
            (!hasDestination && parkourTravelDirection.sqrMagnitude <= 0.001f))
            return;

        nextSurfaceConnectionCheckTime = Time.time + Mathf.Max(0.05f, repathInterval);
        if (feetPosition.y <= agent.nextPosition.y + agentProxyGroundCheckDistance)
            return;

        var filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };
        float surfaceHeightTolerance = agentProxyGroundCheckDistance +
            characterController.skinWidth * 2f;
        bool hasTopNavMesh = NavMesh.SamplePosition(
            feetPosition, out NavMeshHit topHit, agentProxySampleRadius, filter) &&
            Mathf.Abs(topHit.position.y - feetPosition.y) <= surfaceHeightTolerance;

        // Allocate on the main thread, never in a MonoBehaviour field initializer.
        surfaceConnectionPath ??= new NavMeshPath();
        bool connectedToGround = hasTopNavMesh && NavMesh.CalculatePath(
            topHit.position, agent.nextPosition, filter, surfaceConnectionPath) &&
            surfaceConnectionPath.status == NavMeshPathStatus.PathComplete;

        if (!connectedToGround)
            SetParkourSurfaceExit(true);
    }

    private void SetParkourSurfaceExit(bool exiting)
    {
        if (IsExitingParkourSurface == exiting)
            return;

        IsExitingParkourSurface = exiting;
        characterPathValid = false;
        repathTimer = 0f;
        navigationOriginFrame = -1;

        if (exiting && parkourTravelDirection.sqrMagnitude <= 0.001f)
        {
            parkourTravelDirection = transform.forward;
            parkourTravelDirection.y = 0f;
            parkourTravelDirection.Normalize();
        }

        if (exiting)
        {
            EndParkourAlignment();
            ContinueParkourTravel();
        }
    }

    private bool TryFindSafeProxyPosition(
        Vector3 sampleOrigin,
        float sampleRadius,
        out NavMeshHit safeHit)
    {
        if (TrySampleSafeProxyPosition(sampleOrigin, sampleRadius, out safeHit))
            return true;

        Vector3 forward = movementIntent != null
            ? movementIntent.MovementDirection
            : transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.001f)
            forward = transform.forward;
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        float offset = Mathf.Clamp(sampleRadius * 0.5f, 0.5f, 1.25f);
        float localSampleRadius = Mathf.Max(0.5f, offset * 0.5f);

        if (TrySampleSafeProxyPosition(
                sampleOrigin + forward * offset,
                localSampleRadius,
                out safeHit) ||
            TrySampleSafeProxyPosition(
                sampleOrigin - forward * offset,
                localSampleRadius,
                out safeHit) ||
            TrySampleSafeProxyPosition(
                sampleOrigin + right * offset,
                localSampleRadius,
                out safeHit) ||
            TrySampleSafeProxyPosition(
                sampleOrigin - right * offset,
                localSampleRadius,
                out safeHit))
        {
            return true;
        }

        safeHit = default;
        return false;
    }

    private bool TrySampleSafeProxyPosition(
        Vector3 sampleOrigin,
        float sampleRadius,
        out NavMeshHit hit)
    {
        var filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };
        if (!NavMesh.SamplePosition(
                sampleOrigin,
                out hit,
                sampleRadius,
                filter))
        {
            return false;
        }

        return IsAgentProxyCapsuleClear(hit.position);
    }

    private bool IsAgentProxyCapsuleClear(Vector3 position)
    {
        float proxyRadius = Mathf.Max(0.05f, agent.radius * 0.9f);
        float proxyHeight = Mathf.Max(proxyRadius * 2f, agent.height * 0.9f);
        Vector3 capsuleBottom = position + Vector3.up * proxyRadius;
        Vector3 capsuleTop = position + Vector3.up * (proxyHeight - proxyRadius);

        return !Physics.CheckCapsule(
                capsuleBottom,
                capsuleTop,
                proxyRadius,
                agentProxyObstacleLayer,
                QueryTriggerInteraction.Ignore);
    }

    private void StopNavigationIntent()
    {
        if (movementIntent != null)
            movementIntent.StopMovement();

        if (parkourDetector != null)
            parkourDetector.ClearObstacleDetectionDirection();
    }
}
