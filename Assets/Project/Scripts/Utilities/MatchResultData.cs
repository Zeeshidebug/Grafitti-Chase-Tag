
public static class MatchResultData
{
    public static int PlayerScore { get; private set; }
    public static int AIScore { get; private set; }

    public static void SaveResult(int playerScore, int aiScore)
    {
        PlayerScore = playerScore;
        AIScore = aiScore;
    }

    public static void Reset()
    {
        PlayerScore = 0;
        AIScore = 0;
    }
}
