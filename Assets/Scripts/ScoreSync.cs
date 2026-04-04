using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ScoreSync : MonoBehaviour
{
    public TextMesh source;

    // Leaderboard
    private List<int> topScores = new List<int>();
    private const int MAX_SCORES = 10;
    private const string SCORES_KEY = "TopScores";

    // State
    private enum State { Title, Playing, Rewinding, GameOver }
    private State state = State.Title;
    private float stateTimer = 0f;
    private string lastScoreText = "0";
    private bool isNewBest = false;

    // Canvas
    private Canvas canvas;

    // Overlay
    private Image overlayImage;

    // Title elements
    private RectTransform titleGroup;
    private TMP_Text titleText;
    private TMP_Text subtitleText;
    private TMP_Text bestScoreText;
    private TMP_Text tapToStartText;

    // Playing elements
    private RectTransform playingGroup;
    private TMP_Text playingScoreText;
    private TMP_Text playingScoreTextOut; // Old score sliding out
    private string lastPlayingScore = "0";
    private float scoreAnimTimer = -1f;
    private const float SCORE_ANIM_DURATION = 0.25f;
    private const float SCORE_SLIDE_DISTANCE = 60f;
    private float scorePopTimer = -1f;
    private const float SCORE_POP_DURATION = 0.3f;
    private const float SCORE_POP_SCALE = 1.35f;

    // Game Over elements
    private RectTransform gameOverGroup;
    private TMP_Text goScoreText;
    private TMP_Text goNewBestText;
    private TMP_Text goTapText;
    private Button goSettingsBtn;
    private TMP_Text goSettingsLabel;

    // Debug
    private Button debugClearBtn;

    // Font
    private TMP_FontAsset defaultFont;

    void Start()
    {
        LoadScores();
        defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
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

        int score = 0;
        int.TryParse(scoreText, out score);

        int rank = InsertScore(score);
        isNewBest = (rank == 1);
    }

    public bool CanRestart()
    {
        if (state != State.GameOver || stateTimer < 1.0f) return false;

        // Wait for obstacle swap animation to finish
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

        // Dark overlay (game over fade)
        overlayImage = CreateImage(canvasObj.transform, "Overlay",
            new Color(0.02f, 0.02f, 0.04f, 0f));
        StretchFull(overlayImage.rectTransform);
        overlayImage.raycastTarget = false;

        // ── TITLE ──
        titleGroup = CreateGroup(canvasObj.transform, "TitleGroup");

        titleText = CreateText(titleGroup, "Title", "LOOPFALL",
            140, FontStyles.Bold, new Color(1f, 1f, 1f, 0f));
        SetAnchored(titleText.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(1000, 160));

        subtitleText = CreateText(titleGroup, "Subtitle", "ENDLESS TUNNEL RUNNER",
            28, FontStyles.Normal, new Color(0.5f, 0.55f, 0.65f, 0f));
        SetAnchored(subtitleText.rectTransform, new Vector2(0.5f, 0.64f), new Vector2(800, 50));
        subtitleText.characterSpacing = 14f;

        bestScoreText = CreateText(titleGroup, "BestScore", "",
            26, FontStyles.Bold, new Color(0.9f, 0.75f, 0.2f, 0f));
        SetAnchored(bestScoreText.rectTransform, new Vector2(0.5f, 0.42f), new Vector2(400, 40));
        bestScoreText.characterSpacing = 4f;

        tapToStartText = CreateText(titleGroup, "TapToStart", "TAP TO START",
            34, FontStyles.Normal, new Color(0.8f, 0.8f, 0.85f, 0f));
        SetAnchored(tapToStartText.rectTransform, new Vector2(0.5f, 0.32f), new Vector2(600, 50));
        tapToStartText.characterSpacing = 8f;

        // ── PLAYING ──
        playingGroup = CreateGroup(canvasObj.transform, "PlayingGroup");

        playingScoreText = CreateText(playingGroup, "Score", "0",
            100, FontStyles.Bold, new Color(1f, 1f, 1f, 0.25f));
        SetAnchored(playingScoreText.rectTransform, new Vector2(0.5f, 0.93f), new Vector2(600, 120));

        playingScoreTextOut = CreateText(playingGroup, "ScoreOut", "",
            100, FontStyles.Bold, new Color(1f, 1f, 1f, 0f));
        SetAnchored(playingScoreTextOut.rectTransform, new Vector2(0.5f, 0.93f), new Vector2(600, 120));

        // ── GAME OVER ──
        gameOverGroup = CreateGroup(canvasObj.transform, "GameOverGroup");

        // The score — big, centered, the hero of the screen
        goScoreText = CreateText(gameOverGroup, "GOScore", "0",
            220, FontStyles.Bold, new Color(1f, 1f, 1f, 0f));
        SetAnchored(goScoreText.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(800, 250));

        // "NEW BEST" — small, gold, only shown when earned
        goNewBestText = CreateText(gameOverGroup, "GONewBest", "NEW BEST",
            30, FontStyles.Normal, new Color(1f, 0.85f, 0.1f, 0f));
        SetAnchored(goNewBestText.rectTransform, new Vector2(0.5f, 0.44f), new Vector2(400, 40));
        goNewBestText.characterSpacing = 10f;

        // Tap to play again
        goTapText = CreateText(gameOverGroup, "GOTap", "TAP TO PLAY",
            28, FontStyles.Normal, new Color(0.6f, 0.6f, 0.65f, 0f));
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
        // TODO: wire to settings screen

        goSettingsLabel = CreateText(settingsRT, "Lbl", "\u2699",
            40, FontStyles.Normal, new Color(0.5f, 0.5f, 0.55f, 0f));
        StretchFull(goSettingsLabel.rectTransform);

        // ── DEBUG CLR ──
        GameObject btnObj = new GameObject("DebugClear");
        RectTransform btnRT = btnObj.AddComponent<RectTransform>();
        btnRT.SetParent(canvasObj.transform, false);
        btnRT.anchorMin = new Vector2(0, 0);
        btnRT.anchorMax = new Vector2(0, 0);
        btnRT.pivot = new Vector2(0, 0);
        btnRT.anchoredPosition = new Vector2(10, 10);
        btnRT.sizeDelta = new Vector2(80, 40);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0, 0, 0, 0.01f);
        debugClearBtn = btnObj.AddComponent<Button>();
        debugClearBtn.onClick.AddListener(ClearAllScores);

        TMP_Text btnLabel = CreateText(btnRT, "Lbl", "CLR",
            14, FontStyles.Normal, new Color(0.4f, 0.4f, 0.4f, 0.3f));
        StretchFull(btnLabel.rectTransform);

        // Start in title
        ShowGroup(State.Title);
    }

    // ── STATE DISPLAY ────────────────────────────────────────

    void ShowGroup(State s)
    {
        SetGroupActive(titleGroup, s == State.Title);
        SetGroupActive(playingGroup, s == State.Playing);
        SetGroupActive(gameOverGroup, s == State.GameOver);
        // Overlay visible during Rewinding and GameOver, hidden otherwise
        if (s != State.Rewinding && s != State.GameOver)
            overlayImage.color = new Color(0.02f, 0.02f, 0.04f, 0f);
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
        float fadeIn = Mathf.Clamp01(stateTimer / 1.2f);

        // Title — gentle float
        float floatY = Mathf.Sin(Time.time * 0.8f) * 4f;
        titleText.rectTransform.anchoredPosition = new Vector2(0, floatY);
        SetAlpha(titleText, fadeIn * 0.9f);

        // Subtitle — delayed fade
        float subFade = Mathf.Clamp01((stateTimer - 0.3f) / 0.8f);
        SetAlpha(subtitleText, subFade * 0.45f);

        // Best score
        if (topScores.Count > 0)
        {
            float bestFade = Mathf.Clamp01((stateTimer - 0.6f) / 0.6f);
            bestScoreText.text = "BEST  " + topScores[0];
            SetAlpha(bestScoreText, bestFade * 0.55f);
        }
        else
        {
            SetAlpha(bestScoreText, 0f);
        }

        // Tap to start — pulsing, delayed
        if (stateTimer > 1.0f)
        {
            float pulse = 0.35f + Mathf.Sin(Time.time * 2.5f) * 0.25f;
            SetAlpha(tapToStartText, pulse);
        }
        else
        {
            SetAlpha(tapToStartText, 0f);
        }
    }

    // ── PLAYING ──────────────────────────────────────────────

    void AnimatePlaying()
    {
        if (source == null) return;
        string text = source.text;
        string scoreOnly = text.Contains("\n") ? text.Split('\n')[0] : text;

        // Detect score change — trigger crossfade + pop
        if (scoreOnly != lastPlayingScore)
        {
            // Old score starts sliding down + fading out
            playingScoreTextOut.text = lastPlayingScore;
            playingScoreTextOut.rectTransform.anchoredPosition = Vector2.zero;

            // New score will slide in from top
            playingScoreText.text = scoreOnly;

            scoreAnimTimer = 0f;
            scorePopTimer = 0f;
            lastPlayingScore = scoreOnly;
        }

        // Animate the crossfade
        if (scoreAnimTimer >= 0f && scoreAnimTimer < SCORE_ANIM_DURATION)
        {
            scoreAnimTimer += Time.deltaTime;
            float p = Mathf.Clamp01(scoreAnimTimer / SCORE_ANIM_DURATION);
            float eased = EaseOutCubic(p);

            // New score: slides down from above, fades in
            float inY = Mathf.Lerp(SCORE_SLIDE_DISTANCE, 0f, eased);
            playingScoreText.rectTransform.anchoredPosition = new Vector2(0, inY);
            SetAlpha(playingScoreText, eased * 0.8f);

            // Old score: slides down and out, fades out
            float outY = Mathf.Lerp(0f, -SCORE_SLIDE_DISTANCE, eased);
            playingScoreTextOut.rectTransform.anchoredPosition = new Vector2(0, outY);
            SetAlpha(playingScoreTextOut, (1f - eased) * 0.8f);
        }
        else
        {
            // Steady state
            playingScoreText.text = scoreOnly;
            playingScoreText.rectTransform.anchoredPosition = Vector2.zero;
            float alpha = scoreOnly == "0" ? 0.2f : 0.8f;
            SetAlpha(playingScoreText, alpha);
            SetAlpha(playingScoreTextOut, 0f);
        }

        // Scale pop — punchy overshoot then settle
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
    }

    // ── REWINDING ────────────────────────────────────────────

    void AnimateRewinding()
    {
        // No overlay during rewind — let the player see the path clearly
    }

    // ── GAME OVER ────────────────────────────────────────────

    void AnimateGameOver()
    {
        if (goScoreText == null) return;
        float t = stateTimer;

        // Overlay: quick fade to dark
        float overlayAlpha = Mathf.Clamp01(t / 0.5f) * 0.6f;
        overlayImage.color = new Color(0.02f, 0.02f, 0.04f, overlayAlpha);

        // Score: fade in + subtle scale
        if (t > 0.4f)
        {
            float p = Mathf.Clamp01((t - 0.4f) / 0.5f);
            float eased = EaseOutCubic(p);

            goScoreText.text = lastScoreText;
            SetAlpha(goScoreText, eased);

            float scale = Mathf.Lerp(1.15f, 1.0f, eased);
            goScoreText.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            SetAlpha(goScoreText, 0f);
        }

        // "NEW BEST" — only if earned, subtle fade
        if (isNewBest && t > 0.9f)
        {
            float p = Mathf.Clamp01((t - 0.9f) / 0.4f);
            float glow = 0.7f + Mathf.Sin(Time.time * 3f) * 0.15f;
            SetAlpha(goNewBestText, p * glow);
        }
        else
        {
            SetAlpha(goNewBestText, 0f);
        }

        // Settings gear — fade with overlay
        if (t > 0.6f)
        {
            float p = Mathf.Clamp01((t - 0.6f) / 0.4f);
            SetAlpha(goSettingsLabel, p * 0.4f);
        }
        else
        {
            SetAlpha(goSettingsLabel, 0f);
        }

        // Tap to play — pulsing, after restart is allowed
        if (t > 1.5f)
        {
            float pulse = 0.25f + Mathf.Sin(Time.time * 2.5f) * 0.2f;
            SetAlpha(goTapText, pulse);
        }
        else
        {
            SetAlpha(goTapText, 0f);
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
}
