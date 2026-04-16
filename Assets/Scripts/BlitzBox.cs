using UnityEngine;

/// <summary>
/// Shootable target for Blitz mode.
/// 1HP boxes = cubes (shoot for points).
/// 3HP sentinels = spiky stars — each hit pops off a cluster of spikes until only a
/// stub remains, then the final hit kills it. Spike count is the HP readout.
/// Buttons = pyramids (shoot to open linked gate).
/// </summary>
public class BlitzBox
{
    public const int LAYER = 8;

    public GameObject mGameObject;
    public GameObject mCube;
    public float mAngle;
    public bool mDestroyed;
    public int mHitPoints;
    public int mMaxHitPoints;
    public BlitzGate mLinkedGate;

    Material mMat;
    Vector3 mSurface;
    Vector3 mNormal;
    bool mIsButton;
    bool mIsSentinel;

    Quaternion mBaseRotation;
    Vector3 mBasePosition;
    float mPulsePhase;

    // Spiky sentinel children (3HP only)
    GameObject[] mSpikes;
    Vector3[] mSpikeDirs;

    const float SIZE_1HP = 0.14f;
    const float SIZE_BUTTON = 0.24f;
    const float CORE_RADIUS = 0.055f;
    const float SPIKE_LENGTH = 0.085f;
    const int SPIKES_AT_3HP = 12;
    const int SPIKES_AT_2HP = 7;
    const int SPIKES_AT_1HP = 3;

    static Mesh sPyramidMesh;
    static Mesh sOctahedronMesh;
    static Mesh sIcosahedronMesh;
    static Mesh sSpikeMesh;
    static Vector3[] sIcosaVerts;

    public BlitzBox(float crossAngleDeg, Material boxMat, int hitPoints = 1, bool isButton = false)
    {
        mDestroyed = false;
        mHitPoints = hitPoints;
        mMaxHitPoints = hitPoints;
        mIsButton = isButton;
        mIsSentinel = !isButton && hitPoints >= 3;
        mPulsePhase = Random.value * Mathf.PI * 2f;

        float a = crossAngleDeg * Mathf.Deg2Rad;
        float sinA = Mathf.Sin(a);
        float cosA = Mathf.Cos(a);

        mSurface = new Vector3(0f, -10f - sinA, -cosA);
        mNormal = new Vector3(0f, sinA, cosA).normalized;

        mGameObject = new GameObject("BlitzBoxRoot");

        if (isButton)
        {
            BuildButton(boxMat);
        }
        else if (mIsSentinel)
        {
            BuildSentinel(boxMat);
        }
        else
        {
            BuildSmallCube(boxMat);
        }

        ApplyVisuals();
    }

    // ── 1HP CUBE ──────────────────────────────────────────────

    void BuildSmallCube(Material boxMat)
    {
        float size = SIZE_1HP;
        Vector3 center = mSurface + mNormal * (size * 0.75f);

        mCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mCube.name = "BlitzBox";
        mCube.transform.parent = mGameObject.transform;
        mCube.transform.localPosition = center;
        mCube.transform.localScale = Vector3.one * size;
        mBaseRotation = Quaternion.LookRotation(Vector3.right, mNormal);
        mCube.transform.localRotation = mBaseRotation;
        mBasePosition = center;
        mCube.layer = LAYER;

        SetupRenderer(mCube, boxMat, receiveShadows: true);
    }

    // ── BUTTON (pyramid beacon) ──────────────────────────────

    void BuildButton(Material boxMat)
    {
        float size = SIZE_BUTTON;
        Vector3 center = mSurface + mNormal * (size * 0.75f);

        mCube = new GameObject("BlitzBox");
        mCube.AddComponent<MeshFilter>().mesh = GetPyramidMesh();
        mCube.AddComponent<MeshRenderer>();
        mCube.AddComponent<BoxCollider>();
        mCube.transform.parent = mGameObject.transform;
        mCube.transform.localPosition = center;
        mCube.transform.localScale = Vector3.one * size;
        mBaseRotation = Quaternion.LookRotation(Vector3.right, mNormal);
        mCube.transform.localRotation = mBaseRotation;
        mBasePosition = center;
        mCube.layer = LAYER;

        SetupRenderer(mCube, boxMat, receiveShadows: true);
    }

    // ── 3HP SPIKY SENTINEL ────────────────────────────────────

    void BuildSentinel(Material boxMat)
    {
        float outerRadius = CORE_RADIUS + SPIKE_LENGTH;
        Vector3 center = mSurface + mNormal * (outerRadius * 0.9f);

        // Core: icosahedron, scaled to world-space CORE_RADIUS at its vertices.
        // Name matches 1HP cubes/buttons so Sphere death + BlitzBeam hit checks pick it up.
        mCube = new GameObject("BlitzBox");
        mCube.AddComponent<MeshFilter>().mesh = GetIcosahedronMesh();
        mCube.AddComponent<MeshRenderer>();
        mCube.transform.parent = mGameObject.transform;
        mCube.transform.localPosition = center;
        mCube.transform.localScale = Vector3.one * CORE_RADIUS;
        mBaseRotation = Quaternion.LookRotation(Vector3.right, mNormal);
        mCube.transform.localRotation = mBaseRotation;
        mBasePosition = center;
        mCube.layer = LAYER;

        // Sphere collider sized to wrap spike tips so shots hit the silhouette, not just the core.
        SphereCollider sc = mCube.AddComponent<SphereCollider>();
        sc.center = Vector3.zero;
        sc.radius = outerRadius / CORE_RADIUS * 0.92f; // local radius; scales with core

        SetupRenderer(mCube, boxMat, receiveShadows: false);

        CreateSpikes();
    }

    void CreateSpikes()
    {
        Vector3[] dirs = GetIcosaVerts();
        mSpikes = new GameObject[dirs.Length];
        mSpikeDirs = new Vector3[dirs.Length];

        // Spikes live as children of the core, so they inherit its spin and
        // orient naturally with vertex positions. Scale compensates for parent scaling.
        float spikeLocalScale = SPIKE_LENGTH / CORE_RADIUS;

        for (int i = 0; i < dirs.Length; i++)
        {
            Vector3 dir = dirs[i].normalized;
            mSpikeDirs[i] = dir;

            GameObject spike = new GameObject("Spike_" + i);
            spike.AddComponent<MeshFilter>().mesh = GetSpikeMesh();
            spike.AddComponent<MeshRenderer>();
            spike.transform.parent = mCube.transform;
            spike.transform.localPosition = dir; // in core-local units; dir is unit, surface sits at 1
            spike.transform.localRotation = Quaternion.LookRotation(dir);
            spike.transform.localScale = Vector3.one * spikeLocalScale;

            MeshRenderer mr = spike.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mMat; // share core material so emission pulses together
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            mSpikes[i] = spike;
        }
    }

    void SetupRenderer(GameObject obj, Material boxMat, bool receiveShadows)
    {
        MeshRenderer mr = obj.GetComponent<MeshRenderer>();
        mMat = new Material(boxMat);
        mr.material = mMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = receiveShadows;
    }

    void ApplyVisuals()
    {
        // Emission ramps up as HP drops — weakened sentinel looks more volatile.
        float intensity;
        if (mIsButton) intensity = 2.0f;
        else if (mIsSentinel) intensity = mHitPoints >= 3 ? 2.0f : mHitPoints == 2 ? 2.8f : 3.8f;
        else intensity = 1.5f;
        mMat.SetFloat("_EmissionIntensity", intensity);
    }

    /// <summary>Hit once. Returns true if destroyed.</summary>
    public bool Hit()
    {
        if (mDestroyed) return false;

        mHitPoints--;
        if (mHitPoints <= 0)
        {
            DestroyBox();
            return true;
        }

        if (mIsSentinel)
        {
            int target = mHitPoints == 2 ? SPIKES_AT_2HP : SPIKES_AT_1HP;
            RemoveSpikesDown(target);
        }
        ApplyVisuals();
        return false;
    }

    void RemoveSpikesDown(int targetCount)
    {
        if (mSpikes == null) return;

        int active = 0;
        for (int i = 0; i < mSpikes.Length; i++)
            if (mSpikes[i].activeSelf) active++;

        // Pop spikes one by one (random pick) until we hit the target count.
        while (active > targetCount)
        {
            int pick = Random.Range(0, active);
            int idx = -1;
            for (int i = 0; i < mSpikes.Length; i++)
            {
                if (!mSpikes[i].activeSelf) continue;
                if (pick == 0) { idx = i; break; }
                pick--;
            }
            if (idx < 0) break;

            SpawnSpikeFragments(mSpikes[idx].transform);
            mSpikes[idx].SetActive(false);
            active--;
        }
    }

    void SpawnSpikeFragments(Transform spikeT)
    {
        Vector3 pos = spikeT.position;
        Vector3 outward = spikeT.forward;

        for (int i = 0; i < 4; i++)
        {
            GameObject frag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frag.name = "spikefrag";
            frag.transform.position = pos;
            frag.transform.localScale = Vector3.one * Random.Range(0.015f, 0.03f);
            frag.transform.rotation = Random.rotation;

            Object.Destroy(frag.GetComponent<BoxCollider>());

            MeshRenderer mr = frag.GetComponent<MeshRenderer>();
            mr.material = mMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Rigidbody rb = frag.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearVelocity = outward * Random.Range(2f, 3.5f) + Random.insideUnitSphere * 1.2f;
            rb.angularVelocity = Random.insideUnitSphere * 18f;

            Object.Destroy(frag, 0.5f);
        }
    }

    /// <summary>Destroy with fragment burst.</summary>
    public void DestroyBox()
    {
        if (mDestroyed) return;
        mDestroyed = true;

        Vector3 pos = mCube.transform.position;
        int fragCount = mMaxHitPoints >= 3 ? 10 : mIsButton ? 8 : 6;
        float speed = mMaxHitPoints >= 3 ? 3.5f : 2.5f;

        for (int i = 0; i < fragCount; i++)
        {
            GameObject frag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frag.name = "frag";
            frag.transform.position = pos;
            frag.transform.localScale = Vector3.one * Random.Range(0.02f, 0.05f);
            frag.transform.rotation = Random.rotation;

            Object.Destroy(frag.GetComponent<BoxCollider>());

            MeshRenderer mr = frag.GetComponent<MeshRenderer>();
            mr.material = mMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Rigidbody rb = frag.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearVelocity = Random.insideUnitSphere * speed;
            rb.angularVelocity = Random.insideUnitSphere * 15f;

            Object.Destroy(frag, 0.5f);
        }

        mGameObject.SetActive(false);
    }

    // ── Per-frame animation ──

    public void Animate(float time)
    {
        if (mDestroyed || mCube == null) return;

        if (mIsButton)
        {
            mCube.transform.localRotation = mBaseRotation * Quaternion.AngleAxis(time * 60f, Vector3.up);
            float pulse = 2.0f + Mathf.Sin(time * 6f + mPulsePhase) * 0.8f
                        + Mathf.Sin(time * 10f + mPulsePhase * 0.7f) * 0.3f;
            mMat.SetFloat("_EmissionIntensity", pulse);
        }
        else if (mIsSentinel)
        {
            mCube.transform.localRotation = mBaseRotation * Quaternion.AngleAxis(time * 28f, Vector3.up);
            // Subtle breathing pulse on top of HP-driven base intensity
            float basePulse = mHitPoints >= 3 ? 2.0f : mHitPoints == 2 ? 2.8f : 3.8f;
            float pulse = basePulse + Mathf.Sin(time * 5f + mPulsePhase) * 0.35f;
            mMat.SetFloat("_EmissionIntensity", pulse);
        }
        // 1HP static cube — no animation, identity is clarity
    }

    // ── Icosahedron mesh (cached) — flat-shaded, 12 verts become 60 via per-face expansion ──

    public static Vector3[] GetIcosaVerts()
    {
        if (sIcosaVerts != null) return sIcosaVerts;

        float phi = (1f + Mathf.Sqrt(5f)) * 0.5f;
        sIcosaVerts = new Vector3[]
        {
            new Vector3(-1,  phi, 0), new Vector3( 1,  phi, 0),
            new Vector3(-1, -phi, 0), new Vector3( 1, -phi, 0),
            new Vector3(0, -1,  phi), new Vector3(0,  1,  phi),
            new Vector3(0, -1, -phi), new Vector3(0,  1, -phi),
            new Vector3( phi, 0, -1), new Vector3( phi, 0,  1),
            new Vector3(-phi, 0, -1), new Vector3(-phi, 0,  1),
        };
        for (int i = 0; i < sIcosaVerts.Length; i++)
            sIcosaVerts[i] = sIcosaVerts[i].normalized;
        return sIcosaVerts;
    }

    static readonly int[] sIcosaTris = new int[]
    {
        0,11, 5,  0, 5, 1,  0, 1, 7,  0, 7,10,  0,10,11,
        1, 5, 9,  5,11, 4, 11,10, 2, 10, 7, 6,  7, 1, 8,
        3, 9, 4,  3, 4, 2,  3, 2, 6,  3, 6, 8,  3, 8, 9,
        4, 9, 5,  2, 4,11,  6, 2,10,  8, 6, 7,  9, 8, 1
    };

    static Mesh GetIcosahedronMesh()
    {
        if (sIcosahedronMesh != null) return sIcosahedronMesh;

        sIcosahedronMesh = new Mesh();
        sIcosahedronMesh.name = "BlitzIcosahedron";

        Vector3[] base_ = GetIcosaVerts();
        Vector3[] verts = new Vector3[sIcosaTris.Length];
        int[] tris = new int[sIcosaTris.Length];

        // Per-face expansion for flat shading.
        for (int i = 0; i < sIcosaTris.Length; i++)
        {
            verts[i] = base_[sIcosaTris[i]];
            tris[i] = i;
        }

        sIcosahedronMesh.vertices = verts;
        sIcosahedronMesh.triangles = tris;
        sIcosahedronMesh.RecalculateNormals();
        sIcosahedronMesh.RecalculateBounds();

        return sIcosahedronMesh;
    }

    // ── Spike mesh (cached) — unit pyramid, base 0.3×0.3 at z=0, apex at (0,0,1) ──

    static Mesh GetSpikeMesh()
    {
        if (sSpikeMesh != null) return sSpikeMesh;

        sSpikeMesh = new Mesh();
        sSpikeMesh.name = "BlitzSpike";

        const float bw = 0.15f; // base half-width
        Vector3 bl = new Vector3(-bw, -bw, 0f);
        Vector3 br = new Vector3( bw, -bw, 0f);
        Vector3 tr = new Vector3( bw,  bw, 0f);
        Vector3 tl = new Vector3(-bw,  bw, 0f);
        Vector3 ap = new Vector3(0f, 0f, 1f);

        // 4 side triangles + 2 base triangles (closed), flat-shaded via per-face verts.
        Vector3[] verts = new Vector3[]
        {
            // Sides (apex + two base corners, CCW looking from outside)
            ap, bl, br,
            ap, br, tr,
            ap, tr, tl,
            ap, tl, bl,
            // Base (facing -Z)
            bl, tl, br,
            br, tl, tr,
        };

        int[] tris = new int[verts.Length];
        for (int i = 0; i < tris.Length; i++) tris[i] = i;

        sSpikeMesh.vertices = verts;
        sSpikeMesh.triangles = tris;
        sSpikeMesh.RecalculateNormals();
        sSpikeMesh.RecalculateBounds();

        return sSpikeMesh;
    }

    // ── Octahedron mesh (cached) — used by Gun orb ──

    public static Mesh GetOctahedronMesh()
    {
        if (sOctahedronMesh != null) return sOctahedronMesh;

        sOctahedronMesh = new Mesh();
        sOctahedronMesh.name = "BlitzOctahedron";

        Vector3 top = new Vector3(0f, 0.5f, 0f);
        Vector3 bot = new Vector3(0f, -0.5f, 0f);
        Vector3 fr = new Vector3(0f, 0f, 0.5f);
        Vector3 bk = new Vector3(0f, 0f, -0.5f);
        Vector3 rt = new Vector3(0.5f, 0f, 0f);
        Vector3 lt = new Vector3(-0.5f, 0f, 0f);

        Vector3[] verts = new Vector3[]
        {
            top, fr, rt,
            top, rt, bk,
            top, bk, lt,
            top, lt, fr,
            bot, rt, fr,
            bot, bk, rt,
            bot, lt, bk,
            bot, fr, lt
        };

        int[] tris = new int[verts.Length];
        for (int i = 0; i < tris.Length; i++) tris[i] = i;

        sOctahedronMesh.vertices = verts;
        sOctahedronMesh.triangles = tris;
        sOctahedronMesh.RecalculateNormals();
        sOctahedronMesh.RecalculateBounds();

        return sOctahedronMesh;
    }

    // ── Pyramid mesh (cached) — used by button beacons ──

    static Mesh GetPyramidMesh()
    {
        if (sPyramidMesh != null) return sPyramidMesh;

        sPyramidMesh = new Mesh();
        sPyramidMesh.name = "BlitzPyramid";

        Vector3 bl = new Vector3(-0.5f, -0.5f, -0.5f);
        Vector3 br = new Vector3( 0.5f, -0.5f, -0.5f);
        Vector3 fr = new Vector3( 0.5f, -0.5f,  0.5f);
        Vector3 fl = new Vector3(-0.5f, -0.5f,  0.5f);
        Vector3 ap = new Vector3( 0f,    0.5f,  0f);

        Vector3[] verts = new Vector3[]
        {
            bl, fr, br,
            bl, fl, fr,
            fl, ap, fr,
            fr, ap, br,
            br, ap, bl,
            bl, ap, fl
        };

        int[] tris = new int[verts.Length];
        for (int i = 0; i < tris.Length; i++) tris[i] = i;

        sPyramidMesh.vertices = verts;
        sPyramidMesh.triangles = tris;
        sPyramidMesh.RecalculateNormals();
        sPyramidMesh.RecalculateBounds();

        return sPyramidMesh;
    }
}
