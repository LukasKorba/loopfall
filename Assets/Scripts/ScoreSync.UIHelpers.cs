using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public partial class ScoreSync
{
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

    Sprite CreateRingSprite(int size, float strokeThickness)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f;
        float outerR = half - 1f;
        float innerR = outerR - strokeThickness;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(half, half));
                float outerA = 1f - Mathf.Clamp01(dist - outerR);
                float innerA = Mathf.Clamp01(dist - innerR);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, outerA * innerA));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f);
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

#if UNITY_IOS && !UNITY_TVOS
    // Inset a stretched group by Screen.safeArea so notch/rounded corners don't clip the
    // HUD. Children remain anchored to the group, so they shift inward uniformly.
    // Gameplay-only — title/game-over screens keep edge-to-edge by design.
    void ApplySafeAreaInsets(RectTransform rt)
    {
        Rect safe = Screen.safeArea;
        float w = Screen.width;
        float h = Screen.height;
        if (w <= 0f || h <= 0f) return;
        rt.anchorMin = new Vector2(safe.xMin / w, safe.yMin / h);
        rt.anchorMax = new Vector2(safe.xMax / w, safe.yMax / h);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
#endif

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

    // Primary mode-pick button: dark bg, 2px neon edge frame, 4 corner brackets,
    // left accent bar, centered bold label. Port from navigation-prototype-backup.
    Button CreatePrimaryButton(RectTransform parent, string name, Vector2 anchor,
        Vector2 size, string text, Color neon, out TMP_Text label)
    {
        GameObject btnObj = new GameObject(name);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        // CanvasGroup lets callers fade the entire button as one unit.
        btnObj.AddComponent<CanvasGroup>();

        Image bg = btnObj.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.03f, 0.09f, 0.78f);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;

        Color edgeCol = new Color(neon.r, neon.g, neon.b, 0.85f);
        CreateButtonEdge(rt, "EdgeT", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 2f), edgeCol);
        CreateButtonEdge(rt, "EdgeB", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f), edgeCol);
        CreateButtonEdge(rt, "EdgeL", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(2f, 0f), edgeCol);
        CreateButtonEdge(rt, "EdgeR", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(2f, 0f), edgeCol);

        Color bracketCol = neon;
        float bLen = 14f, bThick = 3f, bInset = 6f;
        CreateButtonCornerBracket(rt, "CornerTL", new Vector2(0f, 1f), new Vector2(bInset, -bInset), bLen, bThick, bracketCol, +1, -1);
        CreateButtonCornerBracket(rt, "CornerTR", new Vector2(1f, 1f), new Vector2(-bInset, -bInset), bLen, bThick, bracketCol, -1, -1);
        CreateButtonCornerBracket(rt, "CornerBL", new Vector2(0f, 0f), new Vector2(bInset, bInset), bLen, bThick, bracketCol, +1, +1);
        CreateButtonCornerBracket(rt, "CornerBR", new Vector2(1f, 0f), new Vector2(-bInset, bInset), bLen, bThick, bracketCol, -1, +1);

        GameObject accent = new GameObject("Accent");
        RectTransform accentRT = accent.AddComponent<RectTransform>();
        accentRT.SetParent(rt, false);
        accentRT.anchorMin = new Vector2(0f, 0.2f);
        accentRT.anchorMax = new Vector2(0f, 0.8f);
        accentRT.pivot = new Vector2(0f, 0.5f);
        accentRT.anchoredPosition = new Vector2(10f, 0f);
        accentRT.sizeDelta = new Vector2(4f, 0f);
        Image accentImg = accent.AddComponent<Image>();
        accentImg.color = neon;
        accentImg.raycastTarget = false;

        label = CreateText(rt, "Label", text, 24, FontStyles.Bold, neon);
        RectTransform lblRT = label.rectTransform;
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = new Vector2(24f, 0f);
        lblRT.offsetMax = new Vector2(-16f, 0f);
        label.alignment = TextAlignmentOptions.Center;
        label.characterSpacing = 4f;
        label.raycastTarget = false;

        return btn;
    }

    // Lays the existing centered label into the button's top half and adds a
    // dimmer "BEST nnn" sub-label in the bottom half. The label param (from
    // CreatePrimaryButton) is repositioned in-place so no signature change.
    void AddBestLineToButton(Button btn, Color neon, out TMP_Text bestLabel)
    {
        RectTransform btnRT = btn.GetComponent<RectTransform>();
        TMP_Text nameLabel = btn.transform.Find("Label") != null
            ? btn.transform.Find("Label").GetComponent<TMP_Text>()
            : null;
        if (nameLabel != null)
        {
            RectTransform nameRT = nameLabel.rectTransform;
            nameRT.anchorMin = new Vector2(0f, 0.5f);
            nameRT.anchorMax = new Vector2(1f, 1f);
            nameRT.offsetMin = new Vector2(24f, 0f);
            nameRT.offsetMax = new Vector2(-16f, -8f);
        }

        bestLabel = CreateText(btnRT, "BestSubLabel", "",
            16, FontStyles.Bold, new Color(neon.r, neon.g, neon.b, 0.65f));
        RectTransform bestRT = bestLabel.rectTransform;
        bestRT.anchorMin = new Vector2(0f, 0f);
        bestRT.anchorMax = new Vector2(1f, 0.5f);
        bestRT.offsetMin = new Vector2(24f, 12f);
        bestRT.offsetMax = new Vector2(-16f, 0f);
        bestLabel.alignment = TextAlignmentOptions.Center;
        bestLabel.characterSpacing = 6f;
        bestLabel.raycastTarget = false;
    }

    int GetBestForMode(GameModeType mode)
    {
        string saved = PlayerPrefs.GetString(GameConfig.GetScoresKey(mode), "");
        if (string.IsNullOrEmpty(saved)) return 0;
        int best = 0;
        foreach (string p in saved.Split(','))
        {
            string[] pair = p.Split(':');
            int v;
            if (int.TryParse(pair[0], out v) && v > best) best = v;
        }
        return best;
    }

    void RefreshModeBestLabels()
    {
        if (titlePureHellBestLabel != null)
        {
            int best = GetBestForMode(GameModeType.PureHell);
            titlePureHellBestLabel.text = best > 0
                ? L10n.T("title.best") + "  " + FormatModeScore(best)
                : L10n.T("title.no_runs_yet");
        }
        if (titleBlitzBestLabel != null)
        {
            int best = GetBestForMode(GameModeType.Blitz);
            titleBlitzBestLabel.text = best > 0
                ? L10n.T("title.best") + "  " + FormatModeScore(best)
                : L10n.T("title.no_runs_yet");
        }
    }

    void CreateButtonEdge(RectTransform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 thickness, Color color)
    {
        GameObject obj = new GameObject(name);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = thickness;
        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    void CreateButtonCornerBracket(RectTransform parent, string name,
        Vector2 anchor, Vector2 inset, float len, float thick, Color color, int signX, int signY)
    {
        GameObject h = new GameObject(name + "H");
        RectTransform hrt = h.AddComponent<RectTransform>();
        hrt.SetParent(parent, false);
        hrt.anchorMin = anchor;
        hrt.anchorMax = anchor;
        hrt.pivot = new Vector2(signX > 0 ? 0f : 1f, signY > 0 ? 0f : 1f);
        hrt.anchoredPosition = inset;
        hrt.sizeDelta = new Vector2(len, thick);
        Image hi = h.AddComponent<Image>();
        hi.color = color;
        hi.raycastTarget = false;

        GameObject v = new GameObject(name + "V");
        RectTransform vrt = v.AddComponent<RectTransform>();
        vrt.SetParent(parent, false);
        vrt.anchorMin = anchor;
        vrt.anchorMax = anchor;
        vrt.pivot = new Vector2(signX > 0 ? 0f : 1f, signY > 0 ? 0f : 1f);
        vrt.anchoredPosition = inset;
        vrt.sizeDelta = new Vector2(thick, len);
        Image vi = v.AddComponent<Image>();
        vi.color = color;
        vi.raycastTarget = false;
    }

    void SetButtonAlpha(Button btn, float alpha)
    {
        if (btn == null) return;
        CanvasGroup cg = btn.GetComponent<CanvasGroup>();
        if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = alpha;
        cg.blocksRaycasts = alpha > 0.05f;
    }

    void StartWithMode(GameModeType mode)
    {
        if (!GameConfig.HasSeenTutorial())
        {
            // First-ever run: route into tutorial. The flag stays active until
            // both L and R have fired + the player taps "Ready?". IsTutorialActive
            // is read by Torus (skip obstacle spawn, freeze rotation) and Sphere
            // (turn wall/trailing hits into ball-resets).
            GameConfig.IsTutorialActive = true;
            ResetTutorialState();
            if (mSphere != null) mSphere.ResetTutorialInputs();
        }

        GameConfig.ActiveMode = mode;
        LoadScores(); // Reload for the selected mode's leaderboard
        if (mSphere != null && mSphere.IsWaiting())
            mSphere.StartGame();
    }

    void ResetTutorialState()
    {
        mTutorialSinceBothSeenTimer = -1f;
        mTutorialNudgeTimer = 0f;
        mTutorialDeathFlashTimer = -1f;
        mTutorialReady = false;
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

        // Hit target only — the dock strip frames the button area, so there's no
        // per-button circle anymore. Image stays for raycast + ColorTint hover tile.
        Image bg = btnObj.AddComponent<Image>();
        bg.color = new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0f);

        // Hover/press: cyan tile fades in, sits within the dock strip. Pressed is
        // brighter than highlighted so taps register clearly on touch devices.
        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0f);
        cb.highlightedColor = new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0.15f);
        cb.pressedColor     = new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0.25f);
        cb.selectedColor    = new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0.15f);
        cb.disabledColor    = new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 0f);
        cb.colorMultiplier = 1f;
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
            case "star":    BuildStarIcon(iconRT, iconColor); break;
            case "bars":    BuildBarsIcon(iconRT, iconColor); break;
            case "gear":    BuildPhosphorIcon(iconRT, PHOSPHOR_GEAR, iconColor, false); break;
            case "power":   BuildPhosphorIcon(iconRT, PHOSPHOR_POWER, iconColor, false); break;
            case "back":    BuildPhosphorIcon(iconRT, PHOSPHOR_CARET_LEFT, iconColor, false); break;
            case "trophy":  BuildPhosphorIcon(iconRT, PHOSPHOR_TROPHY, iconColor, false); break;
            case "chart":   BuildPhosphorIcon(iconRT, PHOSPHOR_CHART_BAR, iconColor, false); break;
            case "stats":   BuildPhosphorIcon(iconRT, PHOSPHOR_LIST_BULLET, iconColor, true, true); break;
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

    void BuildBackIcon(RectTransform parent, Color color)
    {
        // Left-pointing chevron — two short bars meeting at a left-facing point.
        Image top = CreateIconBar(parent, "ChevTop", color, 0.55f, 0.18f);
        top.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        top.rectTransform.anchoredPosition = new Vector2(-3f, 10f);
        Image bot = CreateIconBar(parent, "ChevBot", color, 0.55f, 0.18f);
        bot.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);
        bot.rectTransform.anchoredPosition = new Vector2(-3f, -10f);
    }

    void BuildPhosphorIcon(RectTransform parent, string glyph, Color color, bool flipY, bool flipX = false)
    {
        GameObject obj = new GameObject("Glyph");
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = new Vector3(flipX ? -1f : 1f, flipY ? -1f : 1f, 1f);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        if (phosphorFont != null) tmp.font = phosphorFont;
        tmp.text = glyph;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 8f;
        tmp.fontSizeMax = 200f;
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
