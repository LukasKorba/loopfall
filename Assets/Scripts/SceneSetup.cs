using UnityEngine;

/// <summary>
/// Creates the game scene at runtime since we don't have the original .lxo models.
/// Generates the torus track, ball, camera, lighting, and materials.
/// Attach this to an empty GameObject in the scene.
/// </summary>
public class SceneSetup : MonoBehaviour
{
    // MANUAL PARAM: Torus dimensions
    public float torusMajorRadius = 10.0f;
    public float torusMinorRadius = 1.0f;
    // MANUAL PARAM: Mesh resolution (fewer = more visible facets for flat shading)
    public int majorSegments = 64;
    public int minorSegments = 16;
    // MANUAL PARAM: Only render bottom hemisphere of tube (the U-shape)
    public bool halfTubeOnly = true;
    // MANUAL PARAM: Use spline-extruded track instead of fixed torus
    public bool useSplineTrack = false;
    // MANUAL PARAM: Blitz mode — laser beam shooting, no gates
    public bool useBlitzMode = false;

    // Direct references — ensures shaders are included in builds
    public Shader depthHueShiftRef;
    public Shader gateShaderRef;
    public Shader blackHoleWarpRef;
    public Shader obstacleShadowShaderRef;
    public Shader ballShaderRef;
    public Shader trackGridShaderRef;
    public Shader railShaderRef;
    public Shader trailGlowShaderRef;

    void Awake()
    {
        // Set mode before anything else — other scripts read GameConfig on Start
        if (useBlitzMode)
            GameConfig.ActiveMode = GameModeType.Blitz;

        // Force highest graphics tier — Tier2 on iOS disables HDR, killing emission glow
        Graphics.activeTier = UnityEngine.Rendering.GraphicsTier.Tier3;

        // Force Ultra quality on all platforms (iOS defaults to Medium otherwise)
        QualitySettings.SetQualityLevel(5, true);

        CreateMaterials();
        CreateTorus();
        CreateBall();
        CreateCamera();
        CreateLighting();
        CreateUI();
        CreateBackground();
        CreateRewindSystem();
        CreateAudio();
        CreatePlatformServices();
        CreateDisplaySettings();
        if (useSplineTrack) CreateSplineTrack();
#if UNITY_EDITOR
        CreateDebugPanel();
#endif

        // Physics settings
        Physics.gravity = new Vector3(0, -9.81f, 0);

        // Force high quality rendering on all platforms
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
        QualitySettings.shadowDistance = 15.0f; // Tight range — shadow map texels focused on visible area
        QualitySettings.shadowCascades = 1;
        QualitySettings.shadowProjection = ShadowProjection.StableFit; // Prevents cascade shifting
        QualitySettings.pixelLightCount = 4;
    }

    Material trackMaterial;
    Material obstacleFrontMaterial;
    Material obstacleTopMaterial;
    Material obstacleShadowMaterial;
    Material ballMaterial;
    Material railMaterialLeft;
    Material railMaterialRight;
    Material trailMaterial;
    Material beamMaterial;
    Material blitzBoxMaterial;
    Material blitzGateMaterial;
    Material blitzButtonMaterial;
    Material blitzConnectionMaterial;
    Material blitzOrbGunMaterial;
    Material blitzOrbCadencyMaterial;
    Material blitzOrbShieldMaterial;
    Material blitzShieldVisualMaterial;
    Shader depthHueShiftShader;
    BackgroundRings backgroundRings;

    public static ThemeData activeTheme;

    // ── AUTO THEME MODE ──────────────────────────────────────
    // Crossfades between random themes every AUTO_CYCLE_SECONDS.
    // Active when saved index == ThemeData.AUTO_INDEX.
    bool mAutoActive;
    int mAutoFromIdx;
    int mAutoToIdx;
    float mAutoPhase;
    ThemeData mAutoBuffer;
    ThemeData[] mAutoThemes;  // cached — rebuilding each frame would allocate 8 themes
    const float AUTO_CYCLE_SECONDS = 10f;

    public bool IsAutoThemeActive() { return mAutoActive; }

    /// <summary>
    /// Apply a theme to all existing materials without reloading the scene.
    /// Call from settings UI when browsing themes.
    /// </summary>
    public void ApplyThemeLive(ThemeData t)
    {
        activeTheme = t;

        if (trackMaterial != null)
        {
            trackMaterial.SetColor("_BaseColor", t.trackBase);
            trackMaterial.SetColor("_GridColor1", t.gridColor1);
            trackMaterial.SetColor("_GridColor2", t.gridColor2);
            trackMaterial.SetColor("_GridColor3", t.gridColor3);
            trackMaterial.SetColor("_FarColor", t.gridFarColor);
            trackMaterial.SetColor("_SparkColor1", t.sparkColor1);
            trackMaterial.SetColor("_SparkColor2", t.sparkColor2);
            trackMaterial.SetColor("_SparkColor3", t.sparkColor3);
        }
        if (obstacleFrontMaterial != null)
        {
            obstacleFrontMaterial.SetColor("_Color", t.gateFrontColor);
            obstacleFrontMaterial.SetColor("_EmissionColor", t.gateFrontEmission);
        }
        if (obstacleTopMaterial != null)
        {
            obstacleTopMaterial.SetColor("_Color", t.gateTopColor);
            obstacleTopMaterial.SetColor("_EmissionColor", t.gateTopEmission);
        }
        if (ballMaterial != null)
        {
            ballMaterial.SetColor("_Color", t.ballColor);
            ballMaterial.SetColor("_RimColor", t.ballRimColor);
            ballMaterial.SetColor("_EmissionBase", t.ballEmissionBase);
        }
        if (railMaterialLeft != null)
        {
            railMaterialLeft.SetColor("_NearColor", t.railLeftNear);
            railMaterialLeft.SetColor("_FarColor", t.railLeftFar);
            railMaterialLeft.SetColor("_NearEmission", t.railLeftEmissionNear);
            railMaterialLeft.SetColor("_FarEmission", t.railLeftEmissionFar);
        }
        if (railMaterialRight != null)
        {
            railMaterialRight.SetColor("_NearColor", t.railRightNear);
            railMaterialRight.SetColor("_FarColor", t.railRightFar);
            railMaterialRight.SetColor("_NearEmission", t.railRightEmissionNear);
            railMaterialRight.SetColor("_FarEmission", t.railRightEmissionFar);
        }
        if (backgroundRings != null) backgroundRings.ApplyThemeLive(t);

        Camera cam = Camera.main;
        if (cam != null) cam.backgroundColor = t.cameraBg;
    }

    /// <summary>Begin AUTO mode: crossfade from the currently-shown theme to a random different one.</summary>
    public void StartAutoMode()
    {
        if (mAutoThemes == null) mAutoThemes = ThemeData.All();
        mAutoActive = true;
        mAutoPhase = 0f;
        if (mAutoBuffer == null) mAutoBuffer = new ThemeData();

        // Start from whatever is currently shown so the switch into AUTO is smooth.
        // If no active theme yet, start from a random one.
        if (activeTheme == null)
        {
            mAutoFromIdx = Random.Range(0, mAutoThemes.Length);
            activeTheme = mAutoThemes[mAutoFromIdx];
        }
        else
        {
            mAutoFromIdx = FindThemeIndex(mAutoThemes, activeTheme);
            if (mAutoFromIdx < 0) mAutoFromIdx = 0;
        }
        mAutoToIdx = PickDifferentIndex(mAutoThemes.Length, mAutoFromIdx);
    }

    public void StopAutoMode()
    {
        mAutoActive = false;
    }

    static int FindThemeIndex(ThemeData[] themes, ThemeData t)
    {
        if (t == null) return -1;
        for (int i = 0; i < themes.Length; i++)
            if (themes[i].name == t.name) return i;
        return -1;
    }

    static int PickDifferentIndex(int count, int exclude)
    {
        if (count <= 1) return 0;
        int pick = Random.Range(0, count - 1);
        if (pick >= exclude) pick++;
        return pick;
    }

    void Update()
    {
        if (!mAutoActive) return;

        mAutoPhase += Time.deltaTime / AUTO_CYCLE_SECONDS;
        if (mAutoPhase >= 1f)
        {
            mAutoFromIdx = mAutoToIdx;
            mAutoToIdx = PickDifferentIndex(mAutoThemes.Length, mAutoFromIdx);
            mAutoPhase = 0f;
        }

        float eased = mAutoPhase * mAutoPhase * (3f - 2f * mAutoPhase); // smoothstep
        ThemeData.LerpInto(mAutoBuffer, mAutoThemes[mAutoFromIdx], mAutoThemes[mAutoToIdx], eased);
        ApplyThemeLive(mAutoBuffer);
    }

    void CreateMaterials()
    {
        // Load saved theme. AUTO_INDEX (-1) means "crossfade between random themes";
        // a specific index sticks until the user changes it in settings.
        ThemeData[] themes = ThemeData.All();
        int savedIdx = ThemeData.LoadSavedIndex();
        int idx;
        if (savedIdx == ThemeData.AUTO_INDEX)
            idx = Random.Range(0, themes.Length);
        else
            idx = Mathf.Clamp(savedIdx, 0, themes.Length - 1);
        activeTheme = themes[idx];
        ThemeData t = activeTheme;

        // Track: dark base with glowing grid lines
        Shader trackGridShader = trackGridShaderRef != null ? trackGridShaderRef : Shader.Find("Loopfall/TrackGrid");
        trackMaterial = new Material(trackGridShader);
        trackMaterial.SetColor("_BaseColor", t.trackBase);
        trackMaterial.SetColor("_GridColor1", t.gridColor1);
        trackMaterial.SetColor("_GridColor2", t.gridColor2);
        trackMaterial.SetColor("_GridColor3", t.gridColor3);
        trackMaterial.SetFloat("_MajorGridU", 16);
        trackMaterial.SetFloat("_MajorGridV", 8);
        trackMaterial.SetFloat("_MinorGridU", 4);
        trackMaterial.SetFloat("_MinorGridV", 2);
        trackMaterial.SetFloat("_MajorLineWidth", 0.012f);
        trackMaterial.SetFloat("_MinorLineWidth", 0.005f);
        trackMaterial.SetFloat("_GlowIntensity", 1.0f);
        trackMaterial.SetFloat("_GlowFalloff", 8f);
        trackMaterial.SetFloat("_PulseSpeed", 0.4f);
        trackMaterial.SetFloat("_PulseAmount", 0.12f);
        trackMaterial.SetFloat("_Glossiness", 0.25f);
        trackMaterial.SetFloat("_Metallic", 0.05f);
        trackMaterial.SetFloat("_DepthFadeStart", 1.5f);
        trackMaterial.SetFloat("_DepthFadeEnd", 14.0f);
        trackMaterial.SetColor("_FarColor", t.gridFarColor);
        trackMaterial.SetColor("_SparkColor1", t.sparkColor1);
        trackMaterial.SetColor("_SparkColor2", t.sparkColor2);
        trackMaterial.SetColor("_SparkColor3", t.sparkColor3);

        // Obstacle front: custom shader — identical rendering on all platforms
        Shader gateShader = gateShaderRef != null ? gateShaderRef : Shader.Find("Loopfall/Gate");
        obstacleFrontMaterial = new Material(gateShader);
        obstacleFrontMaterial.SetColor("_Color", t.gateFrontColor);
        obstacleFrontMaterial.SetColor("_EmissionColor", t.gateFrontEmission);
        obstacleFrontMaterial.SetFloat("_EmissionIntensity", 1.0f);

        // Obstacle top: brighter
        obstacleTopMaterial = new Material(gateShader);
        obstacleTopMaterial.SetColor("_Color", t.gateTopColor);
        obstacleTopMaterial.SetColor("_EmissionColor", t.gateTopEmission);
        obstacleTopMaterial.SetFloat("_EmissionIntensity", 1.0f);

        // Obstacle shadow: double-sided transparent dark strip — custom shader (no Standard on iOS)
        Shader shadowShader = obstacleShadowShaderRef != null
            ? obstacleShadowShaderRef
            : Shader.Find("Loopfall/ObstacleShadow");
        obstacleShadowMaterial = new Material(shadowShader);
        obstacleShadowMaterial.SetColor("_Color", new Color(0, 0, 0, 0.85f));

        // Ball: neon-rimmed sphere — fresnel glow matches the tunnel aesthetic
        Shader ballShader = ballShaderRef != null ? ballShaderRef : Shader.Find("Loopfall/Ball");
        ballMaterial = new Material(ballShader);
        ballMaterial.SetColor("_Color", t.ballColor);
        ballMaterial.SetColor("_RimColor", t.ballRimColor);
        ballMaterial.SetFloat("_RimPower", 2.5f);
        ballMaterial.SetFloat("_RimStrength", 1.2f);
        ballMaterial.SetFloat("_Glossiness", 0.85f);
        ballMaterial.SetFloat("_Metallic", 0.3f);
        ballMaterial.SetColor("_EmissionBase", t.ballEmissionBase);

        // Edge rails: distance-fading opaque tubes
        Shader railShader = railShaderRef != null ? railShaderRef : Shader.Find("Loopfall/Rail");
        railMaterialLeft = new Material(railShader);
        railMaterialLeft.SetColor("_NearColor", t.railLeftNear);
        railMaterialLeft.SetColor("_FarColor", t.railLeftFar);
        railMaterialLeft.SetColor("_NearEmission", t.railLeftEmissionNear);
        railMaterialLeft.SetColor("_FarEmission", t.railLeftEmissionFar);
        railMaterialLeft.SetFloat("_Glossiness", 0.7f);
        railMaterialLeft.SetFloat("_Metallic", 0.3f);

        railMaterialRight = new Material(railShader);
        railMaterialRight.SetColor("_NearColor", t.railRightNear);
        railMaterialRight.SetColor("_FarColor", t.railRightFar);
        railMaterialRight.SetColor("_NearEmission", t.railRightEmissionNear);
        railMaterialRight.SetColor("_FarEmission", t.railRightEmissionFar);
        railMaterialRight.SetFloat("_Glossiness", 0.7f);
        railMaterialRight.SetFloat("_Metallic", 0.3f);

        // Trail: additive glow — color/alpha set per-segment at runtime
        Shader trailShader = trailGlowShaderRef != null ? trailGlowShaderRef : Shader.Find("Loopfall/TrailGlow");
        trailMaterial = new Material(trailShader);
        trailMaterial.SetColor("_Color", Color.white);
        trailMaterial.SetFloat("_Intensity", 2.0f);
        trailMaterial.renderQueue = 3000;

        // Blitz beam material — additive glow, same shader as trail
        Shader beamShader = trailGlowShaderRef != null ? trailGlowShaderRef : Shader.Find("Loopfall/TrailGlow");
        beamMaterial = new Material(beamShader);
        beamMaterial.SetColor("_Color", Color.white);
        beamMaterial.SetFloat("_Intensity", 3.0f);
        beamMaterial.renderQueue = 3000;

        // Blitz box material — cyan glowing target cubes (shoot for points)
        blitzBoxMaterial = new Material(gateShader);
        blitzBoxMaterial.SetColor("_Color", new Color(0.15f, 0.85f, 0.95f));
        blitzBoxMaterial.SetColor("_EmissionColor", new Color(0.08f, 0.55f, 0.75f));
        blitzBoxMaterial.SetFloat("_EmissionIntensity", 1.5f);

        // Blitz gate material — purple-blue electric bars (deadly barrier)
        blitzGateMaterial = new Material(gateShader);
        blitzGateMaterial.SetColor("_Color", new Color(0.5f, 0.2f, 1.0f));
        blitzGateMaterial.SetColor("_EmissionColor", new Color(0.3f, 0.1f, 0.9f));
        blitzGateMaterial.SetFloat("_EmissionIntensity", 2.0f);

        // Blitz button material — orange/amber (shoot to open linked gate)
        blitzButtonMaterial = new Material(gateShader);
        blitzButtonMaterial.SetColor("_Color", new Color(0.95f, 0.5f, 0.1f));
        blitzButtonMaterial.SetColor("_EmissionColor", new Color(0.8f, 0.35f, 0.05f));
        blitzButtonMaterial.SetFloat("_EmissionIntensity", 2.0f);

        // Blitz connection material — electric trail from button to gate
        Shader connectionShader = trailGlowShaderRef != null ? trailGlowShaderRef : Shader.Find("Loopfall/TrailGlow");
        blitzConnectionMaterial = new Material(connectionShader);
        blitzConnectionMaterial.SetColor("_Color", new Color(0.5f, 0.2f, 1.0f));
        blitzConnectionMaterial.SetFloat("_Intensity", 2.0f);
        blitzConnectionMaterial.renderQueue = 3000;

        // Blitz orb materials — additive glow (TrailGlow) for semi-transparent look.
        // Intensity 2.5 still clears the DepthHueShift bright-exemption threshold
        // (lum-0.5)*4 so orbs keep their identity color at all depths, but less
        // clamping means the base hue shows through closer to the UI slot color.
        Shader orbShader = trailGlowShaderRef != null ? trailGlowShaderRef : Shader.Find("Loopfall/TrailGlow");
        blitzOrbGunMaterial = new Material(orbShader);
        blitzOrbGunMaterial.SetColor("_Color", new Color(1.0f, 0.85f, 0.1f));
        blitzOrbGunMaterial.SetFloat("_Intensity", 2.5f);
        blitzOrbGunMaterial.renderQueue = 3000;

        blitzOrbCadencyMaterial = new Material(orbShader);
        blitzOrbCadencyMaterial.SetColor("_Color", new Color(0.2f, 0.7f, 1.0f));
        blitzOrbCadencyMaterial.SetFloat("_Intensity", 2.5f);
        blitzOrbCadencyMaterial.renderQueue = 3000;

        blitzOrbShieldMaterial = new Material(orbShader);
        blitzOrbShieldMaterial.SetColor("_Color", new Color(0.1f, 1.0f, 0.4f));
        blitzOrbShieldMaterial.SetFloat("_Intensity", 2.5f);
        blitzOrbShieldMaterial.renderQueue = 3000;

        // Blitz shield visual — green glow around ball when shield is active
        Shader shieldShader = trailGlowShaderRef != null ? trailGlowShaderRef : Shader.Find("Loopfall/TrailGlow");
        blitzShieldVisualMaterial = new Material(shieldShader);
        blitzShieldVisualMaterial.SetColor("_Color", new Color(0.1f, 1.0f, 0.4f));
        blitzShieldVisualMaterial.SetFloat("_Intensity", 1.5f);

        // Use direct scene reference (Shader.Find gets stripped on iOS)
        depthHueShiftShader = depthHueShiftRef;
    }

    void CreateTorus()
    {
        // Torus parent object — this is what rotates
        GameObject torusObj = new GameObject("TorusTrack");
        torusObj.transform.position = Vector3.zero;

        // Track mesh (the visible torus)
        GameObject trackMesh = new GameObject("Psychokinesis3");
        trackMesh.transform.parent = torusObj.transform;

        MeshFilter mf = trackMesh.AddComponent<MeshFilter>();
        mf.mesh = GenerateTorusMesh();

        MeshRenderer mr = trackMesh.AddComponent<MeshRenderer>();
        mr.material = trackMaterial;
        mr.receiveShadows = true;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // Track receives but doesn't cast

        // Collision
        MeshCollider mc = trackMesh.AddComponent<MeshCollider>();
        mc.sharedMesh = mf.mesh;

        // Physics material: zero friction (matching Unity original)
        PhysicsMaterial meshFriction = new PhysicsMaterial("meshFriction");
        meshFriction.dynamicFriction = 0.0f;
        meshFriction.staticFriction = 0.0f;
        meshFriction.bounciness = 0.0f;
        meshFriction.frictionCombine = PhysicsMaterialCombine.Minimum;
        meshFriction.bounceCombine = PhysicsMaterialCombine.Minimum;
        mc.material = meshFriction;

        // Torus script
        Torus torusScript = torusObj.AddComponent<Torus>();

        // Edge rails (deadly boundaries)
        CreateEdgeRails(torusObj);

        // Store reference for later
        torusObj.tag = "GameController"; // We'll find it by tag
    }

    void CreateEdgeRails(GameObject torusParent)
    {
        // Rails at the edges of the U-shape hemisphere
        float minorAngleLeft = -Mathf.PI / 2.0f;
        float minorAngleRight = Mathf.PI / 2.0f;

        CreateSingleRail(torusParent, minorAngleLeft, railMaterialLeft);
        CreateSingleRail(torusParent, minorAngleRight, railMaterialRight);
    }

    void CreateSingleRail(GameObject parent, float minorAngle, Material mat)
    {
        GameObject railObj = new GameObject("torusObstacle"); // Named so collision = death
        railObj.transform.parent = parent.transform;

        Mesh railMesh = GenerateRailMesh(minorAngle, 0.09f);

        MeshFilter mf = railObj.AddComponent<MeshFilter>();
        mf.mesh = railMesh;

        MeshRenderer mr = railObj.AddComponent<MeshRenderer>();
        mr.material = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        MeshCollider mc = railObj.AddComponent<MeshCollider>();
        mc.sharedMesh = railMesh;
    }

    Mesh GenerateRailMesh(float minorAngle, float railRadius)
    {
        int railSegs = 128;
        int tubeSegs = 16;

        // Shared vertex grid: (railSegs+1) rings x tubeSegs verts each
        // Ring 0 and ring railSegs are the same position (loop closed via indices)
        int ringCount = railSegs; // Closed loop — last ring wraps to first
        int vertCount = ringCount * tubeSegs;
        Vector3[] verts = new Vector3[vertCount];
        Vector3[] normals = new Vector3[vertCount];

        for (int i = 0; i < ringCount; i++)
        {
            float ma = ((float)i / railSegs) * Mathf.PI * 2;
            float maNext = ((float)(i + 1) / railSegs) * Mathf.PI * 2;
            Vector3 center = RailCenter(ma, minorAngle);
            Vector3 centerNext = RailCenter(maNext, minorAngle);
            Vector3 fwd = (centerNext - center).normalized;

            Vector3 tubeCenter = new Vector3(
                torusMajorRadius * Mathf.Cos(ma),
                torusMajorRadius * Mathf.Sin(ma),
                0
            );
            Vector3 radialOut = (center - tubeCenter).normalized;
            Vector3 binormal = Vector3.Cross(fwd, radialOut).normalized;

            for (int j = 0; j < tubeSegs; j++)
            {
                float ta = ((float)j / tubeSegs) * Mathf.PI * 2;
                Vector3 localDir = radialOut * Mathf.Cos(ta) + binormal * Mathf.Sin(ta);
                int idx = i * tubeSegs + j;
                verts[idx] = center + localDir * railRadius;
                normals[idx] = localDir;
            }
        }

        // Triangles: front + back faces (double-sided)
        int[] tris = new int[railSegs * tubeSegs * 12];
        int t = 0;
        for (int i = 0; i < railSegs; i++)
        {
            int iNext = (i + 1) % ringCount;
            for (int j = 0; j < tubeSegs; j++)
            {
                int jNext = (j + 1) % tubeSegs;
                int v00 = i * tubeSegs + j;
                int v10 = iNext * tubeSegs + j;
                int v01 = i * tubeSegs + jNext;
                int v11 = iNext * tubeSegs + jNext;

                // Front face
                tris[t++] = v00; tris[t++] = v10; tris[t++] = v11;
                tris[t++] = v00; tris[t++] = v11; tris[t++] = v01;

                // Back face (reversed winding)
                tris[t++] = v00; tris[t++] = v11; tris[t++] = v10;
                tris[t++] = v00; tris[t++] = v01; tris[t++] = v11;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "rail";
        mesh.vertices = verts;
        mesh.normals = normals;
        mesh.triangles = tris;
        return mesh;
    }

    Vector3 RailCenter(float majorAngle, float minorAngle)
    {
        float r = torusMajorRadius + torusMinorRadius * Mathf.Cos(minorAngle);
        return new Vector3(
            r * Mathf.Cos(majorAngle),
            r * Mathf.Sin(majorAngle),
            torusMinorRadius * Mathf.Sin(minorAngle)
        );
    }

    Vector3 RailTubePoint(Vector3 center, Vector3 forward, float majorAngle, float tubeAngle, float radius)
    {
        Vector3 tubeCenter = new Vector3(
            torusMajorRadius * Mathf.Cos(majorAngle),
            torusMajorRadius * Mathf.Sin(majorAngle),
            0
        );
        Vector3 radialOut = (center - tubeCenter).normalized;
        Vector3 binormal = Vector3.Cross(forward.normalized, radialOut).normalized;
        return center + (radialOut * Mathf.Cos(tubeAngle) + binormal * Mathf.Sin(tubeAngle)) * radius;
    }

    void CreateBall()
    {
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "Ball";
        // Original: scale 0.2, position (0, -10.9, 0)
        ball.transform.localScale = Vector3.one * 0.2f;
        ball.transform.position = new Vector3(0, -10.9f, 0);

        ball.GetComponent<Renderer>().material = ballMaterial;

        // Replace SphereCollider default
        Destroy(ball.GetComponent<SphereCollider>());
        SphereCollider sc = ball.AddComponent<SphereCollider>();
        sc.radius = 0.5f; // Unity sphere primitive has 0.5 radius at scale 1

        // Physics material: friction 0.2 (original value)
        PhysicsMaterial sphereFriction = new PhysicsMaterial("sphereFriction");
        sphereFriction.dynamicFriction = 0.2f;
        sphereFriction.staticFriction = 0.2f;
        sphereFriction.bounciness = 0.0f;
        sphereFriction.frictionCombine = PhysicsMaterialCombine.Minimum;
        sphereFriction.bounceCombine = PhysicsMaterialCombine.Minimum;
        sc.material = sphereFriction;

        // Rigidbody — original mass 0.65, continuous collision, interpolation
        Rigidbody rb = ball.AddComponent<Rigidbody>();
        rb.mass = 0.65f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Ball casts real-time shadows
        ball.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        ball.GetComponent<Renderer>().receiveShadows = true;

        // Sphere script
        Sphere sphereScript = ball.AddComponent<Sphere>();

        // Find torus
        GameObject torusObj = GameObject.Find("TorusTrack");
        sphereScript.mTorus = torusObj;

        // Assign materials to Torus script
        Torus torusScript = torusObj.GetComponent<Torus>();
        torusScript.mObstacleFront = obstacleFrontMaterial;
        torusScript.mObstacleTop = obstacleTopMaterial;
        torusScript.mObstacleShadow = obstacleShadowMaterial;
        torusScript.mBallTransform = ball.transform;

        // Blitz mode — wire materials and beam system
        if (GameConfig.IsBlitz())
        {
            torusScript.mBlitzBoxMat = blitzBoxMaterial;
            torusScript.mBlitzGateMat = blitzGateMaterial;
            torusScript.mBlitzButtonMat = blitzButtonMaterial;
            torusScript.mBlitzConnectionMat = blitzConnectionMaterial;
            torusScript.mBlitzOrbGunMat = blitzOrbGunMaterial;
            torusScript.mBlitzOrbCadencyMat = blitzOrbCadencyMaterial;
            torusScript.mBlitzOrbShieldMat = blitzOrbShieldMaterial;

            BlitzBeam beam = ball.AddComponent<BlitzBeam>();
            beam.Initialize(beamMaterial, new Color(0.4f, 0.95f, 1f), torusObj.transform, torusScript);
            sphereScript.mBlitzBeam = beam;
            sphereScript.mBlitzShieldMat = blitzShieldVisualMaterial;
            torusScript.mBlitzBeam = beam;
        }
    }

    void CreateCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("MainCamera");
            mainCam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }

        // Position camera inside the tube, behind and above the ball
        // Tube center ring at bottom = (0, -majorRadius, 0) = (0, -10, 0)
        // Tube bottom surface = (0, -(majorRadius+minorRadius), 0) = (0, -11, 0)
        // Ball at (0, -10.9, 0) sits near tube bottom
        // Camera slightly below tube center for best view of track ahead
        mainCam.transform.position = new Vector3(-1.727f, -10.205f, 0);
        mainCam.transform.rotation = new Quaternion(0, 0.707106829f, 0, 0.707106709f);
#if UNITY_IOS && !UNITY_EDITOR
        mainCam.fieldOfView = 55.0f;
#elif UNITY_TVOS && !UNITY_EDITOR
        mainCam.fieldOfView = 70.0f;
#else
        mainCam.fieldOfView = 65.0f;
#endif
        mainCam.nearClipPlane = 0.3f;
        mainCam.allowHDR = true; // Emission values >1.0 render without clamping
        mainCam.backgroundColor = activeTheme.cameraBg;
        mainCam.clearFlags = CameraClearFlags.SolidColor;

        // Camera swing script
        CameraSwing swing = mainCam.gameObject.AddComponent<CameraSwing>();
        swing.mMainCamera = mainCam;

        // Death effect (shake + particles)
        mainCam.gameObject.AddComponent<DeathEffect>();

        // Depth-based hue shift — rainbow color variation over distance
        DepthHueShift depthHue = mainCam.gameObject.AddComponent<DepthHueShift>();
        depthHue.depthShader = depthHueShiftShader;

        // Black hole warp — gravitational lens at top of screen
        BlackHoleWarp warp = mainCam.gameObject.AddComponent<BlackHoleWarp>();
        if (blackHoleWarpRef != null)
            warp.warpShader = blackHoleWarpRef;

        // Wire camera to ball
        GameObject ball = GameObject.Find("Ball");
        swing.ballTransform = ball.transform;
        Sphere sphereScript = ball.GetComponent<Sphere>();
        sphereScript.mCamera = mainCam;
    }

    void CreateLighting()
    {
        // ANALYSIS:
        // Ball at (0, -10.9, 0), track floor at (0, -11, 0), tube center at (0, -10, 0)
        // Camera at (-1.727, -10.205, 0) looking along +X
        // U-shape opens inward (toward 0,0,0), edges at Z=±1
        //
        // For shadows: track mesh is double-sided but set to NOT cast shadows,
        // only receive. So directional shadow map sees the ball unobstructed.
        // Light pointing from torus center outward (toward track floor) = Euler(90, 0, 0)
        // = pointing -Y, so light goes from (0,-10,0) toward (0,-11,0) conceptually.
        // Ball shadow falls onto the track floor between ball and floor.

        // Main shadow-casting light — from inside tube pointing toward track floor
        // Euler(90,0,0) = forward becomes -Y = pointing down toward floor
        GameObject keyObj = new GameObject("KeyLight");
        Light keyLight = keyObj.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.color = new Color(0.95f, 0.92f, 0.85f);
        keyLight.intensity = 0.7f;
        keyLight.shadows = LightShadows.Soft;
        keyLight.shadowStrength = 0.8f;
        keyLight.shadowBias = 0.005f;
        keyLight.shadowNormalBias = 0.05f;
        keyLight.shadowResolution = UnityEngine.Rendering.LightShadowResolution.VeryHigh;
        keyObj.transform.rotation = Quaternion.Euler(90, 0, 0);

        // Fill from ahead along the track — reveals tube depth, no shadows
        GameObject fillObj = new GameObject("FillLight");
        Light fillLight = fillObj.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.color = new Color(0.6f, 0.65f, 0.8f);
        fillLight.intensity = 0.3f;
        fillLight.shadows = LightShadows.None;
        fillObj.transform.rotation = Quaternion.Euler(20, -90, 0);

        // Ambient — low for visible tile shading
        RenderSettings.ambientLight = new Color(0.25f, 0.25f, 0.3f);

        // Reflection probe near the ball — gives chrome ball something to reflect
        GameObject probeObj = new GameObject("ReflectionProbe");
        probeObj.transform.position = new Vector3(0, -10.5f, 0);
        ReflectionProbe probe = probeObj.AddComponent<ReflectionProbe>();
        probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
        probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.EveryFrame;
        probe.size = new Vector3(6, 4, 4);
        probe.resolution = 64; // Low res is fine, just need color hints
    }

    void CreateUI()
    {
        // Hidden TextMesh — Torus writes score here, ScoreSync reads it
        GameObject scoreObj = new GameObject("ScoreTextMesh");
        scoreObj.transform.position = new Vector3(0, -100, 0); // Off-screen
        TextMesh textMesh = scoreObj.AddComponent<TextMesh>();
        textMesh.text = "0";

        // Wire to Torus
        Torus torusScript = GameObject.Find("TorusTrack").GetComponent<Torus>();
        torusScript.mScoreLbl = textMesh;

        // ScoreSync builds its own Canvas + TMP UI
        GameObject guiObj = new GameObject("ScoreUI");
        ScoreSync syncer = guiObj.AddComponent<ScoreSync>();
        syncer.source = textMesh;
    }

    void CreateBackground()
    {
        GameObject bgObj = new GameObject("BackgroundRings");
        BackgroundRings bg = bgObj.AddComponent<BackgroundRings>();
        bg.Setup();
        backgroundRings = bg;

        // Start AUTO crossfader if saved preference is AUTO (default for new players).
        if (ThemeData.LoadSavedIndex() == ThemeData.AUTO_INDEX)
            StartAutoMode();
    }

    void CreateAudio()
    {
        GameObject audioObj = new GameObject("GameAudio");
        audioObj.AddComponent<GameAudio>();
    }

    void CreatePlatformServices()
    {
        // PlatformManager — routes to the active platform service
        GameObject pmObj = new GameObject("PlatformManager");
        PlatformManager pm = pmObj.AddComponent<PlatformManager>();

#if STEAMWORKS
        // Steam builds: SteamManager + SteamService
        GameObject steamObj = new GameObject("SteamManager");
        steamObj.AddComponent<SteamManager>();

        GameObject steamSvcObj = new GameObject("SteamService");
        SteamService steamSvc = steamSvcObj.AddComponent<SteamService>();
        pm.SetService(steamSvc);
#else
        // Apple builds (iOS, tvOS, macOS App Store): GameCenter
        GameObject gcObj = new GameObject("GameCenterManager");
        GameCenterManager gc = gcObj.AddComponent<GameCenterManager>();
        pm.SetService(gc);
#endif
    }

    void CreateDisplaySettings()
    {
#if UNITY_STANDALONE
        GameObject dsObj = new GameObject("DisplaySettings");
        dsObj.AddComponent<DisplaySettings>();
#endif
    }

#if UNITY_EDITOR
    void CreateDebugPanel()
    {
        GameObject dbgObj = new GameObject("DebugPanel");
        dbgObj.AddComponent<DebugPanel>();
    }
#endif

    void CreateSplineTrack()
    {
        GameObject ball = GameObject.Find("Ball");
        Camera cam = Camera.main;

        // Create spline controller
        GameObject splineObj = new GameObject("SplineTrackController");
        SplineGameController splineCtrl = splineObj.AddComponent<SplineGameController>();

        // Wire score output to the same TextMesh that ScoreSync reads
        TextMesh scoreLbl = GameObject.Find("ScoreTextMesh").GetComponent<TextMesh>();
        splineCtrl.scoreLbl = scoreLbl;

        splineCtrl.Initialize(
            trackMaterial, trackMaterial,
            ball.transform, cam,
            obstacleFrontMaterial, obstacleTopMaterial, obstacleShadowMaterial);

        // Wire to Sphere so input uses spline tangent for force direction
        Sphere sphereScript = ball.GetComponent<Sphere>();
        sphereScript.mSplineController = splineCtrl;

        // Hide torus mesh and stop its rotation (keep script alive for Sphere.cs calls)
        GameObject torusObj = GameObject.Find("TorusTrack");
        if (torusObj != null)
        {
            // Hide torus track mesh
            Transform trackChild = torusObj.transform.Find("Psychokinesis3");
            if (trackChild != null) trackChild.gameObject.SetActive(false);

            // Hide edge rails
            foreach (Transform child in torusObj.transform)
            {
                if (child.name == "torusObstacle")
                    child.gameObject.SetActive(false);
            }

            // Pause torus rotation — prevents obstacle generation
            Torus torusScript = torusObj.GetComponent<Torus>();
            torusScript.SetPaused(true);
        }

        // Disable CameraSwing — SplineCameraFollow handles camera in spline mode
        CameraSwing swing = cam.GetComponent<CameraSwing>();
        if (swing != null) swing.enabled = false;

        // Re-initialize RewindSystem for spline mode
        RewindSystem rewind = FindAnyObjectByType<RewindSystem>();
        if (rewind != null)
        {
            rewind.InitializeSpline(
                ball.transform,
                ball.GetComponent<Rigidbody>(),
                splineCtrl,
                trailMaterial,
                cam.transform,
                cam.transform.position,
                cam.transform.rotation);
        }
    }

    void CreateRewindSystem()
    {
        // Blitz has no rewind — no data to collect, no replay to show
        if (GameConfig.IsBlitz()) return;

        GameObject rewindObj = new GameObject("RewindSystem");
        RewindSystem rewind = rewindObj.AddComponent<RewindSystem>();

        GameObject ball = GameObject.Find("Ball");
        GameObject torusObj = GameObject.Find("TorusTrack");

        Sphere sphereScript = ball.GetComponent<Sphere>();
        sphereScript.mRewindSystem = rewind;

        Camera mainCam = Camera.main;
        rewind.Initialize(
            ball.transform,
            ball.GetComponent<Rigidbody>(),
            torusObj.GetComponent<Torus>(),
            torusObj.transform,
            trailMaterial,
            mainCam.transform,
            mainCam.transform.position,
            mainCam.transform.rotation
        );
    }

    Mesh GenerateTorusMesh()
    {
        var verts = new System.Collections.Generic.List<Vector3>();
        var tris = new System.Collections.Generic.List<int>();
        var normals = new System.Collections.Generic.List<Vector3>();
        var colors = new System.Collections.Generic.List<Color>();
        var uvs = new System.Collections.Generic.List<Vector2>();

        for (int i = 0; i < majorSegments; i++)
        {
            float ma0 = ((float)i / majorSegments) * Mathf.PI * 2;
            float ma1 = ((float)(i + 1) / majorSegments) * Mathf.PI * 2;

            // UV: U = major angle normalized to 0..1
            float u0 = (float)i / majorSegments;
            float u1 = (float)(i + 1) / majorSegments;

            for (int j = 0; j < minorSegments; j++)
            {
                float minStart = halfTubeOnly ? -Mathf.PI * 0.5f : 0;
                float minRange = halfTubeOnly ? Mathf.PI : Mathf.PI * 2;

                float mi0 = minStart + ((float)j / minorSegments) * minRange;
                float mi1 = minStart + ((float)(j + 1) / minorSegments) * minRange;

                // UV: V = minor angle normalized to 0..1
                float v0 = (float)j / minorSegments;
                float v1 = (float)(j + 1) / minorSegments;

                Vector3 p00 = TorusPoint(ma0, mi0);
                Vector3 p10 = TorusPoint(ma1, mi0);
                Vector3 p01 = TorusPoint(ma0, mi1);
                Vector3 p11 = TorusPoint(ma1, mi1);

                // Per-tile color variation — visible shade differences
                float variation = Random.Range(-0.15f, 0.15f);
                float tintR = Random.Range(-0.06f, 0.06f);
                float tintB = Random.Range(-0.06f, 0.06f);
                Color tileColor = new Color(1f + variation + tintR, 1f + variation, 1f + variation + tintB);

                // Flat shading: per-face normals
                Vector3 faceNormal1 = Vector3.Cross(p10 - p00, p11 - p00).normalized;
                Vector3 faceNormal2 = Vector3.Cross(p11 - p00, p01 - p00).normalized;

                // Front side (outward facing) — alpha=0 to suppress grid emission
                Color outerColor = new Color(tileColor.r, tileColor.g, tileColor.b, 0f);
                int idx = verts.Count;
                verts.Add(p00); verts.Add(p10); verts.Add(p11);
                normals.Add(faceNormal1); normals.Add(faceNormal1); normals.Add(faceNormal1);
                colors.Add(outerColor); colors.Add(outerColor); colors.Add(outerColor);
                uvs.Add(new Vector2(u0, v0)); uvs.Add(new Vector2(u1, v0)); uvs.Add(new Vector2(u1, v1));
                tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);

                idx = verts.Count;
                verts.Add(p00); verts.Add(p11); verts.Add(p01);
                normals.Add(faceNormal2); normals.Add(faceNormal2); normals.Add(faceNormal2);
                colors.Add(outerColor); colors.Add(outerColor); colors.Add(outerColor);
                uvs.Add(new Vector2(u0, v0)); uvs.Add(new Vector2(u1, v1)); uvs.Add(new Vector2(u0, v1));
                tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);

                // Back side (inward facing) — alpha=1 for full grid, reversed winding for double-sided collision
                idx = verts.Count;
                verts.Add(p00); verts.Add(p11); verts.Add(p10);
                normals.Add(-faceNormal1); normals.Add(-faceNormal1); normals.Add(-faceNormal1);
                colors.Add(tileColor); colors.Add(tileColor); colors.Add(tileColor);
                uvs.Add(new Vector2(u0, v0)); uvs.Add(new Vector2(u1, v1)); uvs.Add(new Vector2(u1, v0));
                tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);

                idx = verts.Count;
                verts.Add(p00); verts.Add(p01); verts.Add(p11);
                normals.Add(-faceNormal2); normals.Add(-faceNormal2); normals.Add(-faceNormal2);
                colors.Add(tileColor); colors.Add(tileColor); colors.Add(tileColor);
                uvs.Add(new Vector2(u0, v0)); uvs.Add(new Vector2(u0, v1)); uvs.Add(new Vector2(u1, v1));
                tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "TorusTrack";
        if (verts.Count > 65535)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.normals = normals.ToArray();
        mesh.colors = colors.ToArray();
        mesh.uv = uvs.ToArray();
        return mesh;
    }

    Vector3 TorusPoint(float majorAngle, float minorAngle)
    {
        float r = torusMajorRadius + torusMinorRadius * Mathf.Cos(minorAngle);
        return new Vector3(
            r * Mathf.Cos(majorAngle),
            r * Mathf.Sin(majorAngle),
            torusMinorRadius * Mathf.Sin(minorAngle)
        );
    }

}
