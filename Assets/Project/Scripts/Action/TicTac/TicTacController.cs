using UnityEngine;

public class TicTacController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TicTacEvaluator ticTacEvaluator;
    [SerializeField] private TicTacExecutor ticTacExecutor;
    [SerializeField] private ParkourDetector parkourDetector;
    [SerializeField] private CharacterMovement characterMovement;
    [SerializeField] private CharacterState characterState;

    private PlayerMovementIntent movementIntent;

    private void Awake()
    {
        movementIntent =
            GetComponent<PlayerMovementIntent>();
    }

    private void Update()
    {
        TryTicTac();
    }

    private void TryTicTac()
    {
        if (ticTacExecutor.IsExecuting)
            return;

        // Tic Tac hanya bisa dimulai saat airborne.
        if (characterState.CurrentState != LocomotionState.Airborne)
            return;

        // Second jump input.
        if (!movementIntent.JumpPressed)
            return;

        TicTacCandidate candidate =
            ticTacEvaluator.Evaluate(
                parkourDetector.CurrentObstacle,
                characterMovement.CurrentVelocity
            );

        if (candidate == null ||
            !candidate.IsValid)
        {
            return;
        }

        ticTacExecutor.TryExecute(candidate);
    }
}