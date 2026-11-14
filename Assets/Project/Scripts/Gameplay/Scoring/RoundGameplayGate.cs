using UnityEngine;
using UnityEngine.AI;

// Keep the simulation still during entry without pausing the presentation clock
// or disabling the input actions used by Escape / Start to pause the sequence.
[DefaultExecutionOrder(-220)]
public sealed class RoundGameplayGate : MonoBehaviour
{
    [SerializeField] private RoundController roundController;
    [SerializeField] private Behaviour[] gameplayBehaviours;
    [SerializeField] private NavMeshAgent[] agents;
    private bool[] enabledBeforeLock, stoppedBeforeLock;
    private bool locked;

    private void Start() => SetLocked(roundController.CurrentState != RoundState.Playing);
    private void Update()
    {
        SetLocked(roundController.CurrentState != RoundState.Playing);
        // A spawn reset can re-enable navigation on an already-locked agent.
        if (locked)
            foreach (var agent in agents)
                if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
    }

    private void SetLocked(bool value)
    {
        if (locked == value) return;
        locked = value;
        if (value)
        {
            enabledBeforeLock = new bool[gameplayBehaviours.Length];
            for (int i = 0; i < gameplayBehaviours.Length; i++)
                if (gameplayBehaviours[i] != null)
                {
                    enabledBeforeLock[i] = gameplayBehaviours[i].enabled;
                    gameplayBehaviours[i].enabled = false;
                }
            stoppedBeforeLock = new bool[agents.Length];
            for (int i = 0; i < agents.Length; i++)
                if (agents[i] != null && agents[i].isOnNavMesh)
                {
                    stoppedBeforeLock[i] = agents[i].isStopped;
                    agents[i].isStopped = true;
                }
        }
        else
        {
            for (int i = 0; i < gameplayBehaviours.Length; i++)
                if (gameplayBehaviours[i] != null) gameplayBehaviours[i].enabled = enabledBeforeLock[i];
            for (int i = 0; i < agents.Length; i++)
                if (agents[i] != null && agents[i].isOnNavMesh) agents[i].isStopped = stoppedBeforeLock[i];
        }
    }

    private void OnDisable() { if (locked) SetLocked(false); }
}
