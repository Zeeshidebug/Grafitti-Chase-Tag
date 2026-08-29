using UnityEngine;

public class ParkourCandidateEvaluator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParkourDetector detector;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private LayerMask parkourLayer;

    [Header("Vault")]
    [SerializeField] private float maxVaultHeight = 1.5f;
    [SerializeField] private float clearanceMargin = 0.05f;
    [SerializeField] private float landingDistance = 1.5f;
    [SerializeField] private float maxLandingDrop = 2f;
    [SerializeField] private float maxLandingSlope = 45f;

    [SerializeField] private LayerMask walkableSurfaceLayer;

    public ParkourCandidate CurrentCandidate
    {
        get;
        private set;
    }

    private void Awake()
    {
        characterController =
            GetComponent<CharacterController>();
    }

    private void Update()
    {
        CurrentCandidate =
            Evaluate(detector.CurrentObstacle);
    }

    public ParkourCandidate Evaluate(
        ParkourObstacleData obstacle
    )
    {
        if (obstacle == null || !obstacle.IsValid)
        {
            return null;
        }

        ParkourCandidate candidate =
            new ParkourCandidate();

        if (obstacle.Height <= 0f)
        {
            candidate.CanVault = false;
            candidate.VaultResult =
                ParkourValidationResult.TooLow;

            return candidate;
        }

        if (obstacle.Height > maxVaultHeight)
        {
            candidate.CanVault = false;
            candidate.VaultResult =
                ParkourValidationResult.TooHigh;

            return candidate;
        }

        if (!HasVaultClearance(obstacle))
        {
            candidate.CanVault = false;

            candidate.HasClearance = false;

            candidate.VaultResult =
                ParkourValidationResult.NoClearance;

            return candidate;
        }

        if (!FindLandingSurface(obstacle))
        {
            candidate.CanVault = false;

            candidate.HasLandingSpace = false;

            candidate.VaultResult =
                ParkourValidationResult.NoLandingSpace;

            return candidate;
        }

        candidate.HasLandingSpace = true;

        candidate.HasClearance = true;

        candidate.CanVault = true;

        candidate.VaultResult =
            ParkourValidationResult.Valid;

        return candidate;
    }

    private bool HasVaultClearance(
        ParkourObstacleData obstacle
    )
    {
        Vector3 center =
            GetClearanceCenter(obstacle);

        GetCapsulePointsAtPosition(
            center,
            out Vector3 point1,
            out Vector3 point2,
            out float radius
        );

        return !Physics.CheckCapsule(
            point1,
            point2,
            radius,
            parkourLayer
        );
    }

    private Vector3 GetClearanceCenter(
    ParkourObstacleData obstacle
)
    {
        float halfHeight =
            characterController.height * 0.5f;

        return obstacle.TopPosition
            + Vector3.up *
            (halfHeight + clearanceMargin);
    }

    private bool FindLandingSurface(
        ParkourObstacleData obstacle
    )
    {
        float playerHeight =
            characterController.height;

        Vector3 landingOrigin =
            obstacle.TopPosition
            + transform.forward * landingDistance
            + Vector3.up * playerHeight;

        float rayDistance =
            playerHeight + maxLandingDrop;

        if (!Physics.Raycast(
            landingOrigin,
            Vector3.down,
            out RaycastHit landingHit,
            rayDistance,
            walkableSurfaceLayer))
        {
            return false;
        }

        obstacle.LandingPosition =
            landingHit.point;

        obstacle.LandingNormal =
            landingHit.normal;

        float slopeAngle =
            Vector3.Angle(
                landingHit.normal,
                Vector3.up
            );

        return slopeAngle <= maxLandingSlope;
    }

    private void GetCapsulePointsAtPosition(
    Vector3 center,
    out Vector3 point1,
    out Vector3 point2,
    out float radius
)
    {
        radius = characterController.radius;

        float height =
            Mathf.Max(
                characterController.height,
                radius * 2f
            );

        float cylinderHeight =
            height - radius * 2f;

        point1 =
            center +
            Vector3.up *
            (cylinderHeight * 0.5f);

        point2 =
            center -
            Vector3.up *
            (cylinderHeight * 0.5f);
    }
}