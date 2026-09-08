using UnityEngine;

public enum ClimbValidationResult
{
    Valid,
    NoObstacle,
    TooHigh,
    TooLow,
    InvalidSurface,
    NoClearance,
    NoLandingSpace
}

public class ClimbCandidate
{
    public bool IsValid { get; set; }

    public ClimbValidationResult Result { get; set; }

    public Vector3 TopPosition { get; set; }

    public Vector3 ClimbPosition { get; set; }

    public ParkourObstacleData Obstacle { get; set; }
}