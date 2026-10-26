
using UnityEngine;
using UnityEngine.AI;

public class UtilityAIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoleController roleController;
    [SerializeField] private Transform player;
    [SerializeField] private AINavMeshDriver navMeshDriver;
    [SerializeField] private NavMeshAgent navMeshAgent;

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

    public int DebugCandidateCount { get; private set; }
    public int DebugEligibleCount { get; private set; }

    public string DebugSelectedDecision { get; private set; } = "None";
    public Vector3 DebugDestination { get; private set; }

    public System.Collections.Generic.List<string> DebugCandidateLines
    { get; } = new System.Collections.Generic.List<string>();

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

        public EvadeCandidate(
            Vector3 position,
            float score,
            float distanceFromPlayer)
        {
            Position = position;
            Score = score;
            DistanceFromPlayer = distanceFromPlayer;
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
        if (!navMeshAgent.isOnNavMesh)
        {
            ClearCurrentDestination();
            DebugSelectedDecision = "Off NavMesh";
            return;
        }
        UpdateSprintDecision();

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

        if (logDecisions)
        {
            // Debug.Log(
            //     $"AI Chaser | Distance: {playerDistance:F2} | " +
            //     $"Chase Score: {score:F2} | " +
            //     $"Destination: {hit.position}",
            //     this
            // );

            DebugPlayerDistance = playerDistance;
            DebugChaseScore = score;
            DebugDestination = hit.position;
        }
    }


    private void ChooseEvadeDestination()
    {
        DebugSelectedDecision = "Evade";
        DebugCandidateLines.Clear();

        Vector3 awayFromPlayer = transform.position - player.position;
        awayFromPlayer.y = 0f;

        if (awayFromPlayer.sqrMagnitude < 0.001f)
            awayFromPlayer = transform.forward;

        awayFromPlayer.Normalize();

        var candidates = new System.Collections.Generic.List<EvadeCandidate>();

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
                continue;
            }

            NavMeshPath path = new NavMeshPath();

            bool pathFound = navMeshAgent.CalculatePath(hit.position, path);

            if (!pathFound || path.status != NavMeshPathStatus.PathComplete)
                continue;

            float distanceFromPlayer =
                Vector3.Distance(hit.position, player.position);

            float score = EvaluateEvaderDistance(distanceFromPlayer);

            candidates.Add(new EvadeCandidate(
                hit.position,
                score,
                distanceFromPlayer
            ));

            if (logEvadeCandidates)
            {
                DebugCandidateLines.Add(
                    $"Score: {score:F2} | Distance: {distanceFromPlayer:F2}"
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
                Debug.Log(
                    "AI Evader | No reachable candidate found.",
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
            TryGetCurrentEvadeScore(out float currentScore);

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
            new System.Collections.Generic.List<EvadeCandidate>();

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

        if (logDecisions)
        {
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
        }
    }

    private EvadeCandidate SelectWeightedRandom(
        System.Collections.Generic.List<EvadeCandidate> candidates)
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

    private bool TryGetCurrentEvadeScore(out float score)
    {
        score = 0f;

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

        float distanceFromPlayer = Vector3.Distance(
            currentDestination,
            player.position
        );

        score = EvaluateEvaderDistance(distanceFromPlayer);
        return true;
    }

    private void ClearCurrentDestination()
    {
        hasCurrentDestination = false;
        commitmentTimer = 0f;
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

        float chaserSprintDistance = 10f;
        float evaderSprintThreatDistance = 5f;

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
