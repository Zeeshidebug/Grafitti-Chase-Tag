
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AINavMeshDriver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AIMovementIntent movementIntent;

    [Header("Navigation")]
    [SerializeField] private float stoppingDistance = 1.5f;
    [SerializeField] private float repathInterval = 0.2f;

    [Header("Movement")]
    [SerializeField] private bool sprint;

    private NavMeshAgent agent;
    private float repathTimer;
    private Vector3 lastDestination;
    private bool hasDestination;
    private Vector3 destination;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // NavMesh computes paths; CharacterMovement moves the character.
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.stoppingDistance = stoppingDistance;
    }

    private void Start()
    {
        if (movementIntent == null)
        {
            Debug.LogError(
                "AINavMeshDriver requires Movement Intent",
                this
            );

            enabled = false;
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError(
                "AI NavMeshAgent is not positioned on a baked NavMesh.",
                this
            );

            enabled = false;
        }
    }

    private void Update()
    {
        if (!agent.isOnNavMesh)
        {
            movementIntent.StopMovement();
            return;
        }

        // Keep the navigation simulation synchronized with the
        // position moved by CharacterMovement.
        agent.nextPosition = transform.position;

        if (!hasDestination)
        {
            movementIntent.StopMovement();
            return;
        }

        repathTimer -= Time.deltaTime;

        if (repathTimer <= 0f)
        {
            if (agent.SetDestination(destination))
                repathTimer = repathInterval;
        }

        if (agent.pathPending ||
            !agent.hasPath ||
            agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            movementIntent.StopMovement();
            return;
        }

        if (agent.remainingDistance <= stoppingDistance)
        {
            movementIntent.StopMovement();
            return;
        }

        Vector3 direction = agent.steeringTarget - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            movementIntent.StopMovement();
            return;
        }

        direction.Normalize();

        movementIntent.SetMovementIntent(
            direction,
            direction,
            sprint
        );
    }

    public void SetDestination(Vector3 newDestination)
    {
        destination = newDestination;
        hasDestination = true;
    }

    public void ClearDestination()
    {
        hasDestination = false;

        if (agent != null && agent.isOnNavMesh)
            agent.ResetPath();

        if (movementIntent != null)
            movementIntent.StopMovement();
    }

    private void OnDisable()
    {
        if (movementIntent != null)
            movementIntent.StopMovement();
    }
}
