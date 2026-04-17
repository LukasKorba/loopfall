using UnityEngine;

/// <summary>
/// Shimmering trail hazard for Blitz mode.
/// Runs along the tube at a fixed cross-section angle (default 90° = bottom, where
/// the ball naturally rests). Passing through = death — player must swing off-center
/// to clear it. Punishes passive bottom-sitting independent of beam behavior.
///
/// Visual: twin LineRenderers (bright core + soft halo) tracing a curved arc on the
/// inner tube surface, using the same TrailGlow shader as ball trails and gate arcs.
/// Collision: chain of thin box colliders named "BlitzDividerBar".
/// </summary>
public class BlitzDivider
{
    public GameObject mGameObject;
    public float mAngle;
    public bool mDestroyed;

    LineRenderer mCoreLine;
    LineRenderer mHaloLine;
    Material mCoreMat;
    Material mHaloMat;
    Vector3[] mPoints;
    float mPulsePhase;
    float mSpanDeg;
    float mCrossDeg;

    const int SEGMENTS = 12;
    const float SURFACE_OFFSET = 0.06f;
    const float BAR_THICKNESS = 0.06f;
    const float CORE_WIDTH = 0.04f;
    const float HALO_WIDTH = 0.10f;

    public BlitzDivider(float crossAngleDeg, float spanDeg, Material trailMat)
    {
        mCrossDeg = crossAngleDeg;
        mSpanDeg = spanDeg;
        mPulsePhase = Random.value * Mathf.PI * 2f;
        mDestroyed = false;

        mGameObject = new GameObject("BlitzDivider");

        // Teal electric palette — distinct from purple gates, red full-gates, and orb hues.
        mCoreMat = new Material(trailMat);
        mCoreMat.SetColor("_Color", new Color(0.2f, 1.0f, 0.85f));
        mCoreMat.SetFloat("_Intensity", 3.5f);

        mHaloMat = new Material(trailMat);
        mHaloMat.SetColor("_Color", new Color(0.05f, 0.5f, 0.4f));
        mHaloMat.SetFloat("_Intensity", 1.3f);

        BuildGeometry();
    }

    void BuildGeometry()
    {
        float crossRad = mCrossDeg * Mathf.Deg2Rad;
        float sinC = Mathf.Sin(crossRad);
        float cosC = Mathf.Cos(crossRad);
        float r = 10f + sinC;

        // Local points trace an arc on the tube surface from phi=0 to phi=spanDeg.
        // After the object is Rotate'd by mAngle around Z, these land at major-angles mAngle..mAngle+span.
        mPoints = new Vector3[SEGMENTS + 1];
        for (int i = 0; i <= SEGMENTS; i++)
        {
            float phi = (i / (float)SEGMENTS) * mSpanDeg * Mathf.Deg2Rad;
            float sinP = Mathf.Sin(phi);
            float cosP = Mathf.Cos(phi);
            Vector3 surface = new Vector3(r * sinP, -r * cosP, -cosC);
            Vector3 normal = new Vector3(-sinC * sinP, sinC * cosP, cosC);
            mPoints[i] = surface + normal * SURFACE_OFFSET;
        }

        mCoreLine = BuildLine("DividerCore", mCoreMat, CORE_WIDTH);
        mHaloLine = BuildLine("DividerHalo", mHaloMat, HALO_WIDTH);

        // Colliders — one thin box per segment, name triggers death in Sphere.OnCollisionEnter.
        for (int i = 0; i < mPoints.Length - 1; i++)
        {
            Vector3 a = mPoints[i];
            Vector3 b = mPoints[i + 1];
            Vector3 mid = (a + b) * 0.5f;
            Vector3 diff = b - a;
            float len = diff.magnitude;

            GameObject col = new GameObject("BlitzDividerBar");
            col.transform.parent = mGameObject.transform;
            col.transform.localPosition = mid;
            col.transform.localRotation = Quaternion.LookRotation(diff.normalized, Vector3.up);
            BoxCollider bc = col.AddComponent<BoxCollider>();
            bc.size = new Vector3(BAR_THICKNESS, BAR_THICKNESS, len);
        }
    }

    LineRenderer BuildLine(string name, Material mat, float width)
    {
        GameObject obj = new GameObject(name);
        obj.transform.parent = mGameObject.transform;
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.material = mat;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.positionCount = mPoints.Length;
        lr.useWorldSpace = false;
        lr.receiveShadows = false;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.numCapVertices = 3;
        for (int i = 0; i < mPoints.Length; i++)
            lr.SetPosition(i, mPoints[i]);
        return lr;
    }

    public void Animate(float time)
    {
        if (mDestroyed) return;

        float pulse = 3.0f + Mathf.Sin(time * 8f + mPulsePhase) * 1.2f
                           + Mathf.Sin(time * 13f + mPulsePhase * 0.7f) * 0.4f;
        mCoreMat.SetFloat("_Intensity", pulse);
        mHaloMat.SetFloat("_Intensity", 1.1f + Mathf.Sin(time * 6f + mPulsePhase) * 0.3f);

        float coreW = CORE_WIDTH + Mathf.Sin(time * 10f + mPulsePhase) * 0.008f;
        float haloW = HALO_WIDTH + Mathf.Sin(time * 7f + mPulsePhase) * 0.015f;
        mCoreLine.startWidth = coreW;
        mCoreLine.endWidth = coreW;
        mHaloLine.startWidth = haloW;
        mHaloLine.endWidth = haloW;
    }
}
