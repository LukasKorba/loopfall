using UnityEngine;

public class BackgroundRings : MonoBehaviour
{
    private Transform[] stars;
    private Vector3[] starBasePositions;
    private float[] starTwinklePhase;
    private float[] starBaseIntensity;
    private float[] starDriftSpeed;

    // Kept for AUTO-theme live updates; stars remain baked with per-instance colors.
    private Material nebulaMat;
    private Material godRaysMat;

    public void Setup()
    {
        CreateNebula();
        CreateGodRays();
        CreateStarfield();
    }

    /// <summary>Update nebula + godrays colors to match a new theme (called by AUTO crossfader).</summary>
    public void ApplyThemeLive(ThemeData t)
    {
        if (nebulaMat != null)
        {
            nebulaMat.SetFloat("_Brightness", t.nebulaBrightness);
            nebulaMat.SetColor("_Color1", t.nebulaColor1);
            nebulaMat.SetColor("_Color2", t.nebulaColor2);
            nebulaMat.SetColor("_Color3", t.nebulaColor3);
        }
        if (godRaysMat != null)
        {
            godRaysMat.SetFloat("_Intensity", t.rayIntensity);
            godRaysMat.SetColor("_Color1", t.rayColor1);
            godRaysMat.SetColor("_Color2", t.rayColor2);
        }
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

        ThemeData t = SceneSetup.activeTheme;
        Material mat = new Material(Shader.Find("Loopfall/Nebula"));
        mat.SetFloat("_Speed", 0.04f);
        mat.SetFloat("_Scale", 1.8f);
        mat.SetFloat("_Brightness", t.nebulaBrightness);
        mat.SetColor("_Color1", t.nebulaColor1);
        mat.SetColor("_Color2", t.nebulaColor2);
        mat.SetColor("_Color3", t.nebulaColor3);
        mat.renderQueue = 1000; // Background queue
        nebulaMat = mat;

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

        ThemeData t = SceneSetup.activeTheme;
        Material mat = new Material(Shader.Find("Loopfall/GodRays"));
        mat.SetFloat("_RayCount", 8);
        mat.SetFloat("_RaySharpness", 5);
        mat.SetFloat("_RayLength", 0.7f);
        mat.SetFloat("_Intensity", t.rayIntensity);
        mat.SetFloat("_RotationSpeed", 0.02f);
        mat.SetColor("_Color1", t.rayColor1);
        mat.SetColor("_Color2", t.rayColor2);
        mat.SetFloat("_CenterX", 0.5f);
        mat.SetFloat("_CenterY", 0.42f);  // Where the track converges — wormhole center
        mat.SetFloat("_PulseSpeed", 0.3f);
        mat.SetFloat("_PulseAmount", 0.12f);
        mat.renderQueue = 2900; // After nebula (1000), before stars (2950)
        godRaysMat = mat;

        MeshRenderer mr = raysObj.AddComponent<MeshRenderer>();
        mr.material = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    void CreateStarfield()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        int starCount = 220;
        stars = new Transform[starCount];
        starBasePositions = new Vector3[starCount];
        starTwinklePhase = new float[starCount];
        starBaseIntensity = new float[starCount];
        starDriftSpeed = new float[starCount];

        Shader glowShader = Shader.Find("Loopfall/TrailGlow");

        for (int i = 0; i < starCount; i++)
        {
            GameObject star = GameObject.CreatePrimitive(PrimitiveType.Quad);
            star.name = "Star" + i;
            star.transform.parent = transform;
            Object.Destroy(star.GetComponent<Collider>());

            float halfAngle = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;

            float size;
            float brightness;
            float alpha;
            float depth;

            float roll = Random.value;
            if (roll < 0.06f)
            {
                // Bright feature stars
                depth = Random.Range(20f, 40f);
                size = Random.Range(0.08f, 0.14f);
                brightness = Random.Range(2.5f, 4.0f);
                alpha = Random.Range(0.6f, 0.9f);
            }
            else if (roll < 0.25f)
            {
                // Medium stars
                depth = Random.Range(10f, 40f);
                size = Random.Range(0.05f, 0.1f);
                brightness = Random.Range(1.5f, 3.0f);
                alpha = Random.Range(0.4f, 0.7f);
            }
            else
            {
                // Dim dust
                depth = Random.Range(5f, 40f);
                size = Random.Range(0.02f, 0.06f);
                brightness = Random.Range(0.8f, 1.8f);
                alpha = Random.Range(0.15f, 0.45f);
            }

            float halfH = depth * Mathf.Tan(halfAngle) * 1.3f;
            float halfW = halfH * (Screen.width > 0 ? (float)Screen.width / Screen.height : 1.78f) * 1.3f;

            Vector3 localPos = new Vector3(
                Random.Range(-halfW, halfW),
                Random.Range(-halfH, halfH),
                depth
            );
            star.transform.position = cam.transform.TransformPoint(localPos);
            star.transform.rotation = cam.transform.rotation;
            star.transform.localScale = Vector3.one * size;

            // Color palette from active theme
            ThemeData t = SceneSetup.activeTheme;
            Color starColor;
            float colorRoll = Random.value;
            if (colorRoll < 0.12f)
                starColor = new Color(t.starAccent1.r, t.starAccent1.g, t.starAccent1.b, alpha);
            else if (colorRoll < 0.28f)
                starColor = new Color(t.starAccent2.r, t.starAccent2.g, t.starAccent2.b, alpha);
            else if (colorRoll < 0.40f)
                starColor = new Color(t.starAccent3.r, t.starAccent3.g, t.starAccent3.b, alpha);
            else if (colorRoll < 0.50f)
                starColor = new Color(t.starAccent4.r, t.starAccent4.g, t.starAccent4.b, alpha);
            else
            {
                Color b = t.starBase;
                starColor = new Color(
                    b.r + Random.Range(-0.1f, 0.1f),
                    b.g + Random.Range(-0.1f, 0.1f),
                    b.b + Random.Range(-0.05f, 0.05f),
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
            // Slow drift speed — deeper stars drift slower (parallax)
            starDriftSpeed[i] = Random.Range(0.01f, 0.04f) * (1f - depth / 50f);
        }
    }

    void Update()
    {
        if (stars == null) return;

        float time = Time.time;

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null) continue;

            // Twinkle — varied rates per star
            float twinkle = 0.65f + 0.35f * Mathf.Sin(time * (1.0f + (i % 7) * 0.5f) + starTwinklePhase[i]);
            MeshRenderer mr = stars[i].GetComponent<MeshRenderer>();
            if (mr != null && mr.material != null)
                mr.material.SetFloat("_Intensity", starBaseIntensity[i] * twinkle);

            // Slow drift — stars creep sideways, wrapping gives sense of motion
            Vector3 pos = starBasePositions[i];
            float drift = starDriftSpeed[i];
            pos.x += Mathf.Sin(time * drift + starTwinklePhase[i]) * 0.3f;
            pos.y += Mathf.Cos(time * drift * 0.7f + starTwinklePhase[i] * 1.3f) * 0.15f;
            stars[i].position = pos;
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
