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
    public Material mBlitzRingMat;
    public BlitzBeam mBlitzBeam;
    private List<BlitzBox> mBlitzBoxes;
    private List<BlitzGate> mBlitzGates;
    private float mBlitzTimer;
    private float mLastBlitzAngle;

    // Blitz speed ramp
    private const float BLITZ_BASE_SPEED = 0.12f;
    private const float BLITZ_MAX_SPEED = 0.22f;
    private const float BLITZ_SPEED_RAMP = 0.001f; // per second

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
    private const int ORBS_PER_UPGRADE = 5;

    void Awake()
    {
        mBeginRotation = transform.rotation;
    }

    void Start()
    {
        mObstacleStep *= Mathf.Deg2Rad;
        mObstacleStepInv = 1.0f / mObstacleStep;

        mObstacles = new List<Obstacle>();

        if (GameConfig.IsBlitz())
        {
            mAngleStep = BLITZ_BASE_SPEED;
            mScoreSync = FindAnyObjectByType<ScoreSync>();
            mBlitzBoxes = new List<BlitzBox>();
            mBlitzGates = new List<BlitzGate>();
            mBlitzOrbs = new List<BlitzOrb>();
            mLastBlitzAngle = mObstaclesAngleOrigin - 10f;
            UpdateBlitzObstacles();
        }
        else
        {
            UpdateObstacles();
        }
    }

    void Update()
    {
        if (mGameOver || mPaused)
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
            AnimateBlitzBoxes();
            UpdateBlitzFireRate();
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

    public void Reset()
    {
        mAngle = 0.0f;
        mAngleScore = 0.0f;
        mRounds = 0;
        transform.rotation = mBeginRotation;

        mScore = 0;
        mScoreLbl.text = mScore.ToString();

        mGameOver = false;

        foreach (Obstacle obstacle in mObstacles)
            Destroy(obstacle.mGameObject);

        mObstacles.Clear();

        mLastObstacle = null;
        mCurrentObstacle = null;

        if (GameConfig.IsBlitz())
        {
            mAngleStep = BLITZ_BASE_SPEED;
            mBlitzTimer = 0f;
            if (mBlitzBoxes != null)
            {
                foreach (BlitzBox box in mBlitzBoxes)
                    Destroy(box.mGameObject);
                mBlitzBoxes.Clear();
            }
            if (mBlitzGates != null)
            {
                foreach (BlitzGate gate in mBlitzGates)
                    Destroy(gate.mGameObject);
                mBlitzGates.Clear();
            }
            if (mBlitzOrbs != null)
            {
                foreach (BlitzOrb orb in mBlitzOrbs)
                    Destroy(orb.mGameObject);
                mBlitzOrbs.Clear();
            }
            mGunOrbCount = 0;
            mCadencyOrbCount = 0;
            mShieldOrbCount = 0;
            mGunLevel = 0;
            mCadencyLevel = 0;
            mShieldLevel = 0;
            mShieldActive = false;
            if (mBlitzBeam != null) mBlitzBeam.SetBeamCount(1);
            mLastBlitzAngle = mObstaclesAngleOrigin - 10f;
            UpdateBlitzObstacles();
        }
        else
        {
            UpdateObstacles();
        }

        mScoreWithoutInteraction = 0;
        ResetSwing();
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

    void UpdateBlitzSpeed()
    {
        mAngleStep = Mathf.Min(BLITZ_BASE_SPEED + mBlitzTimer * BLITZ_SPEED_RAMP, BLITZ_MAX_SPEED);
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
            if (gate.mAngle > mAngle - 240f) break;
            if (gate.mGameObject != null && gate.mGameObject.activeSelf)
                gate.mGameObject.SetActive(false);
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
        float r = Random.value;

        if (mBlitzTimer > 90f)
        {
            // Phase 4: dense, full gates required
            float sp = Random.Range(5f, 8f);
            if (r < 0.25f) SpawnFullGateWithButton(sp);
            else if (r < 0.50f) SpawnGateWithGap(sp);
            else if (r < 0.70f) SpawnGateWithButton(sp);
            else SpawnBlitzBox(sp, Random.value < 0.5f ? 3 : 1);
        }
        else if (mBlitzTimer > 45f)
        {
            // Phase 3: gates appear, some with buttons
            float sp = Random.Range(6f, 10f);
            if (r < 0.15f) SpawnFullGateWithButton(sp);
            else if (r < 0.35f) SpawnGateWithGap(sp);
            else if (r < 0.50f) SpawnGateWithButton(sp);
            else SpawnBlitzBox(sp, Random.value < 0.35f ? 3 : 1);
        }
        else if (mBlitzTimer > 15f)
        {
            // Phase 2: first gates, more variety
            float sp = Random.Range(7f, 12f);
            if (r < 0.20f) SpawnGateWithGap(sp);
            else SpawnBlitzBox(sp, Random.value < 0.2f ? 3 : 1);
        }
        else
        {
            // Phase 1: easy intro, boxes only
            SpawnBlitzBox(Random.Range(10f, 14f), 1);
        }

        // Chance to spawn an upgrade orb after each obstacle
        if (Random.value < 0.3f)
            SpawnBlitzOrb(mLastBlitzAngle);
    }

    void SpawnBlitzBox(float spacing, int hp)
    {
        float cross = Random.Range(30f, 150f);
        BlitzBox box = new BlitzBox(cross, mBlitzBoxMat, hp, ringMat: hp >= 3 ? mBlitzRingMat : null);
        mLastBlitzAngle += spacing;
        box.mAngle = mLastBlitzAngle;
        box.mGameObject.transform.parent = transform;
        box.mGameObject.transform.Rotate(0f, 0f, box.mAngle - mAngle);
        mBlitzBoxes.Add(box);
    }

    void SpawnGateWithGap(float spacing)
    {
        mLastBlitzAngle += spacing;

        // Half-gate: randomly block left or right side of the cross-section
        bool blockLeft = Random.value < 0.5f;
        float from = blockLeft ? 25f : 90f;
        float to = blockLeft ? 90f : 155f;

        BlitzGate gate = new BlitzGate(from, to, true, mBlitzGateMat, mBlitzConnectionMat);
        gate.mAngle = mLastBlitzAngle;
        gate.mGameObject.transform.parent = transform;
        gate.mGameObject.transform.Rotate(0f, 0f, gate.mAngle - mAngle);
        mBlitzGates.Add(gate);
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

        // Full gate (no gap) — must destroy button or die
        mLastBlitzAngle += 30f;
        BlitzGate gate = new BlitzGate(90f, 0f, mBlitzGateMat, mBlitzConnectionMat);
        gate.mAngle = mLastBlitzAngle;
        gate.mGameObject.transform.parent = transform;
        gate.mGameObject.transform.Rotate(0f, 0f, gate.mAngle - mAngle);
        gate.LinkButton(button);
        mBlitzGates.Add(gate);
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
            if (box.mGameObject.activeSelf)
                box.Animate(time);
        }
    }

    void UpdateBlitzFireRate()
    {
        if (mBlitzBeam == null) return;
        float interval = mBlitzTimer > 90f ? 0.4f
                       : mBlitzTimer > 45f ? 0.5f
                       : mBlitzTimer > 15f ? 0.65f
                       : 0.8f;

        // Cadency upgrade reduces fire interval
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
                    // Points: 1HP = 10, 3HP = 20
                    int points = box.mMaxHitPoints >= 3 ? 20 : 10;
                    mScore += points;

                    // Gate deactivation bonus
                    if (box.mLinkedGate != null)
                    {
                        box.mLinkedGate.Deactivate();
                        mScore += 50;
                        HeavyHaptic();
                    }

                    mScoreLbl.text = mScore.ToString();

                    // Grid pulse at destruction point
                    if (mBallTransform != null)
                    {
                        Shader.SetGlobalFloat("_ScorePulseTime", Time.time);
                        Shader.SetGlobalVector("_ScorePulsePos", cubeObj.transform.position);
                    }
                }
                break;
            }
        }
    }

    // ── BLITZ ORBS ──────────────────────────────────────────

    void SpawnBlitzOrb(float baseAngle)
    {
        // Don't spawn if all tracks maxed
        bool gunMaxed = mGunOrbCount >= ORBS_PER_UPGRADE * 2;
        bool cadencyMaxed = mCadencyOrbCount >= ORBS_PER_UPGRADE * 2;
        bool shieldMaxed = mShieldOrbCount >= ORBS_PER_UPGRADE;
        if (gunMaxed && cadencyMaxed && shieldMaxed) return;

        BlitzOrb.OrbType type = PickOrbType(gunMaxed, cadencyMaxed, shieldMaxed);
        Material mat = type == BlitzOrb.OrbType.Gun ? mBlitzOrbGunMat
                     : type == BlitzOrb.OrbType.Cadency ? mBlitzOrbCadencyMat
                     : mBlitzOrbShieldMat;

        float cross = Random.Range(40f, 140f);
        BlitzOrb orb = new BlitzOrb(type, cross, mat);
        orb.mAngle = baseAngle + Random.Range(2f, 5f);
        orb.mGameObject.transform.parent = transform;
        orb.mGameObject.transform.Rotate(0f, 0f, orb.mAngle - mAngle);
        mBlitzOrbs.Add(orb);
    }

    BlitzOrb.OrbType PickOrbType(bool gunMaxed, bool cadencyMaxed, bool shieldMaxed)
    {
        // Weight toward incomplete tracks
        float gW = gunMaxed ? 0f : 1f;
        float cW = cadencyMaxed ? 0f : 1f;
        float sW = shieldMaxed ? 0f : 1f;
        float total = gW + cW + sW;

        float r = Random.value * total;
        if (r < gW) return BlitzOrb.OrbType.Gun;
        if (r < gW + cW) return BlitzOrb.OrbType.Cadency;
        return BlitzOrb.OrbType.Shield;
    }

    const float ORB_COLLECT_RADIUS = 0.7f;

    void CheckBlitzOrbPickups()
    {
        if (mBlitzOrbs == null || mBallTransform == null) return;

        Vector3 ballPos = mBallTransform.position;

        foreach (BlitzOrb orb in mBlitzOrbs)
        {
            if (orb.mDismissed) continue;
            if (orb.mGameObject == null || !orb.mGameObject.activeSelf) continue;

            float ringDist = mAngle - orb.mAngle;

            // Collection window: check proximity while orb is near the ball
            if (ringDist >= -2f && ringDist <= 5f)
            {
                Vector3 orbCenter = orb.GetWorldCenter();
                float dist = Vector3.Distance(ballPos, orbCenter);
                if (dist < ORB_COLLECT_RADIUS)
                {
                    // Collected — flash + spark to HUD
                    orb.StartCollectedFade();
                    OnOrbCollected(orb.mType);
                    LightHaptic();

                    if (mScoreSync != null)
                    {
                        int slotIndex = GetOrbSlotIndex(orb.mType);
                        mScoreSync.TriggerOrbSpark(orbCenter, orb.mType, slotIndex);
                    }
                }
            }

            // Past collection window — missed
            if (ringDist > 5f)
            {
                orb.StartMissedFade();
            }
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
                mGunOrbCount++;
                if (mGunOrbCount == ORBS_PER_UPGRADE) ApplyGunUpgrade(1);
                else if (mGunOrbCount == ORBS_PER_UPGRADE * 2) ApplyGunUpgrade(2);
                break;
            case BlitzOrb.OrbType.Cadency:
                mCadencyOrbCount++;
                if (mCadencyOrbCount == ORBS_PER_UPGRADE) ApplyCadencyUpgrade(1);
                else if (mCadencyOrbCount == ORBS_PER_UPGRADE * 2) ApplyCadencyUpgrade(2);
                break;
            case BlitzOrb.OrbType.Shield:
                mShieldOrbCount++;
                if (mShieldOrbCount == ORBS_PER_UPGRADE) ApplyShieldUpgrade();
                break;
        }
    }

    void ApplyGunUpgrade(int level)
    {
        mGunLevel = level;
        if (mBlitzBeam != null) mBlitzBeam.SetBeamCount(level + 1);
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

    public bool ConsumeShield()
    {
        if (!mShieldActive) return false;
        mShieldActive = false;
        mShieldLevel = 0;
        mShieldOrbCount = 0;
        return true;
    }
}
