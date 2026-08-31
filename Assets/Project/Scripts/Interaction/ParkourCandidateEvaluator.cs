using UnityEngine;

public class ParkourCandidateEvaluator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParkourDetector detector;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private LayerMask parkourLayer;
    [SerializeField] private VaultEvaluator vaultEvaluator;

    public VaultCandidate CurrentVaultCandidate
    {
        get;
        private set;
    }

    private void Awake()
    {
        characterController =
            GetComponent<CharacterController>();
    }

    private void Update()
    {
        CurrentVaultCandidate =
            vaultEvaluator.Evaluate(
                detector.CurrentObstacle
            );
    }
}