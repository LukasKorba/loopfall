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

    // Rewind
    public RewindSystem mRewindSystem;

    // Persistent stats
    private const string STAT_TAPS = "TotalTaps";
    private const string STAT_RUNS = "TotalRuns";
    private int mSessionTaps = 0;

    void Awake()
    {
        QualitySettings.vSyncCount = 0; // Disable VSync so targetFrameRate is respected
        Application.targetFrameRate = 60;
        Input.multiTouchEnabled = false;
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

        // Start paused — waiting for first tap
        mTorusScript.SetPaused(true);
    }

    bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    void Update()
    {
        // Block all input during splash screen
        ScoreSync splashCheck = FindAnyObjectByType<ScoreSync>();
        if (splashCheck != null && splashCheck.IsSplash()) return;

        // Title state — any tap starts the game
        if (mWaitingToStart)
        {
            bool tapped = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) ||
                          (!IsPointerOverUI() && Input.touchCount == 0 && Input.GetMouseButtonDown(0)) ||
                          (!IsPointerOverUI() && Input.touchCount == 1 && Input.touches[0].phase == TouchPhase.Began);

            if (tapped)
            {
                mWaitingToStart = false;
                mTorusScript.SetPaused(false);
                if (mRewindSystem != null)
                    mRewindSystem.StartRecording();
                IncrementRuns();

                // Apply first impulse based on tap side
                bool leftSide = false;
                if (Input.touchCount == 1)
                    leftSide = Input.touches[0].position.x < Screen.width * 0.5f;
                else if (Input.touchCount == 0 && Input.GetMouseButtonDown(0))
                    leftSide = Input.mousePosition.x < Screen.width * 0.5f;
                else if (Input.GetKeyDown(KeyCode.A))
                    leftSide = true;

                ApplyForceWithForwardVector(new Vector3(leftSide ? 1.0f : -1.0f, 0.0f, 0.0f));
                return;
            }
            return;
        }

        // DEBUG: W key simulates death
        if (mDebugGodMode && !mGameOver && Input.GetKeyDown(KeyCode.W))
        {
            mGameOver = true;
            mTorusScript.GameOver();
            mRigid.linearDamping = 8f;
            mRigid.angularDamping = 8f;
            CameraSwing swing = mCamera.GetComponent<CameraSwing>();
            if (swing != null) swing.mDiff = Vector3.zero;
            DeathEffect death = mCamera.GetComponent<DeathEffect>();
            if (death != null) death.TriggerDeath(transform.position);
            if (mRewindSystem != null) mRewindSystem.OnDeath();
            GameAudio debugAudio = FindAnyObjectByType<GameAudio>();
            if (debugAudio != null) debugAudio.PlayGameOver();
        }

        // Game over state — any tap resets
        if (mGameOver)
        {
            // Wait for game over animation before allowing reset
            ScoreSync sync = FindAnyObjectByType<ScoreSync>();
            if (sync != null && !sync.CanRestart()) return;

            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) ||
                (!IsPointerOverUI() && Input.touchCount == 0 && Input.GetMouseButtonDown(0)) ||
                (!IsPointerOverUI() && Input.touchCount == 1 && Input.touches[0].phase == TouchPhase.Began))
            {
                DoReset();
                return;
            }
        }

        if (!mGameOver)
        {
            // Keyboard: A = left, D = right
            if (Input.GetKeyDown(KeyCode.A))
                ApplyForceWithForwardVector(new Vector3(1.0f, 0.0f, 0.0f));
            if (Input.GetKeyDown(KeyCode.D))
                ApplyForceWithForwardVector(new Vector3(-1.0f, 0.0f, 0.0f));

            // Mouse: left half = left, right half = right (desktop only)
            if (!IsPointerOverUI() && Input.touchCount == 0 && Input.GetMouseButtonDown(0))
            {
                if (Input.mousePosition.x < Screen.width * 0.5f)
                    ApplyForceWithForwardVector(new Vector3(1.0f, 0.0f, 0.0f));
                else
                    ApplyForceWithForwardVector(new Vector3(-1.0f, 0.0f, 0.0f));
            }

            // Touch: left half = left, right half = right
            if (!IsPointerOverUI() && Input.touchCount == 1 && Input.touches[0].phase == TouchPhase.Began)
            {
                if (Input.touches[0].position.x < Screen.width * 0.5f)
                    ApplyForceWithForwardVector(new Vector3(1.0f, 0.0f, 0.0f));
                else
                    ApplyForceWithForwardVector(new Vector3(-1.0f, 0.0f, 0.0f));
            }
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

        GameAudio audio = FindAnyObjectByType<GameAudio>();
        if (audio != null) audio.PlayTap();

        // Track taps — save every 10 to avoid I/O spam
        mSessionTaps++;
        if (mSessionTaps % 10 == 0)
            SaveTaps();
    }

    public void DoReset()
    {
        IncrementRuns();
        bool rewindHandled = mRewindSystem != null && mRewindSystem.IsComplete();

        if (mRewindSystem != null)
        {
            mRewindSystem.ResetSystem();
            mRewindSystem.StartRecording();
        }

        if (!rewindHandled)
        {
            // Fallback if no rewind — full torus reset
            mTorusScript.Reset();
        }

        mTorusScript.SetPaused(false);
        mGameOver = false;
        mRigid.isKinematic = false;
        mRigid.linearDamping = mOriginalDrag;
        mRigid.angularDamping = mOriginalAngularDrag;
        mRigid.linearVelocity = Vector3.zero;
        mRigid.angularVelocity = Vector3.zero;
        transform.position = mBeginPosition;
        mPrevPosition = mBeginPosition;

        mCamera.transform.position = mCameraStartPos;
        mCamera.transform.rotation = mCameraStartRot;

        DeathEffect death = mCamera.GetComponent<DeathEffect>();
        if (death != null)
            death.ResetShake();

        CameraSwing swing = mCamera.GetComponent<CameraSwing>();
        if (swing != null)
        {
            swing.mDiff = Vector3.zero;
            swing.ResetSpring();
        }

    }

    public bool IsWaiting() { return mWaitingToStart; }
    public bool IsGameOver() { return mGameOver; }
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

        if (!mGameOver && !mDebugGodMode && (collision.gameObject.name == "torusObstacle" || collision.gameObject.name == "Mesh"))
        {
            mGameOver = true;
            mTorusScript.GameOver();

            mRigid.linearDamping = 8f;
            mRigid.angularDamping = 8f;

            CameraSwing swing = mCamera.GetComponent<CameraSwing>();
            if (swing != null)
                swing.mDiff = Vector3.zero;

            DeathEffect death = mCamera.GetComponent<DeathEffect>();
            if (death != null)
                death.TriggerDeath(transform.position);

            if (mRewindSystem != null)
                mRewindSystem.OnDeath();

            GameAudio audio = FindAnyObjectByType<GameAudio>();
            if (audio != null)
                audio.PlayGameOver();
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
        int runs = PlayerPrefs.GetInt(STAT_RUNS, 0) + 1;
        PlayerPrefs.SetInt(STAT_RUNS, runs);
        PlayerPrefs.Save();
    }

    void SaveTaps()
    {
        int total = PlayerPrefs.GetInt(STAT_TAPS, 0) + mSessionTaps;
        PlayerPrefs.SetInt(STAT_TAPS, total);
        PlayerPrefs.Save();
        mSessionTaps = 0;
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
}
