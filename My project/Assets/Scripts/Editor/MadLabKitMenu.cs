using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Adds "Tools > Mad Lab" to the Unity menu bar (same idea as the map kits in Arena Survival).
//
//   1. Make Lab Kit       - turns the pictures in Assets/MadLab/Textures into things you can build maps with:
//                           ground paints (Terrain Layers), trees, bushes, giant mushrooms, glowing mutant trees,
//                           rocks, crystals, crates, barrels, fences, pipes, lamps, a reactor... plus the
//                           material spots (Node_Herb, Node_Crystal, ...) and the LABORATORY.
//                           Everything lands in Assets/MadLab/Prefabs - drag them into the scene anywhere.
//   2. Create Lab Arena   - builds the whole arena in your scene: a 200 x 200 m Terrain with a big field in the
//                           middle, hills around the edge, a pond, a toxic swamp, a crystal crag, forests,
//                           paths, 8 laboratories in a ring, material spots, target dummies and markers.
//                           Then edit it however you like: Terrain tools to sculpt / paint / plant trees,
//                           move the labs, add or remove material spots. Press Play and hit PLAY.
public static partial class MadLabKitMenu
{
    const string Root = "Assets/MadLab";
    static string TexDir => Root + "/Textures";
    static string LayerDir => Root + "/Terrain Layers";
    static string MatDir => Root + "/Materials";
    static string MeshDir => Root + "/Meshes";
    static string PrefabDir => Root + "/Prefabs";

    // Ground paints: texture name, how many metres one copy of the picture covers.
    static readonly string[] LayerNames = { "Grass_Lab", "Grass_Field", "Dirt", "Gravel", "Rock", "Toxic" };
    static readonly float[] LayerTiles = { 6f, 7f, 5f, 3f, 6f, 6f };

    // Grass you paint with "Paint Details".
    static readonly string[] DetailNames = { "GrassBlades", "GrassBlades_Dry", "Flowers" };

    // Prefabs you paint with "Paint Trees".
    static readonly string[] TreeNames = { "Oak_A", "Oak_B", "Pine_A", "Pine_B", "GlowTree_A", "GlowTree_B", "Bush_A", "Bush_B", "Mushroom_Giant" };

    // ================================================================
    // 1. Make Lab Kit
    // ================================================================

    [MenuItem("Tools/Mad Lab/1. Make Lab Kit", priority = 1)]
    public static void MakeKit()
    {
        if (!AssetDatabase.IsValidFolder(TexDir) || AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/Grass_Lab.png") == null)
        {
            EditorUtility.DisplayDialog("Textures missing", "Couldn't find the pictures in " + TexDir + ". They should be in that folder already.", "OK");
            return;
        }
        try
        {
            EditorUtility.DisplayProgressBar("Mad Lab Kit", "Setting up textures", 0.05f);
            FixTextureImports();
            MakeFolder(LayerDir); MakeFolder(MatDir); MakeFolder(MeshDir); MakeFolder(PrefabDir);

            EditorUtility.DisplayProgressBar("Mad Lab Kit", "Making ground paints", 0.15f);
            for (int i = 0; i < LayerNames.Length; i++) MakeTerrainLayer(LayerNames[i], LayerTiles[i]);

            EditorUtility.DisplayProgressBar("Mad Lab Kit", "Mixing materials", 0.2f);
            MakeMaterials();

            EditorUtility.DisplayProgressBar("Mad Lab Kit", "Growing trees and mushrooms", 0.3f);
            MakeNature();

            EditorUtility.DisplayProgressBar("Mad Lab Kit", "Building props", 0.5f);
            MakeProps();

            EditorUtility.DisplayProgressBar("Mad Lab Kit", "Making material spots", 0.65f);
            MakeNodes();

            EditorUtility.DisplayProgressBar("Mad Lab Kit", "Building the reactor", 0.75f);
            MakeReactor();

            EditorUtility.DisplayProgressBar("Mad Lab Kit", "Building the LABORATORY", 0.85f);
            MakeLaboratory();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally { EditorUtility.ClearProgressBar(); }

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(PrefabDir);
        EditorGUIUtility.PingObject(Selection.activeObject);
        Debug.Log("Mad Lab Kit ready! Look in " + PrefabDir + ". Next: Tools > Mad Lab > 2. Create Lab Arena.");
    }

    [MenuItem("Tools/Mad Lab/Help", priority = 30)]
    static void Help()
    {
        EditorUtility.DisplayDialog("Mad Lab map tools",
            "1. Make Lab Kit - builds all the models (trees, props, material spots, the Laboratory) into Assets/MadLab/Prefabs.\n\n" +
            "2. Create Lab Arena - builds the arena into the open scene and saves it.\n\n" +
            "Editing the map:\n" +
            "- Select 'Lab Arena > Terrain' and use the Terrain tools to raise / lower ground, paint ground, plant trees or grass.\n" +
            "- Move the labs (Lab Arena > Labs). Each has a Lab Number (1-8). Lab 1 is yours in solo play.\n" +
            "- Drag Node_Herb / Node_Slime / Node_Crystal / Node_Metal / Node_Chemical prefabs anywhere for more material spots.\n" +
            "- DummySpot markers = target dummies. DroneSpawn markers = where raid drones come from. ZoneCenter = where the zone shrinks to.\n" +
            "- Open the Laboratory prefab to change how every lab looks (keep the part names the same).", "OK");
    }

    static void MakeFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
    }

    static void FixTextureImports()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TexDir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;
            string file = Path.GetFileNameWithoutExtension(path);
            bool isGrass = file.Contains("Blades") || file.Contains("Flowers");
            imp.textureType = TextureImporterType.Default;
            imp.mipmapEnabled = true;
            imp.alphaIsTransparency = isGrass;
            imp.alphaSource = isGrass ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            imp.wrapMode = isGrass ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            imp.filterMode = FilterMode.Trilinear;
            imp.anisoLevel = isGrass ? 1 : 4;
            imp.maxTextureSize = 1024;
            imp.isReadable = isGrass;
            imp.SaveAndReimport();
        }
        // UI icons: crisp, see-through
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Icons" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;
            imp.textureType = TextureImporterType.Default;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.alphaSource = TextureImporterAlphaSource.FromInput;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
    }

    static Texture2D Tex(string name) => name == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/" + name + ".png");

    static TerrainLayer MakeTerrainLayer(string name, float tile)
    {
        string path = LayerDir + "/" + name + ".terrainlayer";
        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        bool isNew = layer == null;
        if (isNew) layer = new TerrainLayer();
        layer.diffuseTexture = Tex(name);
        layer.tileSize = new Vector2(tile, tile);
        layer.smoothness = name == "Toxic" ? 0.4f : 0f;
        layer.metallic = 0f;
        if (isNew) AssetDatabase.CreateAsset(layer, path);
        else EditorUtility.SetDirty(layer);
        return layer;
    }

    // A material in Assets/MadLab/Materials. glow > 0 makes it glow in the dark.
    static Material MakeMat(string name, string texture, Color tint, float smooth = 0.1f, float metal = 0f, float glow = 0f)
    {
        string path = MatDir + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool isNew = m == null;
        if (isNew)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh);
        }
        Texture2D t = Tex(texture);
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", t);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
        m.mainTexture = t;
        m.color = tint;
        if (glow > 0f)
        {
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", tint * glow);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            m.DisableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
        }
        m.enableInstancing = true;
        if (isNew) AssetDatabase.CreateAsset(m, path);
        else EditorUtility.SetDirty(m);
        return m;
    }

    // ---------------- all the materials ----------------

    static Material bark, barkDark, leaves, leavesDark, leavesTeal, needles, rock, rockDark, capPurple, capOrange, stem, glowCyan, glowPurple, glowGreen, glowOrange,
        crystal, crystalPink, wood, woodDark, metal, metalDark, rust, hazard, grate, concrete, labFloor, labWall, glass, screen, accent, fire, toxic, water,
        brass, iron, rubber, white, redGlow, slime, chem, herbLeaf, barrelGreen, barrelBlue, chalk, board, stone;
    static Material[] binGlow;

    static void MakeMaterials()
    {
        bark = MakeMat("Bark", "Bark", Color.white);
        barkDark = MakeMat("Bark_Mutant", "Bark", new Color(0.55f, 0.45f, 0.6f));
        leaves = MakeMat("Leaves", "Leaves", Color.white);
        leavesDark = MakeMat("Leaves_Dark", "Leaves", new Color(0.7f, 0.82f, 0.68f));
        leavesTeal = MakeMat("Leaves_Mutant", "Leaves", new Color(0.45f, 0.85f, 0.85f));
        needles = MakeMat("Needles", "Leaves", new Color(0.6f, 0.78f, 0.7f));
        rock = MakeMat("Rock", "Rock", Color.white, 0.12f);
        rockDark = MakeMat("Rock_Dark", "Rock", new Color(0.6f, 0.6f, 0.65f), 0.12f);
        stone = MakeMat("Stone", "Rock", new Color(0.8f, 0.8f, 0.85f), 0.2f);
        capPurple = MakeMat("Mushroom_Cap", "Mushroom", Color.white, 0.35f);
        capOrange = MakeMat("Mushroom_Cap_Orange", "Mushroom", new Color(1f, 0.55f, 0.35f), 0.35f);
        stem = MakeMat("Mushroom_Stem", null, new Color(0.9f, 0.88f, 0.8f), 0.2f);
        glowCyan = MakeMat("Glow_Cyan", null, new Color(0.35f, 0.95f, 1f), 0.6f, 0f, 2.5f);
        glowPurple = MakeMat("Glow_Purple", null, new Color(0.8f, 0.4f, 1f), 0.6f, 0f, 2.5f);
        glowGreen = MakeMat("Glow_Green", null, new Color(0.45f, 1f, 0.35f), 0.6f, 0f, 2.5f);
        glowOrange = MakeMat("Glow_Orange", null, new Color(1f, 0.6f, 0.2f), 0.6f, 0f, 2.5f);
        crystal = MakeMat("Crystal", null, new Color(0.45f, 0.9f, 1f), 0.95f, 0.1f, 1.4f);
        crystalPink = MakeMat("Crystal_Pink", null, new Color(1f, 0.5f, 0.9f), 0.95f, 0.1f, 1.4f);
        wood = MakeMat("Wood", "Wood", Color.white, 0.15f);
        woodDark = MakeMat("Wood_Dark", "Wood", new Color(0.6f, 0.5f, 0.42f), 0.15f);
        metal = MakeMat("Metal", "Lab_Wall", new Color(0.85f, 0.87f, 0.9f), 0.55f, 0.7f);
        metalDark = MakeMat("Metal_Dark", null, new Color(0.22f, 0.24f, 0.27f), 0.45f, 0.6f);
        rust = MakeMat("Rust", "Dirt", new Color(0.85f, 0.55f, 0.35f), 0.25f, 0.4f);
        hazard = MakeMat("Hazard", "Hazard", Color.white, 0.3f);
        grate = MakeMat("Grate", "Grate", Color.white, 0.4f, 0.6f);
        concrete = MakeMat("Concrete", "Gravel", new Color(0.85f, 0.85f, 0.86f), 0.1f);
        labFloor = MakeMat("Lab_Floor", "Lab_Floor", Color.white, 0.55f);
        labWall = MakeMat("Lab_Wall", "Lab_Wall", Color.white, 0.4f, 0.4f);
        glass = MakeMat("Glass", null, new Color(0.15f, 0.25f, 0.32f), 0.95f, 0.2f, 0.15f);
        screen = MakeMat("Screen", null, new Color(0.25f, 0.65f, 1f), 0.9f, 0f, 1.8f);
        accent = MakeMat("LabAccent", null, new Color(0.35f, 1f, 0.3f), 0.6f, 0f, 2f);   // turns into each lab's colour in the game
        fire = MakeMat("Fire", null, new Color(1f, 0.5f, 0.1f), 0.5f, 0f, 4f);
        toxic = MakeMat("Toxic_Liquid", null, new Color(0.45f, 1f, 0.25f), 0.9f, 0f, 1.6f);
        water = MakeMat("Water", null, new Color(0.12f, 0.35f, 0.45f), 0.95f, 0.1f);
        brass = MakeMat("Brass", null, new Color(0.8f, 0.6f, 0.25f), 0.7f, 0.8f);
        iron = MakeMat("Iron", null, new Color(0.13f, 0.13f, 0.14f), 0.35f, 0.6f);
        rubber = MakeMat("Rubber", null, new Color(0.08f, 0.08f, 0.08f), 0.2f);
        white = MakeMat("Lamp", null, new Color(1f, 0.97f, 0.88f), 0.5f, 0f, 2.5f);
        redGlow = MakeMat("Glow_Red", null, new Color(1f, 0.2f, 0.15f), 0.5f, 0f, 3f);
        slime = MakeMat("Slime", null, new Color(0.3f, 1f, 0.7f), 0.95f, 0f, 1.2f);
        chem = MakeMat("Chemical", null, new Color(0.8f, 0.35f, 1f), 0.9f, 0f, 1.8f);
        herbLeaf = MakeMat("Herb_Leaf", "Leaves", new Color(0.6f, 1f, 0.5f), 0.3f, 0f, 0.5f);
        barrelGreen = MakeMat("Barrel_Green", null, new Color(0.25f, 0.45f, 0.22f), 0.45f, 0.5f);
        barrelBlue = MakeMat("Barrel_Blue", null, new Color(0.2f, 0.35f, 0.6f), 0.45f, 0.5f);
        chalk = MakeMat("Chalk", null, new Color(0.92f, 0.95f, 0.92f), 0.1f);
        board = MakeMat("Chalkboard", null, new Color(0.12f, 0.22f, 0.17f), 0.15f);
        binGlow = new Material[Items.MatCount];
        for (int m = 0; m < Items.MatCount; m++) binGlow[m] = MakeMat("Glow_" + Items.MatNames[m], null, Items.MatColors[m], 0.6f, 0f, 1.8f);
    }

    // ================================================================
    // Building blocks
    // ================================================================

    // Trunk = upright capsule. Mesh = convex hull. Shape = exact. Box = a box around it.
    enum Solid { None, Trunk, Mesh, Shape, Box }

    struct Part
    {
        public Mesh mesh; public Matrix4x4 m; public Material mat;
        public Part(Mesh mesh, Matrix4x4 m, Material mat) { this.mesh = mesh; this.m = m; this.mat = mat; }
    }

    static Matrix4x4 TRS(Vector3 p, Vector3 euler, Vector3 s) => Matrix4x4.TRS(p, Quaternion.Euler(euler), s);

    // shorthand
    static Part B(Vector3 center, Vector3 size, Material m, Vector3 euler = default(Vector3)) => new Part(Box(size), TRS(center, euler, Vector3.one), m);
    static Part Cy(Vector3 bottom, float r0, float r1, float h, Material m, int sides = 12, Vector3 euler = default(Vector3), bool caps = true)
        => new Part(Cyl(sides, r0, r1, h, caps), TRS(bottom, euler, Vector3.one), m);
    static Part Ball(Vector3 c, Vector3 size, Material m, int seed = 0, float bump = 0f) => new Part(Lumpy(seed, bump), TRS(c, Vector3.zero, size), m);
    static Part Ball(Vector3 c, float d, Material m) => Ball(c, Vector3.one * d, m);

    static void SavePrefab(string name, List<Part> parts, Solid col, float radius = 0f, float height = 0f, System.Action<GameObject> extra = null)
    {
        Material[] mats;
        Mesh mesh = BuildMesh(name, parts, out mats);
        var go = new GameObject(name);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterials = mats;
        mr.shadowCastingMode = ShadowCastingMode.On;
        AddCollider(go, mesh, col, radius, height);
        if (extra != null) extra(go);
        PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/" + name + ".prefab");
        Object.DestroyImmediate(go);
    }

    static void AddCollider(GameObject go, Mesh mesh, Solid col, float radius, float height)
    {
        switch (col)
        {
            case Solid.Trunk:
                var cap = go.AddComponent<CapsuleCollider>();
                cap.radius = radius; cap.height = height; cap.center = new Vector3(0f, height / 2f, 0f);
                break;
            case Solid.Mesh:
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = mesh;
                mc.convex = true;
                break;
            case Solid.Shape:
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
                break;
            case Solid.Box:
                var bc = go.AddComponent<BoxCollider>();
                bc.center = mesh.bounds.center;
                bc.size = mesh.bounds.size;
                break;
        }
    }

    // A child object with its own mesh (for prefabs made of several parts).
    static GameObject MeshChild(Transform parent, string meshName, string childName, List<Part> parts, Solid col, Vector3 localPos, Vector3 euler = default(Vector3))
    {
        Material[] mats;
        Mesh mesh = BuildMesh(meshName, parts, out mats);
        var go = new GameObject(childName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterials = mats;
        mr.shadowCastingMode = ShadowCastingMode.On;
        AddCollider(go, mesh, col, 0f, 0f);
        return go;
    }

    static GameObject Empty(Transform parent, string name, Vector3 localPos, Vector3 euler = default(Vector3))
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(euler);
        return go;
    }

    static Light AddLight(Transform parent, string name, Vector3 pos, Color c, float range, float intensity)
    {
        var go = Empty(parent, name, pos);
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = c;
        l.range = range;
        l.intensity = intensity;
        l.shadows = LightShadows.None;
        return l;
    }

    // Combines parts into one mesh (one piece per material) and saves it in the Meshes folder.
    static Mesh BuildMesh(string name, List<Part> parts, out Material[] matsOut)
    {
        var mats = new List<Material>();
        var groups = new List<List<CombineInstance>>();
        foreach (Part p in parts)
        {
            int i = mats.IndexOf(p.mat);
            if (i < 0) { mats.Add(p.mat); groups.Add(new List<CombineInstance>()); i = mats.Count - 1; }
            groups[i].Add(new CombineInstance { mesh = p.mesh, transform = p.m });
        }
        var subs = new List<CombineInstance>();
        foreach (List<CombineInstance> g in groups)
        {
            var sub = new Mesh { indexFormat = IndexFormat.UInt32 };
            sub.CombineMeshes(g.ToArray(), true, true);
            subs.Add(new CombineInstance { mesh = sub, transform = Matrix4x4.identity });
        }
        var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
        mesh.CombineMeshes(subs.ToArray(), false, false);
        mesh.RecalculateBounds();

        string meshPath = MeshDir + "/" + name + ".asset";
        Mesh old = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (old != null) { EditorUtility.CopySerialized(mesh, old); mesh = old; EditorUtility.SetDirty(old); }
        else AssetDatabase.CreateAsset(mesh, meshPath);
        matsOut = mats.ToArray();
        return mesh;
    }

    // ================================================================
    // Low-poly shapes (every triangle has its own corners: the faceted look)
    // ================================================================

    class MeshMaker
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int> t = new List<int>();

        public void Tri(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            Tri(a, b, c, Map(a, n), Map(b, n), Map(c, n));
        }

        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c);
            uv.Add(ua); uv.Add(ub); uv.Add(uc);
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
        }

        static Vector2 Map(Vector3 p, Vector3 n)
        {
            n = new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z));
            if (n.y >= n.x && n.y >= n.z) return new Vector2(p.x, p.z) * 0.5f;
            if (n.x >= n.z) return new Vector2(p.z, p.y) * 0.5f;
            return new Vector2(p.x, p.y) * 0.5f;
        }

        public void TriOut(Vector3 a, Vector3 b, Vector3 c, Vector3 inside)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), (a + b + c) / 3f - inside) < 0f) Tri(a, c, b);
            else Tri(a, b, c);
        }

        public void QuadOut(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 inside)
        {
            TriOut(a, b, c, inside);
            TriOut(a, c, d, inside);
        }

        public Mesh Build(string name)
        {
            var m = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds(); m.RecalculateTangents();
            return m;
        }
    }

    // Box centred on (0,0,0).
    static Mesh Box(Vector3 size)
    {
        var mm = new MeshMaker();
        Vector3 h = size / 2f;
        Vector3[] c =
        {
            new Vector3(-h.x, -h.y, -h.z), new Vector3(h.x, -h.y, -h.z), new Vector3(h.x, -h.y, h.z), new Vector3(-h.x, -h.y, h.z),
            new Vector3(-h.x, h.y, -h.z), new Vector3(h.x, h.y, -h.z), new Vector3(h.x, h.y, h.z), new Vector3(-h.x, h.y, h.z),
        };
        Vector3 o = Vector3.zero;
        mm.QuadOut(c[0], c[1], c[2], c[3], o);
        mm.QuadOut(c[4], c[5], c[6], c[7], o);
        mm.QuadOut(c[0], c[1], c[5], c[4], o);
        mm.QuadOut(c[3], c[2], c[6], c[7], o);
        mm.QuadOut(c[0], c[3], c[7], c[4], o);
        mm.QuadOut(c[1], c[2], c[6], c[5], o);
        return mm.Build("Box");
    }

    // Tube from y=0 up to y=h, radius r0 at the bottom and r1 at the top, with lids.
    static Mesh Cyl(int sides, float r0, float r1, float h, bool caps)
    {
        var mm = new MeshMaker();
        for (int i = 0; i < sides; i++)
        {
            float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
            Vector3 p00 = new Vector3(Mathf.Cos(a0) * r0, 0f, Mathf.Sin(a0) * r0), p10 = new Vector3(Mathf.Cos(a1) * r0, 0f, Mathf.Sin(a1) * r0);
            Vector3 p01 = new Vector3(Mathf.Cos(a0) * r1, h, Mathf.Sin(a0) * r1), p11 = new Vector3(Mathf.Cos(a1) * r1, h, Mathf.Sin(a1) * r1);
            float u0 = (float)i / sides * Mathf.Max(1f, (r0 + r1) * 3f), u1 = (float)(i + 1) / sides * Mathf.Max(1f, (r0 + r1) * 3f);
            mm.Tri(p00, p01, p11, new Vector2(u0, 0f), new Vector2(u0, h * 0.5f), new Vector2(u1, h * 0.5f));
            mm.Tri(p00, p11, p10, new Vector2(u0, 0f), new Vector2(u1, h * 0.5f), new Vector2(u1, 0f));
            if (caps)
            {
                if (r1 > 0.001f) mm.Tri(new Vector3(0f, h, 0f), p11, p01);
                if (r0 > 0.001f) mm.Tri(Vector3.zero, p00, p10);
            }
        }
        return mm.Build("Cyl");
    }

    // A ring / pipe flange: outer radius, inner radius, height.
    static Mesh Tube(int sides, float rOut, float rIn, float h)
    {
        var mm = new MeshMaker();
        for (int i = 0; i < sides; i++)
        {
            float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
            Vector3 c0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)), c1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
            Vector3 up = Vector3.up * h;
            mm.Tri(c0 * rOut, c0 * rOut + up, c1 * rOut + up); mm.Tri(c0 * rOut, c1 * rOut + up, c1 * rOut);          // outside
            mm.Tri(c0 * rIn, c1 * rIn + up, c0 * rIn + up); mm.Tri(c0 * rIn, c1 * rIn, c1 * rIn + up);              // inside
            mm.Tri(c0 * rIn + up, c1 * rOut + up, c0 * rOut + up); mm.Tri(c0 * rIn + up, c1 * rIn + up, c1 * rOut + up);   // top
            mm.Tri(c0 * rIn, c0 * rOut, c1 * rOut); mm.Tri(c0 * rIn, c1 * rOut, c1 * rIn);                          // bottom
        }
        return mm.Build("Tube");
    }

    // Flat circle facing up at y=0.
    static Mesh Disc(int sides, float radius)
    {
        var mm = new MeshMaker();
        for (int i = 0; i < sides; i++)
        {
            float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
            mm.Tri(Vector3.zero, new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius), new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius));
        }
        return mm.Build("Disc");
    }

    // Ragged cone (pine layer) with an underside.
    static Mesh Cone(int sides, float radius, float height, int seed)
    {
        var r = new System.Random(seed);
        var mm = new MeshMaker();
        var rim = new Vector3[sides];
        for (int i = 0; i < sides; i++)
        {
            float a = i * Mathf.PI * 2f / sides;
            float rr = radius * (0.85f + (float)r.NextDouble() * 0.3f);
            rim[i] = new Vector3(Mathf.Cos(a) * rr, (float)r.NextDouble() * -0.25f, Mathf.Sin(a) * rr);
        }
        Vector3 tip = new Vector3(0f, height, 0f), under = new Vector3(0f, height * 0.18f, 0f);
        for (int i = 0; i < sides; i++)
        {
            Vector3 a = rim[i], b = rim[(i + 1) % sides];
            mm.Tri(a, tip, b);
            mm.Tri(a, b, under);
        }
        return mm.Build("Cone");
    }

    // Bumpy ball (radius 0.5). bump 0 = smooth-ish low-poly sphere.
    static Mesh Lumpy(int seed, float bumpiness)
    {
        var r = new System.Random(seed);
        int lon = 10, lat = 7;
        var pts = new Vector3[lat + 1, lon];
        float ox = (float)r.NextDouble() * 100f, oz = (float)r.NextDouble() * 100f;
        for (int j = 0; j <= lat; j++)
            for (int i = 0; i < lon; i++)
            {
                float th = Mathf.PI * j / lat, ph = Mathf.PI * 2f * i / lon + (j % 2) * Mathf.PI / lon;
                Vector3 d = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                float bump = 1f + (Mathf.PerlinNoise(ox + d.x * 1.7f + d.y, oz + d.z * 1.7f - d.y) - 0.5f) * 2f * bumpiness
                               + ((float)r.NextDouble() - 0.5f) * bumpiness * 0.6f;
                if (j == 0 || j == lat) bump = 1f - bumpiness * 0.3f;
                pts[j, i] = d * 0.5f * bump;
            }
        var mm = new MeshMaker();
        for (int j = 0; j < lat; j++)
            for (int i = 0; i < lon; i++)
            {
                int i1 = (i + 1) % lon;
                Vector3 a = pts[j, i], b = pts[j, i1], c = pts[j + 1, i], d = pts[j + 1, i1];
                if (j > 0) mm.Tri(a, b, c);
                if (j < lat - 1) mm.Tri(b, d, c);
            }
        return mm.Build("Lumpy");
    }

    // Top half of a ball with a flat bottom (mushroom caps, domes).
    static Mesh Dome(int sides, float radius)
    {
        var mm = new MeshMaker();
        int rings = 4;
        for (int j = 0; j < rings; j++)
        {
            float t0 = Mathf.PI / 2f * j / rings, t1 = Mathf.PI / 2f * (j + 1) / rings;
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 p00 = Sph(a0, t0, radius), p10 = Sph(a1, t0, radius), p01 = Sph(a0, t1, radius), p11 = Sph(a1, t1, radius);
                if (j < rings - 1) mm.Tri(p00, p01, p11);
                mm.Tri(p00, p11, p10);
            }
        }
        for (int i = 0; i < sides; i++)
        {
            float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
            mm.Tri(Vector3.zero, Sph(a0, 0f, radius), Sph(a1, 0f, radius));
        }
        return mm.Build("Dome");
    }

    static Vector3 Sph(float a, float up, float r) => new Vector3(Mathf.Cos(a) * Mathf.Cos(up), Mathf.Sin(up), Mathf.Sin(a) * Mathf.Cos(up)) * r;

    // Hexagonal crystal with a pointy top (and optionally a pointy bottom).
    static Mesh CrystalMesh(float radius, float height, float tip, bool both)
    {
        var mm = new MeshMaker();
        const int sides = 6;
        Vector3 inside = new Vector3(0f, height / 2f, 0f);
        float bottom = both ? tip : 0f;
        for (int i = 0; i < sides; i++)
        {
            float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
            Vector3 b0 = new Vector3(Mathf.Cos(a0) * radius, bottom, Mathf.Sin(a0) * radius), b1 = new Vector3(Mathf.Cos(a1) * radius, bottom, Mathf.Sin(a1) * radius);
            Vector3 t0 = b0 + Vector3.up * (height - bottom), t1 = b1 + Vector3.up * (height - bottom);
            mm.QuadOut(b0, b1, t1, t0, inside);
            mm.TriOut(t0, t1, new Vector3(0f, height + tip, 0f), inside);
            if (both) mm.TriOut(b0, b1, Vector3.zero, inside);
            else mm.TriOut(b0, b1, new Vector3(0f, 0f, 0f), new Vector3(0f, 1f, 0f));
        }
        return mm.Build("Crystal");
    }

    static Part CrystalPart(Vector3 pos, float radius, float height, Vector3 tilt, Material m, bool both = false)
        => new Part(CrystalMesh(radius, height, radius * 1.4f, both), TRS(pos, tilt, Vector3.one), m);
}
