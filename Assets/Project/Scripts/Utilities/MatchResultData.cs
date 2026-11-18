
public static class MatchResultData
{
    public static int PlayerScore { get; private set; }
    public static int AIScore { get; private set; }
    public static bool HasResult { get; private set; }
    public static bool HasPresentation { get; private set; }
    public static int Round { get; private set; }
    public static int PreviousPlayerScore { get; private set; }
    public static int PreviousAIScore { get; private set; }

    public static void SaveResult(int playerScore, int aiScore)
    {
        PlayerScore = playerScore;
        AIScore = aiScore;
        HasResult = true;
        HasPresentation = false;
        Round = 0;
        PreviousPlayerScore = playerScore;
        PreviousAIScore = aiScore;
    }

    // The score is already committed by GameController. Endscreen only presents it.
    public static void SavePresentation(int round, int previousPlayer, int previousAI)
    {
        if (!HasResult) return;
        Round = round;
        PreviousPlayerScore = previousPlayer;
        PreviousAIScore = previousAI;
        HasPresentation = true;
    }

    public static void Reset()
    {
        PlayerScore = 0;
        AIScore = 0;
        HasResult = HasPresentation = false;
        Round = PreviousPlayerScore = PreviousAIScore = 0;
    }
}
