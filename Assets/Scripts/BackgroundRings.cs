using UnityEngine;

public class BackgroundRings : MonoBehaviour
{
    private Transform[] rings;
    private Vector3[] rotationSpeeds;

    public void Setup()
    {
        int ringCount = 10;
        rings = new Transform[ringCount];
        rotationSpeeds = new Vector3[ringCount];

        // Color palette: purples, blues, reds, teals
        Color[] palette = {
            new Color(0.15f, 0.08f, 0.25f),  // Deep purple
            new Color(0.08f, 0.15f, 0.25f),  // Dark blue
            new Color(0.2f, 0.06f, 0.1f),    // Dark red
            new Color(0.06f, 0.18f, 0.2f),   // Teal
            new Color(0.2f, 0.1f, 0.02f),    // Amber
            new Color(0.1f, 0.06f, 0.22f),   // Violet
            new Color(0.05f, 0.1f, 0.2f),    // Navy
            new Color(0.22f, 0.04f, 0.15f),  // Magenta
            new Color(0.08f, 0.2f, 0.12f),   // Forest
            new Color(0.18f, 0.12f, 0.06f),  // Bronze
        };

        for (int i = 0; i < ringCount; i++)
        {
            float radius = Random.Range(16.0f, 40.0f);
            float thickness = Random.Range(0.2f, 0.5f);
            float alpha = Random.Range(0.2f, 0.45f);
            Color baseColor = palette[i];

            GameObject ringObj = new GameObject("BackgroundRing" + i);
            ringObj.transform.parent = transform;
            ringObj.transform.localPosition = Vector3.zero;
            ringObj.transform.rotation = Quaternion.Euler(
                Random.Range(-50f, 50f),
                Random.Range(-180f, 180f),
                Random.Range(-30f, 30f)
            );

            Mesh mesh = GenerateFlatRingMesh(radius, thickness, 96);

            MeshFilter mf = ringObj.AddComponent<MeshFilter>();
            mf.mesh = mesh;

            Material mat = new Material(Shader.Find("Standard"));
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 2900;
            mat.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            mat.SetFloat("_Glossiness", 0.0f);
            mat.SetFloat("_Metallic", 0.0f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", baseColor * 0.4f);

            MeshRenderer mr = ringObj.AddComponent<MeshRenderer>();
            mr.material = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            rings[i] = ringObj.transform;

            // Randomized rotation speed — some lazy, some faster
            float speedScale = Random.Range(1.0f, 8.0f);
            rotationSpeeds[i] = new Vector3(
                Random.Range(-1f, 1f) * speedScale,
                Random.Range(-1f, 1f) * speedScale,
                Random.Range(-1f, 1f) * speedScale
            );
        }
    }

    void Update()
    {
        if (rings == null) return;

        for (int i = 0; i < rings.Length; i++)
        {
            rings[i].Rotate(rotationSpeeds[i] * Time.deltaTime);
        }
    }

    Mesh GenerateFlatRingMesh(float radius, float tubeRadius, int segments)
    {
        int tubeSegs = 6;
        var verts = new System.Collections.Generic.List<Vector3>();
        var tris = new System.Collections.Generic.List<int>();

        for (int i = 0; i < segments; i++)
        {
            float a0 = ((float)i / segments) * Mathf.PI * 2;
            float a1 = ((float)(i + 1) / segments) * Mathf.PI * 2;

            Vector3 c0 = new Vector3(Mathf.Cos(a0) * radius, Mathf.Sin(a0) * radius, 0);
            Vector3 c1 = new Vector3(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius, 0);

            Vector3 radial0 = c0.normalized;
            Vector3 radial1 = c1.normalized;
            Vector3 fwd = (c1 - c0).normalized;
            Vector3 up0 = Vector3.Cross(fwd, radial0).normalized;
            Vector3 fwd1 = fwd; // approximate
            Vector3 up1 = Vector3.Cross(fwd1, radial1).normalized;

            for (int j = 0; j < tubeSegs; j++)
            {
                float t0 = ((float)j / tubeSegs) * Mathf.PI * 2;
                float t1 = ((float)(j + 1) / tubeSegs) * Mathf.PI * 2;

                Vector3 v00 = c0 + (radial0 * Mathf.Cos(t0) + up0 * Mathf.Sin(t0)) * tubeRadius;
                Vector3 v10 = c1 + (radial1 * Mathf.Cos(t0) + up1 * Mathf.Sin(t0)) * tubeRadius;
                Vector3 v01 = c0 + (radial0 * Mathf.Cos(t1) + up0 * Mathf.Sin(t1)) * tubeRadius;
                Vector3 v11 = c1 + (radial1 * Mathf.Cos(t1) + up1 * Mathf.Sin(t1)) * tubeRadius;

                int idx = verts.Count;
                verts.Add(v00); verts.Add(v10); verts.Add(v11); verts.Add(v01);
                tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
                tris.Add(idx); tris.Add(idx + 2); tris.Add(idx + 3);
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "BackgroundRing";
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }
}
