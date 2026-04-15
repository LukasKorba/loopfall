/// <summary>
/// Abstraction for platform-specific services (leaderboards, achievements, cloud saves).
/// Implemented by GameCenterService (Apple) and SteamService (Steam/PC).
/// </summary>
public interface IPlatformService
{
    bool IsAuthenticated();

    // Leaderboards
    void ReportScore(int score);
    void ReportTaps(int totalTaps);
    void ReportRuns(int totalRuns);
    void ShowLeaderboard();

    // Achievements
    void UnlockAchievement(string achievementID);
    void ShowAchievements();

    // Cloud saves
    void SaveToCloud(string key, string data);
    string LoadFromCloud(string key);
}
