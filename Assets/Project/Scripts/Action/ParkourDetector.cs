using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ParkourDetector : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionDistance = 2f;
    [SerializeField] private float maxObstacleHeight = 3f;
    [SerializeField] private LayerMask parkourLayer;

    [Header("Pole Detection")]
    [SerializeField] private float poleDetectionRadius = 1.5f;
    [SerializeField] private LayerMask poleLayer;

    [Header("Wall Rebound Detection")]
    [SerializeField] private LayerMask wallReboundLayer;

    private CharacterController characterController;
    private Collider[] poleOverlaps = new Collider[8];

    // Optional direction override for AI/local path-aware detection.
    private Vector3 obstacleDetectionDirection;
    private bool hasCustomObstacleDetectionDirection;

    public ParkourObstacleData CurrentObstacle { get; private set; }
    public ParkourObstacleData CurrentPole { get; private set; }
    public ParkourObstacleData CurrentWallReboundSurface { get; private set; }

    public bool PoleDetected => CurrentPole != null && CurrentPole.IsValid;
    public bool ObstacleDetected => CurrentObstacle != null && CurrentObstacle.IsValid;
    public LayerMask ParkourLayer => parkourLayer;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Vector3 direction = hasCustomObstacleDetectionDirection
            ? obstacleDetectionDirection
            : transform.forward;

        DetectObstacle(direction);
        DetectPole();
        DetectWallReboundSurface();
    }

    public void SetObstacleDetectionDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            ClearObstacleDetectionDirection();
            return;
        }

        obstacleDetectionDirection = direction.normalized;
        hasCustomObstacleDetectionDirection = true;
    }

    public void ClearObstacleDetectionDirection()
    {
        obstacleDetectionDirection = Vector3.zero;
        hasCustomObstacleDetectionDirection = false;
    }

    private void DetectObstacle(Vector3 direction)
    {
        direction.y = 0f;

        if (!TryCastSurface(direction, parkourLayer, out ParkourObstacleData obstacle))
        {
            CurrentObstacle = null;
            return;
        }

        CurrentObstacle = obstacle;
        FindTopSurface(CurrentObstacle);
    }

    private bool TryCastSurface(
        Vector3 direction,
        LayerMask layerMask,
        out ParkourObstacleData obstacle)
    {
        obstacle = null;
        if (direction.sqrMagnitude <= 0.001f)
            return false;

        direction.Normalize();
        Vector3 capsuleCenter = transform.TransformPoint(characterController.center);
        CharacterControllerGeometry.GetCapsulePoints(
            characterController,
            capsuleCenter,
            out Vector3 point1,
            out Vector3 point2,
            out float radius);

        if (!Physics.CapsuleCast(
                point1,
                point2,
                radius,
                direction,
                out RaycastHit hit,
                detectionDistance,
                layerMask,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        obstacle = new ParkourObstacleData
        {
            IsValid = true,
            HitPoint = hit.point,
            SurfaceNormal = hit.normal,
            Distance = hit.distance,
            ObstacleCollider = hit.collider,
            ObstacleBounds = hit.collider != null ? hit.collider.bounds : new Bounds(),
            HasObstacleBounds = hit.collider != null
        };

        return true;
    }

    private void FindTopSurface(ParkourObstacleData obstacle)
    {
        Vector3 topOrigin =
            obstacle.HitPoint
            - obstacle.SurfaceNormal * 0.15f
            + Vector3.up * maxObstacleHeight;

        if (!Physics.Raycast(
                topOrigin,
                Vector3.down,
                out RaycastHit topHit,
                maxObstacleHeight,
                parkourLayer,
                QueryTriggerInteraction.Ignore))
        {
            obstacle.IsValid = false;
            return;
        }

        obstacle.TopPosition = topHit.point;

        // Measure obstacle height from the CharacterController's bottom,
        // rather than from the GameObject pivot.
        Vector3 worldBottom = transform.position
            + CharacterControllerGeometry.GetWorldBottomOffset(characterController);

        obstacle.Height = topHit.point.y - worldBottom.y;
    }

    private void DetectPole()
    {
        int overlapCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            poleDetectionRadius,
            poleOverlaps,
            poleLayer,
            QueryTriggerInteraction.Ignore);

        if (overlapCount == poleOverlaps.Length)
        {
            // A full buffer may omit the closest pole. Keep all hits and grow
            // the retained buffer so dense areas do not allocate every frame.
            Collider[] allOverlaps = Physics.OverlapSphere(
                transform.position,
                poleDetectionRadius,
                poleLayer,
                QueryTriggerInteraction.Ignore);
            poleOverlaps = new Collider[Mathf.Max(poleOverlaps.Length * 2, allOverlaps.Length + 1)];
            System.Array.Copy(allOverlaps, poleOverlaps, allOverlaps.Length);
            overlapCount = allOverlaps.Length;
        }

        if (overlapCount == 0)
        {
            CurrentPole = null;
            return;
        }

        Collider closestCollider = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < overlapCount; i++)
        {
            Collider collider = poleOverlaps[i];
            poleOverlaps[i] = null;
            Vector3 closestPoint = collider.ClosestPoint(transform.position);
            float distance = Vector3.Distance(transform.position, closestPoint);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestCollider = collider;
            }
        }

        if (closestCollider == null)
        {
            CurrentPole = null;
            return;
        }

        Vector3 poleCenter = closestCollider.bounds.center;
        Vector3 radialDirection = transform.position - poleCenter;
        radialDirection.y = 0f;

        if (radialDirection.sqrMagnitude <= 0.001f)
        {
            CurrentPole = null;
            return;
        }

        radialDirection.Normalize();

        CurrentPole = new ParkourObstacleData
        {
            IsValid = true,
            HitPoint = new Vector3(
                poleCenter.x,
                transform.position.y,
                poleCenter.z),
            SurfaceNormal = radialDirection,
            Distance = closestDistance,
            ObstacleCollider = closestCollider,
            ObstacleBounds = closestCollider.bounds,
            HasObstacleBounds = true
        };
    }

    private void DetectWallReboundSurface()
    {
        CurrentWallReboundSurface = TryCastSurface(
            transform.forward,
            wallReboundLayer,
            out ParkourObstacleData surface)
                ? surface
                : null;
    }
}
