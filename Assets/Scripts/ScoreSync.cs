using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public partial class ScoreSync : MonoBehaviour
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
    private enum State { Splash, Title, Tutorial, Playing, Rewinding, GameOver }
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
    // Toggle in the Inspector to force-play the tutorial on the next run (clears
    // HasSeenTutorial + HasPlayed at Start). Handy for design review without a rebuild.
    [SerializeField] private bool debugForceTutorial = false;
    private bool isPaused = false;
    // Set by OnModesTap so the state machine shows the title instead of skipping to Playing.
    private bool forceShowTitle = false;

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
    private TMP_Text titlePureHellBestLabel;
    private Button titleBlitzBtn;
    private TMP_Text titleBlitzLabel;
    private TMP_Text titleBlitzBestLabel;

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

    // Phones (iPhone + Android) get a larger powerup / multiplier HUD — the default
    // tvOS/desktop/iPad sizes read too small at phone screen distances.
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
    private const float PHONE_HUD_SCALE = 1.33f;
#else
    private const float PHONE_HUD_SCALE = 1.0f;
#endif

    // ── BLITZ UPGRADE HUD ────────────────────────────────────
    private RectTransform blitzUpgradeGroup;
    private Image[] blitzGunSlots;
    private Image[] blitzCadencySlots;
    private Image[] blitzShieldSlots;
    private TMP_Text blitzGunLabel;
    private TMP_Text blitzCadencyLabel;
    private TMP_Text blitzShieldLabel;
    private int blitzLastGunCount = -1;
    private int blitzLastCadencyCount = -1;
    private int blitzLastShieldCount = -1;
    private int blitzLastGunLevel = -1;
    private int blitzLastCadencyLevel = -1;

    // ── BLITZ KILL-STREAK HUD ───────────────────────────────
    private RectTransform blitzStreakGroup;
    private TMP_Text blitzStreakTierLabel;
    private Image blitzStreakBarTrack;
    private Image blitzStreakBarFill;
    private int blitzLastStreakTier = -1;
    private float blitzLastStreakBar = -1f;
    private float blitzStreakFlashTimer = -1f;
    private const float BLITZ_STREAK_FLASH_DURATION = 0.35f;

    // ── BLITZ ORB SPARKS ────────────────────────────────────
    private const int MAX_SPARKS = 6;
    private const float SPARK_DURATION = 0.45f;
    private const float SPARK_SIZE = 28f;
    private Image[] sparkImages;
    private Vector2[] sparkFrom;
    private Vector2[] sparkTo;
    private float[] sparkTimers;
    private int[] sparkSlotIndex;
    private BlitzOrb.OrbType[] sparkOrbType;
    private RectTransform canvasRT;

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
    private TMP_Text settingsMusicIcon;
    private TMP_Text settingsSoundIcon;
    private TMP_Text settingsThemeLabel;
    private TMP_Text settingsLanguageLabel;
    private TMP_Text settingsFullscreenLabel;
    private TMP_Text settingsResLabel;

    // ── QUIT BUTTON (desktop only) + BACK BUTTON (game over, all platforms) ────
    private Button titleQuitBtn;
    private CanvasGroup titleQuitIcon;
    private Button goBackBtn;
    private CanvasGroup goBackIcon;

    // ── STATS PANEL ─────────────────────────────────────────
    // No title stats button — stats are per-mode and only reached from in-game/game-over.
    private RectTransform statsPanel;
    private Button goStatsBtn;
    private CanvasGroup goStatsIcon;
    private TMP_Text statsRunsLabel;
    private TMP_Text statsTapsLabel;
    private TMP_Text statsBestLabel;
    private TMP_Text statsAvgLabel;
    private TMP_Text statsGatesLabel;
    private TMP_Text statsObstaclesLabel;

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

    // ── TUTORIAL ─────────────────────────────────────────────
    // First-run onboarding: stationary torus, no obstacles, UI walks the player through
    // left + right swing. Stashed mode re-enters the normal flow once both fired + tap.
    private RectTransform tutorialGroup;
    private TMP_Text tutorialLeftArrow;
    private TMP_Text tutorialRightArrow;
    private TMP_Text tutorialInstructionText;
    private TMP_Text tutorialNudgeText;
    private TMP_Text tutorialReadyText;
    private TMP_Text tutorialReadyHintText;
    private Image tutorialPlatformImage; // Placeholder — user will supply per-platform sprites
    private float mTutorialSinceBothSeenTimer = -1f; // When both L/R fired, countdown to Ready prompt
    private float mTutorialNudgeTimer = 0f;          // Elapsed since last progress — nudges after threshold
    private float mTutorialDeathFlashTimer = -1f;    // Non-neg: death message visible
    private bool  mTutorialReady = false;            // Ready? prompt shown, awaiting tap
    private const float TUTORIAL_NUDGE_DELAY = 5f;   // Seconds with only one dir before nudging for the other
    private const float TUTORIAL_READY_DELAY = 0.6f; // Pause after both fired before showing "Ready?"
    private const float TUTORIAL_DEATH_FLASH_DURATION = 2.0f;

    // ── FONT ─────────────────────────────────────────────────
    private TMP_FontAsset defaultFont;
    private TMP_FontAsset phosphorFont;
    private Sprite circleSprite;

    // Phosphor Icons codepoints (Private Use Area).
    private const string PHOSPHOR_TROPHY        = "\uE67E";
    private const string PHOSPHOR_GEAR          = "\uE270";
    private const string PHOSPHOR_CARET_LEFT    = "\uE138";
    private const string PHOSPHOR_LIST_BULLET   = "\uE2F2";
    private const string PHOSPHOR_SPEAKER_HIGH  = "\uE44A";
    private const string PHOSPHOR_SPEAKER_X     = "\uE45C";
    private const string PHOSPHOR_POWER         = "\uE3DA";

    // ── CACHED REFS ─────────────────────────────────────────
    private Sphere mSphere;
    private GameAudio mAudio;

    void Start()
    {
#if UNITY_EDITOR
        PlayerPrefs.DeleteKey("HasPlayed");
#endif
        if (debugForceTutorial)
        {
            PlayerPrefs.DeleteKey("HasPlayed");
            PlayerPrefs.DeleteKey("HasSeenTutorial");
            PlayerPrefs.Save();
        }
        LoadScores();
        L10n.Initialize();
        L10n.OnLanguageChanged += OnLanguageChanged;
        defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        // Switch to dynamic atlas so glyphs outside ASCII (Cyrillic, accented Latin) get
        // rasterized on demand from the underlying TTF. Translations pull in German,
        // French, Spanish, Italian, Portuguese, and Russian character sets at runtime.
        if (defaultFont != null) defaultFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        Font phosphorSource = Resources.Load<Font>("Fonts/Phosphor-Thin");
        if (phosphorSource != null)
            phosphorFont = TMP_FontAsset.CreateFontAsset(phosphorSource);
        circleSprite = CreateCircleSprite(32);
        mSphere = FindAnyObjectByType<Sphere>();
        mAudio = FindAnyObjectByType<GameAudio>();
        isFirstRun = PlayerPrefs.GetInt(PREF_FIRST_RUN, 0) == 0;
        BuildUI();
    }

    void OnDestroy()
    {
        L10n.OnLanguageChanged -= OnLanguageChanged;
    }

    void OnLanguageChanged()
    {
        RefreshLocalizedLabels();
        // Rebuild any panel that's currently visible so the swap lands in-place
        // instead of kicking the player back out. Hidden panels just get dropped
        // and will rebuild fresh on next open.
        bool settingsWasOpen = IsSettingsOpen();
        if (settingsPanel != null)
        {
            Destroy(settingsPanel.gameObject);
            settingsPanel = null;
        }
        if (settingsWasOpen)
        {
            BuildSettingsPanel(canvas.transform);
            RefreshSettingsLabels();
            settingsPanel.gameObject.SetActive(true);
        }

        bool statsWasOpen = IsStatsOpen();
        if (statsPanel != null)
        {
            Destroy(statsPanel.gameObject);
            statsPanel = null;
        }
        if (statsWasOpen)
        {
            BuildStatsPanel(canvas.transform);
            RefreshStatsLabels();
            statsPanel.gameObject.SetActive(true);
        }
    }

    // Sweep all persistent in-scene labels so a language swap takes effect immediately
    // without needing to rebuild the whole UI. Panels that are torn down on close
    // (settings, stats) are handled separately in OnLanguageChanged.
    void RefreshLocalizedLabels()
    {
        if (splashPresentsText != null) splashPresentsText.text = L10n.T("splash.presents");
        if (titleHintText != null) titleHintText.text = L10n.T("title.select_mode");
        if (pausedText != null) pausedText.text = L10n.T("pause.paused");
        if (pauseHintText != null) pauseHintText.text = GetResumeHint();
        if (goTapText != null) goTapText.text = GetTapPrompt();
        if (tutorialInstructionText != null) tutorialInstructionText.text = GetTutorialInstruction();
        if (tutorialReadyText != null) tutorialReadyText.text = L10n.T("tutorial.ready");
        if (tutorialReadyHintText != null) tutorialReadyHintText.text = GetTutorialReadyHint();
        // Force the Blitz HUD labels to re-emit from their cached levels on the next tick.
        blitzLastGunLevel = -1;
        blitzLastCadencyLevel = -1;
        blitzLastCadencyCount = -1;
        RefreshModeBestLabels();
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
        else if (GameConfig.IsTutorialActive)
            newState = State.Tutorial;
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
                UpdatePlayingHUDVisibility();
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
                blitzLastGunLevel = -1;
                blitzLastCadencyLevel = -1;
                titleFadeOutTimer = 0f;

                // First-run tutorial: mark as played (hint text won't show again)
                if (isFirstRun)
                {
                    isFirstRun = false;
                    PlayerPrefs.SetInt(PREF_FIRST_RUN, 1);
                    PlayerPrefs.Save();
                }
            }

            // Skip title screen on restart — fade game over overlay straight to gameplay.
            // But if the player explicitly tapped Back-to-modes, honor it and show title.
            bool skipTitle = (state == State.Title && fromState == State.GameOver && !forceShowTitle);
            if (forceShowTitle && state == State.Title) forceShowTitle = false;

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

        if (source == null) return;
        string text = source.text;
        string scoreText = text.Contains("\n") ? text.Split('\n')[0] : text;
        lastScoreText = scoreText;
        int.TryParse(scoreText, out goFinalScore);

        goRank = InsertScore(goFinalScore);
        isNewBest = (goRank == 1);
        isTopFive = (goRank >= 2 && goRank <= 5);
        goLastDisplayScore = -1;
        goNewBestSfxPlayed = false;
    }

    string FormatModeScore(int score)
    {
        return score.ToString();
    }

    string FormatTimestamp(long unixSeconds)
    {
        if (unixSeconds <= 0) return "---";
        var dt = new System.DateTimeOffset(1970, 1, 1, 0, 0, 0, System.TimeSpan.Zero)
            .AddSeconds(unixSeconds).LocalDateTime;
        var now = System.DateTime.Now;
        string month = L10n.MonthAbbr(dt.Month);
        if (dt.Year != now.Year)
            return month + " '" + (dt.Year % 100).ToString("D2");
        return month + " " + dt.Day;
    }

    public bool IsSplash() { return state == State.Splash; }
    public bool IsPaused() { return isPaused; }

    public bool CanRestart()
    {
        if (state != State.GameOver || stateTimer < 1.0f) return false;

        if (!GameConfig.IsBlitz())
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
        BuildTutorialGroup(canvasObj.transform);
        BuildGameOverGroup(canvasObj.transform);
        BuildPauseGroup(canvasObj.transform);

        ShowGroup(state);

        // Spark pool for orb collection effects — built unconditionally; only fires in Blitz.
        canvasRT = canvasObj.GetComponent<RectTransform>();
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
        splashPresentsText = CreateText(splashGroup, "SplashPresents", L10n.T("splash.presents"),
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

        // Mode picker — both modes always available, side by side. Owns input on title.
        // Squarish buttons carry both the mode name (top) and that mode's BEST (bottom),
        // so the single centered BEST line is no longer needed above.
        Vector2 modeBtnSize = new Vector2(372f, 150f);
        titlePureHellBtn = CreatePrimaryButton(titleGroup, "GatesToHellBtn",
            new Vector2(0.5f, 0.28f), modeBtnSize,
            GameConfig.GetModeName(GameModeType.PureHell), NEON_MAGENTA, out titlePureHellLabel);
        titlePureHellBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-210f, 0f);
        titlePureHellBtn.onClick.AddListener(() => StartWithMode(GameModeType.PureHell));
        AddBestLineToButton(titlePureHellBtn, NEON_MAGENTA, out titlePureHellBestLabel);
        SetButtonAlpha(titlePureHellBtn, 0f);

        titleBlitzBtn = CreatePrimaryButton(titleGroup, "PathToRedemptionBtn",
            new Vector2(0.5f, 0.28f), modeBtnSize,
            GameConfig.GetModeName(GameModeType.Blitz), NEON_CYAN, out titleBlitzLabel);
        titleBlitzBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(210f, 0f);
        titleBlitzBtn.onClick.AddListener(() => StartWithMode(GameModeType.Blitz));
        AddBestLineToButton(titleBlitzBtn, NEON_CYAN, out titleBlitzBestLabel);
        SetButtonAlpha(titleBlitzBtn, 0f);

        // Placeholder kept for animation hooks; mode buttons own the "tap to play" role.
        titleTapText = CreateText(titleGroup, "TitleTap", "",
            36, FontStyles.Normal, new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0f));
        SetAnchored(titleTapText.rectTransform, new Vector2(0.5f, 0.12f), new Vector2(600, 55));
        titleTapText.characterSpacing = 8f;

        // Above the mode buttons — soft prompt.
        titleHintText = CreateText(titleGroup, "TutorialHint", L10n.T("title.select_mode"),
            24, FontStyles.Normal, new Color(NEON_GOLD.r, NEON_GOLD.g, NEON_GOLD.b, 0f));
        SetAnchored(titleHintText.rectTransform, new Vector2(0.5f, 0.37f), new Vector2(600, 40));
        titleHintText.rectTransform.anchoredPosition = new Vector2(0f, 15f);
        titleHintText.characterSpacing = 6f;
        titleHintText.alignment = TextAlignmentOptions.Center;

        // Icon dock — top-right: Leaderboards + Settings only (no stats on title).
#if !UNITY_TVOS
        CreateDockStrip(titleGroup, "TitleDockR",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-270, -140), new Vector2(340, 100));

        titleLBBtn = CreateIconButton(titleGroup, "TitleLBBtn",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-360, -140), new Vector2(120, 120),
            "trophy", NEON_GOLD, out titleLBIcon);
        titleLBBtn.onClick.AddListener(OnLeaderboardTap);

        titleSettingsBtn = CreateIconButton(titleGroup, "TitleSettingsBtn",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-180, -140), new Vector2(120, 120),
            "gear", DIM_TEXT, out titleSettingsIcon);
        titleSettingsBtn.onClick.AddListener(OnSettingsTap);
#endif

#if UNITY_STANDALONE
        // Desktop-only: iOS/tvOS/Android must never show Quit per platform HIG.
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
        ApplyDropShadow(titleBlitzLabel);
    }

    void BuildPlayingGroup(Transform parent)
    {
        playingGroup = CreateGroup(parent, "PlayingGroup");
#if UNITY_IOS && !UNITY_TVOS
        ApplySafeAreaInsets(playingGroup);
#endif

        playingScoreText = CreateText(playingGroup, "Score", "0",
            100, FontStyles.Bold, new Color(1f, 1f, 1f, 0.25f));
        SetAnchored(playingScoreText.rectTransform, new Vector2(0.5f, 0.93f), new Vector2(600, 120));

        playingScoreTextOut = CreateText(playingGroup, "ScoreOut", "",
            100, FontStyles.Bold, new Color(1f, 1f, 1f, 0f));
        SetAnchored(playingScoreTextOut.rectTransform, new Vector2(0.5f, 0.93f), new Vector2(600, 120));

        ApplyDropShadow(playingScoreText);

        // Build BOTH mode HUDs unconditionally; visibility is toggled on Playing entry
        // via UpdatePlayingHUDVisibility(). Mode isn't committed at BuildUI time.

        // Pure Hell streak dots — pair with swing/no-tap multiplier.
        streakDots = new Image[STREAK_COUNT];
        {
            float dotSize = 10f * PHONE_HUD_SCALE;
            float dotSpacing = 20f * PHONE_HUD_SCALE;
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
        }

        BuildBlitzUpgradeHUD(playingGroup);
        BuildBlitzStreakHUD(playingGroup);

        UpdatePlayingHUDVisibility();
    }

    /// <summary>Show the HUD matching the committed mode; hide the other.</summary>
    void UpdatePlayingHUDVisibility()
    {
        bool blitz = GameConfig.IsBlitz();
        if (streakDots != null)
        {
            for (int i = 0; i < streakDots.Length; i++)
                if (streakDots[i] != null) streakDots[i].gameObject.SetActive(!blitz);
        }
        if (blitzUpgradeGroup != null)
            blitzUpgradeGroup.gameObject.SetActive(blitz);
        if (blitzStreakGroup != null)
            blitzStreakGroup.gameObject.SetActive(blitz);
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
        goNewBestText = CreateText(gameOverGroup, "GONewBest", L10n.T("gameover.new_best"),
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
        goTapText = CreateText(gameOverGroup, "GOTap", GetTapPrompt(),
            36, FontStyles.Normal, new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0f));
        SetAnchored(goTapText.rectTransform, new Vector2(0.5f, 0.12f), new Vector2(600, 55));
        goTapText.characterSpacing = 8f;

        // Icon dock — grouped strip with shared background (hidden on tvOS — no touch)
#if !UNITY_TVOS
        CreateDockStrip(gameOverGroup, "GODockR",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-360, -140), new Vector2(560, 140));

        goLBBtn = CreateIconButton(gameOverGroup, "GOLBBtn",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-540, -140), new Vector2(160, 160),
            "trophy", NEON_GOLD, out goLBIcon);
        goLBBtn.onClick.AddListener(OnLeaderboardTap);

        goStatsBtn = CreateIconButton(gameOverGroup, "GOStatsBtn",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-360, -140), new Vector2(160, 160),
            "stats", NEON_CYAN, out goStatsIcon);
        goStatsBtn.onClick.AddListener(OnStatsTap);

        goSettingsBtn = CreateIconButton(gameOverGroup, "GOSettingsBtn",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-180, -140), new Vector2(160, 160),
            "gear", DIM_TEXT, out goSettingsIcon);
        goSettingsBtn.onClick.AddListener(OnSettingsTap);
#endif

        // Back to modes — cross-platform (all platforms get this on game over).
#if !UNITY_TVOS
        CreateDockStrip(gameOverGroup, "GODockL",
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(170, -140), new Vector2(220, 140));

        goBackBtn = CreateIconButton(gameOverGroup, "GOBackBtn",
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(170, -140), new Vector2(160, 160),
            "back", NEON_CYAN, out goBackIcon);
        goBackBtn.onClick.AddListener(OnModesTap);
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

}
