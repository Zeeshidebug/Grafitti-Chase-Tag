using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(CharacterState))]
[RequireComponent(typeof(PlayerInputHandler))]
public class CharacterMovement : MonoBehaviour
{

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float speedChangeRate = 8f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float sprintStaminaCostRate = 0.05f;
    [SerializeField] private float jumpStaminaCostRate = 0.25f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 2f;

    [Header("Release Momentum")]
    [SerializeField] private float releaseMomentumDecay = 5f;

    private CharacterController characterController;
    private PlayerMovementIntent movementIntent;
    private PlayerInputHandler inputHandler;
    private CharacterState characterState;
    private StaminaSystem staminaSystem;
    private SlideExecutor slideExecutor;
    private PoleSpinExecutor poleSpinExecutor;
    private WallReboundExecutor wallReboundExecutor;
    private TicTacExecutor ticTacExecutor;

    private float currentSpeed;
    private float verticalVelocity;

    public float CurrentSpeed => currentSpeed;
    public Vector3 CurrentVelocity { get; private set; }
    public float VerticalVelocity => verticalVelocity;
    private Vector3 releaseMomentum;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        movementIntent = GetComponent<PlayerMovementIntent>();
        inputHandler = GetComponent<PlayerInputHandler>();
        currentSpeed = moveSpeed;
        characterState = GetComponent<CharacterState>();
        staminaSystem = GetComponent<StaminaSystem>();
        slideExecutor = GetComponent<SlideExecutor>();
        poleSpinExecutor = GetComponent<PoleSpinExecutor>();
        wallReboundExecutor = GetComponent<WallReboundExecutor>();
        ticTacExecutor = GetComponent<TicTacExecutor>();
    }

    private void Update()
    {
        UpdateLocomotionState();

        HandleMovement();
        HandleRotation();
        HandleGravity();
        HandleStaminaRegeneration();
        HandleSprintStamina();
    }

    private void HandleMovement()
    {
        Vector3 direction;

        if (slideExecutor.IsExecuting)
        {
            direction = slideExecutor.CurrentSlideDirection;
        }
        else if (poleSpinExecutor.IsExecuting)
        {
            direction =
                poleSpinExecutor.CurrentSpinDirection;
        }
        else if (wallReboundExecutor.IsExecuting)
        {
            direction =
                wallReboundExecutor.CurrentReboundDirection;

            // currentSpeed =
            //     wallReboundExecutor.ReboundSpeed;
        }

        else if (ticTacExecutor.IsExecuting)
        {
            direction =
                ticTacExecutor.CurrentTicTacDirection;

            currentSpeed =
                ticTacExecutor.LaunchSpeed;
        }
        else
        {
            direction = movementIntent.MovementDirection;
        }

        float targetSpeed = CalculateFinalSpeed();

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            speedChangeRate * Time.deltaTime
        );

        Vector3 velocity =
            direction * currentSpeed;

        velocity += releaseMomentum;

        velocity.y = verticalVelocity;

        CurrentVelocity = velocity;

        releaseMomentum =
    Vector3.MoveTowards(
        releaseMomentum,
        Vector3.zero,
        releaseMomentumDecay * Time.deltaTime
    );

        characterController.Move(
            velocity * Time.deltaTime
        );
    }

    private void HandleRotation()
    {
        Vector3 direction;

        if (poleSpinExecutor.IsExecuting)
        {
            direction =
                poleSpinExecutor.CurrentSpinDirection;
        }
        else if (wallReboundExecutor.IsExecuting)
        {
            direction =
                wallReboundExecutor.CurrentReboundDirection;
        }
        else if (ticTacExecutor.IsExecuting)
        {
            direction =
                ticTacExecutor.CurrentTicTacDirection;
        }
        else
        {
            direction =
                movementIntent.FacingDirection;
        }
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void HandleGravity()
    {
        if (characterController.isGrounded &&
            verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (inputHandler.JumpPressed &&
            characterState.CurrentState == LocomotionState.Grounded)
        {
            float staminaCost =
                staminaSystem.MaxStamina * jumpStaminaCostRate;

            if (!staminaSystem.TryConsume(staminaCost))
                return;

            verticalVelocity =
                Mathf.Sqrt(
                    jumpHeight * -2f * gravity
                );
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void UpdateLocomotionState()
    {
        if (characterState.CurrentState == LocomotionState.Vaulting ||
            characterState.CurrentState == LocomotionState.Sliding ||
            characterState.CurrentState == LocomotionState.PoleSpinning)
        {
            return;
        }

        if (characterController.isGrounded)
        {
            characterState.SetState(
                LocomotionState.Grounded
            );

            return;
        }

        characterState.SetState(
            LocomotionState.Airborne
        );
    }
    private float CalculateFinalSpeed()
    {
        float baseSpeed;

        if (staminaSystem.IsExhausted)
            baseSpeed = moveSpeed * 0.6f;
        else if (movementIntent.Sprinting)
            baseSpeed = sprintSpeed;
        else
            baseSpeed = moveSpeed;

        if (slideExecutor.IsExecuting)
            return baseSpeed * slideExecutor.SlideSpeedMultiplier;

        return baseSpeed;
    }

    private void HandleStaminaRegeneration()
    {
        if (movementIntent.Sprinting)
            return;

        if (characterState.CurrentState != LocomotionState.Grounded)
            return;

        staminaSystem.Regenerate(Time.deltaTime);
    }

    private void HandleSprintStamina()
    {
        if (!movementIntent.Sprinting)
            return;

        if (staminaSystem.IsExhausted)
            return;

        if (characterState.CurrentState != LocomotionState.Grounded)
            return;

        float staminaCost =
            staminaSystem.MaxStamina
            * sprintStaminaCostRate
            * Time.deltaTime;

        staminaSystem.ConsumeContinuous(staminaCost);
    }

    // Additional methods
    public void ApplyReleaseMomentum(Vector3 momentum)
    {
        releaseMomentum =
            new Vector3(
                momentum.x,
                0f,
                momentum.z
            );

        verticalVelocity =
            momentum.y;
    }

    public void SetVerticalVelocity(float velocity)
    {
        verticalVelocity = velocity;
    }
}