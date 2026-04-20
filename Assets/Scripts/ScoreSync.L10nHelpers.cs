using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public partial class ScoreSync
{
    // ── PLATFORM-SPECIFIC LOCALIZED STRINGS ─────────────────────

    string GetTapPrompt()
    {
#if UNITY_STANDALONE
        return L10n.T("tap_prompt.keyboard");
#else
        return L10n.T("tap_prompt.tap");
#endif
    }

    string GetResumeHint()
    {
#if UNITY_TVOS
        return L10n.T("pause.resume.tv");
#elif UNITY_STANDALONE
        return L10n.T("pause.resume.keyboard");
#else
        return L10n.T("pause.resume.tap");
#endif
    }

    string GetTutorialInstruction()
    {
#if UNITY_STANDALONE
        return L10n.T("tutorial.instruction.keyboard");
#else
        return L10n.T("tutorial.instruction.tap");
#endif
    }

    string GetTutorialReadyHint()
    {
#if UNITY_STANDALONE
        return L10n.T("tutorial.ready_hint.keyboard");
#else
        return L10n.T("tutorial.ready_hint.tap");
#endif
    }

    string GetCloseHint()
    {
#if UNITY_TVOS
        return L10n.T("settings.close.tv");
#elif UNITY_STANDALONE
        return L10n.T("settings.close.keyboard");
#else
        return L10n.T("settings.close.tap");
#endif
    }

    void OnLanguageNext()
    {
        int cur = System.Array.IndexOf(L10n.AllPrefs, L10n.CurrentPref);
        int next = (cur + 1 + L10n.AllPrefs.Length) % L10n.AllPrefs.Length;
        L10n.SetPref(L10n.AllPrefs[next]);
    }

    void OnLanguagePrev()
    {
        int cur = System.Array.IndexOf(L10n.AllPrefs, L10n.CurrentPref);
        int prev = (cur - 1 + L10n.AllPrefs.Length) % L10n.AllPrefs.Length;
        L10n.SetPref(L10n.AllPrefs[prev]);
    }

    void ApplyThemeSelection(int index)
    {
        ThemeData.SaveIndex(index);

        SceneSetup setup = FindAnyObjectByType<SceneSetup>();
        if (setup != null)
        {
            if (index == ThemeData.AUTO_INDEX)
            {
                setup.StartAutoMode();
            }
            else
            {
                setup.StopAutoMode();
                setup.ApplyThemeLive(ThemeData.All()[index]);
            }
        }

        RefreshSettingsLabels();
    }

    void RefreshSettingsLabels()
    {
        if (mAudio == null) return;

        bool musicOff = mAudio.IsMusicMuted();
        bool soundOff = mAudio.IsSoundMuted();

        if (settingsMusicIcon != null)
        {
            settingsMusicIcon.text = musicOff ? PHOSPHOR_SPEAKER_X : PHOSPHOR_SPEAKER_HIGH;
            StyleSettingsToggle(settingsMusicIcon, !musicOff);
        }
        if (settingsSoundIcon != null)
        {
            settingsSoundIcon.text = soundOff ? PHOSPHOR_SPEAKER_X : PHOSPHOR_SPEAKER_HIGH;
            StyleSettingsToggle(settingsSoundIcon, !soundOff);
        }

        // Display settings (standalone only)
        if (DisplaySettings.Instance != null)
        {
            if (settingsResLabel != null)
            {
                settingsResLabel.text = L10n.T("settings.res") + "   " + DisplaySettings.Instance.GetResolutionLabel();
                StyleSettingsToggle(settingsResLabel, true); // Always "on" style
            }
            if (settingsFullscreenLabel != null)
            {
                bool fs = DisplaySettings.Instance.IsFullscreen();
                settingsFullscreenLabel.text = L10n.T("settings.fullscreen") + "   " + L10n.T(fs ? "settings.on" : "settings.off");
                StyleSettingsToggle(settingsFullscreenLabel, fs);
            }
            if (settingsVSyncLabel != null)
            {
                bool vs = DisplaySettings.Instance.IsVSync();
                settingsVSyncLabel.text = L10n.T("settings.vsync") + "   " + L10n.T(vs ? "settings.on" : "settings.off");
                StyleSettingsToggle(settingsVSyncLabel, vs);
            }
        }

        if (settingsThemeLabel != null)
        {
            string themeName;
            if (ThemeData.LoadSavedIndex() == ThemeData.AUTO_INDEX)
                themeName = L10n.T("settings.theme.auto");
            else
                themeName = SceneSetup.activeTheme != null ? SceneSetup.activeTheme.name : "NEON VOID";
            settingsThemeLabel.text = themeName;
            settingsThemeLabel.color = Color.white;
        }

        if (settingsLanguageLabel != null)
        {
            settingsLanguageLabel.text = L10n.LanguageDisplayName(L10n.CurrentPref);
            settingsLanguageLabel.color = Color.white;
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

    void OnModesTap()
    {
        if (mSphere != null) mSphere.ReturnToTitle();
        forceShowTitle = true;
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

}
