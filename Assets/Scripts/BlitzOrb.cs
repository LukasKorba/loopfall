using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Collectible orb for Blitz mode upgrade tracks.
/// Short arc strip on torus inner surface — player steers to collect.
/// Three types power three upgrade tracks (Gun, Cadency, Shield).
/// Two dismissal modes: collected (flash + spark) vs missed (gentle fade).
/// </summary>
public class BlitzOrb
{
    public enum OrbType { Gun, Cadency, Shield }
    public enum FadeMode { None, Collected, Missed }

    public GameObject mGameObject;
    public float mAngle;
    public float mCrossCenterDeg; // cross-section center for position check
    public OrbType mType;
    public bool mCollected = false;
    public bool mDismissed = false; // true once any fade starts
    public FadeMode mFadeMode = FadeMode.None;

    Material mMat;
    float mBaseIntensity;
    float mPulsePhase;
    float mFadeTimer;

    const float COLLECTED_FADE_DURATION = 0.8f;
    const float MISSED_FADE_DURATION = 1.5f;
    const float STRIP_WIDTH = 0.25f;
    const float SURFACE_OFFSET = 0.03f;
    public const float ARC_HALF_SPAN = 20f; // degrees — 40° total arc

    public BlitzOrb(OrbType type, float crossAngleDeg, float obstacleStepInv, Material material)
    {
        mType = type;
        mCrossCenterDeg = crossAngleDeg;
        mPulsePhase = Random.value * Mathf.PI * 2f;
        mGameObject = new GameObject("blitzOrb");

        MeshFilter mf = mGameObject.AddComponent<MeshFilter>();
        mf.mesh = GenerateArcMesh(crossAngleDeg, obstacleStepInv);

        MeshRenderer mr = mGameObject.AddComponent<MeshRenderer>();
        mMat = new Material(material);
        mBaseIntensity = material.GetFloat("_Intensity");
        mr.material = mMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    /// <summary>Call each frame for breathing glow + fade animation.</summary>
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
            // Bright flash at start, then smooth decay (intensity only, no scale)
            float flash = p < 0.15f ? Mathf.Lerp(4f, 1f, p / 0.15f) : 1f;
            float fade = 1f - p * p;
            mMat.SetFloat("_Intensity", mBaseIntensity * fade * flash);
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
            // Gentle linear fade — no flash, just dims away
            float fade = 1f - p;
            mMat.SetFloat("_Intensity", mBaseIntensity * fade * 0.5f);
            return;
        }

        // Breathing pulse — layered sine for organic feel
        float pulse = mBaseIntensity
            + Mathf.Sin(time * 4f + mPulsePhase) * 0.6f
            + Mathf.Sin(time * 7f + mPulsePhase * 1.3f) * 0.3f;
        mMat.SetFloat("_Intensity", pulse);
    }

    /// <summary>Start collected fade (flash + decay).</summary>
    public void StartCollectedFade()
    {
        mDismissed = true;
        mCollected = true;
        mFadeMode = FadeMode.Collected;
        mFadeTimer = 0f;
    }

    /// <summary>Start missed fade (gentle dim-out).</summary>
    public void StartMissedFade()
    {
        mDismissed = true;
        mFadeMode = FadeMode.Missed;
        mFadeTimer = 0f;
    }

    /// <summary>World position of the arc center (for spark effect origin).</summary>
    public Vector3 GetWorldCenter()
    {
        if (mGameObject == null) return Vector3.zero;
        float a = mCrossCenterDeg * Mathf.Deg2Rad;
        Vector3 local = new Vector3(0f, -10f - Mathf.Sin(a), -Mathf.Cos(a));
        return mGameObject.transform.TransformPoint(local);
    }

    Mesh GenerateArcMesh(float centerDeg, float obstacleStepInv)
    {
        float fromDeg = Mathf.Max(centerDeg - ARC_HALF_SPAN, 8f);
        float toDeg = Mathf.Min(centerDeg + ARC_HALF_SPAN, 172f);

        float fromAngle = fromDeg * Mathf.Deg2Rad;
        float toAngle = toDeg * Mathf.Deg2Rad;
        float range = toAngle - fromAngle;
        int steps = Mathf.Max((int)(range * obstacleStepInv), 6) + 1;
        float stepSize = range / (steps - 1);
        float halfWidth = STRIP_WIDTH * 0.5f;

        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris = new List<int>();

        for (int i = 0; i < steps; i++)
        {
            float a = fromAngle + i * stepSize;
            float y = Mathf.Sin(a);
            float z = Mathf.Cos(a);

            Vector3 surface = new Vector3(0f, -10f - y, -z);
            Vector3 normal = new Vector3(0f, y, z).normalized;
            Vector3 offset = normal * SURFACE_OFFSET;

            verts.Add(surface + offset + new Vector3(-halfWidth, 0f, 0f));
            verts.Add(surface + offset + new Vector3(halfWidth, 0f, 0f));
            norms.Add(normal);
            norms.Add(normal);

            if (i > 0)
            {
                int idx = i * 2;
                tris.Add(idx - 2); tris.Add(idx - 1); tris.Add(idx);
                tris.Add(idx); tris.Add(idx - 1); tris.Add(idx + 1);
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "blitzOrbArc";
        mesh.vertices = verts.ToArray();
        mesh.normals = norms.ToArray();
        mesh.triangles = tris.ToArray();
        return mesh;
    }
}
