using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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

    // ── CANVAS ───────────────────────────────────────────────
    private Canvas canvas;
    private Image overlayImage;
    private RawImage vignetteImage;
    private RawImage scanlinesImage;
    private Image blackoutImage;  // Full black to hide torus during splash

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
    private TMP_Text bestScoreText;
    private Image bestScoreLine;
    private Button titleLBBtn;
    private RawImage titleLBIcon;
    private Button titleSettingsBtn;
    private RawImage titleSettingsIcon;
    private TMP_Text titleTapText;
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
    private const int LEADERBOARD_SHOW = 5;
    private Button goSettingsBtn;
    private RawImage goSettingsIcon;
    private Button goLBBtn;
    private RawImage goLBIcon;

    // ── SETTINGS PANEL ───────────────────────────────────────
    private RectTransform settingsPanel;
    private TMP_Text settingsMusicLabel;
    private TMP_Text settingsSoundLabel;
    private Texture2D trophyTex;
    private Texture2D cogTex;
    private Texture2D quitTex;

    // ── QUIT BUTTON (macOS only) ────────────────────────────
    private Button titleQuitBtn;
    private RawImage titleQuitIcon;
    private Button goQuitBtn;
    private RawImage goQuitIcon;

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

    // ── FONT ─────────────────────────────────────────────────
    private TMP_FontAsset defaultFont;
    private Sprite circleSprite;

    void Start()
    {
        LoadScores();
        defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        circleSprite = CreateCircleSprite(32);
        BuildUI();
    }

    void Update()
    {
        if (canvas == null) return;

        // Splash is self-timed — don't poll sphere state
        if (state == State.Splash)
        {
            stateTimer += Time.deltaTime;
            AnimateState();
            return;
        }

        Sphere sphere = FindAnyObjectByType<Sphere>();
        if (sphere == null) return;

        State newState;
        if (sphere.IsWaiting())
            newState = State.Title;
        else if (sphere.IsRewinding())
            newState = State.Rewinding;
        else if (sphere.IsGameOver())
            newState = State.GameOver;
        else
            newState = State.Playing;

        if (newState != state)
        {
            State fromState = state;
            state = newState;
            stateTimer = 0f;

            if (state == State.Rewinding)
                OnGameOver();
            if (state == State.GameOver && fromState == State.Playing)
                OnGameOver();
            if (state == State.Playing)
            {
                lastPlayingScore = "0";
                if (playingScoreText != null) playingScoreText.text = "0";
                scoreAnimTimer = -1f;
                scorePopTimer = -1f;
                scoreGlowTimer = -1f;
                currentStreak = 0;
                streakFlashTimer = -1f;
                titleFadeOutTimer = 0f;
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

    public bool IsSplash() { return state == State.Splash; }

    public bool CanRestart()
    {
        if (state != State.GameOver || stateTimer < 1.0f) return false;

        if (!GameConfig.IsTimeWarp())
        {
            Sphere sphere = FindAnyObjectByType<Sphere>();
            if (sphere != null && sphere.mRewindSystem != null
                && !sphere.mRewindSystem.IsFullyComplete())
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
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Full black overlay — hides torus during splash
        blackoutImage = CreateImage(canvasObj.transform, "Blackout",
            new Color(0f, 0f, 0f, 1f));
        StretchFull(blackoutImage.rectTransform);
        blackoutImage.raycastTarget = false;

        // Purple-tinted overlay — retro color grading
        overlayImage = CreateImage(canvasObj.transform, "Overlay",
            new Color(0.06f, 0.02f, 0.10f, 0f));
        StretchFull(overlayImage.rectTransform);
        overlayImage.raycastTarget = false;

        // Vignette overlay (procedural radial gradient)
        vignetteImage = CreateVignette(canvasObj.transform);

        // CRT scanlines — retro horizontal bands
        scanlinesImage = CreateScanlines(canvasObj.transform);

        // Procedural icon textures for buttons
        trophyTex = GenerateTrophyIcon(128);
        cogTex = GenerateCogIcon(128);
        quitTex = GenerateQuitIcon(128);

        BuildSplashGroup(canvasObj.transform);
        BuildTitleGroup(canvasObj.transform);
        BuildPlayingGroup(canvasObj.transform);
        BuildGameOverGroup(canvasObj.transform);

        ShowGroup(state);
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
        int w = 256;
        int h = 512;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        // Fill black
        Color[] pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color(0.01f, 0.005f, 0.02f, 1f);
        // Scatter stars
        Random.InitState(42);
        for (int i = 0; i < 200; i++)
        {
            int x = Random.Range(0, w);
            int y = Random.Range(0, h);
            float brightness = Random.Range(0.2f, 0.7f);
            float size = Random.Range(0.5f, 1.5f);
            // Tint some stars slightly colored
            float r = brightness * Random.Range(0.85f, 1.0f);
            float g = brightness * Random.Range(0.85f, 1.0f);
            float b = brightness * Random.Range(0.9f, 1.0f);
            pixels[y * w + x] = new Color(r, g, b, 1f);
            // Larger stars get a dim halo
            if (size > 1.0f)
            {
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = Mathf.Clamp(x + dx, 0, w - 1);
                        int ny = Mathf.Clamp(y + dy, 0, h - 1);
                        if (nx == x && ny == y) continue;
                        Color existing = pixels[ny * w + nx];
                        float halo = brightness * 0.2f;
                        pixels[ny * w + nx] = new Color(
                            Mathf.Max(existing.r, halo * r),
                            Mathf.Max(existing.g, halo * g),
                            Mathf.Max(existing.b, halo * b), 1f);
                    }
            }
        }
        Random.InitState((int)System.DateTime.Now.Ticks);
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;

        GameObject obj = new GameObject("Stars");
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
        titleTapText = CreateText(titleGroup, "TitleTap", "TAP TO PLAY",
            36, FontStyles.Normal, new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0f));
        SetAnchored(titleTapText.rectTransform, new Vector2(0.5f, 0.12f), new Vector2(600, 55));
        titleTapText.characterSpacing = 8f;

        // Icon buttons — top right, side by side
        titleLBBtn = CreateIconButton(titleGroup, "TitleLBBtn",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-360, -140), new Vector2(140, 140),
            trophyTex, out titleLBIcon);
        titleLBBtn.onClick.AddListener(OnLeaderboardTap);

        titleSettingsBtn = CreateIconButton(titleGroup, "TitleSettingsBtn",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-170, -140), new Vector2(140, 140),
            cogTex, out titleSettingsIcon);
        titleSettingsBtn.onClick.AddListener(OnSettingsTap);

#if !UNITY_IOS || UNITY_EDITOR
        titleQuitBtn = CreateIconButton(titleGroup, "TitleQuitBtn",
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(170, -140), new Vector2(140, 140),
            quitTex, out titleQuitIcon);
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

        // Leaderboard rows — staggered slide-in
        goLeaderboardTexts = new TMP_Text[LEADERBOARD_SHOW];
        for (int i = 0; i < LEADERBOARD_SHOW; i++)
        {
            TMP_Text row = CreateText(gameOverGroup, "LB" + i, "",
                28, FontStyles.Normal, new Color(DIM_TEXT.r, DIM_TEXT.g, DIM_TEXT.b, 0f));
            SetAnchored(row.rectTransform, new Vector2(0.5f, 0.38f - i * 0.038f), new Vector2(350, 40));
            row.characterSpacing = 2f;
            goLeaderboardTexts[i] = row;
        }

        // Tap to play again
        goTapText = CreateText(gameOverGroup, "GOTap", "TAP TO PLAY",
            36, FontStyles.Normal, new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0f));
        SetAnchored(goTapText.rectTransform, new Vector2(0.5f, 0.12f), new Vector2(600, 55));
        goTapText.characterSpacing = 8f;

        // Icon buttons — top right, side by side
        goLBBtn = CreateIconButton(gameOverGroup, "GOLBBtn",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-360, -140), new Vector2(140, 140),
            trophyTex, out goLBIcon);
        goLBBtn.onClick.AddListener(OnLeaderboardTap);

        goSettingsBtn = CreateIconButton(gameOverGroup, "GOSettingsBtn",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-170, -140), new Vector2(140, 140),
            cogTex, out goSettingsIcon);
        goSettingsBtn.onClick.AddListener(OnSettingsTap);

#if !UNITY_IOS || UNITY_EDITOR
        goQuitBtn = CreateIconButton(gameOverGroup, "GOQuitBtn",
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(170, -140), new Vector2(140, 140),
            quitTex, out goQuitIcon);
        goQuitBtn.onClick.AddListener(OnQuitTap);
#endif

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
        TickGlitch();

        switch (state)
        {
            case State.Splash: AnimateSplash(); break;
            case State.Title: AnimateTitle(); break;
            case State.Playing: AnimatePlaying(); break;
            case State.Rewinding: AnimateRewinding(); break;
            case State.GameOver: AnimateGameOver(); break;
        }

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
                    ApplyGlitchToText(goLeaderboardTexts[i]);
            }
            ApplyGlitchToBackdrop();
        }
    }

    // ── SPLASH ───────────────────────────────────────────────

    void AnimateSplash()
    {
        float t = stateTimer;

        // Stars background fade in
        float starsFade = Mathf.Clamp01(t / SPLASH_FADE_IN);
        splashStarsImage.color = new Color(1f, 1f, 1f, starsFade);

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

        // ── CHROMATIC ABERRATION — bold CMYK separation ──
        float chromaFade = Mathf.Clamp01((stateTimer - 0.4f) / 0.6f);
        float chromaAlpha = chromaFade * 0.65f;

        float t = Time.time;
        float spread = 7f + Mathf.Sin(t * 0.7f) * 3f; // 4-10px spread

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

        // Best score with expanding gold line
        if (topScores.Count > 0)
        {
            float bestFade = Mathf.Clamp01((stateTimer - 0.8f) / 0.6f);
            string bestStr = GameConfig.IsTimeWarp()
                ? FormatTimeScore(topScores[0])
                : topScores[0].ToString();
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

        // Tap to play — pulsing cyan
        if (stateTimer > 1.2f)
        {
            float fadeIn = Mathf.Clamp01((stateTimer - 1.2f) / 0.4f);
            float pulse = 0.45f + Mathf.Sin(Time.time * 2.5f) * 0.25f;
            SetAlpha(titleTapText, fadeIn * pulse);
        }
        else
        {
            SetAlpha(titleTapText, 0f);
        }

        // Icon buttons — fade in with best score
        float lbFade = Mathf.Clamp01((stateTimer - 0.8f) / 0.6f);
        SetIconAlpha(titleLBIcon, lbFade * 0.5f);
        SetIconAlpha(titleSettingsIcon, lbFade * 0.5f);
        SetIconAlpha(titleQuitIcon, lbFade * 0.5f);
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

            // Fade overlays with title
            overlayImage.color = new Color(0.06f, 0.02f, 0.10f, fadeAlpha * 0.9f);
            vignetteImage.color = new Color(1f, 1f, 1f, fadeAlpha * 0.55f);
            scanlinesImage.color = new Color(1f, 1f, 1f, fadeAlpha * 0.9f);

            if (fp >= 1f)
            {
                titleFadeOutTimer = -1f;
                titleGroup.gameObject.SetActive(false);
                titleCanvasGroup.alpha = 1f;
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

        // Retro backdrop: heavy vignette + purple overlay + scanlines
        float backdropP = Mathf.Clamp01(t / 0.6f);
        vignetteImage.color = new Color(1f, 1f, 1f, backdropP * 0.55f);
        overlayImage.color = new Color(0.06f, 0.02f, 0.10f, backdropP * 0.9f);
        scanlinesImage.color = new Color(1f, 1f, 1f, backdropP * 0.9f);

        // Score count-up from 0 to final value
        if (t > 0.3f)
        {
            // Duration scales with score (0.3s min, 1.2s max)
            float countScale = GameConfig.IsTimeWarp() ? 0.005f : 0.04f;
            float countDuration = Mathf.Clamp(goFinalScore * countScale, 0.3f, 1.2f);
            float countP = Mathf.Clamp01((t - 0.3f) / countDuration);
            float countEased = EaseOutCubic(countP);
            int displayScore = Mathf.RoundToInt(goFinalScore * countEased);
            if (displayScore != goLastDisplayScore)
            {
                goLastDisplayScore = displayScore;
                GameAudio audio = FindAnyObjectByType<GameAudio>();
                if (audio != null) audio.PlayCount();
            }
            string displayText = GameConfig.IsTimeWarp()
                ? FormatTimeScore(displayScore)
                : displayScore.ToString();
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

            // Spring entrance scale
            float scaleP = Mathf.Clamp01((t - 0.3f) / 0.6f);
            float scale = Mathf.Lerp(1.2f, 1.0f, EaseOutBack(scaleP));
            goScoreText.rectTransform.localScale = new Vector3(scale, scale, 1f);
            goScoreGlowText.rectTransform.localScale = new Vector3(scale, scale, 1f);

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
                GameAudio audio = FindAnyObjectByType<GameAudio>();
                if (audio != null) audio.PlayNewBest(isNewBest);
            }
        }
        else
        {
            SetAlpha(goNewBestText, 0f);
            goNewBestText.rectTransform.localScale = Vector3.one;
        }

        // Mini leaderboard — staggered slide-in
        AnimateLeaderboard(t);

        // Icon buttons
        if (t > 0.6f)
        {
            float p = Mathf.Clamp01((t - 0.6f) / 0.4f);
            SetIconAlpha(goSettingsIcon, p * 0.4f);
            SetIconAlpha(goLBIcon, p * 0.4f);
            SetIconAlpha(goQuitIcon, p * 0.4f);
        }
        else
        {
            SetIconAlpha(goSettingsIcon, 0f);
            SetIconAlpha(goLBIcon, 0f);
            SetIconAlpha(goQuitIcon, 0f);
        }

        // Tap to play — pulsing cyan
        if (t > 1.8f)
        {
            float pulse = 0.45f + Mathf.Sin(Time.time * 2.5f) * 0.25f;
            SetAlpha(goTapText, pulse);
        }
        else
        {
            SetAlpha(goTapText, 0f);
        }
    }

    void AnimateLeaderboard(float t)
    {
        float lbStart = isNewBest ? 1.5f : 1.2f;

        for (int i = 0; i < LEADERBOARD_SHOW; i++)
        {
            if (i >= topScores.Count)
            {
                SetAlpha(goLeaderboardTexts[i], 0f);
                continue;
            }

            float rowDelay = lbStart + i * 0.08f;
            if (t < rowDelay)
            {
                SetAlpha(goLeaderboardTexts[i], 0f);
                goLeaderboardTexts[i].rectTransform.anchoredPosition = new Vector2(30f, 0f);
                continue;
            }

            float rowP = Mathf.Clamp01((t - rowDelay) / 0.3f);
            float rowAlpha = EaseOutCubic(rowP);

            int score = topScores[i];
            bool isCurrent = (score == goFinalScore && i == goRank - 1);

            string scoreStr = GameConfig.IsTimeWarp() ? FormatTimeScore(score) : score.ToString();
            goLeaderboardTexts[i].text = (i + 1) + ".   " + scoreStr;

            // Color hierarchy: current run = cyan, #1 = gold, rest = dim
            Color rowColor;
            float maxAlpha;
            if (isCurrent)
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

            // Slide in from right
            float slideX = Mathf.Lerp(30f, 0f, EaseOutCubic(rowP));
            goLeaderboardTexts[i].rectTransform.anchoredPosition = new Vector2(slideX, 0f);
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
        Sphere sphere = FindAnyObjectByType<Sphere>();
        if (sphere != null && sphere.IsWaiting())
            sphere.StartGame();
    }

    Button CreateIconButton(RectTransform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size,
        Texture2D icon, out RawImage iconImage)
    {
        GameObject btnObj = new GameObject(name);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image bg = btnObj.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.01f);
        Button btn = btnObj.AddComponent<Button>();

        GameObject iconObj = new GameObject("Icon");
        RectTransform iconRT = iconObj.AddComponent<RectTransform>();
        iconRT.SetParent(rt, false);
        iconRT.anchorMin = Vector2.zero;
        iconRT.anchorMax = Vector2.one;
        iconRT.sizeDelta = Vector2.zero;
        iconRT.offsetMin = Vector2.zero;
        iconRT.offsetMax = Vector2.zero;

        iconImage = iconObj.AddComponent<RawImage>();
        iconImage.texture = icon;
        iconImage.color = new Color(1f, 1f, 1f, 0f);
        iconImage.raycastTarget = true;

        return btn;
    }

    void SetIconAlpha(RawImage icon, float alpha)
    {
        if (icon == null) return;
        icon.color = new Color(1f, 1f, 1f, alpha);
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
        dimBg.color = new Color(0f, 0f, 0f, 0.7f);
        Button closeBg = panelObj.AddComponent<Button>();
        closeBg.onClick.AddListener(CloseSettings);

        // Card — raycast-blocking background (no Button, so events don't bubble to dimBg)
        GameObject card = new GameObject("Card");
        RectTransform cardRT = card.AddComponent<RectTransform>();
        cardRT.SetParent(settingsPanel, false);
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(500, 320);
        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(0.08f, 0.05f, 0.12f, 0.95f);
        cardBg.raycastTarget = true;

        // Title
        TMP_Text title = CreateText(cardRT, "Title", "SETTINGS",
            36, FontStyles.Bold, NEON_CYAN);
        SetAnchored(title.rectTransform, new Vector2(0.5f, 0.85f), new Vector2(400, 50));
        title.characterSpacing = 8f;
        title.raycastTarget = false;

        // Music toggle button
        Button musicBtn = CreateSettingsToggle(cardRT, "MusicBtn",
            new Vector2(0.5f, 0.55f), out settingsMusicLabel);
        musicBtn.onClick.AddListener(ToggleMusic);

        // Sound toggle button
        Button soundBtn = CreateSettingsToggle(cardRT, "SoundBtn",
            new Vector2(0.5f, 0.35f), out settingsSoundLabel);
        soundBtn.onClick.AddListener(ToggleSound);

        // Close label
        TMP_Text closeLabel = CreateText(cardRT, "Close", "TAP OUTSIDE TO CLOSE",
            18, FontStyles.Normal, DIM_TEXT);
        SetAnchored(closeLabel.rectTransform, new Vector2(0.5f, 0.1f), new Vector2(400, 30));
        closeLabel.raycastTarget = false;
    }

    Button CreateSettingsToggle(RectTransform parent, string name, Vector2 anchor, out TMP_Text label)
    {
        GameObject btnObj = new GameObject(name);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(400, 60);

        Image bg = btnObj.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.06f);
        bg.raycastTarget = true;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.12f);
        cb.pressedColor = new Color(1f, 1f, 1f, 0.18f);
        btn.colors = cb;

        label = CreateText(rt, "Label", "",
            28, FontStyles.Normal, Color.white);
        StretchFull(label.rectTransform);
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

    void ToggleMusic()
    {
        GameAudio audio = FindAnyObjectByType<GameAudio>();
        if (audio == null) return;
        audio.SetMusicMuted(!audio.IsMusicMuted());
        RefreshSettingsLabels();
    }

    void ToggleSound()
    {
        GameAudio audio = FindAnyObjectByType<GameAudio>();
        if (audio == null) return;
        audio.SetSoundMuted(!audio.IsSoundMuted());
        RefreshSettingsLabels();
    }

    void RefreshSettingsLabels()
    {
        GameAudio audio = FindAnyObjectByType<GameAudio>();
        if (audio == null) return;

        bool musicOff = audio.IsMusicMuted();
        bool soundOff = audio.IsSoundMuted();

        if (settingsMusicLabel != null)
        {
            settingsMusicLabel.text = musicOff ? "MUSIC: OFF" : "MUSIC: ON";
            settingsMusicLabel.color = musicOff ? DIM_TEXT : Color.white;
        }
        if (settingsSoundLabel != null)
        {
            settingsSoundLabel.text = soundOff ? "SOUND: OFF" : "SOUND: ON";
            settingsSoundLabel.color = soundOff ? DIM_TEXT : Color.white;
        }
    }

    // ── PROCEDURAL ICONS ────────────────────────────────────

    Texture2D GenerateTrophyIcon(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f;
        float r = half * 0.9f; // Circle radius
        float strokeW = size * 0.03f;
        Color col = NEON_GOLD;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - half;
                float dy = y - half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Normalized coords within icon area (-1..1)
                float nx = dx / (half * 0.6f);
                float ny = dy / (half * 0.6f);

                float alpha = 0f;

                // Circle stroke
                float ringDist = Mathf.Abs(dist - r);
                if (ringDist < strokeW)
                    alpha = Mathf.Max(alpha, 1f - ringDist / strokeW);

                // Trophy shape (simplified)
                // Cup body: wide at top, narrowing down
                float cupTop = 0.25f;
                float cupBot = -0.15f;
                float cupWidthTop = 0.55f;
                float cupWidthBot = 0.25f;
                if (ny > cupBot && ny < cupTop)
                {
                    float t = (ny - cupBot) / (cupTop - cupBot);
                    float w = Mathf.Lerp(cupWidthBot, cupWidthTop, t);
                    if (Mathf.Abs(nx) < w)
                    {
                        // Cup interior — filled
                        float edge = Mathf.Clamp01((w - Mathf.Abs(nx)) * size * 0.04f);
                        alpha = Mathf.Max(alpha, edge);
                    }
                }

                // Cup handles — arcs on sides
                float handleCy = 0.1f;
                float handleR = 0.3f;
                float handleW = 0.07f;
                for (int side = -1; side <= 1; side += 2)
                {
                    float hx = nx - side * 0.5f;
                    float hy = ny - handleCy;
                    float hDist = Mathf.Sqrt(hx * hx + hy * hy);
                    float hRing = Mathf.Abs(hDist - handleR);
                    if (hRing < handleW && (side * nx > 0.3f))
                        alpha = Mathf.Max(alpha, 1f - hRing / handleW);
                }

                // Stem
                if (ny > -0.35f && ny < cupBot + 0.05f && Mathf.Abs(nx) < 0.08f)
                    alpha = Mathf.Max(alpha, 1f);

                // Base
                if (ny > -0.45f && ny < -0.3f && Mathf.Abs(nx) < 0.3f)
                {
                    float edge = Mathf.Clamp01((0.3f - Mathf.Abs(nx)) * size * 0.04f);
                    alpha = Mathf.Max(alpha, edge);
                }

                // Mask to inside circle
                if (dist > r + strokeW) alpha = 0f;

                tex.SetPixel(x, y, new Color(col.r, col.g, col.b, alpha));
            }
        }
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    Texture2D GenerateCogIcon(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f;
        float r = half * 0.9f;
        float strokeW = size * 0.03f;
        Color col = DIM_TEXT;

        int teeth = 8;
        float outerR = half * 0.55f;
        float innerR = half * 0.35f;
        float holeR = half * 0.18f;
        float toothW = 0.6f; // Tooth angular width as fraction of tooth period

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - half;
                float dy = y - half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx);

                float alpha = 0f;

                // Circle stroke
                float ringDist = Mathf.Abs(dist - r);
                if (ringDist < strokeW)
                    alpha = Mathf.Max(alpha, 1f - ringDist / strokeW);

                // Cog body — gear with teeth
                float toothAngle = (angle / (Mathf.PI * 2f) * teeth) % 1f;
                if (toothAngle < 0) toothAngle += 1f;
                float cogR = (toothAngle < toothW) ? outerR : innerR;
                // Smooth between inner and outer
                float transW = 0.1f;
                if (toothAngle >= toothW - transW && toothAngle < toothW + transW)
                    cogR = Mathf.Lerp(outerR, innerR, (toothAngle - (toothW - transW)) / (transW * 2f));
                if (toothAngle >= 1f - transW)
                    cogR = Mathf.Lerp(innerR, outerR, (toothAngle - (1f - transW)) / transW);

                if (dist < cogR && dist > holeR)
                {
                    float edgeOuter = Mathf.Clamp01((cogR - dist) * 0.15f * size / 128f);
                    float edgeInner = Mathf.Clamp01((dist - holeR) * 0.15f * size / 128f);
                    alpha = Mathf.Max(alpha, Mathf.Min(edgeOuter, edgeInner));
                }

                // Mask to inside circle
                if (dist > r + strokeW) alpha = 0f;

                tex.SetPixel(x, y, new Color(col.r, col.g, col.b, alpha));
            }
        }
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    Texture2D GenerateQuitIcon(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f;
        float r = half * 0.9f;
        float strokeW = size * 0.03f;
        Color col = DIM_TEXT;

        // Power symbol: open circle (gap at top) + vertical line
        float arcR = half * 0.38f;
        float arcW = size * 0.06f;
        float lineW = size * 0.06f;
        float lineTop = half + half * 0.55f;
        float lineBot = half + half * 0.05f;
        float gapAngle = 35f * Mathf.Deg2Rad; // Half-gap from top center

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - half;
                float dy = y - half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx);

                float alpha = 0f;

                // Outer circle stroke
                float ringDist = Mathf.Abs(dist - r);
                if (ringDist < strokeW)
                    alpha = Mathf.Max(alpha, 1f - ringDist / strokeW);

                // Power arc (open circle, gap at top)
                float arcDist = Mathf.Abs(dist - arcR);
                float angleFromTop = Mathf.Abs(Mathf.Atan2(dx, dy)); // angle from 12 o'clock
                if (arcDist < arcW && angleFromTop > gapAngle)
                {
                    float edge = 1f - arcDist / arcW;
                    alpha = Mathf.Max(alpha, edge);
                }

                // Vertical line (top part of power symbol)
                float lineDx = Mathf.Abs(dx);
                if (lineDx < lineW && y >= lineBot && y <= lineTop)
                {
                    float edge = 1f - lineDx / lineW;
                    alpha = Mathf.Max(alpha, edge);
                }

                // Mask to inside circle
                if (dist > r + strokeW) alpha = 0f;

                tex.SetPixel(x, y, new Color(col.r, col.g, col.b, alpha));
            }
        }
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
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
        if (GameCenterManager.Instance != null)
            GameCenterManager.Instance.ShowLeaderboard();
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
        string saved = PlayerPrefs.GetString(GameConfig.GetScoresKey(), "");
        if (!string.IsNullOrEmpty(saved))
        {
            string[] parts = saved.Split(',');
            foreach (string p in parts)
            {
                int val;
                if (int.TryParse(p, out val))
                    topScores.Add(val);
            }
        }
        topScores.Sort((a, b) => b.CompareTo(a));
    }

    void SaveScores()
    {
        string joined = string.Join(",", topScores);
        PlayerPrefs.SetString(GameConfig.GetScoresKey(), joined);
        PlayerPrefs.Save();
    }

    int InsertScore(int score)
    {
        if (score <= 0) return 0;

        topScores.Add(score);
        topScores.Sort((a, b) => b.CompareTo(a));

        int rank = topScores.IndexOf(score) + 1;

        while (topScores.Count > MAX_SCORES)
            topScores.RemoveAt(topScores.Count - 1);

        SaveScores();

        // Report to GameCenter
        if (GameCenterManager.Instance != null)
        {
            GameCenterManager.Instance.ReportScore(score);
            GameCenterManager.Instance.ReportTaps(Sphere.GetTotalTaps());
            GameCenterManager.Instance.ReportRuns(Sphere.GetTotalRuns());
        }

        return rank <= MAX_SCORES ? rank : 0;
    }

    public void ClearAllScores()
    {
        topScores.Clear();
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
            GameAudio audio = FindAnyObjectByType<GameAudio>();
            if (audio != null) audio.PlayGlitch();
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
                GameAudio audio = FindAnyObjectByType<GameAudio>();
                if (audio != null) audio.PlayGlitch();
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

    float EaseOutElastic(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        return Mathf.Pow(2f, -10f * t)
             * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
    }
}
