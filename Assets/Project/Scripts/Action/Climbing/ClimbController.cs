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

        Debug.Log("TryClimb is here! requesting input...");

        if (!movementIntent.Interaction)
            return;

        Debug.Log("Input Called, checking for state...");

        if (characterState.CurrentState != LocomotionState.Airborne)
            return;

        Debug.Log("State is Airborne, checking for candidate...");

        ClimbCandidate candidate =
            climbEvaluator.Evaluate(
                parkourDetector.CurrentObstacle
            );

        if (candidate == null || !candidate.IsValid)
            return;

        Debug.Log("Candidate is valid, executing climb...");


        climbExecutor.TryExecute(candidate);
    }
}
