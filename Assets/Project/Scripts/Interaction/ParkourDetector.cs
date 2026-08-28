using UnityEngine;

public class ParkourDetector : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionDistance = 2f;
    [SerializeField] private float detectionHeight = 1f;
    [SerializeField] private float maxObstacleHeight = 3f;
    [SerializeField] private LayerMask parkourLayer;

    private bool obstacleDetected;
    private float obstacleHeight;

    public bool ObstacleDetected => obstacleDetected;
    public float ObstacleHeight => obstacleHeight;

    private void Update()
    {
        DetectObstacle();
    }

    private void DetectObstacle()
    {
        Vector3 origin =
            transform.position +
            Vector3.up * detectionHeight;

        obstacleDetected = Physics.Raycast(
            origin,
            transform.forward,
            out RaycastHit hit,
            detectionDistance,
            parkourLayer
        );

        if (!obstacleDetected)
        {
            obstacleHeight = 0f;
            return;
        }

        FindObstacleHeight(hit);
    }

    private void FindObstacleHeight(RaycastHit obstacleHit)
    {
        Vector3 topOrigin =
            obstacleHit.point +
            Vector3.up * maxObstacleHeight;

        if (Physics.Raycast(
            topOrigin,
            Vector3.down,
            out RaycastHit topHit,
            maxObstacleHeight,
            parkourLayer))
        {
            obstacleHeight =
                topHit.point.y - transform.position.y;
        }
        else
        {
            obstacleHeight = 0f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin =
            transform.position +
            Vector3.up * detectionHeight;

        Gizmos.DrawRay(
            origin,
            transform.forward * detectionDistance
        );
    }
}