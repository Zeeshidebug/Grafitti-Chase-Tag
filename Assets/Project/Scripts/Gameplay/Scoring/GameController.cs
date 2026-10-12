using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private RoundController roundController;
    [SerializeField] private ScoreController scoreController;
    [SerializeField] private RoleController playerRoleController;
    [SerializeField] private RoleController aiRoleController;

    [Header("Game Settings")]
    [SerializeField] private int maxRounds = 5;
    [SerializeField] private int roundsToWin = 3;

    private int currentRound;
    private GameStatus gameStatus = GameStatus.NotStarted;

    public int CurrentRound => currentRound;
    public GameStatus Status => gameStatus;

    private void Start()
    {
        StartGame();
        AssignInitialRoles();
    }

    public void StartGame()
    {
        if (gameStatus != GameStatus.NotStarted)
            return;

        currentRound = 0;
        gameStatus = GameStatus.Playing;

        StartNextRound();
    }

    private void StartNextRound()
    {
        if (gameStatus != GameStatus.Playing)
            return;

        currentRound++;

        RotateRoles();
        roundController.StartRound();
    }

    public void OnRoundFinished(RoundWinner winner)
    {
        if (gameStatus != GameStatus.Playing)
            return;

        AwardRoundScore(winner);

        if (CheckGameEnd())
        {
            FinishGame();
            return;
        }

        StartNextRound();
    }

    private bool IsRoleWinner(
    RoundWinner winner,
    CharacterRole role
)
    {
        if (winner == RoundWinner.Chaser &&
            role == CharacterRole.Chaser)
            return true;

        if (winner == RoundWinner.Evader &&
            role == CharacterRole.Evader)
            return true;

        return false;
    }

    private void AwardRoundScore(RoundWinner winner)
    {
        if (winner == RoundWinner.None)
            return;

        if (IsRoleWinner(winner, playerRoleController.CurrentRole))
        {
            scoreController.AddPlayerScore();
            return;
        }

        if (IsRoleWinner(winner, aiRoleController.CurrentRole))
        {
            scoreController.AddAIScore();
        }
    }

    private bool CheckGameEnd()
    {
        if (scoreController.PlayerScore >= roundsToWin)
            return true;

        if (scoreController.AIScore >= roundsToWin)
            return true;

        if (currentRound >= maxRounds)
            return true;

        return false;
    }

    private void FinishGame()
    {
        gameStatus = GameStatus.Finished;

        Debug.Log($"GAME FINISHED!");
    }

    private void AssignInitialRoles()
    {
        bool playerIsChaser = Random.value > 0.5f;

        if (playerIsChaser)
        {
            playerRoleController.SetRole(CharacterRole.Chaser);
            aiRoleController.SetRole(CharacterRole.Evader);
        }
        else
        {
            playerRoleController.SetRole(CharacterRole.Evader);
            aiRoleController.SetRole(CharacterRole.Chaser);
        }

    }

    private void RotateRoles()
    {
        CharacterRole currentPlayerRole =
            playerRoleController.CurrentRole;

        playerRoleController.SetRole(
            aiRoleController.CurrentRole
        );

        aiRoleController.SetRole(
            currentPlayerRole
        );
    }
}