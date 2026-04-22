using UnityEngine;
#if UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX
using UnityEngine.SocialPlatforms.GameCenter;
#endif

/// <summary>
/// GameCenter integration — authentication, score submission, leaderboard display.
/// Works on iOS, tvOS, and macOS. Implements IPlatformService for cross-platform routing.
///
/// Usage (via PlatformManager):
///   PlatformManager.Instance.ReportScore(score);
///   PlatformManager.Instance.ShowLeaderboard();
/// </summary>
public class GameCenterManager : MonoBehaviour, IPlatformService
{
    public static GameCenterManager Instance { get; private set; }

    // Apple leaderboard IDs — set in App Store Connect.
    // Per-mode score leaderboards are resolved via GameConfig.GetLeaderboardID() so
    // Pure Hell and Blitz submissions can't cross-contaminate.
    private const string LB_TAP_MASTER = "com.lukaskorba.loopfall.tapmaster";
    private const string LB_RUNS = "com.lukaskorba.loopfall.runs";

    // Game Center group prefix. Set on Apple after group registration — every
    // leaderboard/achievement ID submitted to GKLeaderboard/GKAchievement APIs
    // must carry it. CSVs + Steam/Android stay bare; fastlane prepends at push
    // time for App Store Connect metadata.
    private const string APPLE_GROUP_PREFIX = "grp.";
    static string AppleID(string bareID) => APPLE_GROUP_PREFIX + bareID;

    private bool mAuthenticated = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        Authenticate();
    }

    void Authenticate()
    {
#if (UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
        Social.localUser.Authenticate((bool success) =>
        {
            mAuthenticated = success;
            if (success)
                Debug.Log("[GameCenter] Authenticated: " + Social.localUser.userName);
            else
                Debug.Log("[GameCenter] Authentication failed");
        });
#else
        Debug.Log("[GameCenter] Skipped — not on Apple device");
#endif
    }

    public void ReportScore(int score)
    {
        if (score <= 0) return;
        Report(score, GameConfig.GetLeaderboardID());
    }

    public void ReportTaps(int totalTaps)
    {
        if (totalTaps <= 0) return;
        Report(totalTaps, LB_TAP_MASTER);
    }

    public void ReportRuns(int totalRuns)
    {
        if (totalRuns <= 0) return;
        Report(totalRuns, LB_RUNS);
    }

    void Report(int value, string leaderboardID)
    {
#if (UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
        if (!mAuthenticated) return;

        Social.ReportScore(value, AppleID(leaderboardID), (bool success) =>
        {
            if (success)
                Debug.Log($"[GameCenter] {leaderboardID}: {value} reported");
            else
                Debug.Log($"[GameCenter] {leaderboardID}: failed to report {value}");
        });
#else
        Debug.Log($"[GameCenter] (Editor) {leaderboardID}: {value}");
#endif
    }

    public void ShowLeaderboard()
    {
#if (UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
        if (!mAuthenticated)
        {
            Debug.Log("[GameCenter] Not authenticated — cannot show leaderboard");
            return;
        }

        GameCenterPlatform.ShowLeaderboardUI(AppleID(GameConfig.GetLeaderboardID()), UnityEngine.SocialPlatforms.TimeScope.AllTime);
#else
        Debug.Log("[GameCenter] (Editor) Would show leaderboard");
#endif
    }

    public void UnlockAchievement(string achievementID)
    {
#if (UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
        if (!mAuthenticated) return;
        Social.ReportProgress(AppleID(achievementID), 100.0, (bool success) =>
        {
            Debug.Log($"[GameCenter] Achievement {achievementID}: {(success ? "unlocked" : "failed")}");
        });
#else
        Debug.Log($"[GameCenter] (Editor) Achievement: {achievementID}");
#endif
    }

    public void ShowAchievements()
    {
#if (UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
        if (!mAuthenticated) return;
        Social.ShowAchievementsUI();
#else
        Debug.Log("[GameCenter] (Editor) Would show achievements");
#endif
    }

    public void SaveToCloud(string key, string data)
    {
        // Routed through ICloudKVStore → NSUbiquitousKeyValueStore.
        // CloudSync uses this store directly; this IPlatformService method exists for parity with Steam.
        if (ICloudKVStore.Instance == null) return;
        ICloudKVStore.Instance.SetString(key, data);
        ICloudKVStore.Instance.Synchronize();
    }

    public string LoadFromCloud(string key)
    {
        if (ICloudKVStore.Instance == null) return null;
        return ICloudKVStore.Instance.GetString(key);
    }

    public bool IsAuthenticated() { return mAuthenticated; }
}
