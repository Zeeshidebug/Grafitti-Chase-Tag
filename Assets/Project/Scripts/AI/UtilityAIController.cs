
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class UtilityAIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoleController roleController;
    [SerializeField] private Transform player;
    [SerializeField] private AINavMeshDriver navMeshDriver;
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private CharacterState characterState;

    [Header("Decision")]
    [SerializeField, Min(0.05f)] private float decisionInterval = 0.5f;
    [SerializeField, Min(0.1f)] private float destinationSampleRadius = 2f;

    [Header("Evade Candidates")]
    [SerializeField, Min(0.1f)] private float evadeSearchRadius = 7f;
    [SerializeField, Min(4)] private int evadeCandidateCount = 8;

    [Header("Distance Utility")]
    [Tooltip("Distance at which Chaser distance utility reaches its minimum.")]
    [SerializeField, Min(0.1f)] private float chaseScoreDistance = 20f;

    [Tooltip("Distance at which Evader distance utility reaches its maximum.")]
    [SerializeField, Min(0.1f)] private float evadeScoreDistance = 20f;

    [Tooltip("Input: normalized distance (0 = near, 1 = far). Output: utility score.")]
    [SerializeField]
    private AnimationCurve chaserDistanceCurve =
        AnimationCurve.Linear(0f, 1f, 1f, 0f);

    [SerializeField]
    private AnimationCurve evaderDistanceCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Path Quality Utility")]
    [SerializeField, Range(0f, 1f)]
    private float evadeDistanceWeight = 0.7f;

    [SerializeField, Range(0f, 1f)]
    private float evadePathQualityWeight = 0.3f;

    [Header("Hybrid Selection")]
    [SerializeField] private bool useWeightedRandomForEvade = true;

    [Tooltip("Only candidates within this score gap from the best are eligible.")]
    [SerializeField, Range(0f, 1f)]
    private float evadeScoreTolerance = 0.08f;

    [Header("Commitment & Hysteresis")]
    [SerializeField] private bool useDestinationHysteresis = true;

    [Tooltip("Minimum utility advantage required to switch destinations.")]
    [SerializeField, Range(0f, 1f)]
    private float evadeSwitchingMargin = 0.08f;

    [Tooltip("Minimum time to keep a selected destination, if still valid.")]
    [SerializeField, Min(0f)]
    private float minDestinationCommitment = 1f;

    [Tooltip("Consider the destination reached within this distance.")]
    [SerializeField, Min(0.1f)]
    private float destinationArrivalDistance = 1.5f;

    [Header("Stamina Awareness")]
    [SerializeField] private StaminaUtilityEvaluator staminaEvaluator;

    [SerializeField, Range(0f, 1f)]
    private float sprintStartRatio = 0.55f;

    [SerializeField, Range(0f, 1f)]
    private float sprintStopRatio = 0.35f;

    private bool isSprintRequested;

    [Header("Debug")]
    [SerializeField] private bool logDecisions = true;
    [SerializeField] private bool logEvadeCandidates;

    private float decisionTimer;
    private CharacterRole lastRole = CharacterRole.None;
    private Vector3 currentDestination;
    private bool hasCurrentDestination;
    private float commitmentTimer;

    // Read-only debug data for DebugHUD.
    public string DebugRole =>
        roleController != null ? roleController.CurrentRole.ToString() : "Missing";

    public float DebugPlayerDistance { get; private set; }
    public float DebugChaseScore { get; private set; }
    public float DebugBestEvadeScore { get; private set; }
    public float DebugSelectedScore { get; private set; }
    public float DebugSelectedDistance { get; private set; }
    public float DebugSelectedPathLength { get; private set; }
    public float DebugSelectedPathQuality { get; private set; }

    public int DebugCandidateCount { get; private set; }
    public int DebugEligibleCount { get; private set; }

    public string DebugSelectedDecision { get; private set; } = "None";
    public Vector3 DebugDestination { get; private set; }

    public List<string> DebugCandidateLines { get; } = new List<string>();

    // Diagnostics for investigating post-vault NavMesh failures.
    public int DebugSampleSuccessCount { get; private set; }
    public int DebugSampleFailureCount { get; private set; }
    public int DebugPathCompleteCount { get; private set; }
    public int DebugPathPartialCount { get; private set; }
    public int DebugPathInvalidCount { get; private set; }
    public string DebugLastCandidateFailure { get; private set; } = "None";
    public float DebugAgentTransformDelta { get; private set; }

    public AnimationCurve DebugChaserDistanceCurve =>
    chaserDistanceCurve;

    public AnimationCurve DebugEvaderDistanceCurve =>
    evaderDistanceCurve;
    // End read-only debug data.

    private struct EvadeCandidate
    {
        public Vector3 Position;
        public float Score;
        public float DistanceFromPlayer;
        public float PathLength;
        public float PathQuality;

        public EvadeCandidate(
            Vector3 position,
            float score,
            float distanceFromPlayer,
            float pathLength,
            float pathQuality)
        {
            Position = position;
            Score = score;
            DistanceFromPlayer = distanceFromPlayer;
            PathLength = pathLength;
            PathQuality = pathQuality;
        }
    }

    private void Awake()
    {
        if (navMeshAgent == null)
            navMeshAgent = GetComponent<NavMeshAgent>();

        if (navMeshDriver == null)
            navMeshDriver = GetComponent<AINavMeshDriver>();

        if (staminaEvaluator == null)
            staminaEvaluator = GetComponent<StaminaUtilityEvaluator>();

        if (characterState == null)
            characterState = GetComponent<CharacterState>();
    }

    private void Start()
    {
        if (roleController == null ||
            player == null ||
            navMeshDriver == null ||
            navMeshAgent == null)
        {
            Debug.LogError(
                "UtilityAIController is missing references.",
                this
            );

            enabled = false;
            return;
        }

        decisionTimer = 0f;
    }

    private void Update()
    {
        if (characterState != null &&
            characterState.CurrentState != LocomotionState.Grounded)
        {
            return;
        }

        UpdateSprintDecision();

        if (navMeshDriver.IsOnParkourSurface)
        {
            Vector3 parkourDirection = roleController.CurrentRole switch
            {
                CharacterRole.Chaser => player.position - transform.position,
                CharacterRole.Evader => transform.position - player.position,
                _ => Vector3.zero
            };

            navMeshDriver.SetParkourMovementIntent(parkourDirection);
            DebugSelectedDecision = $"{roleController.CurrentRole} (Parkour Movement)";
            return;
        }

        if (!navMeshAgent.isOnNavMesh)
        {
            ClearCurrentDestination();
            DebugSelectedDecision = "Off NavMesh";
            return;
        }

        decisionTimer -= Time.deltaTime;
        commitmentTimer = Mathf.Max(
            0f,
            commitmentTimer - Time.deltaTime
        );

        bool roleChanged = roleController.CurrentRole != lastRole;

        if (!roleChanged && decisionTimer > 0f)
            return;

        if (roleChanged)
        {
            ClearCurrentDestination();
            DebugSelectedDecision = "Role Changed";
        }

        lastRole = roleController.CurrentRole;
        decisionTimer = decisionInterval;

        switch (roleController.CurrentRole)
        {
            case CharacterRole.Chaser:
                ChooseChaseDestination();
                break;

            case CharacterRole.Evader:
                ChooseEvadeDestination();
                break;

            default:
                navMeshDriver.ClearDestination();

                if (logDecisions)
                    Debug.Log("AI Decision: Role None - navigation cleared.", this);

                break;
        }
    }

    private void ChooseChaseDestination()
    {
        DebugSelectedDecision = "Chase";

        float playerDistance = Vector3.Distance(
            transform.position,
            player.position
        );

        float score = EvaluateChaserDistance(playerDistance);

        if (!NavMesh.SamplePosition(
                player.position,
                out NavMeshHit hit,
                destinationSampleRadius,
                navMeshAgent.areaMask))
        {
            ClearCurrentDestination();
            DebugDestination = Vector3.zero;

            if (logDecisions)
            {
                Debug.Log(
                    $"AI Chaser | Distance: {playerDistance:F2} | " +
                    $"Chase Score: {score:F2} | No valid destination.",
                    this
                );
            }

            return;
        }

        navMeshDriver.SetDestination(hit.position);

        DebugPlayerDistance = playerDistance;
        DebugChaseScore = score;
        DebugDestination = hit.position;
    }


    private void ChooseEvadeDestination()
    {
        DebugSelectedDecision = "Evade";
        DebugCandidateLines.Clear();

        // Reset diagnostics every time we run a fresh evade search.
        DebugSampleSuccessCount = 0;
        DebugSampleFailureCount = 0;
        DebugPathCompleteCount = 0;
        DebugPathPartialCount = 0;
        DebugPathInvalidCount = 0;
        DebugLastCandidateFailure = "None";
        DebugAgentTransformDelta = Vector3.Distance(transform.position, navMeshAgent.nextPosition);

        Vector3 awayFromPlayer = transform.position - player.position;
        awayFromPlayer.y = 0f;

        if (awayFromPlayer.sqrMagnitude < 0.001f)
            awayFromPlayer = transform.forward;

        awayFromPlayer.Normalize();

        var candidates = new List<EvadeCandidate>();

        int count = Mathf.Max(4, evadeCandidateCount);

        for (int i = 0; i < count; i++)
        {
            float angle = i * 360f / count;

            Vector3 direction =
                Quaternion.AngleAxis(angle, Vector3.up) * awayFromPlayer;

            Vector3 rawPosition =
                transform.position + direction * evadeSearchRadius;

            if (!NavMesh.SamplePosition(
                    rawPosition,
                    out NavMeshHit hit,
                    destinationSampleRadius,
                    navMeshAgent.areaMask))
            {
                DebugSampleFailureCount++;
                DebugLastCandidateFailure = $"SamplePosition failed for candidate {i}; raw={rawPosition}";
                if (logEvadeCandidates)
                    DebugCandidateLines.Add($"Candidate {i}: SAMPLE FAILED | raw={rawPosition}");
                continue;
            }

            DebugSampleSuccessCount++;
            NavMeshPath path = new NavMeshPath();

            bool pathFound = navMeshAgent.CalculatePath(hit.position, path);

            if (!pathFound || path.status != NavMeshPathStatus.PathComplete)
            {
                if (path.status == NavMeshPathStatus.PathPartial)
                    DebugPathPartialCount++;
                else
                    DebugPathInvalidCount++;

                DebugLastCandidateFailure =
                    $"Path failed for candidate {i}; found={pathFound}, status={path.status}, sampled={hit.position}";
                if (logEvadeCandidates)
                    DebugCandidateLines.Add($"Candidate {i}: PATH FAILED | found={pathFound} status={path.status} | sampled={hit.position}");
                continue;
            }

            DebugPathCompleteCount++;

            float distanceFromPlayer = Vector3.Distance(
                hit.position,
                player.position
            );

            // Straight-line distance from the AI to this candidate.
            float straightDistance = Vector3.Distance(
                transform.position,
                hit.position
            );

            float pathLength = CalculatePathLength(path);

            float pathQuality = EvaluatePathQuality(
                straightDistance,
                pathLength
            );

            float score = EvaluateEvadeUtility(
                distanceFromPlayer,
                pathQuality
            );

            EvadeCandidate candidate = new EvadeCandidate(
                hit.position,
                score,
                distanceFromPlayer,
                pathLength,
                pathQuality
            );

            candidates.Add(candidate);

            if (logEvadeCandidates)
            {
                DebugCandidateLines.Add(
                    $"Score: {score:F2} | " +
                    $"Player Dist: {distanceFromPlayer:F2}m | " +
                    $"Path: {pathLength:F2}m | " +
                    $"Quality: {pathQuality:F2}"
                );
            }
        }

        if (candidates.Count == 0)
        {
            ClearCurrentDestination();

            DebugSelectedDecision = "Evade (No Valid Candidate)";
            DebugSelectedScore = 0f;
            DebugSelectedDistance = 0f;
            DebugDestination = Vector3.zero;

            if (logDecisions)
            {
                Debug.LogWarning(
                    "AI Evader | No reachable candidate found. " +
                    $"Transform={transform.position}, Agent.nextPosition={navMeshAgent.nextPosition}, " +
                    $"Agent.isOnNavMesh={navMeshAgent.isOnNavMesh}, Agent.enabled={navMeshAgent.enabled}, " +
                    $"Agent.updatePosition={navMeshAgent.updatePosition}, Agent.velocity={navMeshAgent.velocity}, " +
                    $"Transform-Agent delta={DebugAgentTransformDelta:F3}, areaMask={navMeshAgent.areaMask}, " +
                    $"sample success/fail={DebugSampleSuccessCount}/{DebugSampleFailureCount}, " +
                    $"path complete/partial/invalid={DebugPathCompleteCount}/{DebugPathPartialCount}/{DebugPathInvalidCount}, " +
                    $"last failure={DebugLastCandidateFailure}",
                    this
                );
            }

            return;
        }

        // 1. Find the highest utility score FIRST.
        float bestScore = float.NegativeInfinity;

        foreach (EvadeCandidate candidate in candidates)
        {
            if (candidate.Score > bestScore)
                bestScore = candidate.Score;
        }

        // 2. Evaluate the current destination.
        bool currentDestinationValid =
            TryGetCurrentEvadeScore(out float currentScore, out float currentPathLength, out float currentPathQuality);

        // 3. Check whether we must switch.
        bool mustSwitch = !currentDestinationValid;

        // 4. Apply commitment and hysteresis here.
        // Both can now safely use bestScore.

        // Keep the current choice during its commitment period.
        if (currentDestinationValid && commitmentTimer > 0f)
        {
            navMeshDriver.SetDestination(currentDestination);

            DebugSelectedDecision = "Evade (Committed)";
            DebugSelectedScore = currentScore;
            DebugSelectedDistance = Vector3.Distance(
                currentDestination,
                player.position
            );
            DebugDestination = currentDestination;
            DebugSelectedPathLength = currentPathLength;
            DebugSelectedPathQuality = currentPathQuality;

            if (logDecisions)
            {
                Debug.Log(
                    $"AI Evader | Keeping destination | " +
                    $"Score: {currentScore:F2} | " +
                    $"Commitment remaining: {commitmentTimer:F2}s",
                    this
                );
            }

            return;
        }

        // Once commitment expires, apply hysteresis.
        if (!mustSwitch && currentDestinationValid && useDestinationHysteresis)
        {
            float scoreAdvantage = bestScore - currentScore;

            if (scoreAdvantage <= evadeSwitchingMargin)
            {
                navMeshDriver.SetDestination(currentDestination);

                DebugSelectedDecision = "Evade (Hysteresis)";
                DebugSelectedScore = currentScore;
                DebugSelectedDistance = Vector3.Distance(
                    currentDestination,
                    player.position
                );
                DebugDestination = currentDestination;
                DebugSelectedPathLength = currentPathLength;
                DebugSelectedPathQuality = currentPathQuality;

                if (logDecisions)
                {
                    Debug.Log(
                        $"AI Evader | Keeping destination | " +
                        $"Current: {currentScore:F2} | " +
                        $"Best alternative: {bestScore:F2} | " +
                        $"Advantage: {scoreAdvantage:F2}",
                        this
                    );
                }

                return;
            }
        }

        // Keep only candidates close enough to the best score.
        var eligibleCandidates =
            new List<EvadeCandidate>();

        foreach (EvadeCandidate candidate in candidates)
        {
            if (bestScore - candidate.Score <= evadeScoreTolerance)
                eligibleCandidates.Add(candidate);
        }

        EvadeCandidate selectedCandidate = eligibleCandidates[0];

        if (useWeightedRandomForEvade && eligibleCandidates.Count > 1)
        {
            selectedCandidate = SelectWeightedRandom(eligibleCandidates);
        }
        else
        {
            // Deterministic fallback: choose the highest-scoring candidate.
            foreach (EvadeCandidate candidate in eligibleCandidates)
            {
                if (candidate.Score > selectedCandidate.Score)
                    selectedCandidate = candidate;
            }
        }

        SetCurrentDestination(selectedCandidate.Position);

        DebugPlayerDistance = Vector3.Distance(
            transform.position,
            player.position
        );
        DebugCandidateCount = candidates.Count;
        DebugEligibleCount = eligibleCandidates.Count;
        DebugBestEvadeScore = bestScore;
        DebugSelectedScore = selectedCandidate.Score;
        DebugSelectedDistance = selectedCandidate.DistanceFromPlayer;
        DebugDestination = selectedCandidate.Position;
        DebugSelectedPathLength = selectedCandidate.PathLength;
        DebugSelectedPathQuality = selectedCandidate.PathQuality;
    }

    private EvadeCandidate SelectWeightedRandom(
        List<EvadeCandidate> candidates)
    {
        // Scores are clamped to [0, 1]. The small minimum weight
        // allows eligible zero-score candidates to remain selectable.
        float totalWeight = 0f;

        foreach (EvadeCandidate candidate in candidates)
            totalWeight += Mathf.Max(candidate.Score, 0.01f);

        float roll = Random.value * totalWeight;

        foreach (EvadeCandidate candidate in candidates)
        {
            roll -= Mathf.Max(candidate.Score, 0.01f);

            if (roll <= 0f)
                return candidate;
        }

        return candidates[candidates.Count - 1];
    }


    private float EvaluateChaserDistance(float distance)
    {
        float normalizedDistance = Mathf.Clamp01(
            distance / chaseScoreDistance
        );

        return Mathf.Clamp01(
            chaserDistanceCurve.Evaluate(normalizedDistance)
        );
    }

    private float EvaluateEvaderDistance(float distance)
    {
        float normalizedDistance = Mathf.Clamp01(
            distance / evadeScoreDistance
        );

        return Mathf.Clamp01(
            evaderDistanceCurve.Evaluate(normalizedDistance)
        );
    }


    private void SetCurrentDestination(Vector3 destination)
    {
        currentDestination = destination;
        hasCurrentDestination = true;
        commitmentTimer = minDestinationCommitment;

        navMeshDriver.SetDestination(destination);
    }

    private bool TryGetCurrentEvadeScore(out float score, out float pathLength, out float pathQuality)
    {
        score = 0f;
        pathLength = 0f;
        pathQuality = 0f;

        if (!hasCurrentDestination || !navMeshAgent.isOnNavMesh)
            return false;

        // Don't keep a destination that has already been reached.
        if (Vector3.Distance(transform.position, currentDestination)
            <= destinationArrivalDistance)
        {
            return false;
        }

        // Confirm that the current destination is still reachable.
        NavMeshPath path = new NavMeshPath();

        bool pathFound = navMeshAgent.CalculatePath(
            currentDestination,
            path
        );

        if (!pathFound || path.status != NavMeshPathStatus.PathComplete)
            return false;

        float straightDistance = Vector3.Distance(
            transform.position,
            currentDestination
        );

        pathLength = CalculatePathLength(path);

        pathQuality = EvaluatePathQuality(
            straightDistance,
            pathLength
        );

        float distanceFromPlayer = Vector3.Distance(
            currentDestination,
            player.position
        );

        score = EvaluateEvadeUtility(
            distanceFromPlayer,
            pathQuality
        );

        return true;
    }

    private float CalculatePathLength(NavMeshPath path)
    {
        if (path == null)
            return 0f;

        Vector3[] corners = path.corners;
        if (corners.Length < 2)
            return 0f;

        float totalLength = 0f;

        for (int i = 1; i < corners.Length; i++)
        {
            totalLength += Vector3.Distance(
                corners[i - 1],
                corners[i]
            );
        }

        return totalLength;
    }

    private float EvaluatePathQuality(
        float straightDistance,
        float pathLength)
    {
        if (pathLength <= 0f)
            return 0f;

        float detourRatio =
            pathLength / Mathf.Max(straightDistance, 0.001f);

        return Mathf.Clamp01(1f / Mathf.Max(1f, detourRatio));
    }

    private float EvaluateEvadeUtility(
        float distanceFromPlayer,
        float pathQuality)
    {
        float distanceUtility =
            EvaluateEvaderDistance(distanceFromPlayer);

        float totalWeight =
            evadeDistanceWeight + evadePathQualityWeight;

        if (totalWeight <= 0f)
            return 0f;

        return Mathf.Clamp01(
            (distanceUtility * evadeDistanceWeight +
             pathQuality * evadePathQualityWeight) / totalWeight
        );
    }

    private void ClearCurrentDestination()
    {
        hasCurrentDestination = false;
        commitmentTimer = 0f;
        DebugSelectedPathLength = 0f;
        DebugSelectedPathQuality = 0f;
        navMeshDriver.ClearDestination();
    }


    private void UpdateSprintDecision()
    {
        if (staminaEvaluator == null ||
            navMeshDriver == null ||
            roleController == null ||
            player == null)
        {
            isSprintRequested = false;

            if (navMeshDriver != null)
                navMeshDriver.SetSprintIntent(false);

            return;
        }

        staminaEvaluator.Evaluate();

        float playerDistance = Vector3.Distance(
            transform.position,
            player.position
        );

        bool roleWantsSprint = false;

        float chaserSprintDistance = 5f;
        float evaderSprintThreatDistance = 15f;

        switch (roleController.CurrentRole)
        {
            case CharacterRole.Chaser:
                roleWantsSprint =
                    playerDistance > chaserSprintDistance;
                break;

            case CharacterRole.Evader:
                roleWantsSprint =
                    playerDistance < evaderSprintThreatDistance;
                break;
        }

        // Hard constraints always override hysteresis.
        if (!roleWantsSprint || !staminaEvaluator.CanSprint)
        {
            isSprintRequested = false;
        }
        else if (isSprintRequested)
        {
            // Keep sprinting until stamina reaches the stop threshold.
            if (staminaEvaluator.StaminaRatio <= sprintStopRatio)
                isSprintRequested = false;
        }
        else
        {
            // Only start sprinting when stamina reaches the start threshold.
            if (staminaEvaluator.StaminaRatio >= sprintStartRatio)
                isSprintRequested = true;
        }

        navMeshDriver.SetSprintIntent(isSprintRequested);
    }


}
