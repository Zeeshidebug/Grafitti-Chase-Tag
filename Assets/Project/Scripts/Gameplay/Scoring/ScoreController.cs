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
    }

    public void AddAIScore()
    {
        aiScore++;
    }
}