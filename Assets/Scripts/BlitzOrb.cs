using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Collectible orb for Blitz mode upgrade tracks.
/// Short arc strip on torus inner surface — player steers to collect.
/// Three types power three upgrade tracks (Gun, Cadency, Shield).
/// Uses TrailGlow (additive) for semi-transparent glow.
/// </summary>
public class BlitzOrb
{
    public enum OrbType { Gun, Cadency, Shield }

    public GameObject mGameObject;
    public float mAngle;
    public OrbType mType;
    public bool mCollected = false;
    public bool mFading = false;

    Material mMat;
    float mBaseIntensity;
    float mPulsePhase;
    float mFadeTimer;

    const float FADE_DURATION = 1.2f;
    const float STRIP_WIDTH = 0.25f;
    const float SURFACE_OFFSET = 0.03f;
    const float ARC_HALF_SPAN = 20f; // degrees — 40° total arc

    public BlitzOrb(OrbType type, float crossAngleDeg, float obstacleStepInv, Material material)
    {
        mType = type;
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

    /// <summary>Call each frame for breathing glow + fade-out animation.</summary>
    public void Animate(float time)
    {
        if (mGameObject == null || !mGameObject.activeSelf) return;

        if (mFading)
        {
            mFadeTimer += Time.deltaTime;
            float p = mFadeTimer / FADE_DURATION;
            if (p >= 1f)
            {
                mGameObject.SetActive(false);
                return;
            }
            // Bright flash at start, then smooth decay
            float flash = p < 0.15f ? Mathf.Lerp(3f, 1f, p / 0.15f) : 1f;
            float fade = 1f - p * p; // quadratic — stays visible longer, drops off at end
            mMat.SetFloat("_Intensity", mBaseIntensity * fade * flash);
            mGameObject.transform.localScale = Vector3.one * (1f + p * 2f); // expand 3x over duration
            return;
        }

        // Breathing pulse — layered sine for organic feel
        float pulse = mBaseIntensity
            + Mathf.Sin(time * 4f + mPulsePhase) * 0.6f
            + Mathf.Sin(time * 7f + mPulsePhase * 1.3f) * 0.3f;
        mMat.SetFloat("_Intensity", pulse);
    }

    /// <summary>Start fade-out (called on collection).</summary>
    public void StartFade()
    {
        mFading = true;
        mFadeTimer = 0f;
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
