/// <summary>
/// Lightweight mode state — no MonoBehaviour needed, just static config.
/// Set ActiveMode from title screen before starting a run.
/// </summary>
public enum GameModeType
{
    PureHell,
    Blitz
}

public static class GameConfig
{
    public static GameModeType ActiveMode = GameModeType.PureHell;

    public static string GetScoresKey() { return GetScoresKey(ActiveMode); }
    public static string GetScoresKey(GameModeType mode)
    {
        switch (mode)
        {
            case GameModeType.Blitz: return "TopScores_Blitz";
            default:                 return "TopScores";
        }
    }

    public static string GetLeaderboardID()
    {
        switch (ActiveMode)
        {
            case GameModeType.Blitz: return "com.lukaskorba.loopfall.blitz";
            default:                 return "com.lukaskorba.loopfall.purehell";
        }
    }

    public static string GetModeName() { return GetModeName(ActiveMode); }
    public static string GetModeName(GameModeType mode)
    {
        switch (mode)
        {
            case GameModeType.Blitz: return "PATH TO REDEMPTION";
            default:                 return "GATES TO HELL";
        }
    }

    public static bool IsBlitz()
    {
        return ActiveMode == GameModeType.Blitz;
    }
}
