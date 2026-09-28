using UnityEngine;

public class TagReceiver : MonoBehaviour
{
    [SerializeField] private RoundController roundController;

    public void ReceiveTag()
    {
        roundController.EndRound(RoundWinner.Chaser);
    }
}
