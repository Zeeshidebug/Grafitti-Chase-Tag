using UnityEngine;

public class ParkourObstacleData
{
    public bool IsValid { get; set; }

    public Vector3 HitPoint { get; set; }
    public Vector3 SurfaceNormal { get; set; }
    public Vector3 TopPosition { get; set; }

    public Vector3 LandingPosition { get; set; }
    public Vector3 LandingNormal { get; set; }

    public float Distance { get; set; }
    public float Height { get; set; }
}