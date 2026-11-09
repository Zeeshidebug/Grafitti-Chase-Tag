using UnityEngine.SceneManagement;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private RoundController roundController;
    [SerializeField] private ScoreController scoreController;
    [SerializeField] private RoleController playerRoleController;
    [SerializeField] private RoleController aiRoleController;

    [Header("Round Spawn Points (feet)")]
    [SerializeField] private Transform spawnPointA1;
    [SerializeField] private Transform spawnPointA2;
    [SerializeField] private Transform spawnPointB1;
    [SerializeField] private Transform spawnPointB2;

    [Header("Game Settings")]
    [SerializeField] private int maxRounds = 5;
    [SerializeField] private int roundsToWin = 3;

    [Header("Debug")]
    [SerializeField] private bool manualRoleAssignment = false;
    [SerializeField] private CharacterRole manualPlayerRole = CharacterRole.Chaser;
    [SerializeField] private CharacterRole manualAIRole = CharacterRole.Evader;

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

        PositionCharactersForRound();
        roundController.StartRound();
    }

    private void PositionCharactersForRound()
    {
        if (!spawnPointA1 && !spawnPointA2 && !spawnPointB1 && !spawnPointB2)
            return;

        if (!spawnPointA1 || !spawnPointA2 || !spawnPointB1 || !spawnPointB2)
        {
            Debug.LogError("Assign all four round spawn points.", this);
            return;
        }

        Transform pointA = Random.value < 0.5f ? spawnPointA1 : spawnPointA2;
        Transform pointB = Random.value < 0.5f ? spawnPointB1 : spawnPointB2;
        bool playerStartsAtA = Random.value < 0.5f;

        playerRoleController.GetComponent<CharacterMovement>().SpawnAt(playerStartsAtA ? pointA : pointB);
        aiRoleController.GetComponent<CharacterMovement>().SpawnAt(playerStartsAtA ? pointB : pointA);
        Physics.SyncTransforms();
    }

    /// Memproses skor dan mengecek apakah match sudah berakhir.
    /// Tidak memulai ronde berikutnya.
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

    /// Menentukan siapa yang mendapatkan poin,
    /// berdasarkan role sebelum rotasi.
    public string GetRoundWinnerMessage(RoundWinner winner)
    {
        if (IsRoleWinner(winner, playerRoleController.CurrentRole))
            return "PLAYER SCORES!";

        if (IsRoleWinner(winner, aiRoleController.CurrentRole))
            return "ENEMY SCORES!";

        return "ROUND ENDED";
    }

    /// Dipanggil setelah animasi START! selesai.
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
    }

    private void AssignInitialRoles()
    {
        if (manualRoleAssignment)
        {
            playerRoleController.SetRole(manualPlayerRole);
            aiRoleController.SetRole(manualAIRole);

            return;
        }

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
