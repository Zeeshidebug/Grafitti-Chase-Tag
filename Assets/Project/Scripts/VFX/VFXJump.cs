using UnityEngine;

public class JumpVFXController : MonoBehaviour
{
    [Header("VFX Prefabs")]
    [SerializeField] private GameObject jumpStartVFX;
    [SerializeField] private GameObject jumpLandVFX;

    [Header("VFX Position Settings")]
    [SerializeField] private Transform jumpStartPoint;
    [SerializeField] private Transform jumpLandPoint;
    [Tooltip("Vertical offset from character origin when no emission point is assigned.")]
    [SerializeField] private float groundOffset = -0.9f;
    [SerializeField] private float destroyDelay = 2.0f;

    private CharacterState characterState;
    private CharacterMovement movement;

    private LocomotionState previousState;

    private void Awake()
    {
        characterState = GetComponent<CharacterState>();
        movement = GetComponent<CharacterMovement>();
    }

    private void OnEnable()
    {
        if (characterState != null) previousState = characterState.CurrentState;
        if (movement != null) movement.Jumped += OnJump;
    }

    private void OnDisable()
    {
        if (movement != null) movement.Jumped -= OnJump;
    }

    private void LateUpdate()
    {
        if (characterState == null) return;

        HandleJumpLandDetection();

        previousState = characterState.CurrentState;
    }

    public void ResetForRound()
    {
        // A teleport to the next spawn is not a landing from the previous jump.
        previousState = LocomotionState.Grounded;
    }

    private void OnJump() => SpawnVFX(jumpStartVFX, jumpStartPoint);
    private void HandleJumpLandDetection()
    {
        LocomotionState currentState = characterState.CurrentState;
        if (previousState == LocomotionState.Airborne && currentState == LocomotionState.Grounded)
        {
            SpawnVFX(jumpLandVFX, jumpLandPoint);
        }
    }
    private void SpawnVFX(GameObject vfxPrefab, Transform point)
    {
        if (vfxPrefab == null) return;
        Vector3 spawnPosition = point != null ? point.position : transform.position + Vector3.up * groundOffset;
        
        GameObject spawnedInstance = Instantiate(vfxPrefab, spawnPosition, point != null ? point.rotation : Quaternion.identity);

        Destroy(spawnedInstance, destroyDelay);
    }
}
