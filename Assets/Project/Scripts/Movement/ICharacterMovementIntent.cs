using UnityEngine;

public interface ICharacterMovementIntent
{
    Vector3 MovementDirection { get; }
    Vector3 FacingDirection { get; }

    bool Sprinting { get; }
    bool JumpPressed { get; }
}
