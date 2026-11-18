using UnityEngine;

public class JumpVFXController : MonoBehaviour
{
    [Header("VFX Prefabs")]
    [SerializeField] private GameObject jumpStartVFX;
    [SerializeField] private GameObject jumpLandVFX;

    [Header("VFX Position Settings")]
    [SerializeField] private float groundOffset = -0.9f;
    [SerializeField] private float destroyDelay = 2.0f;

    private PlayerInputHandler inputHandler;
    private CharacterState characterState;
    private CharacterController characterController;

    private LocomotionState previousState;
    private bool wasGroundedLastFrame;

    private void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        characterState = GetComponent<CharacterState>();
        characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        if (characterState != null) previousState = characterState.CurrentState;
        if (characterController != null) wasGroundedLastFrame = characterController.isGrounded;
    }

    private void Update()
    {
        if (inputHandler == null || characterState == null || characterController == null) return;

        HandleJumpStartDetecion();
        HandleJumpLandDetection();

        previousState = characterState.CurrentState;
        wasGroundedLastFrame = characterController.isGrounded;
    }
    private void HandleJumpStartDetecion()
    {
        if (inputHandler.JumpPressed && wasGroundedLastFrame && characterState.CurrentState == LocomotionState.Grounded)
        {
            SpawnVFX(jumpStartVFX);
        }
    }
    private void HandleJumpLandDetection()
    {
        LocomotionState currentState = characterState.CurrentState;
        if (previousState == LocomotionState.Airborne && currentState == LocomotionState.Grounded)
        {
            SpawnVFX(jumpLandVFX);
        }
    }
    private void SpawnVFX(GameObject vfxPrefab)
    {
        if (vfxPrefab == null) return;
        Vector3 spawnPosition = new Vector3(transform.position.x, transform.position.y + groundOffset, transform.position.z);
        
        GameObject spawnedInstance = Instantiate(vfxPrefab, spawnPosition, Quaternion.identity);

        Destroy(spawnedInstance, destroyDelay);
    }
}
