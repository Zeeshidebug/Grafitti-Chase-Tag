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
    private CharacterController capsule;

    private LocomotionState previousState;
    private bool waitingForSpawnContact;

    private void Awake()
    {
        characterState = GetComponent<CharacterState>();
        movement = GetComponent<CharacterMovement>();
        capsule = GetComponent<CharacterController>();
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

        var state = characterState.CurrentState;
        if (waitingForSpawnContact)
        {
            // SpawnAt reenables the capsule before its next Move updates isGrounded.
            // The initial contact at the marker should not produce a landing burst.
            if (movement != null && movement.CurrentVelocity.y < 0f &&
                capsule.isGrounded && state == LocomotionState.Grounded)
                waitingForSpawnContact = false;
        }
        else if (previousState == LocomotionState.Airborne && state == LocomotionState.Grounded)
        {
            SpawnVFX(jumpLandVFX, jumpLandPoint);
        }
        previousState = state;
    }

    public void ResetForRound()
    {
        // A teleport to the next spawn is not a landing from the previous jump.
        previousState = LocomotionState.Grounded;
        waitingForSpawnContact = true;
    }

    private void OnJump()
    {
        waitingForSpawnContact = false;
        SpawnVFX(jumpStartVFX, jumpStartPoint);
    }

    private void SpawnVFX(GameObject vfxPrefab, Transform point)
    {
        if (vfxPrefab == null) return;
        Vector3 spawnPosition = point != null ? point.position : transform.position + Vector3.up * groundOffset;
        
        GameObject spawnedInstance = Instantiate(vfxPrefab, spawnPosition, point != null ? point.rotation : Quaternion.identity);

        Destroy(spawnedInstance, destroyDelay);
    }
}
