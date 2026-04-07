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

    // Pending event for audio system to consume
    private int mPendingSwingTier = 0; // 0=none, 1=swing, 2=milestone

    private List<Obstacle> mObstacles;

    public Material mObstacleTop;
    public Material mObstacleFront;
    public Material mObstacleShadow;

    void Awake()
    {
        mBeginRotation = transform.rotation;
    }

    void Start()
    {
        mObstacleStep *= Mathf.Deg2Rad;
        mObstacleStepInv = 1.0f / mObstacleStep;

        mObstacles = new List<Obstacle>();
        UpdateObstacles();
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

        UpdateObstacles();

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
}
