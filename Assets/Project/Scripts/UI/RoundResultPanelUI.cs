
using TMPro;
using UnityEngine;

public class RoundResultPanelUI : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TMP_Text roundTitleText;
    [SerializeField] private TMP_Text playerScoreText;
    [SerializeField] private TMP_Text enemyScoreText;
    [SerializeField] private TMP_Text roundWinnerText;

    public void ShowRoundResult(
        int roundNumber,
        int playerScore,
        int enemyScore,
        string winnerMessage)
    {
        roundTitleText.text =
            $"Round {roundNumber} Concluded!";

        playerScoreText.text = playerScore.ToString();
        enemyScoreText.text = enemyScore.ToString();
        roundWinnerText.text = winnerMessage;

        gameObject.SetActive(true);
    }

    public void HidePanel()
    {
        gameObject.SetActive(false);
    }
}
