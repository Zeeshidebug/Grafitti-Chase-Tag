using UnityEngine;

public class TagReceiver : MonoBehaviour
{
    [SerializeField] private RoundController roundController;

    public bool RoundIsPlaying => roundController != null && roundController.CurrentState == RoundState.Playing;

    public void ReceiveTag()
    {
        if (RoundIsPlaying)
            roundController.EndRound(RoundWinner.Chaser);
    }
}
