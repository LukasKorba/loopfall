using UnityEngine;
using System.Collections.Generic;

public class Obstacle
{
    public GameObject mGameObject = null;
    public float mAngle = float.MaxValue;
    public Obstacle mNextOne = null;

    public Obstacle(int gapSizeFrom, int gapSizeTo, float obstacleStepInv, Material obstacleTop, Material obstacleFront, Material obstacleShadow,
                    bool avoidCenter = false)
    {
        mGameObject = new GameObject("torusObstacle");

        // MESH
        MeshFilter meshFilter = mGameObject.AddComponent<MeshFilter>();

        int gapSize = Random.Range(gapSizeFrom, gapSizeTo);

        // Ensure gap origin leaves at least 3° of wall on both sides
        int originMin = gapSize + 3;
        int originMax = 180 - gapSize - 2;
        if (originMax <= originMin) originMax = originMin + 1;

        float gapOrigin;
        if (avoidCenter)
        {
            // Force gap to left or right side — never at center (90°)
            // Pick from [originMin..65] or [115..originMax]
            if (Random.value < 0.5f)
                gapOrigin = (float)Random.Range(originMin, Mathf.Min(65, originMax));
            else
                gapOrigin = (float)Random.Range(Mathf.Max(115, originMin), originMax);
        }
        else
        {
            gapOrigin = (float)Random.Range(originMin, originMax);
        }

        Mesh shadowMesh = null;
        Mesh mesh = generateObstacleMeshGap(
            gapOrigin,
            (float)gapSize,
            obstacleStepInv,
            out shadowMesh
        );
        meshFilter.mesh = mesh;

        // COLLIDER
        MeshCollider collider = mGameObject.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;

        // RENDERER
        MeshRenderer renderer = mGameObject.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Material[] obstacleMaterials = new Material[2];
        obstacleMaterials[0] = obstacleFront;
        obstacleMaterials[1] = obstacleTop;
        renderer.materials = obstacleMaterials;

        // Shadow on track surface (extends toward camera)
        GameObject shadow = new GameObject("torusObstacleShadow");
        MeshFilter meshFilterShadow = shadow.AddComponent<MeshFilter>();
        meshFilterShadow.mesh = shadowMesh;

        MeshRenderer rendererShadow = shadow.AddComponent<MeshRenderer>();
        rendererShadow.material = obstacleShadow;
        rendererShadow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        shadow.transform.parent = mGameObject.transform;

        // Dark base edge — thin strip on the front face at the base, facing toward camera
        GameObject baseEdge = new GameObject("torusObstacleBase");
        Mesh baseMesh = generateBaseMesh(
            gapOrigin,
            (float)gapSize,
            obstacleStepInv
        );
        MeshFilter baseMF = baseEdge.AddComponent<MeshFilter>();
        baseMF.mesh = baseMesh;
        MeshRenderer baseMR = baseEdge.AddComponent<MeshRenderer>();
        baseMR.material = obstacleShadow;
        baseMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        baseEdge.transform.parent = mGameObject.transform;
    }

    Mesh generateBaseMesh(float origin, float halfSize, float obstacleStepInv)
    {
        // Dark strip along the front face base of the obstacle
        var verts = new List<Vector3>();
        var tris = new List<int>();

        float from = origin - halfSize;
        float to = origin + halfSize;

        if (from > 0.0f)
            generateBaseStrip(0.0f * Mathf.Deg2Rad, from * Mathf.Deg2Rad, obstacleStepInv, verts, tris);
        if (to < 180.0f)
            generateBaseStrip(to * Mathf.Deg2Rad, 180.0f * Mathf.Deg2Rad, obstacleStepInv, verts, tris);

        Mesh mesh = new Mesh();
        mesh.name = "obstacleBase";
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }

    void generateBaseStrip(float from, float to, float obstacleStepInv, List<Vector3> verts, List<int> tris)
    {
        float diff = to - from;
        int steps = (int)Mathf.Ceil(diff * obstacleStepInv) + 2;
        float realStep = diff / (float)(steps - 1);
        float baseHeight = 0.04f; // Height of dark edge strip

        bool first = true;
        int startIdx = verts.Count;

        for (int i = 0; i < steps; i++)
        {
            float y = Mathf.Sin(from);
            float z = Mathf.Cos(from);

            // Bottom of obstacle (on torus surface)
            Vector3 bottom = new Vector3(0.0f, -10.0f - y, -z);
            // Slightly above bottom on the front face
            Vector3 top = new Vector3(0.0f, -10.0f - (y * (1.0f - baseHeight)), -z * (1.0f - baseHeight));

            // Push slightly forward (toward camera) to avoid Z-fighting
            Vector3 offset = new Vector3(-0.003f, 0, 0);
            verts.Add(bottom + offset);
            verts.Add(top + offset);

            if (!first)
            {
                int idx = startIdx + (i * 2) - 2;
                tris.Add(idx);
                tris.Add(idx + 1);
                tris.Add(idx + 2);
                tris.Add(idx + 2);
                tris.Add(idx + 1);
                tris.Add(idx + 3);
            }

            first = false;
            from += realStep;
        }
    }

    Mesh generateObstacleMeshGap(float origin, float halfSize, float obstacleStepInv, out Mesh shadowMesh)
    {
        Mesh mesh = new Mesh();
        mesh.subMeshCount = 2;
        mesh.name = "torusObstacleMeshGap";

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> UVs = new List<Vector2>();
        List<int> triangles = new List<int>();
        List<int> triangles2 = new List<int>();

        List<Vector3> verticesShadow = new List<Vector3>();
        List<Vector2> UVsShadow = new List<Vector2>();
        List<int> trianglesShadow = new List<int>();

        float from = origin - halfSize;
        float to = origin + halfSize;

        if (from > 0.0f)
            generateVerticesFromTo(0.0f * Mathf.Deg2Rad, from * Mathf.Deg2Rad, vertices, triangles, UVs, triangles2, verticesShadow, trianglesShadow, UVsShadow, obstacleStepInv);
        if (to < 180.0f)
            generateVerticesFromTo(to * Mathf.Deg2Rad, 180.0f * Mathf.Deg2Rad, vertices, triangles, UVs, triangles2, verticesShadow, trianglesShadow, UVsShadow, obstacleStepInv);

        mesh.vertices = vertices.ToArray();
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(triangles2.ToArray(), 1);
        mesh.uv = UVs.ToArray();
        mesh.Optimize();
        mesh.RecalculateNormals();

        shadowMesh = new Mesh();
        shadowMesh.name = "torusObstacleMeshGapShadow";
        shadowMesh.vertices = verticesShadow.ToArray();
        shadowMesh.triangles = trianglesShadow.ToArray();
        shadowMesh.uv = UVsShadow.ToArray();
        shadowMesh.Optimize();
        shadowMesh.RecalculateNormals();

        return mesh;
    }

    void generateVerticesFromTo(float from, float to, List<Vector3> vertices, List<int> triangles, List<Vector2> UVs, List<int> triangles2, List<Vector3> verticesShadow, List<int> trianglesShadow, List<Vector2> UVsShadow, float obstacleStepInv)
    {
        float diff = to - from;
        int stepsCount = (int)Mathf.Ceil(diff * obstacleStepInv);
        float sizeofObstacle = 0.1f;

        stepsCount++;
        float realStep = diff / (float)stepsCount;
        stepsCount++;

        List<Vector3> verticesHelper = new List<Vector3>();
        List<int> trianglesHelper = new List<int>();
        List<Vector2> UVsHelper = new List<Vector2>();

        List<Vector3> verticesHelper2 = new List<Vector3>();
        List<int> trianglesHelper2 = new List<int>();
        List<Vector2> UVsHelper2 = new List<Vector2>();

        bool firstCycle = true;
        int since = vertices.Count / 2;
        int ceilSince = stepsCount + since;
        int sinceShadow = verticesShadow.Count / 2;

        for (int i = 0; i < stepsCount; ++i)
        {
            // FRONT SIDE
            float y = Mathf.Sin(from);
            float z = Mathf.Cos(from);

            Vector3 upperVertex = new Vector3(0.0f, -10.0f - (y * 0.936f), -z * 0.936f);
            vertices.Add(upperVertex);
            Vector3 bottomVertex = new Vector3(0.0f, -10.0f - y, -z);
            vertices.Add(bottomVertex);

            if (!firstCycle)
            {
                int realI = ((since + i) * 2) - 2;
                triangles.Add(realI);
                triangles.Add(realI + 1);
                triangles.Add(realI + 2);
                triangles.Add(realI + 2);
                triangles.Add(realI + 1);
                triangles.Add(realI + 3);
            }

            UVs.Add(new Vector2(0.0f, 0.0f));
            UVs.Add(new Vector2(0.0f, 0.0f));

            // CEIL
            Vector3 farVertex = new Vector3(sizeofObstacle, -10.0f - (y * 0.916f), -z * 0.916f);
            verticesHelper.Add(farVertex);
            verticesHelper.Add(upperVertex);

            if (!firstCycle)
            {
                int realI = ((ceilSince + i) * 2) - 2;
                trianglesHelper.Add(realI);
                trianglesHelper.Add(realI + 1);
                trianglesHelper.Add(realI + 2);
                trianglesHelper.Add(realI + 2);
                trianglesHelper.Add(realI + 1);
                trianglesHelper.Add(realI + 3);
            }

            UVsHelper.Add(new Vector2(0.0f, 0.0f));
            UVsHelper.Add(new Vector2(0.0f, 0.0f));

            // SHADOW — extends toward camera (negative X) from obstacle base
            // Sits just above torus surface to avoid Z-fighting
            float shadowOffset = 0.008f;
            float shadowWidth = 0.25f; // Wide enough to be clearly visible

            // Surface normal at this point (radial outward from torus center)
            Vector3 surfaceNormal = new Vector3(0.0f, -y, -z).normalized;

            // Inner edge: at obstacle base
            Vector3 shadowInner = bottomVertex + surfaceNormal * shadowOffset;
            // Outer edge: extends toward camera along track
            Vector3 shadowOuter = new Vector3(bottomVertex.x - shadowWidth, bottomVertex.y, bottomVertex.z) + surfaceNormal * shadowOffset;

            verticesShadow.Add(shadowInner);
            verticesShadow.Add(shadowOuter);

            if (!firstCycle)
            {
                int realI = ((sinceShadow + i) * 2) - 2;
                trianglesShadow.Add(realI);
                trianglesShadow.Add(realI + 1);
                trianglesShadow.Add(realI + 2);
                trianglesShadow.Add(realI + 2);
                trianglesShadow.Add(realI + 1);
                trianglesShadow.Add(realI + 3);
            }

            UVsShadow.Add(new Vector2(0.0f, 1.0f));
            UVsShadow.Add(new Vector2(0.0f, 0.0f));

            // SIDES
            if (i == 0)
            {
                Vector3 farBottomVertex = new Vector3(bottomVertex.x + sizeofObstacle, bottomVertex.y, bottomVertex.z);
                verticesHelper2.Add(upperVertex);
                verticesHelper2.Add(farVertex);
                verticesHelper2.Add(bottomVertex);
                verticesHelper2.Add(farBottomVertex);

                int realI = ((ceilSince + stepsCount) * 2);
                trianglesHelper2.Add(realI);
                trianglesHelper2.Add(realI + 1);
                trianglesHelper2.Add(realI + 2);
                trianglesHelper2.Add(realI + 2);
                trianglesHelper2.Add(realI + 1);
                trianglesHelper2.Add(realI + 3);

                UVsHelper2.Add(new Vector2(0.0f, 0.0f));
                UVsHelper2.Add(new Vector2(0.0f, 0.0f));
                UVsHelper2.Add(new Vector2(0.0f, 0.0f));
                UVsHelper2.Add(new Vector2(0.0f, 0.0f));
            }
            else if (i == stepsCount - 1)
            {
                Vector3 farBottomVertex = new Vector3(bottomVertex.x + sizeofObstacle, bottomVertex.y, bottomVertex.z);
                verticesHelper2.Add(upperVertex);
                verticesHelper2.Add(farVertex);
                verticesHelper2.Add(bottomVertex);
                verticesHelper2.Add(farBottomVertex);

                int realI = ((ceilSince + stepsCount) * 2) + 4;
                trianglesHelper2.Add(realI);
                trianglesHelper2.Add(realI + 2);
                trianglesHelper2.Add(realI + 1);
                trianglesHelper2.Add(realI + 1);
                trianglesHelper2.Add(realI + 2);
                trianglesHelper2.Add(realI + 3);

                UVsHelper2.Add(new Vector2(0.0f, 0.0f));
                UVsHelper2.Add(new Vector2(0.0f, 0.0f));
                UVsHelper2.Add(new Vector2(0.0f, 0.0f));
                UVsHelper2.Add(new Vector2(0.0f, 0.0f));
            }

            firstCycle = false;
            from += realStep;
        }

        vertices.AddRange(verticesHelper);
        triangles2.AddRange(trianglesHelper);
        UVs.AddRange(UVsHelper);

        vertices.AddRange(verticesHelper2);
        triangles.AddRange(trianglesHelper2);
        UVs.AddRange(UVsHelper2);
    }
}
