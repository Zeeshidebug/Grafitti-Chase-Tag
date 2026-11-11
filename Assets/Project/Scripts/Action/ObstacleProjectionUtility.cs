using UnityEngine;

/// <summary>
/// Finds an obstacle's far edge along a movement direction.
/// BoxCollider uses its actual transformed local axes. Other collider types
/// use the stored world-space AABB as a conservative fallback.
/// </summary>
public static class ObstacleProjectionUtility
{
    public static bool TryGetProjectedFarEdge(
        ParkourObstacleData obstacle,
        Vector3 direction,
        out float farEdgeProjection)
    {
        farEdgeProjection = 0f;

        if (obstacle == null)
            return false;

        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
            return false;

        direction.Normalize();

        BoxCollider box = obstacle.ObstacleCollider as BoxCollider;
        if (box != null)
        {
            return TryGetBoxFarEdge(box, direction, out farEdgeProjection);
        }

        if (obstacle.HasObstacleBounds)
        {
            Bounds bounds = obstacle.ObstacleBounds;
            Vector3 extents = bounds.extents;
            float projectedRadius =
                Mathf.Abs(direction.x) * extents.x +
                Mathf.Abs(direction.z) * extents.z;

            farEdgeProjection =
                Vector3.Dot(bounds.center, direction) + projectedRadius;
            return true;
        }

        return false;
    }

    private static bool TryGetBoxFarEdge(
        BoxCollider box,
        Vector3 direction,
        out float farEdgeProjection)
    {
        farEdgeProjection = 0f;

        if (box == null || !box.enabled || !box.gameObject.activeInHierarchy)
            return false;

        Transform boxTransform = box.transform;
        Vector3 halfSize = box.size * 0.5f;

        // TransformVector incorporates the collider's full transform hierarchy,
        // including rotation and scale. Each vector is a transformed half-axis.
        Vector3 halfAxisX = boxTransform.TransformVector(Vector3.right * halfSize.x);
        Vector3 halfAxisY = boxTransform.TransformVector(Vector3.up * halfSize.y);
        Vector3 halfAxisZ = boxTransform.TransformVector(Vector3.forward * halfSize.z);

        float projectedRadius =
            Mathf.Abs(Vector3.Dot(direction, halfAxisX)) +
            Mathf.Abs(Vector3.Dot(direction, halfAxisY)) +
            Mathf.Abs(Vector3.Dot(direction, halfAxisZ));

        Vector3 worldCenter = boxTransform.TransformPoint(box.center);
        float centerProjection = Vector3.Dot(worldCenter, direction);

        if (float.IsNaN(projectedRadius) || float.IsInfinity(projectedRadius) ||
            float.IsNaN(centerProjection) || float.IsInfinity(centerProjection))
        {
            return false;
        }

        farEdgeProjection = centerProjection + projectedRadius;
        return true;
    }
}
