using UnityEngine;

public enum VaultValidationResult
{
    Valid,
    TooHigh,
    TooLow,
    InvalidSurface,
    NoClearance,
    NoLandingSpace
}

public class VaultCandidate
{
    public bool IsValid { get; set; }
    public VaultValidationResult Result { get; set; }
    public bool HasClearance { get; set; }
    public bool HasLandingSpace { get; set; }

    // World-space point on the floor found by the landing raycast.
    public Vector3 LandingSurfacePosition { get; set; }

    // World-space position for the character root, accounting for the
    // CharacterController's bottom offset.
    public Vector3 LandingPosition { get; set; }

    // Sinusoidal arc amplitude used by VaultExecutor:
    // y(t) = Lerp(startY, landingY, t) + sin(pi * t) * RequiredArcHeight.
    public float RequiredArcHeight { get; set; }

    // Normalized time at which the trajectory passes the obstacle top.
    public float ObstacleTime { get; set; }
}
