using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ScoreSync : MonoBehaviour
{
    public TextMesh source;

    // ── COLOR PALETTE ────────────────────────────────────────
    static readonly Color NEON_CYAN = new Color(0.0f, 0.75f, 1.0f);
    static readonly Color NEON_MAGENTA = new Color(1.0f, 0.15f, 0.55f);
    static readonly Color NEON_GOLD = new Color(1.0f, 0.85f, 0.1f);
    static readonly Color DEEP_PURPLE = new Color(0.08f, 0.03f, 0.14f);
    static readonly Color DIM_TEXT = new Color(0.4f, 0.42f, 0.5f);

    // ── LEADERBOARD ──────────────────────────────────────────
    private List<int> topScores = new List<int>();
    private const int MAX_SCORES = 10;
    private const string SCORES_KEY = "TopScores";

    // ── STATE ────────────────────────────────────────────────
    private enum State { Title, Playing, Rewinding, GameOver }
    private State state = State.Title;
    private float stateTimer = 0f;
    private string lastScoreText = "0";
    private bool isNewBest = false;
    private int goFinalScore = 0;
    private int goRank = 0;

    // ── CANVAS ───────────────────────────────────────────────
    private Canvas canvas;
    private Image overlayImage;
    private RawImage vignetteImage;

    // ── TITLE ────────────────────────────────────────────────
    private RectTransform titleGroup;
    private TMP_Text titleText;
    private TMP_Text titleGlowText;
    private TMP_Text subtitleText;
    private TMP_Text bestScoreText;
    private Image bestScoreLine;
    private TMP_Text tapToStartText;

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

    // ── GAME OVER ────────────────────────────────────────────
    private RectTransform gameOverGroup;
    private TMP_Text goScoreText;
    private TMP_Text goScoreGlowText;
    private TMP_Text goNewBestText;
    private TMP_Text goTapText;
    private TMP_Text[] goLeaderboardTexts;
    private const int LEADERBOARD_SHOW = 5;
    private Button goSettingsBtn;
    private TMP_Text goSettingsLabel;

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
            state = newState;
            stateTimer = 0f;

            if (state == State.Rewinding)
                OnGameOver();
            if (state == State.Playing)
            {
                lastPlayingScore = "0";
                scoreAnimTimer = -1f;
                scorePopTimer = -1f;
                scoreGlowTimer = -1f;
                currentStreak = 0;
                streakFlashTimer = -1f;
            }

            ShowGroup(state);
        }

        stateTimer += Time.deltaTime;
        AnimateState();
    }

    void OnGameOver()
    {
        if (source == null) return;
        string text = source.text;
        string scoreText = text.Contains("\n") ? text.Split('\n')[0] : text;
        lastScoreText = scoreText;

        int.TryParse(scoreText, out goFinalScore);
        goRank = InsertScore(goFinalScore);
        isNewBest = (goRank == 1);
    }

    public bool CanRestart()
    {
        if (state != State.GameOver || stateTimer < 1.0f) return false;

        Sphere sphere = FindAnyObjectByType<Sphere>();
        if (sphere != null && sphere.mRewindSystem != null
            && !sphere.mRewindSystem.IsFullyComplete())
            return false;

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

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Dark overlay
        overlayImage = CreateImage(canvasObj.transform, "Overlay",
            new Color(0.02f, 0.02f, 0.04f, 0f));
        StretchFull(overlayImage.rectTransform);
        overlayImage.raycastTarget = false;

        // Vignette overlay (procedural radial gradient)
        vignetteImage = CreateVignette(canvasObj.transform);

        BuildTitleGroup(canvasObj.transform);
        BuildPlayingGroup(canvasObj.transform);
        BuildGameOverGroup(canvasObj.transform);

        ShowGroup(State.Title);
    }

    void BuildTitleGroup(Transform parent)
    {
        titleGroup = CreateGroup(parent, "TitleGroup");

        // Glow layer behind title — colored halo
        titleGlowText = CreateText(titleGroup, "TitleGlow", "LOOPFALL",
            148, FontStyles.Bold, new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0f));
        SetAnchored(titleGlowText.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(1000, 180));

        // Main title — per-character animation driven in AnimateTitle
        titleText = CreateText(titleGroup, "Title", "LOOPFALL",
            140, FontStyles.Bold, new Color(1f, 1f, 1f, 0f));
        SetAnchored(titleText.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(1000, 160));

        // Subtitle — different parallax speed
        subtitleText = CreateText(titleGroup, "Subtitle", "ENDLESS TUNNEL RUNNER",
            28, FontStyles.Normal, new Color(DIM_TEXT.r, DIM_TEXT.g, DIM_TEXT.b, 0f));
        SetAnchored(subtitleText.rectTransform, new Vector2(0.5f, 0.64f), new Vector2(800, 50));
        subtitleText.characterSpacing = 14f;

        // Best score
        bestScoreText = CreateText(titleGroup, "BestScore", "",
            28, FontStyles.Bold, new Color(NEON_GOLD.r, NEON_GOLD.g, NEON_GOLD.b, 0f));
        SetAnchored(bestScoreText.rectTransform, new Vector2(0.5f, 0.42f), new Vector2(400, 40));
        bestScoreText.characterSpacing = 6f;

        // Thin gold line under best score — expands from center
        bestScoreLine = CreateImage(titleGroup.transform, "BestLine",
            new Color(NEON_GOLD.r, NEON_GOLD.g, NEON_GOLD.b, 0f));
        RectTransform lineRT = bestScoreLine.rectTransform;
        lineRT.anchorMin = new Vector2(0.5f, 0.405f);
        lineRT.anchorMax = new Vector2(0.5f, 0.405f);
        lineRT.pivot = new Vector2(0.5f, 0.5f);
        lineRT.sizeDelta = new Vector2(0f, 1.5f);
        lineRT.anchoredPosition = Vector2.zero;

        // Tap to start — pulsing in neon cyan
        tapToStartText = CreateText(titleGroup, "TapToStart", "TAP TO START",
            34, FontStyles.Normal, new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0f));
        SetAnchored(tapToStartText.rectTransform, new Vector2(0.5f, 0.32f), new Vector2(600, 50));
        tapToStartText.characterSpacing = 8f;
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
    }

    void BuildGameOverGroup(Transform parent)
    {
        gameOverGroup = CreateGroup(parent, "GameOverGroup");

        // Score glow layer — colored halo behind the number
        goScoreGlowText = CreateText(gameOverGroup, "GOScoreGlow", "0",
            230, FontStyles.Bold, new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0f));
        SetAnchored(goScoreGlowText.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(800, 260));

        // Main score — counts up from 0
        goScoreText = CreateText(gameOverGroup, "GOScore", "0",
            220, FontStyles.Bold, new Color(1f, 1f, 1f, 0f));
        SetAnchored(goScoreText.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(800, 250));

        // "NEW BEST" — gold shimmer
        goNewBestText = CreateText(gameOverGroup, "GONewBest", "NEW BEST",
            30, FontStyles.Normal, new Color(NEON_GOLD.r, NEON_GOLD.g, NEON_GOLD.b, 0f));
        SetAnchored(goNewBestText.rectTransform, new Vector2(0.5f, 0.475f), new Vector2(400, 40));
        goNewBestText.characterSpacing = 10f;

        // Leaderboard rows — staggered slide-in
        goLeaderboardTexts = new TMP_Text[LEADERBOARD_SHOW];
        for (int i = 0; i < LEADERBOARD_SHOW; i++)
        {
            TMP_Text row = CreateText(gameOverGroup, "LB" + i, "",
                24, FontStyles.Normal, new Color(DIM_TEXT.r, DIM_TEXT.g, DIM_TEXT.b, 0f));
            SetAnchored(row.rectTransform, new Vector2(0.5f, 0.38f - i * 0.035f), new Vector2(300, 35));
            row.characterSpacing = 2f;
            goLeaderboardTexts[i] = row;
        }

        // Tap to play again
        goTapText = CreateText(gameOverGroup, "GOTap", "TAP TO PLAY",
            28, FontStyles.Normal, new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0f));
        SetAnchored(goTapText.rectTransform, new Vector2(0.5f, 0.12f), new Vector2(500, 45));
        goTapText.characterSpacing = 8f;

        // Settings button — top right corner
        GameObject settingsObj = new GameObject("SettingsBtn");
        RectTransform settingsRT = settingsObj.AddComponent<RectTransform>();
        settingsRT.SetParent(gameOverGroup, false);
        settingsRT.anchorMin = new Vector2(1, 1);
        settingsRT.anchorMax = new Vector2(1, 1);
        settingsRT.pivot = new Vector2(1, 1);
        settingsRT.anchoredPosition = new Vector2(-40, -40);
        settingsRT.sizeDelta = new Vector2(80, 80);
        Image settingsImg = settingsObj.AddComponent<Image>();
        settingsImg.color = new Color(0, 0, 0, 0.01f);
        goSettingsBtn = settingsObj.AddComponent<Button>();

        goSettingsLabel = CreateText(settingsRT, "Lbl", "\u2699",
            40, FontStyles.Normal, new Color(DIM_TEXT.r, DIM_TEXT.g, DIM_TEXT.b, 0f));
        StretchFull(goSettingsLabel.rectTransform);
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
        SetGroupActive(titleGroup, s == State.Title);
        SetGroupActive(playingGroup, s == State.Playing);
        SetGroupActive(gameOverGroup, s == State.GameOver);

        // Clear overlays when entering non-overlay states
        if (s == State.Playing)
        {
            overlayImage.color = new Color(0.02f, 0.02f, 0.04f, 0f);
            vignetteImage.color = new Color(1f, 1f, 1f, 0f);
        }
        else if (s == State.Title)
        {
            overlayImage.color = new Color(0.02f, 0.02f, 0.04f, 0f);
            // Vignette will fade in via AnimateTitle
            vignetteImage.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    void AnimateState()
    {
        switch (state)
        {
            case State.Title: AnimateTitle(); break;
            case State.Playing: AnimatePlaying(); break;
            case State.Rewinding: AnimateRewinding(); break;
            case State.GameOver: AnimateGameOver(); break;
        }
    }

    // ── TITLE ────────────────────────────────────────────────

    void AnimateTitle()
    {
        // Per-character staggered elastic entrance
        AnimateTitleCharacters();

        // Glow layer — breathes with slow cyan/magenta color shift
        // Delayed until most letters are visible
        float glowFade = Mathf.Clamp01((stateTimer - 0.8f) / 0.7f);
        float glowBreath = 0.10f + Mathf.Sin(Time.time * 1.2f) * 0.04f;
        Color glowColor = Color.Lerp(NEON_CYAN, NEON_MAGENTA,
            Mathf.Sin(Time.time * 0.5f) * 0.5f + 0.5f);
        titleGlowText.color = new Color(glowColor.r, glowColor.g, glowColor.b,
            glowBreath * glowFade);
        // Glow drifts at slower parallax
        float glowFloat = Mathf.Sin(Time.time * 0.5f) * 2f;
        titleGlowText.rectTransform.anchoredPosition = new Vector2(0, glowFloat);

        // Subtitle — delayed fade, different parallax speed
        float subFade = Mathf.Clamp01((stateTimer - 0.5f) / 0.8f);
        SetAlpha(subtitleText, subFade * 0.5f);
        float subFloat = Mathf.Sin(Time.time * 0.6f) * 2f;
        subtitleText.rectTransform.anchoredPosition = new Vector2(0, subFloat);

        // Best score with expanding gold line
        if (topScores.Count > 0)
        {
            float bestFade = Mathf.Clamp01((stateTimer - 0.8f) / 0.6f);
            bestScoreText.text = "BEST  " + topScores[0];
            SetAlpha(bestScoreText, bestFade * 0.6f);

            float lineWidth = Mathf.Lerp(0f, 120f, EaseOutCubic(bestFade));
            bestScoreLine.rectTransform.sizeDelta = new Vector2(lineWidth, 1.5f);
            Color lc = bestScoreLine.color;
            lc.a = bestFade * 0.4f;
            bestScoreLine.color = lc;
        }
        else
        {
            SetAlpha(bestScoreText, 0f);
            Color lc = bestScoreLine.color;
            lc.a = 0f;
            bestScoreLine.color = lc;
        }

        // Tap to start — pulsing cyan
        if (stateTimer > 1.2f)
        {
            float pulse = 0.3f + Mathf.Sin(Time.time * 2.5f) * 0.2f;
            SetAlpha(tapToStartText, pulse);
        }
        else
        {
            SetAlpha(tapToStartText, 0f);
        }

        // Subtle vignette on title for atmosphere
        float vigFade = Mathf.Clamp01(stateTimer / 2f);
        vignetteImage.color = new Color(1f, 1f, 1f, vigFade * 0.25f);
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

            byte alpha = (byte)(Mathf.Clamp01(charAlpha) * 230);
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

        // Vignette: fade in
        float vigAlpha = Mathf.Clamp01(t / 0.6f) * 0.8f;
        vignetteImage.color = new Color(1f, 1f, 1f, vigAlpha);

        // Overlay: subtle dark fill
        float overlayAlpha = Mathf.Clamp01(t / 0.5f) * 0.45f;
        overlayImage.color = new Color(0.02f, 0.02f, 0.04f, overlayAlpha);

        // Score count-up from 0 to final value
        if (t > 0.3f)
        {
            // Duration scales with score (0.3s min, 1.2s max)
            float countDuration = Mathf.Clamp(goFinalScore * 0.04f, 0.3f, 1.2f);
            float countP = Mathf.Clamp01((t - 0.3f) / countDuration);
            float countEased = EaseOutCubic(countP);
            int displayScore = Mathf.RoundToInt(goFinalScore * countEased);
            goScoreText.text = displayScore.ToString();
            goScoreGlowText.text = displayScore.ToString();

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
        }
        else
        {
            SetAlpha(goScoreText, 0f);
            goScoreGlowText.color = new Color(0, 0, 0, 0f);
        }

        // "NEW BEST" — gold with double-frequency shimmer
        if (isNewBest && t > 1.0f)
        {
            float p = Mathf.Clamp01((t - 1.0f) / 0.4f);
            float shimmer = 0.6f + Mathf.Sin(Time.time * 3f) * 0.2f
                          + Mathf.Sin(Time.time * 7f) * 0.05f;
            SetAlpha(goNewBestText, EaseOutCubic(p) * shimmer);
        }
        else
        {
            SetAlpha(goNewBestText, 0f);
        }

        // Mini leaderboard — staggered slide-in
        AnimateLeaderboard(t);

        // Settings gear
        if (t > 0.6f)
        {
            float p = Mathf.Clamp01((t - 0.6f) / 0.4f);
            SetAlpha(goSettingsLabel, p * 0.35f);
        }
        else
        {
            SetAlpha(goSettingsLabel, 0f);
        }

        // Tap to play — pulsing cyan
        if (t > 1.8f)
        {
            float pulse = 0.25f + Mathf.Sin(Time.time * 2.5f) * 0.18f;
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

            goLeaderboardTexts[i].text = (i + 1) + ".   " + score;

            // Color hierarchy: current run = cyan, #1 = gold, rest = dim
            Color rowColor;
            float maxAlpha;
            if (isCurrent)
            {
                rowColor = NEON_CYAN;
                maxAlpha = 0.85f;
            }
            else if (i == 0)
            {
                rowColor = NEON_GOLD;
                maxAlpha = 0.5f;
            }
            else
            {
                rowColor = DIM_TEXT;
                maxAlpha = 0.35f;
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
        string saved = PlayerPrefs.GetString(SCORES_KEY, "");
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
        PlayerPrefs.SetString(SCORES_KEY, joined);
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
        return rank <= MAX_SCORES ? rank : 0;
    }

    public void ClearAllScores()
    {
        topScores.Clear();
        PlayerPrefs.DeleteKey(SCORES_KEY);
        PlayerPrefs.Save();
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
