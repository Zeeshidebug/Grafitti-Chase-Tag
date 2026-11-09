
using UnityEngine;

public class TagReceiver : MonoBehaviour
{
    [Header("Round Controller")]
    [SerializeField] private RoundController roundController;

    [Header("Tag VFX")]
    [SerializeField] private ParticleSystem tagSmokeMain;

    public void ReceiveTag()
    {
        // Play tag smoke effect
        if (tagSmokeMain != null)
        {
            tagSmokeMain.Play(true);
        }

        // End the round
        roundController.EndRound(RoundWinner.Chaser);
    }
}
