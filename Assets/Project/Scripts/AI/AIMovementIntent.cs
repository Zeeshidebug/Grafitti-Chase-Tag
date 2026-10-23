
using UnityEngine;

public class AIMovementIntent : MonoBehaviour,
    ICharacterMovementIntent
{
    public Vector3 MovementDirection { get; private set; }
    public Vector3 FacingDirection { get; private set; }
    public bool Sprinting { get; private set; }
    public bool JumpPressed { get; private set; }

    public void SetMovementIntent(
        Vector3 movementDirection,
        Vector3 facingDirection,
        bool sprinting = false)
    {
        movementDirection.y = 0f;
        facingDirection.y = 0f;

        MovementDirection =
            movementDirection.sqrMagnitude > 1f
                ? movementDirection.normalized
                : movementDirection;

        if (facingDirection.sqrMagnitude > 0.001f)
        {
            FacingDirection = facingDirection.normalized;
        }

        Sprinting = sprinting;
    }

    public void StopMovement()
    {
        MovementDirection = Vector3.zero;
        Sprinting = false;
    }

    public void SetJumpPressed(bool pressed)
    {
        JumpPressed = pressed;
    }
}
