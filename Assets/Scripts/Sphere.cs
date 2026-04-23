using UnityEngine;
using UnityEngine.EventSystems;

public class Sphere : MonoBehaviour
{
    private Vector3 mNormal;
    private Vector3 mLastUsedNormal;
    private Vector3 mPrevPosition;

    public Camera mCamera;
    public GameObject mTorus;
    private Torus mTorusScript;

    private Vector3 mBeginPosition;
    private Rigidbody mRigid;

    private bool mGameOver = false;
    private bool mWaitingToStart = true; // Title screen state
    private float mOriginalDrag;
    private float mOriginalAngularDrag;
    private Vector3 mCameraStartPos;
    private Quaternion mCameraStartRot;

    // MANUAL PARAM: Impulse multiplier — increase to make taps stronger
    public float impulseMultiplier = 1.0f;

    // DEBUG: disable obstacle collision, press W to simulate death
    private bool mDebugGodMode = false;

    // MFi gamepad — d-pad/stick edge detection
    private bool mPadLeftFired = false;
    private bool mPadRightFired = false;
    private const float PAD_THRESHOLD = 0.5f;

    // Tutorial — set by GetForwardVector so ScoreSync can detect left/right coverage.
    // Consumed via public Was/Consume helpers; we never clear on its own.
    private bool mTutorialLeftFired;
    private bool mTutorialRightFired;
    // Death during tutorial turns into a ball-reset + one-shot flag the tutorial UI
    // reads to flash a "don't hit the walls" message, then clears.
    private bool mTutorialDeathPending;

    // Rewind
    public RewindSystem mRewindSystem;

    // Spline track (optional — null when using classic torus)
    public SplineGameController mSplineController;

    // Blitz mode beam (optional — null when not in Blitz)
    public BlitzBeam mBlitzBeam;
    public Material mBlitzShieldMat;
    private GameObject mShieldVisual;

    // Cached references
    private ScoreSync mScoreSync;
    private GameAudio mAudio;

    // Mode-commit startup sequence: tap a mode button → title dismisses → obstacles
    // spawn → torus unpauses. Ball is held kinematic through Dismissing + Spawning so
    // the player sees a clear three-beat opening instead of everything happening at
    // once. tvOS remote taps buffer their direction into mPendingStartForce.
    private enum StartPhase { Idle, Dismissing, Spawning, Running }
    private StartPhase mStartPhase = StartPhase.Idle;
    private float mStartPhaseTimer;
    private Vector3 mPendingStartForce;
    private bool mHasPendingStartForce;
    private const float START_DISMISS_DURATION = 0.5f; // matches ScoreSync.TITLE_FADE_DURATION
    private const float START_SPAWN_DURATION = 0.6f;

    // Blitz smooth-restart: ball stays kinematic while Torus runs its dismiss→spawn
    // animation. Update() polls Torus.IsBlitzTransitionActive() and restores physics
    // once it flips back to None.
    private bool mBlitzTransitionPending;
    private Vector3 mBlitzCamLerpStartPos;
    private Quaternion mBlitzCamLerpStartRot;
    private float mBlitzCamLerpTimer;
    private const float BLITZ_CAM_LERP_DURATION = 0.45f; // matches Torus dismiss phase

    // Tutorial exit camera recovery: player's free-swinging during tutorial leaves the
    // camera tilted; lerp it back so the real run doesn't begin with a one-frame snap.
    private bool mTutorialCamLerpActive;
    private Vector3 mTutorialCamLerpStartPos;
    private Quaternion mTutorialCamLerpStartRot;
    private float mTutorialCamLerpTimer;
    private const float TUTORIAL_CAM_LERP_DURATION = 0.45f;

    // Persistent stats. Cross-mode counters (STAT_TAPS / STAT_RUNS) stay authoritative
    // for career leaderboards (TapMaster_Total, Runs_Total) and cross-mode achievements
    // (ACH_DEDICATED etc.). Mode-specific counters were added when Blitz got its own
    // stats panel — they increment alongside the career totals so history is not lost.
    private const string STAT_TAPS = "TotalTaps";
    private const string STAT_RUNS = "TotalRuns";
    private const string STAT_TAPS_BLITZ = "TotalTaps_Blitz";
    private const string STAT_RUNS_BLITZ = "TotalRuns_Blitz";
    private const string STAT_OBSTACLES_BLITZ = "TotalObstacles_Blitz";
    private int mSessionTaps = 0;

    void Awake()
    {
        QualitySettings.vSyncCount = 0; // Disable VSync so targetFrameRate is respected
        Application.targetFrameRate = 60;
        Input.multiTouchEnabled = false;

#if UNITY_TVOS
        UnityEngine.tvOS.Remote.reportAbsoluteDpadValues = true;
        UnityEngine.tvOS.Remote.allowExitToHome = true;
#endif
    }

    void Start()
    {
        mTorusScript = mTorus.GetComponent<Torus>();
        mBeginPosition = transform.position;
        mRigid = GetComponent<Rigidbody>();
        mPrevPosition = transform.position;
        mOriginalDrag = mRigid.linearDamping;
        mOriginalAngularDrag = mRigid.angularDamping;
        mCameraStartPos = mCamera.transform.position;
        mCameraStartRot = mCamera.transform.rotation;

        mScoreSync = FindAnyObjectByType<ScoreSync>();
        mAudio = FindAnyObjectByType<GameAudio>();

        // Blitz shield visual — transparent green sphere around ball. Created
        // unconditionally since mode isn't committed yet at Start(); visibility is
        // driven by Torus.IsShieldActive() which only becomes true in Blitz.
        if (mBlitzShieldMat != null)
        {
            GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shield.name = "ShieldVisual";
            Object.Destroy(shield.GetComponent<Collider>());
            shield.transform.SetParent(transform, false);
            shield.transform.localScale = Vector3.one * 1.08f;
            MeshRenderer smr = shield.GetComponent<MeshRenderer>();
            smr.material = mBlitzShieldMat;
            smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            smr.receiveShadows = false;
            shield.SetActive(false);
            mShieldVisual = shield;
        }

        // Start paused — waiting for mode pick on title screen. Beam inactive until Blitz commit.
        mTorusScript.SetPaused(true);
        if (mBlitzBeam != null) mBlitzBeam.SetActive(false);
    }

    bool IsPointerOverUI()
    {
        var es = EventSystem.current;
        if (es == null) return false;
        // Mouse pointer (editor, desktop)
        if (es.IsPointerOverGameObject()) return true;
        // Touch: the no-arg overload ignores fingers, so check each active touch by id.
        // Without this, taps on game-over buttons on iOS fire the button AND the "any tap
        // → DoReset" handler below, making Blitz buttons feel broken.
        for (int i = 0; i < Input.touchCount; i++)
        {
            if (es.IsPointerOverGameObject(Input.touches[i].fingerId))
                return true;
        }
        return false;
    }

    void Update()
    {
        // Block all input during splash screen, settings, stats, pause, or Steam overlay
        if (mScoreSync != null && (mScoreSync.IsSplash() || mScoreSync.IsSettingsOpen() || mScoreSync.IsStatsOpen() || mScoreSync.IsPaused())) return;
#if STEAMWORKS
        if (SteamManager.Initialized && Steamworks.SteamUtils.IsOverlayEnabled()
            && Steamworks.SteamUtils.BOverlayNeedsPresent()) return;
#endif


        // Blitz shield visual toggle
        if (mShieldVisual != null)
            mShieldVisual.SetActive(mTorusScript.IsShieldActive());

        // Blitz transition: un-lean the camera in parallel with Torus's dismiss animation.
        if (mBlitzTransitionPending && mBlitzCamLerpTimer < BLITZ_CAM_LERP_DURATION)
        {
            mBlitzCamLerpTimer += Time.deltaTime;
            float ct = Mathf.Clamp01(mBlitzCamLerpTimer / BLITZ_CAM_LERP_DURATION);
            float ce = ct < 0.5f ? 4f * ct * ct * ct : 1f - Mathf.Pow(-2f * ct + 2f, 3f) * 0.5f;
            mCamera.transform.position = Vector3.Lerp(mBlitzCamLerpStartPos, mCameraStartPos, ce);
            mCamera.transform.rotation = Quaternion.Slerp(mBlitzCamLerpStartRot, mCameraStartRot, ce);
        }

        // Tutorial exit: lerp camera rotation/position back to rest. Runs in parallel with
        // the spawn anim (0.6s) and finishes before the opening force fires.
        if (mTutorialCamLerpActive)
        {
            mTutorialCamLerpTimer += Time.deltaTime;
            float tt = Mathf.Clamp01(mTutorialCamLerpTimer / TUTORIAL_CAM_LERP_DURATION);
            float te = tt < 0.5f ? 4f * tt * tt * tt : 1f - Mathf.Pow(-2f * tt + 2f, 3f) * 0.5f;
            mCamera.transform.position = Vector3.Lerp(mTutorialCamLerpStartPos, mCameraStartPos, te);
            mCamera.transform.rotation = Quaternion.Slerp(mTutorialCamLerpStartRot, mCameraStartRot, te);
            if (tt >= 1f)
            {
                mCamera.transform.position = mCameraStartPos;
                mCamera.transform.rotation = mCameraStartRot;
                mTutorialCamLerpActive = false;
            }
        }

        // Blitz transition finish-up — restore physics once the dismiss→spawn cycle ends.
        if (mBlitzTransitionPending && !mTorusScript.IsBlitzTransitionActive())
        {
            mBlitzTransitionPending = false;
            mRigid.isKinematic = false;
            mRigid.linearDamping = mOriginalDrag;
            mRigid.angularDamping = mOriginalAngularDrag;
            mRigid.linearVelocity = Vector3.zero;
            mRigid.angularVelocity = Vector3.zero;
            transform.position = mBeginPosition;
            mPrevPosition = transform.position;
            mCamera.transform.position = mCameraStartPos;
            mCamera.transform.rotation = mCameraStartRot;
        }

        // Suppress input while Torus is mid-transition (ball is kinematic, world is animating).
        if (mBlitzTransitionPending) return;

        // Title state — the 2-mode picker owns input. Mode buttons call StartGame via
        // ScoreSync after setting GameConfig.ActiveMode. tvOS has no on-screen picker,
        // so the remote/gamepad commits to the last-active mode directly. The first
        // force is buffered and applied at the end of the startup sequence.
        if (mWaitingToStart)
        {
#if UNITY_TVOS
            int remoteTap = GetRemoteTap();
            if (remoteTap != 0)
            {
                StartGame();
                mPendingStartForce = GetForwardVector(remoteTap < 0 ? 1f : -1f);
                mHasPendingStartForce = true;
            }
            else
            {
                int padTap = GetGamepadTap();
                if (padTap != 0)
                {
                    StartGame();
                    mPendingStartForce = GetForwardVector(padTap < 0 ? 1f : -1f);
                    mHasPendingStartForce = true;
                }
            }
#endif
            return;
        }

        // Startup sequence: hold gameplay input/physics while the title dismisses and
        // obstacles materialize. TickStartSequence flips us to Running when done.
        if (IsStartSequencing())
        {
            TickStartSequence();
            return;
        }

        // DEBUG: W key simulates death
        if (mDebugGodMode && !mGameOver && Input.GetKeyDown(KeyCode.W))
        {
            TriggerGameOver();
        }

        // Game over state — any tap resets
        if (mGameOver)
        {
            // Wait for game over animation before allowing reset
            if (mScoreSync != null && !mScoreSync.CanRestart()) return;

#if UNITY_TVOS
            if (GetRemoteTap() != 0 || GetGamepadTap() != 0)
#else
            if (GetGamepadTap() != 0 ||
                (Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1) && !Input.GetMouseButtonDown(2)) ||
                (!IsPointerOverUI() && Input.touchCount == 0 && Input.GetMouseButtonDown(0)) ||
                (!IsPointerOverUI() && Input.touchCount == 1 && Input.touches[0].phase == TouchPhase.Began))
#endif
            {
                DoReset();
                return;
            }
        }

        if (!mGameOver)
        {
#if UNITY_TVOS
            int remoteTap = GetRemoteTap();
            if (remoteTap != 0)
                ApplyForceWithForwardVector(GetForwardVector(remoteTap < 0 ? 1f : -1f));
#else
            // Keyboard: A/Left = left, D/Right = right
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                ApplyForceWithForwardVector(GetForwardVector(1f));
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                ApplyForceWithForwardVector(GetForwardVector(-1f));

            // Mouse: left half = left, right half = right (desktop only)
            if (!IsPointerOverUI() && Input.touchCount == 0 && Input.GetMouseButtonDown(0))
            {
                if (Input.mousePosition.x < Screen.width * 0.5f)
                    ApplyForceWithForwardVector(GetForwardVector(1f));
                else
                    ApplyForceWithForwardVector(GetForwardVector(-1f));
            }

            // Touch: left half = left, right half = right
            if (!IsPointerOverUI() && Input.touchCount == 1 && Input.touches[0].phase == TouchPhase.Began)
            {
                if (Input.touches[0].position.x < Screen.width * 0.5f)
                    ApplyForceWithForwardVector(GetForwardVector(1f));
                else
                    ApplyForceWithForwardVector(GetForwardVector(-1f));
            }
#endif

            // MFi gamepad (all platforms)
            int padTap = GetGamepadTap();
            if (padTap != 0)
                ApplyForceWithForwardVector(GetForwardVector(padTap < 0 ? 1f : -1f));
        }

        // Camera diff (swing effect) — only while alive
        if (!mGameOver)
        {
            Vector3 diff = mPrevPosition - transform.position;
            mPrevPosition = transform.position;

            CameraSwing script = mCamera.GetComponent<CameraSwing>();
            if (script != null)
                script.mDiff += diff * 0.35f;
        }

        // Keep awake
        if (mRigid.IsSleeping())
            mRigid.WakeUp();
    }

#if UNITY_TVOS
    /// <summary>
    /// Detect Siri Remote input. Remote held horizontally:
    /// Clickpad press (JoystickButton14) = left impulse
    /// Play/Pause   (JoystickButton15) = right impulse
    /// Returns -1 (left), 1 (right), or 0 (none).
    /// </summary>
    int GetRemoteTap()
    {
        // Two-button mode: clickpad = left, Play/Pause = right
        if (Input.GetKeyDown(KeyCode.JoystickButton14))
            return -1;
        if (Input.GetKeyDown(KeyCode.JoystickButton15))
            return 1;

        return 0;
    }
#endif

    /// <summary>
    /// MFi gamepad input — all left-side buttons = left, all right-side = right.
    /// Works on iOS, macOS, and tvOS. Returns -1 (left), 1 (right), or 0 (none).
    /// </summary>
    int GetGamepadTap()
    {
        // Shoulders
        if (Input.GetKeyDown(KeyCode.JoystickButton4)) return -1; // L1
        if (Input.GetKeyDown(KeyCode.JoystickButton5)) return 1;  // R1

        // Triggers
        if (Input.GetKeyDown(KeyCode.JoystickButton6)) return -1; // L2
        if (Input.GetKeyDown(KeyCode.JoystickButton7)) return 1;  // R2

        // D-pad / left stick — silent fallback, not advertised in tutorial art. Edge triggered.
        float h = Input.GetAxis("Horizontal");
        if (h < -PAD_THRESHOLD && !mPadLeftFired)
        {
            mPadLeftFired = true;
            return -1;
        }
        if (h > PAD_THRESHOLD && !mPadRightFired)
        {
            mPadRightFired = true;
            return 1;
        }
        if (h > -0.1f) mPadLeftFired = false;
        if (h < 0.1f) mPadRightFired = false;

        return 0;
    }

    /// <summary>
    /// Get the forward vector for force calculation.
    /// Torus mode: hardcoded ±X. Spline mode: spline tangent direction.
    /// Sign: +1 = left tap, -1 = right tap.
    /// </summary>
    Vector3 GetForwardVector(float sign)
    {
        if (GameConfig.IsTutorialActive)
        {
            if (sign > 0f) mTutorialLeftFired = true;
            else           mTutorialRightFired = true;
        }
        if (mSplineController != null)
            return mSplineController.GetBallForwardDirection() * sign;
        return new Vector3(sign, 0f, 0f);
    }

    public bool WasTutorialLeftFired()  { return mTutorialLeftFired; }
    public bool WasTutorialRightFired() { return mTutorialRightFired; }
    public void ResetTutorialInputs()
    {
        mTutorialLeftFired = false;
        mTutorialRightFired = false;
    }
    public bool ConsumeTutorialDeath()
    {
        bool v = mTutorialDeathPending;
        mTutorialDeathPending = false;
        return v;
    }

    /// Called from OnCollisionEnter when IsTutorialActive — resets ball to start
    /// position with zero velocity, sets the death flag for ScoreSync to flash a
    /// nudge. No mGameOver, no life deduction, tutorial continues.
    void ResetBallForTutorial()
    {
        transform.position = mBeginPosition;
        if (mRigid != null)
        {
            mRigid.linearVelocity = Vector3.zero;
            mRigid.angularVelocity = Vector3.zero;
        }
        mTutorialDeathPending = true;
    }

    /// Finish the tutorial and hand off to the stashed mode. Re-enters the spawn
    /// phase so obstacles materialize with their normal entrance anim; the tap that
    /// dismissed "Ready?" becomes the opening force via mPendingStartForce.
    public void ExitTutorial(float tapSign)
    {
        GameConfig.IsTutorialActive = false;
        GameConfig.MarkTutorialSeen();
        mTutorialLeftFired = false;
        mTutorialRightFired = false;
        mTutorialDeathPending = false;

        mTorusScript.SetPaused(true);
        mTorusScript.Reset(true);
        mTorusScript.BeginInitialSpawnAnim();

        transform.position = mBeginPosition;
        mPrevPosition = transform.position;
        if (mRigid != null)
        {
            mRigid.linearVelocity = Vector3.zero;
            mRigid.angularVelocity = Vector3.zero;
        }

        // Camera recovery — snapshot current tilt and lerp back to rest over
        // TUTORIAL_CAM_LERP_DURATION so the real run doesn't snap in one frame.
        mTutorialCamLerpStartPos = mCamera.transform.position;
        mTutorialCamLerpStartRot = mCamera.transform.rotation;
        mTutorialCamLerpTimer = 0f;
        mTutorialCamLerpActive = true;

        CameraSwing swing = mCamera.GetComponent<CameraSwing>();
        if (swing != null)
        {
            swing.mDiff = Vector3.zero;
            swing.ResetSpring();
        }

        // Buffer the Ready? tap as the opening force — fires when TickStartSequence
        // transitions into Running after SPAWN_DURATION.
        mPendingStartForce = GetForwardVector(tapSign);
        mHasPendingStartForce = true;

        mStartPhase = StartPhase.Spawning;
        mStartPhaseTimer = 0f;
    }

    void ApplyForceWithForwardVector(Vector3 forward)
    {
        bool clearLast = false;

        if (mRigid && mRigid.IsSleeping())
        {
            mRigid.WakeUp();
            mNormal = mLastUsedNormal;
        }

        if (mNormal.magnitude == 0.0f)
        {
            mNormal = mLastUsedNormal;
            clearLast = true;
        }

        if (mRigid && mNormal.magnitude > 0.0f)
        {
            Vector3 side = Vector3.Cross(forward, mNormal).normalized;
            Vector3 up = new Vector3(0.0f, 1.0f, 0.0f);
            float dot = Vector3.Dot(up, mNormal);

            float sin = Mathf.Sin((Mathf.PI - (Mathf.Asin(dot) * 2.0f)) * 0.625f);
            side *= (sin + (dot * 0.12f) + 0.28f) * impulseMultiplier;

            mRigid.AddForce(side, ForceMode.Impulse);

            if (clearLast)
                mLastUsedNormal = Vector3.zero;
            else
                mLastUsedNormal = mNormal;
        }

        mTorusScript.UserInteraction();

        if (mAudio != null) mAudio.PlayTap();

        // Track taps — save every 10 to avoid I/O spam
        mSessionTaps++;
        if (mSessionTaps % 10 == 0)
            SaveTaps();
    }

    public void DoReset()
    {
        IncrementRuns();

        if (GameConfig.IsBlitz())
        {
            // Smooth transition — Torus animates obstacles out + slides the ball back
            // to the tube bottom, then spawns fresh obstacles ahead. mAngle and torus
            // rotation are preserved so there's no visual snap-back to the origin.
            mGameOver = false;
            mRigid.linearVelocity = Vector3.zero;
            mRigid.angularVelocity = Vector3.zero;
            mRigid.isKinematic = true;
            mBlitzTransitionPending = true;

            mBlitzCamLerpStartPos = mCamera.transform.position;
            mBlitzCamLerpStartRot = mCamera.transform.rotation;
            mBlitzCamLerpTimer = 0f;

            mTorusScript.StartBlitzTransition(transform.position, mBeginPosition);

            DeathEffect death = mCamera.GetComponent<DeathEffect>();
            if (death != null) death.ResetShake();

            CameraSwing swing = mCamera.GetComponent<CameraSwing>();
            if (swing != null)
            {
                swing.mDiff = Vector3.zero;
                swing.ResetSpring();
            }
            return;
        }

        bool rewindHandled = mRewindSystem != null && mRewindSystem.IsComplete();

        if (mRewindSystem != null)
        {
            mRewindSystem.ResetSystem();
            mRewindSystem.StartRecording();
        }

        if (!rewindHandled)
            mTorusScript.Reset();

        if (mSplineController == null)
            mTorusScript.SetPaused(false);
        mGameOver = false;
        mRigid.isKinematic = false;
        mRigid.linearDamping = mOriginalDrag;
        mRigid.angularDamping = mOriginalAngularDrag;
        mRigid.linearVelocity = Vector3.zero;
        mRigid.angularVelocity = Vector3.zero;

        // In spline mode, reset to spline start; otherwise torus start
        if (mSplineController != null)
            mSplineController.ResetBall();
        else
            transform.position = mBeginPosition;
        mPrevPosition = transform.position;

        // In spline mode, SplineCameraFollow handles camera positioning
        if (mSplineController == null)
        {
            mCamera.transform.position = mCameraStartPos;
            mCamera.transform.rotation = mCameraStartRot;
        }

        DeathEffect death2 = mCamera.GetComponent<DeathEffect>();
        if (death2 != null)
            death2.ResetShake();

        CameraSwing swing2 = mCamera.GetComponent<CameraSwing>();
        if (swing2 != null)
        {
            swing2.mDiff = Vector3.zero;
            swing2.ResetSpring();
        }

        SplineCameraFollow splineCam = mCamera.GetComponent<SplineCameraFollow>();
        if (splineCam != null)
            splineCam.ResetSpring();

    }

    void TriggerGameOver()
    {
        mGameOver = true;
        mTorusScript.GameOver();

        mRigid.linearDamping = 8f;
        mRigid.angularDamping = 8f;

        CameraSwing swing = mCamera.GetComponent<CameraSwing>();
        if (swing != null) swing.mDiff = Vector3.zero;

        DeathEffect death = mCamera.GetComponent<DeathEffect>();
        if (death != null) death.TriggerDeath(transform.position);

        // Rewind only in Pure Hell
        if (!GameConfig.IsBlitz() && mRewindSystem != null)
            mRewindSystem.OnDeath();

        // Deactivate beam in Blitz
        if (GameConfig.IsBlitz() && mBlitzBeam != null)
            mBlitzBeam.SetActive(false);

        if (mAudio != null)
        {
            if (GameConfig.IsBlitz()) mAudio.PlayBlitzDeath();
            else mAudio.PlayGameOver();
        }
    }

    public bool IsWaiting() { return mWaitingToStart; }
    public bool IsGameOver() { return mGameOver; }

    /// <summary>Commit to the selected mode and kick off the three-phase startup
    /// sequence: title dismiss → obstacle spawn → torus rotate. Actual Reset and
    /// SetPaused fire later from TickStartSequence as each phase completes.</summary>
    public void StartGame()
    {
        if (!mWaitingToStart) return;
        if (mStartPhase != StartPhase.Idle) return; // already sequencing

        // First-run onboarding — covers tvOS where the mode picker isn't visited.
        // On iOS/Android/macOS this is already set by ScoreSync.StartWithMode, which
        // also resets the tutorial UI timers; setting the flag again is harmless.
        if (!GameConfig.HasSeenTutorial())
            GameConfig.IsTutorialActive = true;

        mWaitingToStart = false; // ScoreSync will transition Title→Playing; title fade begins
        mStartPhase = StartPhase.Dismissing;
        mStartPhaseTimer = 0f;
    }

    bool IsStartSequencing()
    {
        return mStartPhase == StartPhase.Dismissing || mStartPhase == StartPhase.Spawning;
    }

    void TickStartSequence()
    {
        mStartPhaseTimer += Time.deltaTime;

        if (mStartPhase == StartPhase.Dismissing && mStartPhaseTimer >= START_DISMISS_DURATION)
        {
            // Phase 2 — spawn obstacles for the committed mode. Torus still paused.
            // Spawn-in anim matches the post-death materialize (staggered _SpawnProgress 0→1).
            mTorusScript.Reset(true);
            mTorusScript.BeginInitialSpawnAnim();
            mStartPhase = StartPhase.Spawning;
            mStartPhaseTimer = 0f;
        }
        else if (mStartPhase == StartPhase.Spawning && mStartPhaseTimer >= START_SPAWN_DURATION)
        {
            // Phase 3 — world wakes up: torus rotates, mode systems arm, stats tick.
            // Tutorial entry skips mode arming (beam/rewind) and run counting — the
            // real mode arms later when Sphere.ExitTutorial re-enters the spawn phase.
            bool tutorialEntry = GameConfig.IsTutorialActive;

            if (mSplineController == null)
                mTorusScript.SetPaused(false);

            if (!tutorialEntry)
            {
                if (GameConfig.IsBlitz())
                {
                    if (mBlitzBeam != null) mBlitzBeam.SetActive(true);
                    if (GameConfig.BlitzDebugMaxPower)
                        mTorusScript.DebugApplyMaxBlitzPower();
                }
                else
                {
                    if (mRewindSystem != null)
                        mRewindSystem.StartRecording();
                }

                IncrementRuns();
            }

            if (mHasPendingStartForce)
            {
                ApplyForceWithForwardVector(mPendingStartForce);
                mHasPendingStartForce = false;
                mPendingStartForce = Vector3.zero;
            }

            mStartPhase = StartPhase.Running;
        }
    }

    /// <summary>Return to title state — clears torus, parks ball, pauses. Called from back-to-modes button.</summary>
    public void ReturnToTitle()
    {
        mWaitingToStart = true;
        mGameOver = false;
        mBlitzTransitionPending = false;
        mStartPhase = StartPhase.Idle;
        mStartPhaseTimer = 0f;
        mHasPendingStartForce = false;

        // Clear torus obstacles without respawning — stays empty until next mode commit.
        mTorusScript.Reset(false);
        mTorusScript.SetPaused(true);

        // Park ball at start, reset physics.
        mRigid.isKinematic = false;
        mRigid.linearDamping = mOriginalDrag;
        mRigid.angularDamping = mOriginalAngularDrag;
        mRigid.linearVelocity = Vector3.zero;
        mRigid.angularVelocity = Vector3.zero;
        transform.position = mBeginPosition;
        mPrevPosition = transform.position;

        mCamera.transform.position = mCameraStartPos;
        mCamera.transform.rotation = mCameraStartRot;

        CameraSwing swing = mCamera.GetComponent<CameraSwing>();
        if (swing != null) { swing.mDiff = Vector3.zero; swing.ResetSpring(); }

        DeathEffect death = mCamera.GetComponent<DeathEffect>();
        if (death != null) death.ResetShake();

        if (mRewindSystem != null) mRewindSystem.ResetSystem();

        if (mBlitzBeam != null) mBlitzBeam.SetActive(false);
    }
    public bool IsRewinding()
    {
        return mRewindSystem != null && mRewindSystem.IsPausingOrRewinding();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.contacts.Length > 0 && collision.gameObject.name == "Psychokinesis3")
        {
            mNormal = collision.contacts[0].normal;
        }

        if (!mGameOver && !mDebugGodMode && !mBlitzTransitionPending && (collision.gameObject.name == "torusObstacle" || collision.gameObject.name == "Mesh" || collision.gameObject.name == "BlitzBox" || collision.gameObject.name == "BlitzGateBar" || collision.gameObject.name == "BlitzDividerBar"))
        {
            // Tutorial: no obstacles to hit, but tube walls (Mesh) still lethal. Turn
            // every lethal collision into a ball reset so the player learns the rule
            // without ending the run.
            if (GameConfig.IsTutorialActive)
            {
                ResetBallForTutorial();
                return;
            }

            // Blitz shield absorbs one lethal hit
            if (GameConfig.IsBlitz() && mTorusScript.ConsumeShield())
            {
                if (mAudio != null)
                {
                    mAudio.PlayShieldAbsorb();
                    mAudio.PlayVoiceShieldLost();
                }
                // Visual feedback — camera death flash
                DeathEffect death = mCamera.GetComponent<DeathEffect>();
                if (death != null) death.TriggerDeath(transform.position);
                return;
            }

            TriggerGameOver();
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.contacts.Length > 0 && collision.gameObject.name == "Psychokinesis3")
        {
            mNormal = collision.contacts[0].normal;
        }
    }

    // ── PERSISTENT STATS ─────────────────────────────────────

    void IncrementRuns()
    {
        PlayerPrefs.SetInt(STAT_RUNS, PlayerPrefs.GetInt(STAT_RUNS, 0) + 1);
        if (GameConfig.IsBlitz())
            PlayerPrefs.SetInt(STAT_RUNS_BLITZ, PlayerPrefs.GetInt(STAT_RUNS_BLITZ, 0) + 1);
        PlayerPrefs.Save();
        CloudSync.PushAll();
    }

    void SaveTaps()
    {
        PlayerPrefs.SetInt(STAT_TAPS, PlayerPrefs.GetInt(STAT_TAPS, 0) + mSessionTaps);
        if (GameConfig.IsBlitz())
            PlayerPrefs.SetInt(STAT_TAPS_BLITZ, PlayerPrefs.GetInt(STAT_TAPS_BLITZ, 0) + mSessionTaps);
        PlayerPrefs.Save();
        mSessionTaps = 0;
        CloudSync.PushAll();
    }

    void OnApplicationPause(bool paused)
    {
        if (paused && mSessionTaps > 0) SaveTaps();
    }

    void OnApplicationQuit()
    {
        if (mSessionTaps > 0) SaveTaps();
    }

    public static int GetTotalTaps() { return PlayerPrefs.GetInt(STAT_TAPS, 0); }
    public static int GetTotalRuns() { return PlayerPrefs.GetInt(STAT_RUNS, 0); }

    public static int GetBlitzTaps() { return PlayerPrefs.GetInt(STAT_TAPS_BLITZ, 0); }
    public static int GetBlitzRuns() { return PlayerPrefs.GetInt(STAT_RUNS_BLITZ, 0); }
    public static int GetBlitzObstacles() { return PlayerPrefs.GetInt(STAT_OBSTACLES_BLITZ, 0); }

    public static void IncrementBlitzObstacles()
    {
        PlayerPrefs.SetInt(STAT_OBSTACLES_BLITZ, PlayerPrefs.GetInt(STAT_OBSTACLES_BLITZ, 0) + 1);
    }
}
