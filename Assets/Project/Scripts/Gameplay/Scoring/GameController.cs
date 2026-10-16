using UnityEngine.SceneManagement;
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

    // Pengaman agar hasil ronde tidak diproses dua kali.
    private bool resultProcessedForCurrentRound;

    public int CurrentRound => currentRound;
    public int NextRoundNumber => currentRound + 1;

    public int PlayerScore => scoreController.PlayerScore;
    public int AIScore => scoreController.AIScore;

    public GameStatus Status => gameStatus;

    private void Start()
    {
        AssignInitialRoles();
        StartGame();
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

        // Role baru dirotasi setelah ronde pertama.
        if (currentRound > 0)
            RotateRoles();

        currentRound++;
        resultProcessedForCurrentRound = false;

        Debug.Log($"ROUND {currentRound} STARTING");

        roundController.StartRound();
    }

    /// <summary>
    /// Memproses skor dan mengecek apakah match sudah berakhir.
    /// Tidak memulai ronde berikutnya.
    /// </summary>
    public bool ProcessRoundResult(RoundWinner winner)
    {
        if (gameStatus != GameStatus.Playing)
            return gameStatus == GameStatus.Finished;

        if (resultProcessedForCurrentRound)
            return gameStatus == GameStatus.Finished;

        resultProcessedForCurrentRound = true;

        AwardRoundScore(winner);

        Debug.Log(
            $"ROUND {currentRound} RESULT | " +
            $"Player: {PlayerScore} - AI: {AIScore}"
        );

        if (CheckGameEnd())
        {
            FinishGame();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Menentukan siapa yang mendapatkan poin,
    /// berdasarkan role sebelum rotasi.
    /// </summary>
    public string GetRoundWinnerMessage(RoundWinner winner)
    {
        if (IsRoleWinner(winner, playerRoleController.CurrentRole))
            return "PLAYER SCORES!";

        if (IsRoleWinner(winner, aiRoleController.CurrentRole))
            return "ENEMY SCORES!";

        return "ROUND ENDED";
    }

    /// <summary>
    /// Dipanggil setelah animasi START! selesai.
    /// </summary>
    public void OnRoundFinished()
    {
        if (gameStatus != GameStatus.Playing)
            return;

        if (!resultProcessedForCurrentRound)
        {
            Debug.LogWarning(
                "Cannot start next round: " +
                "the current round result has not been processed."
            );

            return;
        }

        StartNextRound();
    }

    private bool IsRoleWinner(
        RoundWinner winner,
        CharacterRole role)
    {
        return
            (winner == RoundWinner.Chaser &&
             role == CharacterRole.Chaser)
            ||
            (winner == RoundWinner.Evader &&
             role == CharacterRole.Evader);
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
        return
            PlayerScore >= roundsToWin ||
            AIScore >= roundsToWin ||
            currentRound >= maxRounds;
    }

    private void FinishGame()
    {
        gameStatus = GameStatus.Finished;

        MatchResultData.SaveResult(PlayerScore, AIScore);

        Debug.Log(
            $"MATCH FINISHED! " +
            $"Player: {PlayerScore} - AI: {AIScore}"
        );

        // TODO: Hubungkan ke Endscreen nanti.
    }

    private void AssignInitialRoles()
    {
        CharacterRole playerRole =
            Random.value < 0.5f
                ? CharacterRole.Chaser
                : CharacterRole.Evader;

        CharacterRole aiRole =
            playerRole == CharacterRole.Chaser
                ? CharacterRole.Evader
                : CharacterRole.Chaser;

        playerRoleController.SetRole(playerRole);
        aiRoleController.SetRole(aiRole);
    }

    private void RotateRoles()
    {
        CharacterRole previousPlayerRole =
            playerRoleController.CurrentRole;

        CharacterRole previousAIRole =
            aiRoleController.CurrentRole;

        playerRoleController.SetRole(previousAIRole);
        aiRoleController.SetRole(previousPlayerRole);
    }
}
