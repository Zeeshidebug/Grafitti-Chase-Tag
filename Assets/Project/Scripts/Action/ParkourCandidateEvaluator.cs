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

    private Collider lastStartedObstacle;
    private VaultCandidate currentVaultCandidate;

    private void Awake()
    {
        if (detector == null)
            detector = GetComponent<ParkourDetector>();

        if (vaultEvaluator == null)
            vaultEvaluator = GetComponent<VaultEvaluator>();

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
            lastStartedObstacle = null;
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

        if (detector == null ||
            !detector.ObstacleDetected ||
            currentVaultCandidate == null ||
            !currentVaultCandidate.IsValid)
        {
            return false;
        }

        Collider currentCollider = detector.CurrentObstacle.ObstacleCollider;
        if (currentCollider != null && currentCollider == lastStartedObstacle)
            return false;

        candidate = currentVaultCandidate;
        return true;
    }

    /// <summary>
    /// Call only after VaultExecutor.TryExecute(candidate) returns true.
    /// Prevents the same detected collider from repeatedly starting Vault.
    /// </summary>
    public void NotifyVaultStarted()
    {
        if (detector != null && detector.CurrentObstacle != null)
            lastStartedObstacle = detector.CurrentObstacle.ObstacleCollider;
    }
}
