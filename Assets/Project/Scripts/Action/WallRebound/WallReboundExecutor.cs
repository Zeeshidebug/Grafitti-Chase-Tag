using UnityEngine;

public class WallReboundExecutor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WallReboundEvaluator evaluator;
    [SerializeField] private CharacterMovement characterMovement;
    [SerializeField] private CharacterState characterState;
    [SerializeField] private StaminaSystem staminaSystem;
    [SerializeField] private ParkourDetector parkourDetector;

    [Header("Settings")]
    [SerializeField] private float staminaCostMultiplier = 0.1f;
    [SerializeField] private float reboundSpeedMultiplier = 1f;
    [SerializeField] private float reboundDuration = 0.25f;

    private float reboundTimer;

    public bool IsExecuting { get; private set; }

    public Vector3 CurrentReboundDirection
    {
        get;
        private set;
    }

    public float ReboundSpeed
    {
        get;
        private set;
    }

    private void Update()
    {
        if (!IsExecuting)
            return;

        reboundTimer -= Time.deltaTime;

        if (reboundTimer <= 0f)
        {
            FinishRebound();
        }
    }

    private void OnControllerColliderHit(
        ControllerColliderHit hit)
    {
        if (IsExecuting)
            return;

        Debug.Log(
$"Hit: {hit.collider.name}, " +
$"Obstacle: {parkourDetector.CurrentObstacle}, " +
$"Velocity: {characterMovement.CurrentVelocity}"
);

        Vector3 velocity =
            characterMovement.CurrentVelocity;

        WallReboundCandidate candidate =
            evaluator.Evaluate(
                parkourDetector.CurrentObstacle,
                velocity
            );

        if (candidate == null ||
            !candidate.IsValid)
        {
            return;
        }

        TryExecute(candidate);
    }

    public bool TryExecute(
        WallReboundCandidate candidate)
    {
        if (IsExecuting)
            return false;

        if (candidate == null ||
            !candidate.IsValid)
        {
            return false;
        }

        float staminaCost =
            staminaSystem.MaxStamina *
            staminaCostMultiplier;

        if (!staminaSystem.TryConsume(staminaCost))
            return false;

        CurrentReboundDirection =
            candidate.ReboundDirection;

        ReboundSpeed =
            candidate.ImpactSpeed;

        Vector3 reboundVelocity =
            candidate.ReboundDirection *
            candidate.ImpactSpeed *
            reboundSpeedMultiplier;

        characterMovement.ApplyReleaseMomentum(
            reboundVelocity
        );

        IsExecuting = true;

        reboundTimer = reboundDuration;

        characterState.SetState(
            LocomotionState.WallRebounding
        );

        return true;
    }

    public void FinishRebound()
    {
        IsExecuting = false;

        characterState.SetState(
            LocomotionState.Airborne
        );
    }
}