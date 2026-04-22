using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public partial class ScoreSync
{
    // ── STATE DISPLAY ────────────────────────────────────────

    void ShowGroup(State s)
    {
        SetGroupActive(splashGroup, s == State.Splash);
        SetGroupActive(titleGroup, s == State.Title);
        SetGroupActive(tutorialGroup, s == State.Tutorial);
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
            case State.Tutorial: AnimateTutorial(); break;
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
                ApplyGlitchToText(titleBlitzLabel);
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

        // Chromatic aberration on name — suppressed when Reduce Motion is active.
        float chromaAlpha = nameFade * 0.55f * (AccessibilitySettings.IsReduceMotionActive() ? 0f : 1f);
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

        // Chromatic aberration — bold CMYK separation, suppressed when Reduce Motion is active.
        float chromaFade = Mathf.Clamp01((stateTimer - 0.4f) / 0.6f);
        float chromaAlpha = chromaFade * 0.65f * (AccessibilitySettings.IsReduceMotionActive() ? 0f : 1f);

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

        // Per-mode BEST is rendered inside each mode button now, so the single
        // centered BEST line + gold underline stay hidden on title.
        SetAlpha(bestScoreText, 0f);
        if (bestScoreLine != null)
        {
            Color lc = bestScoreLine.color;
            lc.a = 0f;
            bestScoreLine.color = lc;
        }
        RefreshModeBestLabels();

        // Mode picker buttons — fade in the whole button (frame + brackets + label)
        // via CanvasGroup, after title settles.
        if (stateTimer > 1.2f)
        {
            float fadeIn = Mathf.Clamp01((stateTimer - 1.2f) / 0.4f);
            SetButtonAlpha(titlePureHellBtn, fadeIn);
            SetButtonAlpha(titleBlitzBtn, fadeIn);
        }
        else
        {
            SetButtonAlpha(titlePureHellBtn, 0f);
            SetButtonAlpha(titleBlitzBtn, 0f);
        }
        SetAlpha(titleTapText, 0f);

        // "CHOOSE YOUR MODE" — always visible once title settles
        if (titleHintText != null)
        {
            float hintFade = Mathf.Clamp01((stateTimer - 1.0f) / 0.5f);
            SetAlpha(titleHintText, hintFade * 0.75f);
        }

        // Icon buttons — fade in with best score
        float lbFade = Mathf.Clamp01((stateTimer - 0.8f) / 0.6f);
        SetGlyphAlpha(titleLBIcon, lbFade * 0.75f);
        SetGlyphAlpha(titleAchIcon, lbFade * 0.75f);
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

        // Blitz: update upgrade HUD and spark effects
        if (GameConfig.IsBlitz())
        {
            UpdateBlitzUpgradeHUD();
            UpdateBlitzStreakHUD();
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
        const float SLOT_SIZE = 9.4f * PHONE_HUD_SCALE;
        const float SLOT_GAP = 4f * PHONE_HUD_SCALE;
        const float ROW_GAP = 6f * PHONE_HUD_SCALE;
        const float LABEL_WIDTH = 86f * PHONE_HUD_SCALE;
        const float MARGIN_X = 30f;
        const float MARGIN_Y = 30f;

        GameObject grp = new GameObject("BlitzUpgradeHUD");
        blitzUpgradeGroup = grp.AddComponent<RectTransform>();
        blitzUpgradeGroup.SetParent(parent, false);
        blitzUpgradeGroup.anchorMin = new Vector2(0f, 1f);
        blitzUpgradeGroup.anchorMax = new Vector2(0f, 1f);
        blitzUpgradeGroup.pivot = new Vector2(0f, 1f);
        blitzUpgradeGroup.anchoredPosition = new Vector2(MARGIN_X, -MARGIN_Y);
        blitzUpgradeGroup.sizeDelta = new Vector2(240f * PHONE_HUD_SCALE, 80f * PHONE_HUD_SCALE);

        Color gunColor = new Color(1f, 0.85f, 0.1f);
        Color cadencyColor = new Color(0.2f, 0.7f, 1.0f);
        Color shieldColor = new Color(0.1f, 1.0f, 0.4f);

        blitzGunLabel = CreateRowLabel(blitzUpgradeGroup, "GunLabel", L10n.T("hud.beams") + " 1/3", 0, gunColor, SLOT_SIZE, ROW_GAP, LABEL_WIDTH);
        blitzCadencyLabel = CreateRowLabel(blitzUpgradeGroup, "CadencyLabel", L10n.T("hud.cadency") + " 1x", 1, cadencyColor, SLOT_SIZE, ROW_GAP, LABEL_WIDTH);
        blitzShieldLabel = CreateRowLabel(blitzUpgradeGroup, "ShieldLabel", "Shield", 2, shieldColor, SLOT_SIZE, ROW_GAP, LABEL_WIDTH);

        blitzGunSlots = CreateSlotRow(blitzUpgradeGroup, 0, 5, gunColor, SLOT_SIZE, SLOT_GAP, ROW_GAP, LABEL_WIDTH);
        blitzCadencySlots = CreateSlotRow(blitzUpgradeGroup, 1, 5, cadencyColor, SLOT_SIZE, SLOT_GAP, ROW_GAP, LABEL_WIDTH);
        blitzShieldSlots = CreateSlotRow(blitzUpgradeGroup, 2, 5, shieldColor, SLOT_SIZE, SLOT_GAP, ROW_GAP, LABEL_WIDTH);
    }

    // Top-right mirror of the upgrade HUD. Fill bar on top + "x1" tier label below it —
    // bar shows progress to the next tier (or stays full at x4 max). Magenta palette
    // differentiates it from the gold/cyan/green upgrade tracks. Label sized to match
    // the upgrade row labels so the multiplier reads as a peer, not a hero element.
    void BuildBlitzStreakHUD(RectTransform parent)
    {
        const float MARGIN_X = 30f;
        const float MARGIN_Y = 30f;
        const float GROUP_W = 120f * PHONE_HUD_SCALE;
        const float BAR_H = 6f * PHONE_HUD_SCALE;
        const float LABEL_GAP = 4f * PHONE_HUD_SCALE;
        const float TIER_FONT = 13f * PHONE_HUD_SCALE;
        const float LABEL_ROW_H = 16f * PHONE_HUD_SCALE;
        const float GROUP_H = BAR_H + LABEL_GAP + LABEL_ROW_H;

        GameObject grp = new GameObject("BlitzStreakHUD");
        blitzStreakGroup = grp.AddComponent<RectTransform>();
        blitzStreakGroup.SetParent(parent, false);
        blitzStreakGroup.anchorMin = new Vector2(1f, 1f);
        blitzStreakGroup.anchorMax = new Vector2(1f, 1f);
        blitzStreakGroup.pivot = new Vector2(1f, 1f);
        blitzStreakGroup.anchoredPosition = new Vector2(-MARGIN_X, -MARGIN_Y);
        blitzStreakGroup.sizeDelta = new Vector2(GROUP_W, GROUP_H);

        Color tierColor = NEON_MAGENTA;

        // Bar track (dim) — top of the group
        blitzStreakBarTrack = CreateImage(blitzStreakGroup, "StreakBarTrack",
            new Color(tierColor.r, tierColor.g, tierColor.b, 0.15f));
        RectTransform trt = blitzStreakBarTrack.rectTransform;
        trt.anchorMin = new Vector2(1f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(1f, 1f);
        trt.anchoredPosition = new Vector2(0f, 0f);
        trt.sizeDelta = new Vector2(GROUP_W, BAR_H);

        // Bar fill (bright) — grows from the right side so it visually aligns with the label.
        blitzStreakBarFill = CreateImage(blitzStreakGroup, "StreakBarFill",
            new Color(tierColor.r, tierColor.g, tierColor.b, 0.9f));
        RectTransform frt = blitzStreakBarFill.rectTransform;
        frt.anchorMin = new Vector2(1f, 1f);
        frt.anchorMax = new Vector2(1f, 1f);
        frt.pivot = new Vector2(1f, 1f);
        frt.anchoredPosition = new Vector2(0f, 0f);
        frt.sizeDelta = new Vector2(0f, BAR_H);

        // Tier readout — right-aligned under the bar, peer-sized to BEAMS/CADENCE/SHIELD.
        blitzStreakTierLabel = CreateText(blitzStreakGroup, "StreakTier", "x1",
            TIER_FONT, FontStyles.Bold, new Color(tierColor.r, tierColor.g, tierColor.b, 0.9f));
        blitzStreakTierLabel.alignment = TextAlignmentOptions.TopRight;
        RectTransform lrt = blitzStreakTierLabel.rectTransform;
        lrt.anchorMin = new Vector2(1f, 1f);
        lrt.anchorMax = new Vector2(1f, 1f);
        lrt.pivot = new Vector2(1f, 1f);
        lrt.anchoredPosition = new Vector2(0f, -(BAR_H + LABEL_GAP));
        lrt.sizeDelta = new Vector2(GROUP_W, LABEL_ROW_H);
    }

    void UpdateBlitzStreakHUD()
    {
        if (blitzStreakGroup == null) return;
        Torus torus = FindAnyObjectByType<Torus>();
        if (torus == null) return;

        int tier = torus.GetStreakTier();
        float barP = torus.GetStreakBarProgress();

        // Magenta at tier 1, warming to gold/white at max — palette tells the story.
        Color baseColor = tier >= 4 ? NEON_GOLD
                        : tier == 3 ? new Color(1f, 0.55f, 0.3f)
                        : tier == 2 ? new Color(1f, 0.35f, 0.55f)
                        : NEON_MAGENTA;

        if (tier != blitzLastStreakTier)
        {
            if (tier > blitzLastStreakTier && blitzLastStreakTier > 0)
                blitzStreakFlashTimer = 0f;
            blitzLastStreakTier = tier;
            if (blitzStreakTierLabel != null) blitzStreakTierLabel.text = "x" + tier;
        }

        // Flash on tier-up: bump alpha/brightness briefly.
        float flashBoost = 0f;
        if (blitzStreakFlashTimer >= 0f && blitzStreakFlashTimer < BLITZ_STREAK_FLASH_DURATION)
        {
            blitzStreakFlashTimer += Time.deltaTime;
            float p = Mathf.Clamp01(blitzStreakFlashTimer / BLITZ_STREAK_FLASH_DURATION);
            flashBoost = (1f - p);
            if (p >= 1f) blitzStreakFlashTimer = -1f;
        }

        Color labelColor = Color.Lerp(
            new Color(baseColor.r, baseColor.g, baseColor.b, 0.9f),
            Color.white, flashBoost);
        if (blitzStreakTierLabel != null) blitzStreakTierLabel.color = labelColor;

        if (blitzStreakBarTrack != null)
            blitzStreakBarTrack.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.15f);

        // Bar fill width — only animate on meaningful change so we avoid per-frame GC.
        if (!Mathf.Approximately(barP, blitzLastStreakBar))
        {
            blitzLastStreakBar = barP;
            if (blitzStreakBarFill != null)
            {
                RectTransform frt = blitzStreakBarFill.rectTransform;
                frt.sizeDelta = new Vector2(120f * PHONE_HUD_SCALE * barP, frt.sizeDelta.y);
            }
        }
        if (blitzStreakBarFill != null)
        {
            blitzStreakBarFill.color = new Color(baseColor.r, baseColor.g, baseColor.b,
                0.9f + flashBoost * 0.1f);
        }
    }

    TMP_Text CreateRowLabel(RectTransform parent, string name, string content, int rowIndex,
        Color color, float slotSize, float rowGap, float width)
    {
        TMP_Text t = CreateText(parent, name, content, 13f * PHONE_HUD_SCALE, FontStyles.Bold,
            new Color(color.r, color.g, color.b, 0.9f));
        t.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform rt = t.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        float y = -(rowIndex * (slotSize + rowGap));
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(width, slotSize);
        return t;
    }

    Image[] CreateSlotRow(RectTransform parent, int rowIndex, int count, Color color,
        float slotSize, float slotGap, float rowGap, float xOffset)
    {
        // Circles match the game's aesthetic (ball + orb rings). Each slot: ring sprite
        // always visible at low alpha + inner filled circle toggled by UpdateSlotRow.
        // Uncollected = hollow ring; collected = solid disc (fill covers the ring).
        Image[] fills = new Image[count];
        float y = -(rowIndex * (slotSize + rowGap));
        Color strokeColor = new Color(color.r, color.g, color.b, 0.55f);

        for (int i = 0; i < count; i++)
        {
            float x = xOffset + i * (slotSize + slotGap);

            Image ring = CreateImage(parent, "Slot_" + rowIndex + "_" + i, strokeColor);
            if (ringSprite != null) ring.sprite = ringSprite;
            RectTransform rt = ring.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(slotSize, slotSize);

            Image fill = CreateImage(rt, "Fill",
                new Color(color.r, color.g, color.b, 0f));
            if (circleSprite != null) fill.sprite = circleSprite;
            RectTransform frt = fill.rectTransform;
            frt.anchorMin = new Vector2(0f, 0f);
            frt.anchorMax = new Vector2(1f, 1f);
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            fills[i] = fill;
        }

        return fills;
    }

    void UpdateBlitzUpgradeHUD()
    {
        if (blitzUpgradeGroup == null) return;
        Torus torus = FindAnyObjectByType<Torus>();
        if (torus == null) return;

        int gun = torus.GetGunOrbCount();
        int cadency = torus.GetCadencyOrbCount();
        int shield = torus.GetShieldOrbCount();
        int gunLevel = torus.GetGunLevel();
        int cadencyLevel = torus.GetCadencyLevel();

        if (gun != blitzLastGunCount || gunLevel != blitzLastGunLevel)
        {
            blitzLastGunCount = gun;
            blitzLastGunLevel = gunLevel;
            int filled = gunLevel >= 2 ? 5 : gun - gunLevel * 5;
            UpdateSlotRow(blitzGunSlots, filled, new Color(1f, 0.85f, 0.1f));
            if (blitzGunLabel != null) blitzGunLabel.text = L10n.T("hud.beams") + " " + (gunLevel + 1) + "/3";
        }
        if (cadency != blitzLastCadencyCount || cadencyLevel != blitzLastCadencyLevel)
        {
            blitzLastCadencyCount = cadency;
            blitzLastCadencyLevel = cadencyLevel;
            int filled = cadencyLevel >= 2 ? 5 : cadency - cadencyLevel * 5;
            UpdateSlotRow(blitzCadencySlots, filled, new Color(0.2f, 0.7f, 1.0f));
            if (blitzCadencyLabel != null) blitzCadencyLabel.text = L10n.T("hud.cadency") + " " + (cadencyLevel + 1) + "x";
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
            float alpha = i < filledCount ? 1f : 0f;
            slots[i].color = new Color(color.r, color.g, color.b, alpha);
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

        // Find target slot position — wrap Gun/Cadency indices into current 5-slot tier
        Image[] slots = type == BlitzOrb.OrbType.Gun ? blitzGunSlots
                      : type == BlitzOrb.OrbType.Cadency ? blitzCadencySlots
                      : blitzShieldSlots;
        if (slots == null) return;
        if (type != BlitzOrb.OrbType.Shield && slotIndex >= 0)
            slotIndex = slotIndex % slots.Length;
        if (slotIndex < 0 || slotIndex >= slots.Length) return;

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

                // Slot lights up via UpdateSlotRow (alpha change) — no scale pop.
                continue;
            }

            // Ease-in-out arc path
            float eased = p < 0.5f ? 2f * p * p : 1f - Mathf.Pow(-2f * p + 2f, 2f) * 0.5f;
            Vector2 pos = Vector2.Lerp(sparkFrom[i], sparkTo[i], eased);
            // Upward arc — higher arc gives the spark a readable "launch & land" trajectory
            float arc = Mathf.Sin(p * Mathf.PI) * 100f;
            pos.y += arc;

            sparkImages[i].rectTransform.anchoredPosition = pos;

            // Size: launches 2.5× (fat bolt leaving the orb), lands at slot size
            float size = Mathf.Lerp(SPARK_SIZE * 2.5f, SPARK_SIZE * 0.6f, eased);
            sparkImages[i].rectTransform.sizeDelta = new Vector2(size, size);

            // Fade alpha: full brightness, slight fade at end
            float alpha = p < 0.8f ? 1f : Mathf.Lerp(1f, 0.6f, (p - 0.8f) / 0.2f);
            Color sc = sparkImages[i].color;
            sparkImages[i].color = new Color(sc.r, sc.g, sc.b, alpha);
        }
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
            float countScale = GameConfig.IsBlitz() ? 0.002f : 0.04f;
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

                float chromaAlpha = fadeEased * 0.4f * (AccessibilitySettings.IsReduceMotionActive() ? 0f : 1f);
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
                goNewBestText.text = L10n.T("gameover.new_best");
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
                goNewBestText.text = L10n.T("gameover.top_five");
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
            SetGlyphAlpha(goAchIcon, p * 0.65f);
            SetGlyphAlpha(goBackIcon, p * 0.65f);
        }
        else
        {
            SetGlyphAlpha(goSettingsIcon, 0f);
            SetGlyphAlpha(goStatsIcon, 0f);
            SetGlyphAlpha(goLBIcon, 0f);
            SetGlyphAlpha(goAchIcon, 0f);
            SetGlyphAlpha(goBackIcon, 0f);
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

            // Color hierarchy: gold = the run that just ended (only if it's in the top 5),
            // cyan = everything else. Rank #1 gets bold regardless of current-run status so
            // it still reads as "the best" even when the player didn't land in the top 5.
            Color rowColor = isCurrent ? NEON_GOLD : NEON_CYAN;
            // Keep the hierarchy via color + bold only; alphas stay near-opaque so every row
            // is comfortably readable (earlier 0.55 for rows 2–5 was unreadable in daylight).
            float maxAlpha;
            if (isCurrent)        maxAlpha = 1.0f;
            else if (i == 0)      maxAlpha = 1.0f;
            else                  maxAlpha = 0.9f;

            goLeaderboardTexts[i].color = new Color(rowColor.r, rowColor.g, rowColor.b,
                rowAlpha * maxAlpha);
            goLeaderboardTexts[i].fontStyle = (i == 0)
                ? FontStyles.Bold : FontStyles.Normal;

            // Slide in from right
            float slideX = Mathf.Lerp(30f, 0f, EaseOutCubic(rowP));
            goLeaderboardTexts[i].rectTransform.anchoredPosition = new Vector2(slideX, 0f);

            // Date column
            if (goLeaderboardDateTexts[i] != null)
            {
                long ts = i < topTimestamps.Count ? topTimestamps[i] : 0;
                goLeaderboardDateTexts[i].text = FormatTimestamp(ts);
                float dateAlpha = maxAlpha * 0.75f; // Subtler than score, still readable
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
        pausedText = CreateText(pauseGroup, "PausedLabel", L10n.T("pause.paused"),
            64, FontStyles.Bold, NEON_CYAN);
        SetAnchored(pausedText.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(600, 90));
        pausedText.characterSpacing = 16f;

        // Resume hint
        pauseHintText = CreateText(pauseGroup, "PauseHint", GetResumeHint(),
            28, FontStyles.Normal, DIM_TEXT);
        SetAnchored(pauseHintText.rectTransform, new Vector2(0.5f, 0.42f), new Vector2(600, 50));
        pauseHintText.characterSpacing = 6f;
    }

}
