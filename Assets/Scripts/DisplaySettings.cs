using UnityEngine;

/// <summary>
/// PC display settings — resolution, fullscreen, vsync.
/// Only active on standalone (Windows/macOS/Linux) builds.
/// Persists choices via PlayerPrefs.
/// Created by SceneSetup; ScoreSync calls BuildUI() to add toggles to the settings panel.
/// </summary>
public class DisplaySettings : MonoBehaviour
{
    public static DisplaySettings Instance { get; private set; }

    private const string PREF_FULLSCREEN = "DisplayFullscreen";
    private const string PREF_VSYNC = "DisplayVSync";
    private const string PREF_RES_W = "DisplayResW";
    private const string PREF_RES_H = "DisplayResH";

    // Common 16:9 resolutions (portrait-first since Loopfall is portrait)
    private static readonly Vector2Int[] RESOLUTIONS = new Vector2Int[]
    {
        new Vector2Int(720, 1280),
        new Vector2Int(1080, 1920),
        new Vector2Int(1440, 2560),
    };

    private int mCurrentResIndex = 1; // default 1080x1920
    private bool mFullscreen;
    private bool mVSync;

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
#if UNITY_STANDALONE
        LoadPrefs();
        ApplySettings();
#endif
    }

    void LoadPrefs()
    {
        mFullscreen = PlayerPrefs.GetInt(PREF_FULLSCREEN, 1) == 1;
        mVSync = PlayerPrefs.GetInt(PREF_VSYNC, 0) == 1;

        int savedW = PlayerPrefs.GetInt(PREF_RES_W, 1080);
        int savedH = PlayerPrefs.GetInt(PREF_RES_H, 1920);

        mCurrentResIndex = 1; // default
        for (int i = 0; i < RESOLUTIONS.Length; i++)
        {
            if (RESOLUTIONS[i].x == savedW && RESOLUTIONS[i].y == savedH)
            {
                mCurrentResIndex = i;
                break;
            }
        }
    }

    void SavePrefs()
    {
        PlayerPrefs.SetInt(PREF_FULLSCREEN, mFullscreen ? 1 : 0);
        PlayerPrefs.SetInt(PREF_VSYNC, mVSync ? 1 : 0);
        PlayerPrefs.SetInt(PREF_RES_W, RESOLUTIONS[mCurrentResIndex].x);
        PlayerPrefs.SetInt(PREF_RES_H, RESOLUTIONS[mCurrentResIndex].y);
        PlayerPrefs.Save();
    }

    void ApplySettings()
    {
        Vector2Int res = RESOLUTIONS[mCurrentResIndex];
        Screen.SetResolution(res.x, res.y, mFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        QualitySettings.vSyncCount = mVSync ? 1 : 0;
        if (!mVSync)
            Application.targetFrameRate = 60;
    }

    // ── PUBLIC API (called by ScoreSync settings UI) ─────────

    public void ToggleFullscreen()
    {
        mFullscreen = !mFullscreen;
        SavePrefs();
        ApplySettings();
    }

    public bool IsFullscreen() { return mFullscreen; }

    public void ToggleVSync()
    {
        mVSync = !mVSync;
        SavePrefs();
        ApplySettings();
    }

    public bool IsVSync() { return mVSync; }

    public void CycleResolution()
    {
        mCurrentResIndex = (mCurrentResIndex + 1) % RESOLUTIONS.Length;
        SavePrefs();
        ApplySettings();
    }

    public string GetResolutionLabel()
    {
        Vector2Int res = RESOLUTIONS[mCurrentResIndex];
        return $"{res.x} x {res.y}";
    }
}
