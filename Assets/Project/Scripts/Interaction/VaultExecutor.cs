using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class VaultExecutor : MonoBehaviour
{
    [Header("Vault")]
    [SerializeField] private float vaultDuration = 0.6f;

    private CharacterController characterController;

    private CharacterState characterMovement;

    private VaultCandidate activeCandidate;

    private PlayerInputHandler inputHandler;

    private bool isExecuting;

    private float elapsedTime;
    private float currentArcHeight;
    private Vector3 startPosition;
    private Vector3 landingPosition;

    private void Awake()
    {
        characterController =
            GetComponent<CharacterController>();

        characterMovement =
            GetComponent<CharacterState>();

        inputHandler =
            GetComponent<PlayerInputHandler>();
    }

    public bool IsExecuting => isExecuting;

    public bool TryExecute(VaultCandidate candidate)
    {

        if (candidate == null)
            return false;

        if (!candidate.IsValid)
            return false;

        if (isExecuting)
            return false;

        activeCandidate = candidate;

        startPosition =
            transform.position;

        landingPosition =
            candidate.LandingPosition;

        elapsedTime = 0f;

        currentArcHeight =
    candidate.RequiredArcHeight;

        Debug.Log(
        $"VAULT SNAPSHOT: " +
        $"Arc={candidate.RequiredArcHeight:F2}"
    );

        isExecuting = true;

        characterMovement.SetState(
            LocomotionState.Vaulting
        );

        return true;
    }

    private void Update()
    {
        if (!isExecuting)
            return;

        ExecuteVault();
    }


    private void ExecuteVault()
    {
        elapsedTime += Time.deltaTime;

        float t =
            Mathf.Clamp01(
                elapsedTime / vaultDuration
            );

        Vector3 position =
            Vector3.Lerp(
                startPosition,
                landingPosition,
                t
            );

        float arc =
            Mathf.Sin(t * Mathf.PI)
            * currentArcHeight;

        position +=
            Vector3.up * arc;

        Vector3 movement =
            position - transform.position;

        characterController.Move(movement);

        if (t >= 1f)
        {
            FinishVault();
        }
    }

    private void FinishVault()
    {
        isExecuting = false;

        activeCandidate = null;

        characterMovement.SetState(
            LocomotionState.Grounded
        );
    }
}