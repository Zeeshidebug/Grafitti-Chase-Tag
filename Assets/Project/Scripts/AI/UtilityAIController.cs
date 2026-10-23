
using UnityEngine;
using UnityEngine.AI;

public class UtilityAIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoleController roleController;
    [SerializeField] private Transform player;
    [SerializeField] private AINavMeshDriver navMeshDriver;
    [SerializeField] private NavMeshAgent navMeshAgent;

    [Header("Decision")]
    [SerializeField] private float decisionInterval = 0.5f;
    [SerializeField] private float destinationSampleRadius = 2f;

    [Header("Evade")]
    [SerializeField] private float evadeSearchRadius = 7f;
    [SerializeField] private int evadeCandidateCount = 8;

    private float decisionTimer;
    private CharacterRole lastRole = CharacterRole.None;

    private void Awake()
    {
        if (navMeshAgent == null)
            navMeshAgent = GetComponent<NavMeshAgent>();

        if (navMeshDriver == null)
            navMeshDriver = GetComponent<AINavMeshDriver>();
    }

    private void Start()
    {
        if (roleController == null ||
            player == null ||
            navMeshDriver == null ||
            navMeshAgent == null)
        {
            Debug.LogError(
                "UtilityAIController is missing references.",
                this
            );

            enabled = false;
            return;
        }

        decisionTimer = 0f;
    }

    private void Update()
    {
        if (!navMeshAgent.isOnNavMesh)
        {
            navMeshDriver.ClearDestination();
            return;
        }

        decisionTimer -= Time.deltaTime;

        bool roleChanged =
            roleController.CurrentRole != lastRole;

        if (!roleChanged && decisionTimer > 0f)
            return;

        lastRole = roleController.CurrentRole;
        decisionTimer = decisionInterval;

        switch (roleController.CurrentRole)
        {
            case CharacterRole.Chaser:
                ChooseChaseDestination();
                break;

            case CharacterRole.Evader:
                ChooseEvadeDestination();
                break;

            default:
                navMeshDriver.ClearDestination();
                break;
        }
    }

    private void ChooseChaseDestination()
    {
        if (!NavMesh.SamplePosition(
                player.position,
                out NavMeshHit hit,
                destinationSampleRadius,
                navMeshAgent.areaMask))
        {
            navMeshDriver.ClearDestination();
            return;
        }

        navMeshDriver.SetDestination(hit.position);
    }

    private void ChooseEvadeDestination()
    {
        Vector3 awayFromPlayer =
            transform.position - player.position;

        awayFromPlayer.y = 0f;

        if (awayFromPlayer.sqrMagnitude < 0.001f)
            awayFromPlayer = transform.forward;

        awayFromPlayer.Normalize();

        Vector3 bestPosition = Vector3.zero;
        float bestScore = float.NegativeInfinity;
        bool foundCandidate = false;

        int count = Mathf.Max(4, evadeCandidateCount);

        for (int i = 0; i < count; i++)
        {
            float angle = i * 360f / count;

            Vector3 direction =
                Quaternion.AngleAxis(angle, Vector3.up)
                * awayFromPlayer;

            Vector3 rawPosition =
                transform.position + direction * evadeSearchRadius;

            if (!NavMesh.SamplePosition(
                    rawPosition,
                    out NavMeshHit hit,
                    destinationSampleRadius,
                    navMeshAgent.areaMask))
            {
                continue;
            }

            NavMeshPath path = new NavMeshPath();

            bool pathFound = navMeshAgent.CalculatePath(
                hit.position,
                path
            );

            if (!pathFound ||
                path.status != NavMeshPathStatus.PathComplete)
            {
                continue;
            }

            float distanceFromPlayer =
                Vector3.Distance(hit.position, player.position);

            float distanceFromAI =
                Vector3.Distance(transform.position, hit.position);

            // Prefer positions farther from the Player,
            // while avoiding destinations that barely move the AI.
            float score =
                distanceFromPlayer +
                Mathf.Min(distanceFromAI, evadeSearchRadius) * 0.1f;

            if (score > bestScore)
            {
                bestScore = score;
                bestPosition = hit.position;
                foundCandidate = true;
            }
        }

        if (foundCandidate)
            navMeshDriver.SetDestination(bestPosition);
        else
            navMeshDriver.ClearDestination();
    }
}
