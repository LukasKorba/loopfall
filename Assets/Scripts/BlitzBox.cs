using UnityEngine;

/// <summary>
/// Shootable target for Blitz mode.
/// Regular boxes = cubes (shoot for points).
/// Buttons = pyramids (shoot to open linked gate).
/// Supports multi-hit (3HP shrinks on each hit before breaking).
/// </summary>
public class BlitzBox
{
    public const int LAYER = 8; // dedicated physics layer for beam detection

    public GameObject mGameObject;
    public GameObject mCube;        // "mCube" name kept for compatibility — may be pyramid
    public float mAngle;
    public bool mDestroyed;
    public int mHitPoints;
    public int mMaxHitPoints;
    public BlitzGate mLinkedGate;

    Material mMat;
    Vector3 mSurface;
    Vector3 mNormal;
    bool mIsButton;

    // Animation state
    Quaternion mBaseRotation;
    Vector3 mBasePosition;
    float mPulsePhase;

    // Shield ring (3HP sentinel)
    LineRenderer mRingLine;
    GameObject mRingObj;
    Material mRingMat;

    const float SIZE_1HP = 0.14f;
    const float SIZE_2HP = 0.19f;
    const float SIZE_3HP = 0.24f;
    const int RING_SEGMENTS = 24;
    const float RING_WIDTH = 0.015f;

    static Mesh sPyramidMesh;
    static Mesh sOctahedronMesh;

    public BlitzBox(float crossAngleDeg, Material boxMat, int hitPoints = 1,
        bool isButton = false, Material ringMat = null)
    {
        mDestroyed = false;
        mHitPoints = hitPoints;
        mMaxHitPoints = hitPoints;
        mIsButton = isButton;
        mPulsePhase = Random.value * Mathf.PI * 2f;

        float a = crossAngleDeg * Mathf.Deg2Rad;
        float sinA = Mathf.Sin(a);
        float cosA = Mathf.Cos(a);

        mSurface = new Vector3(0f, -10f - sinA, -cosA);
        mNormal = new Vector3(0f, sinA, cosA).normalized;

        float size = SizeForHP(hitPoints);
        Vector3 center = mSurface + mNormal * (size * 0.75f);

        mGameObject = new GameObject("BlitzBoxRoot");

        if (isButton)
        {
            mCube = new GameObject("BlitzBox");
            mCube.AddComponent<MeshFilter>().mesh = GetPyramidMesh();
            mCube.AddComponent<MeshRenderer>();
            mCube.AddComponent<BoxCollider>();
        }
        else if (hitPoints == 1)
        {
            // 1HP — small static cube. Silhouette distinct from Gun orb's octahedron.
            mCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mCube.name = "BlitzBox";
        }
        else
        {
            // 3HP Sentinel — cube
            mCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mCube.name = "BlitzBox";
        }

        mCube.transform.parent = mGameObject.transform;
        mCube.transform.localPosition = center;
        mCube.transform.localScale = Vector3.one * size;

        // Base rotation: 3HP sentinel tilted 45° for diamond stance
        Quaternion look = Quaternion.LookRotation(Vector3.right, mNormal);
        if (!isButton && hitPoints >= 3)
            mBaseRotation = look * Quaternion.AngleAxis(45f, Vector3.forward);
        else
            mBaseRotation = look;
        mCube.transform.localRotation = mBaseRotation;
        mBasePosition = center;
        mCube.layer = LAYER;

        MeshRenderer mr = mCube.GetComponent<MeshRenderer>();
        mMat = new Material(boxMat);
        mr.material = mMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = true;

        // Shield ring for 3HP sentinel
        if (hitPoints >= 3 && !isButton && ringMat != null)
            CreateShieldRing(ringMat, size);

        ApplyVisuals();
    }

    static float SizeForHP(int hp)
    {
        return hp >= 3 ? SIZE_3HP : hp == 2 ? SIZE_2HP : SIZE_1HP;
    }

    void ApplyVisuals()
    {
        float size = SizeForHP(mHitPoints);
        Vector3 center = mSurface + mNormal * (size * 0.75f);
        mCube.transform.localPosition = center;
        mCube.transform.localScale = Vector3.one * size;
        mBasePosition = center;

        float intensity = mHitPoints >= 3 ? 3.0f : mHitPoints == 2 ? 2.0f : 1.5f;
        mMat.SetFloat("_EmissionIntensity", intensity);

        // Shield ring damage states
        if (mRingLine != null)
        {
            if (mHitPoints <= 1)
            {
                // Ring gone — cube exposed, emission shifts warm red
                mRingLine.enabled = false;
                mMat.SetColor("_EmissionColor", new Color(0.9f, 0.25f, 0.1f));
            }
            else if (mHitPoints == 2)
            {
                // Ring dims
                mRingMat.SetFloat("_Intensity", 1.5f);
            }
        }
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

        ApplyVisuals();
        return false;
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
            // Beacon: fast spin + emission pulse
            mCube.transform.localRotation = mBaseRotation * Quaternion.AngleAxis(time * 60f, Vector3.up);
            float pulse = 2.0f + Mathf.Sin(time * 6f + mPulsePhase) * 0.8f
                        + Mathf.Sin(time * 10f + mPulsePhase * 0.7f) * 0.3f;
            mMat.SetFloat("_EmissionIntensity", pulse);
        }
        else if (mMaxHitPoints >= 3)
        {
            // Sentinel: slow spin + ring orbit
            mCube.transform.localRotation = mBaseRotation * Quaternion.AngleAxis(time * 25f, Vector3.up);

            if (mRingObj != null && mRingLine != null && mRingLine.enabled)
            {
                // Orbit the ring around the normal axis
                mRingObj.transform.localRotation = Quaternion.AngleAxis(time * 40f, Vector3.up);

                // Flicker at 2HP
                if (mHitPoints == 2)
                {
                    float flicker = 1.5f + Mathf.Sin(time * 25f + mPulsePhase) * 1.0f;
                    mRingMat.SetFloat("_Intensity", Mathf.Max(0.3f, flicker));
                }
            }
        }
        // 1HP static cube — no animation, identity is clarity
    }

    void CreateShieldRing(Material ringMat, float cubeSize)
    {
        float radius = cubeSize * 0.6f;
        mRingObj = new GameObject("BlitzShieldRing");
        mRingObj.transform.parent = mCube.transform;
        mRingObj.transform.localPosition = Vector3.zero;
        mRingObj.transform.localRotation = Quaternion.identity;

        mRingLine = mRingObj.AddComponent<LineRenderer>();
        mRingMat = new Material(ringMat);
        mRingLine.material = mRingMat;
        mRingLine.startWidth = RING_WIDTH;
        mRingLine.endWidth = RING_WIDTH;
        mRingLine.positionCount = RING_SEGMENTS + 1;
        mRingLine.useWorldSpace = false;
        mRingLine.loop = false;
        mRingLine.receiveShadows = false;
        mRingLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mRingLine.numCapVertices = 2;

        for (int i = 0; i <= RING_SEGMENTS; i++)
        {
            float angle = (float)i / RING_SEGMENTS * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            mRingLine.SetPosition(i, new Vector3(x, 0f, z));
        }
    }

    // ── Octahedron mesh (cached, created once) ──

    public static Mesh GetOctahedronMesh()
    {
        if (sOctahedronMesh != null) return sOctahedronMesh;

        sOctahedronMesh = new Mesh();
        sOctahedronMesh.name = "BlitzOctahedron";

        // 6 vertices defining the octahedron corners
        Vector3 top = new Vector3(0f, 0.5f, 0f);
        Vector3 bot = new Vector3(0f, -0.5f, 0f);
        Vector3 fr = new Vector3(0f, 0f, 0.5f);
        Vector3 bk = new Vector3(0f, 0f, -0.5f);
        Vector3 rt = new Vector3(0.5f, 0f, 0f);
        Vector3 lt = new Vector3(-0.5f, 0f, 0f);

        // 8 faces, 3 vertices each (24 total) for flat shading
        Vector3[] verts = new Vector3[]
        {
            // Upper 4 faces
            top, fr, rt,
            top, rt, bk,
            top, bk, lt,
            top, lt, fr,
            // Lower 4 faces
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

    // ── Pyramid mesh (cached, created once) ──

    static Mesh GetPyramidMesh()
    {
        if (sPyramidMesh != null) return sPyramidMesh;

        sPyramidMesh = new Mesh();
        sPyramidMesh.name = "BlitzPyramid";

        // Square-based pyramid: base at y=-0.5, apex at y=0.5
        // Each face has its own vertices for flat-shaded normals
        Vector3 bl = new Vector3(-0.5f, -0.5f, -0.5f);
        Vector3 br = new Vector3( 0.5f, -0.5f, -0.5f);
        Vector3 fr = new Vector3( 0.5f, -0.5f,  0.5f);
        Vector3 fl = new Vector3(-0.5f, -0.5f,  0.5f);
        Vector3 ap = new Vector3( 0f,    0.5f,  0f);

        Vector3[] verts = new Vector3[]
        {
            // Base (2 tris)
            bl, fr, br,
            bl, fl, fr,
            // Front face
            fl, ap, fr,
            // Right face
            fr, ap, br,
            // Back face
            br, ap, bl,
            // Left face
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
