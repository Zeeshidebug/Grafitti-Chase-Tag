using UnityEngine;

public class VaultCandidate
{
    public bool IsValid { get; set; }

    public ParkourValidationResult Result { get; set; }

    public bool HasClearance { get; set; }

    public bool HasLandingSpace { get; set; }

    public Vector3 LandingPosition { get; set; }

    public Vector3 LandingNormal { get; set; }

    public float ObstacleHeight { get; set; }

    public float RequiredArcHeight { get; set; }
}