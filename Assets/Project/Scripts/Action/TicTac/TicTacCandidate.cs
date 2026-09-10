using UnityEngine;

public enum TicTacValidationResult
{
    Valid,
    NoWall,
    TooFar
}

public class TicTacCandidate
{
    public bool IsValid { get; set; }

    public TicTacValidationResult Result { get; set; }

    public Vector3 TicTacDirection { get; set; }

    public Vector3 WallNormal { get; set; }

    public float LaunchSpeed { get; set; }
}