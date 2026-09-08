using UnityEngine;

public class SlideController : MonoBehaviour
{
    [SerializeField]
    private PlayerMovementIntent movementIntent;

    [SerializeField]
    private CharacterState characterState;

    [SerializeField]
    private SlideExecutor slideExecutor;

    private void Update()
    {
        TrySlide();
    }

    private void TrySlide()
    {
        if (!movementIntent.Interaction)
            return;

        if (!movementIntent.Sprinting)
            return;

        if (characterState.CurrentState != LocomotionState.Grounded)
            return;

        Vector3 slideDirection = GetSlideDirection();

        if (slideDirection == Vector3.zero)
            return;

        slideExecutor.TryExecute(slideDirection);
    }

    private Vector3 GetSlideDirection()
    {
        Vector3 direction =
            movementIntent.MovementDirection;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return transform.forward;

        direction.Normalize();

        float backwardDot =
            Vector3.Dot(
                direction,
                -transform.forward
            );

        if (backwardDot > 0.5f)
            return Vector3.zero;

        return direction;
    }
}