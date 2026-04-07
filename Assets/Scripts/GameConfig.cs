/// <summary>
/// Lightweight mode state — no MonoBehaviour needed, just static config.
/// Set ActiveMode from title screen before starting a run.
/// </summary>
public enum GameModeType
{
    PureHell,
    TimeWarp
}

public static class GameConfig
{
    public static GameModeType ActiveMode = GameModeType.PureHell;

    public static string GetScoresKey()
    {
        return ActiveMode == GameModeType.TimeWarp ? "TopScores_TimeWarp" : "TopScores";
    }

    public static string GetLeaderboardID()
    {
        return ActiveMode == GameModeType.TimeWarp
            ? "com.lukaskorba.loopfall.timewarp"
            : "com.lukaskorba.loopfall.purehell";
    }

    public static string GetModeName()
    {
        return ActiveMode == GameModeType.TimeWarp ? "TIME WARP" : "PURE HELL";
    }

    public static bool IsTimeWarp()
    {
        return ActiveMode == GameModeType.TimeWarp;
    }
}
