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
    List<MeshRenderer> mEndpoints;
    Material mArcMainMat;
    Material mArcHaloMat;
    Material mConnectionMat;
    float mPulsePhase;
    float mDeactivateTimer;

    // Connection strands (button → gate). One strand per button HP — each hit peels one.
    // Strands fan out from the button and attach at different points along the gate arc.
    LineRenderer[] mConnectionLines;
    GameObject[] mConnectionObjs;
    Vector3[] mStrandLocalEnds;
    int mStrandsRemaining;
    bool mIsFullGate;
    const int CONNECTION_STRANDS = 3;

    const float ARC_STEP = 3f;
    const float ARC_RADIUS = 0.04f;
    const float COL_SPACING = 12f;
    const float COL_LENGTH = 0.08f;
    const float COL_HEIGHT = 0.06f;
    const float COL_WIDTH = 0.06f;
    const float ENDPOINT_SIZE = 0.07f;
    const float CROSS_MIN = 25f;
    const float CROSS_MAX = 155f;

    /// <summary>Gap-based gate: arc covers full cross-section minus the gap.
    /// Pass isFullGate=true for the red must-destroy variant (gapSizeDeg == 0).</summary>
    public BlitzGate(float gapCenterDeg, float gapSizeDeg, Material gateMat, Material connectionMat, bool isFullGate = false)
    {
        mIsFullGate = isFullGate;
        InitMaterials(connectionMat);

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

        for (float a = CROSS_MIN; a <= CROSS_MAX; a += COL_SPACING)
        {
            if (gapSizeDeg > 0f && a >= gapMin && a <= gapMax) continue;
            CreateCollider(a);
        }
    }

    /// <summary>Half-gate: arc covers only fromDeg–toDeg range.</summary>
    public BlitzGate(float fromDeg, float toDeg, bool halfGate, Material gateMat, Material connectionMat)
    {
        InitMaterials(connectionMat);

        CreateArcSegment(fromDeg, toDeg);

        for (float a = fromDeg; a <= toDeg; a += COL_SPACING)
            CreateCollider(a);
    }

    void InitMaterials(Material connectionMat)
    {
        mActive = true;
        mDestroyed = false;
        mPulsePhase = Random.value * Mathf.PI * 2f;
        mConnectionMat = connectionMat;

        mGameObject = new GameObject("BlitzGateRoot");
        mColliders = new List<GameObject>();
        mArcs = new List<ArcSegment>();
        mEndpoints = new List<MeshRenderer>();

        // Red palette for full (must-destroy) gates so the player can tell them apart at distance.
        Color mainColor = mIsFullGate ? new Color(1.0f, 0.12f, 0.18f) : new Color(0.7f, 0.4f, 1.0f);
        Color haloColor = mIsFullGate ? new Color(0.7f, 0.03f, 0.08f) : new Color(0.3f, 0.1f, 0.6f);

        mArcMainMat = new Material(connectionMat);
        mArcMainMat.SetColor("_Color", mainColor);
        mArcMainMat.SetFloat("_Intensity", 4.0f);

        mArcHaloMat = new Material(connectionMat);
        mArcHaloMat.SetColor("_Color", haloColor);
        mArcHaloMat.SetFloat("_Intensity", 1.5f);
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

        // Endpoint cubes at arc start and end
        CreateEndpoint(surfaces[0], normals[0]);
        CreateEndpoint(surfaces[count - 1], normals[count - 1]);

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

    void CreateEndpoint(Vector3 surface, Vector3 normal)
    {
        GameObject ep = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ep.name = "BlitzGateEnd";
        Object.Destroy(ep.GetComponent<Collider>());
        ep.transform.parent = mGameObject.transform;
        ep.transform.localPosition = surface + normal * ARC_RADIUS;
        ep.transform.localScale = Vector3.one * ENDPOINT_SIZE;
        ep.transform.localRotation = Quaternion.LookRotation(Vector3.right, normal);

        MeshRenderer mr = ep.GetComponent<MeshRenderer>();
        Material mat = new Material(mArcMainMat);
        mr.material = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mEndpoints.Add(mr);
    }

    /// <summary>Link a button — destroying it deactivates this gate.
    /// For full gates, tints the button red and creates 3 separate strands (one peels per button hit).</summary>
    public void LinkButton(BlitzBox button)
    {
        mButton = button;
        button.mLinkedGate = this;

        // Red-flag the button so the player knows it gates a must-destroy barrier.
        if (mIsFullGate)
            button.SetTint(new Color(1.0f, 0.15f, 0.22f), new Color(0.9f, 0.1f, 0.18f));

        mStrandsRemaining = CONNECTION_STRANDS;
        mConnectionLines = new LineRenderer[CONNECTION_STRANDS];
        mConnectionObjs = new GameObject[CONNECTION_STRANDS];
        mStrandLocalEnds = new Vector3[CONNECTION_STRANDS];

        // Cache one attachment point per strand along the primary arc segment (local space).
        // For 3 strands this lands them at ~25%, 50%, 75% of the arc — a visible fan.
        if (mArcs.Count > 0 && mArcs[0].surfaces.Length > 0)
        {
            var seg = mArcs[0];
            int count = seg.surfaces.Length;
            for (int s = 0; s < CONNECTION_STRANDS; s++)
            {
                float t = (s + 1f) / (CONNECTION_STRANDS + 1f);
                int idx = Mathf.Clamp(Mathf.RoundToInt(t * (count - 1)), 0, count - 1);
                mStrandLocalEnds[s] = seg.surfaces[idx] + seg.normals[idx] * ARC_RADIUS;
            }
        }

        Color strandColor = mIsFullGate ? new Color(1.0f, 0.15f, 0.22f) : new Color(0.7f, 0.35f, 1.0f);

        for (int s = 0; s < CONNECTION_STRANDS; s++)
        {
            GameObject obj = new GameObject("BlitzConnection_" + s);
            obj.transform.parent = mGameObject.transform;
            LineRenderer lr = obj.AddComponent<LineRenderer>();
            Material cMat = new Material(mConnectionMat);
            cMat.SetColor("_Color", strandColor);
            cMat.SetFloat("_Intensity", 3.5f);
            lr.material = cMat;
            lr.startWidth = 0.028f;
            lr.endWidth = 0.018f;
            lr.positionCount = 8;
            lr.useWorldSpace = true;
            lr.receiveShadows = false;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.numCapVertices = 3;

            mConnectionObjs[s] = obj;
            mConnectionLines[s] = lr;
        }
    }

    /// <summary>Peel one connection strand — called each time the linked button is hit but not destroyed.
    /// Removal order is outer-in (0, 2, 1) so the final remaining strand sits dead-center.</summary>
    public void RemoveStrand()
    {
        if (mConnectionObjs == null || mStrandsRemaining <= 0) return;

        int[] order = { 0, 2, 1 };
        int idx = order[CONNECTION_STRANDS - mStrandsRemaining];
        if (idx < mConnectionObjs.Length && mConnectionObjs[idx] != null)
            mConnectionObjs[idx].SetActive(false);

        mStrandsRemaining--;
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
        if (mConnectionObjs != null)
        {
            for (int s = 0; s < mConnectionObjs.Length; s++)
                if (mConnectionObjs[s] != null) mConnectionObjs[s].SetActive(false);
        }
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
                float epScale = ENDPOINT_SIZE * fade;
                foreach (var mr in mEndpoints)
                    mr.transform.localScale = Vector3.one * epScale;
            }
            return;
        }

        // Intensity shimmer — layered sine waves
        float pulse = 3.0f + Mathf.Sin(time * 8f + mPulsePhase) * 1.5f
                    + Mathf.Sin(time * 13f + mPulsePhase * 0.7f) * 0.5f;
        mArcMainMat.SetFloat("_Intensity", pulse);
        float haloPulse = 1.2f + Mathf.Sin(time * 6f + mPulsePhase) * 0.4f;
        mArcHaloMat.SetFloat("_Intensity", haloPulse);

        // Endpoint cubes pulse with the arc
        foreach (var mr in mEndpoints)
            mr.material.SetFloat("_Intensity", pulse);

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

        // Animate each surviving strand — shared start at the button, different end per strand.
        if (mConnectionLines != null && mButton != null && !mButton.mDestroyed
            && mButton.mCube != null && mButton.mCube.activeInHierarchy)
        {
            Vector3 start = mButton.mCube.transform.position;

            for (int s = 0; s < mConnectionLines.Length; s++)
            {
                LineRenderer lr = mConnectionLines[s];
                GameObject obj = mConnectionObjs[s];
                if (lr == null || obj == null || !obj.activeSelf) continue;

                Vector3 end = mGameObject.transform.TransformPoint(mStrandLocalEnds[s]);

                int pts = lr.positionCount;
                lr.SetPosition(0, start);
                lr.SetPosition(pts - 1, end);

                for (int i = 1; i < pts - 1; i++)
                {
                    float t = (float)i / (pts - 1);
                    Vector3 mid = Vector3.Lerp(start, end, t);
                    mid += Random.insideUnitSphere * 0.035f;
                    lr.SetPosition(i, mid);
                }

                float w = 0.024f + Mathf.Sin(time * 15f + s * 1.3f) * 0.008f;
                lr.startWidth = w;
                lr.endWidth = w * 0.7f;
            }
        }
    }
}
