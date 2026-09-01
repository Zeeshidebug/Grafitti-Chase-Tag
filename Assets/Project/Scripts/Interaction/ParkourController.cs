using UnityEngine;

public class ParkourController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovementIntent movementIntent;
    [SerializeField] private CharacterState characterState;
    [SerializeField] private ParkourCandidateEvaluator candidateEvaluator;
    [SerializeField] private VaultExecutor vaultExecutor;

    private void Update()
    {
        TryParkour();
    }
    private void TryParkour()
    {
        // Player tidak meminta interaction.
        if (!movementIntent.Interaction)
        {
            return;
        }

        // Vault hanya boleh dilakukan saat sprinting.
        if (!movementIntent.Sprinting)
        {
            return;
        }

        // Jangan mulai parkour ketika sedang dalam aksi lain.
        if (characterState.CurrentState != LocomotionState.Grounded)
        {
            return;
        }

        VaultCandidate candidate =
            candidateEvaluator.CurrentVaultCandidate;

        // Tidak ada Vault candidate yang valid.
        if (candidate == null || !candidate.IsValid)
        {
            return;
        }

        vaultExecutor.TryExecute(candidate);
    }
}