using UnityEngine;

/// <summary>
/// Singleton facade that routes to the active platform service (GameCenter or Steam).
/// All game code should use PlatformManager.Instance instead of calling GameCenterManager
/// or SteamService directly.
///
/// Usage:
///   PlatformManager.Instance.ReportScore(score);
///   PlatformManager.Instance.ShowLeaderboard();
///   PlatformManager.Instance.UnlockAchievement("ACH_FLOW_STATE");
/// </summary>
public class PlatformManager : MonoBehaviour
{
    public static PlatformManager Instance { get; private set; }

    private IPlatformService mService;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Called by SceneSetup after creating the appropriate platform service.
    /// </summary>
    public void SetService(IPlatformService service)
    {
        mService = service;
        Debug.Log($"[Platform] Service set: {service.GetType().Name}");
    }

    public bool IsAuthenticated()
    {
        return mService != null && mService.IsAuthenticated();
    }

    // ── LEADERBOARDS ─────────────────────────────────────────

    public void ReportScore(int score)
    {
        if (mService != null) mService.ReportScore(score);
    }

    public void ReportTaps(int totalTaps)
    {
        if (mService != null) mService.ReportTaps(totalTaps);
    }

    public void ReportRuns(int totalRuns)
    {
        if (mService != null) mService.ReportRuns(totalRuns);
    }

    public void ShowLeaderboard()
    {
        if (mService != null) mService.ShowLeaderboard();
    }

    // ── ACHIEVEMENTS ─────────────────────────────────────────

    public void UnlockAchievement(string achievementID)
    {
        if (mService != null) mService.UnlockAchievement(achievementID);
    }

    public void ShowAchievements()
    {
        if (mService != null) mService.ShowAchievements();
    }

    // ── CLOUD SAVES ──────────────────────────────────────────

    public void SaveToCloud(string key, string data)
    {
        if (mService != null) mService.SaveToCloud(key, data);
    }

    public string LoadFromCloud(string key)
    {
        return mService != null ? mService.LoadFromCloud(key) : null;
    }

    // ── ACHIEVEMENT IDs (shared constants) ───────────────────

    // Pure Hell milestones
    public const string ACH_FIRST_STEPS = "ACH_FIRST_STEPS";       // Complete a run
    public const string ACH_GETTING_HANG = "ACH_GETTING_HANG";     // 10 gates
    public const string ACH_FLOW_STATE = "ACH_FLOW_STATE";         // 25 gates
    public const string ACH_TUNNEL_VISION = "ACH_TUNNEL_VISION";   // 50 gates
    public const string ACH_EVENT_HORIZON = "ACH_EVENT_HORIZON";   // 100 gates

    // Time Warp milestones
    public const string ACH_TIME_LORD = "ACH_TIME_LORD";           // 30s survived
    public const string ACH_CHRONO_MASTER = "ACH_CHRONO_MASTER";   // 60s survived
    public const string ACH_TIME_PARADOX = "ACH_TIME_PARADOX";     // 120s survived

    // Career milestones
    public const string ACH_SWING_KING = "ACH_SWING_KING";         // 10 swing detections
    public const string ACH_TAP_MASTER = "ACH_TAP_MASTER";         // 1000 total taps
    public const string ACH_DEDICATED = "ACH_DEDICATED";           // 100 total runs
    public const string ACH_OBSESSED = "ACH_OBSESSED";             // 500 total runs
}
