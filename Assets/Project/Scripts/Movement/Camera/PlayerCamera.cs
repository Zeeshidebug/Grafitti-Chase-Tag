using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot;

    [Header("Camera Settings")]
    [SerializeField] private float sensitivity = 2f;
    [SerializeField] private float distance = 5f;

    [Header("Rotation Limits")]
    [SerializeField] private float minYaw = -75f;
    [SerializeField] private float maxYaw = 75f;
    [SerializeField] private float minPitch = -15f;
    [SerializeField] private float maxPitch = 20f;

    [Header("Sprint FOV")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float sprintFOV = 70f;
    [SerializeField] private float fovChangeSpeed = 8f;

    private PlayerInputHandler inputHandler;

    private Camera playerCamera;

    private float yaw;
    private float pitch;

    private void Awake()
    {
        inputHandler =
            cameraPivot.GetComponentInParent<PlayerInputHandler>();

        playerCamera = GetComponent<Camera>();

        playerCamera.fieldOfView = normalFOV;
    }

    private void LateUpdate()
    {
        HandleRotation();
        HandlePosition();
        HandleFOV();
    }

    private void HandleRotation()
    {
        Vector2 lookInput = inputHandler.LookInput;

        yaw += lookInput.x * sensitivity;
        pitch -= lookInput.y * sensitivity;

        yaw = Mathf.Clamp(yaw, minYaw, maxYaw);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        cameraPivot.localRotation = Quaternion.Euler(0f, yaw, 0f);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandlePosition()
    {
        transform.localPosition = new Vector3(0f, 0f, -distance);
    }

    private void HandleFOV()
    {
        bool isMoving =
            inputHandler.MoveInput.sqrMagnitude > 0.01f;

        bool isSprinting =
            inputHandler.SprintHeld && isMoving;

        float targetFOV =
            isSprinting
                ? sprintFOV
                : normalFOV;

        playerCamera.fieldOfView = Mathf.MoveTowards(
            playerCamera.fieldOfView,
            targetFOV,
            fovChangeSpeed * Time.deltaTime
        );
    }
}