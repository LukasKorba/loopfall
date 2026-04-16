using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class ScoreSync : MonoBehaviour
{
    public TextMesh source;

    // ── COLOR PALETTE ────────────────────────────────────────
    static readonly Color NEON_CYAN = new Color(0.0f, 0.75f, 1.0f);
    static readonly Color NEON_MAGENTA = new Color(1.0f, 0.15f, 0.55f);
    static readonly Color NEON_GOLD = new Color(1.0f, 0.92f, 0.2f);
    static readonly Color DEEP_PURPLE = new Color(0.08f, 0.03f, 0.14f);
    static readonly Color DIM_TEXT = new Color(0.55f, 0.58f, 0.65f);

    // ── LEADERBOARD ──────────────────────────────────────────
    private List<int> topScores = new List<int>();
    private List<long> topTimestamps = new List<long>();
    private const int MAX_SCORES = 10;
    private const string SCORES_KEY = "TopScores";

    // ── STATE ────────────────────────────────────────────────
    private enum State { Splash, Title, Playing, Rewinding, GameOver }
#if UNITY_EDITOR
    private State state = State.Title;
#else
    private State state = State.Splash;
#endif
    private float stateTimer = 0f;
    private State prevState = State.Title;
    private string lastScoreText = "0";
    private bool isNewBest = false;
    private bool isTopFive = false;
    private int goFinalScore = 0;
    private int goRank = 0;
    private bool isFirstRun = false;
    private const string PREF_FIRST_RUN = "HasPlayed";
    private bool isPaused = false;

    // ── CANVAS ───────────────────────────────────────────────
    private Canvas canvas;
    private Image overlayImage;
    private RawImage vignetteImage;
    private RawImage scanlinesImage;
    private Image blackoutImage;  // Full black to hide torus during splash
    private Image deathFlashImage; // Brief white flash on death
    private float deathFlashTimer = -1f;
    private const float DEATH_FLASH_DURATION = 0.35f;

    // ── SPLASH ───────────────────────────────────────────────
    private RectTransform splashGroup;
    private TMP_Text splashNameText;
    private TMP_Text splashNameCyanText;
    private TMP_Text splashNameMagentaText;
    private TMP_Text splashNameYellowText;
    private TMP_Text splashPresentsText;
    private RawImage splashStarsImage;
    private const float SPLASH_HOLD = 4.5f;
    private const float SPLASH_FADE_IN = 0.8f;
    private const float SPLASH_FADE_OUT = 1.0f;

    // ── TITLE ────────────────────────────────────────────────
    private RectTransform titleGroup;
    private TMP_Text titleText;
    private TMP_Text titleCyanText;
    private TMP_Text titleMagentaText;
    private TMP_Text titleYellowText;
    private TMP_Text subtitleText;
    private Image titleRuleLeft;
    private Image titleRuleRight;
    private TMP_Text bestScoreText;
    private Image bestScoreLine;
    private Button titleLBBtn;
    private CanvasGroup titleLBIcon;
    private Button titleSettingsBtn;
    private CanvasGroup titleSettingsIcon;
    private TMP_Text titleTapText;
    private TMP_Text titleHintText;
    private Button titlePureHellBtn;
    private TMP_Text titlePureHellLabel;
    private Button titleTimeWarpBtn;
    private TMP_Text titleTimeWarpLabel;

    // ── PLAYING ──────────────────────────────────────────────
    private RectTransform playingGroup;
    private TMP_Text playingScoreText;
    private TMP_Text playingScoreTextOut;
    private string lastPlayingScore = "0";
    private float scoreAnimTimer = -1f;
    private const float SCORE_ANIM_DURATION = 0.25f;
    private const float SCORE_SLIDE_DISTANCE = 60f;
    private float scorePopTimer = -1f;
    private const float SCORE_POP_DURATION = 0.3f;
    private const float SCORE_POP_SCALE = 1.35f;
    private float scoreGlowTimer = -1f;
    private const float SCORE_GLOW_DURATION = 0.5f;
    private Image[] streakDots;
    private const int STREAK_COUNT = 5;
    private int currentStreak = 0;
    private float streakFlashTimer = -1f;

    // ── BLITZ UPGRADE HUD ────────────────────────────────────
    private RectTransform blitzUpgradeGroup;
    private Image[] blitzGunSlots;
    private Image[] blitzCadencySlots;
    private Image[] blitzShieldSlots;
    private int blitzLastGunCount = -1;
    private int blitzLastCadencyCount = -1;
    private int blitzLastShieldCount = -1;

    // ── BLITZ ORB SPARKS ────────────────────────────────────
    private const int MAX_SPARKS = 6;
    private const float SPARK_DURATION = 0.45f;
    private const float SPARK_SIZE = 16f;
    private Image[] sparkImages;
    private Vector2[] sparkFrom;
    private Vector2[] sparkTo;
    private float[] sparkTimers;
    private int[] sparkSlotIndex;
    private BlitzOrb.OrbType[] sparkOrbType;
    private RectTransform canvasRT;

    // ── TIME WARP POPUP ──────────────────────────────────────
    private TMP_Text playingPopupText;
    private float popupAnimTimer = -1f;
    private float popupAnimSign = 0f;
    private const float POPUP_DURATION = 0.9f;

    // ── GAME OVER ────────────────────────────────────────────
    private RectTransform gameOverGroup;
    private TMP_Text goScoreText;
    private TMP_Text goScoreGlowText;
    private TMP_Text goNewBestText;
    private TMP_Text goTapText;
    private TMP_Text[] goLeaderboardTexts;
    private TMP_Text[] goLeaderboardDateTexts;
    private const int LEADERBOARD_SHOW = 5;
    private Button goSettingsBtn;
    private CanvasGroup goSettingsIcon;
    private Button goLBBtn;
    private CanvasGroup goLBIcon;
    private RectTransform[] goCornerBrackets;
    private Image goScoreDivider;

    // ── SETTINGS PANEL ───────────────────────────────────────
    private RectTransform settingsPanel;
    private TMP_Text settingsMusicLabel;
    private TMP_Text settingsSoundLabel;
    private TMP_Text settingsThemeLabel;
    private TMP_Text settingsFullscreenLabel;
    private TMP_Text settingsVSyncLabel;
    private TMP_Text settingsResLabel;
    // ── QUIT BUTTON (macOS only) ────────────────────────────
    private Button titleQuitBtn;
    private CanvasGroup titleQuitIcon;
    private Button goQuitBtn;
    private CanvasGroup goQuitIcon;

    // ── STATS PANEL ─────────────────────────────────────────
    private RectTransform statsPanel;
    private Button titleStatsBtn;
    private CanvasGroup titleStatsIcon;
    private Button goStatsBtn;
    private CanvasGroup goStatsIcon;
    private TMP_Text statsRunsLabel;
    private TMP_Text statsTapsLabel;
    private TMP_Text statsBestLabel;
    private TMP_Text statsAvgLabel;
    private TMP_Text statsGatesLabel;

    // ── TITLE FADE ───────────────────────────────────────────
    private CanvasGroup titleCanvasGroup;
    private float titleFadeOutTimer = -1f;
    private const float TITLE_FADE_DURATION = 0.5f;

    // ── GAME OVER CHROMATIC ──────────────────────────────────
    private TMP_Text goScoreCyanText;
    private TMP_Text goScoreMagentaText;
    private TMP_Text goScoreYellowText;
    private int goLastDisplayScore = -1; // Track count-up ticks for SFX
    private bool goNewBestSfxPlayed = false;

    // ── NEW BEST GLITTER ─────────────────────────────────────
    private const int GLITTER_COUNT = 120;
    private RectTransform[] glitterRTs;
    private Image[] glitterImages;
    private Vector2[] glitterVel;
    private float[] glitterLife;
    private float[] glitterMaxLife;
    private float[] glitterRot;
    private float[] glitterRotSpeed;
    private float[] glitterSize;
    private float glitterSpawnTimer;
    private bool glitterActive;

    // ── VHS GLITCH ────────────────────────────────────────────
    private float glitchTimer = 0f;          // Countdown to next burst
    private float glitchBurstTimer = -1f;    // Active burst timer
    private float glitchBurstDuration = 0f;  // How long current burst lasts
    private float glitchIntensity = 0f;      // 0-1 severity of current burst
    private float glitchSeed = 0f;           // Random seed per burst
    private const float GLITCH_MIN_INTERVAL = 2.5f;
    private const float GLITCH_MAX_INTERVAL = 7.0f;
    private const float GLITCH_MIN_DURATION = 0.06f;
    private const float GLITCH_MAX_DURATION = 0.25f;

    // Splash-specific scheduled glitches (1-3 guaranteed bursts)
    private float[] splashGlitchTimes;
    private int splashGlitchNext;

    // ── PAUSE ────────────────────────────────────────────────
    private RectTransform pauseGroup;
    private TMP_Text pausedText;
    private TMP_Text pauseHintText;
    private Image pauseDimImage;
    private float pauseAnimTimer = -1f;
    private const float PAUSE_ANIM_DURATION = 0.4f;

    // ── FONT ─────────────────────────────────────────────────
    private TMP_FontAsset defaultFont;
    private Sprite circleSprite;

    // ── CACHED REFS ─────────────────────────────────────────
    private Sphere mSphere;
    private GameAudio mAudio;

    void Start()
    {
#if UNITY_EDITOR
        PlayerPrefs.DeleteKey("HasPlayed");
#endif
        LoadScores();
        defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        circleSprite = CreateCircleSprite(32);
        mSphere = FindAnyObjectByType<Sphere>();
        mAudio = FindAnyObjectByType<GameAudio>();
        isFirstRun = PlayerPrefs.GetInt(PREF_FIRST_RUN, 0) == 0;
        BuildUI();
    }

    void Update()
    {
        if (canvas == null) return;

        // Menu button (tvOS) / Escape = close settings if open
        if (Input.GetKeyDown(KeyCode.Escape) && IsSettingsOpen())
        {
            CloseSettings();
            return;
        }

        // Escape during gameplay = pause; Escape while paused = resume
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
                return;
            }
            else if (state == State.Playing)
            {
                PauseGame();
                return;
            }
        }

        // While paused, tap anywhere to resume (mobile/touch)
        if (isPaused)
        {
            if (Input.GetMouseButtonDown(0) || (Input.touchCount == 1 && Input.touches[0].phase == TouchPhase.Began))
                ResumeGame();
            // If player died while paused, force-clear pause so rewind can proceed
            if (mSphere != null && (mSphere.IsGameOver() || mSphere.IsRewinding()))
                ResumeGame();
            // Keep animating pause overlay even while paused
            AnimatePause();
            if (isPaused) return;
        }

        // Splash is self-timed — don't poll sphere state
        if (state == State.Splash)
        {
            stateTimer += Time.deltaTime;
            AnimateState();
            return;
        }

        if (mSphere == null) return;

        State newState;
        if (mSphere.IsWaiting())
            newState = State.Title;
        else if (mSphere.IsRewinding())
            newState = State.Rewinding;
        else if (mSphere.IsGameOver())
            newState = State.GameOver;
        else
            newState = State.Playing;

        if (newState != state)
        {
            // Clear pause when leaving Playing state
            if (isPaused) ResumeGame();

            State fromState = state;
            state = newState;
            stateTimer = 0f;

            if (state == State.Rewinding)
            {
                OnGameOver();
                deathFlashTimer = 0f;
            }
            if (state == State.GameOver && fromState == State.Playing)
            {
                OnGameOver();
                deathFlashTimer = 0f;
            }
            if (state == State.Playing)
            {
                StopGlitter();
                lastPlayingScore = "0";
                if (playingScoreText != null) playingScoreText.text = "0";
                scoreAnimTimer = -1f;
                scorePopTimer = -1f;
                scoreGlowTimer = -1f;
                currentStreak = 0;
                streakFlashTimer = -1f;
                blitzLastGunCount = -1;
                blitzLastCadencyCount = -1;
                blitzLastShieldCount = -1;
                titleFadeOutTimer = 0f;

                // First-run tutorial: mark as played (hint text won't show again)
                if (isFirstRun)
                {
                    isFirstRun = false;
                    PlayerPrefs.SetInt(PREF_FIRST_RUN, 1);
                    PlayerPrefs.Save();
                }
            }

            // Skip title screen on restart — fade game over overlay straight to gameplay
            bool skipTitle = (state == State.Title && fromState == State.GameOver);

            if (skipTitle)
            {
                // Hide all groups, fade overlays from current values
                SetGroupActive(titleGroup, false);
                SetGroupActive(playingGroup, false);
                SetGroupActive(gameOverGroup, false);
                titleFadeOutTimer = 0f;
            }
            else
            {
                ShowGroup(state);
            }

            // Keep title visible during fade-out (only from actual title screen)
            if (state == State.Playing && titleCanvasGroup != null
                && fromState == State.Title && prevState != State.GameOver)
            {
                titleGroup.gameObject.SetActive(true);
                titleCanvasGroup.alpha = 1f;
            }

            prevState = fromState;
        }

        stateTimer += Time.deltaTime;
        AnimateState();
    }

    void OnGameOver()
    {
        if (GameConfig.IsBlitz())
        {
            Torus torus = FindAnyObjectByType<Torus>();
            goFinalScore = torus != null ? torus.GetScore() : 0;
            lastScoreText = goFinalScore.ToString();
            goRank = InsertScore(goFinalScore);
            isNewBest = (goRank == 1);
            isTopFive = (goRank >= 2 && goRank <= 5);
            goLastDisplayScore = -1;
            goNewBestSfxPlayed = false;
            return;
        }

        if (GameConfig.IsTimeWarp())
        {
            FrenzyTimer timer = FindAnyObjectByType<FrenzyTimer>();
            if (timer != null)
                goFinalScore = Mathf.RoundToInt(timer.GetElapsedTime() * 10f);
            else
                goFinalScore = 0;
            lastScoreText = FormatTimeScore(goFinalScore);
        }
        else
        {
            if (source == null) return;
            string text = source.text;
            string scoreText = text.Contains("\n") ? text.Split('\n')[0] : text;
            lastScoreText = scoreText;
            int.TryParse(scoreText, out goFinalScore);
        }

        goRank = InsertScore(goFinalScore);
        isNewBest = (goRank == 1);
        isTopFive = (goRank >= 2 && goRank <= 5);
        goLastDisplayScore = -1;
        goNewBestSfxPlayed = false;
    }

    string FormatTimeScore(int deciseconds)
    {
        return (deciseconds / 10f).ToString("F1") + "s";
    }

    string FormatModeScore(int score)
    {
        if (GameConfig.IsTimeWarp()) return FormatTimeScore(score);
        return score.ToString();
    }

    string FormatTimestamp(long unixSeconds)
    {
        if (unixSeconds <= 0) return "---";
        var dt = new System.DateTimeOffset(1970, 1, 1, 0, 0, 0, System.TimeSpan.Zero)
            .AddSeconds(unixSeconds).LocalDateTime;
        var now = System.DateTime.Now;
        string month = dt.ToString("MMM").ToUpper();
        if (dt.Year != now.Year)
            return month + " '" + (dt.Year % 100).ToString("D2");
        return month + " " + dt.Day;
    }

    public bool IsSplash() { return state == State.Splash; }
    public bool IsPaused() { return isPaused; }

    public bool CanRestart()
    {
        if (state != State.GameOver || stateTimer < 1.0f) return false;

        if (!GameConfig.IsTimeWarp() && !GameConfig.IsBlitz())
        {
            if (mSphere != null && mSphere.mRewindSystem != null
                && !mSphere.mRewindSystem.IsFullyComplete())
                return false;
        }

        return true;
    }

    // ── UI CONSTRUCTION ──────────────────────────────────────

    void BuildUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("UICanvas");
        canvasObj.transform.SetParent(transform);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        // EventSystem — required for button clicks and UI raycasting
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Full black overlay — hides torus during splash
        blackoutImage = CreateImage(canvasObj.transform, "Blackout",
            new Color(0f, 0f, 0f, 1f));
        StretchFull(blackoutImage.rectTransform);
        blackoutImage.raycastTarget = false;

        // Death flash overlay — white, starts invisible
        deathFlashImage = CreateImage(canvasObj.transform, "DeathFlash",
            new Color(0.7f, 0.85f, 1f, 0f));
        StretchFull(deathFlashImage.rectTransform);
        deathFlashImage.raycastTarget = false;

        // Purple-tinted overlay — retro color grading
        overlayImage = CreateImage(canvasObj.transform, "Overlay",
            new Color(0.06f, 0.02f, 0.10f, 0f));
        StretchFull(overlayImage.rectTransform);
        overlayImage.raycastTarget = false;

        // Vignette overlay (procedural radial gradient)
        vignetteImage = CreateVignette(canvasObj.transform);

        // CRT scanlines — retro horizontal bands
        scanlinesImage = CreateScanlines(canvasObj.transform);

        BuildSplashGroup(canvasObj.transform);
        BuildTitleGroup(canvasObj.transform);
        BuildPlayingGroup(canvasObj.transform);
        BuildGameOverGroup(canvasObj.transform);
        BuildPauseGroup(canvasObj.transform);

        ShowGroup(state);

        // Spark pool for orb collection effects
        canvasRT = canvasObj.GetComponent<RectTransform>();
        if (GameConfig.IsBlitz())
            InitOrbSparks(canvasObj.transform);
    }

    void InitOrbSparks(Transform parent)
    {
        sparkImages = new Image[MAX_SPARKS];
        sparkFrom = new Vector2[MAX_SPARKS];
        sparkTo = new Vector2[MAX_SPARKS];
        sparkTimers = new float[MAX_SPARKS];
        sparkSlotIndex = new int[MAX_SPARKS];
        sparkOrbType = new BlitzOrb.OrbType[MAX_SPARKS];

        for (int i = 0; i < MAX_SPARKS; i++)
        {
            Image img = CreateImage(parent, "OrbSpark_" + i, Color.white);
            RectTransform srt = img.rectTransform;
            srt.anchorMin = new Vector2(0.5f, 0.5f);
            srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.pivot = new Vector2(0.5f, 0.5f);
            srt.sizeDelta = new Vector2(SPARK_SIZE, SPARK_SIZE);
            img.gameObject.SetActive(false);
            sparkImages[i] = img;
            sparkTimers[i] = -1f;
        }
    }

    void BuildSplashGroup(Transform parent)
    {
        splashGroup = CreateGroup(parent, "SplashGroup");

        // Stars background — procedural
        splashStarsImage = CreateStarsTexture(splashGroup.transform);

        // Chromatic aberration layers for name
        splashNameYellowText = CreateText(splashGroup, "SplashYellow", "LUKAS KORBA",
            90, FontStyles.Bold, new Color(1f, 0.95f, 0.1f, 0f));
        SetAnchored(splashNameYellowText.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(1000, 120));

        splashNameMagentaText = CreateText(splashGroup, "SplashMagenta", "LUKAS KORBA",
            90, FontStyles.Bold, new Color(1f, 0.15f, 0.55f, 0f));
        SetAnchored(splashNameMagentaText.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(1000, 120));

        splashNameCyanText = CreateText(splashGroup, "SplashCyan", "LUKAS KORBA",
            90, FontStyles.Bold, new Color(0f, 0.85f, 1f, 0f));
        SetAnchored(splashNameCyanText.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(1000, 120));

        // Main name (white)
        splashNameText = CreateText(splashGroup, "SplashName", "LUKAS KORBA",
            90, FontStyles.Bold, new Color(1f, 1f, 1f, 0f));
        SetAnchored(splashNameText.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(1000, 120));

        // "PRESENTS"
        splashPresentsText = CreateText(splashGroup, "SplashPresents", "PRESENTS",
            30, FontStyles.Normal, new Color(DIM_TEXT.r, DIM_TEXT.g, DIM_TEXT.b, 0f));
        SetAnchored(splashPresentsText.rectTransform, new Vector2(0.5f, 0.46f), new Vector2(500, 45));
        splashPresentsText.characterSpacing = 16f;

        ApplyDropShadow(splashNameText);
        ApplyDropShadow(splashPresentsText);
    }

    RawImage CreateStarsTexture(Transform parent)
    {
        int w = 512;
        int h = 1024;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[w * h];

        float cx = w * 0.5f;
        float cy = h * 0.45f; // Vanishing point slightly above center
        float maxR = w * 0.7f;

        for (int py = 0; py < h; py++)
        {
            for (int px = 0; px < w; px++)
            {
                float dx = px - cx;
                float dy = py - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float normDist = dist / maxR;

                // Dark void background with subtle purple
                float bgR = 0.02f + normDist * 0.01f;
                float bgG = 0.008f + normDist * 0.005f;
                float bgB = 0.035f + normDist * 0.015f;

                // Concentric rings — tunnel cross-section
                float ringSpacing = 38f;
                float ringDist = (dist % ringSpacing) / ringSpacing;
                float ringEdge = 1f - Mathf.Abs(ringDist - 0.5f) * 2f;
                float ring = Mathf.Pow(Mathf.Clamp01(ringEdge), 18f);
                // Rings fade toward center (perspective: closer = wider apart)
                float ringFade = Mathf.Clamp01((normDist - 0.08f) / 0.4f) * Mathf.Clamp01(1f - (normDist - 0.85f) / 0.15f);
                ring *= ringFade * 0.35f;

                // Radial lines — like looking down a tunnel
                float angle = Mathf.Atan2(dy, dx);
                float radialCount = 12f;
                float radialDist = Mathf.Abs(Mathf.Sin(angle * radialCount));
                float radial = Mathf.Pow(Mathf.Clamp01(1f - radialDist), 28f);
                float radialFade = Mathf.Clamp01((normDist - 0.06f) / 0.3f) * Mathf.Clamp01(1f - (normDist - 0.8f) / 0.2f);
                radial *= radialFade * 0.2f;

                // Central glow — faint neon at vanishing point
                float glow = Mathf.Exp(-normDist * normDist * 8f) * 0.12f;

                // Combine: cyan rings, magenta radials, white center glow
                float r = bgR + ring * 0.0f  + radial * 0.6f + glow * 0.7f;
                float g = bgG + ring * 0.45f + radial * 0.08f + glow * 0.7f;
                float b = bgB + ring * 0.65f + radial * 0.35f + glow * 0.85f;

                pixels[py * w + px] = new Color(
                    Mathf.Clamp01(r),
                    Mathf.Clamp01(g),
                    Mathf.Clamp01(b), 1f);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;

        GameObject obj = new GameObject("TunnelBg");
        obj.transform.SetParent(parent, false);
        RawImage img = obj.AddComponent<RawImage>();
        img.texture = tex;
        img.color = new Color(1f, 1f, 1f, 0f);
        img.raycastTarget = false;
        StretchFull(img.rectTransform);
        return img;
    }

    void BuildTitleGroup(Transform parent)
    {
        titleGroup = CreateGroup(parent, "TitleGroup");
        titleCanvasGroup = titleGroup.gameObject.AddComponent<CanvasGroup>();

        // Chromatic aberration layers — CMYK channel separation
        // Order: yellow (back), magenta, cyan, white (front)
        titleYellowText = CreateText(titleGroup, "TitleYellow", "LOOPFALL",
            140, FontStyles.Bold, new Color(1f, 0.95f, 0.1f, 0f));
        SetAnchored(titleYellowText.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(1000, 160));

        titleMagentaText = CreateText(titleGroup, "TitleMagenta", "LOOPFALL",
            140, FontStyles.Bold, new Color(1f, 0.15f, 0.55f, 0f));
        SetAnchored(titleMagentaText.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(1000, 160));

        titleCyanText = CreateText(titleGroup, "TitleCyan", "LOOPFALL",
            140, FontStyles.Bold, new Color(0f, 0.85f, 1f, 0f));
        SetAnchored(titleCyanText.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(1000, 160));

        // Main title (white) — per-character animation driven in AnimateTitle
        titleText = CreateText(titleGroup, "Title", "LOOPFALL",
            140, FontStyles.Bold, new Color(1f, 1f, 1f, 0f));
        SetAnchored(titleText.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(1000, 160));

        // Subtitle — different parallax speed
        subtitleText = CreateText(titleGroup, "Subtitle", "ENDLESS TUNNEL RUNNER",
            32, FontStyles.Normal, new Color(DIM_TEXT.r, DIM_TEXT.g, DIM_TEXT.b, 0f));
        SetAnchored(subtitleText.rectTransform, new Vector2(0.5f, 0.64f), new Vector2(900, 55));
        subtitleText.characterSpacing = 14f;

        // Thin rule lines flanking the subtitle — expand from center
        titleRuleLeft = CreateImage(titleGroup.transform, "RuleL",
            new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0f));
        RectTransform rlRT = titleRuleLeft.rectTransform;
        rlRT.anchorMin = new Vector2(0.5f, 0.645f);
        rlRT.anchorMax = new Vector2(0.5f, 0.645f);
        rlRT.pivot = new Vector2(1f, 0.5f);
        rlRT.sizeDelta = new Vector2(0f, 1f);
        rlRT.anchoredPosition = new Vector2(-260f, 0f);

        titleRuleRight = CreateImage(titleGroup.transform, "RuleR",
            new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0f));
        RectTransform rrRT = titleRuleRight.rectTransform;
        rrRT.anchorMin = new Vector2(0.5f, 0.645f);
        rrRT.anchorMax = new Vector2(0.5f, 0.645f);
        rrRT.pivot = new Vector2(0f, 0.5f);
        rrRT.sizeDelta = new Vector2(0f, 1f);
        rrRT.anchoredPosition = new Vector2(260f, 0f);

        // Best score
        bestScoreText = CreateText(titleGroup, "BestScore", "",
            38, FontStyles.Bold, new Color(NEON_GOLD.r, NEON_GOLD.g, NEON_GOLD.b, 0f));
        SetAnchored(bestScoreText.rectTransform, new Vector2(0.5f, 0.42f), new Vector2(500, 50));
        bestScoreText.characterSpacing = 6f;

        // Thin gold line under best score — expands from center
        bestScoreLine = CreateImage(titleGroup.transform, "BestLine",
            new Color(NEON_GOLD.r, NEON_GOLD.g, NEON_GOLD.b, 0f));
        RectTransform lineRT = bestScoreLine.rectTransform;
        lineRT.anchorMin = new Vector2(0.5f, 0.397f);
        lineRT.anchorMax = new Vector2(0.5f, 0.397f);
        lineRT.pivot = new Vector2(0.5f, 0.5f);
        lineRT.sizeDelta = new Vector2(0f, 1.5f);
        lineRT.anchoredPosition = Vector2.zero;

        // Mode buttons — hidden for now (tap anywhere starts Pure Hell)
        titlePureHellBtn = CreateModeButton(titleGroup, "PureHellBtn",
            new Vector2(0.5f, 0.28f), out titlePureHellLabel, "PURE HELL",
            NEON_MAGENTA);
        titlePureHellBtn.onClick.AddListener(() => StartWithMode(GameModeType.PureHell));
        titlePureHellBtn.gameObject.SetActive(false);

        titleTimeWarpBtn = CreateModeButton(titleGroup, "TimeWarpBtn",
            new Vector2(0.7f, 0.28f), out titleTimeWarpLabel, "TIME WARP",
            NEON_CYAN);
        titleTimeWarpBtn.onClick.AddListener(() => StartWithMode(GameModeType.TimeWarp));
        titleTimeWarpBtn.gameObject.SetActive(false);

        // Tap to play
#if UNITY_TVOS
        string tapPrompt = "SWIPE TO PLAY";
#elif UNITY_STANDALONE
        string tapPrompt = "PRESS ANY KEY TO PLAY";
#else
        string tapPrompt = "TAP TO PLAY";
#endif
        titleTapText = CreateText(titleGroup, "TitleTap", tapPrompt,
            36, FontStyles.Normal, new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0f));
        SetAnchored(titleTapText.rectTransform, new Vector2(0.5f, 0.12f), new Vector2(600, 55));
        titleTapText.characterSpacing = 8f;

        // First-run tutorial hint
#if UNITY_TVOS
        string hintText = "SWIPE LEFT OR RIGHT\nTO DODGE THE GATES";
#elif UNITY_STANDALONE
        string hintText = "USE A/D, ARROWS, OR CONTROLLER\nTO DODGE THE GATES";
#else
        string hintText = "TAP LEFT OR RIGHT\nTO DODGE THE GATES";
#endif
        titleHintText = CreateText(titleGroup, "TutorialHint", hintText,
            28, FontStyles.Normal, new Color(NEON_GOLD.r, NEON_GOLD.g, NEON_GOLD.b, 0f));
        SetAnchored(titleHintText.rectTransform, new Vector2(0.5f, 0.22f), new Vector2(600, 80));
        titleHintText.characterSpacing = 4f;
        titleHintText.enableWordWrapping = true;
        titleHintText.alignment = TextAlignmentOptions.Center;

        // Icon dock — grouped strip with shared background (hidden on tvOS — no touch)
#if !UNITY_TVOS
        CreateDockStrip(titleGroup, "TitleDockR",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-360, -140), new Vector2(520, 100));

        titleLBBtn = CreateIconButton(titleGroup, "TitleLBBtn",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-540, -140), new Vector2(120, 120),
            "star", NEON_GOLD, out titleLBIcon);
        titleLBBtn.onClick.AddListener(OnLeaderboardTap);

        titleStatsBtn = CreateIconButton(titleGroup, "TitleStatsBtn",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-360, -140), new Vector2(120, 120),
            "bars", NEON_CYAN, out titleStatsIcon);
        titleStatsBtn.onClick.AddListener(OnStatsTap);

        titleSettingsBtn = CreateIconButton(titleGroup, "TitleSettingsBtn",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-180, -140), new Vector2(120, 120),
            "gear", DIM_TEXT, out titleSettingsIcon);
        titleSettingsBtn.onClick.AddListener(OnSettingsTap);
#endif

#if (!UNITY_IOS && !UNITY_TVOS) || UNITY_EDITOR
        CreateDockStrip(titleGroup, "TitleDockL",
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(170, -140), new Vector2(180, 100));

        titleQuitBtn = CreateIconButton(titleGroup, "TitleQuitBtn",
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(170, -140), new Vector2(120, 120),
            "power", DIM_TEXT, out titleQuitIcon);
        titleQuitBtn.onClick.AddListener(OnQuitTap);
#endif

        // Drop shadows on main labels
        ApplyDropShadow(titleText);
        ApplyDropShadow(subtitleText);
        ApplyDropShadow(bestScoreText);
        ApplyDropShadow(titlePureHellLabel);
        ApplyDropShadow(titleTimeWarpLabel);
    }

    void BuildPlayingGroup(Transform parent)
    {
        playingGroup = CreateGroup(parent, "PlayingGroup");

        playingScoreText = CreateText(playingGroup, "Score", "0",
            100, FontStyles.Bold, new Color(1f, 1f, 1f, 0.25f));
        SetAnchored(playingScoreText.rectTransform, new Vector2(0.5f, 0.93f), new Vector2(600, 120));

        playingScoreTextOut = CreateText(playingGroup, "ScoreOut", "",
            100, FontStyles.Bold, new Color(1f, 1f, 1f, 0f));
        SetAnchored(playingScoreTextOut.rectTransform, new Vector2(0.5f, 0.93f), new Vector2(600, 120));

        ApplyDropShadow(playingScoreText);

        // Streak dots — fill up on each gate, flash and reset at STREAK_COUNT
        streakDots = new Image[STREAK_COUNT];
        float dotSize = 10f;
        float dotSpacing = 20f;
        float totalWidth = (STREAK_COUNT - 1) * dotSpacing;
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < STREAK_COUNT; i++)
        {
            Image dot = CreateImage(playingGroup.transform, "Dot" + i,
                new Color(DIM_TEXT.r, DIM_TEXT.g, DIM_TEXT.b, 0.15f));
            if (circleSprite != null) dot.sprite = circleSprite;
            RectTransform drt = dot.rectTransform;
            drt.anchorMin = new Vector2(0.5f, 0.895f);
            drt.anchorMax = new Vector2(0.5f, 0.895f);
            drt.pivot = new Vector2(0.5f, 0.5f);
            drt.sizeDelta = new Vector2(dotSize, dotSize);
            drt.anchoredPosition = new Vector2(startX + i * dotSpacing, 0);
            streakDots[i] = dot;
        }

        // Time Warp: floating popup text for bonuses/penalties
        playingPopupText = CreateText(playingGroup, "Popup", "",
            52, FontStyles.Bold, new Color(1f, 1f, 1f, 0f));
        SetAnchored(playingPopupText.rectTransform, new Vector2(0.5f, 0.85f), new Vector2(300, 80));
        ApplyDropShadow(playingPopupText);

        if (GameConfig.IsBlitz())
            BuildBlitzUpgradeHUD(playingGroup);
    }

    void BuildGameOverGroup(Transform parent)
    {
        gameOverGroup = CreateGroup(parent, "GameOverGroup");

        // Score glow layer — colored halo behind the number
        goScoreGlowText = CreateText(gameOverGroup, "GOScoreGlow", "0",
            230, FontStyles.Bold, new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0f));
        SetAnchored(goScoreGlowText.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(800, 260));

        // Chromatic aberration layers for score (behind main text)
        goScoreYellowText = CreateText(gameOverGroup, "GOScoreYellow", "0",
            220, FontStyles.Bold, new Color(1f, 0.95f, 0.1f, 0f));
        SetAnchored(goScoreYellowText.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(800, 250));

        goScoreMagentaText = CreateText(gameOverGroup, "GOScoreMagenta", "0",
            220, FontStyles.Bold, new Color(1f, 0.15f, 0.55f, 0f));
        SetAnchored(goScoreMagentaText.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(800, 250));

        goScoreCyanText = CreateText(gameOverGroup, "GOScoreCyan", "0",
            220, FontStyles.Bold, new Color(0f, 0.85f, 1f, 0f));
        SetAnchored(goScoreCyanText.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(800, 250));

        // Main score — counts up from 0
        goScoreText = CreateText(gameOverGroup, "GOScore", "0",
            220, FontStyles.Bold, new Color(1f, 1f, 1f, 0f));
        SetAnchored(goScoreText.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(800, 250));

        // "NEW BEST" — gold shimmer
        goNewBestText = CreateText(gameOverGroup, "GONewBest", "NEW BEST",
            38, FontStyles.Bold, new Color(NEON_GOLD.r, NEON_GOLD.g, NEON_GOLD.b, 0f));
        SetAnchored(goNewBestText.rectTransform, new Vector2(0.5f, 0.475f), new Vector2(500, 50));
        goNewBestText.characterSpacing = 10f;

        // Golden glitter particles for NEW BEST celebration
        BuildGlitter(gameOverGroup);

        // Leaderboard rows — score left, date right, centered as a pair
        goLeaderboardTexts = new TMP_Text[LEADERBOARD_SHOW];
        goLeaderboardDateTexts = new TMP_Text[LEADERBOARD_SHOW];
        for (int i = 0; i < LEADERBOARD_SHOW; i++)
        {
            float rowY = 0.38f - i * 0.038f;

            TMP_Text row = CreateText(gameOverGroup, "LB" + i, "",
                28, FontStyles.Normal, new Color(DIM_TEXT.r, DIM_TEXT.g, DIM_TEXT.b, 0f));
            SetAnchored(row.rectTransform, new Vector2(0.42f, rowY), new Vector2(200, 40));
            row.alignment = TextAlignmentOptions.Left;
            row.characterSpacing = 2f;
            goLeaderboardTexts[i] = row;

            TMP_Text dateRow = CreateText(gameOverGroup, "LBDate" + i, "",
                22, FontStyles.Normal, new Color(DIM_TEXT.r, DIM_TEXT.g, DIM_TEXT.b, 0f));
            SetAnchored(dateRow.rectTransform, new Vector2(0.58f, rowY), new Vector2(160, 40));
            dateRow.alignment = TextAlignmentOptions.Right;
            dateRow.characterSpacing = 1f;
            goLeaderboardDateTexts[i] = dateRow;
        }

        // Tap to play again
#if UNITY_TVOS
        string goTapPrompt = "SWIPE TO PLAY";
#elif UNITY_STANDALONE
        string goTapPrompt = "PRESS ANY KEY TO PLAY";
#else
        string goTapPrompt = "TAP TO PLAY";
#endif
        goTapText = CreateText(gameOverGroup, "GOTap", goTapPrompt,
            36, FontStyles.Normal, new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0f));
        SetAnchored(goTapText.rectTransform, new Vector2(0.5f, 0.12f), new Vector2(600, 55));
        goTapText.characterSpacing = 8f;

        // Icon dock — grouped strip with shared background (hidden on tvOS — no touch)
#if !UNITY_TVOS
        CreateDockStrip(gameOverGroup, "GODockR",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-360, -140), new Vector2(520, 100));

        goLBBtn = CreateIconButton(gameOverGroup, "GOLBBtn",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-540, -140), new Vector2(120, 120),
            "star", NEON_GOLD, out goLBIcon);
        goLBBtn.onClick.AddListener(OnLeaderboardTap);

        goStatsBtn = CreateIconButton(gameOverGroup, "GOStatsBtn",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-360, -140), new Vector2(120, 120),
            "bars", NEON_CYAN, out goStatsIcon);
        goStatsBtn.onClick.AddListener(OnStatsTap);

        goSettingsBtn = CreateIconButton(gameOverGroup, "GOSettingsBtn",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-180, -140), new Vector2(120, 120),
            "gear", DIM_TEXT, out goSettingsIcon);
        goSettingsBtn.onClick.AddListener(OnSettingsTap);
#endif

#if (!UNITY_IOS && !UNITY_TVOS) || UNITY_EDITOR
        CreateDockStrip(gameOverGroup, "GODockL",
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(170, -140), new Vector2(180, 100));

        goQuitBtn = CreateIconButton(gameOverGroup, "GOQuitBtn",
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(170, -140), new Vector2(120, 120),
            "power", DIM_TEXT, out goQuitIcon);
        goQuitBtn.onClick.AddListener(OnQuitTap);
#endif

        // Corner bracket accents framing the score
        goCornerBrackets = new RectTransform[4];
        float bracketLen = 35f;
        float bracketThick = 2f;
        float bracketInset = -180f; // From score center
        Color bracketColor = new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0f); // Animated in

        // TL, TR, BL, BR — each is an L-shaped pair (horizontal + vertical bar)
        for (int i = 0; i < 4; i++)
        {
            float sx = (i % 2 == 0) ? -1f : 1f;
            float sy = (i < 2) ? 1f : -1f;

            GameObject bracket = new GameObject("Bracket" + i);
            RectTransform brt = bracket.AddComponent<RectTransform>();
            brt.SetParent(goScoreText.rectTransform.parent, false);
            brt.anchorMin = new Vector2(0.5f, 0.58f);
            brt.anchorMax = new Vector2(0.5f, 0.58f);
            brt.anchoredPosition = new Vector2(sx * Mathf.Abs(bracketInset), sy * 90f);
            brt.sizeDelta = new Vector2(bracketLen, bracketLen);

            // Corner point within parent rect
            float cx = sx > 0 ? 1f : 0f;
            float cy = sy > 0 ? 1f : 0f;

            // Horizontal bar — anchored at corner, extends inward
            GameObject hBar = new GameObject("H");
            RectTransform hrt = hBar.AddComponent<RectTransform>();
            hrt.SetParent(brt, false);
            hrt.anchorMin = hrt.anchorMax = new Vector2(cx, cy);
            hrt.pivot = new Vector2(cx, 0.5f);
            hrt.sizeDelta = new Vector2(bracketLen, bracketThick);
            hrt.anchoredPosition = Vector2.zero;
            Image hImg = hBar.AddComponent<Image>();
            hImg.color = bracketColor;
            hImg.raycastTarget = false;

            // Vertical bar — anchored at corner, extends inward
            GameObject vBar = new GameObject("V");
            RectTransform vrt = vBar.AddComponent<RectTransform>();
            vrt.SetParent(brt, false);
            vrt.anchorMin = vrt.anchorMax = new Vector2(cx, cy);
            vrt.pivot = new Vector2(0.5f, cy);
            vrt.sizeDelta = new Vector2(bracketThick, bracketLen);
            vrt.anchoredPosition = Vector2.zero;
            Image vImg = vBar.AddComponent<Image>();
            vImg.color = bracketColor;
            vImg.raycastTarget = false;

            goCornerBrackets[i] = brt;
        }

        // Thin divider below the score/celebration area, above leaderboard
        GameObject divObj = new GameObject("ScoreDivider");
        RectTransform divRT = divObj.AddComponent<RectTransform>();
        divRT.SetParent(gameOverGroup, false);
        divRT.anchorMin = new Vector2(0.27f, 0.42f);
        divRT.anchorMax = new Vector2(0.67f, 0.42f);
        divRT.pivot = new Vector2(0.5f, 0.5f);
        divRT.sizeDelta = new Vector2(0f, 1f);
        goScoreDivider = divObj.AddComponent<Image>();
        goScoreDivider.color = new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0f);
        goScoreDivider.raycastTarget = false;

        // Drop shadows on main labels
        ApplyDropShadow(goScoreText);
        ApplyDropShadow(goNewBestText);
        ApplyDropShadow(goTapText);
    }

    // ── PROCEDURAL TEXTURES ──────────────────────────────────

    RawImage CreateVignette(Transform parent)
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Pow(Mathf.Clamp01((dist - 0.3f) / 0.7f), 2.5f);
                tex.SetPixel(x, y, new Color(0.02f, 0.01f, 0.05f, alpha));
            }
        }
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;

        GameObject obj = new GameObject("Vignette");
        obj.transform.SetParent(parent, false);
        RawImage img = obj.AddComponent<RawImage>();
        img.texture = tex;
        img.color = new Color(1f, 1f, 1f, 0f);
        img.raycastTarget = false;
        StretchFull(img.rectTransform);
        return img;
    }

    RawImage CreateScanlines(Transform parent)
    {
        // 4-pixel repeating pattern: 2 clear + 2 dark
        Texture2D tex = new Texture2D(1, 4, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0f));
        tex.SetPixel(0, 1, new Color(0f, 0f, 0f, 0f));
        tex.SetPixel(0, 2, new Color(0f, 0f, 0f, 0.15f));
        tex.SetPixel(0, 3, new Color(0f, 0f, 0f, 0.15f));
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;

        GameObject obj = new GameObject("Scanlines");
        obj.transform.SetParent(parent, false);
        RawImage img = obj.AddComponent<RawImage>();
        img.texture = tex;
        img.color = new Color(1f, 1f, 1f, 0f);
        img.raycastTarget = false;
        // Tile to match physical screen pixels
        img.uvRect = new Rect(0, 0, 1, Screen.height / 4f);
        StretchFull(img.rectTransform);
        return img;
    }

    Sprite CreateCircleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(half, half));
                float alpha = 1f - Mathf.Clamp01((dist - half + 2f) / 2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f);
    }

    // ── STATE DISPLAY ────────────────────────────────────────

    void ShowGroup(State s)
    {
        SetGroupActive(splashGroup, s == State.Splash);
        SetGroupActive(titleGroup, s == State.Title);
        SetGroupActive(playingGroup, s == State.Playing);
        SetGroupActive(gameOverGroup, s == State.GameOver);
        CloseSettings();

        // Playing: overlays fade out via titleFadeOutTimer, not cleared instantly
        // Title + GameOver: overlays animate in via their animate methods
        if (s == State.Title)
        {
            blackoutImage.color = new Color(0f, 0f, 0f, 0f);
            overlayImage.color = new Color(0.06f, 0.02f, 0.10f, 0f);
            vignetteImage.color = new Color(1f, 1f, 1f, 0f);
            scanlinesImage.color = new Color(1f, 1f, 1f, 0f);
        }
        if (s == State.Splash)
        {
            blackoutImage.color = new Color(0f, 0f, 0f, 1f);
            scanlinesImage.color = new Color(1f, 1f, 1f, 0f);
            ScheduleSplashGlitches();
        }
    }

    void AnimateState()
    {
        // Death flash — runs across state boundaries
        if (deathFlashTimer >= 0f)
        {
            deathFlashTimer += Time.deltaTime;
            float p = deathFlashTimer / DEATH_FLASH_DURATION;
            if (p >= 1f)
            {
                deathFlashTimer = -1f;
                deathFlashImage.color = new Color(0.7f, 0.85f, 1f, 0f);
            }
            else
            {
                // Sharp attack, fast decay — like a CRT hit
                float flash = p < 0.08f
                    ? Mathf.Clamp01(p / 0.08f) * 0.4f  // snap to 0.4 alpha in ~3 frames
                    : 0.4f * (1f - EaseOutCubic((p - 0.08f) / 0.92f));
                deathFlashImage.color = new Color(0.7f, 0.85f, 1f, flash);
            }
        }

        TickGlitch();

        switch (state)
        {
            case State.Splash: AnimateSplash(); break;
            case State.Title: AnimateTitle(); break;
            case State.Playing: AnimatePlaying(); break;
            case State.Rewinding: AnimateRewinding(); break;
            case State.GameOver: AnimateGameOver(); break;
        }

        // Pause overlay animation (runs on top of playing state)
        AnimatePause();

        // VHS glitch pass — applied after normal animation
        if (IsGlitching())
        {
            if (state == State.Splash)
            {
                ApplyGlitchToText(splashNameText);
                ApplyGlitchToText(splashPresentsText);
                ApplyGlitchToBackdrop();
            }
            else if (state == State.Title)
            {
                ApplyGlitchToText(titleText);
                ApplyGlitchToText(subtitleText);
                ApplyGlitchToText(bestScoreText);
                ApplyGlitchToText(titlePureHellLabel);
                ApplyGlitchToText(titleTimeWarpLabel);
            }
            else if (state == State.GameOver)
            {
                ApplyGlitchToText(goScoreText);
                ApplyGlitchToText(goNewBestText);
                ApplyGlitchToText(goTapText);
                for (int i = 0; i < LEADERBOARD_SHOW; i++)
                {
                    ApplyGlitchToText(goLeaderboardTexts[i]);
                    ApplyGlitchToText(goLeaderboardDateTexts[i]);
                }
            }
            ApplyGlitchToBackdrop();
        }
    }

    // ── SPLASH ───────────────────────────────────────────────

    void AnimateSplash()
    {
        float t = stateTimer;

        // Tunnel background fade in + slow rotation
        float starsFade = Mathf.Clamp01(t / SPLASH_FADE_IN);
        splashStarsImage.color = new Color(1f, 1f, 1f, starsFade);
        splashStarsImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, t * -4f);
        float breathe = 1f + Mathf.Sin(t * 0.6f) * 0.03f;
        splashStarsImage.rectTransform.localScale = Vector3.one * (1.15f + breathe * 0.05f);

        // Keep blackout solid during splash
        blackoutImage.color = new Color(0f, 0f, 0f, 1f);

        // Scanlines
        scanlinesImage.color = new Color(1f, 1f, 1f, starsFade * 0.7f);

        // Name text fade in
        float nameFade = Mathf.Clamp01((t - 0.3f) / 0.6f);
        SetAlpha(splashNameText, nameFade);

        // Chromatic aberration on name
        float chromaAlpha = nameFade * 0.55f;
        float spread = 5f + Mathf.Sin(t * 0.8f) * 3f;

        Vector2 cyanOff = new Vector2(
            Mathf.Cos(t * 0.4f) * spread,
            Mathf.Sin(t * 0.55f) * spread * 0.6f);
        Vector2 magentaOff = new Vector2(
            Mathf.Cos(t * 0.4f + 2.09f) * spread,
            Mathf.Sin(t * 0.55f + 2.09f) * spread * 0.6f);
        Vector2 yellowOff = new Vector2(
            Mathf.Cos(t * 0.4f + 4.19f) * spread,
            Mathf.Sin(t * 0.55f + 4.19f) * spread * 0.6f);

        splashNameCyanText.rectTransform.anchoredPosition = cyanOff;
        splashNameMagentaText.rectTransform.anchoredPosition = magentaOff;
        splashNameYellowText.rectTransform.anchoredPosition = yellowOff;
        SetAlpha(splashNameCyanText, chromaAlpha);
        SetAlpha(splashNameMagentaText, chromaAlpha);
        SetAlpha(splashNameYellowText, chromaAlpha);

        // "PRESENTS" fade in (delayed)
        float presentsFade = Mathf.Clamp01((t - 1.0f) / 0.6f);
        SetAlpha(splashPresentsText, presentsFade * 0.8f);

        // Fade out phase
        float fadeOutStart = SPLASH_HOLD;
        if (t > fadeOutStart)
        {
            float fadeP = Mathf.Clamp01((t - fadeOutStart) / SPLASH_FADE_OUT);
            float fadeAlpha = 1f - EaseOutCubic(fadeP);

            // Fade splash elements
            SetAlpha(splashNameText, nameFade * fadeAlpha);
            SetAlpha(splashNameCyanText, chromaAlpha * fadeAlpha);
            SetAlpha(splashNameMagentaText, chromaAlpha * fadeAlpha);
            SetAlpha(splashNameYellowText, chromaAlpha * fadeAlpha);
            SetAlpha(splashPresentsText, presentsFade * 0.8f * fadeAlpha);
            splashStarsImage.color = new Color(1f, 1f, 1f, starsFade * fadeAlpha);
            scanlinesImage.color = new Color(1f, 1f, 1f, starsFade * 0.7f * fadeAlpha);

            // Fade blackout to reveal torus
            blackoutImage.color = new Color(0f, 0f, 0f, fadeAlpha);

            // Transition to Title when fully faded
            if (fadeP >= 1f)
            {
                state = State.Title;
                stateTimer = 0f;
                ShowGroup(State.Title);
                blackoutImage.color = new Color(0f, 0f, 0f, 0f);
            }
        }
    }

    // ── TITLE ────────────────────────────────────────────────

    void AnimateTitle()
    {
        // Restart skip: fade overlays out without showing title UI
        if (titleFadeOutTimer >= 0f && !titleGroup.gameObject.activeSelf)
        {
            titleFadeOutTimer += Time.deltaTime;
            float fp = Mathf.Clamp01(titleFadeOutTimer / TITLE_FADE_DURATION);
            float fadeAlpha = 1f - EaseOutCubic(fp);
            overlayImage.color = new Color(0.06f, 0.02f, 0.10f, fadeAlpha * 0.9f);
            vignetteImage.color = new Color(1f, 1f, 1f, fadeAlpha * 0.55f);
            scanlinesImage.color = new Color(1f, 1f, 1f, fadeAlpha * 0.9f);
            if (fp >= 1f)
            {
                titleFadeOutTimer = -1f;
                overlayImage.color = new Color(0.06f, 0.02f, 0.10f, 0f);
                vignetteImage.color = new Color(1f, 1f, 1f, 0f);
                scanlinesImage.color = new Color(1f, 1f, 1f, 0f);
            }
            return;
        }

        // Per-character staggered elastic entrance
        AnimateTitleCharacters();

        // ── RETRO BACKDROP: heavy overlay + vignette + scanlines ──
        float backdropFade = Mathf.Clamp01(stateTimer / 1.2f);
        overlayImage.color = new Color(0.06f, 0.02f, 0.10f, backdropFade * 0.9f);
        vignetteImage.color = new Color(1f, 1f, 1f, backdropFade * 0.55f);
        scanlinesImage.color = new Color(1f, 1f, 1f, backdropFade * 0.9f);

        // Chromatic aberration — bold CMYK separation
        float chromaFade = Mathf.Clamp01((stateTimer - 0.4f) / 0.6f);
        float chromaAlpha = chromaFade * 0.65f;

        float t = Time.time;
        float spread = 7f + Mathf.Sin(t * 0.7f) * 3f;

        Vector2 cyanOff = new Vector2(
            Mathf.Cos(t * 0.4f) * spread,
            Mathf.Sin(t * 0.55f) * spread * 0.6f);
        Vector2 magentaOff = new Vector2(
            Mathf.Cos(t * 0.4f + 2.09f) * spread,
            Mathf.Sin(t * 0.55f + 2.09f) * spread * 0.6f);
        Vector2 yellowOff = new Vector2(
            Mathf.Cos(t * 0.4f + 4.19f) * spread,
            Mathf.Sin(t * 0.55f + 4.19f) * spread * 0.6f);

        titleCyanText.rectTransform.anchoredPosition = cyanOff;
        titleMagentaText.rectTransform.anchoredPosition = magentaOff;
        titleYellowText.rectTransform.anchoredPosition = yellowOff;

        SetAlpha(titleCyanText, chromaAlpha);
        SetAlpha(titleMagentaText, chromaAlpha);
        SetAlpha(titleYellowText, chromaAlpha);

        // Subtitle — delayed fade, different parallax speed
        float subFade = Mathf.Clamp01((stateTimer - 0.5f) / 0.8f);
        SetAlpha(subtitleText, subFade * 0.9f);
        float subFloat = Mathf.Sin(Time.time * 0.6f) * 2f;
        subtitleText.rectTransform.anchoredPosition = new Vector2(0, subFloat);

        // Rule lines flanking subtitle — expand outward
        if (titleRuleLeft != null)
        {
            float ruleWidth = Mathf.Lerp(0f, 80f, EaseOutCubic(subFade));
            titleRuleLeft.rectTransform.sizeDelta = new Vector2(ruleWidth, 1f);
            titleRuleRight.rectTransform.sizeDelta = new Vector2(ruleWidth, 1f);
            Color rc = new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, subFade * 0.25f);
            titleRuleLeft.color = rc;
            titleRuleRight.color = rc;
        }

        // Best score with expanding gold line
        if (topScores.Count > 0)
        {
            float bestFade = Mathf.Clamp01((stateTimer - 0.8f) / 0.6f);
            string bestStr = FormatModeScore(topScores[0]);
            bestScoreText.text = "BEST  " + bestStr;
            SetAlpha(bestScoreText, bestFade);

            float lineWidth = Mathf.Lerp(0f, 200f, EaseOutCubic(bestFade));
            bestScoreLine.rectTransform.sizeDelta = new Vector2(lineWidth, 2.5f);
            Color lc = bestScoreLine.color;
            lc.a = bestFade * 0.8f;
            bestScoreLine.color = lc;
        }
        else
        {
            SetAlpha(bestScoreText, 0f);
            Color lc = bestScoreLine.color;
            lc.a = 0f;
            bestScoreLine.color = lc;
        }

        // Tap to play — heartbeat flash
        if (stateTimer > 1.2f)
        {
            float fadeIn = Mathf.Clamp01((stateTimer - 1.2f) / 0.4f);
            float pulse = HeartbeatPulse(Time.time);
            SetAlpha(titleTapText, fadeIn * pulse);
        }
        else
        {
            SetAlpha(titleTapText, 0f);
        }

        // Tutorial hint — steady gold, only on first run
        if (isFirstRun && titleHintText != null)
        {
            float hintFade = Mathf.Clamp01((stateTimer - 1.5f) / 0.5f);
            SetAlpha(titleHintText, hintFade * 0.85f);
        }
        else if (titleHintText != null)
        {
            SetAlpha(titleHintText, 0f);
        }

        // Icon buttons — fade in with best score
        float lbFade = Mathf.Clamp01((stateTimer - 0.8f) / 0.6f);
        SetGlyphAlpha(titleLBIcon, lbFade * 0.75f);
        SetGlyphAlpha(titleStatsIcon, lbFade * 0.75f);
        SetGlyphAlpha(titleSettingsIcon, lbFade * 0.75f);
        SetGlyphAlpha(titleQuitIcon, lbFade * 0.75f);
    }

    void AnimateTitleCharacters()
    {
        titleText.ForceMeshUpdate();
        TMP_TextInfo textInfo = titleText.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            // Staggered entrance — each letter 70ms apart
            float charDelay = i * 0.07f;
            float charTime = stateTimer - charDelay;

            // Alpha: fade in over 400ms
            float charAlpha = Mathf.Clamp01(charTime / 0.4f);

            // Elastic drop from above
            float dropOffset = 0f;
            if (charTime > 0f && charTime < 0.8f)
            {
                float dp = Mathf.Clamp01(charTime / 0.8f);
                dropOffset = (1f - EaseOutElastic(dp)) * 30f;
            }

            // Per-character gentle float (phase offset per letter)
            float floatY = Mathf.Sin(Time.time * 0.8f + i * 0.3f) * 3f;
            float yOff = dropOffset + floatY;

            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            Color32[] colors = textInfo.meshInfo[materialIndex].colors32;
            Vector3[] verts = textInfo.meshInfo[materialIndex].vertices;

            byte alpha = (byte)(Mathf.Clamp01(charAlpha) * 255);
            for (int v = 0; v < 4; v++)
            {
                colors[vertexIndex + v].a = alpha;
                verts[vertexIndex + v].y += yOff;
            }
        }

        // Push modified mesh data
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            titleText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }

    // ── PLAYING ──────────────────────────────────────────────

    void AnimatePlaying()
    {
        // Title fade-out (smooth transition from title screen)
        if (titleFadeOutTimer >= 0f && titleCanvasGroup != null)
        {
            titleFadeOutTimer += Time.deltaTime;
            float fp = Mathf.Clamp01(titleFadeOutTimer / TITLE_FADE_DURATION);
            float fadeAlpha = 1f - EaseOutCubic(fp);
            titleCanvasGroup.alpha = fadeAlpha;

            // Spatial spread — elements rush away as if diving into tunnel
            float spread = EaseOutCubic(fp);
            titleText.rectTransform.anchoredPosition = new Vector2(0f, spread * 200f);
            if (titleMagentaText != null) titleMagentaText.rectTransform.anchoredPosition = new Vector2(0f, spread * 200f);
            if (titleCyanText != null) titleCyanText.rectTransform.anchoredPosition = new Vector2(0f, spread * 200f);
            if (titleYellowText != null) titleYellowText.rectTransform.anchoredPosition = new Vector2(0f, spread * 200f);
            subtitleText.rectTransform.anchoredPosition = new Vector2(0f, spread * 80f);
            bestScoreText.rectTransform.anchoredPosition = new Vector2(0f, -spread * 120f);
            titleTapText.rectTransform.anchoredPosition = new Vector2(0f, -spread * 180f);
            float zoomScale = 1f + spread * 0.3f;
            titleText.rectTransform.localScale = new Vector3(zoomScale, zoomScale, 1f);

            // Fade overlays with title
            overlayImage.color = new Color(0.06f, 0.02f, 0.10f, fadeAlpha * 0.9f);
            vignetteImage.color = new Color(1f, 1f, 1f, fadeAlpha * 0.55f);
            scanlinesImage.color = new Color(1f, 1f, 1f, fadeAlpha * 0.9f);

            if (fp >= 1f)
            {
                titleFadeOutTimer = -1f;
                titleGroup.gameObject.SetActive(false);
                titleCanvasGroup.alpha = 1f;
                // Reset positions for next title show
                titleText.rectTransform.anchoredPosition = Vector2.zero;
                titleText.rectTransform.localScale = Vector3.one;
                if (titleMagentaText != null) titleMagentaText.rectTransform.anchoredPosition = Vector2.zero;
                if (titleCyanText != null) titleCyanText.rectTransform.anchoredPosition = Vector2.zero;
                if (titleYellowText != null) titleYellowText.rectTransform.anchoredPosition = Vector2.zero;
                subtitleText.rectTransform.anchoredPosition = Vector2.zero;
                bestScoreText.rectTransform.anchoredPosition = Vector2.zero;
                titleTapText.rectTransform.anchoredPosition = Vector2.zero;
                overlayImage.color = new Color(0.06f, 0.02f, 0.10f, 0f);
                vignetteImage.color = new Color(1f, 1f, 1f, 0f);
                scanlinesImage.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        // Time Warp: show countdown timer instead of score
        if (GameConfig.IsTimeWarp())
        {
            AnimatePlayingTimeWarp();
            return;
        }

        // Blitz: update upgrade HUD and spark effects
        if (GameConfig.IsBlitz())
        {
            UpdateBlitzUpgradeHUD();
            AnimateOrbSparks();
        }

        if (source == null) return;
        string text = source.text;
        string scoreOnly = text.Contains("\n") ? text.Split('\n')[0] : text;

        // Detect score change
        if (scoreOnly != lastPlayingScore)
        {
            // Capture current color for outgoing text
            Color prevColor = playingScoreText.color;
            playingScoreTextOut.color = new Color(prevColor.r, prevColor.g, prevColor.b, 0f);
            playingScoreTextOut.text = lastPlayingScore;
            playingScoreTextOut.rectTransform.anchoredPosition = Vector2.zero;

            playingScoreText.text = scoreOnly;
            scoreAnimTimer = 0f;
            scorePopTimer = 0f;
            scoreGlowTimer = 0f;
            lastPlayingScore = scoreOnly;

            // Streak: fill a dot, flash and reset when full
            currentStreak++;
            if (currentStreak > STREAK_COUNT)
            {
                streakFlashTimer = 0f;
                currentStreak = 1;
            }
        }

        // Score color: warm shift based on score value
        int scoreVal = 0;
        int.TryParse(scoreOnly, out scoreVal);
        float warmth = Mathf.Clamp01(scoreVal / 50f);
        Color baseScoreColor = Color.Lerp(Color.white, NEON_GOLD, warmth * 0.4f);

        // Cyan flash on increment, decays back to warm base
        if (scoreGlowTimer >= 0f && scoreGlowTimer < SCORE_GLOW_DURATION)
        {
            scoreGlowTimer += Time.deltaTime;
            float gp = Mathf.Clamp01(scoreGlowTimer / SCORE_GLOW_DURATION);
            Color glowColor = Color.Lerp(NEON_CYAN, baseScoreColor, EaseOutCubic(gp));
            playingScoreText.color = new Color(glowColor.r, glowColor.g, glowColor.b,
                playingScoreText.color.a);
        }
        else
        {
            playingScoreText.color = new Color(baseScoreColor.r, baseScoreColor.g,
                baseScoreColor.b, playingScoreText.color.a);
        }

        // Crossfade animation
        if (scoreAnimTimer >= 0f && scoreAnimTimer < SCORE_ANIM_DURATION)
        {
            scoreAnimTimer += Time.deltaTime;
            float p = Mathf.Clamp01(scoreAnimTimer / SCORE_ANIM_DURATION);
            float eased = EaseOutCubic(p);

            float inY = Mathf.Lerp(SCORE_SLIDE_DISTANCE, 0f, eased);
            playingScoreText.rectTransform.anchoredPosition = new Vector2(0, inY);
            SetAlpha(playingScoreText, eased * 0.85f);

            float outY = Mathf.Lerp(0f, -SCORE_SLIDE_DISTANCE, eased);
            playingScoreTextOut.rectTransform.anchoredPosition = new Vector2(0, outY);
            SetAlpha(playingScoreTextOut, (1f - eased) * 0.85f);
        }
        else
        {
            playingScoreText.rectTransform.anchoredPosition = Vector2.zero;
            float alpha = scoreOnly == "0" ? 0.2f : 0.85f;
            SetAlpha(playingScoreText, alpha);
            SetAlpha(playingScoreTextOut, 0f);
        }

        // Scale pop — elastic overshoot
        if (scorePopTimer >= 0f && scorePopTimer < SCORE_POP_DURATION)
        {
            scorePopTimer += Time.deltaTime;
            float p = Mathf.Clamp01(scorePopTimer / SCORE_POP_DURATION);
            float scale = Mathf.Lerp(SCORE_POP_SCALE, 1f, EaseOutBack(p));
            playingScoreText.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            playingScoreText.rectTransform.localScale = Vector3.one;
        }

        // Streak dots
        UpdateStreakDots();
    }

    // ── BLITZ UPGRADE HUD ──────────────────────────────────

    void BuildBlitzUpgradeHUD(RectTransform parent)
    {
        const float SLOT_SIZE = 14f;
        const float SLOT_GAP = 4f;
        const float ROW_GAP = 6f;
        const float LEVEL_GAP = 10f; // extra gap between L1 and L2 slots
        const float MARGIN_X = 30f;
        const float MARGIN_Y = 30f;

        GameObject grp = new GameObject("BlitzUpgradeHUD");
        blitzUpgradeGroup = grp.AddComponent<RectTransform>();
        blitzUpgradeGroup.SetParent(parent, false);
        blitzUpgradeGroup.anchorMin = new Vector2(0f, 1f);
        blitzUpgradeGroup.anchorMax = new Vector2(0f, 1f);
        blitzUpgradeGroup.pivot = new Vector2(0f, 1f);
        blitzUpgradeGroup.anchoredPosition = new Vector2(MARGIN_X, -MARGIN_Y);
        blitzUpgradeGroup.sizeDelta = new Vector2(400f, 120f);

        Color gunColor = new Color(1f, 0.85f, 0.1f);
        Color cadencyColor = new Color(0.2f, 0.7f, 1.0f);
        Color shieldColor = new Color(0.1f, 1.0f, 0.4f);

        blitzGunSlots = CreateSlotRow(blitzUpgradeGroup, 0, 10, gunColor, SLOT_SIZE, SLOT_GAP, ROW_GAP, LEVEL_GAP);
        blitzCadencySlots = CreateSlotRow(blitzUpgradeGroup, 1, 10, cadencyColor, SLOT_SIZE, SLOT_GAP, ROW_GAP, LEVEL_GAP);
        blitzShieldSlots = CreateSlotRow(blitzUpgradeGroup, 2, 5, shieldColor, SLOT_SIZE, SLOT_GAP, ROW_GAP, 0f);
    }

    Image[] CreateSlotRow(RectTransform parent, int rowIndex, int count, Color color,
        float slotSize, float slotGap, float rowGap, float levelGap)
    {
        Image[] slots = new Image[count];
        float y = -(rowIndex * (slotSize + rowGap));

        for (int i = 0; i < count; i++)
        {
            float extraGap = (levelGap > 0f && i >= 5) ? levelGap : 0f;
            float x = i * (slotSize + slotGap) + extraGap;

            Image img = CreateImage(parent, "Slot_" + rowIndex + "_" + i,
                new Color(color.r, color.g, color.b, 0.15f));
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(slotSize, slotSize);
            slots[i] = img;
        }

        return slots;
    }

    void UpdateBlitzUpgradeHUD()
    {
        if (blitzUpgradeGroup == null) return;
        Torus torus = FindAnyObjectByType<Torus>();
        if (torus == null) return;

        int gun = torus.GetGunOrbCount();
        int cadency = torus.GetCadencyOrbCount();
        int shield = torus.GetShieldOrbCount();

        if (gun != blitzLastGunCount)
        {
            blitzLastGunCount = gun;
            UpdateSlotRow(blitzGunSlots, gun, new Color(1f, 0.85f, 0.1f));
        }
        if (cadency != blitzLastCadencyCount)
        {
            blitzLastCadencyCount = cadency;
            UpdateSlotRow(blitzCadencySlots, cadency, new Color(0.2f, 0.7f, 1.0f));
        }
        if (shield != blitzLastShieldCount)
        {
            blitzLastShieldCount = shield;
            UpdateSlotRow(blitzShieldSlots, shield, new Color(0.1f, 1.0f, 0.4f));
        }
    }

    void UpdateSlotRow(Image[] slots, int filledCount, Color color)
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            float alpha = i < filledCount ? 1f : 0.15f;
            slots[i].color = new Color(color.r, color.g, color.b, alpha);

            // Decay pop scale back to 1
            Vector3 s = slots[i].rectTransform.localScale;
            if (s.x > 1.01f)
            {
                float ns = Mathf.Lerp(s.x, 1f, Time.deltaTime * 8f);
                slots[i].rectTransform.localScale = Vector3.one * ns;
            }
            else if (s.x != 1f)
            {
                slots[i].rectTransform.localScale = Vector3.one;
            }
        }
    }

    /// <summary>Spawn a spark that flies from world position to the target HUD slot.</summary>
    public void TriggerOrbSpark(Vector3 worldPos, BlitzOrb.OrbType type, int slotIndex)
    {
        if (sparkImages == null || canvasRT == null) return;

        // Convert world position to canvas local position
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector2 screenPos = cam.WorldToScreenPoint(worldPos);
        Vector2 canvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, screenPos, null, out canvasPos);

        // Find target slot position
        Image[] slots = type == BlitzOrb.OrbType.Gun ? blitzGunSlots
                      : type == BlitzOrb.OrbType.Cadency ? blitzCadencySlots
                      : blitzShieldSlots;
        if (slots == null || slotIndex < 0 || slotIndex >= slots.Length) return;

        Vector2 slotScreenPos = RectTransformUtility.WorldToScreenPoint(
            null, slots[slotIndex].rectTransform.position);
        Vector2 targetPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, slotScreenPos, null, out targetPos);

        // Find a free spark slot
        int s = -1;
        for (int i = 0; i < MAX_SPARKS; i++)
        {
            if (sparkTimers[i] < 0f)
            {
                s = i;
                break;
            }
        }
        if (s < 0) return; // all slots busy

        // Set spark color to match orb type
        Color c = type == BlitzOrb.OrbType.Gun ? new Color(1f, 0.85f, 0.1f)
                : type == BlitzOrb.OrbType.Cadency ? new Color(0.2f, 0.7f, 1.0f)
                : new Color(0.1f, 1.0f, 0.4f);

        sparkImages[s].color = c;
        sparkImages[s].gameObject.SetActive(true);
        sparkImages[s].rectTransform.anchoredPosition = canvasPos;
        sparkFrom[s] = canvasPos;
        sparkTo[s] = targetPos;
        sparkTimers[s] = 0f;
        sparkSlotIndex[s] = slotIndex;
        sparkOrbType[s] = type;
    }

    void AnimateOrbSparks()
    {
        if (sparkImages == null) return;

        for (int i = 0; i < MAX_SPARKS; i++)
        {
            if (sparkTimers[i] < 0f) continue;

            sparkTimers[i] += Time.deltaTime;
            float p = sparkTimers[i] / SPARK_DURATION;

            if (p >= 1f)
            {
                // Arrived — hide spark, light up the slot
                sparkImages[i].gameObject.SetActive(false);
                sparkTimers[i] = -1f;

                Image[] slots = sparkOrbType[i] == BlitzOrb.OrbType.Gun ? blitzGunSlots
                              : sparkOrbType[i] == BlitzOrb.OrbType.Cadency ? blitzCadencySlots
                              : blitzShieldSlots;
                int idx = sparkSlotIndex[i];
                if (slots != null && idx >= 0 && idx < slots.Length)
                {
                    // Pop effect on arrival — scale up briefly
                    slots[idx].rectTransform.localScale = Vector3.one * 1.6f;
                }
                continue;
            }

            // Ease-in-out arc path
            float eased = p < 0.5f ? 2f * p * p : 1f - Mathf.Pow(-2f * p + 2f, 2f) * 0.5f;
            Vector2 pos = Vector2.Lerp(sparkFrom[i], sparkTo[i], eased);
            // Slight upward arc
            float arc = Mathf.Sin(p * Mathf.PI) * 60f;
            pos.y += arc;

            sparkImages[i].rectTransform.anchoredPosition = pos;

            // Size: starts big, shrinks to slot size
            float size = Mathf.Lerp(SPARK_SIZE * 1.5f, SPARK_SIZE * 0.5f, eased);
            sparkImages[i].rectTransform.sizeDelta = new Vector2(size, size);

            // Fade alpha: full brightness, slight fade at end
            float alpha = p < 0.8f ? 1f : Mathf.Lerp(1f, 0.6f, (p - 0.8f) / 0.2f);
            Color sc = sparkImages[i].color;
            sparkImages[i].color = new Color(sc.r, sc.g, sc.b, alpha);
        }
    }

    void AnimatePlayingTimeWarp()
    {
        FrenzyTimer timer = FindAnyObjectByType<FrenzyTimer>();
        if (timer == null) return;

        // Consume popup events from item pickups
        float sign;
        string popupText = timer.ConsumePopup(out sign);
        if (popupText != null)
        {
            playingPopupText.text = popupText;
            popupAnimSign = sign;
            popupAnimTimer = 0f;
        }

        // Animate popup (scale up, drift up, fade out)
        if (popupAnimTimer >= 0f)
        {
            popupAnimTimer += Time.deltaTime;
            float p = popupAnimTimer / POPUP_DURATION;
            if (p >= 1f)
            {
                popupAnimTimer = -1f;
                SetAlpha(playingPopupText, 0f);
            }
            else
            {
                float alpha = 1f - p * p;
                float yDrift = p * 50f;
                float scale = Mathf.Lerp(1.4f, 1f, Mathf.Min(p * 4f, 1f));

                Color c = popupAnimSign > 0
                    ? new Color(0.2f, 1f, 0.4f)
                    : new Color(1f, 0.2f, 0.2f);
                playingPopupText.color = new Color(c.r, c.g, c.b, alpha);
                playingPopupText.rectTransform.anchoredPosition = new Vector2(0, yDrift);
                playingPopupText.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        // Timer countdown display
        float remaining = timer.GetTimeRemaining();
        playingScoreText.text = remaining.ToString("F1");

        // Timer color: white -> yellow (<5s) -> red pulsing (<3s)
        Color timerColor;
        if (timer.IsWarning())
        {
            float pulse = 0.5f + Mathf.Sin(Time.time * 8f) * 0.5f;
            timerColor = Color.Lerp(new Color(1f, 0.15f, 0.1f), new Color(1f, 0.4f, 0.3f), pulse);
        }
        else if (remaining < 5f)
            timerColor = new Color(1f, 0.92f, 0.2f);
        else
            timerColor = Color.white;

        playingScoreText.color = new Color(timerColor.r, timerColor.g, timerColor.b, 0.9f);
        playingScoreText.rectTransform.anchoredPosition = Vector2.zero;

        // Warning scale pulse
        if (timer.IsWarning())
        {
            float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.06f;
            playingScoreText.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
        }
        else
        {
            playingScoreText.rectTransform.localScale = Vector3.one;
        }

        // Hide streak dots and outgoing text in Time Warp
        if (streakDots != null)
            for (int i = 0; i < STREAK_COUNT; i++)
                streakDots[i].color = new Color(0, 0, 0, 0);
        SetAlpha(playingScoreTextOut, 0f);
    }

    void UpdateStreakDots()
    {
        if (streakDots == null) return;

        // Flash timer for completed streak
        if (streakFlashTimer >= 0f)
        {
            streakFlashTimer += Time.deltaTime;
            if (streakFlashTimer > 0.4f)
                streakFlashTimer = -1f;
        }

        for (int i = 0; i < STREAK_COUNT; i++)
        {
            bool filled = i < currentStreak;
            float targetAlpha = filled ? 0.8f : 0.15f;
            Color targetColor = filled ? NEON_CYAN : DIM_TEXT;

            // Full-streak flash: all dots go gold then fade
            if (streakFlashTimer >= 0f)
            {
                float flashP = streakFlashTimer / 0.4f;
                float flashBright = 1f - EaseOutCubic(flashP);
                targetAlpha = Mathf.Lerp(targetAlpha, 1f, flashBright);
                targetColor = Color.Lerp(targetColor, NEON_GOLD, flashBright);
            }

            // Scale pop on the dot that just filled
            float dotScale = 1f;
            if (filled && i == currentStreak - 1
                && scorePopTimer >= 0f && scorePopTimer < SCORE_POP_DURATION)
            {
                float dp = Mathf.Clamp01(scorePopTimer / SCORE_POP_DURATION);
                dotScale = Mathf.Lerp(1.8f, 1f, EaseOutBack(dp));
            }

            streakDots[i].color = new Color(targetColor.r, targetColor.g,
                targetColor.b, targetAlpha);
            streakDots[i].rectTransform.localScale = new Vector3(dotScale, dotScale, 1f);
        }
    }

    // ── REWINDING ────────────────────────────────────────────

    void AnimateRewinding()
    {
        // Clean view during rewind — no overlay
    }

    // ── GAME OVER ────────────────────────────────────────────

    void AnimateGameOver()
    {
        if (goScoreText == null) return;
        float t = stateTimer;

        // Retro backdrop: snap to intense then settle to normal
        float backdropSnap = Mathf.Clamp01(t / 0.08f); // Snap on in ~5 frames
        float backdropSettle = t < 0.08f ? 1f : Mathf.Lerp(1f, 0.7f, Mathf.Clamp01((t - 0.08f) / 0.5f));
        float backdropP = backdropSnap * backdropSettle;
        vignetteImage.color = new Color(1f, 1f, 1f, backdropP * 0.55f);
        overlayImage.color = new Color(0.06f, 0.02f, 0.10f, backdropP * 0.9f);
        scanlinesImage.color = new Color(1f, 1f, 1f, backdropP * 0.9f);

        // Score count-up from 0 to final value
        if (t > 0.3f)
        {
            // Duration scales with score (0.3s min, 1.2s max)
            float countScale = GameConfig.IsTimeWarp() ? 0.005f
                             : GameConfig.IsBlitz() ? 0.002f : 0.04f;
            float countDuration = Mathf.Clamp(goFinalScore * countScale, 0.3f, 1.2f);
            float countP = Mathf.Clamp01((t - 0.3f) / countDuration);
            float countEased = EaseOutCubic(countP);
            int displayScore = Mathf.RoundToInt(goFinalScore * countEased);
            if (displayScore != goLastDisplayScore)
            {
                goLastDisplayScore = displayScore;
                if (mAudio != null) mAudio.PlayCount();
            }
            string displayText = FormatModeScore(displayScore);
            goScoreText.text = displayText;
            goScoreGlowText.text = displayText;

            // Fade in
            float fadeP = Mathf.Clamp01((t - 0.3f) / 0.4f);
            float fadeEased = EaseOutCubic(fadeP);
            SetAlpha(goScoreText, fadeEased);

            // Rank-based color tint
            Color scoreColor = Color.white;
            if (isNewBest)
                scoreColor = Color.Lerp(Color.white, NEON_GOLD, 0.3f);
            else if (goRank >= 2 && goRank <= 3)
                scoreColor = Color.Lerp(Color.white, new Color(0.75f, 0.8f, 0.85f), 0.2f);
            goScoreText.color = new Color(scoreColor.r, scoreColor.g, scoreColor.b,
                goScoreText.color.a);

            // Glow layer: gold for new best, cyan otherwise
            Color glowCol = isNewBest ? NEON_GOLD : NEON_CYAN;
            float glowAlpha = fadeEased * 0.10f;
            if (countP >= 1f) // Subtle breathe after count-up finishes
                glowAlpha += Mathf.Sin(Time.time * 2f) * 0.03f;
            goScoreGlowText.color = new Color(glowCol.r, glowCol.g, glowCol.b, glowAlpha);

            // Slam entrance: drop from above with elastic settle
            float scaleP = Mathf.Clamp01((t - 0.3f) / 0.6f);
            float scale = Mathf.Lerp(1.35f, 1.0f, EaseOutBack(scaleP));
            float slamY = (1f - EaseOutBack(scaleP)) * 80f; // Drop from 80px above
            goScoreText.rectTransform.localScale = new Vector3(scale, scale, 1f);
            goScoreGlowText.rectTransform.localScale = new Vector3(scale, scale, 1f);
            goScoreText.rectTransform.anchoredPosition = new Vector2(0f, slamY);
            goScoreGlowText.rectTransform.anchoredPosition = new Vector2(0f, slamY);

            // Chromatic aberration on score
            if (goScoreCyanText != null)
            {
                goScoreCyanText.text = displayText;
                goScoreMagentaText.text = displayText;
                goScoreYellowText.text = displayText;

                float ct = Time.time;
                float chromaSpread = 4f + Mathf.Sin(ct * 0.8f) * 2f;
                goScoreCyanText.rectTransform.anchoredPosition = new Vector2(
                    Mathf.Cos(ct * 0.5f) * chromaSpread,
                    Mathf.Sin(ct * 0.6f) * chromaSpread * 0.5f);
                goScoreMagentaText.rectTransform.anchoredPosition = new Vector2(
                    Mathf.Cos(ct * 0.5f + 2.09f) * chromaSpread,
                    Mathf.Sin(ct * 0.6f + 2.09f) * chromaSpread * 0.5f);
                goScoreYellowText.rectTransform.anchoredPosition = new Vector2(
                    Mathf.Cos(ct * 0.5f + 4.19f) * chromaSpread,
                    Mathf.Sin(ct * 0.6f + 4.19f) * chromaSpread * 0.5f);

                float chromaAlpha = fadeEased * 0.4f;
                SetAlpha(goScoreCyanText, chromaAlpha);
                SetAlpha(goScoreMagentaText, chromaAlpha);
                SetAlpha(goScoreYellowText, chromaAlpha);

                goScoreCyanText.rectTransform.localScale = new Vector3(scale, scale, 1f);
                goScoreMagentaText.rectTransform.localScale = new Vector3(scale, scale, 1f);
                goScoreYellowText.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }
        }
        else
        {
            SetAlpha(goScoreText, 0f);
            goScoreGlowText.color = new Color(0, 0, 0, 0f);
            if (goScoreCyanText != null)
            {
                SetAlpha(goScoreCyanText, 0f);
                SetAlpha(goScoreMagentaText, 0f);
                SetAlpha(goScoreYellowText, 0f);
            }
        }

        // Celebration label — "NEW BEST" (gold) or "TOP 5" (cyan)
        if ((isNewBest || isTopFive) && t > 1.0f)
        {
            float p = Mathf.Clamp01((t - 1.0f) / 0.4f);
            float eased = EaseOutCubic(p);

            if (isNewBest)
            {
                goNewBestText.text = "NEW BEST";
                float shimmer = 0.8f + Mathf.Sin(Time.time * 3f) * 0.15f
                              + Mathf.Sin(Time.time * 7f) * 0.05f;
                SetAlpha(goNewBestText, eased * shimmer);
                goNewBestText.color = new Color(NEON_GOLD.r, NEON_GOLD.g, NEON_GOLD.b,
                    goNewBestText.color.a);

                // Scale pop on new best
                float popScale = Mathf.Lerp(1.5f, 1.0f, EaseOutBack(p));
                goNewBestText.rectTransform.localScale = new Vector3(popScale, popScale, 1f);

                // Start glitter on first frame the label appears
                if (!glitterActive) StartGlitter();
            }
            else
            {
                goNewBestText.text = "TOP 5";
                float pulse = 0.6f + Mathf.Sin(Time.time * 2f) * 0.1f;
                SetAlpha(goNewBestText, eased * pulse);
                goNewBestText.color = new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b,
                    goNewBestText.color.a);

                float popScale = Mathf.Lerp(1.2f, 1.0f, EaseOutBack(p));
                goNewBestText.rectTransform.localScale = new Vector3(popScale, popScale, 1f);
            }

            // SFX — fire once when label appears
            if (!goNewBestSfxPlayed && p > 0f)
            {
                goNewBestSfxPlayed = true;
                if (mAudio != null) mAudio.PlayNewBest(isNewBest);
            }
        }
        else
        {
            SetAlpha(goNewBestText, 0f);
            goNewBestText.rectTransform.localScale = Vector3.one;
        }

        // Corner brackets — fade in with score, subtle breathe; gold for new best
        if (goCornerBrackets != null)
        {
            float bracketP = Mathf.Clamp01((t - 0.4f) / 0.5f);
            float bracketAlpha = EaseOutCubic(bracketP) * 0.3f;
            if (bracketP >= 1f)
                bracketAlpha += Mathf.Sin(Time.time * 1.2f) * 0.05f;
            Color bracketBase = isNewBest ? NEON_GOLD : NEON_CYAN;
            Color bc = new Color(bracketBase.r, bracketBase.g, bracketBase.b, bracketAlpha);
            foreach (RectTransform brt in goCornerBrackets)
            {
                if (brt == null) continue;
                foreach (Transform child in brt)
                {
                    Image img = child.GetComponent<Image>();
                    if (img != null) img.color = bc;
                }
            }
        }

        // Score-leaderboard divider
        if (goScoreDivider != null)
        {
            float divP = Mathf.Clamp01((t - 0.8f) / 0.4f);
            goScoreDivider.color = new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b,
                EaseOutCubic(divP) * 0.15f);
        }

        // Mini leaderboard — staggered slide-in
        AnimateLeaderboard(t);

        // Icon buttons
        if (t > 0.6f)
        {
            float p = Mathf.Clamp01((t - 0.6f) / 0.4f);
            SetGlyphAlpha(goSettingsIcon, p * 0.65f);
            SetGlyphAlpha(goStatsIcon, p * 0.65f);
            SetGlyphAlpha(goLBIcon, p * 0.65f);
            SetGlyphAlpha(goQuitIcon, p * 0.65f);
        }
        else
        {
            SetGlyphAlpha(goSettingsIcon, 0f);
            SetGlyphAlpha(goStatsIcon, 0f);
            SetGlyphAlpha(goLBIcon, 0f);
            SetGlyphAlpha(goQuitIcon, 0f);
        }

        // Tap to play — heartbeat flash
        if (t > 1.8f)
        {
            float pulse = HeartbeatPulse(Time.time);
            SetAlpha(goTapText, pulse);
        }
        else
        {
            SetAlpha(goTapText, 0f);
        }

        // Golden glitter — loops while on game over screen
        UpdateGlitter();
    }

    void AnimateLeaderboard(float t)
    {
        float lbStart = isNewBest ? 1.5f : 1.2f;

        for (int i = 0; i < LEADERBOARD_SHOW; i++)
        {
            if (i >= topScores.Count)
            {
                SetAlpha(goLeaderboardTexts[i], 0f);
                if (goLeaderboardDateTexts[i] != null) SetAlpha(goLeaderboardDateTexts[i], 0f);
                continue;
            }

            float rowDelay = lbStart + i * 0.08f;
            if (t < rowDelay)
            {
                SetAlpha(goLeaderboardTexts[i], 0f);
                goLeaderboardTexts[i].rectTransform.anchoredPosition = new Vector2(30f, 0f);
                if (goLeaderboardDateTexts[i] != null)
                {
                    SetAlpha(goLeaderboardDateTexts[i], 0f);
                    goLeaderboardDateTexts[i].rectTransform.anchoredPosition = new Vector2(30f, 0f);
                }
                continue;
            }

            float rowP = Mathf.Clamp01((t - rowDelay) / 0.3f);
            float rowAlpha = EaseOutCubic(rowP);

            int score = topScores[i];
            bool isCurrent = (score == goFinalScore && i == goRank - 1);

            string scoreStr = FormatModeScore(score);
            goLeaderboardTexts[i].text = (i + 1) + ".   " + scoreStr;

            // Color hierarchy: new best = gold, current run = cyan, #1 = gold, rest = dim
            Color rowColor;
            float maxAlpha;
            if (isCurrent && isNewBest)
            {
                rowColor = NEON_GOLD;
                maxAlpha = 1.0f;
            }
            else if (isCurrent)
            {
                rowColor = NEON_CYAN;
                maxAlpha = 0.95f;
            }
            else if (i == 0)
            {
                rowColor = NEON_GOLD;
                maxAlpha = 0.7f;
            }
            else
            {
                rowColor = DIM_TEXT;
                maxAlpha = 0.5f;
            }

            goLeaderboardTexts[i].color = new Color(rowColor.r, rowColor.g, rowColor.b,
                rowAlpha * maxAlpha);
            goLeaderboardTexts[i].fontStyle = (isCurrent && isNewBest)
                ? FontStyles.Bold : FontStyles.Normal;

            // Slide in from right
            float slideX = Mathf.Lerp(30f, 0f, EaseOutCubic(rowP));
            goLeaderboardTexts[i].rectTransform.anchoredPosition = new Vector2(slideX, 0f);

            // Date column
            if (goLeaderboardDateTexts[i] != null)
            {
                long ts = i < topTimestamps.Count ? topTimestamps[i] : 0;
                goLeaderboardDateTexts[i].text = FormatTimestamp(ts);
                float dateAlpha = maxAlpha * 0.6f; // Subtler than score
                goLeaderboardDateTexts[i].color = new Color(rowColor.r, rowColor.g, rowColor.b,
                    rowAlpha * dateAlpha);
                goLeaderboardDateTexts[i].rectTransform.anchoredPosition = new Vector2(slideX, 0f);
            }
        }
    }

    // ── NEW BEST GLITTER ─────────────────────────────────────

    void BuildGlitter(RectTransform parent)
    {
        glitterRTs = new RectTransform[GLITTER_COUNT];
        glitterImages = new Image[GLITTER_COUNT];
        glitterVel = new Vector2[GLITTER_COUNT];
        glitterLife = new float[GLITTER_COUNT];
        glitterMaxLife = new float[GLITTER_COUNT];
        glitterRot = new float[GLITTER_COUNT];
        glitterRotSpeed = new float[GLITTER_COUNT];
        glitterSize = new float[GLITTER_COUNT];
        glitterActive = false;

        for (int i = 0; i < GLITTER_COUNT; i++)
        {
            GameObject g = new GameObject("Glitter" + i);
            RectTransform rt = g.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.475f);
            rt.sizeDelta = new Vector2(4f, 4f);

            Image img = g.AddComponent<Image>();
            img.color = new Color(1f, 0.85f, 0.2f, 0f);
            img.raycastTarget = false;

            glitterRTs[i] = rt;
            glitterImages[i] = img;
            glitterLife[i] = -1f;
        }
    }

    void StartGlitter()
    {
        glitterActive = true;
        glitterSpawnTimer = 0f;
        // Initial burst
        for (int i = 0; i < 40; i++)
            SpawnGlitter(i);
    }

    void StopGlitter()
    {
        glitterActive = false;
        for (int i = 0; i < GLITTER_COUNT; i++)
        {
            glitterLife[i] = -1f;
            if (glitterImages[i] != null)
                glitterImages[i].color = new Color(1f, 0.85f, 0.2f, 0f);
        }
    }

    void SpawnGlitter(int i)
    {
        // Emit from NEW BEST text area, spreading left and right
        float side = Random.value < 0.5f ? -1f : 1f;
        float spreadX = Random.Range(80f, 220f) * side;
        float spreadY = Random.Range(-15f, 30f);

        glitterRTs[i].anchoredPosition = new Vector2(
            Random.Range(-100f, 100f),
            Random.Range(-10f, 10f)
        );

        glitterVel[i] = new Vector2(spreadX, spreadY + Random.Range(40f, 120f));
        glitterMaxLife[i] = Random.Range(1.2f, 2.5f);
        glitterLife[i] = glitterMaxLife[i];
        glitterSize[i] = Random.Range(4.5f, 10.5f);
        glitterRot[i] = Random.Range(0f, 360f);
        glitterRotSpeed[i] = Random.Range(-300f, 300f);

        // Gold color with slight variation
        float hueShift = Random.Range(-0.06f, 0.06f);
        glitterImages[i].color = new Color(
            1.0f + hueShift,
            0.78f + Random.Range(-0.1f, 0.15f),
            0.15f + Random.Range(-0.1f, 0.1f),
            0.9f
        );

        glitterRTs[i].sizeDelta = new Vector2(glitterSize[i], glitterSize[i]);
    }

    void UpdateGlitter()
    {
        if (glitterRTs == null) return;

        float dt = Time.deltaTime;
        float gravity = -180f;

        // Continuous spawn — 24 per second for steady stream
        if (glitterActive)
        {
            glitterSpawnTimer += dt;
            float spawnInterval = 1f / 24f;
            while (glitterSpawnTimer >= spawnInterval)
            {
                glitterSpawnTimer -= spawnInterval;
                // Find a dead particle to recycle
                for (int j = 0; j < GLITTER_COUNT; j++)
                {
                    if (glitterLife[j] <= 0f)
                    {
                        SpawnGlitter(j);
                        break;
                    }
                }
            }
        }

        for (int i = 0; i < GLITTER_COUNT; i++)
        {
            if (glitterLife[i] <= 0f)
            {
                if (glitterImages[i] != null)
                    glitterImages[i].color = new Color(glitterImages[i].color.r,
                        glitterImages[i].color.g, glitterImages[i].color.b, 0f);
                continue;
            }

            glitterLife[i] -= dt;
            float lifeT = 1f - (glitterLife[i] / glitterMaxLife[i]);

            // Physics: velocity + gravity
            glitterVel[i].x *= (1f - 1.5f * dt); // Air drag
            glitterVel[i].y += gravity * dt;
            Vector2 pos = glitterRTs[i].anchoredPosition;
            pos += glitterVel[i] * dt;
            glitterRTs[i].anchoredPosition = pos;

            // Rotation — tumble
            glitterRot[i] += glitterRotSpeed[i] * dt;
            glitterRTs[i].localRotation = Quaternion.Euler(0, 0, glitterRot[i]);

            // Scale: slight shrink at end
            float scale = lifeT > 0.7f ? Mathf.Lerp(1f, 0.3f, (lifeT - 0.7f) / 0.3f) : 1f;
            glitterRTs[i].sizeDelta = new Vector2(glitterSize[i] * scale, glitterSize[i] * scale);

            // Alpha: full for most of life, fade at end
            float alpha = 1f;
            if (lifeT < 0.1f) alpha = lifeT / 0.1f; // Fade in
            else if (lifeT > 0.6f) alpha = Mathf.Lerp(1f, 0f, (lifeT - 0.6f) / 0.4f); // Fade out

            // Shimmer — flicker brightness
            float shimmer = 0.7f + 0.3f * Mathf.Sin(Time.time * 15f + i * 2.3f);

            Color c = glitterImages[i].color;
            glitterImages[i].color = new Color(c.r, c.g, c.b, alpha * shimmer * 0.85f);
        }
    }

    // ── HELPERS: UI CREATION ─────────────────────────────────

    TMP_Text CreateText(Transform parent, string name, string content,
        float fontSize, FontStyles style, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) tmp.font = defaultFont;
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;

        return tmp;
    }

    Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    RectTransform CreateGroup(Transform parent, string name)
    {
        GameObject obj = new GameObject(name);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        StretchFull(rt);
        return rt;
    }

    void SetAnchored(RectTransform rt, Vector2 anchor, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
    }

    void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    Button CreateModeButton(RectTransform parent, string name, Vector2 anchor,
        out TMP_Text label, string text, Color color)
    {
        GameObject btnObj = new GameObject(name);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(280, 70);

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.01f); // Near-invisible hit area
        Button btn = btnObj.AddComponent<Button>();

        label = CreateText(rt, "Label", text,
            36, FontStyles.Bold, new Color(color.r, color.g, color.b, 0f));
        StretchFull(label.rectTransform);
        label.characterSpacing = 6f;

        return btn;
    }

    void StartWithMode(GameModeType mode)
    {
        GameConfig.ActiveMode = mode;
        LoadScores(); // Reload for the selected mode's leaderboard
        if (mSphere != null && mSphere.IsWaiting())
            mSphere.StartGame();
    }

    void CreateDockStrip(RectTransform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        GameObject strip = new GameObject(name);
        RectTransform rt = strip.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image bg = strip.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.03f, 0.10f, 0.3f);
        bg.raycastTarget = false;

        // Thin top edge accent
        GameObject edge = new GameObject("Edge");
        RectTransform ert = edge.AddComponent<RectTransform>();
        ert.SetParent(rt, false);
        ert.anchorMin = new Vector2(0.1f, 1f);
        ert.anchorMax = new Vector2(0.9f, 1f);
        ert.pivot = new Vector2(0.5f, 1f);
        ert.sizeDelta = new Vector2(0f, 1f);
        Image edgeImg = edge.AddComponent<Image>();
        edgeImg.color = new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0.15f);
        edgeImg.raycastTarget = false;
    }

    Button CreateIconButton(RectTransform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size,
        string iconType, Color iconColor, out CanvasGroup iconGroup)
    {
        GameObject btnObj = new GameObject(name);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        // Circular dark background
        Image bg = btnObj.AddComponent<Image>();
        if (circleSprite != null) bg.sprite = circleSprite;
        bg.color = new Color(0.08f, 0.05f, 0.12f, 0f);

        // Press feedback
        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.2f, 1.2f, 1.3f, 1f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.85f, 1f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
        btn.transition = Selectable.Transition.ColorTint;

        // Icon container — CanvasGroup for unified alpha fade
        GameObject iconObj = new GameObject("Icon");
        RectTransform iconRT = iconObj.AddComponent<RectTransform>();
        iconRT.SetParent(rt, false);
        iconRT.anchorMin = new Vector2(0.2f, 0.2f);
        iconRT.anchorMax = new Vector2(0.8f, 0.8f);
        iconRT.offsetMin = Vector2.zero;
        iconRT.offsetMax = Vector2.zero;
        iconGroup = iconObj.AddComponent<CanvasGroup>();
        iconGroup.alpha = 0f;
        iconGroup.interactable = false;
        iconGroup.blocksRaycasts = false;

        switch (iconType)
        {
            case "star": BuildStarIcon(iconRT, iconColor); break;
            case "bars": BuildBarsIcon(iconRT, iconColor); break;
            case "gear": BuildGearIcon(iconRT, iconColor); break;
            case "power": BuildPowerIcon(iconRT, iconColor); break;
        }

        return btn;
    }

    void BuildStarIcon(RectTransform parent, Color color)
    {
        // 6-pointed star from 3 overlapping narrow rectangles at 0°/60°/-60°
        float w = 0.22f, h = 0.9f;
        for (int i = 0; i < 3; i++)
        {
            Image bar = CreateIconBar(parent, "Ray" + i, color, w, h);
            bar.rectTransform.localRotation = Quaternion.Euler(0f, 0f, i * 60f);
        }
    }

    void BuildBarsIcon(RectTransform parent, Color color)
    {
        // Three horizontal bars — classic list/stats icon
        float[] yPos = { 0.22f, 0.5f, 0.78f };
        float[] widths = { 0.7f, 0.55f, 0.7f };
        float barH = 0.12f;
        for (int i = 0; i < 3; i++)
        {
            float halfW = widths[i] * 0.5f;
            float halfH = barH * 0.5f;
            Image bar = CreateIconBar(parent, "Bar" + i, color,
                widths[i], barH);
            bar.rectTransform.anchorMin = new Vector2(0.5f - halfW, yPos[i] - halfH);
            bar.rectTransform.anchorMax = new Vector2(0.5f + halfW, yPos[i] + halfH);
            bar.rectTransform.offsetMin = Vector2.zero;
            bar.rectTransform.offsetMax = Vector2.zero;
        }
    }

    void BuildGearIcon(RectTransform parent, Color color)
    {
        // Cross shape + center dot — simple gear/settings silhouette
        CreateIconBar(parent, "H", color, 0.85f, 0.22f); // horizontal
        CreateIconBar(parent, "V", color, 0.22f, 0.85f); // vertical
        // Diagonal cross for 8 spokes
        Image d1 = CreateIconBar(parent, "D1", color, 0.65f, 0.18f);
        d1.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Image d2 = CreateIconBar(parent, "D2", color, 0.65f, 0.18f);
        d2.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);
        // Center dot
        Image dot = CreateIconBar(parent, "Dot", new Color(0.06f, 0.03f, 0.10f), 0.28f, 0.28f);
        if (circleSprite != null) dot.sprite = circleSprite;
    }

    void BuildPowerIcon(RectTransform parent, Color color)
    {
        // X shape — two crossed bars
        Image d1 = CreateIconBar(parent, "X1", color, 0.8f, 0.18f);
        d1.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Image d2 = CreateIconBar(parent, "X2", color, 0.8f, 0.18f);
        d2.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);
    }

    Image CreateIconBar(RectTransform parent, string name, Color color, float wFrac, float hFrac)
    {
        GameObject obj = new GameObject(name);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        // Size relative to parent — use parent's rect
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);
        // Anchor-stretch approach: fraction of parent
        rt.anchorMin = new Vector2(0.5f - wFrac * 0.5f, 0.5f - hFrac * 0.5f);
        rt.anchorMax = new Vector2(0.5f + wFrac * 0.5f, 0.5f + hFrac * 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    void SetGlyphAlpha(CanvasGroup icon, float alpha)
    {
        if (icon == null) return;
        icon.alpha = alpha;

        // Also set the circular background alpha
        Transform p = icon.transform.parent;
        if (p != null)
        {
            Image bg = p.GetComponent<Image>();
            if (bg != null)
                bg.color = new Color(0.08f, 0.05f, 0.12f, alpha * 0.7f);
        }
    }

    // ── PAUSE GROUP ────────────────────────────────────────

    void BuildPauseGroup(Transform parent)
    {
        pauseGroup = CreateGroup(parent, "PauseGroup");
        pauseGroup.gameObject.SetActive(false);

        // Semi-transparent dim overlay
        GameObject dimObj = new GameObject("PauseDim");
        RectTransform dimRT = dimObj.AddComponent<RectTransform>();
        dimRT.SetParent(pauseGroup, false);
        StretchFull(dimRT);
        pauseDimImage = dimObj.AddComponent<Image>();
        pauseDimImage.color = new Color(0f, 0f, 0f, 0.6f);
        pauseDimImage.raycastTarget = false;

        // "PAUSED" text
        pausedText = CreateText(pauseGroup, "PausedLabel", "PAUSED",
            64, FontStyles.Bold, NEON_CYAN);
        SetAnchored(pausedText.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(600, 90));
        pausedText.characterSpacing = 16f;

        // Resume hint
#if UNITY_TVOS
        string resumeHint = "PRESS MENU TO RESUME";
#elif UNITY_STANDALONE
        string resumeHint = "PRESS ESC TO RESUME";
#else
        string resumeHint = "TAP TO RESUME";
#endif
        pauseHintText = CreateText(pauseGroup, "PauseHint", resumeHint,
            28, FontStyles.Normal, DIM_TEXT);
        SetAnchored(pauseHintText.rectTransform, new Vector2(0.5f, 0.42f), new Vector2(600, 50));
        pauseHintText.characterSpacing = 6f;
    }

    // ── SETTINGS PANEL ──────────────────────────────────────

    void BuildSettingsPanel(Transform parent)
    {
        GameObject panelObj = new GameObject("SettingsPanel");
        settingsPanel = panelObj.AddComponent<RectTransform>();
        settingsPanel.SetParent(parent, false);
        StretchFull(settingsPanel);

        // Dim background tap-to-close
        Image dimBg = panelObj.AddComponent<Image>();
        dimBg.color = new Color(0f, 0f, 0f, 0.75f);
        Button closeBg = panelObj.AddComponent<Button>();
        closeBg.onClick.AddListener(CloseSettings);

        // Card — raycast-blocking background
        GameObject card = new GameObject("Card");
        RectTransform cardRT = card.AddComponent<RectTransform>();
        cardRT.SetParent(settingsPanel, false);
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);

        // Taller card on standalone to fit display settings
        bool showDisplay = false;
#if UNITY_STANDALONE && !UNITY_EDITOR
        showDisplay = true;
#endif
        cardRT.sizeDelta = showDisplay ? new Vector2(500, 620) : new Vector2(500, 400);

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(0.06f, 0.03f, 0.10f, 0.96f);
        cardBg.raycastTarget = true;

        // ── Cyan border frame ──
        CreateSettingsBorderEdge(cardRT, "BorderTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, 2f));
        CreateSettingsBorderEdge(cardRT, "BorderBot", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(0f, 2f));
        CreateSettingsBorderEdge(cardRT, "BorderL", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(2f, 0f));
        CreateSettingsBorderEdge(cardRT, "BorderR", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(2f, 0f));

        // Title
        TMP_Text title = CreateText(cardRT, "Title", "SETTINGS",
            36, FontStyles.Bold, NEON_CYAN);
        SetAnchored(title.rectTransform, new Vector2(0.5f, showDisplay ? 0.92f : 0.85f), new Vector2(400, 50));
        title.characterSpacing = 8f;
        title.raycastTarget = false;

        // Title underline accent
        CreateSettingsDivider(cardRT, showDisplay ? 0.87f : 0.78f, NEON_CYAN, 0.25f);

        if (showDisplay)
        {
            // ── AUDIO SECTION ──
            TMP_Text audioHeader = CreateText(cardRT, "AudioHeader", "AUDIO",
                20, FontStyles.Bold, new Color(0.45f, 0.48f, 0.55f));
            SetAnchored(audioHeader.rectTransform, new Vector2(0.5f, 0.82f), new Vector2(400, 30));
            audioHeader.characterSpacing = 6f;
            audioHeader.raycastTarget = false;

            Button musicBtn = CreateSettingsToggle(cardRT, "MusicBtn",
                new Vector2(0.5f, 0.73f), out settingsMusicLabel);
            musicBtn.onClick.AddListener(ToggleMusic);

            Button soundBtn = CreateSettingsToggle(cardRT, "SoundBtn",
                new Vector2(0.5f, 0.63f), out settingsSoundLabel);
            soundBtn.onClick.AddListener(ToggleSound);

            // Section divider between Audio and Display
            CreateSettingsDivider(cardRT, 0.565f, DIM_TEXT, 0.08f);

            // ── DISPLAY SECTION ──
            TMP_Text displayHeader = CreateText(cardRT, "DisplayHeader", "DISPLAY",
                20, FontStyles.Bold, new Color(0.45f, 0.48f, 0.55f));
            SetAnchored(displayHeader.rectTransform, new Vector2(0.5f, 0.52f), new Vector2(400, 30));
            displayHeader.characterSpacing = 6f;
            displayHeader.raycastTarget = false;

            Button resBtn = CreateSettingsToggle(cardRT, "ResBtn",
                new Vector2(0.5f, 0.43f), out settingsResLabel);
            resBtn.onClick.AddListener(OnCycleResolution);

            Button fullBtn = CreateSettingsToggle(cardRT, "FullBtn",
                new Vector2(0.5f, 0.33f), out settingsFullscreenLabel);
            fullBtn.onClick.AddListener(OnToggleFullscreen);

            Button vsyncBtn = CreateSettingsToggle(cardRT, "VSyncBtn",
                new Vector2(0.5f, 0.23f), out settingsVSyncLabel);
            vsyncBtn.onClick.AddListener(OnToggleVSync);

            // Theme divider + selector
            CreateSettingsDivider(cardRT, 0.175f, DIM_TEXT, 0.08f);
            CreateThemeSelector(cardRT, new Vector2(0.5f, 0.12f));
        }
        else
        {
            // Mobile layout — audio + theme
            Button musicBtn = CreateSettingsToggle(cardRT, "MusicBtn",
                new Vector2(0.5f, 0.65f), out settingsMusicLabel);
            musicBtn.onClick.AddListener(ToggleMusic);

            Button soundBtn = CreateSettingsToggle(cardRT, "SoundBtn",
                new Vector2(0.5f, 0.48f), out settingsSoundLabel);
            soundBtn.onClick.AddListener(ToggleSound);

            CreateSettingsDivider(cardRT, 0.375f, DIM_TEXT, 0.08f);
            CreateThemeSelector(cardRT, new Vector2(0.5f, 0.28f));
        }

        // Close label
        string closeHint = showDisplay ? "CLICK OUTSIDE TO CLOSE" : "TAP OUTSIDE TO CLOSE";
#if UNITY_TVOS
        closeHint = "PRESS MENU TO CLOSE";
#endif
        TMP_Text closeLabel = CreateText(cardRT, "Close", closeHint,
            16, FontStyles.Normal, new Color(DIM_TEXT.r, DIM_TEXT.g, DIM_TEXT.b, 0.5f));
        SetAnchored(closeLabel.rectTransform, new Vector2(0.5f, showDisplay ? 0.07f : 0.1f), new Vector2(400, 30));
        closeLabel.characterSpacing = 3f;
        closeLabel.raycastTarget = false;
    }

    void CreateSettingsBorderEdge(RectTransform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 thickness)
    {
        GameObject obj = new GameObject(name);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = thickness;
        Image img = obj.AddComponent<Image>();
        img.color = new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0.35f);
        img.raycastTarget = false;
    }

    void CreateSettingsDivider(RectTransform parent, float yAnchor, Color color, float alpha)
    {
        GameObject obj = new GameObject("Divider");
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.1f, yAnchor);
        rt.anchorMax = new Vector2(0.9f, yAnchor);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0f, 1f);
        Image img = obj.AddComponent<Image>();
        img.color = new Color(color.r, color.g, color.b, alpha);
        img.raycastTarget = false;
    }

    Button CreateSettingsToggle(RectTransform parent, string name, Vector2 anchor, out TMP_Text label)
    {
        GameObject btnObj = new GameObject(name);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(400, 55);

        Image bg = btnObj.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.04f);
        bg.raycastTarget = true;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.10f);
        cb.pressedColor = new Color(1f, 1f, 1f, 0.18f);
        btn.colors = cb;

        // Left accent bar — indicates on/off state, colored in RefreshSettingsLabels
        GameObject accent = new GameObject("Accent");
        RectTransform accentRT = accent.AddComponent<RectTransform>();
        accentRT.SetParent(rt, false);
        accentRT.anchorMin = new Vector2(0f, 0.15f);
        accentRT.anchorMax = new Vector2(0f, 0.85f);
        accentRT.pivot = new Vector2(0f, 0.5f);
        accentRT.sizeDelta = new Vector2(3f, 0f);
        Image accentImg = accent.AddComponent<Image>();
        accentImg.color = NEON_CYAN;
        accentImg.raycastTarget = false;

        label = CreateText(rt, "Label", "",
            26, FontStyles.Normal, Color.white);
        StretchFull(label.rectTransform);
        label.margin = new Vector4(16f, 0f, 0f, 0f);
        label.raycastTarget = false;

        return btn;
    }

    void OnSettingsTap()
    {
        if (settingsPanel == null)
            BuildSettingsPanel(canvas.transform);
        RefreshSettingsLabels();
        settingsPanel.gameObject.SetActive(true);
    }

    public bool IsSettingsOpen()
    {
        return settingsPanel != null && settingsPanel.gameObject.activeSelf;
    }

    void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.gameObject.SetActive(false);
    }

    // ── STATS PANEL ─────────────────────────────────────────

    void OnStatsTap()
    {
        if (statsPanel == null)
            BuildStatsPanel(canvas.transform);
        RefreshStatsLabels();
        statsPanel.gameObject.SetActive(true);
    }

    public bool IsStatsOpen()
    {
        return statsPanel != null && statsPanel.gameObject.activeSelf;
    }

    void CloseStats()
    {
        if (statsPanel != null)
            statsPanel.gameObject.SetActive(false);
    }

    void BuildStatsPanel(Transform parent)
    {
        GameObject panelObj = new GameObject("StatsPanel");
        statsPanel = panelObj.AddComponent<RectTransform>();
        statsPanel.SetParent(parent, false);
        StretchFull(statsPanel);

        // Dim background tap-to-close
        Image dimBg = panelObj.AddComponent<Image>();
        dimBg.color = new Color(0f, 0f, 0f, 0.75f);
        Button closeBg = panelObj.AddComponent<Button>();
        closeBg.onClick.AddListener(CloseStats);

        // Card
        GameObject card = new GameObject("Card");
        RectTransform cardRT = card.AddComponent<RectTransform>();
        cardRT.SetParent(statsPanel, false);
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(500, 480);

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(0.06f, 0.03f, 0.10f, 0.96f);
        cardBg.raycastTarget = true;

        // Cyan border frame
        CreateSettingsBorderEdge(cardRT, "BorderTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, 2f));
        CreateSettingsBorderEdge(cardRT, "BorderBot", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(0f, 2f));
        CreateSettingsBorderEdge(cardRT, "BorderL", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(2f, 0f));
        CreateSettingsBorderEdge(cardRT, "BorderR", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(2f, 0f));

        // Title
        TMP_Text title = CreateText(cardRT, "Title", "STATISTICS",
            36, FontStyles.Bold, NEON_CYAN);
        SetAnchored(title.rectTransform, new Vector2(0.5f, 0.90f), new Vector2(400, 50));
        title.characterSpacing = 8f;
        title.raycastTarget = false;

        CreateSettingsDivider(cardRT, 0.84f, NEON_CYAN, 0.25f);

        // Stat rows — label left, value right
        statsRunsLabel = CreateStatsRow(cardRT, "Runs", "TOTAL RUNS", 0.72f);
        statsTapsLabel = CreateStatsRow(cardRT, "Taps", "TOTAL TAPS", 0.60f);
        statsBestLabel = CreateStatsRow(cardRT, "Best", "BEST SCORE", 0.48f);
        statsAvgLabel = CreateStatsRow(cardRT, "Avg", "AVG SCORE", 0.36f);
        statsGatesLabel = CreateStatsRow(cardRT, "Gates", "TOTAL GATES", 0.24f);

        // Close hint
        string closeHint = "TAP OUTSIDE TO CLOSE";
#if UNITY_STANDALONE && !UNITY_EDITOR
        closeHint = "CLICK OUTSIDE TO CLOSE";
#elif UNITY_TVOS
        closeHint = "PRESS MENU TO CLOSE";
#endif
        TMP_Text closeLabel = CreateText(cardRT, "Close", closeHint,
            16, FontStyles.Normal, new Color(DIM_TEXT.r, DIM_TEXT.g, DIM_TEXT.b, 0.5f));
        SetAnchored(closeLabel.rectTransform, new Vector2(0.5f, 0.08f), new Vector2(400, 30));
        closeLabel.characterSpacing = 3f;
        closeLabel.raycastTarget = false;
    }

    TMP_Text CreateStatsRow(RectTransform parent, string name, string label, float yAnchor)
    {
        // Label on the left
        TMP_Text lbl = CreateText(parent, name + "Label", label,
            22, FontStyles.Normal, DIM_TEXT);
        RectTransform lblRT = lbl.rectTransform;
        lblRT.anchorMin = new Vector2(0.1f, yAnchor);
        lblRT.anchorMax = new Vector2(0.5f, yAnchor);
        lblRT.pivot = new Vector2(0f, 0.5f);
        lblRT.sizeDelta = new Vector2(0f, 35);
        lbl.alignment = TextAlignmentOptions.Left;
        lbl.characterSpacing = 4f;
        lbl.raycastTarget = false;

        // Value on the right — neon colored
        TMP_Text val = CreateText(parent, name + "Value", "0",
            26, FontStyles.Bold, new Color(1f, 1f, 1f, 0.9f));
        RectTransform valRT = val.rectTransform;
        valRT.anchorMin = new Vector2(0.5f, yAnchor);
        valRT.anchorMax = new Vector2(0.9f, yAnchor);
        valRT.pivot = new Vector2(1f, 0.5f);
        valRT.sizeDelta = new Vector2(0f, 35);
        val.alignment = TextAlignmentOptions.Right;
        val.raycastTarget = false;

        // Subtle divider below
        CreateSettingsDivider(parent, yAnchor - 0.055f, DIM_TEXT, 0.06f);

        return val;
    }

    void RefreshStatsLabels()
    {
        int runs = Sphere.GetTotalRuns();
        int taps = Sphere.GetTotalTaps();
        int best = topScores.Count > 0 ? topScores[0] : 0;
        int totalScore = 0;
        for (int i = 0; i < topScores.Count; i++)
            totalScore += topScores[i];
        int avg = topScores.Count > 0 ? totalScore / topScores.Count : 0;

        // Total gates = sum of all scores (each gate = 1 point in Pure Hell)
        int gates = totalScore;

        if (statsRunsLabel != null) statsRunsLabel.text = runs.ToString("N0");
        if (statsTapsLabel != null) statsTapsLabel.text = taps.ToString("N0");
        if (statsBestLabel != null) statsBestLabel.text = best.ToString("N0");
        if (statsAvgLabel != null) statsAvgLabel.text = avg.ToString("N0");
        if (statsGatesLabel != null) statsGatesLabel.text = gates.ToString("N0");
    }

    void PauseGame()
    {
        if (isPaused || state != State.Playing) return;
        isPaused = true;
        pauseAnimTimer = 0f;

        // Freeze shaders, physics timers (Time.deltaTime → 0, _Time.y stops)
        Time.timeScale = 0f;

        // Freeze torus manual rotation (its Update uses constant step, not deltaTime)
        Torus torus = FindAnyObjectByType<Torus>();
        if (torus != null) torus.SetPaused(true);

        if (pauseGroup != null)
        {
            pauseGroup.gameObject.SetActive(true);
            // Start invisible — animation fills it in
            if (pauseDimImage != null) pauseDimImage.color = new Color(0f, 0f, 0f, 0f);
            if (pausedText != null) SetAlpha(pausedText, 0f);
            if (pauseHintText != null) SetAlpha(pauseHintText, 0f);
        }
    }

    void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;
        pauseAnimTimer = -1f;

        // Unfreeze everything
        Time.timeScale = 1f;

        Torus torus = FindAnyObjectByType<Torus>();
        if (torus != null) torus.SetPaused(false);

        if (pauseGroup != null) pauseGroup.gameObject.SetActive(false);
    }

    void AnimatePause()
    {
        if (pauseAnimTimer < 0f || pauseGroup == null) return;
        pauseAnimTimer += Time.unscaledDeltaTime;

        float p = Mathf.Clamp01(pauseAnimTimer / PAUSE_ANIM_DURATION);
        float eased = EaseOutCubic(p);

        // Dim overlay fades in — high alpha to hide track shader sparks
        if (pauseDimImage != null)
            pauseDimImage.color = new Color(0f, 0f, 0f, eased * 0.85f);

        // "PAUSED" — scale down from 1.4x with letter spacing expanding
        if (pausedText != null)
        {
            SetAlpha(pausedText, eased);
            float scale = Mathf.Lerp(1.4f, 1.0f, eased);
            pausedText.rectTransform.localScale = new Vector3(scale, scale, 1f);
            pausedText.characterSpacing = Mathf.Lerp(40f, 16f, eased);

            // Subtle breathe pulse after entrance completes
            if (p >= 1f)
            {
                float breathe = 1f + Mathf.Sin(Time.unscaledTime * 1.5f) * 0.02f;
                pausedText.rectTransform.localScale = new Vector3(breathe, breathe, 1f);
            }
        }

        // Hint text — delayed fade in
        if (pauseHintText != null)
        {
            float hintP = Mathf.Clamp01((pauseAnimTimer - 0.2f) / 0.3f);
            SetAlpha(pauseHintText, EaseOutCubic(hintP) * 0.7f);
        }
    }

    void ToggleMusic()
    {
        if (mAudio == null) return;
        mAudio.SetMusicMuted(!mAudio.IsMusicMuted());
        RefreshSettingsLabels();
    }

    void ToggleSound()
    {
        if (mAudio == null) return;
        mAudio.SetSoundMuted(!mAudio.IsSoundMuted());
        RefreshSettingsLabels();
    }

    void OnCycleResolution()
    {
        if (DisplaySettings.Instance == null) return;
        DisplaySettings.Instance.CycleResolution();
        RefreshSettingsLabels();
    }

    void OnToggleFullscreen()
    {
        if (DisplaySettings.Instance == null) return;
        DisplaySettings.Instance.ToggleFullscreen();
        RefreshSettingsLabels();
    }

    void OnToggleVSync()
    {
        if (DisplaySettings.Instance == null) return;
        DisplaySettings.Instance.ToggleVSync();
        RefreshSettingsLabels();
    }

    void OnThemeNext()
    {
        ThemeData[] themes = ThemeData.All();
        int current = ThemeData.LoadSavedIndex();
        int next = (current + 1) % themes.Length;
        ApplyThemeLive(next, themes[next]);
    }

    void OnThemePrev()
    {
        ThemeData[] themes = ThemeData.All();
        int current = ThemeData.LoadSavedIndex();
        int prev = (current - 1 + themes.Length) % themes.Length;
        ApplyThemeLive(prev, themes[prev]);
    }

    void ApplyThemeLive(int index, ThemeData theme)
    {
        ThemeData.SaveIndex(index);

        SceneSetup setup = FindAnyObjectByType<SceneSetup>();
        if (setup != null)
            setup.ApplyThemeLive(theme);

        RefreshSettingsLabels();
    }

    void CreateThemeSelector(RectTransform parent, Vector2 anchor)
    {
        // Container — same size as a normal toggle row
        GameObject container = new GameObject("ThemeSelector");
        RectTransform crt = container.AddComponent<RectTransform>();
        crt.SetParent(parent, false);
        crt.anchorMin = anchor;
        crt.anchorMax = anchor;
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(400, 55);

        // Background — matches toggle style
        Image bg = container.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.04f);
        bg.raycastTarget = false;

        // Left accent bar
        GameObject accent = new GameObject("Accent");
        RectTransform accentRT = accent.AddComponent<RectTransform>();
        accentRT.SetParent(crt, false);
        accentRT.anchorMin = new Vector2(0f, 0.15f);
        accentRT.anchorMax = new Vector2(0f, 0.85f);
        accentRT.pivot = new Vector2(0f, 0.5f);
        accentRT.sizeDelta = new Vector2(3f, 0f);
        Image accentImg = accent.AddComponent<Image>();
        accentImg.color = NEON_CYAN;
        accentImg.raycastTarget = false;

        // Left arrow button "<"
        GameObject leftObj = new GameObject("ThemePrev");
        RectTransform leftRT = leftObj.AddComponent<RectTransform>();
        leftRT.SetParent(crt, false);
        leftRT.anchorMin = new Vector2(0f, 0f);
        leftRT.anchorMax = new Vector2(0f, 1f);
        leftRT.pivot = new Vector2(0f, 0.5f);
        leftRT.anchoredPosition = new Vector2(8f, 0f);
        leftRT.sizeDelta = new Vector2(55, 0f);

        Image leftBg = leftObj.AddComponent<Image>();
        leftBg.color = new Color(1f, 1f, 1f, 0f);
        leftBg.raycastTarget = true;

        Button leftBtn = leftObj.AddComponent<Button>();
        ColorBlock lcb = leftBtn.colors;
        lcb.normalColor = Color.white;
        lcb.highlightedColor = new Color(1f, 1f, 1f, 0.10f);
        lcb.pressedColor = new Color(1f, 1f, 1f, 0.18f);
        leftBtn.colors = lcb;
        leftBtn.onClick.AddListener(OnThemePrev);

        TMP_Text leftLabel = CreateText(leftRT, "Arrow", "<",
            28, FontStyles.Bold, NEON_CYAN);
        StretchFull(leftLabel.rectTransform);

        // Right arrow button ">"
        GameObject rightObj = new GameObject("ThemeNext");
        RectTransform rightRT = rightObj.AddComponent<RectTransform>();
        rightRT.SetParent(crt, false);
        rightRT.anchorMin = new Vector2(1f, 0f);
        rightRT.anchorMax = new Vector2(1f, 1f);
        rightRT.pivot = new Vector2(1f, 0.5f);
        rightRT.anchoredPosition = new Vector2(-8f, 0f);
        rightRT.sizeDelta = new Vector2(55, 0f);

        Image rightBg = rightObj.AddComponent<Image>();
        rightBg.color = new Color(1f, 1f, 1f, 0f);
        rightBg.raycastTarget = true;

        Button rightBtn = rightObj.AddComponent<Button>();
        ColorBlock rcb = rightBtn.colors;
        rcb.normalColor = Color.white;
        rcb.highlightedColor = new Color(1f, 1f, 1f, 0.10f);
        rcb.pressedColor = new Color(1f, 1f, 1f, 0.18f);
        rightBtn.colors = rcb;
        rightBtn.onClick.AddListener(OnThemeNext);

        TMP_Text rightLabel = CreateText(rightRT, "Arrow", ">",
            28, FontStyles.Bold, NEON_CYAN);
        StretchFull(rightLabel.rectTransform);

        // Center label — theme name
        settingsThemeLabel = CreateText(crt, "ThemeLabel", "",
            24, FontStyles.Normal, Color.white);
        StretchFull(settingsThemeLabel.rectTransform);
    }

    void RefreshSettingsLabels()
    {
        if (mAudio == null) return;

        bool musicOff = mAudio.IsMusicMuted();
        bool soundOff = mAudio.IsSoundMuted();

        if (settingsMusicLabel != null)
        {
            settingsMusicLabel.text = musicOff ? "MUSIC   OFF" : "MUSIC   ON";
            StyleSettingsToggle(settingsMusicLabel, !musicOff);
        }
        if (settingsSoundLabel != null)
        {
            settingsSoundLabel.text = soundOff ? "SOUND   OFF" : "SOUND   ON";
            StyleSettingsToggle(settingsSoundLabel, !soundOff);
        }

        // Display settings (standalone only)
        if (DisplaySettings.Instance != null)
        {
            if (settingsResLabel != null)
            {
                settingsResLabel.text = "RES   " + DisplaySettings.Instance.GetResolutionLabel();
                StyleSettingsToggle(settingsResLabel, true); // Always "on" style
            }
            if (settingsFullscreenLabel != null)
            {
                bool fs = DisplaySettings.Instance.IsFullscreen();
                settingsFullscreenLabel.text = fs ? "FULLSCREEN   ON" : "FULLSCREEN   OFF";
                StyleSettingsToggle(settingsFullscreenLabel, fs);
            }
            if (settingsVSyncLabel != null)
            {
                bool vs = DisplaySettings.Instance.IsVSync();
                settingsVSyncLabel.text = vs ? "VSYNC   ON" : "VSYNC   OFF";
                StyleSettingsToggle(settingsVSyncLabel, vs);
            }
        }

        if (settingsThemeLabel != null)
        {
            string themeName = SceneSetup.activeTheme != null ? SceneSetup.activeTheme.name : "NEON VOID";
            settingsThemeLabel.text = themeName;
            settingsThemeLabel.color = Color.white;
        }
    }

    void StyleSettingsToggle(TMP_Text label, bool isOn)
    {
        // Text color
        label.color = isOn ? Color.white : new Color(DIM_TEXT.r, DIM_TEXT.g, DIM_TEXT.b, 0.6f);

        // Left accent bar color — find Accent child in button parent
        Transform accent = label.transform.parent.Find("Accent");
        if (accent != null)
        {
            Image accentImg = accent.GetComponent<Image>();
            if (accentImg != null)
                accentImg.color = isOn ? NEON_CYAN : new Color(DIM_TEXT.r, DIM_TEXT.g, DIM_TEXT.b, 0.15f);
        }

        // Button background tint
        Image btnBg = label.transform.parent.GetComponent<Image>();
        if (btnBg != null)
            btnBg.color = isOn ? new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0.06f)
                               : new Color(1f, 1f, 1f, 0.02f);
    }

    void OnQuitTap()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void OnLeaderboardTap()
    {
        if (PlatformManager.Instance != null)
            PlatformManager.Instance.ShowLeaderboard();
    }

    void SetGroupActive(RectTransform group, bool active)
    {
        if (group != null)
            group.gameObject.SetActive(active);
    }

    void SetAlpha(TMP_Text text, float alpha)
    {
        if (text == null) return;
        Color c = text.color;
        c.a = alpha;
        text.color = c;
    }

    // ── LEADERBOARD PERSISTENCE ──────────────────────────────

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
    }

    // ── VHS GLITCH ENGINE ─────────────────────────────────────

    void ScheduleSplashGlitches()
    {
        // 1-3 guaranteed glitch bursts during splash (after name appears, before fade out)
        int count = Random.Range(1, 4); // 1, 2, or 3
        splashGlitchTimes = new float[count];
        splashGlitchNext = 0;
        for (int i = 0; i < count; i++)
            splashGlitchTimes[i] = Random.Range(1.2f, SPLASH_HOLD - 0.5f);
        // Sort so we can consume them in order
        System.Array.Sort(splashGlitchTimes);
    }

    void TickGlitch()
    {
        // Only glitch during Title and GameOver (UI-heavy states)
        if (state != State.Splash && state != State.Title && state != State.GameOver) return;

        if (glitchBurstTimer >= 0f)
        {
            // Active burst
            glitchBurstTimer += Time.deltaTime;
            if (glitchBurstTimer >= glitchBurstDuration)
                glitchBurstTimer = -1f;
        }
        else if (state == State.Splash && splashGlitchTimes != null
                 && splashGlitchNext < splashGlitchTimes.Length
                 && stateTimer >= splashGlitchTimes[splashGlitchNext]
                 && stateTimer < SPLASH_HOLD)
        {
            // Fire scheduled splash glitch
            splashGlitchNext++;
            glitchBurstTimer = 0f;
            glitchBurstDuration = Random.Range(GLITCH_MIN_DURATION, GLITCH_MAX_DURATION);
            glitchIntensity = Random.Range(0.5f, 1.0f);
            glitchSeed = Random.Range(0f, 1000f);
            if (mAudio != null) mAudio.PlayGlitch();
        }
        else if (state != State.Splash && stateTimer > 1.5f)
        {
            // Normal random countdown for Title/GameOver (wait for UI to fade in first)
            glitchTimer -= Time.deltaTime;
            if (glitchTimer <= 0f)
            {
                glitchBurstTimer = 0f;
                glitchBurstDuration = Random.Range(GLITCH_MIN_DURATION, GLITCH_MAX_DURATION);
                glitchIntensity = Random.Range(0.3f, 1.0f);
                glitchSeed = Random.Range(0f, 1000f);
                glitchTimer = Random.Range(GLITCH_MIN_INTERVAL, GLITCH_MAX_INTERVAL);
                if (mAudio != null) mAudio.PlayGlitch();
            }
        }
    }

    bool IsGlitching()
    {
        return glitchBurstTimer >= 0f;
    }

    // Per-character horizontal tear + color corruption on a TMP_Text
    void ApplyGlitchToText(TMP_Text text)
    {
        if (text == null || !text.gameObject.activeInHierarchy) return;
        if (text.color.a < 0.01f) return;

        text.ForceMeshUpdate();
        TMP_TextInfo textInfo = text.textInfo;
        if (textInfo.characterCount == 0) return;

        float burstP = glitchBurstTimer / glitchBurstDuration;
        // Intensity envelope: sharp in, sharp out
        float envelope = Mathf.Sin(burstP * Mathf.PI);
        float strength = glitchIntensity * envelope;

        // Pick a "tear line" — a random character index where the split happens
        float tearHash = Mathf.PerlinNoise(glitchSeed, burstP * 5f);
        int tearIndex = Mathf.FloorToInt(tearHash * textInfo.characterCount);

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            int matIdx = textInfo.characterInfo[i].materialReferenceIndex;
            int vertIdx = textInfo.characterInfo[i].vertexIndex;
            Vector3[] verts = textInfo.meshInfo[matIdx].vertices;
            Color32[] colors = textInfo.meshInfo[matIdx].colors32;

            // Horizontal tear: characters on one side of the tear shift sideways
            float xShift = 0f;
            if (i >= tearIndex)
            {
                float shiftNoise = Mathf.PerlinNoise(glitchSeed + 3.7f, burstP * 8f);
                xShift = (shiftNoise - 0.5f) * 60f * strength;
            }

            // Vertical jitter — smaller, random per character
            float yJitter = (Mathf.PerlinNoise(glitchSeed + i * 0.5f, burstP * 12f) - 0.5f)
                          * 9f * strength;

            for (int v = 0; v < 4; v++)
            {
                verts[vertIdx + v].x += xShift;
                verts[vertIdx + v].y += yJitter;

                // Color corruption: randomly shift color channels
                if (strength > 0.5f)
                {
                    float colorNoise = Mathf.PerlinNoise(glitchSeed + i * 1.3f, burstP * 6f);
                    if (colorNoise > 0.6f)
                    {
                        Color32 c = colors[vertIdx + v];
                        // Shift toward cyan or magenta
                        if (colorNoise > 0.8f)
                            c = new Color32((byte)Mathf.Max(0, c.r - 120), c.g, c.b, c.a);
                        else
                            c = new Color32(c.r, (byte)Mathf.Max(0, c.g - 120), c.b, c.a);
                        colors[vertIdx + v] = c;
                    }
                }
            }
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
            text.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }

    // Scanline + overlay flicker during glitch
    void ApplyGlitchToBackdrop()
    {
        if (!IsGlitching()) return;
        float burstP = glitchBurstTimer / glitchBurstDuration;
        float envelope = Mathf.Sin(burstP * Mathf.PI) * glitchIntensity;

        // Scanline UV jump — shifts the scanline pattern vertically
        float uvJump = Mathf.PerlinNoise(glitchSeed + 7f, burstP * 10f) * 0.45f * envelope;
        Rect uvRect = scanlinesImage.uvRect;
        uvRect.y += uvJump;
        scanlinesImage.uvRect = uvRect;

        // Brief brightness flicker on overlay
        Color oc = overlayImage.color;
        float flicker = 1f + (Mathf.PerlinNoise(glitchSeed + 11f, burstP * 15f) - 0.5f)
                       * 0.6f * envelope;
        oc.a *= flicker;
        overlayImage.color = oc;
    }

    // ── DROP SHADOW ──────────────────────────────────────────

    void ApplyDropShadow(TMP_Text text)
    {
        Material mat = text.fontMaterial;
        mat.EnableKeyword("UNDERLAY_ON");
        mat.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.4f));
        mat.SetFloat("_UnderlayOffsetX", 0.8f);
        mat.SetFloat("_UnderlayOffsetY", -0.8f);
        mat.SetFloat("_UnderlaySoftness", 0.3f);
    }

    // ── EASING ───────────────────────────────────────────────

    float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    // Heartbeat: two quick bumps then a rest period (~1.6s cycle)
    float HeartbeatPulse(float time)
    {
        float cycle = time % 1.6f;
        float bump1 = Mathf.Exp(-Mathf.Pow((cycle - 0.0f) * 8f, 2f));  // First beat at 0.0s
        float bump2 = Mathf.Exp(-Mathf.Pow((cycle - 0.25f) * 8f, 2f)); // Second beat at 0.25s
        float beat = Mathf.Max(bump1, bump2);
        return 0.3f + beat * 0.5f; // Base 0.3, peaks at 0.8
    }

    float EaseOutElastic(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        return Mathf.Pow(2f, -10f * t)
             * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
    }
}
