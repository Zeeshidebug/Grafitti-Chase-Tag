using UnityEngine;

/// <summary>
/// Continuously evaluates the detected obstacle and exposes a frame-by-frame
/// vault initiation window. The AI action controller should poll this every
/// frame and start the vault on the first frame the candidate is valid.
/// </summary>
public class ParkourCandidateEvaluator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParkourDetector detector;
    [SerializeField] private VaultEvaluator vaultEvaluator;

    private CharacterState characterState;
    private Collider lastStartedObstacle;
    private float lastActionStartedTime;
    private VaultCandidate currentVaultCandidate;

    private void Awake()
    {
        if (detector == null)
            detector = GetComponent<ParkourDetector>();

        if (vaultEvaluator == null)
            vaultEvaluator = GetComponent<VaultEvaluator>();

        characterState = GetComponent<CharacterState>();

        if (detector == null || vaultEvaluator == null)
        {
            Debug.LogError(
                "ParkourCandidateEvaluator requires ParkourDetector and VaultEvaluator.",
                this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (detector == null || vaultEvaluator == null)
            return;

        if (!detector.ObstacleDetected)
        {
            currentVaultCandidate = null;

            if ((characterState == null ||
                 characterState.CurrentState == LocomotionState.Grounded) &&
                Time.time - lastActionStartedTime > 0.2f)
            {
                lastStartedObstacle = null;
            }

            return;
        }

        // The AI must finish beyond the obstacle so its NavMesh proxy can
        // reconnect on walkable ground instead of remaining on top of it.
        ParkourObstacleData obstacle = detector.CurrentObstacle;
        Vector3 vaultDirection = -obstacle.SurfaceNormal;
        vaultDirection.y = 0f;

        if (vaultDirection.sqrMagnitude <= 0.001f)
            vaultDirection = transform.forward;

        currentVaultCandidate = vaultEvaluator.Evaluate(
            obstacle,
            landBeyondObstacle: true,
            direction: vaultDirection);
    }

    /// <summary>
    /// Called by AIActionController every frame. Returns the current candidate
    /// only while its initiation window is open.
    /// </summary>
    public bool TryGetVaultInitiationCandidate(out VaultCandidate candidate)
    {
        candidate = null;

        if (!TryGetCurrentObstacle(out _) ||
            currentVaultCandidate == null ||
            !currentVaultCandidate.IsValid)
        {
            return false;
        }

        candidate = currentVaultCandidate;
        return true;
    }

    /// <summary>
    /// Returns the detected obstacle unless an action has already started
    /// against the same collider and the character has not cleared it.
    /// </summary>
    public bool TryGetCurrentObstacle(out ParkourObstacleData obstacle)
    {
        obstacle = null;

        if (detector == null || !detector.ObstacleDetected)
            return false;

        obstacle = detector.CurrentObstacle;
        Collider currentCollider = obstacle.ObstacleCollider;
        if (currentCollider != null && currentCollider == lastStartedObstacle)
        {
            obstacle = null;
            return false;
        }

        return true;
    }

    /// <summary>Call after any AI parkour action successfully starts.</summary>
    public void NotifyActionStarted(Collider obstacleCollider)
    {
        lastStartedObstacle = obstacleCollider;
        lastActionStartedTime = Time.time;
    }
}
