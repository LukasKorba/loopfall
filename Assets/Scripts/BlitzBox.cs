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

    const float SIZE_1HP = 0.14f;
    const float SIZE_2HP = 0.19f;
    const float SIZE_3HP = 0.24f;

    static Mesh sPyramidMesh;

    public BlitzBox(float crossAngleDeg, Material boxMat, int hitPoints = 1, bool isButton = false)
    {
        mDestroyed = false;
        mHitPoints = hitPoints;
        mMaxHitPoints = hitPoints;
        mIsButton = isButton;

        float a = crossAngleDeg * Mathf.Deg2Rad;
        float sinA = Mathf.Sin(a);
        float cosA = Mathf.Cos(a);

        mSurface = new Vector3(0f, -10f - sinA, -cosA);
        mNormal = new Vector3(0f, sinA, cosA).normalized;

        float size = SizeForHP(hitPoints);
        Vector3 center = mSurface + mNormal * (size * 0.5f);

        mGameObject = new GameObject("BlitzBoxRoot");

        if (isButton)
        {
            mCube = new GameObject("BlitzBox");
            mCube.AddComponent<MeshFilter>().mesh = GetPyramidMesh();
            mCube.AddComponent<MeshRenderer>();
            mCube.AddComponent<BoxCollider>();
        }
        else
        {
            mCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mCube.name = "BlitzBox";
        }

        mCube.transform.parent = mGameObject.transform;
        mCube.transform.localPosition = center;
        mCube.transform.localScale = Vector3.one * size;
        mCube.transform.localRotation = Quaternion.LookRotation(Vector3.right, mNormal);
        mCube.layer = LAYER;

        MeshRenderer mr = mCube.GetComponent<MeshRenderer>();
        mMat = new Material(boxMat);
        mr.material = mMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = true;

        ApplyVisuals();
    }

    static float SizeForHP(int hp)
    {
        return hp >= 3 ? SIZE_3HP : hp == 2 ? SIZE_2HP : SIZE_1HP;
    }

    void ApplyVisuals()
    {
        float size = SizeForHP(mHitPoints);
        Vector3 center = mSurface + mNormal * (size * 0.5f);
        mCube.transform.localPosition = center;
        mCube.transform.localScale = Vector3.one * size;

        float intensity = mHitPoints >= 3 ? 3.0f : mHitPoints == 2 ? 2.0f : 1.5f;
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

        ApplyVisuals();
        return false;
    }

    /// <summary>Destroy with fragment burst.</summary>
    public void DestroyBox()
    {
        if (mDestroyed) return;
        mDestroyed = true;

        Vector3 pos = mCube.transform.position;

        for (int i = 0; i < 6; i++)
        {
            // Fragments are always cubes (small enough nobody notices)
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
            rb.linearVelocity = Random.insideUnitSphere * 2.5f;
            rb.angularVelocity = Random.insideUnitSphere * 12f;

            Object.Destroy(frag, 0.5f);
        }

        mGameObject.SetActive(false);
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
