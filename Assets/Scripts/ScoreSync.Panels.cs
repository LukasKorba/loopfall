using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public partial class ScoreSync
{
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
        cardRT.sizeDelta = showDisplay ? new Vector2(792, 880) : new Vector2(792, 640);

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(0.06f, 0.03f, 0.10f, 0.96f);
        cardBg.raycastTarget = true;

        // ── Cyan border frame ──
        CreateSettingsBorderEdge(cardRT, "BorderTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, 2f));
        CreateSettingsBorderEdge(cardRT, "BorderBot", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(0f, 2f));
        CreateSettingsBorderEdge(cardRT, "BorderL", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(2f, 0f));
        CreateSettingsBorderEdge(cardRT, "BorderR", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(2f, 0f));

        // Title
        TMP_Text title = CreateText(cardRT, "Title", L10n.T("settings.title"),
            36, FontStyles.Bold, NEON_CYAN);
        SetAnchored(title.rectTransform, new Vector2(0.5f, showDisplay ? 0.93f : 0.88f), new Vector2(640,50));
        title.characterSpacing = 8f;
        title.raycastTarget = false;

        // Title underline accent
        CreateSettingsDivider(cardRT, showDisplay ? 0.885f : 0.81f, NEON_CYAN, 0.25f);

        if (showDisplay)
        {
            // ── AUDIO SECTION ──
            TMP_Text audioHeader = CreateText(cardRT, "AudioHeader", L10n.T("settings.section.audio"),
                18, FontStyles.Bold, new Color(0.45f, 0.48f, 0.55f));
            SetAnchored(audioHeader.rectTransform, new Vector2(0.5f, 0.83f), new Vector2(640,28));
            audioHeader.characterSpacing = 6f;
            audioHeader.raycastTarget = false;

            Button soundBtn = CreateSettingsIconToggle(cardRT, "SoundBtn", L10n.T("settings.sounds"),
                new Vector2(0.5f, 0.76f), out settingsSoundIcon);
            soundBtn.onClick.AddListener(ToggleSound);

            Button musicBtn = CreateSettingsIconToggle(cardRT, "MusicBtn", L10n.T("settings.music"),
                new Vector2(0.5f, 0.68f), out settingsMusicIcon);
            musicBtn.onClick.AddListener(ToggleMusic);

            // Section divider between Audio and Display
            CreateSettingsDivider(cardRT, 0.625f, DIM_TEXT, 0.08f);

            // ── DISPLAY SECTION ──
            TMP_Text displayHeader = CreateText(cardRT, "DisplayHeader", L10n.T("settings.section.display"),
                18, FontStyles.Bold, new Color(0.45f, 0.48f, 0.55f));
            SetAnchored(displayHeader.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(640,28));
            displayHeader.characterSpacing = 6f;
            displayHeader.raycastTarget = false;

            Button resBtn = CreateSettingsToggle(cardRT, "ResBtn",
                new Vector2(0.5f, 0.51f), out settingsResLabel);
            resBtn.onClick.AddListener(OnCycleResolution);

            Button fullBtn = CreateSettingsToggle(cardRT, "FullBtn",
                new Vector2(0.5f, 0.43f), out settingsFullscreenLabel);
            fullBtn.onClick.AddListener(OnToggleFullscreen);

            // ── PREFERENCES SECTION ──
            CreateSettingsDivider(cardRT, 0.38f, DIM_TEXT, 0.08f);

            TMP_Text prefsHeader = CreateText(cardRT, "PrefsHeader", L10n.T("settings.section.preferences"),
                18, FontStyles.Bold, new Color(0.45f, 0.48f, 0.55f));
            SetAnchored(prefsHeader.rectTransform, new Vector2(0.5f, 0.34f), new Vector2(640,28));
            prefsHeader.characterSpacing = 6f;
            prefsHeader.raycastTarget = false;

            CreateSettingsCycleRow(cardRT, "ThemeRow", L10n.T("settings.theme"),
                new Vector2(0.5f, 0.27f), OnThemePrev, OnThemeNext, out settingsThemeLabel);

            CreateSettingsCycleRow(cardRT, "LanguageRow", L10n.T("settings.language"),
                new Vector2(0.5f, 0.19f), OnLanguagePrev, OnLanguageNext, out settingsLanguageLabel);

            CreateSettingsCycleRow(cardRT, "MotionRow", L10n.T("settings.motion"),
                new Vector2(0.5f, 0.11f), OnReduceMotionPrev, OnReduceMotionNext, out settingsMotionLabel);
        }
        else
        {
            // Mobile layout — sounds, music, theme, language, motion
            Button soundBtn = CreateSettingsIconToggle(cardRT, "SoundBtn", L10n.T("settings.sounds"),
                new Vector2(0.5f, 0.72f), out settingsSoundIcon);
            soundBtn.onClick.AddListener(ToggleSound);

            Button musicBtn = CreateSettingsIconToggle(cardRT, "MusicBtn", L10n.T("settings.music"),
                new Vector2(0.5f, 0.58f), out settingsMusicIcon);
            musicBtn.onClick.AddListener(ToggleMusic);

            CreateSettingsDivider(cardRT, 0.495f, DIM_TEXT, 0.08f);

            CreateSettingsCycleRow(cardRT, "ThemeRow", L10n.T("settings.theme"),
                new Vector2(0.5f, 0.41f), OnThemePrev, OnThemeNext, out settingsThemeLabel);

            CreateSettingsCycleRow(cardRT, "LanguageRow", L10n.T("settings.language"),
                new Vector2(0.5f, 0.27f), OnLanguagePrev, OnLanguageNext, out settingsLanguageLabel);

            CreateSettingsCycleRow(cardRT, "MotionRow", L10n.T("settings.motion"),
                new Vector2(0.5f, 0.16f), OnReduceMotionPrev, OnReduceMotionNext, out settingsMotionLabel);
        }

        // Close label
        TMP_Text closeLabel = CreateText(cardRT, "Close", GetCloseHint(),
            16, FontStyles.Normal, new Color(DIM_TEXT.r, DIM_TEXT.g, DIM_TEXT.b, 0.5f));
        SetAnchored(closeLabel.rectTransform, new Vector2(0.5f, showDisplay ? 0.04f : 0.05f), new Vector2(640,30));
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

    // Label on the left, a Phosphor glyph on the right — used for SOUNDS/MUSIC toggles.
    // The glyph itself reflects state (speaker-high = on, speaker-x = off); the whole
    // row background + accent bar animate via StyleSettingsToggle.
    Button CreateSettingsIconToggle(RectTransform parent, string name, string labelText,
        Vector2 anchor, out TMP_Text iconGlyph)
    {
        GameObject btnObj = new GameObject(name);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(640,55);

        Image bg = btnObj.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.04f);
        bg.raycastTarget = true;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.10f);
        cb.pressedColor = new Color(1f, 1f, 1f, 0.18f);
        btn.colors = cb;

        // Left accent bar — reflects on/off state via StyleSettingsToggle
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

        // Static label on the left
        TMP_Text label = CreateText(rt, "Label", labelText, 24, FontStyles.Normal, Color.white);
        RectTransform lblRT = label.rectTransform;
        lblRT.anchorMin = new Vector2(0f, 0f);
        lblRT.anchorMax = new Vector2(1f, 1f);
        lblRT.offsetMin = new Vector2(20f, 0f);
        lblRT.offsetMax = new Vector2(-60f, 0f);
        label.alignment = TextAlignmentOptions.Left;
        label.characterSpacing = 4f;
        label.raycastTarget = false;

        // Phosphor glyph on the right — text is set by RefreshSettingsLabels
        GameObject glyphObj = new GameObject("IconGlyph");
        RectTransform glyphRT = glyphObj.AddComponent<RectTransform>();
        glyphRT.SetParent(rt, false);
        glyphRT.anchorMin = new Vector2(1f, 0.5f);
        glyphRT.anchorMax = new Vector2(1f, 0.5f);
        glyphRT.pivot = new Vector2(1f, 0.5f);
        glyphRT.anchoredPosition = new Vector2(-16f, 0f);
        glyphRT.sizeDelta = new Vector2(42f, 42f);

        iconGlyph = glyphObj.AddComponent<TextMeshProUGUI>();
        if (phosphorFont != null) iconGlyph.font = phosphorFont;
        iconGlyph.text = PHOSPHOR_SPEAKER_HIGH;
        iconGlyph.color = Color.white;
        iconGlyph.alignment = TextAlignmentOptions.Center;
        iconGlyph.enableWordWrapping = false;
        iconGlyph.overflowMode = TextOverflowModes.Overflow;
        iconGlyph.raycastTarget = false;
        iconGlyph.enableAutoSizing = true;
        iconGlyph.fontSizeMin = 8f;
        iconGlyph.fontSizeMax = 60f;

        return btn;
    }

    // Label on the left, < VALUE > cycle cluster on the right — used for THEME/LANGUAGE.
    // Arrows are separate buttons; the row itself is not clickable (only the arrows are).
    void CreateSettingsCycleRow(RectTransform parent, string name, string labelText,
        Vector2 anchor, UnityEngine.Events.UnityAction onPrev, UnityEngine.Events.UnityAction onNext,
        out TMP_Text valueLabel)
    {
        GameObject container = new GameObject(name);
        RectTransform crt = container.AddComponent<RectTransform>();
        crt.SetParent(parent, false);
        crt.anchorMin = anchor;
        crt.anchorMax = anchor;
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(640,55);

        Image bg = container.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.04f);
        bg.raycastTarget = true;

        // Tapping the row cycles forward (same as `>`). Without a Button, pointer clicks
        // bubble up the hierarchy to the panel's close-on-tap Button and dismiss settings.
        Button rowBtn = container.AddComponent<Button>();
        ColorBlock rowColors = rowBtn.colors;
        rowColors.normalColor = Color.white;
        rowColors.highlightedColor = new Color(1f, 1f, 1f, 0.10f);
        rowColors.pressedColor = new Color(1f, 1f, 1f, 0.18f);
        rowBtn.colors = rowColors;
        rowBtn.onClick.AddListener(onNext);

        // Left accent bar — always cyan (cycles are always "active")
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

        // Static label on the left
        TMP_Text label = CreateText(crt, "Label", labelText, 24, FontStyles.Normal, Color.white);
        RectTransform lblRT = label.rectTransform;
        lblRT.anchorMin = new Vector2(0f, 0f);
        lblRT.anchorMax = new Vector2(0.5f, 1f);
        lblRT.offsetMin = new Vector2(20f, 0f);
        lblRT.offsetMax = Vector2.zero;
        label.alignment = TextAlignmentOptions.Left;
        label.characterSpacing = 4f;
        label.raycastTarget = false;

        // Right-side cluster: < VALUE >
        // Right arrow — anchored to right edge
        CreateCycleArrow(crt, "Next", ">", new Vector2(-8f, 0f), onNext);
        // Left arrow — sits to the left of the 200px value label (widened to fit
        // 10-char language names like NEDERLANDS / УКРАЇНСЬКА without clipping).
        CreateCycleArrow(crt, "Prev", "<", new Vector2(-8f - 44f - 200f, 0f), onPrev);

        // Value label — centered between the two arrows
        valueLabel = CreateText(crt, "Value", "", 22, FontStyles.Normal, Color.white);
        RectTransform vrt = valueLabel.rectTransform;
        vrt.anchorMin = new Vector2(1f, 0f);
        vrt.anchorMax = new Vector2(1f, 1f);
        vrt.pivot = new Vector2(1f, 0.5f);
        vrt.anchoredPosition = new Vector2(-8f - 44f, 0f);
        vrt.sizeDelta = new Vector2(200f, 0f);
        valueLabel.alignment = TextAlignmentOptions.Center;
        valueLabel.characterSpacing = 4f;
        valueLabel.raycastTarget = false;
    }

    void CreateCycleArrow(RectTransform parent, string name, string arrowText,
        Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = new GameObject(name);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(44f, 0f);

        Image btnBg = obj.AddComponent<Image>();
        btnBg.color = new Color(1f, 1f, 1f, 0f);
        btnBg.raycastTarget = true;

        Button btn = obj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.10f);
        cb.pressedColor = new Color(1f, 1f, 1f, 0.18f);
        btn.colors = cb;
        btn.onClick.AddListener(onClick);

        TMP_Text arrow = CreateText(rt, "Arrow", arrowText, 28, FontStyles.Bold, NEON_CYAN);
        StretchFull(arrow.rectTransform);
    }

    Button CreateSettingsToggle(RectTransform parent, string name, Vector2 anchor, out TMP_Text label)
    {
        GameObject btnObj = new GameObject(name);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(640,55);

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
        // Rebuild every open so mode switches (Pure Hell ↔ Blitz) show the right rows
        if (statsPanel != null)
        {
            Destroy(statsPanel.gameObject);
            statsPanel = null;
        }
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
        TMP_Text title = CreateText(cardRT, "Title", L10n.T("stats.title"),
            36, FontStyles.Bold, NEON_CYAN);
        SetAnchored(title.rectTransform, new Vector2(0.5f, 0.90f), new Vector2(400, 50));
        title.characterSpacing = 8f;
        title.raycastTarget = false;

        CreateSettingsDivider(cardRT, 0.84f, NEON_CYAN, 0.25f);

        // Stat rows — label left, value right. Blitz swaps GATES for OBSTACLES.
        statsRunsLabel = CreateStatsRow(cardRT, "Runs", L10n.T("stats.total_runs"), 0.72f);
        statsTapsLabel = CreateStatsRow(cardRT, "Taps", L10n.T("stats.total_taps"), 0.60f);
        statsBestLabel = CreateStatsRow(cardRT, "Best", L10n.T("stats.best_score"), 0.48f);
        statsAvgLabel = CreateStatsRow(cardRT, "Avg", L10n.T("stats.avg_score"), 0.36f);
        if (GameConfig.IsBlitz())
        {
            statsObstaclesLabel = CreateStatsRow(cardRT, "Obstacles", L10n.T("stats.obstacles"), 0.24f);
            statsGatesLabel = null;
        }
        else
        {
            statsGatesLabel = CreateStatsRow(cardRT, "Gates", L10n.T("stats.total_gates"), 0.24f);
            statsObstaclesLabel = null;
        }

        // Close hint
        TMP_Text closeLabel = CreateText(cardRT, "Close", GetCloseHint(),
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
        bool blitz = GameConfig.IsBlitz();
        int runs = blitz ? Sphere.GetBlitzRuns() : Sphere.GetTotalRuns();
        int taps = blitz ? Sphere.GetBlitzTaps() : Sphere.GetTotalTaps();
        int best = topScores.Count > 0 ? topScores[0] : 0;
        int totalScore = 0;
        for (int i = 0; i < topScores.Count; i++)
            totalScore += topScores[i];
        int avg = topScores.Count > 0 ? totalScore / topScores.Count : 0;

        if (statsRunsLabel != null) statsRunsLabel.text = runs.ToString("N0");
        if (statsTapsLabel != null) statsTapsLabel.text = taps.ToString("N0");
        if (statsBestLabel != null) statsBestLabel.text = best.ToString("N0");
        if (statsAvgLabel != null) statsAvgLabel.text = avg.ToString("N0");

        if (blitz)
        {
            if (statsObstaclesLabel != null)
                statsObstaclesLabel.text = Sphere.GetBlitzObstacles().ToString("N0");
        }
        else
        {
            // Pure Hell: each gate = 1 point, so sum of Top-N scores approximates gates cleared.
            if (statsGatesLabel != null) statsGatesLabel.text = totalScore.ToString("N0");
        }
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

    void OnThemeNext()
    {
        ThemeData[] themes = ThemeData.All();
        int current = ThemeData.LoadSavedIndex();
        // Cycle order: AUTO (-1) → 0 → 1 → ... → last → AUTO
        int next;
        if (current == ThemeData.AUTO_INDEX) next = 0;
        else if (current >= themes.Length - 1) next = ThemeData.AUTO_INDEX;
        else next = current + 1;
        ApplyThemeSelection(next);
    }

    void OnThemePrev()
    {
        ThemeData[] themes = ThemeData.All();
        int current = ThemeData.LoadSavedIndex();
        int prev;
        if (current == ThemeData.AUTO_INDEX) prev = themes.Length - 1;
        else if (current == 0) prev = ThemeData.AUTO_INDEX;
        else prev = current - 1;
        ApplyThemeSelection(prev);
    }

    void OnReduceMotionNext()
    {
        int cur = (int)AccessibilitySettings.CurrentMode;
        AccessibilitySettings.CurrentMode = (AccessibilitySettings.Mode)((cur + 1) % 3);
        RefreshSettingsLabels();
    }

    void OnReduceMotionPrev()
    {
        int cur = (int)AccessibilitySettings.CurrentMode;
        AccessibilitySettings.CurrentMode = (AccessibilitySettings.Mode)((cur + 2) % 3);
        RefreshSettingsLabels();
    }

}
