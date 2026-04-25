using UnityEngine;

/// <summary>
/// Auto-firing beam projectiles for Blitz mode.
/// Fires a short glowing bolt every FIRE_INTERVAL seconds in the track-forward
/// direction (+X). Each bolt travels at BEAM_SPEED, hits either a BlitzBox (sphere
/// bloom impact) or the torus surface (expanding ring ripple). Impacts are
/// parented to the torus so they rotate with the track.
///
/// Palette: cool plasma (cyan-white) at L0/L1, hot magenta-pink at L2 — chosen so
/// the beam never visually collides with the yellow gun pickup orb.
/// </summary>
public class BlitzBeam : MonoBehaviour
{
    // Tuning
    const int POOL_SIZE = 12;
    const float DEFAULT_FIRE_INTERVAL = 0.8f;
    const float BEAM_SPEED = 20f;
    const float BEAM_LENGTH = 0.45f;
    const float BEAM_WIDTH = 0.035f;
    const float HALO_WIDTH = 0.09f;
    const float IMPACT_DURATION = 0.5f;
    const float IMPACT_MAX_SCALE = 0.18f;
    const float RING_DURATION = 0.35f;
    const float RING_OUTER_SCALE = 0.22f;
    const float RING_INNER_RATIO = 0.55f;
    const float MAX_RANGE = 8f;
    const float BALL_OFFSET = 0.12f;
    const float FIRE_ANGLE = 8f; // degrees upward from horizontal — clears torus curvature

    // Shared
    bool mActive;
    float mFireTimer;
    float mFireInterval = DEFAULT_FIRE_INTERVAL;
    int mBeamCount = 1;
    int mGunLevel = 0;
    Color mCoreColor = Color.white;
    Color mFringeColor = new Color(0.4f, 0.95f, 1f);
    Material mBeamMat;
    Transform mTorusTrans;
    Torus mTorus;
    GameAudio mAudio;

    // Pool — parallel arrays
    LineRenderer[] mLines;
    LineRenderer[] mHalos;
    GameObject[] mHits;       // sphere bloom (box hits)
    GameObject[] mRings;      // annulus ripple (ground hits)
    Collider[] mHitCols;
    Vector3[] mOrigins, mTargets, mHitNormals;
    float[] mDist, mTrav, mImpAge;
    bool[] mFlying, mGlowing, mIsSurfaceHit;
    int mSlot;

    static Mesh sAnnulusMesh;

    public void Initialize(Material beamMat, Color color, Transform torusTransform, Torus torusScript)
    {
        mBeamMat = beamMat;
        mFringeColor = color;
        mTorusTrans = torusTransform;
        mTorus = torusScript;

        if (sAnnulusMesh == null) sAnnulusMesh = BuildAnnulusMesh();

        mLines = new LineRenderer[POOL_SIZE];
        mHalos = new LineRenderer[POOL_SIZE];
        mHitCols = new Collider[POOL_SIZE];
        mHits = new GameObject[POOL_SIZE];
        mRings = new GameObject[POOL_SIZE];
        mOrigins = new Vector3[POOL_SIZE];
        mTargets = new Vector3[POOL_SIZE];
        mHitNormals = new Vector3[POOL_SIZE];
        mDist = new float[POOL_SIZE];
        mTrav = new float[POOL_SIZE];
        mImpAge = new float[POOL_SIZE];
        mFlying = new bool[POOL_SIZE];
        mGlowing = new bool[POOL_SIZE];
        mIsSurfaceHit = new bool[POOL_SIZE];

        for (int i = 0; i < POOL_SIZE; i++)
        {
            // ── Halo glow (wider, dimmer LR behind the core) ──
            GameObject ho = new GameObject("BlitzHalo_" + i);
            LineRenderer hlr = ho.AddComponent<LineRenderer>();
            hlr.material = beamMat;
            hlr.positionCount = 2;
            hlr.useWorldSpace = true;
            hlr.receiveShadows = false;
            hlr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            hlr.enabled = false;
            mHalos[i] = hlr;

            // ── Core beam (bright solid bolt) ──
            GameObject lo = new GameObject("BlitzBeam_" + i);
            LineRenderer lr = lo.AddComponent<LineRenderer>();
            lr.material = beamMat;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.receiveShadows = false;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.enabled = false;
            mLines[i] = lr;

            // ── Impact bloom (sphere, for BlitzBox kills) ──
            GameObject imp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            imp.name = "BlitzImpact_" + i;
            Object.Destroy(imp.GetComponent<Collider>());
            imp.transform.localScale = Vector3.zero;

            MeshRenderer mr = imp.GetComponent<MeshRenderer>();
            Material impMat = new Material(beamMat);
            impMat.SetColor("_Color", mFringeColor);
            mr.material = impMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            imp.transform.SetParent(torusTransform, true);
            imp.SetActive(false);
            mHits[i] = imp;

            // ── Ground ripple (annulus, for torus surface hits) ──
            GameObject ring = new GameObject("BlitzRing_" + i);
            MeshFilter rmf = ring.AddComponent<MeshFilter>();
            rmf.sharedMesh = sAnnulusMesh;
            MeshRenderer rmr = ring.AddComponent<MeshRenderer>();
            Material ringMat = new Material(beamMat);
            ringMat.SetColor("_Color", mFringeColor);
            rmr.material = ringMat;
            rmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rmr.receiveShadows = false;
            ring.transform.SetParent(torusTransform, true);
            ring.SetActive(false);
            mRings[i] = ring;
        }

        ApplyPalette();
        mActive = false;
    }

    public void SetFireInterval(float interval) { mFireInterval = interval; }

    /// <summary>
    /// Drives beam count, color palette, and thickness from gun upgrade level.
    /// L0: baseline cyan plasma, 1 beam. L1: slightly hotter cyan, 2 beams.
    /// L2: magenta-pink, 3 beams, noticeably thicker.
    /// </summary>
    public void SetGunLevel(int level)
    {
        mGunLevel = Mathf.Clamp(level, 0, 2);
        mBeamCount = mGunLevel + 1;
        ApplyPalette();
    }

    void ApplyPalette()
    {
        float widthMul;
        switch (mGunLevel)
        {
            case 0:
                mFringeColor = new Color(0.3f, 0.9f, 1f);   // cyan
                widthMul = 1.0f;
                break;
            case 1:
                mFringeColor = new Color(0.5f, 0.95f, 1f);  // brighter cyan
                widthMul = 1.1f;
                break;
            default: // L2
                mFringeColor = new Color(1f, 0.3f, 0.8f);   // hot magenta-pink
                widthMul = 1.35f;
                break;
        }
        mCoreColor = Color.white;

        if (mLines == null) return;

        float beamW = BEAM_WIDTH * widthMul;
        float haloW = HALO_WIDTH * widthMul;

        for (int i = 0; i < POOL_SIZE; i++)
        {
            // Core gradient: fringe at tail → white at front → fringe at head
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(mFringeColor * 0.6f, 0f),
                    new GradientColorKey(mCoreColor, 0.3f),
                    new GradientColorKey(mFringeColor, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.3f, 0f),
                    new GradientAlphaKey(1f, 0.2f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
            mLines[i].colorGradient = g;
            mLines[i].startWidth = beamW;
            mLines[i].endWidth = beamW * 0.5f;

            // Halo gradient: dim fringe, wider for bloom
            Gradient hg = new Gradient();
            hg.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(mFringeColor * 0.15f, 0f),
                    new GradientColorKey(mFringeColor * 0.5f, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.05f, 0f),
                    new GradientAlphaKey(0.35f, 1f)
                }
            );
            mHalos[i].colorGradient = hg;
            mHalos[i].startWidth = haloW;
            mHalos[i].endWidth = haloW * 0.4f;
        }
    }

    public void SetActive(bool on)
    {
        mActive = on;
        if (!on)
        {
            for (int i = 0; i < POOL_SIZE; i++)
            {
                mLines[i].enabled = false;
                mHalos[i].enabled = false;
                mHits[i].SetActive(false);
                mRings[i].SetActive(false);
                mFlying[i] = false;
                mGlowing[i] = false;
            }
            mFireTimer = 0f;
            // Reset interval to default so the next run doesn't inherit the last run's
            // faster cadence for the first 1-2 fires before Torus.UpdateBlitzFireRate
            // recomputes it from the freshly-reset cadency level.
            mFireInterval = DEFAULT_FIRE_INTERVAL;
        }
    }

    void Start()
    {
        // GameAudio is created after BlitzBeam in SceneSetup — cache on first frame when it exists.
        mAudio = Object.FindAnyObjectByType<GameAudio>();
    }

    void Update()
    {
        if (!mActive) return;

        mFireTimer -= Time.deltaTime;
        if (mFireTimer <= 0f)
        {
            mFireTimer = mFireInterval;
            Fire();
        }

        float dt = Time.deltaTime;

        for (int i = 0; i < POOL_SIZE; i++)
        {
            // ── Animate traveling projectile ──
            if (mFlying[i])
            {
                mTrav[i] += BEAM_SPEED * dt;

                if (mTrav[i] >= mDist[i])
                {
                    // Reached target — beam off, impact on
                    mLines[i].enabled = false;
                    mHalos[i].enabled = false;
                    mFlying[i] = false;

                    if (mHitCols[i] != null
                        && mHitCols[i].gameObject != null
                        && mHitCols[i].gameObject.activeInHierarchy
                        && mHitCols[i].gameObject.name == "BlitzBox")
                    {
                        mTorus.OnBlitzBoxHit(mHitCols[i].gameObject);
                    }

                    if (mIsSurfaceHit[i])
                    {
                        // Flat ring tangent to curved surface needs ~r²/(2R) lift off the hit
                        // point so its far edges don't sink into the tube (minor radius 1.0,
                        // ring outer 0.22 → dip ≈ 0.024, so offset of 0.05 gives clean margin).
                        mRings[i].transform.position = mTargets[i] + mHitNormals[i] * 0.05f;
                        mRings[i].transform.rotation = Quaternion.FromToRotation(Vector3.up, mHitNormals[i]);
                        mRings[i].transform.localScale = Vector3.zero;
                        mRings[i].SetActive(true);
                    }
                    else
                    {
                        mHits[i].transform.position = mTargets[i];
                        mHits[i].SetActive(true);
                    }
                    mGlowing[i] = true;
                    mImpAge[i] = 0f;
                }
                else
                {
                    Vector3 dir = (mTargets[i] - mOrigins[i]).normalized;
                    float head = mTrav[i];
                    float tail = Mathf.Max(0f, head - BEAM_LENGTH);

                    Vector3 tailPos = mOrigins[i] + dir * tail;
                    Vector3 headPos = mOrigins[i] + dir * head;
                    mLines[i].SetPosition(0, tailPos);
                    mLines[i].SetPosition(1, headPos);
                    mHalos[i].SetPosition(0, tailPos);
                    mHalos[i].SetPosition(1, headPos);
                }
            }

            // ── Animate impact (sphere or ring, depending on hit type) ──
            if (mGlowing[i])
            {
                mImpAge[i] += dt;
                float duration = mIsSurfaceHit[i] ? RING_DURATION : IMPACT_DURATION;
                float t = mImpAge[i] / duration;

                if (t >= 1f)
                {
                    if (mIsSurfaceHit[i]) mRings[i].SetActive(false);
                    else mHits[i].SetActive(false);
                    mGlowing[i] = false;
                }
                else if (mIsSurfaceHit[i])
                {
                    // Ring: fast expand with sqrt curve, linear fade
                    float s = RING_OUTER_SCALE * Mathf.Sqrt(t);
                    mRings[i].transform.localScale = new Vector3(s, 1f, s);
                    MeshRenderer mr = mRings[i].GetComponent<MeshRenderer>();
                    Color c = mFringeColor;
                    c.a = (1f - t) * 0.65f;
                    mr.material.SetColor("_Color", c);
                }
                else
                {
                    // Sphere: quick bloom then smooth decay
                    float s = IMPACT_MAX_SCALE * (1f - t * t);
                    mHits[i].transform.localScale = Vector3.one * s;
                    MeshRenderer mr = mHits[i].GetComponent<MeshRenderer>();
                    Color c = mFringeColor;
                    c.a = 1f - t;
                    mr.material.SetColor("_Color", c);
                }
            }
        }
    }

    void Fire()
    {
        if (mAudio != null) mAudio.PlayBeamFire(mGunLevel);

        // Center beam is always present; upgrades add a left beam, then a right beam.
        // Keeps "what the ball is pointed at" hittable at every level — no more being
        // forced slightly off-axis to land shots at L1.
        FireSingleBeam(0f);
        if (mBeamCount >= 2) FireSingleBeam(-8f);
        if (mBeamCount >= 3) FireSingleBeam(8f);
    }

    void FireSingleBeam(float lateralDeg)
    {
        Vector3 origin = transform.position;
        float pitchRad = FIRE_ANGLE * Mathf.Deg2Rad;
        float yawRad = lateralDeg * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(
            Mathf.Cos(pitchRad) * Mathf.Cos(yawRad),
            Mathf.Sin(pitchRad),
            Mathf.Cos(pitchRad) * Mathf.Sin(yawRad)
        );
        Vector3 rayStart = origin + dir * BALL_OFFSET;

        int boxMask = 1 << BlitzBox.LAYER;
        int envMask = ~boxMask;

        // SphereCast on box layer — wider radius catches boxes the thin ray would miss.
        // Ignore triggers so orb pickup colliders don't register as obstacles.
        RaycastHit boxHit;
        bool hitBox = Physics.SphereCast(rayStart, 0.12f, dir, out boxHit, MAX_RANGE, boxMask, QueryTriggerInteraction.Ignore);

        // Raycast on everything else — visual endpoint (torus surface). Also skip triggers
        // so the beam passes through orbs instead of stopping short on their pickup zone.
        RaycastHit surfHit;
        bool hitSurf = Physics.Raycast(rayStart, dir, out surfHit, MAX_RANGE, envMask, QueryTriggerInteraction.Ignore);

        Vector3 target;
        float dist;
        Collider hitCol = null;
        bool isSurface = false;
        Vector3 normal = Vector3.up;

        if (hitBox && (!hitSurf || boxHit.distance <= surfHit.distance))
        {
            target = boxHit.point;
            dist = Vector3.Distance(origin, target);
            hitCol = boxHit.collider;
        }
        else if (hitSurf)
        {
            target = surfHit.point;
            dist = Vector3.Distance(origin, target);
            isSurface = true;
            normal = surfHit.normal;
        }
        else
        {
            target = rayStart + dir * MAX_RANGE;
            dist = MAX_RANGE + BALL_OFFSET;
        }

        // Recycle slot
        int s = mSlot;
        mSlot = (mSlot + 1) % POOL_SIZE;

        // Kill any previous state in this slot
        mLines[s].enabled = true;
        mHalos[s].enabled = true;
        mHits[s].SetActive(false);
        mRings[s].SetActive(false);
        mGlowing[s] = false;
        mHitCols[s] = hitCol;
        mIsSurfaceHit[s] = isSurface;
        mHitNormals[s] = normal;

        mOrigins[s] = origin;
        mTargets[s] = target;
        mDist[s] = dist;
        mTrav[s] = 0f;
        mFlying[s] = true;

        // Start at origin (will animate forward in Update)
        mLines[s].SetPosition(0, origin);
        mLines[s].SetPosition(1, origin);
        mHalos[s].SetPosition(0, origin);
        mHalos[s].SetPosition(1, origin);
    }

    // ── Procedural annulus (flat ring in XZ plane, Y = up normal) ──
    static Mesh BuildAnnulusMesh()
    {
        const int N = 32;
        Mesh m = new Mesh();
        Vector3[] verts = new Vector3[N * 2];
        int[] tris = new int[N * 6];

        for (int i = 0; i < N; i++)
        {
            float a = (float)i / N * Mathf.PI * 2f;
            float ca = Mathf.Cos(a);
            float sa = Mathf.Sin(a);
            verts[i * 2]     = new Vector3(ca * RING_INNER_RATIO, 0f, sa * RING_INNER_RATIO);
            verts[i * 2 + 1] = new Vector3(ca, 0f, sa);
        }

        for (int i = 0; i < N; i++)
        {
            int ni = (i + 1) % N;
            int o = i * 6;
            int a = i * 2;
            int b = i * 2 + 1;
            int c = ni * 2;
            int d = ni * 2 + 1;
            tris[o]     = a; tris[o + 1] = b; tris[o + 2] = d;
            tris[o + 3] = a; tris[o + 4] = d; tris[o + 5] = c;
        }

        m.vertices = verts;
        m.triangles = tris;
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }
}
