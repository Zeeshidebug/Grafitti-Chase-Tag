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

    private CharacterController characterController;
    private PlayerMovementIntent movementIntent;
    private PlayerInputHandler inputHandler;
    private CharacterState characterState;
    private StaminaSystem staminaSystem;

    private float currentSpeed;
    private float verticalVelocity;

    public float CurrentSpeed => currentSpeed;
    public float VerticalVelocity => verticalVelocity; private Vector3 movementDirection;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        movementIntent = GetComponent<PlayerMovementIntent>();
        inputHandler = GetComponent<PlayerInputHandler>();
        currentSpeed = moveSpeed;
        characterState = GetComponent<CharacterState>();
        staminaSystem = GetComponent<StaminaSystem>();
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
        movementDirection =
            movementIntent.MovementDirection;

        float targetSpeed = CalculateFinalSpeed();

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            speedChangeRate * Time.deltaTime
        );

        Vector3 velocity =
            movementDirection * currentSpeed;

        velocity.y = verticalVelocity;

        characterController.Move(
            velocity * Time.deltaTime
        );
    }

    private void HandleRotation()
    {
        Vector3 facingDirection =
            movementIntent.FacingDirection;

        if (facingDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(facingDirection);

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
        if (characterState.CurrentState == LocomotionState.Vaulting)
            return;

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
        if (staminaSystem.IsExhausted)
            return moveSpeed * 0.6f;

        if (movementIntent.Sprinting)
            return sprintSpeed;

        return moveSpeed;
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
}