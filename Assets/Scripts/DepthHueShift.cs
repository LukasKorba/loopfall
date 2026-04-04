using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class DepthHueShift : MonoBehaviour
{
    [Header("Hue Shift")]
    [Range(0f, 1f)]
    public float hueShiftAmount = 0.4f;
    public float depthStart = 1.0f;
    public float depthEnd = 12.0f;
    [Range(0f, 0.5f)]
    public float saturationBoost = 0.15f;

    [Header("Fog")]
    public Color fogColor = new Color(0.12f, 0.04f, 0.2f, 1f);
    [Range(0f, 1f)]
    public float fogAmount = 0.55f;
    public float fogStart = 3.0f;
    public float fogEnd = 15.0f;

    private Material mat;

    [HideInInspector]
    public Shader depthShader;

    private Camera cam;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        cam.depthTextureMode |= DepthTextureMode.Depth;
    }

    void OnPreRender()
    {
        cam.depthTextureMode |= DepthTextureMode.Depth;
    }

    void EnsureMaterial()
    {
        if (mat != null) return;

        if (depthShader == null)
            depthShader = Shader.Find("Loopfall/DepthHueShift");

        if (depthShader != null)
            mat = new Material(depthShader);
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

        Graphics.Blit(src, dst, mat);
    }
}
