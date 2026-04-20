using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class Torus : MonoBehaviour
{
    // Ball reference for score pulse position
    public Transform mBallTransform;

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void AudioServicesPlaySystemSound(uint soundID);
#endif

    static void LightHaptic()
    {
#if UNITY_IOS && !UNITY_EDITOR
        AudioServicesPlaySystemSound(1519); // Peek — light tap
#endif
    }
    static void HeavyHaptic()
    {
#if UNITY_IOS && !UNITY_EDITOR
        AudioServicesPlaySystemSound(1520); // Pop — heavy tap
#endif
    }
    private float mAngle = 0.0f;
    private float mAngleScore = 0.0f;
    // MANUAL PARAM: Rotation speed — original constant speed from the version you loved
    private float mAngleStep = 0.17f;
    private int mRounds = 0;
    private float mObstacleStep = 4.0f;
    private float mObstacleStepInv;

    // MANUAL PARAM: First obstacle distance — higher = more reaction time at start
    private float mObstaclesAngleOrigin = 20.0f;
    private Obstacle mLastObstacle = null;
    private Obstacle mCurrentObstacle = null;

    private Quaternion mBeginRotation;

    public TextMesh mScoreLbl;
    private int mScore = 0;
    private int mScoreWithoutInteraction = 0;

    private bool mGameOver = false;

    // ── SWING DETECTION (tier 1) ─────────────────────────────
    // Pure flow: no taps between gates, ball swings through varied positions
    private float mLastPassAngle = float.MinValue;
    private float mSwingAccumulated = 0f;
    private int mSwingGates = 0;
    private float mSwingCooldown = 0f;
    private const float SWING_COOLDOWN = 10f;
    private const float SWING_TIER1_ANGLE = 25f;

    // ── MILESTONE (tier 2) ───────────────────────────────────
    // Every 5 points — "on fire" energy
    private int mNextMilestone = 5;
    private const int MILESTONE_INTERVAL = 5;

    // Pending events for audio system to consume
    private int mPendingSwingTier = 0; // 0=none, 1=swing, 2=milestone
    private bool mPendingGatePass = false;

    private List<Obstacle> mObstacles;

    public Material mObstacleTop;
    public Material mObstacleFront;
    public Material mObstacleShadow;

    // Blitz mode
    public Material mBlitzBoxMat;
    public Material mBlitzGateMat;
    public Material mBlitzButtonMat;
    public Material mBlitzConnectionMat;
    public BlitzBeam mBlitzBeam;
    private List<BlitzBox> mBlitzBoxes;
    private List<BlitzGate> mBlitzGates;
    private List<BlitzDivider> mBlitzDividers;
    private float mBlitzTimer;
    private float mLastBlitzAngle;

    // Blitz intensity ramp — everything (speed, fire rate, spacing, kind mix) scales off
    // a single curve: intensity = mBlitzTimer / BLITZ_RAMP_SECONDS. Past 1.0 it keeps
    // climbing so the game self-terminates via a density/speed wall (no hard endpoint).
    private const float BLITZ_BASE_SPEED = 0.12f;
    private const float BLITZ_PEAK_SPEED = 0.374f;  // reached at intensity 1.0 (was 0.34, +10%)
    private const float BLITZ_RAMP_SECONDS = 120f;  // time to reach intensity 1.0

    // Content-aware spawn planner state
    enum BlitzSpawnKind { None, Formation, GateGap, GateButton, FullGate, Divider, GatePair, HalfGateMixed }
    private BlitzSpawnKind mLastSpawnKind = BlitzSpawnKind.None;
    private BlitzSpawnKind mPrevSpawnKind = BlitzSpawnKind.None;
    private bool mLastSpawnCommitsSide = false;

    // Run-to-run smooth transition (replaces the old hard reset). Phase A scales out
    // everything on screen + slides the ball to the tube bottom; Phase B destroys +
    // respawns fresh obstacles ahead. mAngle and torus rotation are preserved so the
    // world continues from wherever the previous run ended.
    public enum BlitzTransition { None, Dismissing, Spawning }
    private BlitzTransition mBlitzTransition = BlitzTransition.None;
    private float mBlitzTransitionTimer;
    private Vector3 mBlitzTransitionBallStart;
    private Vector3 mBlitzTransitionBallEnd;
    private List<Transform> mDismissTargets;
    private List<Vector3> mDismissStartScales;
    private const float BLITZ_DISMISS_DURATION = 0.45f;
    private const float BLITZ_SPAWN_DURATION = 0.5f;

    // Blitz orbs (upgrade pickups)
    public Material mBlitzOrbGunMat;
    public Material mBlitzOrbCadencyMat;
    public Material mBlitzOrbShieldMat;
    private List<BlitzOrb> mBlitzOrbs;
    private int mGunOrbCount;
    private int mCadencyOrbCount;
    private int mShieldOrbCount;
    private int mGunLevel;
    private int mCadencyLevel;
    private int mShieldLevel;
    private bool mShieldActive;
    private ScoreSync mScoreSync;
    private GameAudio mAudio;
    private const int ORBS_PER_UPGRADE = 5;

    // Kill-streak multiplier — Blitz only. Fills a bar per destroy (1HP=+1, 3HP=+2).
    // Bar full → tier up (x1→x2→x3→x4). After a 3s grace, bar drains; empty bar drops
    // a tier. Taking damage (or burning the shield) drops a tier + wipes the bar.
    // Multiplier applies to destroy points + gate bonus; chip/orb +5 awards stay flat.
    private float mStreakBar;
    private int mStreakTier = 1;
    private float mStreakGraceTimer;
    private const float STREAK_BAR_CAP = 5f;
    private const int STREAK_MAX_TIER = 4;
    private const float STREAK_GRACE = 3f;
    private const float STREAK_DRAIN_RATE = 1.0f;

    // Full-tube spectacle — Blitz-only sections where the upper tube fades in as pure
    // ambient decoration (ball physics stays bound to the half-tube, so the ceiling is
    // purely visual). No gate markers — players read portal rings as deadly obstacles
    // and that false threat cost more than it gave. The tube just appears and dissolves.
    public MeshRenderer mFullTubeOverlayRenderer; // wired by SceneSetup
    private float mFullTubeSectionStart = -9999f;
    private float mFullTubeSectionEnd = -9999f;
    private bool mFullTubeActive;
    private MaterialPropertyBlock mFullTubeBlock;
    private static readonly int RevealProgressID = Shader.PropertyToID("_RevealProgress");
    private const float FULL_TUBE_FADE_LEAD = 20f;
    private float mFullTubeCooldown;
    private bool mFullTubeFirstFired;
    private const float FULL_TUBE_FIRST_AT_SECONDS = 40f;

    // Initial spawn-in animation — fades obstacles up via the Gate shader's
    // _SpawnProgress property, staggered, matching the post-death materialize
    // effect. Runs while Torus stays paused (before the first frame of motion).
    private struct SpawnInItem { public MeshRenderer renderer; public float triggerTime; }
    private static readonly int SpawnProgressID = Shader.PropertyToID("_SpawnProgress");
    private List<SpawnInItem> mSpawnInItems;
    private MaterialPropertyBlock mSpawnInBlock;
    private bool mSpawnInActive;
    private float mSpawnInTimer;
    private const float SPAWN_IN_STAGGER = 0.015f;
    private const float SPAWN_IN_DURATION = 0.35f;

    void Awake()
    {
        mBeginRotation = transform.rotation;
    }

    void Start()
    {
        mObstacleStep *= Mathf.Deg2Rad;
        mObstacleStepInv = 1.0f / mObstacleStep;

        // Both mode's containers exist from the first frame. Neither spawns content until
        // the player picks a mode (Sphere.StartGame → Reset(true)). Until then the torus
        // geometry is visible but empty — it hasn't committed to either mode yet.
        mObstacles = new List<Obstacle>();
        mBlitzBoxes = new List<BlitzBox>();
        mBlitzGates = new List<BlitzGate>();
        mBlitzDividers = new List<BlitzDivider>();
        mBlitzOrbs = new List<BlitzOrb>();
    }

    void Update()
    {
        if (mBlitzTransition != BlitzTransition.None)
        {
            UpdateBlitzTransition();
            return;
        }

        // Initial spawn-in animation runs independently of pause so obstacles
        // materialize while the torus itself waits for Phase 3 to unpause.
        if (mSpawnInActive)
            TickSpawnIn();

        if (mGameOver || mPaused)
            return;

        // Tutorial: ball physics run normally (Sphere.Update), but the torus is frozen
        // — no rotation, no obstacle windowing, no scoring. This leaves a calm empty
        // tube so the player can focus on left/right swing.
        if (GameConfig.IsTutorialActive)
            return;

        // Constant speed — no acceleration (the version you loved)
        // MANUAL PARAM: mAngleStep controls game speed
        mAngle += mAngleStep;
        mAngleScore += mAngleStep;

        if (mAngleScore >= 360.0f)
        {
            mAngleScore -= 360.0f;
            mRounds++;
        }

        // Rotate mesh
        transform.Rotate(0.0f, 0.0f, -mAngleStep);

        // Blitz: boxes, electric gates, points scoring
        if (GameConfig.IsBlitz())
        {
            mBlitzTimer += Time.deltaTime;
            UpdateBlitzSpeed();
            UpdateBlitzObstacles();
            CheckBlitzOrbPickups();
            AnimateBlitzOrbs();
            AnimateBlitzGates();
            AnimateBlitzDividers();
            AnimateBlitzBoxes();
            UpdateBlitzFireRate();
            TickFullTubeSection();
            TickStreak();
            return;
        }

        // Swing cooldown tick
        if (mSwingCooldown > 0f)
            mSwingCooldown -= Time.deltaTime;

        // Score
        if (mCurrentObstacle != null && mAngle - 1.25f > mCurrentObstacle.mAngle)
        {
            mScore++;
            mScoreLbl.text = mScore.ToString();
            mCurrentObstacle = mCurrentObstacle.mNextOne;

            // ── Swing detection (tier 1) ─────────────────────
            // Check BEFORE incrementing — 0 means player tapped since last gate
            if (mBallTransform != null)
            {
                float ballAngle = GetBallCrossSectionAngle();
                bool noTap = mScoreWithoutInteraction > 0;

                if (mScore >= 2 && noTap && mLastPassAngle > -999f)
                {
                    float delta = Mathf.Abs(ballAngle - mLastPassAngle);
                    mSwingAccumulated += delta;
                    mSwingGates++;

                    if (mSwingCooldown <= 0f && mSwingAccumulated >= SWING_TIER1_ANGLE)
                    {
                        mPendingSwingTier = 1;
                        mSwingCooldown = SWING_COOLDOWN;

                        // Track swing for achievement
                        if (SteamService.Instance != null)
                            SteamService.Instance.OnSwingDetected();
                    }
                }
                else
                {
                    // Tapped since last gate — reset streak
                    mSwingAccumulated = 0f;
                    mSwingGates = 0;
                }

                mLastPassAngle = ballAngle;
            }

            mScoreWithoutInteraction++;

            // ── Milestone (tier 2) — overrides swing ─────────
            bool isMilestone = mScore >= mNextMilestone;
            if (isMilestone)
            {
                mPendingSwingTier = 2;
                mNextMilestone += MILESTONE_INTERVAL;
            }
            else
            {
                mPendingGatePass = true;
            }

            // Grid pulse wave — expanding ring from ball position
            if (mBallTransform != null)
            {
                if (isMilestone)
                {
                    // Gold milestone pulse — wider, brighter, separate slot
                    Shader.SetGlobalFloat("_MilestonePulseTime", Time.time);
                    Shader.SetGlobalVector("_MilestonePulsePos", mBallTransform.position);
                }
                else
                {
                    Shader.SetGlobalFloat("_ScorePulseTime", Time.time);
                    Shader.SetGlobalVector("_ScorePulsePos", mBallTransform.position);
                }

            }

            // Haptic feedback — heavier on milestones
            if (isMilestone)
                HeavyHaptic();
            else
                LightHaptic();
        }

        // Update obstacles
        UpdateObstacles();
    }

    void UpdateObstacles()
    {
        // GENERATING
        if (mLastObstacle == null)
        {
            mLastObstacle = generateObstacle();
            mCurrentObstacle = mLastObstacle;
        }

        while (mLastObstacle.mAngle < mAngle + 85.0f)
        {
            Obstacle newOne = generateObstacle();
            mLastObstacle.mNextOne = newOne;
            mLastObstacle = newOne;
        }

        // Hide obstacles >240° behind — rolling window, no hard loop boundaries.
        // Don't destroy — rewind needs them for replay.
        foreach (Obstacle currentObstacle in mObstacles)
        {
            if (currentObstacle.mAngle > mAngle - 240.0f)
                break;
            if (currentObstacle.mGameObject != null && currentObstacle.mGameObject.activeSelf)
                currentObstacle.mGameObject.SetActive(false);
        }
    }

    Obstacle generateObstacle()
    {
        bool isFirst = (mLastObstacle == null);

        // MANUAL PARAM: gap size range (15-21) controls difficulty
        // First gate: avoid center gap so player must tap to start
        Obstacle obstacle = new Obstacle(15, 21, mObstacleStepInv, mObstacleTop, mObstacleFront, mObstacleShadow,
                                         avoidCenter: isFirst);

        if (mLastObstacle != null)
            obstacle.mAngle = mLastObstacle.mAngle + 10.0f;
        else
            obstacle.mAngle = mObstaclesAngleOrigin;

        // POSITIONING
        obstacle.mGameObject.transform.parent = transform;
        obstacle.mGameObject.transform.Rotate(0.0f, 0.0f, obstacle.mAngle - mAngle);

        mObstacles.Add(obstacle);
        return obstacle;
    }

    public void GameOver()
    {
        mGameOver = true;
        mScoreLbl.text = mScore.ToString() + "\nGAME OVER";
    }

    public void Reset() { Reset(true); }

    /// Clears the world and resets run state. Both mode's obstacle sets are cleared
    /// unconditionally so switching modes never leaves stragglers. When spawnObstacles
    /// is false (ReturnToTitle) or tutorial is active, the torus stays empty.
    public void Reset(bool spawnObstacles)
    {
        if (GameConfig.IsTutorialActive) spawnObstacles = false;
        mAngle = 0.0f;
        mAngleScore = 0.0f;
        mRounds = 0;
        transform.rotation = mBeginRotation;

        mScore = 0;
        mScoreLbl.text = mScore.ToString();

        mGameOver = false;

        // Clear Pure Hell obstacles.
        foreach (Obstacle obstacle in mObstacles)
            Destroy(obstacle.mGameObject);
        mObstacles.Clear();
        mLastObstacle = null;
        mCurrentObstacle = null;

        // Clear Blitz obstacles.
        foreach (BlitzBox box in mBlitzBoxes) Destroy(box.mGameObject);
        mBlitzBoxes.Clear();
        foreach (BlitzGate gate in mBlitzGates) Destroy(gate.mGameObject);
        mBlitzGates.Clear();
        foreach (BlitzDivider div in mBlitzDividers) Destroy(div.mGameObject);
        mBlitzDividers.Clear();
        foreach (BlitzOrb orb in mBlitzOrbs) Destroy(orb.mGameObject);
        mBlitzOrbs.Clear();
        ClearFullTubeSection();

        // Blitz run state — always reset so leftover upgrades/levels never carry across
        // a mode switch or back-to-menu cycle.
        mBlitzTimer = 0f;
        mGunOrbCount = 0;
        mCadencyOrbCount = 0;
        mShieldOrbCount = 0;
        mGunLevel = 0;
        mCadencyLevel = 0;
        mShieldLevel = 0;
        mShieldActive = false;
        if (mBlitzBeam != null) mBlitzBeam.SetGunLevel(0);
        mLastSpawnKind = BlitzSpawnKind.None;
        mPrevSpawnKind = BlitzSpawnKind.None;
        mLastSpawnCommitsSide = false;
        ResetStreak();

        if (spawnObstacles)
        {
            if (GameConfig.IsBlitz())
            {
                mAngleStep = BLITZ_BASE_SPEED;
                if (mScoreSync == null) mScoreSync = FindAnyObjectByType<ScoreSync>();
                if (mAudio == null) mAudio = FindAnyObjectByType<GameAudio>();
                mLastBlitzAngle = mObstaclesAngleOrigin - 10f;
                UpdateBlitzObstacles();
            }
            else
            {
                UpdateObstacles();
            }
        }

        mScoreWithoutInteraction = 0;
        ResetSwing();
    }

    // ── BLITZ RUN-TO-RUN TRANSITION ──────────────────────────
    // Called by Sphere.DoReset (Blitz mode only). The world doesn't reset to mAngle=0;
    // instead we dismiss what's visible, slide the ball back to the tube bottom, and
    // spawn fresh obstacles ahead at the current angle. Score and upgrades still reset.

    public bool IsBlitzTransitionActive() { return mBlitzTransition != BlitzTransition.None; }

    public void StartBlitzTransition(Vector3 ballStart, Vector3 ballEnd)
    {
        if (!GameConfig.IsBlitz()) return;

        mBlitzTransition = BlitzTransition.Dismissing;
        mBlitzTransitionTimer = 0f;
        mBlitzTransitionBallStart = ballStart;
        mBlitzTransitionBallEnd = ballEnd;

        mPaused = true;
        mGameOver = false;

        if (mAudio != null) mAudio.PlayBlitzWipeOut();

        // Snapshot every visible obstacle's transform + scale so we can lerp it to zero.
        mDismissTargets = new List<Transform>();
        mDismissStartScales = new List<Vector3>();
        CollectDismissTarget(mBlitzBoxes);
        CollectDismissTarget(mBlitzGates);
        CollectDismissTarget(mBlitzDividers);
        CollectDismissTarget(mBlitzOrbs);

        // Kill the beam during the wipe so there's no stray plasma firing into emptiness.
        if (mBlitzBeam != null) mBlitzBeam.SetActive(false);
    }

    void CollectDismissTarget<T>(List<T> items) where T : class
    {
        if (items == null) return;
        foreach (T item in items)
        {
            GameObject go = null;
            if (item is BlitzBox b) go = b.mGameObject;
            else if (item is BlitzGate g) go = g.mGameObject;
            else if (item is BlitzDivider d) go = d.mGameObject;
            else if (item is BlitzOrb o) go = o.mGameObject;
            if (go != null && go.activeSelf)
            {
                mDismissTargets.Add(go.transform);
                mDismissStartScales.Add(go.transform.localScale);
            }
        }
    }

    void UpdateBlitzTransition()
    {
        mBlitzTransitionTimer += Time.deltaTime;

        if (mBlitzTransition == BlitzTransition.Dismissing)
        {
            float t = Mathf.Clamp01(mBlitzTransitionTimer / BLITZ_DISMISS_DURATION);
            float s = 1f - (t * t); // ease-in quadratic → snap shut at the end

            for (int i = 0; i < mDismissTargets.Count; i++)
            {
                if (mDismissTargets[i] != null)
                    mDismissTargets[i].localScale = mDismissStartScales[i] * s;
            }

            // Fade the full-tube ceiling with everything else so it doesn't freeze in
            // place while the world scales out on death.
            if (mFullTubeOverlayRenderer != null && mFullTubeOverlayRenderer.enabled)
            {
                if (mFullTubeBlock == null) mFullTubeBlock = new MaterialPropertyBlock();
                mFullTubeBlock.Clear();
                mFullTubeBlock.SetFloat(RevealProgressID, s);
                mFullTubeOverlayRenderer.SetPropertyBlock(mFullTubeBlock);
            }

            if (mBallTransform != null)
            {
                float bt = t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
                mBallTransform.position = Vector3.Lerp(mBlitzTransitionBallStart, mBlitzTransitionBallEnd, bt);
            }

            if (t >= 1f) EnterBlitzSpawnPhase();
        }
        else // Spawning — fresh obstacles already in; hold for audio/visual breathing room
        {
            if (mBlitzTransitionTimer >= BLITZ_SPAWN_DURATION)
            {
                mBlitzTransition = BlitzTransition.None;
                mPaused = false;
                if (mBlitzBeam != null) mBlitzBeam.SetActive(true);
            }
        }
    }

    void EnterBlitzSpawnPhase()
    {
        if (mBlitzBoxes != null)
        {
            foreach (BlitzBox box in mBlitzBoxes) Destroy(box.mGameObject);
            mBlitzBoxes.Clear();
        }
        if (mBlitzGates != null)
        {
            foreach (BlitzGate gate in mBlitzGates) Destroy(gate.mGameObject);
            mBlitzGates.Clear();
        }
        if (mBlitzDividers != null)
        {
            foreach (BlitzDivider div in mBlitzDividers) Destroy(div.mGameObject);
            mBlitzDividers.Clear();
        }
        if (mBlitzOrbs != null)
        {
            foreach (BlitzOrb orb in mBlitzOrbs) Destroy(orb.mGameObject);
            mBlitzOrbs.Clear();
        }
        ClearFullTubeSection();

        // Run-local state resets. mAngle, transform.rotation, mRounds, mAngleScore are
        // preserved — the world continues from wherever the ball died.
        mBlitzTimer = 0f;
        mAngleStep = BLITZ_BASE_SPEED;
        mGunOrbCount = 0;
        mCadencyOrbCount = 0;
        mShieldOrbCount = 0;
        mGunLevel = 0;
        mCadencyLevel = 0;
        mShieldLevel = 0;
        mShieldActive = false;
        if (mBlitzBeam != null) mBlitzBeam.SetGunLevel(0);

        mScore = 0;
        mScoreLbl.text = mScore.ToString();

        mLastBlitzAngle = mAngle + 10f;
        mLastSpawnKind = BlitzSpawnKind.None;
        mPrevSpawnKind = BlitzSpawnKind.None;
        mLastSpawnCommitsSide = false;
        ResetStreak();

        UpdateBlitzObstacles();

        // Debug stress-test: re-apply max power on every retry so the player keeps
        // starting at level 2 + max cadency, not at zero.
        if (GameConfig.BlitzDebugMaxPower)
            DebugApplyMaxBlitzPower();

        mBlitzTransition = BlitzTransition.Spawning;
        mBlitzTransitionTimer = 0f;

        if (mAudio != null) mAudio.PlayBlitzWipeIn();
    }

    public void UserInteraction()
    {
        mScoreWithoutInteraction = 0;
    }

    public bool IsGameOver() { return mGameOver; }

    private bool mPaused = false;
    public void SetPaused(bool paused) { mPaused = paused; }
    public bool IsPaused() { return mPaused; }

    public int GetScore() { return mScore; }
    public float GetAngle() { return mAngle; }
    public float GetBlitzTime() { return mBlitzTimer; }

    public void SetAngle(float angle)
    {
        mAngle = angle;
        transform.rotation = mBeginRotation * Quaternion.Euler(0, 0, -angle);
    }

    public List<Obstacle> GetObstacleList() { return mObstacles; }

    // Reset state without touching obstacles (RewindSystem handles obstacle swap)
    public void SoftReset()
    {
        mAngle = 0.0f;
        mAngleScore = 0.0f;
        mRounds = 0;
        transform.rotation = mBeginRotation;
        mScore = 0;
        mScoreLbl.text = mScore.ToString();
        mGameOver = false;
        mPaused = true; // Stay paused until user taps
        mScoreWithoutInteraction = 0;
        ResetSwing();
    }

    // Destroy current obstacles and generate fresh ones
    public void ReplaceObstacles()
    {
        foreach (Obstacle obstacle in mObstacles)
            Destroy(obstacle.mGameObject);
        mObstacles.Clear();
        mLastObstacle = null;
        mCurrentObstacle = null;
        UpdateObstacles();
    }

    // ── SWING HELPERS ────────────────────────────────────────

    // Ball's angular position on the torus cross-section (0-180°)
    // Matches Obstacle's gapOrigin coordinate space
    float GetBallCrossSectionAngle()
    {
        Vector3 local = transform.InverseTransformPoint(mBallTransform.position);
        // Torus center at (0, -10, 0) in local space, radius 1
        float dy = -(local.y + 10f);
        float dz = -local.z;
        return Mathf.Atan2(dy, dz) * Mathf.Rad2Deg;
    }

    void ResetSwing()
    {
        mLastPassAngle = float.MinValue;
        mSwingAccumulated = 0f;
        mSwingGates = 0;
        mPendingSwingTier = 0;
        mNextMilestone = MILESTONE_INTERVAL;
    }

    /// <summary>
    /// Consume pending swing event. Returns 0 (none), 1 (tier1), or 2 (tier2).
    /// Resets after reading so each event fires once.
    /// </summary>
    public int ConsumeSwingEvent()
    {
        int tier = mPendingSwingTier;
        mPendingSwingTier = 0;
        return tier;
    }

    public bool ConsumeGatePass()
    {
        bool passed = mPendingGatePass;
        mPendingGatePass = false;
        return passed;
    }

    // ── BLITZ MODE ──────────────────────────────────────────

    float GetBlitzIntensity() { return mBlitzTimer / BLITZ_RAMP_SECONDS; }

    void UpdateBlitzSpeed()
    {
        float intensity = GetBlitzIntensity();
        float baseRamp = Mathf.Lerp(BLITZ_BASE_SPEED, BLITZ_PEAK_SPEED, Mathf.Clamp01(intensity));
        // Past peak: keep pushing so spacing × speed eventually outruns reaction — game self-terminates.
        float endgameBoost = intensity > 1f ? (intensity - 1f) * 0.10f : 0f;
        mAngleStep = baseRamp + endgameBoost;
    }

    void UpdateBlitzObstacles()
    {
        if (mBlitzBoxes == null) return;

        // Generate ahead
        while (mLastBlitzAngle < mAngle + 85f)
            SpawnNextBlitzObstacle();

        // Hide behind (rolling window)
        foreach (BlitzBox box in mBlitzBoxes)
        {
            if (box.mAngle > mAngle - 240f) break;
            if (box.mGameObject != null && box.mGameObject.activeSelf)
                box.mGameObject.SetActive(false);
        }
        foreach (BlitzGate gate in mBlitzGates)
        {
            // Gate-pass sound — fires once when ball crosses the gate line. We only
            // reach this branch if the ball survived (collisions halt the ball before
            // UpdateBlitzObstacles runs), so any pass here = successful pass.
            if (!gate.mPassedSoundFired && gate.mAngle <= mAngle)
            {
                gate.mPassedSoundFired = true;
                if (mAudio != null)
                {
                    if (gate.IsFullGate) mAudio.PlayBlitzGatePassFull();
                    else mAudio.PlayBlitzGatePassHalf();
                }
            }

            if (gate.mAngle > mAngle - 240f) break;
            if (gate.mGameObject != null && gate.mGameObject.activeSelf)
                gate.mGameObject.SetActive(false);
        }
        foreach (BlitzDivider div in mBlitzDividers)
        {
            if (div.mAngle > mAngle - 240f) break;
            if (div.mGameObject != null && div.mGameObject.activeSelf)
                div.mGameObject.SetActive(false);
        }
        foreach (BlitzOrb orb in mBlitzOrbs)
        {
            if (orb.mAngle > mAngle - 240f) break;
            if (orb.mGameObject != null && orb.mGameObject.activeSelf)
                orb.mGameObject.SetActive(false);
        }
    }

    void SpawnNextBlitzObstacle()
    {
        float intensity = GetBlitzIntensity();

        // Rare late-game spectacle — full-tube section. Purely visual; obstacles still
        // spawn normally inside the section. Gated by intensity + cooldown so the "wow"
        // moment stays rare.
        TryFullTubeSection(intensity);

        // Onboarding: first 4 seconds are gentle so the player sees the auto-beam work.
        if (mBlitzTimer < 4f)
        {
            SpawnFormation(PickFormation(0f), Random.Range(9f, 12f));
            TryOrb(intensity);
            return;
        }

        BlitzSpawnKind kind = PickKind(intensity);

        // ── Impossible-combo guard ──────────────────────────────────
        // After a divider the player is committed to left or right of the tube.
        // If we drop a must-clear gate (full or gap) right behind it, one side is
        // instant death with no counterplay. Bounce the choice to a formation —
        // which dodges around rather than filtering by side.
        if (mLastSpawnKind == BlitzSpawnKind.Divider &&
            (kind == BlitzSpawnKind.FullGate || kind == BlitzSpawnKind.GateGap ||
             kind == BlitzSpawnKind.GatePair || kind == BlitzSpawnKind.HalfGateMixed))
        {
            kind = BlitzSpawnKind.Formation;
        }

        float spacing = ComputeSpacing(intensity, kind);

        switch (kind)
        {
            case BlitzSpawnKind.GateGap:       SpawnGateWithGap(spacing); break;
            case BlitzSpawnKind.GateButton:    SpawnGateWithButton(spacing); break;
            case BlitzSpawnKind.FullGate:      SpawnFullGateWithButton(spacing); break;
            case BlitzSpawnKind.Divider:       SpawnDivider(spacing); break;
            case BlitzSpawnKind.GatePair:      SpawnGatePair(spacing); break;
            case BlitzSpawnKind.HalfGateMixed: SpawnHalfGateMixed(spacing); break;
            default:                           SpawnFormation(PickFormation(intensity), spacing); break;
        }

        TryOrb(intensity);
    }

    void TryOrb(float intensity)
    {
        // Orb rain is more generous early (upgrade acquisition) and tapers off late
        // (player is already maxed, fewer distractions from the wall of obstacles).
        float orbChance = Mathf.Lerp(0.35f, 0.15f, Mathf.Clamp01(intensity));
        if (Random.value < orbChance)
            SpawnBlitzOrb(mLastBlitzAngle);
    }

    BlitzSpawnKind PickKind(float intensity)
    {
        float c = Mathf.Clamp01(intensity);
        // Weights evolve continuously. Formations shrink as share (not absolute presence),
        // gates and dividers grow, full-gates unlock past ~30s of play.
        float[] w = new float[8];
        w[(int)BlitzSpawnKind.Formation]     = Mathf.Lerp(0.75f, 0.35f, c);
        w[(int)BlitzSpawnKind.GateGap]       = Mathf.Lerp(0.08f, 0.18f, c);
        w[(int)BlitzSpawnKind.GateButton]    = Mathf.Lerp(0.05f, 0.18f, c);
        w[(int)BlitzSpawnKind.FullGate]      = Mathf.Lerp(0.00f, 0.15f, c);
        w[(int)BlitzSpawnKind.Divider]       = Mathf.Lerp(0.12f, 0.14f, c);
        w[(int)BlitzSpawnKind.GatePair]      = Mathf.Lerp(0.00f, 0.10f, c);
        w[(int)BlitzSpawnKind.HalfGateMixed] = Mathf.Lerp(0.00f, 0.12f, c);

        // Late-game skill bias: after ~4 minutes (intensity 2.0) the player is typically
        // maxed on beams/cadency, so destructible formations stop being threatening.
        // Non-destructible compositions (gate pairs, mixed half-gates) keep demanding
        // reads and swings. late=0 at 2min, late=1 at 4min.
        float late = Mathf.Clamp01(intensity - 1f);
        w[(int)BlitzSpawnKind.GatePair]      += late * 0.12f;
        w[(int)BlitzSpawnKind.HalfGateMixed] += late * 0.16f;
        // Formations stay near full share — Blitz is still a points mode, and mixed-HP
        // formations (BAB, ABBA_row, B_AA_B_depth) are the main scoring opportunities
        // once the beam maxes out. Only a mild 15% trim so gates still dominate feel.
        w[(int)BlitzSpawnKind.Formation]     *= Mathf.Lerp(1f, 0.85f, late);

        // Variety enforcement — crushes the 10s-of-just-buttons and 10s-of-just-dividers
        // stretches the user called out. Formations are exempt: they have internal variety
        // (12 sub-formations rotate via PickFormation), so back-to-back formations feel fresh.
        if (mLastSpawnKind != BlitzSpawnKind.None && mLastSpawnKind != BlitzSpawnKind.Formation)
            w[(int)mLastSpawnKind] *= 0.15f;
        if (mPrevSpawnKind != BlitzSpawnKind.None && mPrevSpawnKind != BlitzSpawnKind.Formation)
            w[(int)mPrevSpawnKind] *= 0.50f;

        float total = 0f;
        for (int i = 1; i < w.Length; i++) total += w[i];
        if (total <= 0f) return BlitzSpawnKind.Formation;

        float r = Random.value * total;
        float acc = 0f;
        for (int i = 1; i < w.Length; i++)
        {
            acc += w[i];
            if (r <= acc) return (BlitzSpawnKind)i;
        }
        return BlitzSpawnKind.Formation;
    }

    BlitzFormation PickFormation(float intensity)
    {
        float c = Mathf.Clamp01(intensity);
        // Order matches BlitzFormation.All(): 0 A, 1 B, 2 AAA, 3 ABA, 4 AA_stream,
        // 5 Pyramid, 6 Diamond, 7 Expand, 8 Shrink, 9 Wall, 10 Zigzag, 11 Column_BB,
        // 12 BAB, 13 ABBA_row, 14 B_AA_B_depth.
        // Simple shapes dominate early; composed/multi-row shapes take over late so the
        // player actually meets the variety before the density wall ends the run.
        float[] w = new float[15];
        w[0]  = Mathf.Lerp(0.22f, 0.03f, c); // A
        w[1]  = Mathf.Lerp(0.05f, 0.08f, c); // B (3HP sentinel)
        w[2]  = Mathf.Lerp(0.20f, 0.08f, c); // AAA
        w[3]  = Mathf.Lerp(0.12f, 0.12f, c); // ABA            — mixed HP
        w[4]  = Mathf.Lerp(0.15f, 0.06f, c); // AA_stream
        w[5]  = Mathf.Lerp(0.10f, 0.08f, c); // Pyramid_A_AAA
        w[6]  = Mathf.Lerp(0.06f, 0.10f, c); // Diamond_A_AAA_A
        w[7]  = Mathf.Lerp(0.03f, 0.08f, c); // Expand_A_AAA_AAAAA
        w[8]  = Mathf.Lerp(0.03f, 0.08f, c); // Shrink_AAAAA_AAA_A
        w[9]  = Mathf.Lerp(0.02f, 0.05f, c); // Wall_AAAAA
        w[10] = Mathf.Lerp(0.01f, 0.03f, c); // Zigzag_A_A_A
        w[11] = Mathf.Lerp(0.01f, 0.02f, c); // Column_BB
        w[12] = Mathf.Lerp(0.00f, 0.10f, c); // BAB            — mixed HP
        w[13] = Mathf.Lerp(0.00f, 0.10f, c); // ABBA_row       — mixed HP
        w[14] = Mathf.Lerp(0.00f, 0.09f, c); // B_AA_B_depth   — mixed HP

        // Late-game mixed-HP boost — past 2 min, player has the beams to clear walls,
        // so shift toward formations that reward sustained fire for big points.
        // late=0 at 2min, late=1 at 4min.
        float late = Mathf.Clamp01(intensity - 1f);
        w[3]  += late * 0.05f; // ABA
        w[12] += late * 0.08f; // BAB
        w[13] += late * 0.08f; // ABBA_row
        w[14] += late * 0.08f; // B_AA_B_depth

        BlitzFormation[] all = BlitzFormation.All();
        float total = 0f;
        for (int i = 0; i < w.Length; i++) total += w[i];

        float r = Random.value * total;
        float acc = 0f;
        for (int i = 0; i < w.Length; i++)
        {
            acc += w[i];
            if (r <= acc) return all[i];
        }
        return all[0];
    }

    float ComputeSpacing(float intensity, BlitzSpawnKind kind)
    {
        float c = Mathf.Clamp01(intensity);
        // Base spacing collapses 12° → 3° over the ramp. Past 1.0 it keeps shrinking
        // toward a 1.5° floor — past that point the player can't react, by design.
        float sp = Mathf.Lerp(12f, 3f, c);
        if (intensity > 1f) sp = Mathf.Max(1.5f, sp - (intensity - 1f) * 1.5f);

        // Per-kind floors — below these, an obstacle becomes literally unreadable.
        if (kind == BlitzSpawnKind.Divider)       sp = Mathf.Max(sp, 4f); // time to pick a side
        if (kind == BlitzSpawnKind.FullGate)      sp = Mathf.Max(sp, 5f); // time to aim at button
        if (kind == BlitzSpawnKind.GatePair)      sp = Mathf.Max(sp, 5f); // two gaps to pre-read
        if (kind == BlitzSpawnKind.HalfGateMixed) sp = Mathf.Max(sp, 4f); // long sequence needs setup

        // Recovery spacing after side-committing spawns (half-gate, full-gate, divider,
        // gate pair, mixed half-gates). If the next spawn is also side-committing, the
        // player needs enough forward distance to swing across the cross-section before
        // the next commit forces a new line. Formations don't force a crossing, so they
        // keep the tight 3° floor.
        if (mLastSpawnCommitsSide)
        {
            bool nextCommits = kind != BlitzSpawnKind.Formation;
            sp = Mathf.Max(sp, nextCommits ? 15f : 3f);
        }

        return sp;
    }

    void MarkSpawn(BlitzSpawnKind kind, bool commitsSide)
    {
        mPrevSpawnKind = mLastSpawnKind;
        mLastSpawnKind = kind;
        mLastSpawnCommitsSide = commitsSide;
    }

    void SpawnFormation(BlitzFormation f, float leadSpacing)
    {
        mLastBlitzAngle += leadSpacing;
        float anchorAngle = mLastBlitzAngle;
        float crossAnchor = Random.Range(f.crossAnchorMin, f.crossAnchorMax);

        for (int i = 0; i < f.elements.Length; i++)
        {
            BlitzElement e = f.elements[i];
            float cross = Mathf.Clamp(crossAnchor + e.crossOffset, 30f, 150f);
            float angle = anchorAngle + e.angleOffset;

            BlitzBox box = new BlitzBox(cross, mBlitzBoxMat, e.hp);
            box.mAngle = angle;
            box.mGameObject.transform.parent = transform;
            box.mGameObject.transform.Rotate(0f, 0f, box.mAngle - mAngle);
            mBlitzBoxes.Add(box);
        }

        mLastBlitzAngle = anchorAngle + f.longitudinalDepth;
        // Formations have small cross footprint — player can dodge on either side.
        MarkSpawn(BlitzSpawnKind.Formation, commitsSide: false);
    }

    void SpawnGateWithGap(float spacing)
    {
        mLastBlitzAngle += spacing;

        // Half-gate: randomly block left or right side of the cross-section
        bool blockLeft = Random.value < 0.5f;
        float from = blockLeft ? 25f : 90f;
        float to = blockLeft ? 90f : 155f;

        // Gate-train: 1 gate early, up to 3 mid-game, up to 5 late. All on the
        // SAME side so the player's lane commitment lasts longer — extends the
        // avoidance volume from a single dodge into a sustained hold.
        float c = Mathf.Clamp01(GetBlitzIntensity());
        int maxCount;
        if (c >= 0.6f) maxCount = 5;
        else if (c >= 0.3f) maxCount = 3;
        else maxCount = 1;
        int count = Random.Range(1, maxCount + 1);

        for (int i = 0; i < count; i++)
        {
            if (i > 0) mLastBlitzAngle += 5f; // tight inter-gate spacing
            BlitzGate gate = new BlitzGate(from, to, true, mBlitzGateMat, mBlitzConnectionMat);
            gate.mAngle = mLastBlitzAngle;
            gate.mGameObject.transform.parent = transform;
            gate.mGameObject.transform.Rotate(0f, 0f, gate.mAngle - mAngle);
            mBlitzGates.Add(gate);
        }

        // Half-gate train forces player to opposite side — next spawn needs recovery room.
        MarkSpawn(BlitzSpawnKind.GateGap, commitsSide: true);
    }

    void SpawnGateWithButton(float spacing)
    {
        float gapCenter = Random.Range(50f, 130f);
        float gapSize = Random.Range(25f, 35f);
        float buttonCross = Random.Range(40f, 140f);

        // Button appears first
        mLastBlitzAngle += spacing;
        BlitzBox button = new BlitzBox(buttonCross, mBlitzButtonMat, 3, isButton: true);
        button.mAngle = mLastBlitzAngle;
        button.mGameObject.transform.parent = transform;
        button.mGameObject.transform.Rotate(0f, 0f, button.mAngle - mAngle);
        mBlitzBoxes.Add(button);

        // Gate follows at 25° behind button
        mLastBlitzAngle += 25f;
        BlitzGate gate = new BlitzGate(gapCenter, gapSize, mBlitzGateMat, mBlitzConnectionMat);
        gate.mAngle = mLastBlitzAngle;
        gate.mGameObject.transform.parent = transform;
        gate.mGameObject.transform.Rotate(0f, 0f, gate.mAngle - mAngle);
        gate.LinkButton(button);
        mBlitzGates.Add(gate);
        // Button aim commits player to the button's cross-section side.
        MarkSpawn(BlitzSpawnKind.GateButton, commitsSide: true);
    }

    void SpawnDivider(float spacing)
    {
        mLastBlitzAngle += spacing;
        float startAngle = mLastBlitzAngle;
        float c = Mathf.Clamp01(GetBlitzIntensity());

        // Late game: 50% chance of triple-lane variant. Two dividers slice the
        // playable arc [30°,150°] into three 40° lanes. Each lane is locked-in
        // once entered, and each gets a mandatory box — two 1HP + one 3HP in
        // random order. Player must read which lane is survivable and commit.
        if (c > 0.6f && Random.value < 0.5f)
        {
            SpawnTripleDivider(startAngle);
        }
        else
        {
            SpawnSingleDivider(startAngle, c);
        }

        // Divider commits player to a lane — planner uses this to forbid
        // full-gates / half-gates immediately after.
        MarkSpawn(BlitzSpawnKind.Divider, commitsSide: true);
    }

    void SpawnSingleDivider(float startAngle, float c)
    {
        // Sit at bottom cross-angle (90° = where ball rests untapped).
        // Shorter than the first-pass 25–45° — that span was long enough to trap the
        // player against a gate behind it. 15–22° lets the commit resolve in time.
        float span = Random.Range(15f, 22f);
        BlitzDivider div = new BlitzDivider(90f, span, mBlitzConnectionMat);
        div.mAngle = startAngle;
        div.mGameObject.transform.parent = transform;
        div.mGameObject.transform.Rotate(0f, 0f, div.mAngle - mAngle);
        mBlitzDividers.Add(div);

        // Escort cubes inside the span. Cross 60° (left lane) and 120° (right lane)
        // sit in the ball's natural swing paths, so whichever side the player commits
        // to, the beam picks targets off on the way through. Turns the coast-through
        // dead air into a scoring corridor.
        //
        // Asymmetric: one random side gets a 3HP sentinel as its first escort. Both
        // sides remain passable, but the "hard side" demands sustained fire to clear
        // — player reads the shapes on approach and picks their route.
        int perSide = (c > 0.5f) ? 2 : 1;
        int total = perSide * 2;
        bool hardLeft = Random.value < 0.5f;
        float hardCross = hardLeft ? 60f : 120f;
        float easyCross = hardLeft ? 120f : 60f;
        for (int i = 0; i < total; i++)
        {
            float t = (i + 1) / (float)(total + 1);
            float a = startAngle + span * t;
            bool onHardSide = (i % 2 == 0);
            float cross = onHardSide ? hardCross : easyCross;
            int hp = (onHardSide && i == 0) ? 3 : 1;
            BlitzBox esc = new BlitzBox(cross, mBlitzBoxMat, hp);
            esc.mAngle = a;
            esc.mGameObject.transform.parent = transform;
            esc.mGameObject.transform.Rotate(0f, 0f, esc.mAngle - mAngle);
            mBlitzBoxes.Add(esc);
        }

        mLastBlitzAngle = startAngle + span;
    }

    void SpawnTripleDivider(float startAngle)
    {
        // Half-length span so the triple-lane commitment doesn't last too long.
        float span = Random.Range(7.5f, 11f);

        // Two dividers at cross 70° and 110° cut the [30°,150°] playable arc into
        // three 40° lanes. Lane centers: 50° (left), 90° (center), 130° (right).
        BlitzDivider d1 = new BlitzDivider(70f, span, mBlitzConnectionMat);
        d1.mAngle = startAngle;
        d1.mGameObject.transform.parent = transform;
        d1.mGameObject.transform.Rotate(0f, 0f, d1.mAngle - mAngle);
        mBlitzDividers.Add(d1);

        BlitzDivider d2 = new BlitzDivider(110f, span, mBlitzConnectionMat);
        d2.mAngle = startAngle;
        d2.mGameObject.transform.parent = transform;
        d2.mGameObject.transform.Rotate(0f, 0f, d2.mAngle - mAngle);
        mBlitzDividers.Add(d2);

        // Random shuffle of [1, 1, 3] across the three lane centers.
        int[] hps = { 1, 1, 3 };
        for (int i = hps.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = hps[i]; hps[i] = hps[j]; hps[j] = tmp;
        }
        float[] laneCrosses = { 50f, 90f, 130f };
        float midAngle = startAngle + span * 0.5f;
        for (int i = 0; i < 3; i++)
        {
            BlitzBox b = new BlitzBox(laneCrosses[i], mBlitzBoxMat, hps[i]);
            b.mAngle = midAngle;
            b.mGameObject.transform.parent = transform;
            b.mGameObject.transform.Rotate(0f, 0f, b.mAngle - mAngle);
            mBlitzBoxes.Add(b);
        }

        mLastBlitzAngle = startAngle + span;
    }

    void SpawnFullGateWithButton(float spacing)
    {
        float buttonCross = Random.Range(50f, 130f);

        // Button appears first
        mLastBlitzAngle += spacing;
        BlitzBox button = new BlitzBox(buttonCross, mBlitzButtonMat, 3, isButton: true);
        button.mAngle = mLastBlitzAngle;
        button.mGameObject.transform.parent = transform;
        button.mGameObject.transform.Rotate(0f, 0f, button.mAngle - mAngle);
        mBlitzBoxes.Add(button);

        // Full gate (no gap) — must destroy button or die. Red palette flags the threat.
        mLastBlitzAngle += 30f;
        BlitzGate gate = new BlitzGate(90f, 0f, mBlitzGateMat, mBlitzConnectionMat, isFullGate: true);
        gate.mAngle = mLastBlitzAngle;
        gate.mGameObject.transform.parent = transform;
        gate.mGameObject.transform.Rotate(0f, 0f, gate.mAngle - mAngle);
        gate.LinkButton(button);
        mBlitzGates.Add(gate);
        // Must-destroy button — player must commit to the button's side to get a clean shot.
        MarkSpawn(BlitzSpawnKind.FullGate, commitsSide: true);
    }

    void SpawnGatePair(float spacing)
    {
        mLastBlitzAngle += spacing;

        // Two ring gates — reuses Pure Hell's read-and-thread challenge.
        //   Tight  (4–6° forward): both gates share a single gap line, so one committed
        //                          line clears both. Tighter than this = not readable.
        //   Loose (18–26° forward): each gate has its own random gap — player has time
        //                           to swing between two different pass-through points.
        bool tight = Random.value < 0.5f;

        float sharedCenter = Random.Range(50f, 130f);
        float sharedSize = Random.Range(30f, 40f);

        for (int i = 0; i < 2; i++)
        {
            if (i > 0) mLastBlitzAngle += tight ? Random.Range(4f, 6f) : Random.Range(18f, 26f);

            float gapCenter = tight ? sharedCenter : Random.Range(50f, 130f);
            float gapSize   = tight ? sharedSize   : Random.Range(30f, 40f);
            BlitzGate gate = new BlitzGate(gapCenter, gapSize, mBlitzGateMat, mBlitzConnectionMat);
            gate.mAngle = mLastBlitzAngle;
            gate.mGameObject.transform.parent = transform;
            gate.mGameObject.transform.Rotate(0f, 0f, gate.mAngle - mAngle);
            mBlitzGates.Add(gate);
        }

        MarkSpawn(BlitzSpawnKind.GatePair, commitsSide: true);
    }

    void SpawnHalfGateMixed(float spacing)
    {
        mLastBlitzAngle += spacing;

        // X = left side blocked (player holds right), Y = right side blocked (player holds left).
        // Patterns: XXYXX / YYXYY are sandwich mirrors — two-lane hold, flip, hold.
        // XYXY / YXYX are slaloms — swing every gate.
        // Same-side transitions use tight inter-gate spacing (no swing needed).
        // Side-flip transitions need a swing-friendly gap (≥12° given current ball swing rate).
        int[] pattern;
        switch (Random.Range(0, 4))
        {
            case 0:  pattern = new[] { 0, 0, 1, 0, 0 }; break; // XXYXX
            case 1:  pattern = new[] { 1, 1, 0, 1, 1 }; break; // YYXYY
            case 2:  pattern = new[] { 0, 1, 0, 1 };    break; // XYXY
            default: pattern = new[] { 1, 0, 1, 0 };    break; // YXYX
        }

        for (int i = 0; i < pattern.Length; i++)
        {
            if (i > 0)
            {
                bool flipsSide = pattern[i] != pattern[i - 1];
                mLastBlitzAngle += flipsSide ? Random.Range(12f, 16f) : Random.Range(5f, 7f);
            }

            bool blockLeft = pattern[i] == 0;
            float from = blockLeft ? 25f : 90f;
            float to   = blockLeft ? 90f : 155f;
            BlitzGate gate = new BlitzGate(from, to, true, mBlitzGateMat, mBlitzConnectionMat);
            gate.mAngle = mLastBlitzAngle;
            gate.mGameObject.transform.parent = transform;
            gate.mGameObject.transform.Rotate(0f, 0f, gate.mAngle - mAngle);
            mBlitzGates.Add(gate);
        }

        MarkSpawn(BlitzSpawnKind.HalfGateMixed, commitsSide: true);
    }

    void AnimateBlitzGates()
    {
        float time = Time.time;
        foreach (BlitzGate gate in mBlitzGates)
        {
            if (gate.mGameObject.activeSelf)
                gate.Animate(time);
        }
    }

    void AnimateBlitzDividers()
    {
        float time = Time.time;
        foreach (BlitzDivider div in mBlitzDividers)
        {
            if (div.mGameObject.activeSelf)
                div.Animate(time);
        }
    }

    void AnimateBlitzOrbs()
    {
        float time = Time.time;
        foreach (BlitzOrb orb in mBlitzOrbs)
        {
            if (orb.mGameObject != null && orb.mGameObject.activeSelf)
                orb.Animate(time);
        }
    }

    void AnimateBlitzBoxes()
    {
        float time = Time.time;
        foreach (BlitzBox box in mBlitzBoxes)
        {
            if (!box.mGameObject.activeSelf) continue;
            box.UpdateDrift(mAngle);
            box.Animate(time);
        }
    }

    void UpdateBlitzFireRate()
    {
        if (mBlitzBeam == null) return;
        // Continuous curve mirrors the intensity ramp — no step changes.
        float interval = Mathf.Lerp(0.80f, 0.35f, Mathf.Clamp01(GetBlitzIntensity()));
        float multiplier = mCadencyLevel >= 2 ? 0.5f
                         : mCadencyLevel >= 1 ? 0.7f
                         : 1.0f;
        mBlitzBeam.SetFireInterval(interval * multiplier);
    }

    /// <summary>
    /// Called by BlitzBeam when a beam hits a box/button.
    /// Reduces HP; on destruction awards points and handles gate link.
    /// </summary>
    public void OnBlitzBoxHit(GameObject cubeObj)
    {
        foreach (BlitzBox box in mBlitzBoxes)
        {
            if (box.mCube == cubeObj && !box.mDestroyed)
            {
                bool destroyed = box.Hit();
                LightHaptic();

                if (destroyed)
                {
                    Sphere.IncrementBlitzObstacles();

                    // 1HP destroy: 10 (unchanged). 3HP destroy: 30 (was 20) — plus the two
                    // prior +5 chips each 3HP box earns on the way down = 40 total for a
                    // full clear. Rewards sustained fire on sentinels/buttons.
                    // Kill-streak multiplier scales destroy + gate bonus (not chip/orb).
                    int points = box.mMaxHitPoints >= 3 ? 30 : 10;
                    mScore += points * mStreakTier;
                    AddStreak(box.mMaxHitPoints >= 3 ? 2f : 1f);

                    // Gate deactivation bonus
                    if (box.mLinkedGate != null)
                    {
                        if (mAudio != null)
                        {
                            mAudio.PlayStrandPeel(2); // final peel (descends to v3)
                            mAudio.PlayButtonDestroyed();
                        }
                        box.mLinkedGate.Deactivate();
                        mScore += 50 * mStreakTier;
                        HeavyHaptic();
                    }
                    else if (box.mMaxHitPoints >= 3)
                    {
                        if (mAudio != null) mAudio.PlaySentinelKill();
                    }
                    else
                    {
                        if (mAudio != null) mAudio.PlayBoxHit();
                    }

                    mScoreLbl.text = mScore.ToString();

                    // Grid pulse at destruction point
                    if (mBallTransform != null)
                    {
                        Shader.SetGlobalFloat("_ScorePulseTime", Time.time);
                        Shader.SetGlobalVector("_ScorePulsePos", cubeObj.transform.position);
                    }
                }
                else if (box.mLinkedGate != null)
                {
                    // Button survived this hit — peel one strand off the linked gate's connection.
                    // peelIndex = hits taken so far − 1 (0 on first hit, 1 on second).
                    int peelIndex = box.mMaxHitPoints - box.mHitPoints - 1;
                    if (mAudio != null) mAudio.PlayStrandPeel(peelIndex);
                    box.mLinkedGate.RemoveStrand();
                    mScore += 5;
                    mScoreLbl.text = mScore.ToString();
                }
                else if (box.mMaxHitPoints >= 3)
                {
                    // Standalone sentinel took a non-lethal hit.
                    if (mAudio != null) mAudio.PlaySentinelHit();
                    mScore += 5;
                    mScoreLbl.text = mScore.ToString();
                }
                break;
            }
        }
    }

    // ── BLITZ ORBS ──────────────────────────────────────────

    void SpawnBlitzOrb(float baseAngle)
    {
        // Orbs spawn throughout the run — once fully upgraded they become pure +5 point
        // bonuses, so there's still a reason to chase them.
        bool gunMaxed = mGunOrbCount >= ORBS_PER_UPGRADE * 2;
        bool cadencyMaxed = mCadencyOrbCount >= ORBS_PER_UPGRADE * 2;
        bool shieldMaxed = mShieldOrbCount >= ORBS_PER_UPGRADE;

        BlitzOrb.OrbType type = PickOrbType(gunMaxed, cadencyMaxed, shieldMaxed);
        Material mat = type == BlitzOrb.OrbType.Gun ? mBlitzOrbGunMat
                     : type == BlitzOrb.OrbType.Cadency ? mBlitzOrbCadencyMat
                     : mBlitzOrbShieldMat;

        float cross = Random.Range(40f, 140f);
        BlitzOrb orb = new BlitzOrb(type, cross, mat);
        orb.mAngle = baseAngle + Random.Range(2f, 5f);
        orb.mGameObject.transform.parent = transform;
        orb.mGameObject.transform.Rotate(0f, 0f, orb.mAngle - mAngle);
        if (orb.mTrigger != null) orb.mTrigger.onCollected = HandleOrbCollected;
        mBlitzOrbs.Add(orb);
    }

    void HandleOrbCollected(BlitzOrb orb)
    {
        if (orb.mDismissed) return;
        Vector3 orbCenter = orb.GetWorldCenter();
        orb.StartCollectedFade();
        OnOrbCollected(orb.mType);
        mScore += 5;
        mScoreLbl.text = mScore.ToString();
        LightHaptic();
        if (mScoreSync != null)
        {
            int slotIndex = GetOrbSlotIndex(orb.mType);
            mScoreSync.TriggerOrbSpark(orbCenter, orb.mType, slotIndex);
        }
    }

    BlitzOrb.OrbType PickOrbType(bool gunMaxed, bool cadencyMaxed, bool shieldMaxed)
    {
        // When all tracks are maxed the orb is a pure point bonus — pick uniformly so
        // players still see color/type variety instead of an infinite shield stream.
        if (gunMaxed && cadencyMaxed && shieldMaxed)
        {
            int pick = Random.Range(0, 3);
            if (pick == 0) return BlitzOrb.OrbType.Gun;
            if (pick == 1) return BlitzOrb.OrbType.Cadency;
            return BlitzOrb.OrbType.Shield;
        }

        // Otherwise weight toward incomplete tracks.
        float gW = gunMaxed ? 0f : 1f;
        float cW = cadencyMaxed ? 0f : 1f;
        float sW = shieldMaxed ? 0f : 1f;
        float total = gW + cW + sW;

        float r = Random.value * total;
        if (r < gW) return BlitzOrb.OrbType.Gun;
        if (r < gW + cW) return BlitzOrb.OrbType.Cadency;
        return BlitzOrb.OrbType.Shield;
    }

    void CheckBlitzOrbPickups()
    {
        // Actual pickup runs through BlitzOrbTrigger.OnTriggerEnter → HandleOrbCollected.
        // This loop only handles the "ball passed the orb without grabbing it" case.
        if (mBlitzOrbs == null) return;

        foreach (BlitzOrb orb in mBlitzOrbs)
        {
            if (orb.mDismissed) continue;
            if (orb.mGameObject == null || !orb.mGameObject.activeSelf) continue;

            if (mAngle - orb.mAngle > 5f)
                orb.StartMissedFade();
        }
    }

    int GetOrbSlotIndex(BlitzOrb.OrbType type)
    {
        switch (type)
        {
            case BlitzOrb.OrbType.Gun: return mGunOrbCount - 1;
            case BlitzOrb.OrbType.Cadency: return mCadencyOrbCount - 1;
            case BlitzOrb.OrbType.Shield: return mShieldOrbCount - 1;
            default: return 0;
        }
    }

    void OnOrbCollected(BlitzOrb.OrbType type)
    {
        switch (type)
        {
            case BlitzOrb.OrbType.Gun:
                if (mGunOrbCount >= ORBS_PER_UPGRADE * 2)
                {
                    if (mAudio != null) mAudio.PlayOrbGun();
                    break;
                }
                mGunOrbCount++;
                if (mGunOrbCount == ORBS_PER_UPGRADE)
                {
                    ApplyGunUpgrade(1);
                    if (mAudio != null) mAudio.PlayVoiceCannonUp();
                }
                else if (mGunOrbCount == ORBS_PER_UPGRADE * 2)
                {
                    ApplyGunUpgrade(2);
                    if (mAudio != null) mAudio.PlayVoiceCannonFull();
                }
                else
                {
                    if (mAudio != null) mAudio.PlayOrbGun();
                }
                break;
            case BlitzOrb.OrbType.Cadency:
                if (mCadencyOrbCount >= ORBS_PER_UPGRADE * 2)
                {
                    if (mAudio != null) mAudio.PlayOrbCadency();
                    break;
                }
                mCadencyOrbCount++;
                if (mCadencyOrbCount == ORBS_PER_UPGRADE)
                {
                    ApplyCadencyUpgrade(1);
                    if (mAudio != null) mAudio.PlayVoiceCadenceUp();
                }
                else if (mCadencyOrbCount == ORBS_PER_UPGRADE * 2)
                {
                    ApplyCadencyUpgrade(2);
                    if (mAudio != null) mAudio.PlayVoiceCadenceFull();
                }
                else
                {
                    if (mAudio != null) mAudio.PlayOrbCadency();
                }
                break;
            case BlitzOrb.OrbType.Shield:
                if (mShieldOrbCount >= ORBS_PER_UPGRADE)
                {
                    if (mAudio != null) mAudio.PlayOrbShield();
                    break;
                }
                mShieldOrbCount++;
                if (mShieldOrbCount == ORBS_PER_UPGRADE)
                {
                    ApplyShieldUpgrade();
                    if (mAudio != null) mAudio.PlayVoiceShieldOn();
                }
                else
                {
                    if (mAudio != null) mAudio.PlayOrbShield();
                }
                break;
        }
    }

    void ApplyGunUpgrade(int level)
    {
        mGunLevel = level;
        if (mBlitzBeam != null) mBlitzBeam.SetGunLevel(level);
    }
    void ApplyCadencyUpgrade(int level) { mCadencyLevel = level; }
    void ApplyShieldUpgrade() { mShieldLevel = 1; mShieldActive = true; }

    public int GetGunOrbCount() { return mGunOrbCount; }
    public int GetCadencyOrbCount() { return mCadencyOrbCount; }
    public int GetShieldOrbCount() { return mShieldOrbCount; }
    public int GetGunLevel() { return mGunLevel; }
    public int GetCadencyLevel() { return mCadencyLevel; }
    public int GetShieldLevel() { return mShieldLevel; }
    public bool IsShieldActive() { return mShieldActive; }

    /// Debug-only: short-circuit the orb progression so the player starts a Blitz
    /// run at max cadence + max gun (three beams). Shield is intentionally skipped
    /// per the stress-test use case. Gated by GameConfig.BlitzDebugMaxPower.
    public void DebugApplyMaxBlitzPower()
    {
        if (!GameConfig.IsBlitz()) return;
        mGunOrbCount = ORBS_PER_UPGRADE * 2;
        mCadencyOrbCount = ORBS_PER_UPGRADE * 2;
        mGunLevel = 2;
        mCadencyLevel = 2;
        if (mBlitzBeam != null) mBlitzBeam.SetGunLevel(2);
    }

    /// Kick the post-Reset(true) spawn-in animation: every current obstacle
    /// renderer starts at _SpawnProgress=0 and materializes to 1 with stagger,
    /// matching the post-death/retry look for the title→gameplay transition.
    public void BeginInitialSpawnAnim()
    {
        if (mSpawnInBlock == null) mSpawnInBlock = new MaterialPropertyBlock();
        if (mSpawnInItems == null) mSpawnInItems = new List<SpawnInItem>();
        else mSpawnInItems.Clear();

        int staggerIdx = 0;
        if (GameConfig.IsBlitz())
        {
            foreach (BlitzBox box in mBlitzBoxes)       AddSpawnInTargetDeep(box.mGameObject, ref staggerIdx);
            foreach (BlitzGate gate in mBlitzGates)     AddSpawnInTargetDeep(gate.mGameObject, ref staggerIdx);
            foreach (BlitzDivider div in mBlitzDividers) AddSpawnInTargetDeep(div.mGameObject, ref staggerIdx);
            foreach (BlitzOrb orb in mBlitzOrbs)        AddSpawnInTargetDeep(orb.mGameObject, ref staggerIdx);
        }
        else
        {
            foreach (Obstacle obs in mObstacles)
                AddSpawnInTargetDeep(obs.mGameObject, ref staggerIdx);
        }

        if (mSpawnInItems.Count == 0) return;

        mSpawnInActive = true;
        mSpawnInTimer = 0f;
        if (mAudio == null) mAudio = FindAnyObjectByType<GameAudio>();
        if (mAudio != null) mAudio.PlayGateSpawn();
    }

    void AddSpawnInTarget(GameObject go, ref int staggerIdx)
    {
        if (go == null) return;
        MeshRenderer r = go.GetComponent<MeshRenderer>();
        if (r == null) return;
        mSpawnInBlock.SetFloat(SpawnProgressID, 0f);
        r.SetPropertyBlock(mSpawnInBlock);
        mSpawnInItems.Add(new SpawnInItem { renderer = r, triggerTime = staggerIdx * SPAWN_IN_STAGGER });
        staggerIdx++;
    }

    void AddSpawnInTargetDeep(GameObject go, ref int staggerIdx)
    {
        if (go == null) return;
        MeshRenderer[] renderers = go.GetComponentsInChildren<MeshRenderer>(true);
        if (renderers == null || renderers.Length == 0) return;
        float t = staggerIdx * SPAWN_IN_STAGGER;
        foreach (MeshRenderer r in renderers)
        {
            if (r == null) continue;
            mSpawnInBlock.SetFloat(SpawnProgressID, 0f);
            r.SetPropertyBlock(mSpawnInBlock);
            mSpawnInItems.Add(new SpawnInItem { renderer = r, triggerTime = t });
        }
        staggerIdx++;
    }

    void TickSpawnIn()
    {
        mSpawnInTimer += Time.deltaTime;
        bool allDone = true;
        for (int i = 0; i < mSpawnInItems.Count; i++)
        {
            SpawnInItem item = mSpawnInItems[i];
            if (item.renderer == null) continue;
            float elapsed = mSpawnInTimer - item.triggerTime;
            if (elapsed < 0f) { allDone = false; continue; }
            float p = Mathf.Clamp01(elapsed / SPAWN_IN_DURATION);
            float sp = 1f - (1f - p) * (1f - p) * (1f - p); // ease-out cubic
            mSpawnInBlock.SetFloat(SpawnProgressID, sp);
            item.renderer.SetPropertyBlock(mSpawnInBlock);
            if (p < 1f) allDone = false;
        }
        if (allDone)
        {
            for (int i = 0; i < mSpawnInItems.Count; i++)
            {
                if (mSpawnInItems[i].renderer == null) continue;
                mSpawnInBlock.SetFloat(SpawnProgressID, 1f);
                mSpawnInItems[i].renderer.SetPropertyBlock(mSpawnInBlock);
            }
            mSpawnInItems.Clear();
            mSpawnInActive = false;
        }
    }

    public bool ConsumeShield()
    {
        if (!mShieldActive) return false;
        mShieldActive = false;
        mShieldLevel = 0;
        mShieldOrbCount = 0;
        DropStreakTier();
        return true;
    }

    // ── KILL-STREAK MULTIPLIER ──────────────────────────────
    // Read by ScoreSync for the top-right HUD. Tier stays visible, bar shows the
    // next-tier fill fraction (or the drain level at tier 1).
    public int GetStreakTier() { return mStreakTier; }
    public float GetStreakBarProgress() { return Mathf.Clamp01(mStreakBar / STREAK_BAR_CAP); }
    public bool IsStreakAtMaxTier() { return mStreakTier >= STREAK_MAX_TIER; }

    void TickStreak()
    {
        if (mStreakGraceTimer > 0f)
        {
            mStreakGraceTimer -= Time.deltaTime;
            return;
        }
        if (mStreakBar <= 0f && mStreakTier == 1) return;

        mStreakBar -= STREAK_DRAIN_RATE * Time.deltaTime;
        if (mStreakBar <= 0f)
        {
            if (mStreakTier > 1)
            {
                mStreakTier--;
                // Refill to full on drain-drop so each tier gets its own 5s drain cycle
                // (5 units ÷ 1.0/s). Plus a fresh grace so the player gets a breather.
                mStreakBar = STREAK_BAR_CAP;
                mStreakGraceTimer = STREAK_GRACE;
            }
            else
            {
                mStreakBar = 0f;
            }
        }
    }

    void AddStreak(float amount)
    {
        mStreakGraceTimer = STREAK_GRACE;
        if (mStreakTier >= STREAK_MAX_TIER)
        {
            // Already capped — keep the bar full so the readout shows "held at max".
            mStreakBar = STREAK_BAR_CAP;
            return;
        }

        mStreakBar += amount;
        while (mStreakBar >= STREAK_BAR_CAP && mStreakTier < STREAK_MAX_TIER)
        {
            mStreakBar -= STREAK_BAR_CAP;
            mStreakTier++;
        }
        if (mStreakTier >= STREAK_MAX_TIER) mStreakBar = STREAK_BAR_CAP;
    }

    void DropStreakTier()
    {
        if (mStreakTier > 1) mStreakTier--;
        mStreakBar = 0f;
        mStreakGraceTimer = 0f;
    }

    void ResetStreak()
    {
        mStreakBar = 0f;
        mStreakTier = 1;
        mStreakGraceTimer = 0f;
    }

    // ── FULL-TUBE SPECTACLE ─────────────────────────────────
    // Blitz only. A planner-scheduled "section" [start, end] closes the tube into a
    // full ring for spectacle. No gameplay effect — obstacles still live in the
    // playable half, ball still rides gravity. Fades in/out as the ball approaches
    // each portal edge.

    void TickFullTubeSection()
    {
        if (mFullTubeCooldown > 0f) mFullTubeCooldown -= Time.deltaTime;
        if (mFullTubeBlock == null) mFullTubeBlock = new MaterialPropertyBlock();

        float reveal = 0f;
        if (mFullTubeActive)
        {
            float a = mAngle;
            if (a < mFullTubeSectionStart - FULL_TUBE_FADE_LEAD)
                reveal = 0f;
            else if (a < mFullTubeSectionStart)
                reveal = Mathf.InverseLerp(mFullTubeSectionStart - FULL_TUBE_FADE_LEAD, mFullTubeSectionStart, a);
            else if (a < mFullTubeSectionEnd)
                reveal = 1f;
            else if (a < mFullTubeSectionEnd + FULL_TUBE_FADE_LEAD)
                reveal = 1f - Mathf.InverseLerp(mFullTubeSectionEnd, mFullTubeSectionEnd + FULL_TUBE_FADE_LEAD, a);
            else
            {
                reveal = 0f;
                mFullTubeActive = false;
            }
        }

        if (mFullTubeOverlayRenderer == null) return;
        bool show = reveal > 0.001f;
        if (show != mFullTubeOverlayRenderer.enabled)
            mFullTubeOverlayRenderer.enabled = show;
        if (show)
        {
            mFullTubeBlock.Clear();
            mFullTubeBlock.SetFloat(RevealProgressID, reveal);
            mFullTubeOverlayRenderer.SetPropertyBlock(mFullTubeBlock);
        }
    }

    void TryFullTubeSection(float intensity)
    {
        if (mFullTubeActive || mFullTubeCooldown > 0f) return;

        // Guaranteed first showcase — fires once per run at ~40s so every player sees
        // the feature in the first minute, not buried behind late-game probability.
        bool forceFirst = !mFullTubeFirstFired && mBlitzTimer >= FULL_TUBE_FIRST_AT_SECONDS;

        if (!forceFirst)
        {
            if (intensity < 0.25f) return; // basic unlock after first ~30s of run
            // Chance ramps quickly: at 0.25 → 0%, at 0.5 (1min) → 7.5%, at 1.0 (2min) → 22%.
            float chance = Mathf.Clamp01((intensity - 0.25f) * 0.30f);
            if (Random.value > chance) return;
        }

        float sectionLength = Random.Range(60f, 100f);
        float start = mLastBlitzAngle + Random.Range(12f, 20f);
        float end = start + sectionLength;
        TriggerFullTubeSection(start, end);
        mFullTubeFirstFired = true;
        mFullTubeCooldown = forceFirst ? 25f : 30f;
    }

    public void TriggerFullTubeSection(float startAngle, float endAngle)
    {
        if (mFullTubeActive) return;
        mFullTubeSectionStart = startAngle;
        mFullTubeSectionEnd = endAngle;
        mFullTubeActive = true;
    }

    void ClearFullTubeSection()
    {
        mFullTubeActive = false;
        mFullTubeSectionStart = -9999f;
        mFullTubeSectionEnd = -9999f;
        mFullTubeCooldown = 0f;
        mFullTubeFirstFired = false;
        if (mFullTubeOverlayRenderer != null) mFullTubeOverlayRenderer.enabled = false;
    }
}
