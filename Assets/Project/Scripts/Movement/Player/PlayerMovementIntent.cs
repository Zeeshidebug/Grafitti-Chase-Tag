using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerMovementIntent : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    public Vector3 MovementDirection { get; private set; }
    public Vector3 FacingDirection { get; private set; }
    public bool Sprinting { get; private set; }
    public bool Interaction { get; private set; }

    private PlayerInputHandler inputHandler;

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

        FacingDirection = forward;
    }
}