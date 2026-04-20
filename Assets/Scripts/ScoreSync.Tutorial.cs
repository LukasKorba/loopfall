using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public partial class ScoreSync
{
    // ── TUTORIAL ────────────────────────────────────────────

    void BuildTutorialGroup(Transform parent)
    {
        tutorialGroup = CreateGroup(parent, "TutorialGroup");
#if UNITY_IOS && !UNITY_TVOS
        ApplySafeAreaInsets(tutorialGroup);
#endif
        tutorialGroup.gameObject.SetActive(false);

        // Left / right arrows flanking the line. Unicode arrows keep the TMP path
        // simple and scale cleanly; we tint them when their direction has fired.
        tutorialLeftArrow = CreateText(tutorialGroup, "TutorialLeftArrow", "\u2B05",
            140, FontStyles.Bold, NEON_CYAN);
        SetAnchored(tutorialLeftArrow.rectTransform, new Vector2(0.28f, 0.50f), new Vector2(220, 200));
        ApplyDropShadow(tutorialLeftArrow);

        tutorialRightArrow = CreateText(tutorialGroup, "TutorialRightArrow", "\u27A1",
            140, FontStyles.Bold, NEON_MAGENTA);
        SetAnchored(tutorialRightArrow.rectTransform, new Vector2(0.72f, 0.50f), new Vector2(220, 200));
        ApplyDropShadow(tutorialRightArrow);

        // Primary instruction — modality-agnostic, reads left AND right as input actions.
        tutorialInstructionText = CreateText(tutorialGroup, "TutorialInstruction", GetTutorialInstruction(),
            42, FontStyles.Bold, Color.white);
        SetAnchored(tutorialInstructionText.rectTransform, new Vector2(0.5f, 0.84f), new Vector2(1400, 90));
        tutorialInstructionText.characterSpacing = 6f;
        ApplyDropShadow(tutorialInstructionText);

        // Nudge — only surfaces if the player uses one direction and stops.
        tutorialNudgeText = CreateText(tutorialGroup, "TutorialNudge", "",
            30, FontStyles.Italic, NEON_GOLD);
        SetAnchored(tutorialNudgeText.rectTransform, new Vector2(0.5f, 0.1f), new Vector2(1400, 60));
        tutorialNudgeText.characterSpacing = 4f;
        tutorialNudgeText.color = new Color(NEON_GOLD.r, NEON_GOLD.g, NEON_GOLD.b, 0f);

        // Ready? prompt + resolving hint. Both hidden until both directions fired.
        tutorialReadyText = CreateText(tutorialGroup, "TutorialReady", L10n.T("tutorial.ready"),
            96, FontStyles.Bold, NEON_GOLD);
        SetAnchored(tutorialReadyText.rectTransform, new Vector2(0.5f, 0.50f), new Vector2(1200, 140));
        tutorialReadyText.characterSpacing = 12f;
        ApplyDropShadow(tutorialReadyText);
        tutorialReadyText.color = new Color(NEON_GOLD.r, NEON_GOLD.g, NEON_GOLD.b, 0f);

        tutorialReadyHintText = CreateText(tutorialGroup, "TutorialReadyHint", GetTutorialReadyHint(),
            28, FontStyles.Normal, DIM_TEXT);
        SetAnchored(tutorialReadyHintText.rectTransform, new Vector2(0.5f, 0.40f), new Vector2(1400, 50));
        tutorialReadyHintText.characterSpacing = 6f;
        tutorialReadyHintText.color = new Color(DIM_TEXT.r, DIM_TEXT.g, DIM_TEXT.b, 0f);

        // Platform-specific input hint (touch / remote / keyboard / gamepad). Sprites live in
        // Resources/UI and are picked per build target — desktop falls back to keyboard unless
        // a joystick is connected at scene start.
        Sprite platformSprite = GetTutorialPlatformSprite();
        tutorialPlatformImage = CreateImage(tutorialGroup.transform, "TutorialPlatformImage",
            platformSprite != null ? new Color(1f, 1f, 1f, 0.85f) : new Color(1f, 1f, 1f, 0f));
        if (platformSprite != null)
        {
            tutorialPlatformImage.sprite = platformSprite;
            tutorialPlatformImage.preserveAspect = true;
        }
        RectTransform pImgRT = tutorialPlatformImage.rectTransform;
        pImgRT.anchorMin = new Vector2(0.5f, 0.5f);
        pImgRT.anchorMax = new Vector2(0.5f, 0.5f);
        pImgRT.pivot = new Vector2(0.5f, 0.5f);
        pImgRT.anchoredPosition = Vector2.zero;
        pImgRT.sizeDelta = new Vector2(480f, 900f);
    }

    Sprite GetTutorialPlatformSprite()
    {
#if UNITY_IOS || UNITY_ANDROID
        return Resources.Load<Sprite>("UI/touch");
#elif UNITY_TVOS
        return HasGamepadConnected()
            ? Resources.Load<Sprite>("UI/remote_gamepad")
            : Resources.Load<Sprite>("UI/remote");
#else
        return HasGamepadConnected()
            ? Resources.Load<Sprite>("UI/keyboard_gamepad")
            : Resources.Load<Sprite>("UI/keyboard");
#endif
    }

    bool HasGamepadConnected()
    {
        string[] joys = Input.GetJoystickNames();
        if (joys == null) return false;
        for (int i = 0; i < joys.Length; i++)
            if (!string.IsNullOrEmpty(joys[i])) return true;
        return false;
    }

    void AnimateTutorial()
    {
        if (mSphere == null) return;

        bool leftDone = mSphere.WasTutorialLeftFired();
        bool rightDone = mSphere.WasTutorialRightFired();

        // Arrow tint reflects progress — fired direction dims + desaturates.
        if (tutorialLeftArrow != null)
            tutorialLeftArrow.color = leftDone
                ? new Color(NEON_CYAN.r * 0.35f, NEON_CYAN.g * 0.35f, NEON_CYAN.b * 0.35f, 0.6f)
                : new Color(NEON_CYAN.r, NEON_CYAN.g, NEON_CYAN.b, 1f);
        if (tutorialRightArrow != null)
            tutorialRightArrow.color = rightDone
                ? new Color(NEON_MAGENTA.r * 0.35f, NEON_MAGENTA.g * 0.35f, NEON_MAGENTA.b * 0.35f, 0.6f)
                : new Color(NEON_MAGENTA.r, NEON_MAGENTA.g, NEON_MAGENTA.b, 1f);

        // Subtle float animation on the arrow that still needs hitting.
        if (tutorialLeftArrow != null && !leftDone)
        {
            float ox = Mathf.Sin(Time.time * 3.2f) * 10f;
            RectTransform rt = tutorialLeftArrow.rectTransform;
            Vector2 ap = rt.anchoredPosition; ap.x = ox - 6f; rt.anchoredPosition = ap;
        }
        if (tutorialRightArrow != null && !rightDone)
        {
            float ox = Mathf.Sin(Time.time * 3.2f + Mathf.PI) * 10f;
            RectTransform rt = tutorialRightArrow.rectTransform;
            Vector2 ap = rt.anchoredPosition; ap.x = ox + 6f; rt.anchoredPosition = ap;
        }

        // Death flash from tutorial wall hit — show a corrective nudge.
        if (mSphere.ConsumeTutorialDeath())
        {
            mTutorialDeathFlashTimer = 0f;
            // Full restart of coverage so the player re-earns both directions.
            mSphere.ResetTutorialInputs();
            leftDone = false;
            rightDone = false;
            mTutorialSinceBothSeenTimer = -1f;
            mTutorialReady = false;
            // Restore the platform hint if the Ready phase had faded it out.
            if (tutorialPlatformImage != null && tutorialPlatformImage.sprite != null)
                tutorialPlatformImage.color = new Color(1f, 1f, 1f, 0.85f);
        }

        // Instruction line — replaced by death message for a short window.
        if (mTutorialDeathFlashTimer >= 0f)
        {
            mTutorialDeathFlashTimer += Time.deltaTime;
            if (tutorialInstructionText != null)
            {
                tutorialInstructionText.text = L10n.T("tutorial.hit_walls");
                tutorialInstructionText.color = NEON_MAGENTA;
            }
            if (mTutorialDeathFlashTimer >= TUTORIAL_DEATH_FLASH_DURATION)
            {
                mTutorialDeathFlashTimer = -1f;
                if (tutorialInstructionText != null)
                {
                    tutorialInstructionText.text = GetTutorialInstruction();
                    tutorialInstructionText.color = Color.white;
                }
            }
        }

        // Nudge logic: one direction done + long silence on the other → encourage it.
        bool onlyOneDirection = (leftDone ^ rightDone);
        if (onlyOneDirection && mTutorialDeathFlashTimer < 0f)
        {
            mTutorialNudgeTimer += Time.deltaTime;
            if (mTutorialNudgeTimer >= TUTORIAL_NUDGE_DELAY && tutorialNudgeText != null)
            {
                string nudge = leftDone ? L10n.T("tutorial.nudge_right") : L10n.T("tutorial.nudge_left");
                tutorialNudgeText.text = nudge;
                float a = Mathf.Clamp01((mTutorialNudgeTimer - TUTORIAL_NUDGE_DELAY) / 0.5f);
                tutorialNudgeText.color = new Color(NEON_GOLD.r, NEON_GOLD.g, NEON_GOLD.b, a * 0.9f);
            }
        }
        else
        {
            mTutorialNudgeTimer = 0f;
            if (tutorialNudgeText != null)
            {
                Color c = tutorialNudgeText.color;
                c.a = Mathf.Max(0f, c.a - Time.deltaTime * 3f);
                tutorialNudgeText.color = c;
            }
        }

        // Both fired → small breather, then swap in the "Ready?" prompt.
        if (leftDone && rightDone)
        {
            if (mTutorialSinceBothSeenTimer < 0f) mTutorialSinceBothSeenTimer = 0f;
            mTutorialSinceBothSeenTimer += Time.deltaTime;

            if (mTutorialSinceBothSeenTimer >= TUTORIAL_READY_DELAY)
            {
                mTutorialReady = true;
                if (tutorialInstructionText != null)
                {
                    Color ic = tutorialInstructionText.color;
                    ic.a = Mathf.Max(0f, ic.a - Time.deltaTime * 3f);
                    tutorialInstructionText.color = ic;
                }
                if (tutorialLeftArrow != null)
                {
                    Color lc = tutorialLeftArrow.color;
                    lc.a = Mathf.Max(0f, lc.a - Time.deltaTime * 3f);
                    tutorialLeftArrow.color = lc;
                }
                if (tutorialRightArrow != null)
                {
                    Color rc = tutorialRightArrow.color;
                    rc.a = Mathf.Max(0f, rc.a - Time.deltaTime * 3f);
                    tutorialRightArrow.color = rc;
                }
                if (tutorialPlatformImage != null)
                {
                    Color pc = tutorialPlatformImage.color;
                    pc.a = Mathf.Max(0f, pc.a - Time.deltaTime * 3f);
                    tutorialPlatformImage.color = pc;
                }
                if (tutorialReadyText != null)
                {
                    float t = Mathf.Clamp01((mTutorialSinceBothSeenTimer - TUTORIAL_READY_DELAY) / 0.35f);
                    float pulseAlpha = 0.75f + Mathf.Sin(Time.time * 3.0f) * 0.25f;
                    tutorialReadyText.color = new Color(NEON_GOLD.r, NEON_GOLD.g, NEON_GOLD.b, t * pulseAlpha);
                }
                if (tutorialReadyHintText != null)
                {
                    float t = Mathf.Clamp01((mTutorialSinceBothSeenTimer - TUTORIAL_READY_DELAY - 0.2f) / 0.4f);
                    tutorialReadyHintText.color = new Color(DIM_TEXT.r, DIM_TEXT.g, DIM_TEXT.b, t * 0.9f);
                }
                TickTutorialReadyTap();
            }
        }
    }

    // Ready? accepts the next tap as both "finish tutorial" and "open the mode". The
    // tap direction becomes the first force of the real run via Sphere.ExitTutorial.
    void TickTutorialReadyTap()
    {
        if (!mTutorialReady || mSphere == null) return;

        float tapSign = 0f;
        if (Input.GetMouseButtonDown(0))
        {
            tapSign = Input.mousePosition.x < Screen.width * 0.5f ? 1f : -1f;
        }
        else if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began)
        {
            tapSign = Input.touches[0].position.x < Screen.width * 0.5f ? 1f : -1f;
        }
#if UNITY_STANDALONE || UNITY_EDITOR
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            tapSign = 1f;
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            tapSign = -1f;
        else if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            tapSign = 1f;
#endif

        if (tapSign != 0f)
        {
            mSphere.ExitTutorial(tapSign);
            mTutorialReady = false;
            mTutorialSinceBothSeenTimer = -1f;
            // State transition picked up next frame: IsTutorialActive is now false, so
            // detection lands on State.Playing.
        }
    }

}
