using UnityEngine;

public class ClimbController : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private PlayerMovementIntent movementIntent;
    [SerializeField] private CharacterState characterState;
    [SerializeField] private ClimbCandidate candidateEvaluator;
    [SerializeField] private ClimbExecutor climbExecutor;
    [SerializeField] private ParkourDetector parkourDetector;
    [SerializeField] private ClimbEvaluator climbEvaluator;

    private void Update()
    {
        TryClimb();
    }

    private void TryClimb()
    {
        if (!movementIntent.Interaction)
            return;

        if (characterState.CurrentState != LocomotionState.Airborne)
            return;

        ClimbCandidate candidate =
            climbEvaluator.Evaluate(
                parkourDetector.CurrentObstacle
            );

        if (candidate == null || !candidate.IsValid)
            return;

        climbExecutor.TryExecute(candidate);
    }
}
