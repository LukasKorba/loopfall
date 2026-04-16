using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Collectible upgrade pickup for Blitz mode.
/// Floating 3D objects above the torus surface — player steers to collect.
/// Three types: Gun (yellow octahedron), Cadency (blue ring), Shield (green sphere).
/// Two dismissal modes: collected (flash + shrink) vs missed (gentle fade + shrink).
/// </summary>
public class BlitzOrb
{
    public enum OrbType { Gun, Cadency, Shield }
    public enum FadeMode { None, Collected, Missed }

    public GameObject mGameObject;
    public float mAngle;
    public float mCrossCenterDeg;
    public OrbType mType;
    public bool mCollected = false;
    public bool mDismissed = false;
    public FadeMode mFadeMode = FadeMode.None;

    GameObject mMeshObj;
    Material mMat;
    float mBaseIntensity;
    float mPulsePhase;
    float mFadeTimer;
    Vector3 mSurface;
    Vector3 mNormal;
    Vector3 mBasePosition;
    float mBaseScale;

    const float COLLECTED_FADE_DURATION = 0.5f;
    const float MISSED_FADE_DURATION = 1.0f;
    const float OBJ_SCALE = 0.1f;
    const float SURFACE_OFFSET = 0.15f;

    static Mesh sRingMesh;

    public BlitzOrb(OrbType type, float crossAngleDeg, Material material)
    {
        mType = type;
        mCrossCenterDeg = crossAngleDeg;
        mPulsePhase = Random.value * Mathf.PI * 2f;
        mBaseScale = OBJ_SCALE;

        float a = crossAngleDeg * Mathf.Deg2Rad;
        float sinA = Mathf.Sin(a);
        float cosA = Mathf.Cos(a);
        mSurface = new Vector3(0f, -10f - sinA, -cosA);
        mNormal = new Vector3(0f, sinA, cosA).normalized;
        mBasePosition = mSurface + mNormal * SURFACE_OFFSET;

        mGameObject = new GameObject("blitzOrb");

        switch (type)
        {
            case OrbType.Gun:
                mMeshObj = new GameObject("OrbGun");
                mMeshObj.AddComponent<MeshFilter>().mesh = BlitzBox.GetOctahedronMesh();
                mMeshObj.AddComponent<MeshRenderer>();
                break;

            case OrbType.Cadency:
                mMeshObj = new GameObject("OrbCadency");
                mMeshObj.AddComponent<MeshFilter>().mesh = GetRingMesh();
                mMeshObj.AddComponent<MeshRenderer>();
                break;

            case OrbType.Shield:
                mMeshObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mMeshObj.name = "OrbShield";
                Object.Destroy(mMeshObj.GetComponent<Collider>());
                break;
        }

        mMeshObj.transform.SetParent(mGameObject.transform, false);
        mMeshObj.transform.localPosition = mBasePosition;
        mMeshObj.transform.localScale = Vector3.one * mBaseScale;

        MeshRenderer mr = mMeshObj.GetComponent<MeshRenderer>();
        mMat = new Material(material);
        mBaseIntensity = material.GetFloat("_Intensity");
        mr.material = mMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    /// <summary>Call each frame for spin + bob + pulse + fade animation.</summary>
    public void Animate(float time)
    {
        if (mGameObject == null || !mGameObject.activeSelf) return;

        if (mFadeMode == FadeMode.Collected)
        {
            mFadeTimer += Time.deltaTime;
            float p = mFadeTimer / COLLECTED_FADE_DURATION;
            if (p >= 1f)
            {
                mGameObject.SetActive(false);
                return;
            }
            float flash = p < 0.15f ? Mathf.Lerp(4f, 1f, p / 0.15f) : 1f;
            float fade = 1f - p * p;
            mMat.SetFloat("_Intensity", mBaseIntensity * fade * flash);
            float scale = mBaseScale * fade;
            mMeshObj.transform.localScale = Vector3.one * scale;
            return;
        }

        if (mFadeMode == FadeMode.Missed)
        {
            mFadeTimer += Time.deltaTime;
            float p = mFadeTimer / MISSED_FADE_DURATION;
            if (p >= 1f)
            {
                mGameObject.SetActive(false);
                return;
            }
            float fade = 1f - p;
            mMat.SetFloat("_Intensity", mBaseIntensity * fade * 0.5f);
            float scale = mBaseScale * fade;
            mMeshObj.transform.localScale = Vector3.one * scale;
            return;
        }

        // Spin
        mMeshObj.transform.localRotation = Quaternion.AngleAxis(time * 50f + mPulsePhase * 30f, Vector3.up);

        // Bob along surface normal
        mMeshObj.transform.localPosition = mBasePosition
            + mNormal * Mathf.Sin(time * 2.5f + mPulsePhase) * 0.015f;

        // Breathing intensity pulse
        float pulse = mBaseIntensity
            + Mathf.Sin(time * 4f + mPulsePhase) * 0.6f
            + Mathf.Sin(time * 7f + mPulsePhase * 1.3f) * 0.3f;
        mMat.SetFloat("_Intensity", pulse);
    }

    public void StartCollectedFade()
    {
        mDismissed = true;
        mCollected = true;
        mFadeMode = FadeMode.Collected;
        mFadeTimer = 0f;
    }

    public void StartMissedFade()
    {
        mDismissed = true;
        mFadeMode = FadeMode.Missed;
        mFadeTimer = 0f;
    }

    /// <summary>World position of the floating object (for collection + spark origin).</summary>
    public Vector3 GetWorldCenter()
    {
        if (mMeshObj == null) return Vector3.zero;
        return mMeshObj.transform.position;
    }

    // ── Ring/torus mesh for Cadency orb (cached, created once) ──

    static Mesh GetRingMesh()
    {
        if (sRingMesh != null) return sRingMesh;

        sRingMesh = new Mesh();
        sRingMesh.name = "BlitzOrbRing";

        const int ringSegs = 16;
        const int tubeSegs = 6;
        const float majorR = 0.5f;
        const float minorR = 0.15f;

        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris = new List<int>();

        // Generate vertices
        for (int i = 0; i <= ringSegs; i++)
        {
            float ringAngle = (float)i / ringSegs * Mathf.PI * 2f;
            float cx = Mathf.Cos(ringAngle) * majorR;
            float cz = Mathf.Sin(ringAngle) * majorR;
            Vector3 ringCenter = new Vector3(cx, 0f, cz);
            Vector3 ringDir = new Vector3(Mathf.Cos(ringAngle), 0f, Mathf.Sin(ringAngle));

            for (int j = 0; j <= tubeSegs; j++)
            {
                float tubeAngle = (float)j / tubeSegs * Mathf.PI * 2f;
                Vector3 tubeOffset = ringDir * (Mathf.Cos(tubeAngle) * minorR)
                                   + Vector3.up * (Mathf.Sin(tubeAngle) * minorR);
                verts.Add(ringCenter + tubeOffset);
                norms.Add(tubeOffset.normalized);
            }
        }

        // Generate triangles
        int tubeVerts = tubeSegs + 1;
        for (int i = 0; i < ringSegs; i++)
        {
            for (int j = 0; j < tubeSegs; j++)
            {
                int a = i * tubeVerts + j;
                int b = a + tubeVerts;
                int c = a + 1;
                int d = b + 1;

                tris.Add(a); tris.Add(b); tris.Add(c);
                tris.Add(c); tris.Add(b); tris.Add(d);
            }
        }

        sRingMesh.vertices = verts.ToArray();
        sRingMesh.normals = norms.ToArray();
        sRingMesh.triangles = tris.ToArray();
        sRingMesh.RecalculateBounds();

        return sRingMesh;
    }
}
