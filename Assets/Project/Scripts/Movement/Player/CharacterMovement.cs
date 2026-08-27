using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
public class CharacterMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float speedChangeRate = 8f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 2f;

    private float currentSpeed;

    private CharacterController characterController;
    private PlayerInputHandler inputHandler;

    private float verticalVelocity;
    private Vector3 movementDirection;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        inputHandler = GetComponent<PlayerInputHandler>();
        currentSpeed = moveSpeed;
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleGravity();
    }

    private void HandleMovement()
    {
        Vector2 input = inputHandler.MoveInput;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        movementDirection =
            forward * input.y +
            right * input.x;

        if (movementDirection.sqrMagnitude > 1f)
        {
            movementDirection.Normalize();
        }

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
        if (movementDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        Vector3 facingDirection = cameraTransform.forward;
        facingDirection.y = 0f;

        if (facingDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        facingDirection.Normalize();

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

        verticalVelocity += gravity * Time.deltaTime;

        if (inputHandler.JumpPressed &&
            characterController.isGrounded)
        {
            verticalVelocity =
                Mathf.Sqrt(
                    jumpHeight * -2f * gravity
                );
        }
    }

    private float CalculateFinalSpeed()
    {
        float targetSpeed = moveSpeed;

        bool isMoving =
            inputHandler.MoveInput.sqrMagnitude > 0.01f;

        if (inputHandler.SprintHeld && isMoving)
        {
            targetSpeed = sprintSpeed;
        }

        return targetSpeed;
    }
}