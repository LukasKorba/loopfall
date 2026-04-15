using UnityEngine;

/// <summary>
/// Test controller for the spline track system. Generates a spline track,
/// creates the mesh with proper materials and physics, positions the ball,
/// and sets up camera following.
///
/// This runs alongside the existing torus system for testing. Activated by
/// calling Initialize() from SceneSetup or manually.
///
/// Architecture:
///   - SplineTrack handles spline math + mesh generation
///   - SplineCameraFollow handles camera positioning
///   - Sphere.cs handles input + ball physics (with spline tangent for force direction)
///   - This script orchestrates setup and per-frame updates (gate checks, path extension)
/// </summary>
public class SplineGameController : MonoBehaviour
{
    // ── REFERENCES ───────────────────────────────────────────

    SplineTrack splineTrack;
    SplineCameraFollow cameraFollow;
    Transform ballTransform;
    Rigidbody ballRb;

    // Materials — assigned from SceneSetup
    Material trackFrontMaterial;
    Material trackInnerMaterial;

    // ── TRACK STREAMING ──────────────────────────────────────

    // How far ahead of the ball to keep the track generated
    const float GENERATE_AHEAD = 120f;
    // How far behind the ball to keep before cleanup
    const float CLEANUP_BEHIND = 30f;
    // Current furthest generated distance
    float generatedUpTo = 0f;

    // Track mesh segments — each covers a stretch of the spline
    const float MESH_SEGMENT_LENGTH = 30f;

    struct MeshSegment
    {
        public GameObject gameObject;
        public float startDist;
        public float endDist;
    }

    System.Collections.Generic.List<MeshSegment> meshSegments =
        new System.Collections.Generic.List<MeshSegment>();

    // ── FORWARD MOTION ───────────────────────────────────────

    // The spline slopes downhill so gravity provides forward motion.
    // These parameters control the feel:

    // Ball drag — limits terminal velocity on the slope
    // With slope -0.15 and drag 0.3, terminal velocity ≈ 3 units/sec
    const float BALL_DRAG = 0.3f;

    // Forward force boost — small constant push to maintain minimum speed
    // on flat or slightly uphill sections
    const float FORWARD_FORCE = 1.2f;

    // Maximum forward speed (units/sec)
    const float MAX_FORWARD_SPEED = 8f;

    // ── GAME STATE ───────────────────────────────────────────

    bool initialized = false;
    int score = 0;

    // ── INITIALIZATION ───────────────────────────────────────

    /// <summary>
    /// Set up the spline track system. Call after materials are created.
    /// </summary>
    public void Initialize(Material frontMat, Material innerMat, Transform ball, Camera cam)
    {
        trackFrontMaterial = frontMat;
        trackInnerMaterial = innerMat;
        ballTransform = ball;
        ballRb = ball.GetComponent<Rigidbody>();

        // Create SplineTrack component
        splineTrack = gameObject.AddComponent<SplineTrack>();

        // Generate initial path
        splineTrack.GenerateTestPath(GENERATE_AHEAD + 50f);
        generatedUpTo = splineTrack.TotalLength;

        // Generate gates
        splineTrack.GenerateGates(0f, generatedUpTo);

        // Create initial mesh segments
        RebuildMeshSegments();

        // Position ball at the start of the track
        PositionBallAtStart();

        // Set up camera follower
        cameraFollow = cam.gameObject.AddComponent<SplineCameraFollow>();
        cameraFollow.splineTrack = splineTrack;
        cameraFollow.ballTransform = ballTransform;
        cameraFollow.cam = cam;

        // Adjust ball physics for spline
        if (ballRb != null)
        {
            ballRb.linearDamping = BALL_DRAG;
        }

        initialized = true;
    }

    /// <summary>
    /// Position the ball at the start of the spline, in the bottom of the U-groove.
    /// </summary>
    void PositionBallAtStart()
    {
        float startDist = 5f; // small offset from the very start

        Vector3 pos, tangent, normal, binormal;
        splineTrack.EvaluateFrame(startDist, out pos, out tangent, out normal, out binormal);

        // Place ball at bottom of U-shape: track center - 0.9 * normal
        // (0.9 = majorRadius difference in torus, but here it's just the groove depth)
        float grooveDepth = 0.9f; // how deep into the U the ball sits
        Vector3 ballPos = pos - normal * grooveDepth;
        ballTransform.position = ballPos;

        if (ballRb != null)
        {
            ballRb.linearVelocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;
        }

        splineTrack.BallDistance = startDist;
    }

    // ── UPDATE LOOP ──────────────────────────────────────────

    void FixedUpdate()
    {
        if (!initialized || ballRb == null) return;

        // Find ball's position on the spline
        float ballDist = splineTrack.FindClosestDistance(ballTransform.position);

        // Apply gentle forward force along the spline tangent
        // This supplements gravity on the slope and ensures minimum forward speed
        Vector3 tangent = splineTrack.EvaluateTangent(ballDist);
        float forwardSpeed = Vector3.Dot(ballRb.linearVelocity, tangent);

        if (forwardSpeed < MAX_FORWARD_SPEED)
        {
            float forceMagnitude = FORWARD_FORCE;
            // Reduce force as we approach max speed
            if (forwardSpeed > MAX_FORWARD_SPEED * 0.7f)
                forceMagnitude *= 1f - (forwardSpeed - MAX_FORWARD_SPEED * 0.7f)
                                       / (MAX_FORWARD_SPEED * 0.3f);

            ballRb.AddForce(tangent * forceMagnitude, ForceMode.Force);
        }

        // Check gate passage
        int passed = splineTrack.CheckGatePassage(ballDist);
        if (passed > 0)
        {
            score = splineTrack.Score;
            // TODO: hook into ScoreSync for UI update
        }

        // Extend path if ball is getting close to the end
        float distToEnd = generatedUpTo - ballDist;
        if (distToEnd < GENERATE_AHEAD)
        {
            float extend = GENERATE_AHEAD - distToEnd + 50f;
            splineTrack.ExtendPath(extend);
            splineTrack.GenerateGates(generatedUpTo, splineTrack.TotalLength);
            generatedUpTo = splineTrack.TotalLength;
            RebuildMeshSegments();
        }
    }

    // ── MESH MANAGEMENT ──────────────────────────────────────

    /// <summary>
    /// Create/update mesh segments to cover the visible portion of the track.
    /// </summary>
    void RebuildMeshSegments()
    {
        float totalLen = splineTrack.TotalLength;
        if (totalLen < 1f) return;

        // Determine which segments we need
        int startSeg = 0;
        int endSeg = Mathf.CeilToInt(totalLen / MESH_SEGMENT_LENGTH);

        // Create missing segments
        for (int s = meshSegments.Count; s < endSeg; s++)
        {
            float segStart = s * MESH_SEGMENT_LENGTH;
            float segEnd = Mathf.Min(segStart + MESH_SEGMENT_LENGTH, totalLen);

            if (segEnd - segStart < 0.5f) continue; // skip tiny segments

            // Generate mesh
            Mesh mesh = splineTrack.GenerateTrackMesh(segStart, segEnd);

            // Create GameObject
            GameObject segObj = new GameObject("TrackSegment_" + s);
            segObj.name = "Psychokinesis3"; // Match name for Sphere.cs contact normal detection
            segObj.transform.SetParent(transform);

            MeshFilter mf = segObj.AddComponent<MeshFilter>();
            mf.mesh = mesh;

            MeshRenderer mr = segObj.AddComponent<MeshRenderer>();
            mr.materials = new Material[] { trackFrontMaterial, trackInnerMaterial };
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = true;

            MeshCollider mc = segObj.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;

            // Zero-friction physics material (matching torus)
            PhysicsMaterial trackPhys = new PhysicsMaterial("SplineTrackPhys");
            trackPhys.dynamicFriction = 0f;
            trackPhys.staticFriction = 0f;
            trackPhys.bounciness = 0f;
            trackPhys.frictionCombine = PhysicsMaterialCombine.Minimum;
            trackPhys.bounceCombine = PhysicsMaterialCombine.Minimum;
            mc.material = trackPhys;

            meshSegments.Add(new MeshSegment
            {
                gameObject = segObj,
                startDist = segStart,
                endDist = segEnd,
            });
        }
    }

    /// <summary>
    /// Remove mesh segments that are far behind the ball.
    /// </summary>
    void CleanupBehind(float ballDist)
    {
        for (int i = meshSegments.Count - 1; i >= 0; i--)
        {
            if (meshSegments[i].endDist < ballDist - CLEANUP_BEHIND)
            {
                Destroy(meshSegments[i].gameObject);
                meshSegments.RemoveAt(i);
            }
        }
    }

    // ── PUBLIC API ───────────────────────────────────────────

    /// <summary>Get the spline tangent at the ball's current position (for Sphere.cs input).</summary>
    public Vector3 GetBallForwardDirection()
    {
        if (splineTrack == null) return Vector3.forward;
        return splineTrack.EvaluateTangent(splineTrack.BallDistance);
    }

    /// <summary>Get the spline track reference.</summary>
    public SplineTrack Track { get { return splineTrack; } }

    public int Score { get { return score; } }
}
