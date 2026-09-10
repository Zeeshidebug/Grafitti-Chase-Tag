using UnityEngine;

public enum WallReboundValidationResult
{
    Valid,
    NoWall,
    TooSlow
}

public class WallReboundCandidate
{
    public bool IsValid { get; set; }

    public WallReboundValidationResult Result { get; set; }

    public Vector3 WallNormal { get; set; }

    public Vector3 ReboundDirection { get; set; }

    public float ImpactSpeed { get; set; }
}