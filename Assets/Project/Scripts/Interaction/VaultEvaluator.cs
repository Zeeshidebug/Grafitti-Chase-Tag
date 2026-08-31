using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class VaultEvaluator : MonoBehaviour
{
    [Header("Vault")]
    [SerializeField] private float maxVaultHeight = 1.5f;

    [SerializeField] private float clearanceMargin = 0.05f;

    [SerializeField] private float landingDistance = 1.5f;

    [SerializeField] private float maxLandingDrop = 2f;

    [SerializeField] private float maxLandingSlope = 45f;

    [Header("Environment")]
    [SerializeField] private LayerMask walkableSurfaceLayer;
    [SerializeField] private LayerMask environmentCollisionLayer;
    [SerializeField] private LayerMask parkourLayer;

    private CharacterController characterController;

    private void Awake()
    {
        characterController =
            GetComponent<CharacterController>();
    }

    public VaultCandidate Evaluate(
        ParkourObstacleData obstacle
    )
    {
        if (obstacle == null || !obstacle.IsValid)
        {
            return null;
        }

        VaultCandidate candidate =
            new VaultCandidate();

        candidate.ObstacleHeight =
            obstacle.Height;

        // Height
        if (obstacle.Height <= 0f)
        {
            candidate.Result =
                ParkourValidationResult.TooLow;

            return candidate;
        }

        if (obstacle.Height > maxVaultHeight)
        {
            candidate.Result =
                ParkourValidationResult.TooHigh;

            return candidate;
        }

        // Clearance
        if (!HasVaultClearance(obstacle))
        {
            candidate.HasClearance = false;

            candidate.Result =
                ParkourValidationResult.NoClearance;

            return candidate;
        }

        candidate.HasClearance = true;

        // Landing
        if (!FindLandingSurface(
            obstacle,
            out Vector3 landingPosition,
            out Vector3 landingNormal))
        {
            candidate.HasLandingSpace = false;

            candidate.Result =
                ParkourValidationResult.NoLandingSpace;

            return candidate;
        }

        candidate.HasLandingSpace = true;

        candidate.LandingPosition =
            landingPosition;

        candidate.LandingNormal =
            landingNormal;

        // Valid
        candidate.IsValid = true;

        candidate.Result =
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
    ParkourObstacleData obstacle,
    out Vector3 landingPosition,
    out Vector3 landingNormal
)
    {
        landingPosition = Vector3.zero;
        landingNormal = Vector3.zero;

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

        float slopeAngle =
            Vector3.Angle(
                landingHit.normal,
                Vector3.up
            );

        if (slopeAngle > maxLandingSlope)
        {
            return false;
        }

        landingPosition =
            landingHit.point;

        landingNormal =
            landingHit.normal;

        return true;
    }

    private void GetCapsulePointsAtPosition(
        Vector3 center,
        out Vector3 point1,
        out Vector3 point2,
        out float radius
    )
    {
        radius =
            characterController.radius;

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