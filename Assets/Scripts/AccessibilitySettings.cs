using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Persisted Reduce Motion preference with optional system-level detection on Apple.
/// Three-state: System / On / Off. "System" follows iOS/tvOS/macOS accessibility;
/// on non-Apple platforms it falls through to motion-enabled.
/// Native plugin lives in Assets/Plugins/iOS/ReduceMotionDetector.mm (auto-compiled for iOS/tvOS)
/// and, for macOS standalone, a pre-built .bundle (same pattern as iCloudKV).
/// </summary>
public static class AccessibilitySettings
{
    public enum Mode { System = 0, On = 1, Off = 2 }

    private const string PREF_MODE = "ReduceMotionMode";

    private static bool sPluginHealthy = true;

#if (UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern int _ReduceMotion_IsEnabled();
#endif

    public static Mode CurrentMode
    {
        get { return (Mode)PlayerPrefs.GetInt(PREF_MODE, (int)Mode.System); }
        set
        {
            PlayerPrefs.SetInt(PREF_MODE, (int)value);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Final effective state. Chromatic aberration + camera shake consult this every run.
    /// </summary>
    public static bool IsReduceMotionActive()
    {
        switch (CurrentMode)
        {
            case Mode.On:  return true;
            case Mode.Off: return false;
            default:       return IsSystemReduceMotionEnabled();
        }
    }

    public static bool IsSystemReduceMotionEnabled()
    {
#if (UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
        if (!sPluginHealthy) return false;
        try { return _ReduceMotion_IsEnabled() != 0; }
        catch (Exception e)
        {
            sPluginHealthy = false;
            Debug.LogWarning("[Accessibility] native plugin unavailable: " + e.Message + " — 'System' mode will treat reduce-motion as off.");
            return false;
        }
#else
        return false;
#endif
    }

    public static void CycleMode()
    {
        CurrentMode = (Mode)(((int)CurrentMode + 1) % 3);
    }
}
