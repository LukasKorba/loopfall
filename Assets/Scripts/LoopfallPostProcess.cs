using UnityEngine;

/// <summary>
/// Merged post-process: BlackHoleWarp + DepthHueShift in one OnRenderImage pass.
/// Replaces the two separate components that cost ~15-20% frame time on A10 due
/// to the back-to-back full-screen blits.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class LoopfallPostProcess : MonoBehaviour
{
    [Header("Hue Shift")]
    [Range(0f, 1f)] public float hueShiftAmount = 0.4f;
    public float depthStart = 1.0f;
    public float depthEnd = 12.0f;
    [Range(0f, 0.5f)] public float saturationBoost = 0.15f;

    [Header("Fog")]
    public Color fogColor = new Color(0.12f, 0.04f, 0.2f, 1f);
    [Range(0f, 1f)] public float fogAmount = 0.55f;
    public float fogStart = 3.0f;
    public float fogEnd = 15.0f;

    [Header("Black Hole Warp")]
    public Vector2 focalPoint = new Vector2(0.5f, 1.06f);
    [Range(0f, 0.5f)] public float warpStrength = 0.116f;
    [Range(0.1f, 1.5f)] public float warpRadius = 0.941f;
    [Range(0.5f, 6f)] public float warpFalloff = 6.0f;

    [HideInInspector] public Shader postShader;

    private Material mat;
    private Camera cam;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        cam.depthTextureMode |= DepthTextureMode.Depth;
    }

    void EnsureMaterial()
    {
        if (mat != null) return;

        if (postShader == null)
            postShader = Shader.Find("Loopfall/PostProcess");

        if (postShader != null)
            mat = new Material(postShader);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        EnsureMaterial();
        if (mat == null)
        {
            Graphics.Blit(src, dst);
            return;
        }

        mat.SetFloat("_HueShiftAmount", hueShiftAmount);
        mat.SetFloat("_DepthStartWorld", depthStart);
        mat.SetFloat("_DepthEndWorld", depthEnd);
        mat.SetFloat("_Saturation", saturationBoost);
        mat.SetColor("_FogColor", fogColor);
        mat.SetFloat("_FogAmount", fogAmount);
        mat.SetFloat("_FogStart", fogStart);
        mat.SetFloat("_FogEnd", fogEnd);

        mat.SetFloat("_FocalPointX", focalPoint.x);
        mat.SetFloat("_FocalPointY", focalPoint.y);
        mat.SetFloat("_WarpStrength", warpStrength);
        mat.SetFloat("_WarpRadius", warpRadius);
        mat.SetFloat("_WarpFalloff", warpFalloff);

        Graphics.Blit(src, dst, mat);
    }
}
