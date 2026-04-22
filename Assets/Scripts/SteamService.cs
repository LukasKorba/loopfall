// SteamService — IPlatformService implementation for Steam.
// Handles leaderboards, achievements, and cloud saves via Steamworks.NET.
//
// Leaderboards are found asynchronously on init and cached.
// Scores are queued if the board isn't ready yet and uploaded when it resolves.

using UnityEngine;
using System.Collections.Generic;

#if STEAMWORKS
using Steamworks;
#endif

public class SteamService : MonoBehaviour, IPlatformService
{
    public static SteamService Instance { get; private set; }

#if STEAMWORKS
    // ── LEADERBOARD HANDLES ──────────────────────────────────

    private SteamLeaderboard_t mLBPureHell;
    private SteamLeaderboard_t mLBBlitz;
    private SteamLeaderboard_t mLBTapMaster;
    private SteamLeaderboard_t mLBRuns;

    private bool mLBPureHellReady = false;
    private bool mLBBlitzReady = false;
    private bool mLBTapMasterReady = false;
    private bool mLBRunsReady = false;

    // Queued scores for boards that haven't resolved yet
    private Queue<System.Action> mPendingUploads = new Queue<System.Action>();

    // Callbacks
    private CallResult<LeaderboardFindResult_t> mFindPureHell;
    private CallResult<LeaderboardFindResult_t> mFindBlitz;
    private CallResult<LeaderboardFindResult_t> mFindTapMaster;
    private CallResult<LeaderboardFindResult_t> mFindRuns;
    private CallResult<LeaderboardScoreUploaded_t> mUploadResult;
#endif

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
#if STEAMWORKS
        if (!SteamManager.Initialized) return;

        // Find leaderboards — Steam creates them if they don't exist
        // (must also be created in Steamworks partner site for production)
        mFindPureHell = CallResult<LeaderboardFindResult_t>.Create(OnFindPureHell);
        mFindBlitz = CallResult<LeaderboardFindResult_t>.Create(OnFindBlitz);
        mFindTapMaster = CallResult<LeaderboardFindResult_t>.Create(OnFindTapMaster);
        mFindRuns = CallResult<LeaderboardFindResult_t>.Create(OnFindRuns);
        mUploadResult = CallResult<LeaderboardScoreUploaded_t>.Create(OnScoreUploaded);

        var call1 = SteamUserStats.FindOrCreateLeaderboard("PureHell_HighScore",
            ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending,
            ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric);
        mFindPureHell.Set(call1);

        var call2 = SteamUserStats.FindOrCreateLeaderboard("Blitz_HighScore",
            ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending,
            ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric);
        mFindBlitz.Set(call2);

        var call3 = SteamUserStats.FindOrCreateLeaderboard("TapMaster_Total",
            ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending,
            ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric);
        mFindTapMaster.Set(call3);

        var call4 = SteamUserStats.FindOrCreateLeaderboard("Runs_Total",
            ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending,
            ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric);
        mFindRuns.Set(call4);

        // Request current stats from Steam
        SteamUserStats.RequestCurrentStats();

        Debug.Log("[Steam] Leaderboard discovery started");
#endif
    }

#if STEAMWORKS
    // ── LEADERBOARD CALLBACKS ────────────────────────────────

    void OnFindPureHell(LeaderboardFindResult_t result, bool ioFailure)
    {
        if (!ioFailure && result.m_bLeaderboardFound == 1)
        {
            mLBPureHell = result.m_hSteamLeaderboard;
            mLBPureHellReady = true;
            Debug.Log("[Steam] Pure Hell leaderboard found");
            FlushPending();
        }
    }

    void OnFindBlitz(LeaderboardFindResult_t result, bool ioFailure)
    {
        if (!ioFailure && result.m_bLeaderboardFound == 1)
        {
            mLBBlitz = result.m_hSteamLeaderboard;
            mLBBlitzReady = true;
            Debug.Log("[Steam] Blitz leaderboard found");
            FlushPending();
        }
    }

    void OnFindTapMaster(LeaderboardFindResult_t result, bool ioFailure)
    {
        if (!ioFailure && result.m_bLeaderboardFound == 1)
        {
            mLBTapMaster = result.m_hSteamLeaderboard;
            mLBTapMasterReady = true;
            Debug.Log("[Steam] Tap Master leaderboard found");
            FlushPending();
        }
    }

    void OnFindRuns(LeaderboardFindResult_t result, bool ioFailure)
    {
        if (!ioFailure && result.m_bLeaderboardFound == 1)
        {
            mLBRuns = result.m_hSteamLeaderboard;
            mLBRunsReady = true;
            Debug.Log("[Steam] Runs leaderboard found");
            FlushPending();
        }
    }

    void OnScoreUploaded(LeaderboardScoreUploaded_t result, bool ioFailure)
    {
        if (!ioFailure && result.m_bSuccess == 1)
            Debug.Log($"[Steam] Score uploaded — global rank: {result.m_nGlobalRankNew}");
        else
            Debug.Log("[Steam] Score upload failed");
    }

    void FlushPending()
    {
        while (mPendingUploads.Count > 0)
        {
            var action = mPendingUploads.Dequeue();
            action();
        }
    }

    void UploadScore(SteamLeaderboard_t board, bool ready, int score)
    {
        if (!SteamManager.Initialized) return;

        if (ready)
        {
            var call = SteamUserStats.UploadLeaderboardScore(board,
                ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest,
                score, null, 0);
            mUploadResult.Set(call);
        }
        else
        {
            // Board not found yet — queue for later
            mPendingUploads.Enqueue(() => UploadScore(board, true, score));
        }
    }
#endif

    // ── IPlatformService ─────────────────────────────────────

    public bool IsAuthenticated()
    {
#if STEAMWORKS
        return SteamManager.Initialized;
#else
        return false;
#endif
    }

    public void ReportScore(int score)
    {
#if STEAMWORKS
        if (score <= 0 || !SteamManager.Initialized) return;
        UploadScore(GameConfig.IsBlitz() ? mLBBlitz : mLBPureHell,
                    GameConfig.IsBlitz() ? mLBBlitzReady : mLBPureHellReady,
                    score);
#endif
    }

    public void ReportTaps(int totalTaps)
    {
#if STEAMWORKS
        if (totalTaps <= 0 || !SteamManager.Initialized) return;
        UploadScore(mLBTapMaster, mLBTapMasterReady, totalTaps);
#endif
    }

    public void ReportRuns(int totalRuns)
    {
#if STEAMWORKS
        if (totalRuns <= 0 || !SteamManager.Initialized) return;
        UploadScore(mLBRuns, mLBRunsReady, totalRuns);
#endif
    }

    public void ShowLeaderboard()
    {
#if STEAMWORKS
        if (!SteamManager.Initialized) return;
        // Open Steam overlay to the leaderboard page
        SteamFriends.ActivateGameOverlay("OfficialGameGroup");
        Debug.Log("[Steam] Opening Steam overlay leaderboard");
#endif
    }

    public void UnlockAchievement(string achievementID)
    {
#if STEAMWORKS
        if (!SteamManager.Initialized) return;

        bool alreadyUnlocked;
        SteamUserStats.GetAchievement(achievementID, out alreadyUnlocked);
        if (alreadyUnlocked) return;

        SteamUserStats.SetAchievement(achievementID);
        SteamUserStats.StoreStats();
        Debug.Log($"[Steam] Achievement unlocked: {achievementID}");
#endif
    }

    public void ShowAchievements()
    {
#if STEAMWORKS
        if (!SteamManager.Initialized) return;
        SteamFriends.ActivateGameOverlay("Achievements");
#endif
    }

    // ── CLOUD SAVES ──────────────────────────────────────────

    public void SaveToCloud(string key, string data)
    {
#if STEAMWORKS
        if (!SteamManager.Initialized) return;

        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
        bool ok = SteamRemoteStorage.FileWrite(key, bytes, bytes.Length);
        Debug.Log($"[Steam] Cloud save '{key}': {(ok ? "success" : "failed")} ({bytes.Length} bytes)");
#endif
    }

    public string LoadFromCloud(string key)
    {
#if STEAMWORKS
        if (!SteamManager.Initialized) return null;

        if (!SteamRemoteStorage.FileExists(key)) return null;

        int size = SteamRemoteStorage.GetFileSize(key);
        byte[] bytes = new byte[size];
        int read = SteamRemoteStorage.FileRead(key, bytes, size);
        if (read > 0)
        {
            string data = System.Text.Encoding.UTF8.GetString(bytes, 0, read);
            Debug.Log($"[Steam] Cloud load '{key}': {read} bytes");
            return data;
        }
#endif
        return null;
    }

}
