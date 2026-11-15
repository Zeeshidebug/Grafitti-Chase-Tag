
using UnityEngine;

public class TagReceiver : MonoBehaviour
{
    [Header("Round Controller")]
    [SerializeField] private RoundController roundController;

    [Header("Tag VFX")]
    [SerializeField] private ParticleSystem tagSmokeMain;

    public bool RoundIsPlaying =>
        roundController != null &&
        roundController.CurrentState == RoundState.Playing;

    public void ReceiveTag()
    {
        if (!RoundIsPlaying)
            return;

        // Play tag smoke effectg
        if (tagSmokeMain != null)
        {
            tagSmokeMain.Play(true);
        }

        // End the round
        roundController.EndRound(RoundWinner.Chaser);
    }
}
