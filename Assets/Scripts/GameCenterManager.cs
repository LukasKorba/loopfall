using UnityEngine;
#if UNITY_IOS
using UnityEngine.SocialPlatforms.GameCenter;
#endif

/// <summary>
/// GameCenter integration — authentication, score submission, leaderboard display.
/// Attach to a persistent GameObject via SceneSetup.
///
/// Usage:
///   GameCenterManager.Instance.ReportScore(score);
///   GameCenterManager.Instance.ShowLeaderboard();
/// </summary>
public class GameCenterManager : MonoBehaviour
{
    public static GameCenterManager Instance { get; private set; }

    // Apple leaderboard ID — set this in App Store Connect
    private const string LEADERBOARD_ID = "com.lukaskorba.loopfall.highscore";

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
#if UNITY_IOS && !UNITY_EDITOR
        Social.localUser.Authenticate((bool success) =>
        {
            mAuthenticated = success;
            if (success)
                Debug.Log("[GameCenter] Authenticated: " + Social.localUser.userName);
            else
                Debug.Log("[GameCenter] Authentication failed");
        });
#else
        Debug.Log("[GameCenter] Skipped — not on iOS device");
#endif
    }

    /// <summary>
    /// Submit a score to the GameCenter leaderboard.
    /// Call this after each game over.
    /// </summary>
    public void ReportScore(int score)
    {
        if (score <= 0) return;

#if UNITY_IOS && !UNITY_EDITOR
        if (!mAuthenticated)
        {
            Debug.Log("[GameCenter] Not authenticated — score not reported");
            return;
        }

        Social.ReportScore(score, LEADERBOARD_ID, (bool success) =>
        {
            if (success)
                Debug.Log($"[GameCenter] Score {score} reported");
            else
                Debug.Log($"[GameCenter] Failed to report score {score}");
        });
#else
        Debug.Log($"[GameCenter] (Editor) Would report score: {score}");
#endif
    }

    /// <summary>
    /// Show the native GameCenter leaderboard UI.
    /// </summary>
    public void ShowLeaderboard()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (!mAuthenticated)
        {
            Debug.Log("[GameCenter] Not authenticated — cannot show leaderboard");
            return;
        }

        GameCenterPlatform.ShowLeaderboardUI(LEADERBOARD_ID, UnityEngine.SocialPlatforms.TimeScope.AllTime);
#else
        Debug.Log("[GameCenter] (Editor) Would show leaderboard");
#endif
    }

    public bool IsAuthenticated() { return mAuthenticated; }
}
