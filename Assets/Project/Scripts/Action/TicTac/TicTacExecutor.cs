using UnityEngine;

public class TicTacExecutor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterMovement characterMovement;
    [SerializeField] private CharacterState characterState;
    [SerializeField] private StaminaSystem staminaSystem;

    [Header("Settings")]
    [SerializeField] private float staminaCostMultiplier = 0.25f;
    [SerializeField] private float verticalLaunchVelocity = 7f;

    public bool IsExecuting { get; private set; }

    private bool hasLeftGround;

    public Vector3 CurrentTicTacDirection
    {
        get;
        private set;
    }

    public float LaunchSpeed
    {
        get;
        private set;
    }

    private void Update()
    {
        if (!IsExecuting)
            return;

        if (characterState.CurrentState != LocomotionState.Grounded)
        {
            hasLeftGround = true;
        }

        if (hasLeftGround &&
            characterState.CurrentState == LocomotionState.Grounded)
        {
            FinishTicTac();
        }
    }

    public bool TryExecute(
        TicTacCandidate candidate)
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

        CurrentTicTacDirection =
            candidate.TicTacDirection;

        LaunchSpeed =
            candidate.LaunchSpeed;

        characterMovement.SetVerticalVelocity(
            verticalLaunchVelocity
        );

        IsExecuting = true;
        hasLeftGround = false;

        characterState.SetState(
            LocomotionState.TicTacing
        );

        return true;
    }

    public void FinishTicTac()
    {
        IsExecuting = false;

        characterState.SetState(
            LocomotionState.Airborne
        );
    }
}