using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerMovementIntent : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Facing")]
    [SerializeField] private float facingChangeDelay = 0.35f;



    public Vector3 MovementDirection { get; private set; }
    public Vector3 FacingDirection { get; private set; }
    public bool Sprinting { get; private set; }
    public bool Interaction { get; private set; }
    public bool InteractionHeld { get; private set; }
    public bool JumpPressed { get; private set; }

    private PlayerInputHandler inputHandler;
    private float facingChangeTimer;

    private void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        CalculateIntent();
    }

    private void CalculateIntent()
    {
        Sprinting =
            inputHandler.SprintHeld &&
            inputHandler.MoveInput.sqrMagnitude > 0.01f;

        Vector2 input = inputHandler.MoveInput;

        Interaction = inputHandler.Interaction;
        InteractionHeld = inputHandler.InteractionHeld;
        JumpPressed = inputHandler.JumpPressed;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        MovementDirection =
            forward * input.y +
            right * input.x;

        if (MovementDirection.sqrMagnitude > 1f)
        {
            MovementDirection.Normalize();
        }

        if (MovementDirection.sqrMagnitude > 0.001f)
        {
            Vector3 movementDirection =
                MovementDirection.normalized;

            if (input.y > 0.1f)
            {
                FacingDirection = movementDirection;
                facingChangeTimer = 0f;
            }
            else
            {
                facingChangeTimer += Time.deltaTime;

                if (facingChangeTimer >= facingChangeDelay)
                {
                    FacingDirection = movementDirection;
                }
            }
        }
        else
        {
            facingChangeTimer = 0f;
        }
    }
}