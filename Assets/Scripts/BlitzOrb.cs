using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Collectible orb for Blitz mode upgrade tracks.
/// Short arc strip on torus inner surface — player steers to collect.
/// Three types power three upgrade tracks (Gun, Cadency, Shield).
/// </summary>
public class BlitzOrb
{
    public enum OrbType { Gun, Cadency, Shield }

    public GameObject mGameObject;
    public float mAngle;
    public OrbType mType;
    public bool mCollected = false;

    private const float STRIP_WIDTH = 0.25f;
    private const float SURFACE_OFFSET = 0.03f;
    private const float ARC_HALF_SPAN = 20f; // degrees — 40° total arc

    public BlitzOrb(OrbType type, float crossAngleDeg, float obstacleStepInv, Material material)
    {
        mType = type;
        mGameObject = new GameObject("blitzOrb");

        MeshFilter mf = mGameObject.AddComponent<MeshFilter>();
        mf.mesh = GenerateArcMesh(crossAngleDeg, obstacleStepInv);

        MeshRenderer mr = mGameObject.AddComponent<MeshRenderer>();
        mr.material = material;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
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
