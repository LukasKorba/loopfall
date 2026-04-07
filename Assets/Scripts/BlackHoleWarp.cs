using UnityEngine;

/// <summary>
/// Screen-space gravitational warp effect.
/// Warps the rendered image toward a focal point so the track
/// appears to converge into the distance rather than cut off at the screen edge.
/// Runs as a post-processing pass after DepthHueShift.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class BlackHoleWarp : MonoBehaviour
{
    [Header("Focal Point (screen UV)")]
    public Vector2 focalPoint = new Vector2(0.5f, 1.06f);

    [Header("Warp")]
    [Range(0f, 0.5f)]
    public float warpStrength = 0.116f;
    [Range(0.1f, 1.5f)]
    public float warpRadius = 0.941f;
    [Range(0.5f, 6f)]
    public float warpFalloff = 6.0f;

    private Material mat;

    [HideInInspector]
    public Shader warpShader;

    void EnsureMaterial()
    {
        if (mat != null) return;

        if (warpShader == null)
            warpShader = Shader.Find("Loopfall/BlackHoleWarp");

        if (warpShader != null)
            mat = new Material(warpShader);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        EnsureMaterial();
        if (mat == null)
        {
            Graphics.Blit(src, dst);
            return;
        }

        mat.SetFloat("_FocalPointX", focalPoint.x);
        mat.SetFloat("_FocalPointY", focalPoint.y);
        mat.SetFloat("_WarpStrength", warpStrength);
        mat.SetFloat("_WarpRadius", warpRadius);
        mat.SetFloat("_WarpFalloff", warpFalloff);

        Graphics.Blit(src, dst, mat);
    }
}
