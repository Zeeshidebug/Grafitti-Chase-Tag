using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine;

public class EndScreenPanelUI : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text playerScoreText;
    [SerializeField] private TMP_Text enemyScoreText;

    private void Start()
    {
        ShowEndscreen();
    }

    public void ShowEndscreen()
    {
        int playerScore = MatchResultData.PlayerScore;
        int enemyScore = MatchResultData.AIScore;

        playerScoreText.text = playerScore.ToString();
        enemyScoreText.text = enemyScore.ToString();

        if (playerScore > enemyScore)
        {
            resultText.text = "PLAYER WINS!";
        }
        else if (enemyScore > playerScore)
        {
            resultText.text = "ENEMY WINS!";
        }
        else
        {
            resultText.text = "DRAW!";
        }

        gameObject.SetActive(true);
    }

    public void PlayAgain()
    {
        MatchResultData.Reset();
        SceneManager.LoadScene("Gameplay");
    }

    public void ReturnToMainMenu()
    {
        MatchResultData.Reset();
        SceneManager.LoadScene("MainMenu");
    }
}
