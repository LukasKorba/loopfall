using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spline-extruded track that replaces the fixed torus.
/// A Catmull-Rom spline defines the path; a U-shaped cross-section is extruded along it.
/// The ball moves forward through the track; gates are placed at spline distances.
///
/// Coordinate system:
///   - Spline extends primarily along +Z (forward / into screen)
///   - Y is world up
///   - Cross-section U-profile opens upward (ball sits at bottom)
///
/// Architecture matches the original torus:
///   - Flat-shaded double-sided mesh (inner face has grid emission)
///   - Zero-friction MeshCollider
///   - Gates are thin walls with gaps, placed at spline distances
///   - Rolling visibility window hides distant geometry
/// </summary>
public class SplineTrack : MonoBehaviour
{
    // ── SPLINE ───────────────────────────────────────────────

    /// <summary>
    /// A single control point on the path. Position defines the track centerline;
    /// bankAngle tilts the cross-section for banked turns (0 = level).
    /// </summary>
    public struct ControlPoint
    {
        public Vector3 position;
        public float bankAngle; // degrees, positive = bank right

        public ControlPoint(Vector3 pos, float bank = 0f)
        {
            position = pos;
            bankAngle = bank;
        }
    }

    private List<ControlPoint> controlPoints = new List<ControlPoint>();

    // ── CROSS-SECTION ────────────────────────────────────────

    // U-profile: bottom half of a circle (180°), matching the torus minor radius.
    // minorRadius=1.0 and 16 segments across the half-circle.
    const float MINOR_RADIUS = 1.0f;
    const int PROFILE_SEGMENTS = 16;

    // ── MESH GENERATION ──────────────────────────────────────

    // How many mesh rings to generate per unit of spline arc length.
    // Higher = smoother track at the cost of more triangles.
    const float RINGS_PER_UNIT = 3f;

    // Minimum distance between sample rings (prevents degenerate triangles on tight curves)
    const float MIN_RING_SPACING = 0.15f;

    // Generated mesh data — rebuilt when track changes
    private Mesh trackMesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    // ── TRACK SEGMENTS ───────────────────────────────────────

    // The track is divided into segments for streaming (generate ahead, remove behind).
    // Each segment spans several control points and has its own mesh.
    const float SEGMENT_LENGTH = 30f; // world units per segment
    const int VISIBLE_SEGMENTS_AHEAD = 4;
    const int VISIBLE_SEGMENTS_BEHIND = 1;

    private List<TrackSegment> segments = new List<TrackSegment>();
    private float ballDistance = 0f; // how far the ball has traveled along the spline

    struct TrackSegment
    {
        public GameObject gameObject;
        public MeshFilter meshFilter;
        public MeshRenderer renderer;
        public MeshCollider collider;
        public float startDist; // arc-length distance where this segment starts
        public float endDist;   // arc-length distance where this segment ends
    }

    // ── PATH SPLITS ──────────────────────────────────────────

    public struct SplitZone
    {
        public float startDist;       // where the split begins
        public float endDist;         // where branches merge back
        public float maxSeparation;   // peak lateral offset
        public GameObject leftMeshObj;
        public GameObject rightMeshObj;
    }

    private List<SplitZone> splits = new List<SplitZone>();

    const float SPLIT_MIN_INTERVAL = 80f;   // minimum distance between splits
    const float SPLIT_MAX_INTERVAL = 120f;
    const float SPLIT_LENGTH = 40f;         // how long each split zone is
    const float SPLIT_SEPARATION = 2.5f;    // max lateral offset per branch
    const float SPLIT_FIRST_AT = 60f;       // earliest split distance
    float nextSplitAt = -1f;                // distance where next split starts

    /// <summary>
    /// Compute the lateral offset at a given distance within a split zone.
    /// Returns 0 outside the zone, smooth sine ramp inside.
    /// </summary>
    public float GetSplitOffset(SplitZone zone, float dist)
    {
        if (dist < zone.startDist || dist > zone.endDist) return 0f;
        float t = (dist - zone.startDist) / (zone.endDist - zone.startDist);
        return zone.maxSeparation * Mathf.Sin(Mathf.PI * t);
    }

    /// <summary>Check if a distance falls within any split zone.</summary>
    public bool IsInSplit(float dist, out SplitZone zone)
    {
        for (int i = 0; i < splits.Count; i++)
        {
            if (dist >= splits[i].startDist && dist <= splits[i].endDist)
            {
                zone = splits[i];
                return true;
            }
        }
        zone = default;
        return false;
    }

    /// <summary>Get all split zones.</summary>
    public List<SplitZone> Splits { get { return splits; } }

    /// <summary>
    /// Generate split zones along the track at semi-random intervals.
    /// Called during path extension.
    /// </summary>
    public void GenerateSplits(float fromDist, float toDist)
    {
        if (nextSplitAt < 0f)
            nextSplitAt = SPLIT_FIRST_AT + Random.Range(0f, 20f);

        while (nextSplitAt + SPLIT_LENGTH < toDist)
        {
            if (nextSplitAt >= fromDist)
            {
                SplitZone zone;
                zone.startDist = nextSplitAt;
                zone.endDist = nextSplitAt + SPLIT_LENGTH;
                zone.maxSeparation = SPLIT_SEPARATION;
                zone.leftMeshObj = null;
                zone.rightMeshObj = null;
                splits.Add(zone);
            }
            nextSplitAt += SPLIT_LENGTH + Random.Range(SPLIT_MIN_INTERVAL, SPLIT_MAX_INTERVAL);
        }
    }

    /// <summary>
    /// Generate a branch track mesh for one side of a split zone.
    /// The branch follows the main spline but offset laterally.
    /// sign: +1 = left branch, -1 = right branch.
    /// </summary>
    public Mesh GenerateBranchMesh(SplitZone zone, float sign)
    {
        Vector3[] profile = GenerateProfile();

        float length = zone.endDist - zone.startDist;
        int ringCount = Mathf.Max(2, Mathf.CeilToInt(length * RINGS_PER_UNIT));

        Vector3[] ringPositions = new Vector3[ringCount];
        Vector3[] ringNormals = new Vector3[ringCount];
        Vector3[] ringBinormals = new Vector3[ringCount];

        for (int r = 0; r < ringCount; r++)
        {
            float d = zone.startDist + length * ((float)r / (ringCount - 1));
            Vector3 pos, tangent, normal, binormal;
            EvaluateFrame(d, out pos, out tangent, out normal, out binormal);

            // Apply lateral offset
            float offset = GetSplitOffset(zone, d) * sign;
            ringPositions[r] = pos + binormal * offset;
            ringNormals[r] = normal;
            ringBinormals[r] = binormal;
        }

        // Build mesh — same structure as GenerateTrackMesh
        int quadsPerRing = PROFILE_SEGMENTS;
        int totalQuads = quadsPerRing * (ringCount - 1);
        int vertsPerSide = totalQuads * 6;
        int totalVerts = vertsPerSide * 2;

        Vector3[] vertices = new Vector3[totalVerts];
        Vector3[] normals = new Vector3[totalVerts];
        Vector2[] uvs = new Vector2[totalVerts];
        int[] trisOuter = new int[vertsPerSide];
        int[] trisInner = new int[vertsPerSide];

        int vi = 0;
        int outerTri = 0;
        int innerTri = 0;

        for (int r = 0; r < ringCount - 1; r++)
        {
            float uStart = (float)r / (ringCount - 1);
            float uEnd = (float)(r + 1) / (ringCount - 1);

            for (int p = 0; p < quadsPerRing; p++)
            {
                float vStart = (float)p / quadsPerRing;
                float vEnd = (float)(p + 1) / quadsPerRing;

                Vector3 p00 = TransformProfilePoint(profile[p], ringPositions[r],
                    ringNormals[r], ringBinormals[r]);
                Vector3 p10 = TransformProfilePoint(profile[p + 1], ringPositions[r],
                    ringNormals[r], ringBinormals[r]);
                Vector3 p01 = TransformProfilePoint(profile[p], ringPositions[r + 1],
                    ringNormals[r + 1], ringBinormals[r + 1]);
                Vector3 p11 = TransformProfilePoint(profile[p + 1], ringPositions[r + 1],
                    ringNormals[r + 1], ringBinormals[r + 1]);

                Vector3 faceNormal = Vector3.Cross(p10 - p00, p01 - p00).normalized;

                Vector2 uv00 = new Vector2(uStart, vStart);
                Vector2 uv10 = new Vector2(uStart, vEnd);
                Vector2 uv01 = new Vector2(uEnd, vStart);
                Vector2 uv11 = new Vector2(uEnd, vEnd);

                // Outer face
                vertices[vi] = p00; normals[vi] = faceNormal; uvs[vi] = uv00; trisOuter[outerTri++] = vi++;
                vertices[vi] = p01; normals[vi] = faceNormal; uvs[vi] = uv01; trisOuter[outerTri++] = vi++;
                vertices[vi] = p10; normals[vi] = faceNormal; uvs[vi] = uv10; trisOuter[outerTri++] = vi++;
                vertices[vi] = p10; normals[vi] = faceNormal; uvs[vi] = uv10; trisOuter[outerTri++] = vi++;
                vertices[vi] = p01; normals[vi] = faceNormal; uvs[vi] = uv01; trisOuter[outerTri++] = vi++;
                vertices[vi] = p11; normals[vi] = faceNormal; uvs[vi] = uv11; trisOuter[outerTri++] = vi++;

                // Inner face
                Vector3 innerNormal = -faceNormal;
                vertices[vi] = p00; normals[vi] = innerNormal; uvs[vi] = uv00; trisInner[innerTri++] = vi++;
                vertices[vi] = p10; normals[vi] = innerNormal; uvs[vi] = uv10; trisInner[innerTri++] = vi++;
                vertices[vi] = p01; normals[vi] = innerNormal; uvs[vi] = uv01; trisInner[innerTri++] = vi++;
                vertices[vi] = p10; normals[vi] = innerNormal; uvs[vi] = uv10; trisInner[innerTri++] = vi++;
                vertices[vi] = p11; normals[vi] = innerNormal; uvs[vi] = uv11; trisInner[innerTri++] = vi++;
                vertices[vi] = p01; normals[vi] = innerNormal; uvs[vi] = uv01; trisInner[innerTri++] = vi++;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "SplineBranch";
        if (totalVerts > 65535)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.subMeshCount = 2;
        mesh.SetTriangles(trisOuter, 0);
        mesh.SetTriangles(trisInner, 1);
        mesh.RecalculateBounds();

        return mesh;
    }

    // ── OBSTACLES ────────────────────────────────────────────

    const float GATE_SPACING = 5.0f; // world units between gates (≈ 10° on old torus)

    public struct Gate
    {
        public GameObject gameObject;
        public float distance;    // position along spline (arc length)
        public float gapCenter;   // 0-180° on cross-section (0=left, 90=center, 180=right)
        public int gapHalfWidth;  // degrees
    }

    private List<Gate> gates = new List<Gate>();
    private int currentGateIndex = -1;
    private int score = 0;

    // ── SPLINE EVALUATION ────────────────────────────────────

    /// <summary>
    /// Evaluate a Catmull-Rom spline at parameter t (0..1) between points p1 and p2,
    /// using p0 and p3 as tangent guides. Alpha=0.5 gives centripetal parameterization
    /// which avoids cusps and self-intersections.
    /// </summary>
    public static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        // Standard cubic Catmull-Rom
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    /// <summary>
    /// Evaluate the derivative (tangent) of the Catmull-Rom spline at parameter t.
    /// </summary>
    public static Vector3 CatmullRomDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;

        return 0.5f * (
            (-p0 + p2) +
            (4f * p0 - 10f * p1 + 8f * p2 - 2f * p3) * t +
            (-3f * p0 + 9f * p1 - 9f * p2 + 3f * p3) * t2
        );
    }

    /// <summary>
    /// Evaluate position on the full spline at a given arc-length distance.
    /// Uses the precomputed arc-length table for fast lookup.
    /// </summary>
    public Vector3 EvaluatePosition(float distance)
    {
        if (controlPoints.Count < 4) return Vector3.zero;

        // Convert arc-length distance to segment index + local t
        float t;
        int segIdx;
        DistanceToParam(distance, out segIdx, out t);

        // Must match the arc-length table indexing: segment seg uses [seg, seg+1, seg+2, seg+3]
        int i0 = segIdx;
        int i1 = segIdx + 1;
        int i2 = segIdx + 2;
        int i3 = Mathf.Min(segIdx + 3, controlPoints.Count - 1);

        return CatmullRom(
            controlPoints[i0].position,
            controlPoints[i1].position,
            controlPoints[i2].position,
            controlPoints[i3].position,
            t
        );
    }

    /// <summary>
    /// Evaluate the tangent (forward direction) at a given arc-length distance.
    /// </summary>
    public Vector3 EvaluateTangent(float distance)
    {
        if (controlPoints.Count < 4) return Vector3.forward;

        float t;
        int segIdx;
        DistanceToParam(distance, out segIdx, out t);

        // Must match the arc-length table indexing: segment seg uses [seg, seg+1, seg+2, seg+3]
        int i0 = segIdx;
        int i1 = segIdx + 1;
        int i2 = segIdx + 2;
        int i3 = Mathf.Min(segIdx + 3, controlPoints.Count - 1);

        return CatmullRomDerivative(
            controlPoints[i0].position,
            controlPoints[i1].position,
            controlPoints[i2].position,
            controlPoints[i3].position,
            t
        ).normalized;
    }

    /// <summary>
    /// Compute the orientation frame (tangent, normal, binormal) at a spline distance.
    /// Uses the "up hint" approach with bank angle interpolation for stable frames.
    /// </summary>
    public void EvaluateFrame(float distance, out Vector3 position, out Vector3 tangent,
                              out Vector3 normal, out Vector3 binormal)
    {
        position = EvaluatePosition(distance);
        tangent = EvaluateTangent(distance);

        // Interpolate bank angle
        float bank = EvaluateBankAngle(distance);

        // Start with world up, then apply banking
        Vector3 up = Quaternion.AngleAxis(bank, tangent) * Vector3.up;

        // Gram-Schmidt: make up perpendicular to tangent
        binormal = Vector3.Cross(tangent, up).normalized;
        normal = Vector3.Cross(binormal, tangent).normalized;
    }

    // ── ARC-LENGTH PARAMETERIZATION ──────────────────────────

    // Maps arc-length distances to spline parameters for constant-speed evaluation.
    // Without this, moving along the spline at equal parameter increments would
    // cause speed variation (bunching on tight curves, stretching on straight).

    private List<float> arcLengthTable = new List<float>(); // cumulative arc length at each sample
    private const int ARC_SAMPLES_PER_SEGMENT = 32; // samples between each pair of control points

    /// <summary>
    /// Rebuild the arc-length lookup table. Call after adding/removing control points.
    /// </summary>
    public void RebuildArcLengthTable()
    {
        arcLengthTable.Clear();
        arcLengthTable.Add(0f);

        if (controlPoints.Count < 4) return;

        float cumulative = 0f;
        int segments = controlPoints.Count - 3; // evaluable segments (need 4 points per eval)

        for (int seg = 0; seg < segments; seg++)
        {
            int i0 = seg;
            int i1 = seg + 1;
            int i2 = seg + 2;
            int i3 = seg + 3;

            Vector3 prev = controlPoints[i1].position;

            for (int s = 1; s <= ARC_SAMPLES_PER_SEGMENT; s++)
            {
                float t = (float)s / ARC_SAMPLES_PER_SEGMENT;
                Vector3 curr = CatmullRom(
                    controlPoints[i0].position,
                    controlPoints[i1].position,
                    controlPoints[i2].position,
                    controlPoints[i3].position,
                    t
                );
                cumulative += Vector3.Distance(prev, curr);
                arcLengthTable.Add(cumulative);
                prev = curr;
            }
        }
    }

    /// <summary>Total arc length of the current spline.</summary>
    public float TotalLength
    {
        get { return arcLengthTable.Count > 0 ? arcLengthTable[arcLengthTable.Count - 1] : 0f; }
    }

    /// <summary>
    /// Convert an arc-length distance to a segment index + local parameter t.
    /// Binary search on the arc-length table.
    /// </summary>
    void DistanceToParam(float distance, out int segmentIndex, out float t)
    {
        distance = Mathf.Clamp(distance, 0f, TotalLength);

        // Binary search for the interval containing this distance
        int lo = 0, hi = arcLengthTable.Count - 1;
        while (lo < hi - 1)
        {
            int mid = (lo + hi) / 2;
            if (arcLengthTable[mid] <= distance)
                lo = mid;
            else
                hi = mid;
        }

        // lo is the table index just before (or at) the target distance
        float dLo = arcLengthTable[lo];
        float dHi = arcLengthTable[hi];
        float frac = (dHi > dLo) ? (distance - dLo) / (dHi - dLo) : 0f;

        // Convert table index to segment + local t
        // Table has ARC_SAMPLES_PER_SEGMENT entries per segment, starting at index 0
        float globalSample = lo + frac;
        segmentIndex = Mathf.FloorToInt(globalSample / ARC_SAMPLES_PER_SEGMENT);
        segmentIndex = Mathf.Min(segmentIndex, Mathf.Max(0, controlPoints.Count - 4));
        float localSample = globalSample - segmentIndex * ARC_SAMPLES_PER_SEGMENT;
        t = localSample / ARC_SAMPLES_PER_SEGMENT;
        t = Mathf.Clamp01(t);
    }

    /// <summary>Interpolate bank angle at a given spline distance.</summary>
    float EvaluateBankAngle(float distance)
    {
        if (controlPoints.Count < 4) return 0f;

        float t;
        int segIdx;
        DistanceToParam(distance, out segIdx, out t);

        int i1 = segIdx + 1;
        int i2 = Mathf.Min(segIdx + 2, controlPoints.Count - 1);

        return Mathf.Lerp(controlPoints[i1].bankAngle, controlPoints[i2].bankAngle, t);
    }

    // ── MESH GENERATION ──────────────────────────────────────

    /// <summary>
    /// Generate the U-profile vertices for one cross-section ring.
    /// Returns PROFILE_SEGMENTS+1 positions in local space (relative to the ring center).
    /// The profile spans 180° (bottom half of circle), opening upward.
    /// </summary>
    Vector3[] GenerateProfile()
    {
        Vector3[] profile = new Vector3[PROFILE_SEGMENTS + 1];

        for (int j = 0; j <= PROFILE_SEGMENTS; j++)
        {
            // Angle from 180° (left edge) through 270° (bottom) to 360° (right edge)
            // This sweeps the bottom semicircle — U-shape opening upward
            float angle = Mathf.PI + Mathf.PI * ((float)j / PROFILE_SEGMENTS);
            float x = Mathf.Cos(angle) * MINOR_RADIUS;
            float y = Mathf.Sin(angle) * MINOR_RADIUS;
            profile[j] = new Vector3(x, y, 0f);
        }

        return profile;
    }

    /// <summary>
    /// Generate a track mesh between two arc-length distances.
    /// Produces a flat-shaded, double-sided mesh compatible with the TrackGrid shader.
    /// </summary>
    public Mesh GenerateTrackMesh(float startDist, float endDist)
    {
        Vector3[] profile = GenerateProfile();

        float length = endDist - startDist;
        int ringCount = Mathf.Max(2, Mathf.CeilToInt(length * RINGS_PER_UNIT));

        // Sample the spline at each ring
        Vector3[] ringPositions = new Vector3[ringCount];
        Vector3[] ringTangents = new Vector3[ringCount];
        Vector3[] ringNormals = new Vector3[ringCount];
        Vector3[] ringBinormals = new Vector3[ringCount];

        for (int r = 0; r < ringCount; r++)
        {
            float d = startDist + length * ((float)r / (ringCount - 1));
            EvaluateFrame(d, out ringPositions[r], out ringTangents[r],
                         out ringNormals[r], out ringBinormals[r]);
        }

        // Build mesh: for each quad (between two rings, two profile points),
        // emit 2 triangles × 2 sides (inner + outer) = 12 vertices per quad.
        // Flat-shaded: no vertex sharing between faces.
        int quadsPerRing = PROFILE_SEGMENTS;
        int totalQuads = quadsPerRing * (ringCount - 1);

        // Each quad = 2 sides × 2 triangles × 3 verts = 12 verts
        // But we split into 2 submeshes (outer=0, inner=1) like the torus
        int vertsPerSide = totalQuads * 6; // 2 tris × 3 verts per quad per side
        int totalVerts = vertsPerSide * 2;

        Vector3[] vertices = new Vector3[totalVerts];
        Vector3[] normals = new Vector3[totalVerts];
        Vector2[] uvs = new Vector2[totalVerts];
        int[] trisOuter = new int[vertsPerSide]; // submesh 0: outer (no grid)
        int[] trisInner = new int[vertsPerSide]; // submesh 1: inner (grid emission)

        int vi = 0; // vertex index
        int outerTri = 0;
        int innerTri = 0;

        for (int r = 0; r < ringCount - 1; r++)
        {
            float uStart = (float)r / (ringCount - 1);
            float uEnd = (float)(r + 1) / (ringCount - 1);

            for (int p = 0; p < quadsPerRing; p++)
            {
                float vStart = (float)p / quadsPerRing;
                float vEnd = (float)(p + 1) / quadsPerRing;

                // Four corners of this quad in world space
                Vector3 p00 = TransformProfilePoint(profile[p], ringPositions[r],
                    ringNormals[r], ringBinormals[r]);
                Vector3 p10 = TransformProfilePoint(profile[p + 1], ringPositions[r],
                    ringNormals[r], ringBinormals[r]);
                Vector3 p01 = TransformProfilePoint(profile[p], ringPositions[r + 1],
                    ringNormals[r + 1], ringBinormals[r + 1]);
                Vector3 p11 = TransformProfilePoint(profile[p + 1], ringPositions[r + 1],
                    ringNormals[r + 1], ringBinormals[r + 1]);

                // Face normal (flat shading)
                Vector3 faceNormal = Vector3.Cross(p10 - p00, p01 - p00).normalized;

                // ── Outer face (outward normal, no grid emission) ──
                // UV alpha channel = 0 signals "suppress grid" to TrackGrid shader
                Vector2 uv00 = new Vector2(uStart, vStart);
                Vector2 uv10 = new Vector2(uStart, vEnd);
                Vector2 uv01 = new Vector2(uEnd, vStart);
                Vector2 uv11 = new Vector2(uEnd, vEnd);

                // Tri 1: p00, p01, p10
                vertices[vi] = p00; normals[vi] = faceNormal; uvs[vi] = uv00; trisOuter[outerTri++] = vi++;
                vertices[vi] = p01; normals[vi] = faceNormal; uvs[vi] = uv01; trisOuter[outerTri++] = vi++;
                vertices[vi] = p10; normals[vi] = faceNormal; uvs[vi] = uv10; trisOuter[outerTri++] = vi++;
                // Tri 2: p10, p01, p11
                vertices[vi] = p10; normals[vi] = faceNormal; uvs[vi] = uv10; trisOuter[outerTri++] = vi++;
                vertices[vi] = p01; normals[vi] = faceNormal; uvs[vi] = uv01; trisOuter[outerTri++] = vi++;
                vertices[vi] = p11; normals[vi] = faceNormal; uvs[vi] = uv11; trisOuter[outerTri++] = vi++;

                // ── Inner face (inward normal, grid emission active) ──
                // Reversed winding + inverted normal
                Vector3 innerNormal = -faceNormal;

                // Tri 1: p00, p10, p01 (reversed winding)
                vertices[vi] = p00; normals[vi] = innerNormal; uvs[vi] = uv00; trisInner[innerTri++] = vi++;
                vertices[vi] = p10; normals[vi] = innerNormal; uvs[vi] = uv10; trisInner[innerTri++] = vi++;
                vertices[vi] = p01; normals[vi] = innerNormal; uvs[vi] = uv01; trisInner[innerTri++] = vi++;
                // Tri 2: p10, p11, p01
                vertices[vi] = p10; normals[vi] = innerNormal; uvs[vi] = uv10; trisInner[innerTri++] = vi++;
                vertices[vi] = p11; normals[vi] = innerNormal; uvs[vi] = uv11; trisInner[innerTri++] = vi++;
                vertices[vi] = p01; normals[vi] = innerNormal; uvs[vi] = uv01; trisInner[innerTri++] = vi++;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "SplineTrack";

        // Large meshes may exceed 16-bit index limit
        if (totalVerts > 65535)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.subMeshCount = 2;
        mesh.SetTriangles(trisOuter, 0);
        mesh.SetTriangles(trisInner, 1);
        mesh.RecalculateBounds();

        return mesh;
    }

    /// <summary>
    /// Transform a local profile point into world space using the ring's frame.
    /// profile.x maps to binormal (left/right), profile.y maps to normal (up/down).
    /// </summary>
    Vector3 TransformProfilePoint(Vector3 profilePt, Vector3 ringPos,
                                   Vector3 ringNormal, Vector3 ringBinormal)
    {
        return ringPos + ringBinormal * profilePt.x + ringNormal * profilePt.y;
    }

    // ── PATH GENERATION ──────────────────────────────────────

    /// <summary>
    /// Generate an initial straight-ish track for testing.
    /// Creates control points along +Z with gentle curves.
    /// </summary>
    public void GenerateTestPath(float length)
    {
        controlPoints.Clear();

        float spacing = 5f; // distance between control points
        int count = Mathf.CeilToInt(length / spacing) + 3; // +3 for Catmull-Rom padding

        for (int i = 0; i < count; i++)
        {
            float z = i * spacing;

            // Gentle sine-wave curves
            float x = Mathf.Sin(z * 0.05f) * 4f;
            float y = 0f; // flat for debugging — restore -z * 0.15f for downhill slope

            controlPoints.Add(new ControlPoint(new Vector3(x, y, z)));
        }

        RebuildArcLengthTable();
    }

    /// <summary>
    /// Generate a procedural track segment extending the path forward.
    /// Called as the ball progresses to keep the track ahead populated.
    /// </summary>
    public void ExtendPath(float additionalLength)
    {
        if (controlPoints.Count < 2) return;

        float spacing = 5f;
        int newPoints = Mathf.CeilToInt(additionalLength / spacing);

        ControlPoint last = controlPoints[controlPoints.Count - 1];
        ControlPoint prev = controlPoints[controlPoints.Count - 2];

        // Extrapolate direction from last two points
        Vector3 dir = (last.position - prev.position).normalized;

        for (int i = 0; i < newPoints; i++)
        {
            // Add curvature: smooth random turns
            float turnX = (Mathf.PerlinNoise(last.position.z * 0.02f, 0f) - 0.5f) * 3f;
            float turnY = (Mathf.PerlinNoise(0f, last.position.z * 0.02f) - 0.5f) * 1.5f;

            Vector3 newPos = last.position + dir * spacing;
            newPos.x += turnX;
            newPos.y += turnY - spacing * 0.15f; // maintain downhill slope

            float bank = turnX * 3f; // bank into turns

            ControlPoint newPt = new ControlPoint(newPos, bank);
            controlPoints.Add(newPt);

            dir = (newPt.position - last.position).normalized;
            last = newPt;
        }

        RebuildArcLengthTable();
    }

    // ── GATE GENERATION ──────────────────────────────────────

    /// <summary>
    /// Generate gates along the track at regular intervals.
    /// Gap placement follows the same logic as the torus: random position on the
    /// cross-section (0-180°) with minimum wall guarantee on both sides.
    /// </summary>
    public void GenerateGates(float fromDist, float toDist)
    {
        // Find the first gate position after fromDist
        float firstGate = Mathf.Ceil(fromDist / GATE_SPACING) * GATE_SPACING;
        if (firstGate < GATE_SPACING * 2f) firstGate = GATE_SPACING * 2f; // buffer at start

        for (float d = firstGate; d < toDist; d += GATE_SPACING)
        {
            int gapHalf = Random.Range(15, 22); // degrees, matching torus gap range

            // Gap center: 0-180° cross-section, with wall margin
            int originMin = gapHalf + 3;
            int originMax = 180 - gapHalf - 2;
            float gapCenter = Random.Range(originMin, originMax + 1);

            // Avoid center for early gates (forces commitment left or right)
            if (d < GATE_SPACING * 5f)
            {
                if (gapCenter > 65 && gapCenter < 115)
                    gapCenter = Random.value < 0.5f
                        ? Random.Range(originMin, 66)
                        : Random.Range(115, originMax + 1);
            }

            Gate gate = new Gate
            {
                distance = d,
                gapCenter = gapCenter,
                gapHalfWidth = gapHalf,
            };

            gates.Add(gate);
        }
    }

    // ── BALL POSITION TRACKING ───────────────────────────────

    /// <summary>
    /// Given the ball's world position, find the closest point on the spline
    /// and return the arc-length distance. Uses iterative refinement.
    /// </summary>
    public float FindClosestDistance(Vector3 worldPos)
    {
        if (arcLengthTable.Count < 2) return 0f;

        // Coarse search: check every few units
        float bestDist = ballDistance; // start from last known position
        float bestSqr = float.MaxValue;
        float searchRadius = 10f;
        float step = 0.5f;

        float searchStart = Mathf.Max(0f, bestDist - searchRadius);
        float searchEnd = Mathf.Min(TotalLength, bestDist + searchRadius);

        for (float d = searchStart; d <= searchEnd; d += step)
        {
            float sqr = (EvaluatePosition(d) - worldPos).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                bestDist = d;
            }
        }

        // Fine refinement
        searchStart = Mathf.Max(0f, bestDist - step);
        searchEnd = Mathf.Min(TotalLength, bestDist + step);
        step = 0.05f;

        for (float d = searchStart; d <= searchEnd; d += step)
        {
            float sqr = (EvaluatePosition(d) - worldPos).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                bestDist = d;
            }
        }

        ballDistance = bestDist;
        return bestDist;
    }

    /// <summary>
    /// Get the ball's position on the cross-section (0-180°) at the given spline distance.
    /// 0° = left edge, 90° = center, 180° = right edge.
    /// Matches the torus cross-section angle system.
    /// </summary>
    public float GetBallCrossSectionAngle(Vector3 ballWorldPos, float splineDist)
    {
        Vector3 pos, tangent, normal, binormal;
        EvaluateFrame(splineDist, out pos, out tangent, out normal, out binormal);

        // Project ball position into the cross-section plane
        Vector3 offset = ballWorldPos - pos;
        float x = Vector3.Dot(offset, binormal); // left/right
        float y = Vector3.Dot(offset, normal);    // up/down (relative to track center)

        // Convert to angle matching torus convention (0=left, 90=bottom, 180=right)
        float angle = Mathf.Atan2(-y, -x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        // Map to 0-180 range
        return Mathf.Clamp(angle, 0f, 180f);
    }

    // ── PUBLIC API ───────────────────────────────────────────

    public int ControlPointCount { get { return controlPoints.Count; } }
    public float BallDistance { get { return ballDistance; } set { ballDistance = value; } }
    public int Score { get { return score; } }
    public List<Gate> Gates { get { return gates; } }

    public void AddControlPoint(ControlPoint pt)
    {
        controlPoints.Add(pt);
    }

    /// <summary>
    /// Check if the ball has passed any gates and increment the score.
    /// Returns the number of new gates passed this frame.
    /// </summary>
    public int CheckGatePassage(float currentDistance)
    {
        int passed = 0;

        while (currentGateIndex + 1 < gates.Count)
        {
            Gate next = gates[currentGateIndex + 1];
            if (currentDistance > next.distance + 0.5f) // small buffer past the gate
            {
                currentGateIndex++;
                score++;
                passed++;
            }
            else
            {
                break;
            }
        }

        return passed;
    }

    /// <summary>Reset score and gate tracking for a new run.</summary>
    public void ResetRun()
    {
        score = 0;
        currentGateIndex = -1;
        ballDistance = 0f;
    }

    // ── GATE MESH GENERATION ─────────────────────────────────

    // Gate geometry constants — matching Obstacle.cs proportions
    const float GATE_WALL_RECESS = 0.936f;  // front face inner edge (fraction of MINOR_RADIUS)
    const float GATE_CEIL_INNER = 0.916f;   // ceiling inner edge
    const float GATE_DEPTH = 0.1f;          // depth along tangent (ceiling extent)
    const float GATE_STEP_DEG = 4f;         // angular resolution (degrees per step)
    const float GATE_SHADOW_WIDTH = 0.25f;
    const float GATE_SHADOW_OFFSET = 0.008f;
    const float GATE_BASE_HEIGHT = 0.04f;

    /// <summary>
    /// Generate 3D gate mesh at the given gate's position along the spline.
    /// Creates front face + ceiling (2 submeshes), shadow child, base edge child,
    /// and MeshCollider. Matches Obstacle.cs output but oriented to the spline frame.
    /// </summary>
    public GameObject GenerateGateMesh(int gateIndex, Material frontMat, Material topMat, Material shadowMat)
    {
        if (gateIndex < 0 || gateIndex >= gates.Count) return null;

        Gate gate = gates[gateIndex];

        Vector3 pos, tangent, normal, binormal;
        EvaluateFrame(gate.distance, out pos, out tangent, out normal, out binormal);

        float gapFrom = gate.gapCenter - gate.gapHalfWidth;
        float gapTo = gate.gapCenter + gate.gapHalfWidth;

        // Mesh data for main gate (front face submesh 0, ceiling submesh 1)
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris0 = new List<int>(); // front + side caps
        var tris1 = new List<int>(); // ceiling

        // Shadow mesh data (separate child)
        var sVerts = new List<Vector3>();
        var sUVs = new List<Vector2>();
        var sTris = new List<int>();

        // Base edge mesh data (separate child)
        var bVerts = new List<Vector3>();
        var bTris = new List<int>();

        // Build wall sections around the gap
        if (gapFrom > 0f)
            BuildGateWall(0f, gapFrom, pos, tangent, normal, binormal,
                verts, uvs, tris0, tris1,
                sVerts, sUVs, sTris, bVerts, bTris);
        if (gapTo < 180f)
            BuildGateWall(gapTo, 180f, pos, tangent, normal, binormal,
                verts, uvs, tris0, tris1,
                sVerts, sUVs, sTris, bVerts, bTris);

        // Assemble main gate mesh
        Mesh mesh = new Mesh();
        mesh.name = "splineGateMesh";
        mesh.subMeshCount = 2;
        mesh.vertices = verts.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.SetTriangles(tris0.ToArray(), 0);
        mesh.SetTriangles(tris1.ToArray(), 1);
        mesh.Optimize();
        mesh.RecalculateNormals();

        // Create gate parent — named "torusObstacle" for Sphere.cs collision detection
        GameObject gateObj = new GameObject("torusObstacle");

        MeshFilter mf = gateObj.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        MeshRenderer mr = gateObj.AddComponent<MeshRenderer>();
        mr.materials = new Material[] { frontMat, topMat };
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        MeshCollider mc = gateObj.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;

        // Shadow child
        if (sVerts.Count > 0)
        {
            Mesh sMesh = new Mesh();
            sMesh.name = "splineGateShadow";
            sMesh.vertices = sVerts.ToArray();
            sMesh.uv = sUVs.ToArray();
            sMesh.triangles = sTris.ToArray();
            sMesh.Optimize();
            sMesh.RecalculateNormals();

            GameObject shadowObj = new GameObject("torusObstacleShadow");
            shadowObj.AddComponent<MeshFilter>().mesh = sMesh;
            MeshRenderer smr = shadowObj.AddComponent<MeshRenderer>();
            smr.material = shadowMat;
            smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shadowObj.transform.SetParent(gateObj.transform, false);
        }

        // Base edge child
        if (bVerts.Count > 0)
        {
            Mesh bMesh = new Mesh();
            bMesh.name = "splineGateBase";
            bMesh.vertices = bVerts.ToArray();
            bMesh.triangles = bTris.ToArray();
            bMesh.RecalculateNormals();

            GameObject baseObj = new GameObject("torusObstacleBase");
            baseObj.AddComponent<MeshFilter>().mesh = bMesh;
            MeshRenderer bmr = baseObj.AddComponent<MeshRenderer>();
            bmr.material = shadowMat;
            bmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            baseObj.transform.SetParent(gateObj.transform, false);
        }

        // Store reference in gate data
        gate.gameObject = gateObj;
        gates[gateIndex] = gate;

        return gateObj;
    }

    /// <summary>
    /// Build one wall section of a gate (from one angle to another on the cross-section).
    /// Generates front face, ceiling, shadow strip, base edge, and side caps.
    /// </summary>
    void BuildGateWall(
        float fromDeg, float toDeg,
        Vector3 pos, Vector3 tangent, Vector3 normal, Vector3 binormal,
        List<Vector3> verts, List<Vector2> uvs, List<int> tris0, List<int> tris1,
        List<Vector3> sVerts, List<Vector2> sUVs, List<int> sTris,
        List<Vector3> bVerts, List<int> bTris)
    {
        float fromRad = fromDeg * Mathf.Deg2Rad;
        float toRad = toDeg * Mathf.Deg2Rad;
        float diff = toRad - fromRad;

        int stepsCount = Mathf.CeilToInt(diff / (GATE_STEP_DEG * Mathf.Deg2Rad)) + 2;
        float realStep = diff / (float)(stepsCount - 1);

        // Track base indices for this wall section
        int frontBase = verts.Count;
        int shadowBase = sVerts.Count;
        int baseEdgeBase = bVerts.Count;

        // Temporary storage — appended to main lists after front verts
        var ceilVerts = new List<Vector3>();
        var ceilUVs = new List<Vector2>();
        var sideVerts = new List<Vector3>();
        var sideUVs = new List<Vector2>();

        float angle = fromRad;

        for (int i = 0; i < stepsCount; i++)
        {
            // Radial direction from track center toward surface at this cross-section angle
            // Maps gate angle θ → position: pos - binormal*cos(θ) - normal*sin(θ)
            Vector3 radDir = -binormal * Mathf.Cos(angle) - normal * Mathf.Sin(angle);

            // === FRONT FACE ===
            Vector3 upper = pos + radDir * (MINOR_RADIUS * GATE_WALL_RECESS);
            Vector3 bottom = pos + radDir * MINOR_RADIUS;

            verts.Add(upper);
            verts.Add(bottom);
            uvs.Add(Vector2.zero);
            uvs.Add(Vector2.zero);

            if (i > 0)
            {
                int idx = frontBase + (i * 2) - 2;
                tris0.Add(idx);     tris0.Add(idx + 1); tris0.Add(idx + 2);
                tris0.Add(idx + 2); tris0.Add(idx + 1); tris0.Add(idx + 3);
            }

            // === CEILING ===
            Vector3 ceilFar = pos + tangent * GATE_DEPTH + radDir * (MINOR_RADIUS * GATE_CEIL_INNER);
            ceilVerts.Add(ceilFar);
            ceilVerts.Add(upper);
            ceilUVs.Add(Vector2.zero);
            ceilUVs.Add(Vector2.zero);

            // === SHADOW (extends toward camera from base) ===
            Vector3 sInner = bottom + radDir * GATE_SHADOW_OFFSET;
            Vector3 sOuter = bottom - tangent * GATE_SHADOW_WIDTH + radDir * GATE_SHADOW_OFFSET;

            sVerts.Add(sInner);
            sVerts.Add(sOuter);
            sUVs.Add(new Vector2(0f, 1f));
            sUVs.Add(Vector2.zero);

            if (i > 0)
            {
                int si = shadowBase + (i * 2) - 2;
                sTris.Add(si);     sTris.Add(si + 1); sTris.Add(si + 2);
                sTris.Add(si + 2); sTris.Add(si + 1); sTris.Add(si + 3);
            }

            // === BASE EDGE (thin dark strip at front base) ===
            Vector3 bBottom = bottom - tangent * 0.003f;
            Vector3 bTop = pos + radDir * (MINOR_RADIUS * (1f - GATE_BASE_HEIGHT)) - tangent * 0.003f;

            bVerts.Add(bBottom);
            bVerts.Add(bTop);

            if (i > 0)
            {
                int bi = baseEdgeBase + (i * 2) - 2;
                bTris.Add(bi);     bTris.Add(bi + 1); bTris.Add(bi + 2);
                bTris.Add(bi + 2); bTris.Add(bi + 1); bTris.Add(bi + 3);
            }

            // === SIDE CAPS (at gap edges) ===
            if (i == 0 || i == stepsCount - 1)
            {
                Vector3 farBottom = bottom + tangent * GATE_DEPTH;
                sideVerts.Add(upper);
                sideVerts.Add(ceilFar);
                sideVerts.Add(bottom);
                sideVerts.Add(farBottom);
                sideUVs.Add(Vector2.zero);
                sideUVs.Add(Vector2.zero);
                sideUVs.Add(Vector2.zero);
                sideUVs.Add(Vector2.zero);
            }

            angle += realStep;
        }

        // Append ceiling verts and generate ceiling triangles (submesh 1)
        int ceilStartIdx = verts.Count;
        verts.AddRange(ceilVerts);
        uvs.AddRange(ceilUVs);

        for (int i = 1; i < stepsCount; i++)
        {
            int idx = ceilStartIdx + (i * 2) - 2;
            tris1.Add(idx);     tris1.Add(idx + 1); tris1.Add(idx + 2);
            tris1.Add(idx + 2); tris1.Add(idx + 1); tris1.Add(idx + 3);
        }

        // Append side cap verts and generate triangles (submesh 0)
        int sideStartIdx = verts.Count;
        verts.AddRange(sideVerts);
        uvs.AddRange(sideUVs);

        // First cap (i=0): face toward gap center
        if (sideVerts.Count >= 4)
        {
            int si = sideStartIdx;
            tris0.Add(si);     tris0.Add(si + 1); tris0.Add(si + 2);
            tris0.Add(si + 2); tris0.Add(si + 1); tris0.Add(si + 3);
        }
        // Last cap (i=stepsCount-1): reversed winding
        if (sideVerts.Count >= 8)
        {
            int si = sideStartIdx + 4;
            tris0.Add(si);     tris0.Add(si + 2); tris0.Add(si + 1);
            tris0.Add(si + 1); tris0.Add(si + 2); tris0.Add(si + 3);
        }
    }
}
