using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Collectible track zone for Time Warp mode.
/// Full-width glowing strip on torus inner surface.
/// Ball rolls through to gain/lose time.
/// Extensible: add new ItemType values for future items.
/// </summary>
public class TrackItem
{
    public enum ItemType { TimePlus, TimeMinus }

    public GameObject mGameObject;
    public float mAngle;
    public ItemType mType;
    public bool mCollected = false;

    private const float STRIP_WIDTH = 0.4f;
    private const float SURFACE_OFFSET = 0.025f;

    public TrackItem(ItemType type, float obstacleStepInv, Material material)
    {
        mType = type;
        mGameObject = new GameObject("trackItem");

        MeshFilter mf = mGameObject.AddComponent<MeshFilter>();
        mf.mesh = GenerateStripMesh(obstacleStepInv);

        MeshRenderer mr = mGameObject.AddComponent<MeshRenderer>();
        mr.material = material;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    /// <summary>
    /// Full-width strip across torus cross-section (half-tube: ~8° to ~172°).
    /// Single-sided mesh with manual normals pointing inward (toward camera).
    /// Shader uses Cull Off for double-sided rendering.
    /// </summary>
    Mesh GenerateStripMesh(float obstacleStepInv)
    {
        float fromAngle = 8f * Mathf.Deg2Rad;
        float toAngle = 172f * Mathf.Deg2Rad;
        float range = toAngle - fromAngle;
        int steps = Mathf.Max((int)(range * obstacleStepInv), 10) + 1;
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

            // Surface point on torus (matching Obstacle coordinate system)
            Vector3 surface = new Vector3(0f, -10f - y, -z);

            // Inward normal (toward torus center / camera)
            Vector3 normal = new Vector3(0f, y, z).normalized;
            Vector3 offset = normal * SURFACE_OFFSET;

            verts.Add(surface + offset + new Vector3(-halfWidth, 0f, 0f));
            verts.Add(surface + offset + new Vector3(halfWidth, 0f, 0f));
            norms.Add(normal);
            norms.Add(normal);

            if (i > 0)
            {
                int idx = i * 2;
                // Single-sided quad — same winding as obstacle shadow mesh
                tris.Add(idx - 2); tris.Add(idx - 1); tris.Add(idx);
                tris.Add(idx); tris.Add(idx - 1); tris.Add(idx + 1);
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "trackItemStrip";
        mesh.vertices = verts.ToArray();
        mesh.normals = norms.ToArray();
        mesh.triangles = tris.ToArray();
        return mesh;
    }

    /// <summary>Returns the time effect magnitude (always positive).</summary>
    public float GetTimeValue()
    {
        switch (mType)
        {
            case ItemType.TimePlus: return 2f;
            case ItemType.TimeMinus: return 1f;
            default: return 0f;
        }
    }
}
