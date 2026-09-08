using UnityEngine;

public class ClimbExecutor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private CharacterState characterState;
    [SerializeField] private StaminaSystem staminaSystem;


    [Header("Climb Settings")]
    [SerializeField] private float climbDuration = 0.4f;
    [SerializeField] private float staminaCostMultiplier = 0.3f;

    private bool isExecuting;
    private float elapsedTime;

    private Vector3 startPosition;
    private Vector3 targetPosition;

    public bool IsExecuting =>
        isExecuting;

    private void Awake()
    {
        characterController =
            GetComponent<CharacterController>();
    }

    public bool TryExecute(
        ClimbCandidate candidate)
    {
        if (isExecuting)
            return false;

        if (candidate == null ||
            !candidate.IsValid)
            return false;

        float climbCost =
        staminaSystem.MaxStamina *
        staminaCostMultiplier;

        if (!staminaSystem.TryConsume(climbCost))
            return false;

        startPosition =
            transform.position;

        targetPosition =
            candidate.TopPosition;

        targetPosition.y =
            candidate.TopPosition.y
            - GetCharacterBottomOffset();

        elapsedTime = 0f;
        isExecuting = true;

        characterState.SetState(
            LocomotionState.Climbing
        );

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

        if (t >= 1f)
        {
            FinishClimb();
        }
    }

    private void FinishClimb()
    {
        isExecuting = false;

        characterState.SetState(
            LocomotionState.Grounded
        );
    }

    private float GetCharacterBottomOffset()
    {
        float halfHeight =
            characterController.height * 0.5f;

        return characterController.center.y
            - halfHeight;
    }
}