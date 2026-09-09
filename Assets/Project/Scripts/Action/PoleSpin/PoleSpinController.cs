using UnityEngine;

public class PoleSpinController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PoleSpinEvaluator poleSpinEvaluator;
    [SerializeField] private PoleSpinExecutor poleSpinExecutor;
    [SerializeField] private ParkourDetector parkourDetector;
    private PlayerMovementIntent movementIntent;

    private void Awake()
    {
        movementIntent = GetComponent<PlayerMovementIntent>();
    }

    private void Update()
    {
        TryPoleSpin();
    }

    private void TryPoleSpin()
    {

        if (!poleSpinExecutor.IsExecuting)
        {

            if (movementIntent.InteractionHeld)
            {
                PoleSpinCandidate candidate =
                    poleSpinEvaluator.Evaluate(
                        parkourDetector.CurrentPole
                    );

                if (candidate == null || !candidate.IsValid)
                    return;

                poleSpinExecutor.TryExecute(candidate);
            }
        }
        else
        {
            if (!movementIntent.InteractionHeld)
            {
                poleSpinExecutor.FinishSpin();
            }
        }
    }
}