
using TMPro;
using UnityEngine;

public class ScoreHUDController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private ScoreController scoreController;
    [SerializeField] private RoleController playerRoleController;
    [SerializeField] private RoleController aiRoleController;
    [SerializeField] private RoundController roundController;

    [Header("Score Text")]
    [SerializeField] private TMP_Text playerScoreText;
    [SerializeField] private TMP_Text enemyScoreText;

    [Header("Role Text")]
    [SerializeField] private TMP_Text playerRoleText;
    [SerializeField] private TMP_Text enemyRoleText;

    [Header("Timer Text")]
    [SerializeField] private TMP_Text timerText;

    [Header("Role Colors")]
    [SerializeField] private Color chaserColor = Color.red;
    [SerializeField] private Color evaderColor = Color.cyan;

    private void Update()
    {
        UpdateScores();
        UpdateRoles();
        UpdateTimer();
    }

    private void UpdateScores()
    {
        playerScoreText.text = scoreController.PlayerScore.ToString();
        enemyScoreText.text = scoreController.AIScore.ToString();
    }

    private void UpdateRoles()
    {
        UpdateRoleDisplay(playerRoleController, playerRoleText);
        UpdateRoleDisplay(aiRoleController, enemyRoleText);
    }

    private void UpdateRoleDisplay(
        RoleController roleController,
        TMP_Text roleText)
    {
        if (roleController.IsChaser)
        {
            roleText.color = chaserColor;
        }
        else if (roleController.IsEvader)
        {
            roleText.color = evaderColor;
        }
    }

    private void UpdateTimer()
    {
        int remainingSeconds =
            Mathf.CeilToInt(roundController.RemainingTime);

        int minutes = remainingSeconds / 60;
        int seconds = remainingSeconds % 60;

        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
