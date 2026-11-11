using UnityEngine;

public class ParkourObstacleData
{
    public bool IsValid { get; set; }

    public Vector3 HitPoint { get; set; }
    public Vector3 SurfaceNormal { get; set; }
    public Vector3 TopPosition { get; set; }

    // The collider and world-space bounds hit by ParkourDetector.
    // Bounds are used to estimate the obstacle's full footprint along
    // the current vault direction, not just its front-facing top point.
    public Collider ObstacleCollider { get; set; }
    public Bounds ObstacleBounds { get; set; }
    public bool HasObstacleBounds { get; set; }

    public float Distance { get; set; }
    public float Height { get; set; }
}
