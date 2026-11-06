using UnityEngine;

public class ClimbExecutor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private CharacterState characterState;
    [SerializeField] private StaminaSystem staminaSystem;
    [SerializeField] private Animator animator;
    [SerializeField] private StaminaHUDController staminaHUDController;


    [Header("Climb Settings")]
    [SerializeField] private float climbDuration = 0.4f;
    [SerializeField] private float staminaCostMultiplier = 0.3f;
    [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 2f;

    private AINavMeshDriver aiNavMeshDriver;

    private bool isExecuting;
    private float elapsedTime;

    private Vector3 startPosition;
    private Vector3 targetPosition;

    public bool IsExecuting =>
        isExecuting;

    private void Awake()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (characterState == null)
            characterState = GetComponent<CharacterState>();

        if (staminaSystem == null)
            staminaSystem = GetComponent<StaminaSystem>();

        aiNavMeshDriver = GetComponent<AINavMeshDriver>();
    }

    public bool TryExecute(
        ClimbCandidate candidate)
    {
        if (isExecuting ||
            candidate == null ||
            !candidate.IsValid ||
            characterController == null ||
            characterState == null ||
            staminaSystem == null)
            return false;

        float climbCost =
        staminaSystem.MaxStamina *
        staminaCostMultiplier;

        if (!staminaSystem.TryConsume(climbCost))
        {
            if (staminaHUDController != null)
                staminaHUDController.NotifyInsufficientStamina();
            return false;
        }


        startPosition =
            transform.position;

        targetPosition =
            candidate.TopPosition;

        if (candidate.Obstacle != null)
        {
            Vector3 climbDirection = -candidate.Obstacle.SurfaceNormal;
            climbDirection.y = 0f;

            if (climbDirection.sqrMagnitude > 0.001f)
            {
                climbDirection.Normalize();
                targetPosition += climbDirection *
                    (characterController.radius + 0.05f);
            }
        }

        targetPosition.y =
            candidate.TopPosition.y
            - GetCharacterBottomOffset();

        elapsedTime = 0f;
        isExecuting = true;

        if (aiNavMeshDriver != null)
            aiNavMeshDriver.BeginParkourTraversal();

        characterState.SetState(
            LocomotionState.Climbing
        );

        if (animator != null)
            animator.SetTrigger("Climb");

        return true;
    }

    private void Update()
    {
        if (!isExecuting)
            return;

        ExecuteClimb();
    }

    private void ExecuteClimb()
    {
        elapsedTime +=
            Time.deltaTime;

        float t =
            Mathf.Clamp01(
                elapsedTime / climbDuration
            );

        Vector3 target =
            Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );

        Vector3 movement =
            target - transform.position;

        characterController.Move(
            movement
        );

        if (t < 1f)
            return;

        float remainingDistance =
            Vector3.Distance(transform.position, targetPosition);

        if (remainingDistance <= 0.15f)
        {
            FinishClimb();
        }
        else if (elapsedTime >= climbDuration + 0.75f)
        {
            Debug.LogWarning(
                $"CLIMB TARGET NOT REACHED | position={transform.position}, " +
                $"target={targetPosition}, error={remainingDistance:F2}",
                this);
            FinishClimb();
        }
    }

    private void FinishClimb()
    {
        isExecuting = false;

        characterState.SetState(
            LocomotionState.Grounded
        );

        if (aiNavMeshDriver != null &&
            !aiNavMeshDriver.RecoverAfterParkour(navMeshSampleRadius))
        {
            Debug.LogWarning(
                $"CLIMB NAVMESH RECOVERY FAILED | position={transform.position}",
                this);
        }
    }

    private float GetCharacterBottomOffset()
    {
        float halfHeight =
            characterController.height * 0.5f;

        return characterController.center.y
            - halfHeight;
    }
}
