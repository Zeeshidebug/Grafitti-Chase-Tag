using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine;
using System.Collections;

public class EndScreenPanelUI : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text playerScoreText;
    [SerializeField] private TMP_Text enemyScoreText;
    [SerializeField] private EndscreenPresentation presentation;
    private bool leaving;

    private void Start()
    {
        if (presentation == null) ShowEndscreen();
    }

    public void ShowEndscreen()
    {
        if (presentation != null) return;
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
        Leave("Gameplay");
    }

    public void ReturnToMainMenu()
    {
        Leave("MainMenu");
    }

    private void Leave(string scene)
    {
        if (leaving || (presentation != null && !presentation.Ready)) return;
        leaving = true;
        StartCoroutine(Load(scene));
    }

    private IEnumerator Load(string scene)
    {
        if (presentation != null) yield return presentation.CoverForExit();
        MatchResultData.Reset();
        Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync(scene);
    }
}
