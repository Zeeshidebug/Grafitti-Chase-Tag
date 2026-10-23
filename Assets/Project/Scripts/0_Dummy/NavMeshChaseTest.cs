
using UnityEngine;
using UnityEngine.AI;

public class NavMeshChaseTest : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform target;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (agent == null || target == null)
            return;

        if (!agent.isOnNavMesh)
            return;

        agent.SetDestination(target.position);
    }
}
