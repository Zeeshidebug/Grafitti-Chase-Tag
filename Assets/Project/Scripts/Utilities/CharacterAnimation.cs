using UnityEngine;

public class CharacterAnimation : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovementIntent movementIntent;

    private static readonly int MoveSpeedHash =
        Animator.StringToHash("MoveSpeed");

    private static readonly int StrafeHash =
        Animator.StringToHash("Strafe");

    private void Update()
    {
        UpdateLocomotionAnimation();
    }

    private void UpdateLocomotionAnimation()
    {
        float targetSpeed = 0f;

        if (movementIntent.MovementDirection.sqrMagnitude > 0.001f)
        {
            targetSpeed =
                movementIntent.Sprinting
                    ? 1f
                    : 0.5f;
        }

        animator.SetFloat(
            MoveSpeedHash,
            targetSpeed,
            0.1f,
            Time.deltaTime
        );

        animator.SetFloat(
            StrafeHash,
            movementIntent.StrafeAmount
        );
    }
}