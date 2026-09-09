using UnityEngine;

public class PoleSpinExecutor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private CharacterState characterState;
    [SerializeField] private CharacterMovement characterMovement;
    [SerializeField] private StaminaSystem staminaSystem;

    [Header("Settings")]
    [SerializeField] private float spinSpeed = 180f;

    [Header("Release Momentum")]
    [SerializeField] private float horizontalLaunchSpeed = 10f;
    [SerializeField] private float verticalLaunchSpeed = 2f;

    [Header("Stamina")]
    [SerializeField] private float staminaCostMultiplier = 0.5f;
    [SerializeField] private float minimumStaminaAfterSpin = 20f;

    public bool IsExecuting
    {
        get;
        private set;
    }

    public Vector3 CurrentPolePosition
    {
        get;
        private set;
    }

    public Vector3 CurrentSpinDirection
    {
        get;
        private set;
    }

    public Vector3 ReleaseVelocity
    {
        get;
        private set;
    }

    private Vector3 spinAxis;
    private Vector3 initialOffset;

    private float previousAngle;

    private float currentAngle;

    private void Update()
    {
        if (!IsExecuting)
            return;

        ExecuteSpin();
    }

    public bool TryExecute(
    PoleSpinCandidate candidate)
    {
        if (IsExecuting)
            return false;

        if (candidate == null ||
            !candidate.IsValid)
            return false;

        CurrentPolePosition =
            candidate.PolePosition;

        spinAxis = Vector3.up;

        initialOffset =
            transform.position -
            CurrentPolePosition;

        initialOffset =
            Vector3.ProjectOnPlane(
                initialOffset,
                spinAxis
            );

        if (initialOffset.sqrMagnitude <= 0.001f)
            return false;

        currentAngle = 0f;

        previousAngle = currentAngle;

        IsExecuting = true;

        characterState.SetState(
            LocomotionState.PoleSpinning
        );

        return true;
    }

    private void ExecuteSpin()
    {
        float deltaAngle =
            spinSpeed * Time.deltaTime;

        currentAngle += deltaAngle;

        if (!ConsumeSpinStamina(deltaAngle))
        {
            FinishSpin();
            return;
        }

        Vector3 rotatedOffset =
            Quaternion.AngleAxis(
                currentAngle,
                spinAxis
            ) * initialOffset;

        Vector3 targetPosition =
            CurrentPolePosition +
            rotatedOffset;

        Vector3 movement =
            targetPosition -
            transform.position;

        if (movement.sqrMagnitude > 0.001f)
        {
            CurrentSpinDirection =
                movement.normalized;
        }

        characterController.Move(
            movement
        );
    }

    public void FinishSpin()
    {
        PrepareReleaseMomentum();
        characterMovement.ApplyReleaseMomentum(
            ReleaseVelocity
        );

        IsExecuting = false;

        characterState.SetState(
            LocomotionState.Airborne
        );
    }

    private void PrepareReleaseMomentum()
    {
        Vector3 releaseVelocity =
            CurrentSpinDirection *
            horizontalLaunchSpeed;

        releaseVelocity.y =
            verticalLaunchSpeed;

        ReleaseVelocity =
            releaseVelocity;
    }

    private bool ConsumeSpinStamina(float deltaAngle)
    {
        float staminaCost =
            staminaSystem.MaxStamina *
            staminaCostMultiplier *
            (Mathf.Abs(deltaAngle) / 360f);

        float availableStamina =
            staminaSystem.CurrentStamina -
            minimumStaminaAfterSpin;

        if (availableStamina <= 0f)
        {
            return false;
        }

        if (staminaCost > availableStamina)
        {
            return false;
        }

        staminaSystem.ConsumeContinuous(
            staminaCost
        );

        return true;
    }
}