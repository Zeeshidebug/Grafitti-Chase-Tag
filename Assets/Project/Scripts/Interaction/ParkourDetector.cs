using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ParkourDetector : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionDistance = 2f;
    [SerializeField] private float maxObstacleHeight = 3f;
    [SerializeField] private LayerMask parkourLayer;

    private CharacterController characterController;

    public ParkourObstacleData CurrentObstacle { get; private set; }

    public bool ObstacleDetected =>
        CurrentObstacle != null &&
        CurrentObstacle.IsValid;

    public float ObstacleHeight =>
        CurrentObstacle?.Height ?? 0f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        DetectObstacle();
    }

    private void DetectObstacle()
    {
        GetCapsulePoints(
            out Vector3 point1,
            out Vector3 point2,
            out float radius
        );

        bool hitObstacle = Physics.CapsuleCast(
            point1,
            point2,
            radius,
            transform.forward,
            out RaycastHit hit,
            detectionDistance,
            parkourLayer
        );

        if (!hitObstacle)
        {
            CurrentObstacle = null;
            return;
        }

        CurrentObstacle = new ParkourObstacleData
        {
            IsValid = true,
            HitPoint = hit.point,
            SurfaceNormal = hit.normal,
            Distance = hit.distance
        };

        FindTopSurface(CurrentObstacle);
    }

    private void GetCapsulePoints(
        out Vector3 point1,
        out Vector3 point2,
        out float radius
    )
    {
        radius = characterController.radius;

        Vector3 center =
            transform.position +
            characterController.center;

        float height =
            Mathf.Max(
                characterController.height,
                radius * 2f
            );

        float cylinderHeight =
            height - radius * 2f;

        point1 =
            center +
            Vector3.up * (cylinderHeight * 0.5f);

        point2 =
            center -
            Vector3.up * (cylinderHeight * 0.5f);
    }

    private void FindTopSurface(
        ParkourObstacleData obstacle
    )
    {
        Vector3 topOrigin =
            obstacle.HitPoint +
            Vector3.up * maxObstacleHeight;

        if (!Physics.Raycast(
            topOrigin,
            Vector3.down,
            out RaycastHit topHit,
            maxObstacleHeight,
            parkourLayer))
        {
            obstacle.IsValid = false;
            return;
        }

        obstacle.TopPosition = topHit.point;

        obstacle.Height =
            topHit.point.y -
            transform.position.y;
    }

    // private void OnDrawGizmosSelected()
    // {
    //     Vector3 origin =
    //         transform.position +
    //         Vector3.up * detectionHeight;

    //     Gizmos.DrawRay(
    //         origin,
    //         transform.forward * detectionDistance
    //     );
    // }
}