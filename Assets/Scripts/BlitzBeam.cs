using UnityEngine;

/// <summary>
/// Auto-firing beam projectiles for Blitz mode.
/// Fires a short glowing bolt every FIRE_INTERVAL seconds in the track-forward
/// direction (+X). Each bolt travels at BEAM_SPEED, hits the torus surface,
/// and leaves a brief glowing impact spot. Impacts are parented to the torus
/// so they rotate with the track.
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
    const float MAX_RANGE = 8f;
    const float BALL_OFFSET = 0.12f;
    const float FIRE_ANGLE = 8f; // degrees upward from horizontal — clears torus curvature

    // Shared
    bool mActive;
    float mFireTimer;
    float mFireInterval = DEFAULT_FIRE_INTERVAL;
    int mBeamCount = 1;
    Color mColor;
    Transform mTorusTrans;
    Torus mTorus;

    // Pool — parallel arrays
    LineRenderer[] mLines;
    LineRenderer[] mHalos;
    GameObject[] mHits;
    Collider[] mHitCols;
    Vector3[] mOrigins, mTargets;
    float[] mDist, mTrav, mImpAge;
    bool[] mFlying, mGlowing;
    int mSlot;

    public void Initialize(Material beamMat, Color color, Transform torusTransform, Torus torusScript)
    {
        mColor = color;
        mTorusTrans = torusTransform;
        mTorus = torusScript;

        mLines = new LineRenderer[POOL_SIZE];
        mHalos = new LineRenderer[POOL_SIZE];
        mHitCols = new Collider[POOL_SIZE];
        mHits = new GameObject[POOL_SIZE];
        mOrigins = new Vector3[POOL_SIZE];
        mTargets = new Vector3[POOL_SIZE];
        mDist = new float[POOL_SIZE];
        mTrav = new float[POOL_SIZE];
        mImpAge = new float[POOL_SIZE];
        mFlying = new bool[POOL_SIZE];
        mGlowing = new bool[POOL_SIZE];

        for (int i = 0; i < POOL_SIZE; i++)
        {
            // ── Halo glow (wider, dimmer LR behind the core) ──
            GameObject ho = new GameObject("BlitzHalo_" + i);
            LineRenderer hlr = ho.AddComponent<LineRenderer>();
            hlr.material = beamMat;
            hlr.startWidth = HALO_WIDTH;
            hlr.endWidth = HALO_WIDTH * 0.4f;
            hlr.positionCount = 2;
            hlr.useWorldSpace = true;
            hlr.receiveShadows = false;
            hlr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Gradient hg = new Gradient();
            hg.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(color * 0.15f, 0f),
                    new GradientColorKey(color * 0.5f, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.05f, 0f),
                    new GradientAlphaKey(0.35f, 1f)
                }
            );
            hlr.colorGradient = hg;
            hlr.enabled = false;
            mHalos[i] = hlr;

            // ── Core beam (bright solid bolt) ──
            GameObject lo = new GameObject("BlitzBeam_" + i);
            LineRenderer lr = lo.AddComponent<LineRenderer>();
            lr.material = beamMat;
            lr.startWidth = BEAM_WIDTH;
            lr.endWidth = BEAM_WIDTH * 0.5f;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.receiveShadows = false;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(color * 0.6f, 0f),
                    new GradientColorKey(Color.white, 0.3f),
                    new GradientColorKey(color, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.3f, 0f),
                    new GradientAlphaKey(1f, 0.2f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
            lr.colorGradient = g;
            lr.enabled = false;
            mLines[i] = lr;

            // ── Impact glow (small additive sphere, parented to torus) ──
            GameObject imp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            imp.name = "BlitzImpact_" + i;
            Object.Destroy(imp.GetComponent<Collider>());
            imp.transform.localScale = Vector3.zero;

            MeshRenderer mr = imp.GetComponent<MeshRenderer>();
            Material impMat = new Material(beamMat);
            impMat.SetColor("_Color", color);
            mr.material = impMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            // Parent to torus so impact rotates with the track surface
            imp.transform.SetParent(torusTransform, true);
            imp.SetActive(false);
            mHits[i] = imp;
        }

        mActive = false;
    }

    public void SetFireInterval(float interval) { mFireInterval = interval; }
    public void SetBeamCount(int count) { mBeamCount = Mathf.Clamp(count, 1, 3); }

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
                mFlying[i] = false;
                mGlowing[i] = false;
            }
            mFireTimer = 0f;
        }
    }

    void Update()
    {
        if (!mActive) return;

        // Fire timer
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

                    // Check if we hit a BlitzBox
                    if (mHitCols[i] != null
                        && mHitCols[i].gameObject != null
                        && mHitCols[i].gameObject.activeInHierarchy
                        && mHitCols[i].gameObject.name == "BlitzBox")
                    {
                        mTorus.OnBlitzBoxHit(mHitCols[i].gameObject);
                    }

                    mHits[i].transform.position = mTargets[i];
                    mHits[i].SetActive(true);
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

            // ── Animate impact glow (grow → shrink + fade) ──
            if (mGlowing[i])
            {
                mImpAge[i] += dt;
                float t = mImpAge[i] / IMPACT_DURATION;

                if (t >= 1f)
                {
                    mHits[i].SetActive(false);
                    mGlowing[i] = false;
                }
                else
                {
                    // Quick bloom then smooth decay
                    float s = IMPACT_MAX_SCALE * (1f - t * t);
                    mHits[i].transform.localScale = Vector3.one * s;

                    // Fade intensity
                    MeshRenderer mr = mHits[i].GetComponent<MeshRenderer>();
                    Color c = mColor;
                    c.a = 1f - t;
                    mr.material.SetColor("_Color", c);
                }
            }
        }
    }

    void Fire()
    {
        if (mBeamCount >= 3)
        {
            FireSingleBeam(-8f);
            FireSingleBeam(0f);
            FireSingleBeam(8f);
        }
        else if (mBeamCount >= 2)
        {
            FireSingleBeam(-6f);
            FireSingleBeam(6f);
        }
        else
        {
            FireSingleBeam(0f);
        }
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
        mGlowing[s] = false;
        mHitCols[s] = hitCol;

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
}
