using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Adds "Tools > Mad Lab > Build Game (.exe)" - makes a Windows version of Mad Lab you can run without Unity.
// It ends up in the "Builds/Mad Lab" folder next to Assets. Zip that whole folder to share or download it.
//
// Also keeps a tiny glowing material in Assets/Resources/KeepForBuild. The game turns on glow
// (potions, crystals, plasma, lab lights) from code, and a built game only includes the shader
// features some saved material uses - this material makes sure "glow" is included.
[InitializeOnLoad]
public static class BuildGameMenu
{
    const string KeepFolder = "Assets/Resources/KeepForBuild";
    const string GlowMatPath = KeepFolder + "/Lit_Emission.mat";
    const string BaseMatPath = KeepFolder + "/Lit_Base.mat";
    const string OutputFolder = "Builds/Mad Lab";
    const string SeeThroughPath = KeepFolder + "/SeeThrough.mat";

    static BuildGameMenu()
    {
        EditorApplication.delayCall += EnsureGlowMaterial;
        EditorApplication.delayCall += KeepFogInBuilds;
        EditorApplication.delayCall += ApplyIcon;
        EditorApplication.delayCall += ApplySplash;
    }

    // The game's picture (the glowing flask) - shown for Mad Lab.exe in File Explorer, on the taskbar and the window.
    const string IconPath = "Assets/MadLab/Icon/MadLab_Icon.png";

    static void ApplyIcon()
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (tex == null) return;
        Texture2D[] now = PlayerSettings.GetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, IconKind.Any);
        if (now != null && now.Length > 0 && now[0] == tex) return;
        PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { tex }, IconKind.Any);
        AssetDatabase.SaveAssets();
        Debug.Log("Mad Lab icon set (Edit > Project Settings > Player > Icon).");
    }

    // STARTUP SPLASH: what you see when Mad Lab.exe opens - the Mad Lab logo fading in on a dark background,
    // instead of the plain Unity one. (Edit > Project Settings > Player > Splash Image to change it.)
    const string SplashPath = "Assets/MadLab/Icon/MadLab_Splash.png";

    static void ApplySplash()
    {
        var imp = AssetImporter.GetAtPath(SplashPath) as TextureImporter;
        if (imp == null) return;
        if (imp.textureType != TextureImporterType.Sprite)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        Sprite logo = AssetDatabase.LoadAssetAtPath<Sprite>(SplashPath);
        if (logo == null) return;
        PlayerSettings.SplashScreen.show = true;
        PlayerSettings.SplashScreen.showUnityLogo = false;            // allowed on Unity 6 (if your licence needs it, Unity keeps it)
        PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
        PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;   // slow zoom
        PlayerSettings.SplashScreen.backgroundColor = new Color(0.02f, 0.04f, 0.05f);
        PlayerSettings.SplashScreen.overlayOpacity = 0f;
        PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(3f, logo) };
    }

    // The game turns fog on from code (the hazy distance on the arena). Unity's "Automatic" setting removes
    // fog from the built game if the saved maps don't use it - so keep every fog type in the build.
    static void KeepFogInBuilds()
    {
        Object gs = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
        if (gs == null) return;
        var so = new SerializedObject(gs);
        SerializedProperty strip = so.FindProperty("m_FogStripping");
        if (strip == null) return;
        bool changed = strip.intValue != 1;
        strip.intValue = 1;   // Custom
        foreach (string k in new[] { "m_FogKeepLinear", "m_FogKeepExp", "m_FogKeepExp2" })
        {
            SerializedProperty p = so.FindProperty(k);
            if (p != null && !p.boolValue) { p.boolValue = true; changed = true; }
        }
        if (changed)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log("Fog will now be included in the built game.");
        }
    }

    // Saved URP materials the game copies at runtime. Being saved (and in Resources) is what gets
    // the URP shader and its "glow" feature included in the built game, so nothing turns pink.
    static void EnsureGlowMaterial()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) return;
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(KeepFolder)) AssetDatabase.CreateFolder("Assets/Resources", "KeepForBuild");

        bool made = false;
        if (AssetDatabase.LoadAssetAtPath<Material>(BaseMatPath) == null)
        {
            AssetDatabase.CreateAsset(new Material(lit), BaseMatPath);
            made = true;
        }
        if (AssetDatabase.LoadAssetAtPath<Material>(GlowMatPath) == null)
        {
            var m = new Material(lit);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.white);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            AssetDatabase.CreateAsset(m, GlowMatPath);
            made = true;
        }
        // forcefields, the zone wall, sparks and beams use this see-through shader
        Shader sprite = Shader.Find("Sprites/Default");
        if (sprite != null && AssetDatabase.LoadAssetAtPath<Material>(SeeThroughPath) == null)
        {
            AssetDatabase.CreateAsset(new Material(sprite), SeeThroughPath);
            made = true;
        }
        if (made) AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Mad Lab/Build Game (.exe)", priority = 50)]
    static void Build()
    {
        if (Object.FindAnyObjectByType<Laboratory>() == null &&
            !EditorUtility.DisplayDialog("No arena in this scene", "This scene has no Lab Arena, so PLAY won't work in the built game. Run Tools > Mad Lab > 2. Create Lab Arena first.\n\nBuild anyway?", "Build anyway", "Cancel"))
            return;
        if (PlayerSettings.productName == "My project") PlayerSettings.productName = MainMenu.GameTitle == "MAD LAB" ? "Mad Lab" : MainMenu.GameTitle;
        EnsureGlowMaterial();
        KeepFogInBuilds();
        ApplyIcon();
        ApplySplash();
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // Every map in the map list (File > Build Profiles > Scene List).
        var scenes = new List<string>();
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            if (s.enabled && File.Exists(s.path)) scenes.Add(s.path);
        if (scenes.Count == 0)
        {
            string current = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(current))
            {
                EditorUtility.DisplayDialog("Build Game", "Save your scene first (Ctrl+S), then try again.", "OK");
                return;
            }
            scenes.Add(current);
        }

        string exe = OutputFolder + "/" + PlayerSettings.productName + ".exe";
        var options = new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = exe,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            float mb = report.summary.totalSize / (1024f * 1024f);
            Debug.Log("Mad Lab built: " + Path.GetFullPath(exe) + " (" + mb.ToString("0") + " MB)");
            EditorUtility.RevealInFinder(exe);
        }
        else
        {
            EditorUtility.DisplayDialog("Build failed",
                "The build didn't finish (" + report.summary.result + "). Check the Console for red errors.", "OK");
        }
    }
}
