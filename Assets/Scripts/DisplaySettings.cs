using UnityEngine;

/// <summary>
/// PC display settings — resolution and fullscreen.
/// Only active on standalone (Windows/macOS/Linux) builds.
/// Persists choices via PlayerPrefs.
/// Created by SceneSetup; ScoreSync calls BuildUI() to add toggles to the settings panel.
/// </summary>
public class DisplaySettings : MonoBehaviour
{
    public static DisplaySettings Instance { get; private set; }

    private const string PREF_FULLSCREEN = "DisplayFullscreen";
    private const string PREF_RES_W = "DisplayResW";
    private const string PREF_RES_H = "DisplayResH";

    // Common 16:9 resolutions — landscape for standalone (macOS/Windows/Linux)
    private static readonly Vector2Int[] RESOLUTIONS = new Vector2Int[]
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440),
    };

    private int mCurrentResIndex = 1; // default 1920x1080
    private bool mFullscreen;

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

        int savedW = PlayerPrefs.GetInt(PREF_RES_W, 1920);
        int savedH = PlayerPrefs.GetInt(PREF_RES_H, 1080);

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
        PlayerPrefs.SetInt(PREF_RES_W, RESOLUTIONS[mCurrentResIndex].x);
        PlayerPrefs.SetInt(PREF_RES_H, RESOLUTIONS[mCurrentResIndex].y);
        PlayerPrefs.Save();
    }

    void ApplySettings()
    {
        Vector2Int res = RESOLUTIONS[mCurrentResIndex];
        Screen.SetResolution(res.x, res.y, mFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        // Lock to 60 fps without vsync: on ProMotion/120 Hz macs, vSyncCount=1 renders at
        // refresh rate and ignores targetFrameRate, which made the game run at double speed.
        // Metal's own frame pacing is tear-free at this cap.
        QualitySettings.vSyncCount = 0;
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
