using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// GRAPHICS PRESETS: LOW / MEDIUM / HIGH (Settings > Graphics, or the pause menu).
//
//   LOW     for school laptops and slow computers: draws the game at a lower resolution and stretches it,
//           no shadows, no grass, trees and far-away stuff stop sooner (hidden in the fog), fewer lights.
//   MEDIUM  a bit of everything.
//   HIGH    full resolution, long shadows, lots of grass, see across the whole map.
//
// The first time the game runs it guesses the right one from the graphics card.
public static class GraphicsQuality
{
    public const int Low = 0, Medium = 1, High = 2;
    public static readonly string[] Names = { "LOW", "MEDIUM", "HIGH" };
    public static readonly string[] Hints = { "for school laptops / slow PCs", "balanced", "best looking" };

    public static int Level { get; private set; } = Medium;

    // camera view distance for each level (fog hides where it stops)
    public static float FarClip => Level == Low ? 240f : Level == Medium ? 420f : 900f;

    struct Saved { public float scale, shadow; public int msaa, lights; }
    static readonly Dictionary<UniversalRenderPipelineAsset, Saved> original = new Dictionary<UniversalRenderPipelineAsset, Saved>();
    static bool hooked;
    static float fogStart = -1f, fogEnd = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { original.Clear(); hooked = false; fogStart = fogEnd = -1f; }

    // A first guess for computers that have never run the game.
    public static int Detect()
    {
        int vram = SystemInfo.graphicsMemorySize;            // MB
        string gpu = (SystemInfo.graphicsDeviceName ?? "").ToLowerInvariant();
        bool integrated = (gpu.Contains("intel") && !gpu.Contains("arc")) || gpu.Contains("uhd") || gpu.Contains("iris") ||
                          (gpu.Contains("radeon") && (gpu.Contains("vega") || gpu.Contains("graphics")) && !gpu.Contains("rx"));
        if (integrated || vram < 1800 || SystemInfo.processorCount <= 2) return Low;
        if (vram < 4500) return Medium;
        return High;
    }

    public static void Apply(int level)
    {
        Level = Mathf.Clamp(level, Low, High);

        // Unity's own quality level: lowest for LOW, highest otherwise
        int n = QualitySettings.names.Length;
        if (n > 0)
        {
            int q = Level == Low ? 0 : n - 1;
            if (QualitySettings.GetQualityLevel() != q) QualitySettings.SetQualityLevel(q, true);
        }

        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp != null)
        {
            if (!original.ContainsKey(urp))
                original[urp] = new Saved { scale = urp.renderScale, shadow = urp.shadowDistance, msaa = urp.msaaSampleCount, lights = urp.maxAdditionalLightsCount };
            if (!hooked) { hooked = true; Application.quitting += Restore; }
            urp.renderScale = Level == Low ? 0.6f : Level == Medium ? 0.85f : 1f;     // fewer pixels = much faster
            urp.shadowDistance = Level == Low ? 0f : Level == Medium ? 35f : 90f;      // 0 = no shadows
            urp.msaaSampleCount = Level == High ? 4 : 1;
            urp.maxAdditionalLightsCount = Level == Low ? 1 : Level == Medium ? 4 : 8;
        }
        QualitySettings.vSyncCount = 1;
        ApplyScene();
    }

    // The map and camera (called again whenever a match starts, in case things were rebuilt).
    public static void ApplyScene()
    {
        foreach (Terrain t in Terrain.activeTerrains)
        {
            if (t == null) continue;
            t.detailObjectDistance = Level == Low ? 0f : Level == Medium ? 45f : 75f;     // grass
            t.detailObjectDensity = Level == Low ? 0f : Level == Medium ? 0.55f : 0.9f;
            t.treeDistance = Level == Low ? 150f : Level == Medium ? 260f : 400f;
            t.treeBillboardDistance = t.treeDistance;
            t.heightmapPixelError = Level == Low ? 14f : Level == Medium ? 7f : 3f;
            t.basemapDistance = Level == Low ? 40f : Level == Medium ? 120f : 300f;
            t.shadowCastingMode = Level == Low ? ShadowCastingMode.Off : ShadowCastingMode.On;
        }
        if (fogEnd < 0f) { fogStart = RenderSettings.fogStartDistance; fogEnd = RenderSettings.fogEndDistance; }
        if (RenderSettings.fog)
        {
            // pull the fog in so the shorter view distance doesn't show
            RenderSettings.fogEndDistance = Mathf.Min(fogEnd, FarClip * 0.92f);
            RenderSettings.fogStartDistance = Mathf.Min(fogStart, RenderSettings.fogEndDistance * 0.35f);
        }
        if (GameCam.I != null && GameCam.I.cam != null) GameCam.I.cam.farClipPlane = FarClip;
        else if (Camera.main != null) Camera.main.farClipPlane = FarClip;
    }

    // In the Unity editor, put the project's render settings back when you stop playing
    // (so pressing Play doesn't permanently change your project).
    static void Restore()
    {
        foreach (var kv in original)
        {
            if (kv.Key == null) continue;
            kv.Key.renderScale = kv.Value.scale;
            kv.Key.shadowDistance = kv.Value.shadow;
            kv.Key.msaaSampleCount = kv.Value.msaa;
            kv.Key.maxAdditionalLightsCount = kv.Value.lights;
        }
        original.Clear();
    }
}
