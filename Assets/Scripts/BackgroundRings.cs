using UnityEngine;

public class BackgroundRings : MonoBehaviour
{
    private Transform[] stars;
    private Vector3[] starBasePositions;
    private float[] starTwinklePhase;
    private float[] starBaseIntensity;

    public void Setup()
    {
        CreateNebula();
        CreateGodRays();
        CreateStarfield();
    }

    void CreateNebula()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        GameObject nebulaObj = new GameObject("Nebula");
        nebulaObj.transform.SetParent(cam.transform, false);

        MeshFilter mf = nebulaObj.AddComponent<MeshFilter>();
        mf.mesh = CreateQuadMesh();

        // Position just inside the far clip plane, scaled to overfill the FOV
        float dist = cam.farClipPlane * 0.9f;
        float height = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * (Screen.width > 0 ? (float)Screen.width / Screen.height : 1.78f);
        nebulaObj.transform.localPosition = new Vector3(0, 0, dist);
        nebulaObj.transform.localRotation = Quaternion.identity;
        nebulaObj.transform.localScale = new Vector3(width * 1.5f, height * 1.5f, 1f);

        Material mat = new Material(Shader.Find("Loopfall/Nebula"));
        mat.SetFloat("_Speed", 0.05f);
        mat.SetFloat("_Scale", 1.8f);
        mat.SetFloat("_Brightness", 1.0f);
        mat.SetColor("_Color1", new Color(0.15f, 0.03f, 0.35f));  // Vivid purple
        mat.SetColor("_Color2", new Color(0.02f, 0.2f, 0.35f));   // Vivid teal
        mat.SetColor("_Color3", new Color(0.3f, 0.03f, 0.15f));   // Vivid magenta
        mat.renderQueue = 1000; // Background queue

        MeshRenderer mr = nebulaObj.AddComponent<MeshRenderer>();
        mr.material = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    void CreateGodRays()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        GameObject raysObj = new GameObject("GodRays");
        raysObj.transform.SetParent(cam.transform, false);

        MeshFilter mf = raysObj.AddComponent<MeshFilter>();
        mf.mesh = CreateQuadMesh();

        // Slightly in front of the nebula
        float dist = cam.farClipPlane * 0.85f;
        float height = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * (Screen.width > 0 ? (float)Screen.width / Screen.height : 1.78f);
        raysObj.transform.localPosition = new Vector3(0, 0, dist);
        raysObj.transform.localRotation = Quaternion.identity;
        raysObj.transform.localScale = new Vector3(width * 1.5f, height * 1.5f, 1f);

        Material mat = new Material(Shader.Find("Loopfall/GodRays"));
        mat.SetFloat("_RayCount", 8);
        mat.SetFloat("_RaySharpness", 5);
        mat.SetFloat("_RayLength", 0.75f);
        mat.SetFloat("_Intensity", 0.4f);
        mat.SetFloat("_RotationSpeed", 0.025f);
        mat.SetColor("_Color1", new Color(0.2f, 0.05f, 0.4f));   // Purple rays
        mat.SetColor("_Color2", new Color(0.03f, 0.2f, 0.3f));   // Teal rays
        mat.SetFloat("_CenterX", 0.5f);
        mat.SetFloat("_CenterY", 0.42f);  // Slightly below center — where the track converges
        mat.SetFloat("_PulseSpeed", 0.3f);
        mat.SetFloat("_PulseAmount", 0.12f);
        mat.renderQueue = 2900; // After nebula (1000), before stars (2950)

        MeshRenderer mr = raysObj.AddComponent<MeshRenderer>();
        mr.material = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    void CreateStarfield()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        int starCount = 250;
        stars = new Transform[starCount];
        starBasePositions = new Vector3[starCount];
        starTwinklePhase = new float[starCount];
        starBaseIntensity = new float[starCount];

        Shader glowShader = Shader.Find("Loopfall/TrailGlow");

        for (int i = 0; i < starCount; i++)
        {
            GameObject star = GameObject.CreatePrimitive(PrimitiveType.Quad);
            star.name = "Star" + i;
            star.transform.parent = transform;
            Object.Destroy(star.GetComponent<Collider>());

            // Place stars at moderate depth (5–40 units) so they're actually visible
            float depth = Random.Range(5f, 40f);
            float halfAngle = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float halfH = depth * Mathf.Tan(halfAngle) * 1.3f;
            float halfW = halfH * (Screen.width > 0 ? (float)Screen.width / Screen.height : 1.78f) * 1.3f;

            Vector3 localPos = new Vector3(
                Random.Range(-halfW, halfW),
                Random.Range(-halfH, halfH),
                depth
            );
            star.transform.position = cam.transform.TransformPoint(localPos);
            star.transform.rotation = cam.transform.rotation;

            float size;
            float brightness;
            float alpha;

            float roll = Random.value;
            if (roll < 0.05f)
            {
                size = Random.Range(0.1f, 0.2f);
                brightness = Random.Range(3.0f, 5.0f);
                alpha = Random.Range(0.7f, 1.0f);
            }
            else if (roll < 0.2f)
            {
                size = Random.Range(0.05f, 0.1f);
                brightness = Random.Range(1.5f, 3.0f);
                alpha = Random.Range(0.4f, 0.7f);
            }
            else
            {
                size = Random.Range(0.02f, 0.05f);
                brightness = Random.Range(0.8f, 1.5f);
                alpha = Random.Range(0.15f, 0.4f);
            }
            star.transform.localScale = Vector3.one * size;

            Color starColor;
            if (Random.value < 0.15f)
            {
                starColor = new Color(1.0f, 0.8f, 0.5f, alpha);
            }
            else
            {
                starColor = new Color(
                    0.5f + Random.Range(0f, 0.2f),
                    0.6f + Random.Range(0f, 0.2f),
                    0.9f + Random.Range(0f, 0.1f),
                    alpha
                );
            }

            Material mat = new Material(glowShader);
            mat.SetColor("_Color", starColor);
            mat.SetFloat("_Intensity", brightness);
            mat.renderQueue = 2950;

            MeshRenderer mr = star.GetComponent<MeshRenderer>();
            mr.material = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            stars[i] = star.transform;
            starBasePositions[i] = star.transform.position;
            starTwinklePhase[i] = Random.Range(0f, Mathf.PI * 2f);
            starBaseIntensity[i] = brightness;
        }
    }

    void Update()
    {
        if (stars == null) return;

        float time = Time.time;

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null) continue;

            // Twinkle
            float twinkle = 0.7f + 0.3f * Mathf.Sin(time * (1.2f + (i % 7) * 0.4f) + starTwinklePhase[i]);
            MeshRenderer mr = stars[i].GetComponent<MeshRenderer>();
            if (mr != null && mr.material != null)
                mr.material.SetFloat("_Intensity", starBaseIntensity[i] * twinkle);
        }
    }

    Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "NebulaQuad";
        mesh.vertices = new Vector3[] {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3( 0.5f, -0.5f, 0),
            new Vector3( 0.5f,  0.5f, 0),
            new Vector3(-0.5f,  0.5f, 0)
        };
        mesh.uv = new Vector2[] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };
        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateNormals();
        return mesh;
    }
}
