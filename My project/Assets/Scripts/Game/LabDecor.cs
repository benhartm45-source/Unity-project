using System.Collections.Generic;
using UnityEngine;

// LAB DECORATIONS (made fresh in every lab at the start of each match):
//   - 4 funny posters on the walls (picked at random from Resources/Decor)
//   - 3 lava lamps with glowing blobs floating up and down
//   - the HALL OF DISCOVERIES: a trophy shelf on the back wall showing YOUR 6 best discoveries
//     (the hardest recipes you've ever found - the OMEGA RAY beats everything). It updates as you discover more.
// Everything is attached to the lab's building, so it all blows apart when the lab is destroyed.
// Positions are in the lab's own space (see the layout in Editor/MadLabLab.cs: door at +Z, back wall at -Z).
public class LabDecor : MonoBehaviour
{
    const float FY = 0.5f;          // lab floor height

    static readonly string[] Posters =
    {
        "poster_safety_third", "poster_dont_lick", "poster_mix_it", "poster_hang_in",
        "poster_employee", "poster_zone", "poster_emc", "poster_crystals",
    };

    // where posters go: position, which way they face (rotation around Y)
    static readonly Vector3[] PosterSpots = { new Vector3(2.3f, FY + 2.9f, -5.56f), new Vector3(2.6f, FY + 2.5f, 5.56f), new Vector3(-2.6f, FY + 2.5f, 5.56f), new Vector3(6.56f, FY + 3.05f, -0.4f) };
    static readonly float[] PosterYaw = { 180f, 0f, 0f, 90f };

    static readonly Vector3[] LampSpots = { new Vector3(-3.6f, FY, 4.9f), new Vector3(6.0f, FY, -1.4f), new Vector3(-6.0f, FY, -0.4f) };
    static readonly Color[][] LampColors =
    {
        new[] { new Color(1f, 0.35f, 0.1f), new Color(0.55f, 0.1f, 0.6f) },        // orange blobs, purple goo
        new[] { new Color(0.3f, 1f, 0.45f), new Color(0.05f, 0.25f, 0.45f) },       // green / deep blue
        new[] { new Color(1f, 0.3f, 0.75f), new Color(0.15f, 0.35f, 0.55f) },       // pink / teal
        new[] { new Color(0.4f, 0.8f, 1f), new Color(0.3f, 0.1f, 0.45f) },          // cyan / violet
        new[] { new Color(1f, 0.85f, 0.2f), new Color(0.6f, 0.15f, 0.1f) },         // yellow / red
    };

    const int TrophySlots = 6;
    static readonly Dictionary<string, Material> iconMats = new Dictionary<string, Material>();
    static readonly Dictionary<string, Material> posterMats = new Dictionary<string, Material>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { iconMats.Clear(); posterMats.Clear(); }

    class Blob { public Transform t; public float speed, phase, x, z, size; }
    class Trophy { public Transform root, icon; public Renderer iconRenderer; public Transform empty; public float phase; }

    Laboratory lab;
    readonly List<Blob> blobs = new List<Blob>();
    readonly List<Trophy> trophies = new List<Trophy>();
    int shownKnown = -1;
    float refreshCd, t;

    // ================================================================
    // Building it
    // ================================================================

    public static void Build(Laboratory lab)
    {
        if (lab == null) return;
        // on LOW graphics only your own lab gets decorated (fewer things to draw on slow computers)
        if (GraphicsQuality.Level == GraphicsQuality.Low && !lab.OwnedByPlayer) return;
        Transform structure = lab.transform.Find("Structure");
        Transform parent = structure != null ? structure : lab.transform;
        if (parent.Find("Decor") != null) return;
        var go = new GameObject("Decor");
        go.transform.SetParent(lab.transform, false);         // place it in the lab's space first...
        var d = go.AddComponent<LabDecor>();
        d.lab = lab;
        var rng = new System.Random(lab.Index * 977 + Random.Range(0, 100000));
        d.MakePosters(rng);
        d.MakeLamps(rng);
        d.MakeTrophyShelf();
        go.transform.SetParent(parent, true);                 // ...then hang it on the building (so it falls apart with it)
        Gfx.SetLayer(go, 2);                                  // shots and the camera go straight through
    }

    void MakePosters(System.Random rng)
    {
        var pool = new List<string>(Posters);
        for (int i = 0; i < PosterSpots.Length && pool.Count > 0; i++)
        {
            int k = rng.Next(pool.Count);
            string name = pool[k];
            pool.RemoveAt(k);
            Material m = PosterMat(name);
            if (m == null) continue;
            var p = new GameObject("Poster").transform;
            p.SetParent(transform, false);
            p.localPosition = PosterSpots[i];
            p.localRotation = Quaternion.Euler(0f, PosterYaw[i] + (float)(rng.NextDouble() * 4.0 - 2.0), (float)(rng.NextDouble() * 4.0 - 2.0));
            Gfx.Part(p, PrimitiveType.Cube, new Vector3(0f, 0f, 0.025f), new Vector3(0.98f, 1.33f, 0.03f), Gfx.Lit(new Color(0.12f, 0.12f, 0.14f)), default, false);
            Gfx.Part(p, PrimitiveType.Quad, Vector3.zero, new Vector3(0.9f, 1.24f, 1f), m, default, false);
            // a bit of tape in two corners
            Gfx.Part(p, PrimitiveType.Cube, new Vector3(-0.42f, 0.6f, -0.005f), new Vector3(0.16f, 0.05f, 0.005f), Gfx.Lit(new Color(0.95f, 0.92f, 0.8f)), new Vector3(0f, 0f, 35f), false);
            Gfx.Part(p, PrimitiveType.Cube, new Vector3(0.42f, 0.6f, -0.005f), new Vector3(0.16f, 0.05f, 0.005f), Gfx.Lit(new Color(0.95f, 0.92f, 0.8f)), new Vector3(0f, 0f, -35f), false);
        }
    }

    void MakeLamps(System.Random rng)
    {
        Color metal = new Color(0.55f, 0.57f, 0.62f);
        for (int i = 0; i < LampSpots.Length; i++)
        {
            Color[] c = LampColors[rng.Next(LampColors.Length)];
            Transform l = new GameObject("Lava Lamp").transform;
            l.SetParent(transform, false);
            l.localPosition = LampSpots[i];
            // base (a little cone), glass, goo inside, lid
            Gfx.Part(l, PrimitiveType.Cylinder, new Vector3(0f, 0.12f, 0f), new Vector3(0.42f, 0.12f, 0.42f), Gfx.Lit(metal, 0.7f, 0.8f));
            Gfx.Part(l, PrimitiveType.Cylinder, new Vector3(0f, 0.3f, 0f), new Vector3(0.3f, 0.07f, 0.3f), Gfx.Lit(metal, 0.7f, 0.8f));
            Gfx.Part(l, PrimitiveType.Cylinder, new Vector3(0f, 0.85f, 0f), new Vector3(0.26f, 0.5f, 0.26f), Gfx.Clear(new Color(c[1].r, c[1].g, c[1].b, 0.55f)), default, false);
            Gfx.Part(l, PrimitiveType.Cylinder, new Vector3(0f, 0.85f, 0f), new Vector3(0.3f, 0.5f, 0.3f), Gfx.Clear(new Color(1f, 1f, 1f, 0.12f)), default, false);
            Gfx.Part(l, PrimitiveType.Cylinder, new Vector3(0f, 1.4f, 0f), new Vector3(0.2f, 0.06f, 0.2f), Gfx.Lit(metal, 0.7f, 0.8f));
            Gfx.Part(l, PrimitiveType.Sphere, new Vector3(0f, 1.46f, 0f), new Vector3(0.2f, 0.08f, 0.2f), Gfx.Lit(metal, 0.7f, 0.8f));
            // the blobs
            Material blobMat = Gfx.Glow(c[0], 2.4f);
            for (int b = 0; b < 4; b++)
            {
                float size = 0.07f + (float)rng.NextDouble() * 0.07f;
                Transform bt = Gfx.Part(l, PrimitiveType.Sphere, Vector3.zero, Vector3.one * size, blobMat, default, false).transform;
                blobs.Add(new Blob
                {
                    t = bt, size = size,
                    speed = 0.25f + (float)rng.NextDouble() * 0.3f,
                    phase = (float)rng.NextDouble() * 10f,
                    x = ((float)rng.NextDouble() - 0.5f) * 0.1f,
                    z = ((float)rng.NextDouble() - 0.5f) * 0.1f,
                });
            }
            // one soft light (only in YOUR lab, so the other labs don't cost anything)
            if (lab.OwnedByPlayer) Gfx.AddLight(l, new Vector3(0f, 0.9f, 0f), c[0], 3.5f, 1.4f);
        }
    }

    void MakeTrophyShelf()
    {
        Color wood = new Color(0.36f, 0.22f, 0.12f), gold = new Color(1f, 0.82f, 0.32f);
        Transform s = new GameObject("Hall of Discoveries").transform;
        s.SetParent(transform, false);
        s.localPosition = new Vector3(-2.5f, FY, -5.6f);
        // two shelves with brackets, a back board, and the sign on top
        Gfx.Part(s, PrimitiveType.Cube, new Vector3(0f, 2.35f, 0.03f), new Vector3(2.3f, 1.75f, 0.04f), Gfx.Lit(new Color(0.18f, 0.12f, 0.08f)));
        foreach (float y in new[] { 1.75f, 2.65f })
        {
            Gfx.Part(s, PrimitiveType.Cube, new Vector3(0f, y, 0.22f), new Vector3(2.3f, 0.06f, 0.42f), Gfx.Lit(wood, 0.4f));
            Gfx.Part(s, PrimitiveType.Cube, new Vector3(0f, y + 0.035f, 0.42f), new Vector3(2.3f, 0.012f, 0.02f), Gfx.Glow(lab.LabColor, 1.6f), default, false);
            foreach (float x in new[] { -1.0f, 1.0f })
                Gfx.Part(s, PrimitiveType.Cube, new Vector3(x, y - 0.12f, 0.12f), new Vector3(0.05f, 0.2f, 0.2f), Gfx.Lit(gold, 0.7f, 0.9f));
        }
        Material sign = PosterMat("trophy_sign");
        if (sign != null)
        {
            Gfx.Part(s, PrimitiveType.Cube, new Vector3(0f, 3.48f, 0.03f), new Vector3(2.35f, 0.4f, 0.05f), Gfx.Lit(gold, 0.7f, 0.9f));
            Gfx.Part(s, PrimitiveType.Quad, new Vector3(0f, 3.48f, 0.065f), new Vector3(2.25f, 0.35f, 1f), sign, new Vector3(0f, 180f, 0f), false);
        }
        // 6 spots: 3 on each shelf (best one top-left)
        for (int i = 0; i < TrophySlots; i++)
        {
            float y = i < 3 ? 2.65f : 1.75f;
            float x = -0.72f + (i % 3) * 0.72f;
            var tr = new Trophy { phase = i * 1.3f };
            tr.root = new GameObject("Trophy " + (i + 1)).transform;
            tr.root.SetParent(s, false);
            tr.root.localPosition = new Vector3(x, y + 0.03f, 0.24f);
            Gfx.Part(tr.root, PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.3f, 0.04f, 0.3f), Gfx.Lit(new Color(0.15f, 0.15f, 0.18f), 0.6f, 0.5f));
            Gfx.Part(tr.root, PrimitiveType.Cylinder, new Vector3(0f, 0.085f, 0f), new Vector3(0.24f, 0.008f, 0.24f), Gfx.Glow(gold, 1.5f), default, false);
            // the item picture floats above the stand
            GameObject icon = Gfx.Part(tr.root, PrimitiveType.Quad, new Vector3(0f, 0.38f, 0f), new Vector3(0.5f, 0.5f, 1f), Gfx.Clear(Color.white), new Vector3(0f, 180f, 0f), false);
            tr.icon = icon.transform;
            tr.iconRenderer = icon.GetComponent<Renderer>();
            // empty spot: a faint glass ball
            tr.empty = Gfx.Part(tr.root, PrimitiveType.Sphere, new Vector3(0f, 0.3f, 0f), Vector3.one * 0.22f, Gfx.Clear(new Color(1f, 1f, 1f, 0.08f)), default, false).transform;
            trophies.Add(tr);
        }
        RefreshTrophies();
    }

    // ================================================================
    // Materials
    // ================================================================

    static Material PosterMat(string name)
    {
        if (posterMats.TryGetValue(name, out Material m) && m != null) return m;
        var tex = Resources.Load<Texture2D>("Decor/" + name);
        if (tex == null) return null;
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        m = new Material(sh);
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        m.mainTexture = tex;
        m.SetColor("_BaseColor", Color.white);
        m.color = Color.white;
        m.SetFloat("_Smoothness", 0.15f);
        if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);       // visible from both sides
        posterMats[name] = m;
        return m;
    }

    static Material IconMat(Recipe r)
    {
        if (iconMats.TryGetValue(r.icon, out Material m) && m != null) return m;
        Texture2D tex = Items.Icon(r.icon);
        m = new Material(Shader.Find("Sprites/Default"));
        m.mainTexture = tex;
        m.color = tex != null ? Color.white : r.color;
        iconMats[r.icon] = m;
        return m;
    }

    // ================================================================
    // Every frame: lava blobs, floating trophies, new discoveries
    // ================================================================

    // How impressive a discovery is: the more materials a recipe needs, the better. The OMEGA RAY beats everything.
    static int Score(Recipe r)
    {
        if (r.hideRecipe) return 1000000;
        int total = 0, kinds = 0;
        for (int m = 0; m < Items.MatCount; m++) if (r.cost[m] > 0) { total += r.cost[m]; kinds++; }
        return total * 10 + kinds * 3 + (r.kind == ItemKind.Weapon ? 2 : 0);
    }

    void RefreshTrophies()
    {
        var best = new List<Recipe>();
        // only your OWN lab shows your trophies (the rival labs' shelves are empty)
        if (lab != null && lab.OwnedByPlayer)
        {
            foreach (Recipe r in Items.All)
                if (r.station != Station.Terminal && r.id != ItemId.Zapper && Discoveries.Knows(r.id)) best.Add(r);
            best.Sort((a, b) => Score(b).CompareTo(Score(a)));
        }
        for (int i = 0; i < trophies.Count; i++)
        {
            Trophy tr = trophies[i];
            bool has = i < best.Count;
            tr.icon.gameObject.SetActive(has);
            tr.empty.gameObject.SetActive(!has);
            if (has) tr.iconRenderer.sharedMaterial = IconMat(best[i]);
        }
        shownKnown = CountKnown();
    }

    static int CountKnown()
    {
        int n = 0;
        foreach (Recipe r in Items.All) if (Discoveries.Knows(r.id)) n++;
        return n;
    }

    void Update()
    {
        t += Time.deltaTime;
        foreach (Blob b in blobs)
        {
            if (b.t == null) continue;
            // rise and sink slowly inside the glass, squishing a little
            float k = 0.5f + 0.5f * Mathf.Sin(t * b.speed + b.phase);
            b.t.localPosition = new Vector3(b.x + Mathf.Sin(t * 0.7f + b.phase) * 0.02f, 0.45f + k * 0.78f, b.z);
            float squish = 1f + 0.15f * Mathf.Sin(t * 1.7f + b.phase);
            b.t.localScale = new Vector3(b.size * squish, b.size / squish, b.size * squish);
        }
        foreach (Trophy tr in trophies)
        {
            if (tr.icon == null) continue;
            tr.icon.localPosition = new Vector3(0f, 0.38f + Mathf.Sin(t * 1.6f + tr.phase) * 0.03f, 0f);
            tr.icon.localRotation = Quaternion.Euler(0f, 180f + Mathf.Sin(t * 0.8f + tr.phase) * 25f, 0f);
        }
        refreshCd -= Time.deltaTime;
        if (refreshCd <= 0f)
        {
            refreshCd = 1.5f;
            if (CountKnown() != shownKnown) RefreshTrophies();
        }
    }
}
