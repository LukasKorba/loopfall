using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Electric gate obstacle for Blitz mode.
/// Shimmering energy arcs around the torus cross-section with an optional gap.
/// Visual: layered LineRenderers (bright core + dim halo) tracing the arc.
/// Collision: invisible BoxColliders spaced along the arc.
/// Can be linked to a button (BlitzBox pyramid) — destroying it deactivates the gate.
/// </summary>
public class BlitzGate
{
    public GameObject mGameObject;
    public float mAngle;
    public bool mActive;
    public bool mDestroyed;
    public BlitzBox mButton;

    struct ArcSegment
    {
        public LineRenderer main;
        public LineRenderer halo;
        public Vector3[] surfaces;
        public Vector3[] normals;
    }

    List<ArcSegment> mArcs;
    List<GameObject> mColliders;
    Material mArcMainMat;
    Material mArcHaloMat;
    Material mConnectionMat;
    float mPulsePhase;
    float mDeactivateTimer;

    // Connection line (button → gate)
    LineRenderer mConnectionLine;
    GameObject mConnectionObj;

    const float ARC_STEP = 3f;
    const float ARC_RADIUS = 0.04f;
    const float COL_SPACING = 12f;
    const float COL_LENGTH = 0.15f;
    const float COL_HEIGHT = 0.12f;
    const float COL_WIDTH = 0.15f;
    const float CROSS_MIN = 25f;
    const float CROSS_MAX = 155f;

    public BlitzGate(float gapCenterDeg, float gapSizeDeg, Material gateMat, Material connectionMat)
    {
        mActive = true;
        mDestroyed = false;
        mPulsePhase = Random.value * Mathf.PI * 2f;
        mConnectionMat = connectionMat;

        mGameObject = new GameObject("BlitzGateRoot");
        mColliders = new List<GameObject>();
        mArcs = new List<ArcSegment>();

        // Arc materials — additive glow
        mArcMainMat = new Material(connectionMat);
        mArcMainMat.SetColor("_Color", new Color(0.7f, 0.4f, 1.0f));
        mArcMainMat.SetFloat("_Intensity", 4.0f);

        mArcHaloMat = new Material(connectionMat);
        mArcHaloMat.SetColor("_Color", new Color(0.3f, 0.1f, 0.6f));
        mArcHaloMat.SetFloat("_Intensity", 1.5f);

        // Build arc ranges (split by gap)
        float gapMin = gapCenterDeg - gapSizeDeg * 0.5f;
        float gapMax = gapCenterDeg + gapSizeDeg * 0.5f;

        List<Vector2> ranges = new List<Vector2>();
        if (gapSizeDeg > 0f)
        {
            if (gapMin > CROSS_MIN) ranges.Add(new Vector2(CROSS_MIN, gapMin));
            if (gapMax < CROSS_MAX) ranges.Add(new Vector2(gapMax, CROSS_MAX));
        }
        else
        {
            ranges.Add(new Vector2(CROSS_MIN, CROSS_MAX));
        }

        foreach (var range in ranges)
            CreateArcSegment(range.x, range.y);

        // Invisible collision cubes along the arc
        for (float a = CROSS_MIN; a <= CROSS_MAX; a += COL_SPACING)
        {
            if (gapSizeDeg > 0f && a >= gapMin && a <= gapMax) continue;
            CreateCollider(a);
        }
    }

    void CreateArcSegment(float startDeg, float endDeg)
    {
        List<Vector3> surfaces = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();

        for (float a = startDeg; a <= endDeg + 0.1f; a += ARC_STEP)
        {
            float deg = Mathf.Min(a, endDeg);
            float rad = deg * Mathf.Deg2Rad;
            float sinA = Mathf.Sin(rad);
            float cosA = Mathf.Cos(rad);
            surfaces.Add(new Vector3(0f, -10f - sinA, -cosA));
            normals.Add(new Vector3(0f, sinA, cosA).normalized);
            if (deg >= endDeg) break;
        }

        int count = surfaces.Count;
        if (count < 2) return;

        LineRenderer main = CreateLine("BlitzArcMain", count, mArcMainMat, 0.035f);
        LineRenderer halo = CreateLine("BlitzArcHalo", count, mArcHaloMat, 0.08f);

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = surfaces[i] + normals[i] * ARC_RADIUS;
            main.SetPosition(i, pos);
            halo.SetPosition(i, pos);
        }

        ArcSegment seg;
        seg.main = main;
        seg.halo = halo;
        seg.surfaces = surfaces.ToArray();
        seg.normals = normals.ToArray();
        mArcs.Add(seg);
    }

    LineRenderer CreateLine(string name, int points, Material mat, float width)
    {
        GameObject obj = new GameObject(name);
        obj.transform.parent = mGameObject.transform;
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.material = mat;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.positionCount = points;
        lr.useWorldSpace = false;
        lr.receiveShadows = false;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.numCapVertices = 3;
        lr.numCornerVertices = 3;

        return lr;
    }

    void CreateCollider(float crossAngleDeg)
    {
        float a = crossAngleDeg * Mathf.Deg2Rad;
        float sinA = Mathf.Sin(a);
        float cosA = Mathf.Cos(a);

        Vector3 surface = new Vector3(0f, -10f - sinA, -cosA);
        Vector3 normal = new Vector3(0f, sinA, cosA).normalized;
        Vector3 center = surface + normal * (COL_HEIGHT * 0.5f);

        GameObject col = new GameObject("BlitzGateBar");
        col.transform.parent = mGameObject.transform;
        col.transform.localPosition = center;
        col.transform.localScale = new Vector3(COL_LENGTH, COL_HEIGHT, COL_WIDTH);
        col.transform.localRotation = Quaternion.LookRotation(Vector3.right, normal);
        col.AddComponent<BoxCollider>();

        mColliders.Add(col);
    }

    /// <summary>Link a button — destroying it deactivates this gate.</summary>
    public void LinkButton(BlitzBox button)
    {
        mButton = button;
        button.mLinkedGate = this;

        // Thick visible connection line
        mConnectionObj = new GameObject("BlitzConnection");
        mConnectionObj.transform.parent = mGameObject.transform;
        mConnectionLine = mConnectionObj.AddComponent<LineRenderer>();
        Material cMat = new Material(mConnectionMat);
        cMat.SetColor("_Color", new Color(0.7f, 0.35f, 1.0f));
        cMat.SetFloat("_Intensity", 3.5f);
        mConnectionLine.material = cMat;
        mConnectionLine.startWidth = 0.035f;
        mConnectionLine.endWidth = 0.02f;
        mConnectionLine.positionCount = 8;
        mConnectionLine.useWorldSpace = true;
        mConnectionLine.receiveShadows = false;
        mConnectionLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mConnectionLine.numCapVertices = 3;
    }

    /// <summary>Disable gate: colliders off, start fade-out.</summary>
    public void Deactivate()
    {
        mActive = false;
        foreach (var col in mColliders)
        {
            BoxCollider bc = col.GetComponent<BoxCollider>();
            if (bc != null) bc.enabled = false;
        }
        if (mConnectionObj != null) mConnectionObj.SetActive(false);
    }

    /// <summary>Call each frame for shimmer animation and fade-out.</summary>
    public void Animate(float time)
    {
        if (mDestroyed) return;

        if (!mActive)
        {
            mDeactivateTimer += Time.deltaTime;
            float fade = 1f - mDeactivateTimer / 0.3f;
            if (fade <= 0f)
            {
                mDestroyed = true;
                mGameObject.SetActive(false);
            }
            else
            {
                mArcMainMat.SetFloat("_Intensity", fade * 4f);
                mArcHaloMat.SetFloat("_Intensity", fade * 1.5f);
                foreach (var seg in mArcs)
                {
                    seg.main.startWidth = 0.035f * fade;
                    seg.main.endWidth = 0.035f * fade;
                    seg.halo.startWidth = 0.08f * fade;
                    seg.halo.endWidth = 0.08f * fade;
                }
            }
            return;
        }

        // Intensity shimmer — layered sine waves
        float pulse = 3.0f + Mathf.Sin(time * 8f + mPulsePhase) * 1.5f
                    + Mathf.Sin(time * 13f + mPulsePhase * 0.7f) * 0.5f;
        mArcMainMat.SetFloat("_Intensity", pulse);
        float haloPulse = 1.2f + Mathf.Sin(time * 6f + mPulsePhase) * 0.4f;
        mArcHaloMat.SetFloat("_Intensity", haloPulse);

        // Width shimmer on main arc
        float mainW = 0.035f + Mathf.Sin(time * 10f + mPulsePhase) * 0.008f;
        float haloW = 0.08f + Mathf.Sin(time * 7f + mPulsePhase) * 0.015f;

        // Animate arc positions with electric jitter
        foreach (var seg in mArcs)
        {
            seg.main.startWidth = mainW;
            seg.main.endWidth = mainW;
            seg.halo.startWidth = haloW;
            seg.halo.endWidth = haloW;

            for (int i = 0; i < seg.surfaces.Length; i++)
            {
                // Per-point shimmer: deterministic noise via layered sine
                float n = Mathf.Sin(time * 20f + i * 1.7f) * 0.01f
                        + Mathf.Sin(time * 35f + i * 2.3f) * 0.005f;
                Vector3 pos = seg.surfaces[i] + seg.normals[i] * (ARC_RADIUS + n);
                seg.main.SetPosition(i, pos);
                seg.halo.SetPosition(i, pos);
            }
        }

        // Animate connection line with electric jitter
        if (mConnectionLine != null && mButton != null && !mButton.mDestroyed
            && mButton.mCube != null && mButton.mCube.activeInHierarchy)
        {
            Vector3 start = mButton.mCube.transform.position;
            // Find arc midpoint as target
            Vector3 end = start;
            if (mArcs.Count > 0 && mArcs[0].surfaces.Length > 0)
            {
                var seg = mArcs[0];
                int mid = seg.surfaces.Length / 2;
                end = mGameObject.transform.TransformPoint(
                    seg.surfaces[mid] + seg.normals[mid] * ARC_RADIUS);
            }

            int pts = mConnectionLine.positionCount;
            mConnectionLine.SetPosition(0, start);
            mConnectionLine.SetPosition(pts - 1, end);

            for (int i = 1; i < pts - 1; i++)
            {
                float t = (float)i / (pts - 1);
                Vector3 mid = Vector3.Lerp(start, end, t);
                mid += Random.insideUnitSphere * 0.06f;
                mConnectionLine.SetPosition(i, mid);
            }

            // Pulse width
            float w = 0.03f + Mathf.Sin(time * 15f) * 0.01f;
            mConnectionLine.startWidth = w;
            mConnectionLine.endWidth = w * 0.6f;
        }
    }
}
