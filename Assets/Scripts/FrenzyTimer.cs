using UnityEngine;

/// <summary>
/// Countdown timer for Time Warp mode.
/// Starts at START_TIME, counts down. Game over when expired.
/// Track items call AddTime/SubtractTime to modify.
/// </summary>
public class FrenzyTimer : MonoBehaviour
{
    private const float START_TIME = 10.0f;
    private const float WARNING_THRESHOLD = 3.0f;

    private float mTimeRemaining;
    private bool mRunning = false;
    private bool mExpired = false;

    // Pending popup text for UI to consume
    private string mPendingPopup = null;
    private float mPendingPopupSign = 0f; // +1 = bonus, -1 = penalty

    // Track speed acceleration
    private Torus mTorus;
    private float mBaseAngleStep;
    private float mElapsed = 0f;
    private const float SPEED_INCREASE_RATE = 0.002f; // Per 10 seconds
    private const float SPEED_INCREASE_INTERVAL = 10f;

    public void Initialize(Torus torus)
    {
        mTorus = torus;
        mTimeRemaining = START_TIME;
    }

    public void StartTimer()
    {
        mTimeRemaining = START_TIME;
        mRunning = true;
        mExpired = false;
        mElapsed = 0f;
        if (mTorus != null)
            mBaseAngleStep = 0.14f; // Slightly slower start than Pure Hell's 0.17
    }

    public void StopTimer()
    {
        mRunning = false;
    }

    void Update()
    {
        if (!mRunning || mExpired) return;

        mTimeRemaining -= Time.deltaTime;
        mElapsed += Time.deltaTime;

        if (mTimeRemaining <= 0f)
        {
            mTimeRemaining = 0f;
            mExpired = true;
            mRunning = false;
            return;
        }

        // Gradual speed increase
        if (mTorus != null)
        {
            float speedBoost = Mathf.Floor(mElapsed / SPEED_INCREASE_INTERVAL) * SPEED_INCREASE_RATE;
            mTorus.SetAngleStep(mBaseAngleStep + speedBoost);
        }
    }

    public void AddTime(float seconds)
    {
        if (!mRunning || mExpired) return;
        mTimeRemaining += seconds;
        mPendingPopup = $"+{seconds:F0}s";
        mPendingPopupSign = 1f;
    }

    public void SubtractTime(float seconds)
    {
        if (!mRunning || mExpired) return;
        mTimeRemaining -= seconds;
        mPendingPopup = $"-{seconds:F0}s";
        mPendingPopupSign = -1f;

        if (mTimeRemaining <= 0f)
        {
            mTimeRemaining = 0f;
            mExpired = true;
            mRunning = false;
        }
    }

    public float GetTimeRemaining() { return mTimeRemaining; }
    public float GetElapsedTime() { return mElapsed; }
    public bool IsExpired() { return mExpired; }
    public bool IsRunning() { return mRunning; }
    public bool IsWarning() { return mRunning && mTimeRemaining < WARNING_THRESHOLD; }

    /// <summary>
    /// Consume pending popup text. Returns null if none.
    /// Sign: +1 = green, -1 = red.
    /// </summary>
    public string ConsumePopup(out float sign)
    {
        string text = mPendingPopup;
        sign = mPendingPopupSign;
        mPendingPopup = null;
        return text;
    }
}
