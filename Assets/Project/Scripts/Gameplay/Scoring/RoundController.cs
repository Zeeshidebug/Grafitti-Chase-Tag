using UnityEngine;

public class RoundController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameController gameController;
    [SerializeField] private BuffNerfSpawner buffNerfSpawner;
    [SerializeField] private RoundOutcomeVFX roundOutcomeVFX;
    [SerializeField] private RoundTransitionController roundTransitionController;

    [Header("Round")]
    [SerializeField] private float roundDuration = 120f;

    private float remainingTime;
    private RoundState currentState;

    public float RemainingTime => remainingTime;
    public RoundState CurrentState => currentState;

    private RoundWinner roundWinner = RoundWinner.None;

    public RoundWinner RoundWinner => roundWinner;

    private void Update()
    {
        if (currentState != RoundState.Playing)
            return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            EndRound(RoundWinner.Evader);
        }
    }

    public void StartRound()
    {
        remainingTime = roundDuration;
        roundWinner = RoundWinner.None;
        currentState = RoundState.Playing;

        buffNerfSpawner.StartSpawning();
    }


    public void EndRound(RoundWinner winner)
    {
        if (currentState == RoundState.Finished)
            return;

        if (winner == RoundWinner.None)
            return;

        roundWinner = winner;
        currentState = RoundState.Finished;

        buffNerfSpawner.StopSpawning();

        roundOutcomeVFX.ShowOutcome(
            roundWinner,
            () => roundTransitionController.PlayRoundTransition(roundWinner)
        );
    }

}