using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// "Tools > Mad Lab > 2. Create Lab Arena": builds the whole map into the open scene.
//
//   - a 200 x 200 m Terrain: a flat golden field in the middle with the reactor, rolling green land
//     around it, big hills round the edge, a pond, a toxic swamp, a crystal crag and a mushroom hill
//   - gravel paths from every lab to a ring road around the field
//   - forests (oaks, pines, glowing mutant trees, giant mushrooms, bushes) and grass + flowers
//   - 8 laboratories in a ring, each facing the middle (Lab 1 = green = yours in solo play)
//   - material spots (each lab gets a Herb and a Metal spot close by, the rest are spread around)
//   - props: lamps along the ring road, tesla towers, crates, barrels, scrap, pipes, a satellite dish
//   - markers: ZoneCenter, DummySpot (target dummies), DroneSpawn (where raid drones come from)
//
// Everything is normal Unity stuff afterwards: sculpt and paint the Terrain, move or delete anything.
public static partial class MadLabKitMenu
{
    public const float MapSize = 200f, MapHeight = 40f, BaseHeight = 4f;
    public const float LabRing = 62f;
    const int HRes = 513;

    struct Spot { public Vector2 pos; public float radius, depth; }
    static Vector2 PondAt, SwampAt, CragAt, ShroomAt;
    static readonly List<Vector2> LabSpots = new List<Vector2>();
    static readonly List<Vector2> Hills = new List<Vector2>();
    static readonly List<float> PadHeights = new List<float>();

    static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

    static void Layout()
    {
        LabSpots.Clear();
        for (int i = 0; i < 8; i++)
        {
            float a = (22.5f + i * 45f) * Mathf.Deg2Rad;
            LabSpots.Add(new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * LabRing);
        }
        PondAt = Dir(0f) * 48f;
        SwampAt = Dir(180f) * 50f;
        CragAt = Dir(90f) * 80f;
        ShroomAt = Dir(270f) * 58f;
        Hills.Clear();
        foreach (float a in new[] { 45f, 135f, 225f, 315f }) Hills.Add(Dir(a) * 74f);
    }

    static Vector2 Dir(float deg) => new Vector2(Mathf.Sin(deg * Mathf.Deg2Rad), Mathf.Cos(deg * Mathf.Deg2Rad));

    // Ground height (metres) at world x, z (0,0 = middle of the map), before the lab pads are flattened.
    static float RawHeight(float x, float z)
    {
        float r = Mathf.Sqrt(x * x + z * z);
        float n1 = Mathf.PerlinNoise((x + 500f) / 38f, (z + 300f) / 38f);
        float n2 = Mathf.PerlinNoise((x + 100f) / 13f, (z + 900f) / 13f);
        float n3 = Mathf.PerlinNoise((x + 700f) / 70f, (z + 50f) / 70f);
        float h = BaseHeight + (n1 - 0.5f) * 2.4f + (n2 - 0.5f) * 0.5f;
        // the field in the middle: nearly flat
        float field = 1f - Smooth(30f, 46f, r);
        h = Mathf.Lerp(h, BaseHeight + (n2 - 0.5f) * 0.25f, field);
        // big hills round the edge (a bowl)
        float rim = Smooth(72f, 99f, Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) * 0.6f + r * 0.4f);
        h += rim * (8f + n3 * 14f + n1 * 4f);
        // hills between the labs
        foreach (Vector2 hp in Hills) h += Bump(x, z, hp, 15f, 5f + n3 * 3f);
        h += Bump(x, z, ShroomAt, 11f, 3f);
        // crystal crag: tall and rocky
        h += Bump(x, z, CragAt, 14f, 11f) + Bump(x, z, CragAt + new Vector2(6f, -4f), 6f, 4f) * (0.5f + n2);
        // pond and swamp
        h -= Bump(x, z, PondAt, 8f, 2.6f);
        h -= Bump(x, z, SwampAt, 10.5f, 1.6f);
        return h;
    }

    static float Bump(float x, float z, Vector2 c, float radius, float height)
    {
        float d = Vector2.Distance(new Vector2(x, z), c);
        return height * (1f - Smooth(0f, radius, d));
    }

    static float Height(float x, float z)
    {
        float h = RawHeight(x, z);
        // flat pads for the labs
        for (int i = 0; i < LabSpots.Count; i++)
        {
            float d = Vector2.Distance(new Vector2(x, z), LabSpots[i]);
            if (d < 21f) h = Mathf.Lerp(h, PadHeights[i], 1f - Smooth(12.5f, 21f, d));
        }
        // reactor platform
        float r = Mathf.Sqrt(x * x + z * z);
        if (r < 14f) h = Mathf.Lerp(h, BaseHeight, 1f - Smooth(9f, 14f, r));
        return h;
    }

    // distance to the nearest path (radial paths from each lab door to the ring road, and the ring road itself)
    static float PathDist(float x, float z)
    {
        Vector2 p = new Vector2(x, z);
        float r = p.magnitude;
        float best = Mathf.Abs(r - 37f);
        foreach (Vector2 lab in LabSpots)
        {
            Vector2 dir = lab.normalized;
            float along = Vector2.Dot(p, dir);
            if (along < 36f || along > LabRing - 6f) continue;
            float off = Mathf.Abs(dir.x * z - dir.y * x);
            best = Mathf.Min(best, off);
        }
        return best;
    }

    static float LabDist(float x, float z)
    {
        float best = 999f;
        foreach (Vector2 l in LabSpots) best = Mathf.Min(best, Vector2.Distance(new Vector2(x, z), l));
        return best;
    }

    // ================================================================

    [MenuItem("Tools/Mad Lab/2. Create Lab Arena", priority = 2)]
    static void CreateArena()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Laboratory.prefab") == null) MakeKit();
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Laboratory.prefab") == null) return;

        var scene = EditorSceneManager.GetActiveScene();
        foreach (GameObject go in scene.GetRootGameObjects())
            if (go.name == "Lab Arena")
            {
                if (!EditorUtility.DisplayDialog("You already have an arena",
                    "There's already a \"Lab Arena\" in this scene. Replace it with a brand new one? (Your changes to the old one will be lost.)", "Replace it", "Cancel"))
                {
                    Selection.activeGameObject = go;
                    return;
                }
                Undo.DestroyObjectImmediate(go);
                break;
            }

        try
        {
            EditorUtility.DisplayProgressBar("Lab Arena", "Shaping the land", 0.1f);
            Layout();
            PadHeights.Clear();
            foreach (Vector2 l in LabSpots) PadHeights.Add(RawHeight(l.x, l.y));

            GameObject root = new GameObject("Lab Arena");
            Undo.RegisterCreatedObjectUndo(root, "Create Lab Arena");
            Terrain terrain = MakeTerrain(root.transform);
            TerrainData data = terrain.terrainData;

            EditorUtility.DisplayProgressBar("Lab Arena", "Painting the ground", 0.3f);
            Paint(data);

            EditorUtility.DisplayProgressBar("Lab Arena", "Growing grass", 0.45f);
            Grass(data);

            EditorUtility.DisplayProgressBar("Lab Arena", "Planting forests", 0.6f);
            Trees(data);

            EditorUtility.DisplayProgressBar("Lab Arena", "Building the labs", 0.75f);
            Labs(root.transform);

            EditorUtility.DisplayProgressBar("Lab Arena", "Placing props and material spots", 0.85f);
            Props(root.transform);
            Nodes(root.transform);
            Markers(root.transform);
            Walls(root.transform);
            Lighting();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AddToBuild(scene.path);

            Selection.activeGameObject = root;
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
        }
        finally { EditorUtility.ClearProgressBar(); }
        Debug.Log("Lab Arena created and the scene saved! Press Play, then PLAY on the menu. Edit the Terrain with the Terrain tools.");
    }

    static float Hgt(float x, float z) => Height(x, z);
    static Vector3 World(float x, float z) => new Vector3(x, Height(x, z), z);

    static Terrain MakeTerrain(Transform root)
    {
        string dataPath = AssetDatabase.GenerateUniqueAssetPath(Root + "/Lab Arena Terrain.asset");
        var data = new TerrainData();
        data.heightmapResolution = HRes;
        data.size = new Vector3(MapSize, MapHeight, MapSize);
        float[,] h = new float[HRes, HRes];
        for (int y = 0; y < HRes; y++)
            for (int x = 0; x < HRes; x++)
            {
                float wx = x / (float)(HRes - 1) * MapSize - MapSize / 2f;
                float wz = y / (float)(HRes - 1) * MapSize - MapSize / 2f;
                h[y, x] = Mathf.Clamp01(Height(wx, wz) / MapHeight);
            }
        data.SetHeights(0, 0, h);
        data.alphamapResolution = 512;
        data.baseMapResolution = 1024;
        data.SetDetailResolution(512, 32);

        var layers = new TerrainLayer[LayerNames.Length];
        for (int i = 0; i < layers.Length; i++) layers[i] = AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerDir + "/" + LayerNames[i] + ".terrainlayer");
        data.terrainLayers = layers;

        Color[] healthy = { new Color(0.75f, 0.95f, 0.65f), new Color(0.95f, 0.88f, 0.6f), new Color(1f, 1f, 1f) };
        Color[] dry = { new Color(0.6f, 0.8f, 0.5f), new Color(0.85f, 0.75f, 0.5f), new Color(0.9f, 0.9f, 0.8f) };
        float[] minH = { 0.4f, 0.6f, 0.35f }, maxH = { 0.8f, 1.25f, 0.65f };
        var details = new DetailPrototype[DetailNames.Length];
        for (int i = 0; i < details.Length; i++)
            details[i] = new DetailPrototype
            {
                prototypeTexture = Tex(DetailNames[i]),
                renderMode = DetailRenderMode.Grass,
                usePrototypeMesh = false,
                useInstancing = false,
                minWidth = 0.5f, maxWidth = 1.0f,
                minHeight = minH[i], maxHeight = maxH[i],
                noiseSpread = 0.3f,
                healthyColor = healthy[i], dryColor = dry[i],
            };
        data.detailPrototypes = details;

        var trees = new TreePrototype[TreeNames.Length];
        for (int i = 0; i < trees.Length; i++) trees[i] = new TreePrototype { prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/" + TreeNames[i] + ".prefab") };
        data.treePrototypes = trees;
        data.wavingGrassAmount = 0.35f;
        data.wavingGrassSpeed = 0.4f;
        data.wavingGrassStrength = 0.4f;
        data.wavingGrassTint = new Color(0.85f, 0.9f, 0.75f);
        AssetDatabase.CreateAsset(data, dataPath);

        GameObject tg = Terrain.CreateTerrainGameObject(data);
        tg.name = "Terrain";
        tg.transform.SetParent(root, false);
        tg.transform.localPosition = new Vector3(-MapSize / 2f, 0f, -MapSize / 2f);
        Terrain t = tg.GetComponent<Terrain>();
        Shader ts = Shader.Find("Universal Render Pipeline/Terrain/Lit");
        if (ts != null)
        {
            string matPath = MatDir + "/Terrain_Lit.mat";
            Material tm = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (tm == null) { tm = new Material(ts); AssetDatabase.CreateAsset(tm, matPath); }
            t.materialTemplate = tm;
        }
        t.detailObjectDistance = 75f;      // how far away grass is drawn (lower = faster)
        t.detailObjectDensity = 0.9f;
        t.treeDistance = 400f;
        t.treeBillboardDistance = 400f;
        t.heightmapPixelError = 3f;
        return t;
    }

    // ---------------- ground paint ----------------

    static void Paint(TerrainData data)
    {
        int res = data.alphamapResolution;
        int L = LayerNames.Length;
        var map = new float[res, res, L];
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float wx = (x + 0.5f) / res * MapSize - MapSize / 2f;
                float wz = (y + 0.5f) / res * MapSize - MapSize / 2f;
                float r = Mathf.Sqrt(wx * wx + wz * wz);
                float n = Mathf.PerlinNoise(wx / 18f + 40f, wz / 18f + 7f);
                float n2 = Mathf.PerlinNoise(wx / 6f + 9f, wz / 6f + 90f);
                float steep = data.GetSteepness((x + 0.5f) / res, (y + 0.5f) / res);
                float[] w = new float[L];
                w[0] = 1f;                                                                      // lab grass
                w[1] = (1f - Smooth(30f, 42f, r)) * 1.5f + Mathf.Clamp01((n - 0.62f) * 4f) * 0.8f;   // golden field in the middle (+ patches)
                w[2] = Mathf.Clamp01((n2 - 0.7f) * 5f) * 0.8f;                                    // dirt patches
                float path = PathDist(wx, wz);
                w[3] = (1f - Smooth(1.4f, 2.6f, path)) * 3f;                                       // gravel paths
                float lab = LabDist(wx, wz);
                w[3] += (1f - Smooth(9f, 12f, lab)) * 4f;                                         // gravel round the labs
                w[2] += (1f - Smooth(12f, 16f, lab)) * 1.2f * (0.5f + n2);
                w[3] += (1f - Smooth(8f, 11f, r)) * 4f;                                           // round the reactor
                w[4] = Smooth(28f, 42f, steep) * 4f + Smooth(85f, 99f, Mathf.Max(Mathf.Abs(wx), Mathf.Abs(wz))) * (0.5f + n) * 2f;   // rock on slopes and the far edge
                w[4] += (1f - Smooth(8f, 16f, Vector2.Distance(new Vector2(wx, wz), CragAt))) * 3f;
                float swamp = Vector2.Distance(new Vector2(wx, wz), SwampAt);
                w[5] = (1f - Smooth(9f, 13f + n * 3f, swamp)) * 5f;                              // toxic ground
                float pond = Vector2.Distance(new Vector2(wx, wz), PondAt);
                w[2] += (1f - Smooth(7f, 10f, pond)) * 3f;                                        // muddy pond edge
                float sum = 0f;
                for (int i = 0; i < L; i++) sum += w[i];
                for (int i = 0; i < L; i++) map[y, x, i] = w[i] / sum;
            }
        data.SetAlphamaps(0, 0, map);
    }

    // ---------------- grass and flowers ----------------

    static void Grass(TerrainData data)
    {
        int res = data.detailResolution;
        var green = new int[res, res];
        var dry = new int[res, res];
        var flowers = new int[res, res];
        float[,,] a = data.GetAlphamaps(0, 0, data.alphamapResolution, data.alphamapResolution);
        int ar = data.alphamapResolution;
        var rng = new System.Random(5);
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                int ax = Mathf.Min(ar - 1, x * ar / res), ay = Mathf.Min(ar - 1, y * ar / res);
                float grass = a[ay, ax, 0], field = a[ay, ax, 1];
                float wx = (x + 0.5f) / res * MapSize - MapSize / 2f, wz = (y + 0.5f) / res * MapSize - MapSize / 2f;
                float n = Mathf.PerlinNoise(wx / 9f + 3f, wz / 9f + 33f);
                if (Vector2.Distance(new Vector2(wx, wz), PondAt) < 8.5f) continue;
                if (grass > 0.55f) green[y, x] = rng.NextDouble() < 0.4 + n * 0.35 ? 1 + (n > 0.62f ? 1 : 0) : 0;
                if (field > 0.5f) dry[y, x] = rng.NextDouble() < 0.6 ? 2 : 1;
                if ((grass > 0.6f || field > 0.6f) && n > 0.68f && rng.NextDouble() < 0.25) flowers[y, x] = 1;
            }
        data.SetDetailLayer(0, 0, 0, green);
        data.SetDetailLayer(0, 0, 1, dry);
        data.SetDetailLayer(0, 0, 2, flowers);
    }

    // ---------------- forests ----------------

    static void Trees(TerrainData data)
    {
        var rng = new System.Random(11);
        var list = new List<TreeInstance>();
        const float step = 4.6f;
        for (float z = -MapSize / 2f + 3f; z < MapSize / 2f - 3f; z += step)
            for (float x = -MapSize / 2f + 3f; x < MapSize / 2f - 3f; x += step)
            {
                float px = x + ((float)rng.NextDouble() - 0.5f) * step * 0.9f;
                float pz = z + ((float)rng.NextDouble() - 0.5f) * step * 0.9f;
                float r = Mathf.Sqrt(px * px + pz * pz);
                if (r < 40f || PathDist(px, pz) < 4f || LabDist(px, pz) < 19f) continue;
                Vector2 p = new Vector2(px, pz);
                float pond = Vector2.Distance(p, PondAt), swamp = Vector2.Distance(p, SwampAt), crag = Vector2.Distance(p, CragAt), shroom = Vector2.Distance(p, ShroomAt);
                if (pond < 11f || swamp < 12f || crag < 13f) continue;
                if (data.GetSteepness((px + MapSize / 2f) / MapSize, (pz + MapSize / 2f) / MapSize) > 32f) continue;
                float forest = Mathf.PerlinNoise(px / 42f + 17f, pz / 42f + 5f);
                float chance = Mathf.Clamp01((forest - 0.38f) * 2.2f) * (r > 90f ? 0.5f : 1f);
                bool glowZone = swamp < 26f || Mathf.PerlinNoise(px / 60f + 200f, pz / 60f) > 0.7f;
                bool mushroomZone = shroom < 18f;
                if (mushroomZone) chance = Mathf.Max(chance, 0.55f);
                if (glowZone) chance = Mathf.Max(chance, 0.4f);
                if (rng.NextDouble() > chance) continue;
                int proto;
                double roll = rng.NextDouble();
                if (mushroomZone) proto = roll < 0.7 ? 8 : (roll < 0.85 ? 6 : 7);
                else if (glowZone) proto = roll < 0.45 ? 4 : roll < 0.75 ? 5 : roll < 0.85 ? 8 : 6;
                else if (roll < 0.18) proto = 6 + rng.Next(2);                                      // bushes
                else proto = Mathf.PerlinNoise(px / 30f, pz / 30f + 70f) > 0.5f ? 2 + rng.Next(2) : rng.Next(2);   // pines or oaks
                AddTree(list, data, px, pz, proto, rng);
            }
        // a few bushes round the edge of the field
        for (int i = 0; i < 40; i++)
        {
            float a = (float)rng.NextDouble() * Mathf.PI * 2f, d = 41f + (float)rng.NextDouble() * 4f;
            float px = Mathf.Sin(a) * d, pz = Mathf.Cos(a) * d;
            if (PathDist(px, pz) < 4f || Vector2.Distance(new Vector2(px, pz), PondAt) < 11f || Vector2.Distance(new Vector2(px, pz), SwampAt) < 12f) continue;
            AddTree(list, data, px, pz, 6 + rng.Next(2), rng);
        }
        data.SetTreeInstances(list.ToArray(), true);
    }

    static void AddTree(List<TreeInstance> list, TerrainData data, float x, float z, int proto, System.Random rng)
    {
        float s = 0.8f + (float)rng.NextDouble() * 0.45f;
        list.Add(new TreeInstance
        {
            position = new Vector3((x + MapSize / 2f) / MapSize, Hgt(x, z) / MapHeight, (z + MapSize / 2f) / MapSize),
            prototypeIndex = proto,
            widthScale = s,
            heightScale = s * (0.9f + (float)rng.NextDouble() * 0.25f),
            rotation = (float)rng.NextDouble() * Mathf.PI * 2f,
            color = Color.white,
            lightmapColor = Color.white,
        });
    }

    // ---------------- labs ----------------

    static void Labs(Transform root)
    {
        var folder = new GameObject("Labs").transform;
        folder.SetParent(root, false);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Laboratory.prefab");
        for (int i = 0; i < LabSpots.Count; i++)
        {
            Vector2 s = LabSpots[i];
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(folder, false);
            go.transform.position = new Vector3(s.x, PadHeights[i], s.y);
            go.transform.rotation = Quaternion.LookRotation(new Vector3(-s.x, 0f, -s.y));      // door faces the middle
            go.name = "Lab " + (i + 1) + " (" + Items.LabColorNames[i] + ")";
            var lab = go.GetComponent<Laboratory>();
            lab.labNumber = i + 1;
            PrefabUtility.RecordPrefabInstancePropertyModifications(lab);
        }
    }

    // ---------------- props ----------------

    static GameObject Place(Transform parent, string prefab, float x, float z, float yaw, float scale = 1f, float sink = 0f)
    {
        GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/" + prefab + ".prefab");
        if (p == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(p);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, Hgt(x, z) - sink, z);
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = Vector3.one * scale;
        return go;
    }

    static GameObject PlaceAt(Transform parent, string prefab, Vector2 p, float yaw, float scale = 1f, float sink = 0f) => Place(parent, prefab, p.x, p.y, yaw, scale, sink);

    static void Props(Transform root)
    {
        var props = new GameObject("Props").transform;
        props.SetParent(root, false);
        var rng = new System.Random(21);
        float Rnd() => (float)rng.NextDouble();

        Place(props, "Reactor", 0f, 0f, 0f);
        Place(props, "Generator", Dir(200f).x * 12f, Dir(200f).y * 12f, 20f);

        // lamps along the ring road
        for (int i = 0; i < 12; i++)
        {
            Vector2 p = Dir(15f + i * 30f) * 39.6f;
            Place(props, "Lamp_Post", p.x, p.y, -(15f + i * 30f) + 90f);
        }
        // tesla towers at the edge of the field
        foreach (float a in new[] { 0f, 90f, 180f, 270f }) PlaceAt(props, "Tesla_Tower", Dir(a) * 31f, Rnd() * 360f);
        // around every lab: crates on one side, barrels on the other, a danger sign
        for (int i = 0; i < LabSpots.Count; i++)
        {
            Vector2 s = LabSpots[i];
            Vector2 fwd = -s.normalized, right = new Vector2(fwd.y, -fwd.x);
            float yaw = Mathf.Atan2(fwd.x, fwd.y) * Mathf.Rad2Deg;
            PlaceAt(props, "Crate_Stack", s + right * 10.5f - fwd * 3f, yaw + Rnd() * 30f);
            PlaceAt(props, i % 2 == 0 ? "Barrel_Group" : "Scrap_Pile", s - right * 10.5f - fwd * 4f, Rnd() * 360f);
            PlaceAt(props, "Danger_Sign", s + fwd * 12f + right * 4.5f, yaw);
        }
        // scrap and pipes near the reactor
        foreach (float a in new[] { 45f, 135f, 225f, 315f }) PlaceAt(props, "Scrap_Pile", Dir(a) * 15f, Rnd() * 360f);
        PlaceAt(props, "Pipe_Run", Dir(120f) * 26f, -120f + 90f);
        PlaceAt(props, "Pipe_Run", Dir(300f) * 26f, -300f + 90f);
        // pond and swamp
        Vector2 pond = PondAt;
        Place(props, "Pond", pond.x, pond.y, 0f).transform.position = new Vector3(pond.x, RawHeight(pond.x, pond.y) + 1.4f, pond.y);
        Place(props, "Toxic_Pool", SwampAt.x, SwampAt.y, 0f, 1.25f).transform.position = new Vector3(SwampAt.x, RawHeight(SwampAt.x, SwampAt.y) + 0.65f, SwampAt.y);
        for (int i = 0; i < 6; i++) PlaceAt(props, i % 2 == 0 ? "Barrel_Toxic" : "Barrel_Blue", SwampAt + Dir(i * 60f + 15f) * (13f + Rnd() * 2f), Rnd() * 360f, 1f, 0.05f);
        // crystal crag
        PlaceAt(props, "Satellite_Dish", CragAt + new Vector2(2f, 1f), 180f + 90f);
        PlaceAt(props, "Crystal_Spire", CragAt + new Vector2(-5f, 3f), Rnd() * 360f);
        for (int i = 0; i < 5; i++) PlaceAt(props, i % 2 == 0 ? "Crystal_Cluster" : "Crystal_Cluster_Pink", CragAt + Dir(i * 72f + 20f) * (8f + Rnd() * 4f), Rnd() * 360f, 0.9f + Rnd() * 0.5f, 0.1f);
        foreach (float a in new[] { 30f, 160f, 250f }) PlaceAt(props, "Crystal_Cluster", Dir(a) * 18f, Rnd() * 360f, 0.8f);
        // rocks
        for (int i = 0; i < 70; i++)
        {
            float a = Rnd() * 360f, d = 44f + Rnd() * 52f;
            Vector2 p = Dir(a) * d;
            if (LabDist(p.x, p.y) < 18f || PathDist(p.x, p.y) < 3f || Vector2.Distance(p, PondAt) < 9f) continue;
            string kind = d > 85f && Rnd() < 0.5f ? "Boulder" : Rnd() < 0.6f ? "Rock_Small" : "Rock_Big";
            PlaceAt(props, kind, p, Rnd() * 360f, 0.8f + Rnd() * 0.5f, 0.2f);
        }
        // fences along a few paths
        for (int i = 0; i < LabSpots.Count; i += 2)
        {
            Vector2 dir = LabSpots[i].normalized, side = new Vector2(dir.y, -dir.x);
            float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg + 90f;
            for (int k = 0; k < 3; k++) PlaceAt(props, "Fence", dir * (42f + k * 3.1f) + side * 3.2f, yaw);
        }
    }

    // ---------------- material spots ----------------

    static void Nodes(Transform root)
    {
        var folder = new GameObject("Material Spots").transform;
        folder.SetParent(root, false);
        var rng = new System.Random(33);
        float Rnd() => (float)rng.NextDouble();
        var used = new List<Vector2>();

        System.Func<Vector2, bool> ok = p =>
        {
            if (LabDist(p.x, p.y) < 13f || PathDist(p.x, p.y) < 2.5f || p.magnitude < 9f) return false;
            if (Vector2.Distance(p, PondAt) < 8.5f || Vector2.Distance(p, SwampAt) < 11f) return false;
            if (Mathf.Abs(p.x) > 92f || Mathf.Abs(p.y) > 92f) return false;
            foreach (Vector2 u in used) if (Vector2.Distance(u, p) < 6f) return false;
            return true;
        };
        System.Action<string, Vector2> put = (prefab, p) => { used.Add(p); PlaceAt(folder, prefab, p, Rnd() * 360f); };
        System.Action<string, Vector2, float, float, int> scatter = (prefab, around, minR, maxR, count) =>
        {
            int placed = 0;
            for (int tries = 0; tries < 300 && placed < count; tries++)
            {
                Vector2 p = around + Dir(Rnd() * 360f) * Mathf.Lerp(minR, maxR, Rnd());
                if (!ok(p)) continue;
                put(prefab, p);
                placed++;
            }
        };

        // every lab gets a Herb and a Metal spot just outside, so nobody starts unlucky
        foreach (Vector2 s in LabSpots)
        {
            Vector2 fwd = -s.normalized, right = new Vector2(fwd.y, -fwd.x);
            put("Node_Herb", s + fwd * 14f - right * 7f);
            put("Node_Metal", s + fwd * 14f + right * 7f);
        }
        scatter("Node_Herb", Vector2.zero, 44f, 92f, 8);
        scatter("Node_Herb", Vector2.zero, 20f, 34f, 3);
        scatter("Node_Slime", PondAt, 9f, 13f, 4);
        scatter("Node_Slime", SwampAt, 11.5f, 16f, 4);
        scatter("Node_Slime", ShroomAt, 4f, 12f, 3);
        scatter("Node_Crystal", CragAt, 5f, 14f, 5);
        scatter("Node_Crystal", Vector2.zero, 12f, 20f, 3);
        scatter("Node_Crystal", Vector2.zero, 80f, 92f, 3);
        scatter("Node_Metal", Vector2.zero, 12f, 22f, 3);
        scatter("Node_Metal", Vector2.zero, 34f, 42f, 3);
        scatter("Node_Chemical", SwampAt, 11.5f, 17f, 4);
        scatter("Node_Chemical", Vector2.zero, 10f, 16f, 3);
        scatter("Node_Chemical", Vector2.zero, 48f, 90f, 4);
    }

    // ---------------- markers ----------------

    static void Markers(Transform root)
    {
        var folder = new GameObject("Markers").transform;
        folder.SetParent(root, false);
        Marker(folder, "ZoneCenter", World(0f, 0f), 0f, 1);
        for (int i = 0; i < 5; i++) Marker(folder, "DummySpot_" + (i + 1), World(-10f + i * 5f, -22f), 0f, 4);
        for (int i = 0; i < 8; i++)
        {
            Vector2 p = Dir(i * 45f) * 92f;
            Marker(folder, "DroneSpawn_" + (i + 1), World(p.x, p.y) + Vector3.up * 12f, 0f, 6);
        }
    }

    static void Marker(Transform parent, string name, Vector3 pos, float yaw, int icon)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        var tex = EditorGUIUtility.IconContent("sv_label_" + icon).image as Texture2D;
        if (tex != null) EditorGUIUtility.SetIconForObject(go, tex);
    }

    static void Walls(Transform root)
    {
        var walls = new GameObject("Edge Walls").transform;
        walls.SetParent(root, false);
        float half = MapSize / 2f;
        Wall(walls, new Vector3(0f, 30f, half + 0.5f), new Vector3(MapSize + 2f, 80f, 1f));
        Wall(walls, new Vector3(0f, 30f, -half - 0.5f), new Vector3(MapSize + 2f, 80f, 1f));
        Wall(walls, new Vector3(half + 0.5f, 30f, 0f), new Vector3(1f, 80f, MapSize + 2f));
        Wall(walls, new Vector3(-half - 0.5f, 30f, 0f), new Vector3(1f, 80f, MapSize + 2f));
    }

    static void Wall(Transform parent, Vector3 pos, Vector3 size)
    {
        var go = new GameObject("Wall");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.AddComponent<BoxCollider>().size = size;
    }

    // Sunshine, a bit of haze in the distance.
    static void Lighting()
    {
        Light sun = null;
        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { sun = l; break; }
        if (sun == null)
        {
            var go = new GameObject("Sun");
            sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        Undo.RecordObject(sun, "Sun");
        Undo.RecordObject(sun.transform, "Sun");
        sun.transform.rotation = Quaternion.Euler(46f, -35f, 0f);
        sun.color = new Color(1f, 0.95f, 0.86f);
        sun.intensity = 1.3f;
        sun.shadows = LightShadows.Soft;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.66f, 0.76f, 0.8f);
        RenderSettings.fogStartDistance = 70f;
        RenderSettings.fogEndDistance = 330f;
    }

    static void AddToBuild(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (EditorBuildSettingsScene s in list) if (s.path == path) return;
        list.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
