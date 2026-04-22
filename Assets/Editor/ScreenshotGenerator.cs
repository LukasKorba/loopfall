using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only tool that captures the current Main Camera view at each store's
/// expected landscape screenshot resolution, writing PNGs into a timestamped
/// folder on the Desktop. Intended workflow:
///   1) Press Play, play until a frame looks good.
///   2) Pause the editor.
///   3) Invoke "Loopfall/Screenshots/Capture Paused Frame" (shortcut: Cmd+Shift+9).
///
/// Each preset allocates its own RenderTexture and calls Camera.Render(), so
/// post-processing (chromatic aberration, LoopfallPostProcess, etc.)
/// runs at the target resolution rather than being scaled up from the viewport.
/// Aspect ratio differs per target, so the framing will not be identical across
/// presets from one capture; that's expected — re-pose and re-capture if a
/// specific aspect needs tuning.
/// </summary>
public static class ScreenshotGenerator
{
    struct Preset
    {
        public string Name;
        public int Width;
        public int Height;
    }

    private static readonly Preset[] PRESETS =
    {
        new Preset { Name = "apple-iphone-6_5", Width = 2778, Height = 1284 },
        new Preset { Name = "apple-ipad-13",    Width = 2732, Height = 2048 },
        new Preset { Name = "apple-tvos-4k",    Width = 3840, Height = 2160 },
        new Preset { Name = "apple-macos",      Width = 2880, Height = 1800 },
        new Preset { Name = "google-phone",     Width = 1920, Height = 1080 },
        new Preset { Name = "google-tablet-10", Width = 2560, Height = 1600 },
        new Preset { Name = "steam",            Width = 1920, Height = 1080 },
    };

    // %#9 = Cmd+Shift+9 on macOS, Ctrl+Shift+9 on Windows.
    [MenuItem("Loopfall/Screenshots/Capture Paused Frame %#9")]
    public static void Capture()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Screenshot] Enter Play mode and pause at the frame you want to capture.");
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("[Screenshot] No Camera.main found in the scene.");
            return;
        }

        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string outDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "Loopfall-Screenshots",
            stamp);
        Directory.CreateDirectory(outDir);

        RenderTexture prevTarget = cam.targetTexture;
        RenderTexture prevActive = RenderTexture.active;
        int aa = Mathf.Max(1, QualitySettings.antiAliasing);

        try
        {
            foreach (Preset p in PRESETS)
            {
                // HDR format preserves emission > 1.0 for the neon look; converted on PNG encode.
                RenderTexture rt = new RenderTexture(p.Width, p.Height, 24, RenderTextureFormat.DefaultHDR);
                rt.antiAliasing = aa;
                rt.Create();

                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(p.Width, p.Height, TextureFormat.RGB24, false, false);
                tex.ReadPixels(new Rect(0, 0, p.Width, p.Height), 0, 0);
                tex.Apply();

                string file = Path.Combine(outDir, p.Name + "_" + p.Width + "x" + p.Height + ".png");
                File.WriteAllBytes(file, tex.EncodeToPNG());
                Debug.Log("[Screenshot] " + file);

                UnityEngine.Object.DestroyImmediate(tex);
                RenderTexture.active = null;
                cam.targetTexture = null;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }
        finally
        {
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
        }

        Debug.Log("[Screenshot] Wrote " + PRESETS.Length + " files to " + outDir);
        EditorUtility.RevealInFinder(outDir);
    }
}
