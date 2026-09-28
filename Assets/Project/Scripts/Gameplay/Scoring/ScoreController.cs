using UnityEngine;

public class ScoreController : MonoBehaviour
{
    private int playerScore;
    private int aiScore;

    public int PlayerScore => playerScore;
    public int AIScore => aiScore;

    public void AddPlayerScore()
    {
        playerScore++;
        Debug.Log($"Player Scored! current score : {playerScore}");
    }

    public void AddAIScore()
    {
        aiScore++;
        Debug.Log($"AI Scored! current score : {aiScore}");
    }
}