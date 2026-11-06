using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(BuffNerfController))]
public class AITokenEvaluator : MonoBehaviour
{
    [Header("Awareness & Navigation")]
    [SerializeField, Min(0.1f)] private float awarenessRadius = 20f;
    [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 2f;
    [SerializeField, Min(0f)] private float pickupTriggerDistance = 0.5f;
    [SerializeField, Min(0.1f)] private float maxTokenApproachPath = 25f;
    [SerializeField, Min(0f)] private float maxTacticalDetour = 15f;
    [SerializeField, Min(0f)] private float maxEvaderSeparationLoss = 4f;

    [Header("Effect Values")]
    [SerializeField, Range(0f, 1f)] private float movementSpeedValue = 0.8f;
    [SerializeField, Range(0f, 1f)] private float staminaRegenValue = 0.55f;
    [SerializeField, Range(0f, 1f)] private float staminaCostValue = 0.65f;
    [SerializeField, Range(0f, 1f)] private float sprayRangeValue = 0.8f;
    [SerializeField, Range(0f, 1f)] private float sprayRadiusValue = 0.7f;
    [SerializeField, Range(0f, 1f)] private float evaderSprayBuffDenialValue = 0.45f;
    [SerializeField, Min(0.01f)] private float fullEffectMagnitudeLog = 0.5f;
    [SerializeField, Min(0.1f)] private float fullEffectDuration = 15f;

    private BuffNerfController buffNerfController;
    private StaminaSystem staminaSystem;
    private AINavMeshDriver navMeshDriver;
    private readonly List<TokenCandidate> candidates = new();
    private readonly List<string> debugCandidateLines = new();
    private readonly List<Collider> tokenColliders = new();
    private NavMeshPath evaluationPath;
    private Vector3[] pathCorners;
    private NavMeshPath EvaluationPath => evaluationPath ??= new NavMeshPath();

    public IReadOnlyList<TokenCandidate> Candidates => candidates;
    public IReadOnlyList<string> DebugCandidateLines => debugCandidateLines;
    public int DebugScannedTokenCount { get; private set; }
    public int DebugViableTokenCount => candidates.Count;
    public string DebugStatus { get; private set; } = "Not evaluated";
    public string DebugBestTokenName { get; private set; } = "None";
    public float DebugBestTokenScore { get; private set; }

    public struct TokenCandidate
    {
        public BuffNerfToken Token;
        public Vector3 NavMeshPosition;
        public float Score;
        public float EffectValue;
        public float ApproachPathLength;
        public float DetourDistance;
        public float PlayerDistance;

        public TokenCandidate(
            BuffNerfToken token,
            Vector3 navMeshPosition,
            float score,
            float effectValue,
            float approachPathLength,
            float detourDistance,
            float playerDistance)
        {
            Token = token;
            NavMeshPosition = navMeshPosition;
            Score = score;
            EffectValue = effectValue;
            ApproachPathLength = approachPathLength;
            DetourDistance = detourDistance;
            PlayerDistance = playerDistance;
        }
    }

    private void Awake()
    {
        buffNerfController = GetComponent<BuffNerfController>();
        staminaSystem = GetComponent<StaminaSystem>();
        navMeshDriver = GetComponent<AINavMeshDriver>();
    }

    public void EvaluateCandidates(
        CharacterRole role,
        Transform opponent,
        NavMeshAgent navMeshAgent,
        Vector3 tacticalDestination,
        float tacticalPathLength,
        float tacticalPlayerDistance)
    {
        candidates.Clear();
        debugCandidateLines.Clear();
        DebugBestTokenName = "None";
        DebugBestTokenScore = 0f;
        DebugScannedTokenCount = 0;

        if (role == CharacterRole.None)
        {
            DebugStatus = "No role assigned";
            return;
        }

        if (navMeshAgent == null ||
            !navMeshAgent.enabled)
        {
            DebugStatus = "AI agent is unavailable";
            return;
        }

        bool hasNavigationOrigin = navMeshDriver != null
            ? navMeshDriver.TryGetNavigationOrigin(out _)
            : navMeshAgent.isOnNavMesh;
        if (!hasNavigationOrigin)
        {
            DebugStatus = "No valid navigation origin for token paths";
            return;
        }

        BuffNerfToken[] activeTokens =
            Object.FindObjectsByType<BuffNerfToken>(FindObjectsSortMode.None);

        DebugScannedTokenCount = activeTokens.Length;

        foreach (BuffNerfToken token in activeTokens)
        {
            if (token == null || !token.gameObject.activeInHierarchy)
                continue;

            BuffNerfEffect effect = token.EffectData;

            if (!TryEvaluateToken(
                    token,
                    effect,
                    role,
                    opponent,
                    navMeshAgent,
                    tacticalDestination,
                    tacticalPathLength,
                    tacticalPlayerDistance,
                    out TokenCandidate candidate,
                    out string reason))
            {
                debugCandidateLines.Add(
                    $"{token.name}: SKIP | {reason}");
                continue;
            }

            candidates.Add(candidate);
            debugCandidateLines.Add(
                $"{token.name} ({effect.statType}/{effect.effectType}): " +
                $"score={candidate.Score:F2} " +
                $"effect={candidate.EffectValue:F2} " +
                $"mult={effect.multiplier:F2} duration={effect.duration:F1}s " +
                $"path={candidate.ApproachPathLength:F1}m " +
                $"detour={candidate.DetourDistance:F1}m " +
                $"player={candidate.PlayerDistance:F1}m");
        }

        candidates.Sort(
            (first, second) => second.Score.CompareTo(first.Score));

        if (candidates.Count == 0)
        {
            DebugStatus = DebugScannedTokenCount == 0
                ? "No active tokens"
                : "No viable token candidates";
            return;
        }

        DebugBestTokenName = candidates[0].Token.name;
        DebugBestTokenScore = candidates[0].Score;
        DebugStatus = $"Best: {DebugBestTokenName} ({DebugBestTokenScore:F2})";
    }

    private bool TryEvaluateToken(
        BuffNerfToken token,
        BuffNerfEffect effect,
        CharacterRole role,
        Transform opponent,
        NavMeshAgent navMeshAgent,
        Vector3 tacticalDestination,
        float tacticalPathLength,
        float tacticalPlayerDistance,
        out TokenCandidate candidate,
        out string reason)
    {
        candidate = default;
        reason = string.Empty;

        if (effect == null)
        {
            reason = "Missing effect data";
            return false;
        }

        float worldDistance = Vector3.Distance(
            transform.position,
            token.transform.position);

        if (worldDistance > awarenessRadius)
        {
            reason = $"Outside awareness ({worldDistance:F1}m)";
            return false;
        }

        float effectValue = EvaluateEffectValue(
            effect,
            role,
            opponent,
            token.transform.position,
            out reason);

        if (effectValue <= 0f)
            return false;

        if (!TryFindPickupNavMeshPoint(
                token,
                navMeshAgent.areaMask,
                out NavMeshHit pickupPoint,
                out reason))
        {
            return false;
        }

        NavMeshPath approachPath = EvaluationPath;
        bool approachPathFound = navMeshDriver != null
            ? navMeshDriver.TryCalculateNavigationPath(pickupPoint.position, approachPath)
            : navMeshAgent.CalculatePath(pickupPoint.position, approachPath);

        if (!approachPathFound || approachPath.status != NavMeshPathStatus.PathComplete)
        {
            reason = $"Approach path {(approachPathFound ? approachPath.status : NavMeshPathStatus.PathInvalid)}";
            return false;
        }

        float approachPathLength = NavMeshPathUtility.CalculateLength(approachPath, ref pathCorners);
        if (approachPathLength > maxTokenApproachPath)
        {
            reason = $"Approach path too long ({approachPathLength:F1}m)";
            return false;
        }

        NavMeshPath returnPath = EvaluationPath;
        bool returnPathFound = NavMesh.CalculatePath(
            pickupPoint.position,
            tacticalDestination,
            navMeshAgent.areaMask,
            returnPath);

        if (!returnPathFound || returnPath.status != NavMeshPathStatus.PathComplete)
        {
            reason = $"No complete path back to tactical goal ({returnPath.status})";
            return false;
        }

        float routeViaToken =
            approachPathLength + NavMeshPathUtility.CalculateLength(returnPath, ref pathCorners);
        float detourDistance = Mathf.Max(
            0f,
            routeViaToken - tacticalPathLength);

        if (detourDistance > maxTacticalDetour)
        {
            reason = $"Detour too long ({detourDistance:F1}m)";
            return false;
        }

        float playerDistance = opponent != null
            ? Vector3.Distance(pickupPoint.position, opponent.position)
            : 0f;

        if (role == CharacterRole.Evader &&
            opponent != null &&
            playerDistance < tacticalPlayerDistance - maxEvaderSeparationLoss)
        {
            reason = $"Unsafe separation ({playerDistance:F1}m)";
            return false;
        }

        float approachFactor = 1f - 0.5f * Mathf.Clamp01(
            approachPathLength / Mathf.Max(0.1f, maxTokenApproachPath));

        float detourFactor = maxTacticalDetour > 0f
            ? 1f - Mathf.Clamp01(detourDistance / maxTacticalDetour)
            : (detourDistance <= 0f ? 1f : 0f);

        float score = Mathf.Clamp01(
            effectValue * approachFactor * detourFactor);

        candidate = new TokenCandidate(
            token,
            pickupPoint.position,
            score,
            effectValue,
            approachPathLength,
            detourDistance,
            playerDistance);

        reason = "Evaluated";
        return true;
    }

    private float EvaluateEffectValue(
        BuffNerfEffect effect,
        CharacterRole role,
        Transform opponent,
        Vector3 tokenPosition,
        out string reason)
    {
        reason = string.Empty;

        if (effect.duration <= 0f || effect.multiplier <= 0f ||
            Mathf.Approximately(effect.multiplier, 1f))
        {
            reason = "Effect has no duration or stat change";
            return 0f;
        }

        bool isSprayStat = IsSprayStat(effect.statType);

        if (effect.effectType == EffectType.Nerf)
        {
            reason = role == CharacterRole.Evader && isSprayStat
                ? "Would consume a Chaser penalty"
                : "Applies a penalty to the AI";
            return 0f;
        }

        float value;

        if (role == CharacterRole.Evader && isSprayStat)
        {
            float opponentDistance = opponent != null
                ? Vector3.Distance(opponent.position, tokenPosition)
                : awarenessRadius;

            float contestValue = 1f - Mathf.Clamp01(
                opponentDistance / Mathf.Max(0.1f, awarenessRadius));

            value = evaderSprayBuffDenialValue * contestValue;

            if (value <= 0f)
            {
                reason = "Chaser is too far away for useful denial";
                return 0f;
            }
        }
        else
        {
            value = effect.statType switch
            {
                StatType.MovementSpeed => movementSpeedValue,
                StatType.StaminaRegen => staminaRegenValue * GetStaminaNeedFactor(),
                StatType.StaminaCost => staminaCostValue * GetStaminaNeedFactor(),
                StatType.TagSprayRange when role == CharacterRole.Chaser => sprayRangeValue,
                StatType.TagSprayRadius when role == CharacterRole.Chaser => sprayRadiusValue,
                _ => 0f
            };

            if (value <= 0f)
            {
                reason = $"No benefit for role {role}";
                return 0f;
            }
        }

        // Non-positive multipliers were rejected above.
        float effectMagnitude = Mathf.Abs(Mathf.Log(effect.multiplier));

        float magnitudeFactor = 0.65f + 0.35f * Mathf.Clamp01(
            effectMagnitude / fullEffectMagnitudeLog);
        float durationFactor = 0.7f + 0.3f * Mathf.Clamp01(
            effect.duration / fullEffectDuration);

        value *= magnitudeFactor * durationFactor;

        bool isDenialToken = role == CharacterRole.Evader && isSprayStat;
        if (!isDenialToken)
            value *= GetActiveEffectFactor(effect);

        reason = role == CharacterRole.Evader && isSprayStat
            ? "Denies Chaser spray buff"
            : "Beneficial effect";

        return Mathf.Clamp01(value);
    }

    private float GetStaminaNeedFactor()
    {
        if (staminaSystem == null || staminaSystem.MaxStamina <= 0f)
            return 1f;

        float staminaRatio = Mathf.Clamp01(
            staminaSystem.CurrentStamina / staminaSystem.MaxStamina);

        return Mathf.Lerp(0.5f, 1f, 1f - staminaRatio);
    }

    private float GetActiveEffectFactor(BuffNerfEffect candidateEffect)
    {
        if (buffNerfController == null)
            return 1f;

        foreach (ActiveBuffNerfEffect activeEffect in
                 buffNerfController.ActiveEffects)
        {
            if (activeEffect == null || activeEffect.EffectData == null ||
                activeEffect.EffectData.statType != candidateEffect.statType)
            {
                continue;
            }

            if (activeEffect.EffectData.effectType == candidateEffect.effectType)
            {
                float addedDuration = Mathf.Max(0f, candidateEffect.duration);
                return addedDuration /
                    Mathf.Max(0.1f, addedDuration + activeEffect.RemainingDuration);
            }

            if (candidateEffect.effectType == EffectType.Buff &&
                activeEffect.EffectData.effectType == EffectType.Nerf)
            {
                return 1.2f;
            }
        }

        return 1f;
    }

    private bool TryFindPickupNavMeshPoint(
        BuffNerfToken token,
        int areaMask,
        out NavMeshHit pickupPoint,
        out string reason)
    {
        pickupPoint = default;
        reason = string.Empty;

        token.GetComponentsInChildren(false, tokenColliders);
        bool foundTrigger = false;
        bool foundNavMeshPoint = false;
        float bestTriggerDistance = float.PositiveInfinity;

        foreach (Collider tokenCollider in tokenColliders)
        {
            if (tokenCollider == null ||
                !tokenCollider.enabled ||
                !tokenCollider.isTrigger)
                continue;

            foundTrigger = true;

            Bounds bounds = tokenCollider.bounds;
            Vector3 belowTrigger = new Vector3(
                bounds.center.x,
                bounds.min.y - 0.05f,
                bounds.center.z);
            Vector3 sampleOrigin = tokenCollider.ClosestPoint(belowTrigger);

            if (!NavMesh.SamplePosition(
                    sampleOrigin,
                    out NavMeshHit candidatePoint,
                    navMeshSampleRadius,
                    areaMask))
            {
                continue;
            }

            foundNavMeshPoint = true;
            Vector3 closestPoint = tokenCollider.ClosestPoint(candidatePoint.position);
            float triggerDistance = Vector3.Distance(
                closestPoint,
                candidatePoint.position);

            if (triggerDistance > pickupTriggerDistance ||
                triggerDistance >= bestTriggerDistance)
            {
                continue;
            }

            bestTriggerDistance = triggerDistance;
            pickupPoint = candidatePoint;
        }

        if (bestTriggerDistance < float.PositiveInfinity)
            return true;

        reason = !foundTrigger
            ? "No pickup trigger collider"
            : !foundNavMeshPoint
                ? "No nearby NavMesh point"
                : "NavMesh point is outside pickup trigger";
        return false;
    }

    private static bool IsSprayStat(StatType statType)
    {
        return statType == StatType.TagSprayRange ||
               statType == StatType.TagSprayRadius;
    }

}
