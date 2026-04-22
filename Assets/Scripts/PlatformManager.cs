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

    // Swing count persists across runs — unlocks ACH_SWING_KING at 10.
    private int mSwingCount;
    private const string PREF_SWING_COUNT = "SwingCount";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        mSwingCount = PlayerPrefs.GetInt(PREF_SWING_COUNT, 0);
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

        // Gate-based Pure Hell milestones must NOT fire from Blitz points.
        if (GameConfig.IsBlitz())
            CheckBlitzAchievements(score);
        else
            CheckPureHellAchievements(score);
    }

    public void ReportTaps(int totalTaps)
    {
        if (mService != null) mService.ReportTaps(totalTaps);

        if (totalTaps >= 1000)       UnlockAchievement(ACH_TAP_INITIATE);
        if (totalTaps >= 10000)      UnlockAchievement(ACH_TAP_MASTER);
        if (totalTaps >= 50000)      UnlockAchievement(ACH_TAP_VIRTUOSO);
        if (totalTaps >= 250000)     UnlockAchievement(ACH_TAP_DEMON);
        if (totalTaps >= 500000)     UnlockAchievement(ACH_TAP_UNSTOPPABLE);
        if (totalTaps >= 1000000)    UnlockAchievement(ACH_TAP_MILLION);
    }

    public void ReportRuns(int totalRuns)
    {
        if (mService != null) mService.ReportRuns(totalRuns);

        if (totalRuns >= 10)   UnlockAchievement(ACH_FIRST_STEPS);
        if (totalRuns >= 100)  UnlockAchievement(ACH_DEDICATED);
        if (totalRuns >= 500)  UnlockAchievement(ACH_OBSESSED);
        if (totalRuns >= 1000) UnlockAchievement(ACH_POSSESSED);
    }

    // ── GAMEPLAY EVENT HOOKS ─────────────────────────────────

    /// <summary>
    /// Called by Torus on swing detection. Persists the lifetime counter and
    /// fires ACH_SWING_KING at 10.
    /// </summary>
    public void OnSwingDetected()
    {
        mSwingCount++;
        PlayerPrefs.SetInt(PREF_SWING_COUNT, mSwingCount);
        if (mSwingCount >= 10)
            UnlockAchievement(ACH_SWING_KING);
    }

    /// <summary>Called by Torus when the shield first deploys this run.</summary>
    public void OnShieldDeployed()
    {
        if (GameConfig.IsBlitz())
            UnlockAchievement(ACH_BLITZ_SHIELD_UP);
    }

    /// <summary>Called by Torus when cadence reaches level 2.</summary>
    public void OnCadenceFull()
    {
        if (GameConfig.IsBlitz())
            UnlockAchievement(ACH_BLITZ_CADENCE_FULL);
    }

    /// <summary>Called by Torus when cannon (gun) reaches level 2.</summary>
    public void OnCannonFull()
    {
        if (GameConfig.IsBlitz())
            UnlockAchievement(ACH_BLITZ_CANNON_FULL);
    }

    // ── ACHIEVEMENT DECISION HELPERS ─────────────────────────

    void CheckPureHellAchievements(int gates)
    {
        if (gates >= 10)  UnlockAchievement(ACH_GETTING_HANG);
        if (gates >= 25)  UnlockAchievement(ACH_FLOW_STATE);
        if (gates >= 50)  UnlockAchievement(ACH_TUNNEL_VISION);
        if (gates >= 100) UnlockAchievement(ACH_EVENT_HORIZON);
    }

    void CheckBlitzAchievements(int score)
    {
        if (score >= 100)   UnlockAchievement(ACH_BLITZ_LIVE_WIRE);
        if (score >= 1000)  UnlockAchievement(ACH_BLITZ_TRIGGER_HAPPY);
        if (score >= 2500)  UnlockAchievement(ACH_BLITZ_OVERLOAD);
        if (score >= 5000)  UnlockAchievement(ACH_BLITZ_SHORT_CIRCUIT);
        if (score >= 10000) UnlockAchievement(ACH_BLITZ_MELTDOWN);
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

    // Pure Hell milestones (gates in a single run)
    public const string ACH_GETTING_HANG = "ACH_GETTING_HANG";       // 10 gates
    public const string ACH_FLOW_STATE = "ACH_FLOW_STATE";           // 25 gates
    public const string ACH_TUNNEL_VISION = "ACH_TUNNEL_VISION";     // 50 gates
    public const string ACH_EVENT_HORIZON = "ACH_EVENT_HORIZON";     // 100 gates

    // Career — total runs
    public const string ACH_FIRST_STEPS = "ACH_FIRST_STEPS";         // 10 runs
    public const string ACH_DEDICATED = "ACH_DEDICATED";             // 100 runs
    public const string ACH_OBSESSED = "ACH_OBSESSED";               // 500 runs
    public const string ACH_POSSESSED = "ACH_POSSESSED";             // 1000 runs

    // Career — total taps
    public const string ACH_TAP_INITIATE = "ACH_TAP_INITIATE";       // 1k taps
    public const string ACH_TAP_MASTER = "ACH_TAP_MASTER";           // 10k taps
    public const string ACH_TAP_VIRTUOSO = "ACH_TAP_VIRTUOSO";       // 50k taps
    public const string ACH_TAP_DEMON = "ACH_TAP_DEMON";             // 250k taps
    public const string ACH_TAP_UNSTOPPABLE = "ACH_TAP_UNSTOPPABLE"; // 500k taps
    public const string ACH_TAP_MILLION = "ACH_TAP_MILLION";         // 1M taps

    // Career — swings
    public const string ACH_SWING_KING = "ACH_SWING_KING";           // 10 swing detections

    // Blitz — score in a single run
    public const string ACH_BLITZ_LIVE_WIRE = "ACH_BLITZ_LIVE_WIRE";             // score >= 100
    public const string ACH_BLITZ_TRIGGER_HAPPY = "ACH_BLITZ_TRIGGER_HAPPY";     // score >= 1000
    public const string ACH_BLITZ_OVERLOAD = "ACH_BLITZ_OVERLOAD";               // score >= 2500
    public const string ACH_BLITZ_SHORT_CIRCUIT = "ACH_BLITZ_SHORT_CIRCUIT";     // score >= 5000
    public const string ACH_BLITZ_MELTDOWN = "ACH_BLITZ_MELTDOWN";               // score >= 10000

    // Blitz — powerups (lifetime one-shots)
    public const string ACH_BLITZ_SHIELD_UP = "ACH_BLITZ_SHIELD_UP";             // shield first deployed
    public const string ACH_BLITZ_CADENCE_FULL = "ACH_BLITZ_CADENCE_FULL";       // cadence level 2
    public const string ACH_BLITZ_CANNON_FULL = "ACH_BLITZ_CANNON_FULL";         // cannon level 2
}
