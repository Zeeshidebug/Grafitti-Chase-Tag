using UnityEngine;

public enum PoleSpinValidationResult
{
    Valid,
    NoPole,
    TooFar
}

public class PoleSpinCandidate
{
    public bool IsValid { get; set; }

    public PoleSpinValidationResult Result { get; set; }

    public Vector3 PolePosition { get; set; }

    public Vector3 PoleNormal { get; set; }
}