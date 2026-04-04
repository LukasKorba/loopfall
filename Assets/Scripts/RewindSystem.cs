using UnityEngine;
using System.Collections.Generic;

public class RewindSystem : MonoBehaviour
{
    private Transform ballTransform;
    private Rigidbody ballRigidbody;
    private Torus torusScript;
    private Transform torusTransform;

    // Camera
    private Transform cameraTransform;
    private Vector3 cameraStartPos;
    private Quaternion cameraStartRot;
    private Vector3 cameraDeathPos;
    private Quaternion cameraDeathRot;

    // Recording
    private struct Frame
    {
        public float torusAngle;
        public Vector3 ballLocalPos;
    }
    private List<Frame> frames = new List<Frame>();

    // Trail — segmented: one LineRenderer per 30° of torus rotation
    private const float SEGMENT_DEGREES = 30f;

    private struct TrailSegment
    {
        public LineRenderer renderer;
        public float startAngle;
        public float endAngle;
        public List<Vector3> controlPoints;  // Raw recorded positions
        public List<Vector3> points;         // Smoothed display positions
    }
    private List<TrailSegment> segments = new List<TrailSegment>();
    private Material trailMaterial;
    private const int SMOOTH_SUBDIVISIONS = 3; // Interpolated points between each pair

    // State machine
    public enum State
    {
        Idle,
        Recording,
        Pausing,        // 0.6s post-death (shake plays)
        Rewinding,      // ball follows path back, trail stays visible
        TrailDismiss,   // trail erases point-by-point from newest end
        ObstacleSwap,   // old obstacles shrink out, new pop in
        Complete
    }
    private State currentState = State.Idle;
    private float stateTimer = 0f;

    private const float PAUSE_DURATION = 0.6f;

    // Rewind speed: angle-based, scales with distance traveled
    // Short runs (< 50°): gentle 30°/s so player sees their path
    // Medium runs (50-200°): ramps up to 60°/s
    // Long runs (200°+): ramps up to max speed
    // Cap: max degrees/second — player must still recognize gates/trail
    private const float REWIND_MIN_SPEED = 30f;   // °/s for short runs
    private const float REWIND_MAX_SPEED = 90f;    // °/s cap — still recognizable
    private const float REWIND_RAMP_START = 50f;   // Angle where speedup begins
    private const float REWIND_RAMP_END = 300f;    // Angle where max speed is reached
    private const float REWIND_MIN_DURATION = 1.2f; // Never shorter than this

    // DEBUG: uncomment to test constant speed rewind
    // private const float DEBUG_REWIND_SPEED = 20f; // °/s constant

    private float rewindDuration = 1.5f;
    private const float LANDED_PAUSE_BASE = 0.3f;
    private const float LANDED_PAUSE_PER_POINT = 0.04f;
    private const float LANDED_PAUSE_MAX = 2.0f;
    private float landedPause = LANDED_PAUSE_BASE;
    private const float TRAIL_DISMISS_BASE = 0.8f;  // Base trail dismiss duration
    private const float TRAIL_DISMISS_PER_POINT = 0.04f; // Extra dismiss time per score point
    private const float TRAIL_DISMISS_MAX = 4.0f;    // Cap
    private float trailDismissDuration = TRAIL_DISMISS_BASE;
    private const float OBSTACLE_STAGGER = 0.05f;
    private const float OBSTACLE_ANIM_DURATION = 0.15f;

    // Obstacle swap
    private struct ObstacleAnim
    {
        public GameObject obj;
        public float triggerTime;
        public Vector3 originalScale;
    }
    private List<ObstacleAnim> oldAnims;
    private List<ObstacleAnim> newAnims;
    private bool oldPhaseDone;
    private bool newPhaseStarted;

    public void Initialize(Transform ball, Rigidbody ballRb,
                           Torus torus, Transform torusTrans,
                           Material trailMat,
                           Transform camTransform, Vector3 camStartPos, Quaternion camStartRot)
    {
        ballTransform = ball;
        ballRigidbody = ballRb;
        torusScript = torus;
        torusTransform = torusTrans;
        trailMaterial = trailMat;
        cameraTransform = camTransform;
        cameraStartPos = camStartPos;
        cameraStartRot = camStartRot;
    }

    public void StartRecording()
    {
        frames.Clear();
        DestroyAllSegments();
        currentState = State.Recording;
    }

    public void OnDeath()
    {
        currentState = State.Pausing;
        stateTimer = 0f;

        // Compute rewind speed based on distance traveled
        float deathAngle = frames.Count > 0 ? frames[frames.Count - 1].torusAngle : 0f;

        // Lerp speed from min to max based on how far we got
        float speedT = Mathf.Clamp01((deathAngle - REWIND_RAMP_START) / (REWIND_RAMP_END - REWIND_RAMP_START));
        float speed = Mathf.Lerp(REWIND_MIN_SPEED, REWIND_MAX_SPEED, speedT);

        // Duration = angle / speed, with a minimum floor
        rewindDuration = Mathf.Max(deathAngle / speed, REWIND_MIN_DURATION);
    }

    public void ResetSystem()
    {
        currentState = State.Idle;
        frames.Clear();
        DestroyAllSegments();
        stateTimer = 0f;
    }

    // ScoreSync: true during rewind cinematic (no UI shown)
    public bool IsPausingOrRewinding()
    {
        return currentState == State.Pausing ||
               currentState == State.Rewinding ||
               currentState == State.TrailDismiss;
    }

    // ScoreSync: true when UI should appear (obstacle swap + done)
    public bool IsComplete()
    {
        return currentState == State.ObstacleSwap ||
               currentState == State.Complete;
    }

    // True only when all animations (including obstacle swap) are done
    public bool IsFullyComplete()
    {
        return currentState == State.Complete;
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Recording:
                RecordFrame();
                UpdateSegmentVisibility(torusScript.GetAngle());
                break;

            case State.Pausing:
                stateTimer += Time.deltaTime;
                AnimateConeCollapse();
                if (stateTimer >= PAUSE_DURATION)
                {
                    currentState = State.Rewinding;
                    stateTimer = 0f;
                    ballRigidbody.isKinematic = true;
                    if (cameraTransform != null)
                    {
                        cameraDeathPos = cameraTransform.position;
                        cameraDeathRot = cameraTransform.rotation;
                    }
                }
                break;

            case State.Rewinding:
                stateTimer += Time.deltaTime;
                AnimateRewind();
                break;

            case State.TrailDismiss:
                stateTimer += Time.deltaTime;
                AnimateTrailDismiss();
                break;

            case State.ObstacleSwap:
                stateTimer += Time.deltaTime;
                AnimateObstacleSwap();
                break;
        }
    }

    // ── TRAIL SEGMENTS ──────────────────────────────────────

    TrailSegment CreateSegment(float startAngle)
    {
        TrailSegment seg;
        seg.startAngle = startAngle;
        seg.endAngle = startAngle;
        seg.controlPoints = new List<Vector3>();
        seg.points = new List<Vector3>();

        GameObject segObj = new GameObject("TrailSeg_" + startAngle.ToString("F0"));
        segObj.transform.SetParent(torusTransform, false);

        LineRenderer lr = segObj.AddComponent<LineRenderer>();
        lr.material = new Material(trailMaterial); // Instance so we can tint per-segment
        lr.useWorldSpace = false;
        lr.positionCount = 0;
        lr.numCapVertices = 0;  // No caps — segments overlap at boundaries
        lr.numCornerVertices = 2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.widthCurve = AnimationCurve.Constant(0f, 1f, TRAIL_WIDTH);
        lr.widthMultiplier = 1f;
        lr.startColor = Color.white;
        lr.endColor = Color.white;
        seg.renderer = lr;

        return seg;
    }

    void DestroyAllSegments()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i].renderer != null)
                Destroy(segments[i].renderer.gameObject);
        }
        segments.Clear();
    }

    // ── VISIBILITY ──────────────────────────────────────────────
    // Rolling 240° window — no hard loop boundaries. At any viewing angle,
    // show items within (viewAngle - 240, viewAngle + margin). Items from
    // a previous lap that are >240° behind naturally fall outside the window,
    // so there's no overlap. The transition across 360° is seamless.

    private const float TRAIL_VIEW_RANGE = 240f;

    // Rolling window for trail.
    // aheadMargin: how far ahead of viewAngle to show.
    //   Gameplay: 5° (trail ahead doesn't exist yet)
    //   Rewind: 120° (trail already drawn, camera sees ~90° ahead)
    // 240° behind + 120° ahead = 360° = exactly one lap, no physical overlap.
    // Trail color ramp: bright near ball → dim far away.
    // Each segment gets a uniform tint based on its distance from the view angle.
    private static readonly Color TRAIL_COLOR_NEAR = new Color(0.2f, 0.85f, 1.0f, 0.9f);   // Bright cyan near ball
    private static readonly Color TRAIL_COLOR_FAR  = new Color(0.1f, 0.25f, 0.6f, 0.15f); // Fades to deep blue

    void UpdateSegmentVisibility(float viewAngle, float aheadMargin = 5f)
    {
        for (int i = 0; i < segments.Count; i++)
        {
            TrailSegment seg = segments[i];
            if (seg.renderer == null) continue;

            float distBehind = viewAngle - seg.startAngle;

            bool visible = distBehind > -aheadMargin && distBehind < TRAIL_VIEW_RANGE;
            seg.renderer.enabled = visible;
            if (visible)
            {
                seg.renderer.positionCount = seg.points.Count;

                // Tint via material color — leaves LineRenderer colorGradient
                // free for the cone fade effect on the active segment
                float t = Mathf.Clamp01(distBehind / TRAIL_VIEW_RANGE);
                Color c = Color.Lerp(TRAIL_COLOR_NEAR, TRAIL_COLOR_FAR, t);
                seg.renderer.material.SetColor("_Color", c);
            }
        }
    }

    // Same rolling window for obstacles — 240° behind, 85° ahead
    void UpdateObstacleVisibility(float viewAngle)
    {
        List<Obstacle> obstacles = torusScript.GetObstacleList();
        for (int i = 0; i < obstacles.Count; i++)
        {
            if (obstacles[i].mGameObject == null) continue;
            float distBehind = viewAngle - obstacles[i].mAngle;
            bool visible = distBehind > -85f && distBehind < TRAIL_VIEW_RANGE;
            obstacles[i].mGameObject.SetActive(visible);
        }
    }


    // ── RECORDING ────────────────────────────────────────────

    // Taper: trail starts at ball diameter and narrows to trail width
    private const float TRAIL_WIDTH = 0.035f;
    private const float TAPER_WIDTH = 0.18f;   // ≈ ball diameter (scale 0.2)
    private const int TAPER_POINTS = 18 * (SMOOTH_SUBDIVISIONS + 1);  // Scaled for smoothed points
    private const int FADE_POINTS = 10 * (SMOOTH_SUBDIVISIONS + 1);  // Scaled for smoothed points

    // Smooth transition: previous segment's taper fades out gradually
    private LineRenderer fadingTaper = null;
    private float fadingTaperTimer = 0f;
    private const float TAPER_FADE_DURATION = 0.25f;

    void RecordFrame()
    {
        if (ballTransform == null || torusTransform == null) return;

        Vector3 localPos = torusTransform.InverseTransformPoint(ballTransform.position);
        float angle = torusScript.GetAngle();

        Frame f;
        f.torusAngle = angle;
        f.ballLocalPos = localPos;
        frames.Add(f);

        // Animate previous segment's taper fade-out
        if (fadingTaper != null)
        {
            fadingTaperTimer += Time.deltaTime;
            float fadeT = Mathf.Clamp01(fadingTaperTimer / TAPER_FADE_DURATION);
            float endWidth = Mathf.Lerp(TAPER_WIDTH, TRAIL_WIDTH, fadeT);
            int pc = fadingTaper.positionCount;
            if (pc >= 2)
            {
                float taperStart = 1f - (float)TAPER_POINTS / pc;
                taperStart = Mathf.Clamp01(taperStart);
                fadingTaper.widthCurve = new AnimationCurve(
                    new Keyframe(0f, TRAIL_WIDTH),
                    new Keyframe(taperStart, TRAIL_WIDTH),
                    new Keyframe(1f, endWidth)
                );
                // Fade alpha at the end too
                float endAlpha = 1f - fadeT;
                float fadeStart = 1f - (float)FADE_POINTS / pc;
                fadeStart = Mathf.Clamp01(fadeStart);
                Gradient grad = new Gradient();
                grad.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.white, 0f) },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, fadeStart),
                        new GradientAlphaKey(endAlpha * 0.5f, 1f)
                    }
                );
                fadingTaper.colorGradient = grad;
            }

            if (fadeT >= 1f)
            {
                // Fully faded — flatten for good
                fadingTaper.widthCurve = AnimationCurve.Constant(0f, 1f, TRAIL_WIDTH);
                Gradient flat = new Gradient();
                flat.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.white, 0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f) }
                );
                fadingTaper.colorGradient = flat;
                fadingTaper = null;
            }
        }

        // Determine which segment this point belongs to
        float segStart = Mathf.Floor(angle / SEGMENT_DEGREES) * SEGMENT_DEGREES;

        if (segments.Count == 0 || segments[segments.Count - 1].startAngle != segStart)
        {
            // Start fading the previous segment's taper instead of snapping it off
            if (segments.Count > 0)
            {
                TrailSegment prev = segments[segments.Count - 1];
                fadingTaper = prev.renderer;
                fadingTaperTimer = 0f;
            }

            // New segment — seed with overlap control points from previous
            TrailSegment newSeg = CreateSegment(segStart);
            if (segments.Count > 0)
            {
                TrailSegment prev = segments[segments.Count - 1];
                int overlapCount = Mathf.Min(prev.controlPoints.Count, 3);
                int startFrom = prev.controlPoints.Count - overlapCount;
                for (int oi = startFrom; oi < prev.controlPoints.Count; oi++)
                    newSeg.controlPoints.Add(prev.controlPoints[oi]);
            }
            segments.Add(newSeg);
        }

        // Add control point to the current (last) segment
        TrailSegment current = segments[segments.Count - 1];
        current.controlPoints.Add(localPos);
        current.endAngle = angle;

        // Rebuild smoothed display points via Catmull-Rom
        RebuildSmoothedPoints(ref current);

        current.renderer.positionCount = current.points.Count;
        current.renderer.SetPositions(current.points.ToArray());

        UpdateTaper(current.renderer, current.points.Count);
        segments[segments.Count - 1] = current;
    }

    void UpdateTaper(LineRenderer lr, int pointCount)
    {
        if (pointCount < 2)
        {
            lr.startWidth = TAPER_WIDTH;
            lr.endWidth = TAPER_WIDTH;
            return;
        }

        // Width: flat at TRAIL_WIDTH, ramp to ball diameter over last TAPER_POINTS
        // t=0 is first point (oldest), t=1 is last point (near ball)
        float taperStart = 1f - (float)TAPER_POINTS / pointCount;
        taperStart = Mathf.Clamp01(taperStart);

        lr.widthCurve = new AnimationCurve(
            new Keyframe(0f, TRAIL_WIDTH),
            new Keyframe(taperStart, TRAIL_WIDTH),
            new Keyframe(1f, TAPER_WIDTH)
        );
        lr.widthMultiplier = 1f;

        // Alpha: fully opaque everywhere, then fade to transparent over last
        // FADE_POINTS — so the cone dissolves like wind behind the ball
        float fadeStart = 1f - (float)FADE_POINTS / pointCount;
        fadeStart = Mathf.Clamp01(fadeStart);

        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f) },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, fadeStart),
                new GradientAlphaKey(0f, 1f)
            }
        );
        lr.colorGradient = grad;
    }

    // ── CATMULL-ROM SMOOTHING ──────────────────────────────────

    void RebuildSmoothedPoints(ref TrailSegment seg)
    {
        List<Vector3> cp = seg.controlPoints;
        seg.points.Clear();

        if (cp.Count < 2)
        {
            if (cp.Count == 1) seg.points.Add(cp[0]);
            return;
        }

        // First point
        seg.points.Add(cp[0]);

        for (int i = 0; i < cp.Count - 1; i++)
        {
            Vector3 p0 = cp[Mathf.Max(i - 1, 0)];
            Vector3 p1 = cp[i];
            Vector3 p2 = cp[i + 1];
            Vector3 p3 = cp[Mathf.Min(i + 2, cp.Count - 1)];

            for (int s = 1; s <= SMOOTH_SUBDIVISIONS; s++)
            {
                float t = (float)s / (SMOOTH_SUBDIVISIONS + 1);
                seg.points.Add(CatmullRom(p0, p1, p2, p3, t));
            }
            seg.points.Add(p2);
        }
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    // ── CONE COLLAPSE (death → cone shrinks over a few frames) ────────

    private const float CONE_COLLAPSE_DURATION = 0.25f;

    void AnimateConeCollapse()
    {
        if (segments.Count == 0) return;
        TrailSegment active = segments[segments.Count - 1];
        if (active.renderer == null || active.points.Count < 2) return;

        float t = Mathf.Clamp01(stateTimer / CONE_COLLAPSE_DURATION);
        int pointCount = active.points.Count;

        // Width: lerp taper peak from TAPER_WIDTH → TRAIL_WIDTH
        float currentTaper = Mathf.Lerp(TAPER_WIDTH, TRAIL_WIDTH, t);
        float taperStart = 1f - (float)TAPER_POINTS / pointCount;
        taperStart = Mathf.Clamp01(taperStart);

        active.renderer.widthCurve = new AnimationCurve(
            new Keyframe(0f, TRAIL_WIDTH),
            new Keyframe(taperStart, TRAIL_WIDTH),
            new Keyframe(1f, currentTaper)
        );

        // Alpha: expand the fade zone as cone collapses, ending fully opaque flat
        float fadeStart = 1f - (float)FADE_POINTS / pointCount;
        fadeStart = Mathf.Clamp01(fadeStart);
        float currentFadeStart = Mathf.Lerp(fadeStart, 1f, t);

        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f) },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, currentFadeStart),
                new GradientAlphaKey(Mathf.Lerp(0f, 1f, t), 1f)
            }
        );
        active.renderer.colorGradient = grad;
    }

    // ── REWIND (trail stays intact, ball moves back) ──────────

    void AnimateRewind()
    {
        if (frames.Count < 2)
        {
            DestroyAllSegments();
            BeginObstacleSwap();
            return;
        }

        // DEBUG: uncomment for constant speed testing
        // float deathAngle = frames[frames.Count - 1].torusAngle;
        // float debugDuration = deathAngle / DEBUG_REWIND_SPEED;
        // float progress = Mathf.Clamp01(stateTimer / debugDuration);
        // float eased = progress;

        // Ease in-out: starts gentle (player sees where they died),
        // accelerates through the middle, decelerates to land smoothly
        float progress = Mathf.Clamp01(stateTimer / rewindDuration);
        float eased = EaseInOutCubic(progress);

        float frameF = (1f - eased) * (frames.Count - 1);
        int idx = Mathf.FloorToInt(frameF);
        float frac = frameF - idx;
        idx = Mathf.Clamp(idx, 0, frames.Count - 1);
        int nextIdx = Mathf.Min(idx + 1, frames.Count - 1);

        float angle = Mathf.Lerp(frames[idx].torusAngle, frames[nextIdx].torusAngle, frac);
        torusScript.SetAngle(angle);

        Vector3 localPos = Vector3.Lerp(frames[idx].ballLocalPos, frames[nextIdx].ballLocalPos, frac);
        ballTransform.position = torusTransform.TransformPoint(localPos);

        // During rewind: 120° ahead margin (trail already exists ahead of ball)
        UpdateSegmentVisibility(angle, 120f);
        UpdateObstacleVisibility(angle);

        // Smoothly return camera to start position + rotation
        if (cameraTransform != null)
        {
            cameraTransform.position = Vector3.Lerp(cameraDeathPos, cameraStartPos, eased);
            cameraTransform.rotation = Quaternion.Slerp(cameraDeathRot, cameraStartRot, eased);
        }

        if (progress >= 1f)
        {
            // Ball is at start — begin trail dismiss after brief pause
            float endAngle = frames[frames.Count - 1].torusAngle;
            float score = endAngle / 10f;
            landedPause = Mathf.Min(LANDED_PAUSE_BASE + score * LANDED_PAUSE_PER_POINT, LANDED_PAUSE_MAX);
            currentState = State.TrailDismiss;
            stateTimer = -landedPause; // Negative = pause before erasing starts
            BeginTrailDismiss();
        }
    }

    // ── TRAIL DISMISS ────────────────────────────────────────
    // After rewind lands at angle 0, the camera can only see ~60-90° ahead.
    // We only animate dismiss for segments within DISMISS_RANGE (60°).
    // Everything else is hidden instantly — it's off-screen anyway.

    private const float DISMISS_RANGE = 90f;  // Only animate dismiss for this range
    private List<int> dismissSegIndices = new List<int>();   // ordered oldest→newest
    private List<int> dismissSegCounts = new List<int>();
    private int dismissTotalPoints = 0;

    void BeginTrailDismiss()
    {
        dismissSegIndices.Clear();
        dismissSegCounts.Clear();
        dismissTotalPoints = 0;

        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i].renderer == null) continue;

            // Only animate dismiss for segments within DISMISS_RANGE of start (0°).
            // These are the only ones visible to the camera after rewind.
            if (segments[i].startAngle < DISMISS_RANGE)
            {
                segments[i].renderer.enabled = true;
                segments[i].renderer.positionCount = segments[i].points.Count;
                dismissSegIndices.Add(i);
                dismissSegCounts.Add(segments[i].points.Count);
                dismissTotalPoints += segments[i].points.Count;
            }
            else
            {
                segments[i].renderer.enabled = false;
            }
        }

        // Duration scales with visible points only (much shorter now)
        float approxScore = DISMISS_RANGE / 10f; // ~6 points worth
        trailDismissDuration = Mathf.Min(TRAIL_DISMISS_BASE + approxScore * TRAIL_DISMISS_PER_POINT, TRAIL_DISMISS_MAX);
    }

    void AnimateTrailDismiss()
    {
        // stateTimer starts negative (landed pause), dismiss begins at 0
        if (stateTimer < 0f) return;

        if (dismissTotalPoints == 0)
        {
            DestroyAllSegments();
            BeginObstacleSwap();
            return;
        }

        float progress = Mathf.Clamp01(stateTimer / trailDismissDuration);
        float eased = EaseOutCubic(progress);

        // Total points erased so far, from the newest end
        int pointsErased = Mathf.RoundToInt(dismissTotalPoints * eased);

        // Walk from newest segment backward, erasing points smoothly
        int eraseRemaining = pointsErased;
        for (int vi = dismissSegIndices.Count - 1; vi >= 0; vi--)
        {
            int segIdx = dismissSegIndices[vi];
            TrailSegment seg = segments[segIdx];
            if (seg.renderer == null) continue;

            int fullCount = dismissSegCounts[vi];

            if (eraseRemaining >= fullCount)
            {
                seg.renderer.positionCount = 0;
                seg.renderer.enabled = false;
                eraseRemaining -= fullCount;
            }
            else if (eraseRemaining > 0)
            {
                seg.renderer.positionCount = fullCount - eraseRemaining;
                seg.renderer.enabled = true;
                eraseRemaining = 0;
            }
            else
            {
                seg.renderer.positionCount = fullCount;
                seg.renderer.enabled = true;
            }
        }

        if (progress >= 1f)
        {
            DestroyAllSegments();
            BeginObstacleSwap();
        }
    }

    // ── OBSTACLE SWAP ────────────────────────────────────────

    void BeginObstacleSwap()
    {
        currentState = State.ObstacleSwap;
        stateTimer = 0f;
        oldPhaseDone = false;
        newPhaseStarted = false;

        // Only animate obstacles from the first lap (mAngle < 360) —
        // these are the ones physically visible at angle 0.
        // Hide everything else immediately.
        oldAnims = new List<ObstacleAnim>();
        List<Obstacle> obstacles = torusScript.GetObstacleList();
        int staggerIdx = 0;
        for (int i = 0; i < obstacles.Count; i++)
        {
            if (obstacles[i].mGameObject == null) continue;

            if (obstacles[i].mAngle < 360f)
            {
                obstacles[i].mGameObject.SetActive(true);
                ObstacleAnim a;
                a.obj = obstacles[i].mGameObject;
                a.triggerTime = staggerIdx * OBSTACLE_STAGGER;
                a.originalScale = a.obj.transform.localScale;
                oldAnims.Add(a);
                staggerIdx++;
            }
            else
            {
                obstacles[i].mGameObject.SetActive(false);
            }
        }
    }

    void AnimateObstacleSwap()
    {
        // Phase A: shrink old obstacles to zero
        if (!oldPhaseDone)
        {
            bool allDone = true;
            for (int i = 0; i < oldAnims.Count; i++)
            {
                var a = oldAnims[i];
                if (a.obj == null) continue;

                float elapsed = stateTimer - a.triggerTime;
                if (elapsed < 0f) { allDone = false; continue; }

                float p = Mathf.Clamp01(elapsed / OBSTACLE_ANIM_DURATION);
                a.obj.transform.localScale = a.originalScale * (1f - EaseInCubic(p));

                if (p < 1f) allDone = false;
            }

            if (allDone)
            {
                oldPhaseDone = true;

                // Reset torus state + generate fresh obstacles
                torusScript.SoftReset();
                torusScript.ReplaceObstacles();

                // Prepare new obstacles — start at scale zero
                newAnims = new List<ObstacleAnim>();
                List<Obstacle> newObs = torusScript.GetObstacleList();
                float baseTime = stateTimer + 0.08f;
                for (int i = 0; i < newObs.Count; i++)
                {
                    if (newObs[i].mGameObject == null) continue;
                    ObstacleAnim a;
                    a.obj = newObs[i].mGameObject;
                    a.triggerTime = baseTime + i * OBSTACLE_STAGGER;
                    a.originalScale = a.obj.transform.localScale;
                    a.obj.transform.localScale = Vector3.zero;
                    newAnims.Add(a);
                }
                newPhaseStarted = true;
            }
        }

        // Phase B: scale new obstacles in with bounce
        if (newPhaseStarted)
        {
            bool allDone = true;
            for (int i = 0; i < newAnims.Count; i++)
            {
                var a = newAnims[i];
                if (a.obj == null) continue;

                float elapsed = stateTimer - a.triggerTime;
                if (elapsed < 0f) { allDone = false; continue; }

                float p = Mathf.Clamp01(elapsed / OBSTACLE_ANIM_DURATION);
                a.obj.transform.localScale = a.originalScale * EaseOutBack(p);

                if (p < 1f) allDone = false;
            }

            if (allDone)
                currentState = State.Complete;
        }
    }

    // ── EASING ───────────────────────────────────────────────

    float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }

    float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    float EaseInCubic(float t)
    {
        return t * t * t;
    }

    float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
