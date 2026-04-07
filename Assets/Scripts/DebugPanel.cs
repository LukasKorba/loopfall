using UnityEngine;

/// <summary>
/// Runtime debug controls — toggle audio channels, skip splash, god mode, etc.
/// Created by SceneSetup. Tweak values in the Inspector while playing.
/// </summary>
public class DebugPanel : MonoBehaviour
{
    [Header("Audio")]
    public bool muteMusic = false;
    public bool muteSfx = false;
    public bool muteVoice = false;
    public bool muteRoll = false;
    public bool muteRewind = false;

    [Header("Gameplay")]
    public bool godMode = false;

    [Header("Info (read-only)")]
    [SerializeField] private int currentScore;
    [SerializeField] private float ballSpeed;
    [SerializeField] private string rewindState;
    [SerializeField] private bool gameCenterAuth;
    [SerializeField] private int totalTaps;
    [SerializeField] private int totalRuns;

    // Cached refs
    private GameAudio mAudio;
    private Torus mTorus;
    private Sphere mSphere;
    private RewindSystem mRewind;
    private Rigidbody mBallRb;

    void Start()
    {
        mAudio = FindAnyObjectByType<GameAudio>();
        mTorus = FindAnyObjectByType<Torus>();
        mSphere = FindAnyObjectByType<Sphere>();
        mRewind = FindAnyObjectByType<RewindSystem>();

        GameObject ball = GameObject.Find("Ball");
        if (ball != null)
            mBallRb = ball.GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (mAudio != null)
            mAudio.ApplyDebugMutes(muteMusic, muteSfx, muteVoice, muteRoll, muteRewind);

        // Read-only info
        if (mTorus != null)
            currentScore = mTorus.GetScore();

        if (mBallRb != null)
            ballSpeed = mBallRb.linearVelocity.magnitude;

        if (mRewind != null)
            rewindState = mRewind.GetStateName();

        if (GameCenterManager.Instance != null)
            gameCenterAuth = GameCenterManager.Instance.IsAuthenticated();

        totalTaps = Sphere.GetTotalTaps();
        totalRuns = Sphere.GetTotalRuns();

    }
}
