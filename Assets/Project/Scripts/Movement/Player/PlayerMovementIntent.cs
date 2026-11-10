using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
[DefaultExecutionOrder(-100)]
public class PlayerMovementIntent : MonoBehaviour, ICharacterMovementIntent
{
    [Header("References")]
    [Tooltip("Rendered camera, used only to read the movement basis. Cinemachine owns look input.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Facing")]
    [SerializeField, Min(0f)] private float facingChangeDelay = 0.35f;

    public Vector3 MovementDirection { get; private set; }
    public Vector3 FacingDirection { get; private set; }
    public bool Sprinting { get; private set; }
    public bool Interaction { get; private set; }
    public bool InteractionHeld { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool Tagging { get; private set; }

    private PlayerInputHandler inputHandler;
    private float facingChangeTimer;
    private Vector3 lastCameraForward;

    private void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        FacingDirection = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (FacingDirection.sqrMagnitude < 0.001f)
            FacingDirection = Vector3.forward;
        lastCameraForward = FacingDirection;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform == null)
            Debug.LogWarning("PlayerMovementIntent has no camera; using character-relative movement.", this);
    }

    public void OnSpawn(Vector3 positionDelta)
    {
        Unity.Cinemachine.CinemachineCore.OnTargetObjectWarped(transform, positionDelta);
        FacingDirection = transform.forward;
        facingChangeTimer = 0f;
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
        Tagging = inputHandler.IsTagging;

        // Reading the camera basis does not rotate it. Keep the last valid yaw
        // when looking straight up/down, and build an orthogonal planar basis.
        Vector3 forward = Vector3.ProjectOnPlane(
            cameraTransform != null ? cameraTransform.forward : transform.forward,
            Vector3.up);
        if (forward.sqrMagnitude > 0.001f)
            lastCameraForward = forward.normalized;
        forward = lastCameraForward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);

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

    public float StrafeAmount
    {
        get
        {
            if (MovementDirection.sqrMagnitude < 0.001f)
                return 0f;

            Vector3 movement =
                MovementDirection.normalized;

            Vector3 facing =
                FacingDirection.normalized;

            Vector3 right =
                Vector3.Cross(
                    Vector3.up,
                    facing
                );

            return Vector3.Dot(
                movement,
                right
            );
        }
    }


}
