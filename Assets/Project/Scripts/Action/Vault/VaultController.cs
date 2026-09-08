using UnityEngine;

public class VaultController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovementIntent movementIntent;
    [SerializeField] private CharacterState characterState;
    [SerializeField] private VaultExecutor vaultExecutor;
    [SerializeField] private VaultEvaluator vaultEvaluator;
    [SerializeField] private ParkourDetector parkourDetector;

    private void Update()
    {
        TryVault();
    }
    private void TryVault()
    {
        if (!movementIntent.Interaction)
            return;

        if (!movementIntent.Sprinting)
            return;

        if (characterState.CurrentState != LocomotionState.Grounded)
            return;

        VaultCandidate candidate =
            vaultEvaluator.Evaluate(
                parkourDetector.CurrentObstacle
            );

        if (candidate == null || !candidate.IsValid)
            return;

        vaultExecutor.TryExecute(candidate);
    }
}