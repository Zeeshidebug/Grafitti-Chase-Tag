using UnityEngine;

public enum ParkourValidationResult
{
    Valid,
    NoObstacle,
    TooHigh,
    TooLow,
    InvalidSurface,
    NoClearance,
    NoLandingSpace
}

public class ParkourCandidate
{
    public bool CanVault { get; set; }

    public ParkourValidationResult VaultResult
    {
        get;
        set;
    }

    public bool HasClearance { get; set; }
    public bool HasLandingSpace { get; set; }

    public bool CanClimb { get; set; }
}