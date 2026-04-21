using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reconciles PlayerPrefs (scores + stats) with NSUbiquitousKeyValueStore so a user's best scores
/// and lifetime counters follow them across iOS / tvOS / macOS. No sync on Android/Editor/Steam.
///
/// Strategy:
/// - Score lists: union of local + cloud entries, dedupe on (score, timestamp), sort desc, cap at MAX_SCORES.
/// - Counters: max(local, cloud). Simple and avoids double-counting after reinstall / new device.
/// - On startup: Pull cloud, MergeToLocal, PushAll (so both sides converge).
/// - On local change: PushAll.
/// - On external-change notification: Pull, MergeToLocal. ScoreSync rebuilds its in-memory list from PlayerPrefs.
/// </summary>
public static class CloudSync
{
    private const int MAX_SCORES = 10;

    // Score lists (one per mode). Keys match PlayerPrefs keys in GameConfig.GetScoresKey().
    private static readonly string[] SCORE_KEYS = { "TopScores", "TopScores_Blitz" };

    // Lifetime counters. Keys match STAT_* constants in Sphere.cs.
    private static readonly string[] COUNTER_KEYS = {
        "TotalTaps", "TotalRuns",
        "TotalTaps_Blitz", "TotalRuns_Blitz", "TotalObstacles_Blitz"
    };

    private static bool mHooked = false;
    private static System.Action mExternalChangeHandler;

    /// <summary>Call once at startup after ICloudKVStore is created. Pulls cloud, merges, pushes back.</summary>
    public static void InitAndReconcile(System.Action onExternalChangeRefreshUI = null)
    {
        var kv = ICloudKVStore.Instance;
        if (kv == null || !kv.IsAvailable())
        {
            Debug.Log("[CloudSync] InitAndReconcile skipped — KV store unavailable.");
            return;
        }

        Debug.Log("[CloudSync] InitAndReconcile: synchronize + merge + push");
        kv.Synchronize();
        MergeToLocal();
        PushAll();

        if (!mHooked)
        {
            mExternalChangeHandler = () =>
            {
                Debug.Log("[CloudSync] external change notification — merging");
                MergeToLocal();
                if (onExternalChangeRefreshUI != null) onExternalChangeRefreshUI();
            };
            kv.OnExternalChange += mExternalChangeHandler;
            mHooked = true;
        }
    }

    /// <summary>Push current PlayerPrefs values for scores + counters up to iCloud.</summary>
    public static void PushAll()
    {
        var kv = ICloudKVStore.Instance;
        if (kv == null || !kv.IsAvailable()) return;

        foreach (string key in SCORE_KEYS)
        {
            string val = PlayerPrefs.GetString(key, "");
            kv.SetString(key, val);
            Debug.Log("[CloudSync] push " + key + " = \"" + val + "\" (" + val.Length + " chars)");
        }

        foreach (string key in COUNTER_KEYS)
        {
            long val = PlayerPrefs.GetInt(key, 0);
            kv.SetLong(key, val);
            Debug.Log("[CloudSync] push " + key + " = " + val);
        }

        kv.Synchronize();
    }

    /// <summary>
    /// Read cloud values, merge with local, and write the merged result to PlayerPrefs.
    /// Callers that hold in-memory caches (e.g. ScoreSync.topScores) should reload after this.
    /// </summary>
    public static void MergeToLocal()
    {
        var kv = ICloudKVStore.Instance;
        if (kv == null || !kv.IsAvailable()) return;

        foreach (string key in SCORE_KEYS)
        {
            string local = PlayerPrefs.GetString(key, "");
            string cloud = kv.GetString(key);
            Debug.Log("[CloudSync] pull " + key + ": local=\"" + local + "\" cloud=\"" + (cloud ?? "<null>") + "\"");
            if (string.IsNullOrEmpty(cloud)) continue;
            string merged = MergeScoreLists(local, cloud);
            if (merged != local)
            {
                PlayerPrefs.SetString(key, merged);
                Debug.Log("[CloudSync] merged " + key + " -> \"" + merged + "\"");
            }
        }

        foreach (string key in COUNTER_KEYS)
        {
            long local = PlayerPrefs.GetInt(key, 0);
            long cloud = kv.GetLong(key);
            Debug.Log("[CloudSync] pull " + key + ": local=" + local + " cloud=" + cloud);
            if (cloud > local) PlayerPrefs.SetInt(key, (int)cloud);
        }

        PlayerPrefs.Save();
    }

    // Serialized format (matches ScoreSync.Scores.cs): "score:timestamp,score:timestamp,..."
    private static string MergeScoreLists(string a, string b)
    {
        var map = new Dictionary<long, int>(); // key: (score<<32)|timestamp, value: score
        AddEntries(a, map);
        AddEntries(b, map);

        var entries = new List<KeyValuePair<int, long>>();
        foreach (var kv in map)
        {
            int score = kv.Value;
            long ts = kv.Key & 0xFFFFFFFFL;
            entries.Add(new KeyValuePair<int, long>(score, ts));
        }

        entries.Sort((x, y) => y.Key.CompareTo(x.Key));
        if (entries.Count > MAX_SCORES) entries.RemoveRange(MAX_SCORES, entries.Count - MAX_SCORES);

        var parts = new string[entries.Count];
        for (int i = 0; i < entries.Count; i++)
            parts[i] = entries[i].Key + ":" + entries[i].Value;
        return string.Join(",", parts);
    }

    private static void AddEntries(string serialized, Dictionary<long, int> into)
    {
        if (string.IsNullOrEmpty(serialized)) return;
        foreach (string entry in serialized.Split(','))
        {
            string[] pair = entry.Split(':');
            int score;
            if (!int.TryParse(pair[0], out score)) continue;
            long ts = 0;
            if (pair.Length > 1) long.TryParse(pair[1], out ts);
            long key = ((long)score << 32) | (ts & 0xFFFFFFFFL);
            into[key] = score;
        }
    }
}
