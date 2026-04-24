using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// C# bridge to NSUbiquitousKeyValueStore (Apple iCloud key-value store, 1 MB shared
/// across a user's signed-in devices). Native plugin lives in Assets/Plugins/{iOS,tvOS}/iCloudKV.mm
/// (Unity auto-compiles) and Assets/Plugins/macOS/iCloudKV.bundle (pre-built universal binary,
/// since Unity doesn't auto-compile .mm for macOS standalone).
/// Non-Apple builds (editor, Steam, Android) return empty values silently, so callers don't need platform gates.
/// </summary>
public class ICloudKVStore : MonoBehaviour
{
    public static ICloudKVStore Instance { get; private set; }

    /// <summary>Fires when NSUbiquitousKeyValueStoreDidChangeExternallyNotification arrives (another device pushed).</summary>
    public event Action OnExternalChange;

    // Flips to false on the first DllNotFoundException so a misconfigured native plugin
    // (unlinked symbols, entitlement mismatch, macOS bundle quirks) gracefully degrades
    // to local-only instead of crashing SceneSetup and leaving the game with no UI.
    private static bool sPluginHealthy = true;

#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
    // iOS/tvOS: plugin is statically linked into the main binary.
    private const string DLL = "__Internal";
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
    // macOS: plugin ships as Loopfall.app/Contents/PlugIns/iCloudKV.bundle.
    // DllImport("__Internal") only resolves statically-linked symbols, so it silently
    // fails with DllNotFoundException on Mac — use the bundle's executable name instead.
    private const string DLL = "iCloudKV";
#endif

#if (UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
    [DllImport(DLL)] private static extern void _iCloudKV_Init(string callbackObject);
    [DllImport(DLL)] private static extern void _iCloudKV_SetString(string key, string value);
    [DllImport(DLL)] private static extern IntPtr _iCloudKV_GetString(string key);
    [DllImport(DLL)] private static extern void _iCloudKV_SetLong(string key, long value);
    [DllImport(DLL)] private static extern long _iCloudKV_GetLong(string key);
    [DllImport(DLL)] private static extern void _iCloudKV_Synchronize();
#endif

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
#if (UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
        // Init in Awake (not Start) so that CloudSync can reconcile PlayerPrefs before
        // ScoreSync.LoadScores() runs in the same frame.
        try { _iCloudKV_Init(gameObject.name); }
        catch (Exception e) { MarkUnhealthy("Init", e); }
#endif
    }

    private static void MarkUnhealthy(string op, Exception e)
    {
        sPluginHealthy = false;
        Debug.LogWarning("[iCloud] native plugin unavailable (" + op + "): " + e.Message + " — falling back to local-only persistence.");
    }

    public bool IsAvailable()
    {
#if (UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
        return sPluginHealthy;
#else
        return false;
#endif
    }

    public void SetString(string key, string value)
    {
#if (UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
        if (!sPluginHealthy) return;
        try { _iCloudKV_SetString(key, value); }
        catch (Exception e) { MarkUnhealthy("SetString", e); }
#endif
    }

    public string GetString(string key)
    {
#if (UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
        if (!sPluginHealthy) return null;
        try
        {
            IntPtr p = _iCloudKV_GetString(key);
            return p == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(p);
        }
        catch (Exception e) { MarkUnhealthy("GetString", e); return null; }
#else
        return null;
#endif
    }

    public void SetLong(string key, long value)
    {
#if (UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
        if (!sPluginHealthy) return;
        try { _iCloudKV_SetLong(key, value); }
        catch (Exception e) { MarkUnhealthy("SetLong", e); }
#endif
    }

    public long GetLong(string key)
    {
#if (UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
        if (!sPluginHealthy) return 0L;
        try { return _iCloudKV_GetLong(key); }
        catch (Exception e) { MarkUnhealthy("GetLong", e); return 0L; }
#else
        return 0L;
#endif
    }

    public void Synchronize()
    {
#if (UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
        if (!sPluginHealthy) return;
        try { _iCloudKV_Synchronize(); }
        catch (Exception e) { MarkUnhealthy("Synchronize", e); }
#endif
    }

    // Invoked by the native observer via UnitySendMessage. Name + signature must match exactly.
    void OnCloudChangedExternally(string _)
    {
        Debug.Log("[iCloud] NSUbiquitousKeyValueStoreDidChangeExternallyNotification received");
        if (OnExternalChange != null) OnExternalChange.Invoke();
    }
}
