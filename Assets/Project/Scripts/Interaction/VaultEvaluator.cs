using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class VaultEvaluator : MonoBehaviour
{
    [Header("Vault Height")]
    [SerializeField]
    [Range(0.05f, 1f)]
    private float minVaultHeightRatio = 0.45f;

    [SerializeField]
    [Range(0.1f, 1f)]
    private float maxVaultHeightRatio = 0.85f;

    [Header("Clearance")]
    [SerializeField]
    [Range(0f, 1f)]
    private float clearanceMarginRatio = 0.1f;
    [SerializeField]
    private float vaultClearance = 0.1f;

    [Header("Landing")]
    [SerializeField]
    [Range(0.1f, 2f)]
    private float landingDistanceRatio = 0.75f;

    [SerializeField]
    private float maxLandingDrop = 2f;

    [SerializeField]
    private float maxLandingSlope = 45f;

    [Header("Environment")]
    [SerializeField] private LayerMask walkableSurfaceLayer;
    [SerializeField] private LayerMask environmentCollisionLayer;
    [SerializeField] private LayerMask parkourLayer;

    private float minVaultHeight =>
    characterController.height *
    minVaultHeightRatio;

    private float maxVaultHeight =>
        characterController.height *
        maxVaultHeightRatio;

    private float clearanceMargin =>
        characterController.radius *
        clearanceMarginRatio;

    private float landingDistance =>
        characterController.height *
        landingDistanceRatio;

    private CharacterController characterController;

    private VaultExecutor vaultExecutor;

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
        if (obstacle.Height < minVaultHeight)
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

        Vector3 startPosition = transform.position;

        candidate.RequiredArcHeight =
            CalculateRequiredArcHeight(
                obstacle,
                candidate.LandingPosition,
                startPosition
            );

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

    private float GetCharacterBottomOffset()
    {
        float halfHeight = characterController.height * 0.5f;

        return characterController.center.y - halfHeight;
    }

    public float GetRequiredCenterHeight(
        ParkourObstacleData obstacle)
    {
        float bottomOffset =
            GetCharacterBottomOffset();

        float requiredBottomHeight =
            obstacle.TopPosition.y +
            vaultClearance;

        return requiredBottomHeight -
            bottomOffset;
    }

    private float CalculateRequiredArcHeight(
        ParkourObstacleData obstacle,
        Vector3 landingPosition,
        Vector3 startPosition)
    {
        float requiredCenterHeight =
            GetRequiredCenterHeight(obstacle);

        Vector3 horizontalDelta =
            landingPosition - startPosition;

        horizontalDelta.y = 0f;

        float horizontalLength =
            horizontalDelta.magnitude;

        if (horizontalLength <= 0.001f)
        {
            return 0f;
        }

        Vector3 obstacleOffset =
            obstacle.TopPosition - startPosition;

        obstacleOffset.y = 0f;

        float obstacleDistance =
            Vector3.Dot(
                obstacleOffset,
                horizontalDelta.normalized
            );

        float t =
            Mathf.Clamp01(
                obstacleDistance / horizontalLength
            );

        float baseHeight =
            Mathf.Lerp(
                startPosition.y,
                landingPosition.y,
                t
            );

        float sinValue =
            Mathf.Sin(t * Mathf.PI);

        if (sinValue <= 0.001f)
        {
            return 0f;
        }

        float requiredArc =
            (requiredCenterHeight - baseHeight)
            / sinValue;

        //         Debug.Log(
        // $"Vault Geometry | " +
        // $"ObstacleTopY: {obstacle.TopPosition.y:F2} | " +
        // $"RequiredBottomY: {requiredBottomHeight:F2} | " +
        // $"RequiredCenterY: {requiredCenterHeight:F2} | " +
        // $"BaseY: {baseHeight:F2} | " +
        // $"t: {t:F2} | " +
        // $"sin: {sinValue:F2} | " +
        // $"RequiredArc: {requiredArc:F2}"
        // );

        return Mathf.Max(0f, requiredArc);
    }
}
