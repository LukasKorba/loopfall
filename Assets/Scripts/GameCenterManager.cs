using UnityEngine;
#if UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX
using UnityEngine.SocialPlatforms.GameCenter;
#endif

/// <summary>
/// GameCenter integration — authentication, score submission, leaderboard display.
/// Works on iOS and macOS. Created by SceneSetup.
///
/// Usage:
///   GameCenterManager.Instance.ReportScore(score);
///   GameCenterManager.Instance.ShowLeaderboard();
/// </summary>
public class GameCenterManager : MonoBehaviour
{
    public static GameCenterManager Instance { get; private set; }

    // Apple leaderboard IDs — set in App Store Connect
    private const string LB_PURE_HELL = "com.lukaskorba.loopfall.purehell";
    private const string LB_TAP_MASTER = "com.lukaskorba.loopfall.tapmaster";
    private const string LB_RUNS = "com.lukaskorba.loopfall.runs";

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
        Report(score, LB_PURE_HELL);
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

        Social.ReportScore(value, leaderboardID, (bool success) =>
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

    /// <summary>
    /// Show the native GameCenter leaderboard UI.
    /// </summary>
    public void ShowLeaderboard()
    {
#if (UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
        if (!mAuthenticated)
        {
            Debug.Log("[GameCenter] Not authenticated — cannot show leaderboard");
            return;
        }

        // Show default leaderboard set — user can browse all boards
        GameCenterPlatform.ShowLeaderboardUI(LB_PURE_HELL, UnityEngine.SocialPlatforms.TimeScope.AllTime);
#else
        Debug.Log("[GameCenter] (Editor) Would show leaderboard");
#endif
    }

    public bool IsAuthenticated() { return mAuthenticated; }
}
