using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController))]
public class VaultEvaluator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AIMovementIntent movementIntent;

    [Header("Vault Height")]
    [SerializeField, Range(0.05f, 1f)] private float minVaultHeightRatio = 0.45f;
    [SerializeField, Range(0.1f, 1f)] private float maxVaultHeightRatio = 0.85f;

    [Header("Clearance")]
    [SerializeField, Range(0f, 1f)] private float clearanceMarginRatio = 0.1f;
    [SerializeField] private float vaultClearance = 0.1f;

    [Header("Landing")]
    [SerializeField, Range(0.1f, 2f)] private float landingDistanceRatio = 0.75f;
    [SerializeField, Min(0f)] private float landingExtraClearance = 0.25f;
    [Tooltip("Extra distance after the expanded obstacle footprint. Gives the sinusoidal arc room to descend after the character has fully cleared the obstacle.")]
    [SerializeField, Min(0f)] private float landingRunoutDistance = 2f;
    [SerializeField] private float maxLandingDrop = 2f;
    [SerializeField] private float maxLandingSlope = 45f;

    [Header("Debug")]
    [Tooltip("Log candidate trajectory calculations for debugging.")]
    [FormerlySerializedAs("logObstacleProjectionDebug")]
    [SerializeField] private bool logTrajectoryDebug;

    [Header("Environment")]
    [SerializeField] private LayerMask walkableSurfaceLayer;
    [SerializeField] private LayerMask parkourLayer;

    private CharacterController characterController;

    private float minVaultHeight => characterController.height * minVaultHeightRatio;
    private float maxVaultHeight => characterController.height * maxVaultHeightRatio;
    private float clearanceMargin => characterController.radius * clearanceMarginRatio;
    private float minimumLandingDistance => characterController.height * landingDistanceRatio;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    public VaultCandidate Evaluate(ParkourObstacleData obstacle)
    {
        return Evaluate(obstacle, false);
    }

    public VaultCandidate Evaluate(
        ParkourObstacleData obstacle,
        bool landBeyondObstacle)
    {
        if (obstacle == null || !obstacle.IsValid)
            return null;

        VaultCandidate candidate = new VaultCandidate();

        if (obstacle.Height < minVaultHeight)
        {
            candidate.Result = VaultValidationResult.TooLow;
            return candidate;
        }

        if (obstacle.Height > maxVaultHeight)
        {
            candidate.Result = VaultValidationResult.TooHigh;
            return candidate;
        }

        if (!HasVaultClearance(obstacle))
        {
            candidate.Result = VaultValidationResult.NoClearance;
            return candidate;
        }

        candidate.HasClearance = true;

        Vector3 direction = GetMovementDirection();
        if (direction.sqrMagnitude <= 0.001f)
        {
            candidate.Result = VaultValidationResult.InvalidSurface;
            return candidate;
        }

        Vector3 startPosition = transform.position;
        Vector3 worldBottomOffset =
            CharacterControllerGeometry.GetWorldBottomOffset(characterController);

        float farEdgeProjection = Vector3.Dot(obstacle.TopPosition, direction);
        if (landBeyondObstacle &&
            ObstacleProjectionUtility.TryGetProjectedFarEdge(
                obstacle,
                direction,
                out float projectedFarEdge))
        {
            farEdgeProjection = projectedFarEdge;
        }

        if (!FindLandingSurface(
                obstacle,
                direction,
                landBeyondObstacle,
                farEdgeProjection,
                worldBottomOffset,
                out Vector3 landingSurface,
                out Vector3 landingRoot))
        {
            candidate.Result = VaultValidationResult.NoLandingSpace;
            return candidate;
        }

        candidate.HasLandingSpace = true;
        candidate.LandingSurfacePosition = landingSurface;
        candidate.LandingPosition = landingRoot;

        Vector3 travelDelta = landingRoot - startPosition;
        travelDelta.y = 0f;
        float travelLength = Vector3.Dot(travelDelta, direction);
        float obstacleDistance = Vector3.Dot(obstacle.TopPosition - startPosition, direction);
        float obstacleTime = travelLength > 0.001f
            ? Mathf.Clamp01(obstacleDistance / travelLength)
            : 0f;

        float requiredRootY = obstacle.TopPosition.y
            + vaultClearance
            - worldBottomOffset.y;
        float baseHeight = Mathf.Lerp(startPosition.y, landingRoot.y, obstacleTime);
        float sinValue = Mathf.Sin(obstacleTime * Mathf.PI);
        candidate.RequiredArcHeight = sinValue > 0.001f
            ? Mathf.Max(0f, (requiredRootY - baseHeight) / sinValue)
            : 0f;
        candidate.ObstacleTime = obstacleTime;

        candidate.IsValid = true;
        candidate.Result = VaultValidationResult.Valid;

        if (logTrajectoryDebug)
            Debug.Log($"VAULT CANDIDATE ARC | current={transform.position}, takeoff={startPosition}, " +
                $"landing={landingRoot}, obstacleTime={obstacleTime:F3}, " +
                $"requiredArc={candidate.RequiredArcHeight:F2}, height={obstacle.Height:F2}", this);

        return candidate;
    }

    private Vector3 GetMovementDirection()
    {
        Vector3 direction =
            movementIntent != null &&
            movementIntent.MovementDirection.sqrMagnitude > 0.001f
                ? movementIntent.MovementDirection
                : transform.forward;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return Vector3.zero;

        return direction.normalized;
    }

    private bool HasVaultClearance(ParkourObstacleData obstacle)
    {
        Vector3 center = obstacle.TopPosition
            + Vector3.up * (characterController.height * 0.5f + clearanceMargin);

        CharacterControllerGeometry.GetCapsulePoints(
            characterController,
            center,
            out Vector3 point1,
            out Vector3 point2,
            out float radius);

        return !Physics.CheckCapsule(
            point1,
            point2,
            radius,
            parkourLayer,
            QueryTriggerInteraction.Ignore);
    }

    private bool FindLandingSurface(
        ParkourObstacleData obstacle,
        Vector3 direction,
        bool landBeyondObstacle,
        float farEdgeProjection,
        Vector3 worldBottomOffset,
        out Vector3 landingSurfacePosition,
        out Vector3 landingRootPosition)
    {
        landingSurfacePosition = Vector3.zero;
        landingRootPosition = Vector3.zero;

        Vector3 landingXZ = GetLandingTargetPosition(
            obstacle,
            direction,
            landBeyondObstacle,
            farEdgeProjection);

        return TryFindLandingSurface(
            obstacle,
            landingXZ,
            worldBottomOffset,
            out landingSurfacePosition,
            out landingRootPosition);
    }

    private Vector3 GetLandingTargetPosition(
        ParkourObstacleData obstacle,
        Vector3 direction,
        bool landBeyondObstacle,
        float farEdgeProjection)
    {
        float landingDistance = minimumLandingDistance;

        if (landBeyondObstacle)
        {
            float topProjection = Vector3.Dot(obstacle.TopPosition, direction);
            float distanceBeyondObstacle = Mathf.Max(
                0f,
                farEdgeProjection - topProjection
                    + characterController.radius
                    + landingExtraClearance);

            landingDistance = Mathf.Max(
                landingDistance,
                distanceBeyondObstacle + landingRunoutDistance);
        }

        return obstacle.TopPosition + direction * landingDistance;
    }

    private bool TryFindLandingSurface(
        ParkourObstacleData obstacle,
        Vector3 landingXZ,
        Vector3 worldBottomOffset,
        out Vector3 landingSurfacePosition,
        out Vector3 landingRootPosition)
    {
        landingSurfacePosition = Vector3.zero;
        landingRootPosition = Vector3.zero;

        float expectedSurfaceY =
            transform.position.y + worldBottomOffset.y - maxLandingDrop;

        float rayOriginY =
            Mathf.Max(transform.position.y, obstacle.TopPosition.y)
            + characterController.height
            + maxLandingDrop
            + 0.5f;

        Vector3 rayOrigin = new Vector3(
            landingXZ.x,
            rayOriginY,
            landingXZ.z);

        float rayDistance = rayOriginY - expectedSurfaceY + 0.5f;

        bool foundLanding = Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit landingHit,
            rayDistance,
            walkableSurfaceLayer,
            QueryTriggerInteraction.Ignore);

        if (!foundLanding)
        {
            Debug.LogWarning(
                $"VAULT LANDING RAY MISS | origin={rayOrigin}, " +
                $"distance={rayDistance:F2}, " +
                $"rayEndY={rayOriginY - rayDistance:F2}, " +
                $"walkableMask={walkableSurfaceLayer.value}, " +
                $"obstacleTop={obstacle.TopPosition}",
                this);

            return false;
        }

        float slopeAngle = Vector3.Angle(landingHit.normal, Vector3.up);
        if (slopeAngle > maxLandingSlope)
        {
            Debug.LogWarning(
                $"VAULT LANDING REJECTED | slope={slopeAngle:F2}, " +
                $"maxSlope={maxLandingSlope:F2}, hit={landingHit.point}",
                this);

            return false;
        }

        landingSurfacePosition = landingHit.point;
        landingRootPosition = landingHit.point - worldBottomOffset;
        return true;
    }

}
