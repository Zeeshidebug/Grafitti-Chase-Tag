
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AITokenEvaluator))]
public class UtilityAIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoleController roleController;
    [SerializeField] private Transform player;
    [SerializeField] private AINavMeshDriver navMeshDriver;
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private CharacterState characterState;
    [SerializeField] private AITokenEvaluator tokenEvaluator;

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

    [Header("Token Awareness")]
    [SerializeField, Range(0f, 1f)]
    private float minimumTokenUtility = 0.25f;

    [Tooltip("Chase utility raises the score needed to divert for a token.")]
    [SerializeField, Range(0f, 1f)]
    private float chaserTokenPriorityMargin = 0.15f;

    [Tooltip("A token target stays locked until the player enters this radius.")]
    [SerializeField, Min(0f)]
    private float tokenTargetInterruptDistance = 10f;

    [Header("Stamina Awareness")]
    [SerializeField] private StaminaUtilityEvaluator staminaEvaluator;

    [SerializeField, Range(0f, 1f)]
    private float sprintStartRatio = 0.55f;

    [SerializeField, Range(0f, 1f)]
    private float sprintStopRatio = 0.35f;

    [SerializeField, Range(1f, 50f)]
    private float chaserSprintDistance = 5f;

    [SerializeField, Range(1f, 50f)]
    private float evaderSprintThreatDistance = 15f;

    private bool isSprintRequested;

    [Header("Debug")]
    [SerializeField] private bool logDecisions = true;
    [SerializeField] private bool logEvadeCandidates;

    private float decisionTimer;
    private CharacterRole lastRole = CharacterRole.None;
    private Vector3 currentDestination;
    private bool hasCurrentDestination;
    private float commitmentTimer;
    private bool wasProxyDetached;
    private bool wasParkourTraversalActive;
    private bool wasExitingParkourSurface;
    private BuffNerfToken currentTokenTarget;
    private Vector3 currentTokenDestination;
    private readonly List<EvadeCandidate> evadeCandidates = new();
    private readonly List<EvadeCandidate> eligibleEvadeCandidates = new();
    private NavMeshPath evaluationPath;
    private Vector3[] pathCorners;
    private NavMeshPath EvaluationPath => evaluationPath ??= new NavMeshPath();

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

    public string DebugTokenStatus => tokenEvaluator != null
        ? tokenEvaluator.DebugStatus
        : "AITokenEvaluator missing";

    public int DebugTokenScannedCount => tokenEvaluator != null
        ? tokenEvaluator.DebugScannedTokenCount
        : 0;

    public int DebugTokenViableCount => tokenEvaluator != null
        ? tokenEvaluator.DebugViableTokenCount
        : 0;

    public string DebugBestTokenName => tokenEvaluator != null
        ? tokenEvaluator.DebugBestTokenName
        : "None";

    public float DebugBestTokenScore => tokenEvaluator != null
        ? tokenEvaluator.DebugBestTokenScore
        : 0f;

    public string DebugTokenDecision { get; private set; } = "Not evaluated";

    public string DebugCurrentTokenTarget => currentTokenTarget != null
        ? currentTokenTarget.name
        : "None";

    public string DebugTokenTargetLock
    {
        get
        {
            if (currentTokenTarget == null)
                return "None";

            if (player == null)
                return "Locked (player reference missing)";

            float playerDistance = Vector3.Distance(transform.position, player.position);
            return playerDistance <= tokenTargetInterruptDistance
                ? $"Interruptible ({playerDistance:F1}/{tokenTargetInterruptDistance:F1}m)"
                : $"Locked ({playerDistance:F1}/{tokenTargetInterruptDistance:F1}m)";
        }
    }

    public IReadOnlyList<string> DebugTokenCandidateLines => tokenEvaluator != null
        ? tokenEvaluator.DebugCandidateLines
        : (IReadOnlyList<string>)System.Array.Empty<string>();

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

        if (tokenEvaluator == null)
            tokenEvaluator = GetComponent<AITokenEvaluator>();
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

        if (tokenEvaluator == null)
        {
            DebugTokenDecision = "Evaluator missing";
            Debug.LogWarning(
                "UtilityAIController has no AITokenEvaluator; token awareness is disabled.",
                this);
        }

        decisionTimer = 0f;
    }

    private void Update()
    {
        // Keep decision ticks running while parkour owns physical movement.
        // Preserve the current sprint choice during an airborne/action phase.
        if (characterState == null ||
            characterState.CurrentState == LocomotionState.Grounded)
            UpdateSprintDecision();

        bool proxyDetached = navMeshDriver.IsProxyDetached;
        bool traversalActive = navMeshDriver.IsParkourTraversalActive;
        bool traversalJustFinished = wasParkourTraversalActive && !traversalActive;
        wasParkourTraversalActive = traversalActive;
        bool exitingSurface = navMeshDriver.IsExitingParkourSurface;
        bool surfaceExitJustFinished = wasExitingParkourSurface && !exitingSurface;
        wasExitingParkourSurface = exitingSurface;
        if (proxyDetached != wasProxyDetached || traversalJustFinished || surfaceExitJustFinished)
        {
            // A destination chosen around the old proxy can be behind the
            // character. Re-evaluate tactical destinations from the new origin.
            wasProxyDetached = proxyDetached;
            hasCurrentDestination = false;
            commitmentTimer = 0f;
            decisionTimer = 0f;
        }

        if (exitingSurface && roleController.CurrentRole == lastRole &&
            roleController.CurrentRole != CharacterRole.None)
        {
            // Local candidates on the isolated top must not redirect the exit.
            DebugSelectedDecision = "Parkour Exit (Disconnected Top NavMesh)";
            DebugCandidateLines.Clear();
            DebugCandidateCount = 0;
            DebugEligibleCount = 0;
            DebugSelectedScore = 0f;
            DebugSelectedDistance = 0f;
            DebugSelectedPathLength = 0f;
            DebugSelectedPathQuality = 0f;
            return;
        }

        if (!navMeshDriver.TryGetNavigationOrigin(out _))
        {
            if (!proxyDetached || roleController.CurrentRole != lastRole)
                ClearCurrentDestination();
            DebugSelectedDecision = "Waiting for NavMesh near character";
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

        if (TryMaintainCurrentTokenTarget())
            return;

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
            if (KeepDestinationForProxySearch())
                return;

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

        DebugPlayerDistance = playerDistance;
        DebugChaseScore = score;

        NavMeshPath chasePath = EvaluationPath;
        float chasePathLength = Vector3.Distance(
            navMeshDriver.NavigationOrigin,
            hit.position);

        if (navMeshDriver.TryCalculateNavigationPath(hit.position, chasePath) &&
            chasePath.status == NavMeshPathStatus.PathComplete)
        {
            chasePathLength = NavMeshPathUtility.CalculateLength(chasePath, ref pathCorners);
        }
        else if (KeepDestinationForProxySearch())
        {
            return;
        }

        float playerDistanceAtDestination = Vector3.Distance(
            hit.position,
            player.position);

        if (TryUseTokenDestination(
                hit.position,
                chasePathLength,
                score,
                playerDistanceAtDestination))
        {
            return;
        }

        navMeshDriver.SetDestination(hit.position);
        DebugSelectedDecision = "Chase";
        DebugSelectedScore = score;
        DebugSelectedDistance = playerDistance;
        DebugSelectedPathLength = chasePathLength;
        DebugSelectedPathQuality = 1f;
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
        Vector3 navigationOrigin = navMeshDriver.NavigationOrigin;

        Vector3 awayFromPlayer = transform.position - player.position;
        awayFromPlayer.y = 0f;

        if (awayFromPlayer.sqrMagnitude < 0.001f)
            awayFromPlayer = transform.forward;

        awayFromPlayer.Normalize();

        List<EvadeCandidate> candidates = evadeCandidates;
        candidates.Clear();
        float bestScore = float.NegativeInfinity;

        int count = Mathf.Max(4, evadeCandidateCount);

        for (int i = 0; i < count; i++)
        {
            float angle = i * 360f / count;

            Vector3 direction =
                Quaternion.AngleAxis(angle, Vector3.up) * awayFromPlayer;

            Vector3 rawPosition =
                navigationOrigin + direction * evadeSearchRadius;

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
            NavMeshPath path = EvaluationPath;

            bool pathFound = navMeshDriver.TryCalculateNavigationPath(hit.position, path);
            NavMeshPathStatus pathStatus = pathFound ? path.status : NavMeshPathStatus.PathInvalid;

            if (pathStatus != NavMeshPathStatus.PathComplete)
            {
                if (pathStatus == NavMeshPathStatus.PathPartial)
                    DebugPathPartialCount++;
                else
                    DebugPathInvalidCount++;

                DebugLastCandidateFailure =
                    $"Path failed for candidate {i}; found={pathFound}, status={pathStatus}, sampled={hit.position}";
                if (logEvadeCandidates)
                    DebugCandidateLines.Add($"Candidate {i}: PATH FAILED | found={pathFound} status={pathStatus} | sampled={hit.position}");
                continue;
            }

            DebugPathCompleteCount++;

            float distanceFromPlayer = Vector3.Distance(
                hit.position,
                player.position
            );

            // Compare path length and direct distance from the same NavMesh origin.
            float straightDistance = Vector3.Distance(
                navigationOrigin,
                hit.position
            );

            float pathLength = NavMeshPathUtility.CalculateLength(path, ref pathCorners);

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
            if (score > bestScore)
                bestScore = score;

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
            DebugCandidateCount = 0;
            DebugEligibleCount = 0;
            if (!KeepDestinationForProxySearch())
            {
                ClearCurrentDestination();
                DebugSelectedDecision = "Evade (No Valid Candidate)";
                DebugSelectedScore = 0f;
                DebugSelectedDistance = 0f;
                DebugDestination = Vector3.zero;
            }

            if (logDecisions)
            {
                Debug.LogWarning(
                    "AI Evader | No reachable candidate found. " +
                    $"Transform={transform.position}, Agent.nextPosition={navMeshAgent.nextPosition}, " +
                    $"Agent.isOnNavMesh={navMeshAgent.isOnNavMesh}, Agent.enabled={navMeshAgent.enabled}, " +
                    $"Agent.updatePosition={navMeshAgent.updatePosition}, Agent.velocity={navMeshAgent.velocity}, " +
                    $"navigationOrigin={navigationOrigin}, proxyDetached={navMeshDriver.IsProxyDetached}, " +
                    $"navigationStatus={navMeshDriver.DebugNavigationStatus}, " +
                    $"Transform-Agent delta={DebugAgentTransformDelta:F3}, areaMask={navMeshAgent.areaMask}, " +
                    $"sample success/fail={DebugSampleSuccessCount}/{DebugSampleFailureCount}, " +
                    $"path complete/partial/invalid={DebugPathCompleteCount}/{DebugPathPartialCount}/{DebugPathInvalidCount}, " +
                    $"last failure={DebugLastCandidateFailure}",
                    this
                );
            }

            return;
        }

        // Compare the current destination with the best candidate found above.
        bool currentDestinationValid =
            TryGetCurrentEvadeScore(out float currentScore, out float currentPathLength, out float currentPathQuality);

        bool keepCommitment = currentDestinationValid && commitmentTimer > 0f;
        float scoreAdvantage = bestScore - currentScore;
        bool keepHysteresis = currentDestinationValid && useDestinationHysteresis &&
            scoreAdvantage <= evadeSwitchingMargin;

        if (keepCommitment || keepHysteresis)
        {
            float currentPlayerDistance = Vector3.Distance(currentDestination, player.position);
            if (TryUseTokenDestination(
                    currentDestination,
                    currentPathLength,
                    currentScore,
                    currentPlayerDistance))
            {
                return;
            }

            navMeshDriver.SetDestination(currentDestination);

            DebugSelectedDecision = keepCommitment ? "Evade (Committed)" : "Evade (Hysteresis)";
            DebugSelectedScore = currentScore;
            DebugSelectedDistance = currentPlayerDistance;
            DebugDestination = currentDestination;
            DebugSelectedPathLength = currentPathLength;
            DebugSelectedPathQuality = currentPathQuality;

            if (logDecisions && keepCommitment)
            {
                Debug.Log(
                    $"AI Evader | Keeping destination | " +
                    $"Score: {currentScore:F2} | " +
                    $"Commitment remaining: {commitmentTimer:F2}s",
                    this
                );
            }
            else if (logDecisions)
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

        // Keep only candidates close enough to the best score.
        List<EvadeCandidate> eligibleCandidates = eligibleEvadeCandidates;
        eligibleCandidates.Clear();

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

        if (TryUseTokenDestination(
                selectedCandidate.Position,
                selectedCandidate.PathLength,
                selectedCandidate.Score,
                selectedCandidate.DistanceFromPlayer))
        {
            return;
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

    private bool TryUseTokenDestination(
        Vector3 tacticalDestination,
        float tacticalPathLength,
        float tacticalScore,
        float tacticalPlayerDistance)
    {
        if (tokenEvaluator == null)
        {
            DebugTokenDecision = "Evaluator missing";
            return false;
        }

        tokenEvaluator.EvaluateCandidates(
            roleController.CurrentRole,
            player,
            navMeshAgent,
            tacticalDestination,
            tacticalPathLength,
            tacticalPlayerDistance);

        IReadOnlyList<AITokenEvaluator.TokenCandidate> candidates =
            tokenEvaluator.Candidates;

        if (candidates.Count == 0)
        {
            ClearTokenTarget();
            DebugTokenDecision = "No viable candidate";
            return false;
        }

        float requiredScore = minimumTokenUtility;
        if (roleController.CurrentRole == CharacterRole.Chaser)
        {
            requiredScore = Mathf.Max(
                requiredScore,
                tacticalScore + chaserTokenPriorityMargin);
        }

        AITokenEvaluator.TokenCandidate selected = candidates[0];

        if (selected.Score < requiredScore)
        {
            ClearTokenTarget();
            DebugTokenDecision =
                $"Best score {selected.Score:F2} below required {requiredScore:F2}";
            return false;
        }

        bool changedTarget = selected.Token != currentTokenTarget;
        currentTokenTarget = selected.Token;
        currentTokenDestination = selected.NavMeshPosition;

        navMeshDriver.SetPreciseDestination(selected.NavMeshPosition);

        DebugSelectedDecision =
            $"{roleController.CurrentRole} (Token: {selected.Token.name})";
        DebugSelectedScore = selected.Score;
        DebugSelectedDistance = selected.ApproachPathLength;
        DebugSelectedPathLength = selected.ApproachPathLength;
        DebugSelectedPathQuality = 1f;
        DebugDestination = selected.NavMeshPosition;
        DebugTokenDecision =
            $"Pursuing {selected.Token.name} ({selected.Score:F2}/{requiredScore:F2})";

        if (logDecisions && changedTarget)
        {
            Debug.Log(
                $"AI Token | Target={selected.Token.name} | " +
                $"Score={selected.Score:F2} | Effect={selected.EffectValue:F2} | " +
                $"Path={selected.ApproachPathLength:F1}m | " +
                $"Detour={selected.DetourDistance:F1}m | " +
                $"Required={requiredScore:F2}",
                this);
        }

        return true;
    }

    private bool TryMaintainCurrentTokenTarget()
    {
        if (currentTokenTarget == null)
            return false;

        if (!currentTokenTarget.gameObject.activeInHierarchy)
        {
            ClearTokenTarget();
            return false;
        }

        float playerDistance = player != null
            ? Vector3.Distance(transform.position, player.position)
            : float.PositiveInfinity;

        if (playerDistance <= tokenTargetInterruptDistance)
        {
            DebugTokenDecision =
                $"Token pursuit interrupted: player within {playerDistance:F1}m";
            ClearTokenTarget();
            return false;
        }

        NavMeshPath path = EvaluationPath;
        bool pathFound = navMeshDriver.TryCalculateNavigationPath(
            currentTokenDestination,
            path);

        if (!pathFound || path.status != NavMeshPathStatus.PathComplete)
        {
            DebugTokenDecision = "Token pursuit released: path is no longer complete";
            ClearTokenTarget();
            return false;
        }

        navMeshDriver.SetPreciseDestination(currentTokenDestination);
        DebugSelectedDecision =
            $"{roleController.CurrentRole} (Token Locked: {currentTokenTarget.name})";
        DebugDestination = currentTokenDestination;
        DebugTokenDecision =
            $"Locked on {currentTokenTarget.name} (player {playerDistance:F1}m away)";
        return true;
    }

    private void ClearTokenTarget()
    {
        currentTokenTarget = null;
        currentTokenDestination = Vector3.zero;
    }

    private bool TryGetCurrentEvadeScore(out float score, out float pathLength, out float pathQuality)
    {
        score = 0f;
        pathLength = 0f;
        pathQuality = 0f;

        if (!hasCurrentDestination ||
            !navMeshDriver.TryGetNavigationOrigin(out Vector3 navigationOrigin))
            return false;

        // Don't keep a destination that has already been reached.
        Vector3 arrivalOrigin = navMeshDriver.IsProxyDetached
            ? navigationOrigin : transform.position;
        if (Vector3.Distance(arrivalOrigin, currentDestination)
            <= destinationArrivalDistance)
        {
            return false;
        }

        // Confirm that the current destination is still reachable.
        NavMeshPath path = EvaluationPath;

        bool pathFound = navMeshDriver.TryCalculateNavigationPath(
            currentDestination,
            path
        );

        if (!pathFound || path.status != NavMeshPathStatus.PathComplete)
            return false;

        float straightDistance = Vector3.Distance(
            navigationOrigin,
            currentDestination
        );

        pathLength = NavMeshPathUtility.CalculateLength(path, ref pathCorners);

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

    private bool KeepDestinationForProxySearch()
    {
        if (!navMeshDriver.IsProxyDetached)
            return false;

        // A temporary search failure must not erase the action's exit direction
        // or the stored destination. The next decision tick retries the search.
        DebugSelectedDecision = $"{roleController.CurrentRole} (Proxy: retrying destination search)";
        return true;
    }

    private void ClearCurrentDestination()
    {
        hasCurrentDestination = false;
        commitmentTimer = 0f;
        ClearTokenTarget();
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
