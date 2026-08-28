using UnityEngine;

public class CharacterState : MonoBehaviour
{
    public LocomotionState CurrentState { get; private set; }

    private void Awake()
    {
        CurrentState = LocomotionState.Grounded;
    }

    public void SetState(LocomotionState newState)
    {
        if (CurrentState == newState)
        {
            return;
        }

        CurrentState = newState;
    }
}