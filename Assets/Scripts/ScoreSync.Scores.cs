using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public partial class ScoreSync
{
    // ── LEADERBOARD PERSISTENCE ──────────────────────────────

    /// <summary>
    /// Public entry for CloudSync's external-change callback: re-reads PlayerPrefs
    /// (already merged with cloud by CloudSync.MergeToLocal) and rebuilds the in-memory list.
    /// </summary>
    public void RefreshFromStorage()
    {
        LoadScores();
    }

    void LoadScores()
    {
        topScores.Clear();
        topTimestamps.Clear();
        string saved = PlayerPrefs.GetString(GameConfig.GetScoresKey(), "");
        if (!string.IsNullOrEmpty(saved))
        {
            string[] parts = saved.Split(',');
            foreach (string p in parts)
            {
                // Format: "score:timestamp" or legacy "score"
                string[] pair = p.Split(':');
                int val;
                if (int.TryParse(pair[0], out val))
                {
                    topScores.Add(val);
                    long ts = 0;
                    if (pair.Length > 1) long.TryParse(pair[1], out ts);
                    topTimestamps.Add(ts);
                }
            }
        }
        SortScoresWithTimestamps();
    }

    void SaveScores()
    {
        var entries = new string[topScores.Count];
        for (int i = 0; i < topScores.Count; i++)
        {
            long ts = i < topTimestamps.Count ? topTimestamps[i] : 0;
            entries[i] = topScores[i] + ":" + ts;
        }
        PlayerPrefs.SetString(GameConfig.GetScoresKey(), string.Join(",", entries));
        PlayerPrefs.Save();
        CloudSync.PushAll();
    }

    void SortScoresWithTimestamps()
    {
        // Sort both lists together by score descending
        var paired = new List<System.Tuple<int, long>>();
        for (int i = 0; i < topScores.Count; i++)
        {
            long ts = i < topTimestamps.Count ? topTimestamps[i] : 0;
            paired.Add(new System.Tuple<int, long>(topScores[i], ts));
        }
        paired.Sort((a, b) => b.Item1.CompareTo(a.Item1));
        topScores.Clear();
        topTimestamps.Clear();
        foreach (var p in paired)
        {
            topScores.Add(p.Item1);
            topTimestamps.Add(p.Item2);
        }
    }

    int InsertScore(int score)
    {
        if (score <= 0) return 0;

        long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        topScores.Add(score);
        topTimestamps.Add(now);
        SortScoresWithTimestamps();

        int rank = topScores.LastIndexOf(score) + 1;

        while (topScores.Count > MAX_SCORES)
        {
            topScores.RemoveAt(topScores.Count - 1);
            if (topTimestamps.Count > MAX_SCORES)
                topTimestamps.RemoveAt(topTimestamps.Count - 1);
        }

        SaveScores();

        // Report to platform (GameCenter on Apple, Steam on PC)
        if (PlatformManager.Instance != null)
        {
            PlatformManager.Instance.ReportScore(score);
            PlatformManager.Instance.ReportTaps(Sphere.GetTotalTaps());
            PlatformManager.Instance.ReportRuns(Sphere.GetTotalRuns());
        }

        return rank <= MAX_SCORES ? rank : 0;
    }

    public void ClearAllScores()
    {
        topScores.Clear();
        topTimestamps.Clear();
        PlayerPrefs.DeleteKey(GameConfig.GetScoresKey());
        PlayerPrefs.Save();
        CloudSync.PushAll();
    }

}
