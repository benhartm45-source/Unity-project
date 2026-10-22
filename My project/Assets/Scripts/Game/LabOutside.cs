using System.Collections.Generic;
using UnityEngine;

// THE OUTSIDE OF EVERY LAB (made fresh at the start of each match), so the labs don't look bare:
//   on the walls:  big painted lab numbers (in the lab's colour) and floodlights on the front corners
//   out front:     two lamp posts, a KEEP OUT sign, a mailbox, a WELCOME mat and some traffic cones
//   on the left:   stacked crates, toxic barrels (one tipped over and leaking slime) and glowing mushrooms
//   on the right:  a big fuel tank with a ladder and a pipe into the wall
//   at the back:   a satellite dish that slowly turns, and steam puffing out of the vents
// The props on the ground stay when the lab is destroyed (the wall stuff falls with the walls).
// Positions are in the lab's own space: the door is at +Z (see Editor/MadLabLab.cs).
public class LabOutside : MonoBehaviour
{
    const float FY = 0.5f;
    static readonly Dictionary<string, Material> texMats = new Dictionary<string, Material>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => texMats.Clear();

    Laboratory lab;
    Transform dish;
    float steamCd, t;
    bool mine;

    public static void Build(Laboratory lab)
    {
        if (lab == null) return;
        if (GraphicsQuality.Level == GraphicsQuality.Low && !lab.OwnedByPlayer) return;     // slow computers: only your lab
        if (lab.transform.Find("Outside") != null) return;
        var go = new GameObject("Outside");
        go.transform.SetParent(lab.transform, false);
        var o = go.AddComponent<LabOutside>();
        o.lab = lab;
        o.mine = lab.OwnedByPlayer;
        o.steamCd = Random.value * 2f;
        o.BuildWallStuff();
        o.BuildFront();
        o.BuildLeft();
        o.BuildRight();
        o.BuildBack();
    }

    // ---------------- helpers ----------------

    // A prop standing on the ground at (x, z) in the lab's space, turned by yaw degrees.
    Transform Prop(string name, float x, float z, float yaw = 0f)
    {
        Transform t = new GameObject(name).transform;
        t.SetParent(transform, false);
        Vector3 world = lab.transform.TransformPoint(new Vector3(x, 0f, z));
        world.y = MLGame.Ground(world).y;
        t.position = world;
        t.rotation = lab.transform.rotation * Quaternion.Euler(0f, yaw, 0f);
        return t;
    }

    static Material TexMat(string name, bool lit = true)
    {
        string key = name + (lit ? "" : "_u");
        if (texMats.TryGetValue(key, out Material m) && m != null) return m;
        var tex = Resources.Load<Texture2D>("Decor/" + name);
        if (tex == null) return null;
        if (lit)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh);
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            m.mainTexture = tex;
            m.SetColor("_BaseColor", Color.white);
            m.color = Color.white;
            m.SetFloat("_Smoothness", 0.1f);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
        }
        else m = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex };
        texMats[key] = m;
        return m;
    }

    static void Solid(Transform t, Vector3 center, Vector3 size)
    {
        var box = new GameObject("Collider");
        box.transform.SetParent(t, false);
        box.layer = 0;
        var c = box.AddComponent<BoxCollider>();
        c.center = center;
        c.size = size;
    }

    bool Lights => mine && GraphicsQuality.Level != GraphicsQuality.Low;

    // ---------------- on the walls ----------------

    void BuildWallStuff()
    {
        Transform structure = lab.transform.Find("Structure");
        var wall = new GameObject("Wall Paint").transform;
        wall.SetParent(structure != null ? structure : lab.transform, false);
        // big painted number on both sides and the back (white paint tinted with the lab colour)
        var tex = Resources.Load<Texture2D>("Decor/labnum_" + (lab.Index + 1));
        if (tex != null)
        {
            var paint = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex, color = Color.Lerp(lab.LabColor, Color.white, 0.15f) };
            Gfx.Part(wall, PrimitiveType.Quad, new Vector3(-7.24f, FY + 2.45f, 0f), new Vector3(2.3f, 2.3f, 1f), paint, new Vector3(0f, 90f, 0f), false);
            Gfx.Part(wall, PrimitiveType.Quad, new Vector3(7.24f, FY + 2.45f, 0f), new Vector3(2.3f, 2.3f, 1f), paint, new Vector3(0f, -90f, 0f), false);
            Gfx.Part(wall, PrimitiveType.Quad, new Vector3(0f, FY + 2.0f, -6.24f), new Vector3(1.9f, 1.9f, 1f), paint, new Vector3(0f, 0f, 0f), false);
        }
        // floodlights on the front corners, pointing at the path
        Color lamp = new Color(1f, 0.95f, 0.8f);
        foreach (float s in new[] { -1f, 1f })
        {
            Transform f = Gfx.Pivot(wall, new Vector3(s * 6.4f, FY + 4.05f, 6.12f), "Floodlight");
            f.localRotation = Quaternion.Euler(35f, s * -15f, 0f);
            Gfx.Part(f, PrimitiveType.Cube, Vector3.zero, new Vector3(0.45f, 0.3f, 0.22f), Gfx.Lit(new Color(0.2f, 0.21f, 0.24f), 0.5f, 0.6f));
            Gfx.Part(f, PrimitiveType.Cube, new Vector3(0f, 0f, 0.115f), new Vector3(0.38f, 0.22f, 0.01f), Gfx.Glow(lamp, 4f), default, false);
            Gfx.Part(wall, PrimitiveType.Cube, new Vector3(s * 6.4f, FY + 4.25f, 6.02f), new Vector3(0.06f, 0.3f, 0.06f), Gfx.Lit(new Color(0.2f, 0.21f, 0.24f)), default, false);
            if (Lights) Gfx.AddLight(f, new Vector3(0f, -0.5f, 1.2f), lamp, 9f, 1.6f);
        }
        Gfx.SetLayer(wall.gameObject, 2);
    }

    // ---------------- out front ----------------

    void BuildFront()
    {
        Color post = new Color(0.16f, 0.17f, 0.2f), glow = new Color(1f, 0.85f, 0.55f);
        // lamp posts either side of the path
        foreach (float s in new[] { -1f, 1f })
        {
            Transform p = Prop("Lamp Post", s * 3.4f, 9.4f);
            Gfx.Part(p, PrimitiveType.Cylinder, new Vector3(0f, 0.15f, 0f), new Vector3(0.32f, 0.15f, 0.32f), Gfx.Lit(post, 0.5f, 0.6f));
            Gfx.Part(p, PrimitiveType.Cylinder, new Vector3(0f, 1.6f, 0f), new Vector3(0.1f, 1.6f, 0.1f), Gfx.Lit(post, 0.5f, 0.6f));
            Gfx.Part(p, PrimitiveType.Cube, new Vector3(-s * 0.3f, 3.2f, 0f), new Vector3(0.7f, 0.06f, 0.06f), Gfx.Lit(post, 0.5f, 0.6f));
            Gfx.Part(p, PrimitiveType.Sphere, new Vector3(-s * 0.6f, 3.05f, 0f), new Vector3(0.32f, 0.26f, 0.32f), Gfx.Glow(glow, 3f), default, false);
            Gfx.Part(p, PrimitiveType.Cylinder, new Vector3(-s * 0.6f, 3.2f, 0f), new Vector3(0.38f, 0.04f, 0.38f), Gfx.Lit(post, 0.5f, 0.6f));
            Gfx.Part(p, PrimitiveType.Cube, new Vector3(0f, 2.4f, 0.06f), new Vector3(0.14f, 0.5f, 0.02f), Gfx.Glow(lab.LabColor, 2f), default, false);    // lab-colour banner
            if (Lights) Gfx.AddLight(p, new Vector3(-s * 0.6f, 2.8f, 0f), glow, 8f, 1.3f);
            Solid(p, new Vector3(0f, 1.6f, 0f), new Vector3(0.25f, 3.2f, 0.25f));
        }
        // KEEP OUT sign
        {
            Transform sgn = Prop("Keep Out Sign", -5.2f, 9.6f, 180f + 12f);
            Material m = TexMat("sign_keepout");
            Material wood = Gfx.Lit(new Color(0.42f, 0.28f, 0.16f));
            foreach (float x in new[] { -0.55f, 0.55f }) Gfx.Part(sgn, PrimitiveType.Cube, new Vector3(x, 0.75f, 0f), new Vector3(0.08f, 1.5f, 0.08f), wood);
            Gfx.Part(sgn, PrimitiveType.Cube, new Vector3(0f, 1.3f, 0.03f), new Vector3(1.4f, 0.88f, 0.04f), Gfx.Lit(new Color(0.15f, 0.15f, 0.15f)));
            if (m != null) Gfx.Part(sgn, PrimitiveType.Quad, new Vector3(0f, 1.3f, -0.0f), new Vector3(1.3f, 0.81f, 1f), m, default, false);
        }
        // mailbox with the lab number
        {
            Transform mb = Prop("Mailbox", 5.0f, 9.5f, 180f);
            Gfx.Part(mb, PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f), new Vector3(0.08f, 0.55f, 0.08f), Gfx.Lit(new Color(0.35f, 0.25f, 0.15f)));
            Gfx.Part(mb, PrimitiveType.Cube, new Vector3(0f, 1.2f, 0f), new Vector3(0.32f, 0.3f, 0.55f), Gfx.Lit(lab.LabColor * 0.8f, 0.5f, 0.4f));
            Gfx.Part(mb, PrimitiveType.Cylinder, new Vector3(0f, 1.35f, 0f), new Vector3(0.32f, 0.275f, 0.32f), Gfx.Lit(lab.LabColor * 0.8f, 0.5f, 0.4f), new Vector3(90f, 0f, 0f));
            Gfx.Part(mb, PrimitiveType.Cube, new Vector3(0.17f, 1.42f, 0.12f), new Vector3(0.03f, 0.3f, 0.06f), Gfx.Lit(new Color(0.9f, 0.15f, 0.12f)), new Vector3(0f, 0f, 0f));   // the little flag
        }
        // welcome mat in front of the step
        {
            Transform mat = Prop("Welcome Mat", 0f, 8.95f, 180f);
            Material m = TexMat("welcome_mat");
            if (m != null) Gfx.Part(mat, PrimitiveType.Quad, new Vector3(0f, 0.03f, 0f), new Vector3(2.2f, 1.2f, 1f), m, new Vector3(90f, 0f, 0f), false);
        }
        // traffic cones
        foreach (Vector3 c in new[] { new Vector3(2.1f, 0f, 10.6f), new Vector3(-2.0f, 0f, 11.1f), new Vector3(6.2f, 0f, 8.6f) })
        {
            Transform cone = Prop("Cone", c.x, c.z, Random.value * 360f);
            Material orange = Gfx.Lit(new Color(1f, 0.42f, 0.08f), 0.4f);
            Gfx.Part(cone, PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f), new Vector3(0.42f, 0.04f, 0.42f), orange);
            Gfx.Part(cone, PrimitiveType.Cylinder, new Vector3(0f, 0.3f, 0f), new Vector3(0.24f, 0.28f, 0.24f), orange);
            Gfx.Part(cone, PrimitiveType.Cylinder, new Vector3(0f, 0.33f, 0f), new Vector3(0.25f, 0.06f, 0.25f), Gfx.Lit(Color.white, 0.5f), default, false);
            Gfx.Part(cone, PrimitiveType.Cylinder, new Vector3(0f, 0.6f, 0f), new Vector3(0.1f, 0.04f, 0.1f), orange);
        }
    }

    // ---------------- left side: crates, barrels, mushrooms ----------------

    void BuildLeft()
    {
        Material wood = Gfx.Lit(new Color(0.55f, 0.38f, 0.2f)), woodDark = Gfx.Lit(new Color(0.35f, 0.22f, 0.12f));
        void Crate(Transform parent, Vector3 at, float size, float yaw)
        {
            Transform c = Gfx.Pivot(parent, at, "Crate");
            c.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Gfx.Part(c, PrimitiveType.Cube, new Vector3(0f, size / 2f, 0f), Vector3.one * size, wood);
            foreach (float s in new[] { -1f, 1f })
            {
                Gfx.Part(c, PrimitiveType.Cube, new Vector3(0f, size / 2f, s * size * 0.505f), new Vector3(size * 1.01f, size * 0.12f, 0.02f), woodDark, default, false);
                Gfx.Part(c, PrimitiveType.Cube, new Vector3(s * size * 0.505f, size / 2f, 0f), new Vector3(0.02f, size * 0.12f, size * 1.01f), woodDark, default, false);
            }
            Gfx.Part(c, PrimitiveType.Cube, new Vector3(0f, size / 2f, size * 0.51f), new Vector3(size * 1.1f, 0.06f, 0.02f), woodDark, new Vector3(0f, 0f, 45f), false);
        }
        Transform crates = Prop("Crates", -8.9f, -4.3f, 8f);
        Crate(crates, Vector3.zero, 1.05f, 0f);
        Crate(crates, new Vector3(0.1f, 0f, 1.15f), 0.95f, 12f);
        Crate(crates, new Vector3(0.05f, 1.05f, 0.4f), 0.8f, -15f);
        Solid(crates, new Vector3(0.05f, 0.9f, 0.55f), new Vector3(1.1f, 1.85f, 2.2f));

        // toxic barrels (glowing green tops), one tipped over with a slime puddle
        Material barrel = Gfx.Lit(new Color(0.85f, 0.7f, 0.1f), 0.5f, 0.4f), ring = Gfx.Lit(new Color(0.15f, 0.15f, 0.15f));
        Material goo = Gfx.Glow(new Color(0.35f, 1f, 0.3f), 2f);
        void Barrel(Transform parent, Vector3 at, Vector3 euler)
        {
            Transform b = Gfx.Pivot(parent, at, "Barrel");
            b.localRotation = Quaternion.Euler(euler);
            Gfx.Part(b, PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f), new Vector3(0.6f, 0.45f, 0.6f), barrel);
            foreach (float y in new[] { 0.2f, 0.7f }) Gfx.Part(b, PrimitiveType.Cylinder, new Vector3(0f, y, 0f), new Vector3(0.62f, 0.03f, 0.62f), ring, default, false);
            Gfx.Part(b, PrimitiveType.Cylinder, new Vector3(0f, 0.905f, 0f), new Vector3(0.5f, 0.01f, 0.5f), goo, default, false);
            Gfx.Part(b, PrimitiveType.Cube, new Vector3(0f, 0.45f, 0.305f), new Vector3(0.22f, 0.22f, 0.01f), Gfx.Lit(new Color(0.1f, 0.1f, 0.1f)), new Vector3(0f, 0f, 45f), false);   // hazard diamond
        }
        Transform barrels = Prop("Toxic Barrels", -8.9f, -1.5f, 0f);
        Barrel(barrels, Vector3.zero, Vector3.zero);
        Barrel(barrels, new Vector3(0.65f, 0f, 0.45f), new Vector3(0f, 40f, 0f));
        Barrel(barrels, new Vector3(-0.2f, 0.3f, 1.2f), new Vector3(90f, 20f, 0f));          // tipped over
        Gfx.Part(barrels, PrimitiveType.Cylinder, new Vector3(-0.1f, 0.02f, 2.1f), new Vector3(1.6f, 0.01f, 1.1f), goo, default, false);    // the leak
        Solid(barrels, new Vector3(0.2f, 0.45f, 0.5f), new Vector3(1.4f, 0.9f, 2.1f));
        if (Lights) Gfx.AddLight(barrels, new Vector3(0f, 0.5f, 2f), new Color(0.35f, 1f, 0.3f), 4f, 1f);

        // a patch of glowing mushrooms
        Transform shrooms = Prop("Mushrooms", -8.6f, 3.2f, Random.value * 360f);
        Color[] caps = { new Color(0.4f, 0.8f, 1f), new Color(1f, 0.4f, 0.9f), new Color(0.6f, 1f, 0.4f) };
        var rng = new System.Random(lab.Index * 31 + 7);
        for (int i = 0; i < 9; i++)
        {
            float h = 0.15f + (float)rng.NextDouble() * 0.35f;
            Vector3 p = new Vector3(((float)rng.NextDouble() - 0.5f) * 1.6f, 0f, ((float)rng.NextDouble() - 0.5f) * 1.6f);
            Gfx.Part(shrooms, PrimitiveType.Cylinder, p + Vector3.up * h / 2f, new Vector3(0.06f, h / 2f, 0.06f), Gfx.Lit(new Color(0.92f, 0.9f, 0.82f)));
            Gfx.Part(shrooms, PrimitiveType.Sphere, p + Vector3.up * h, new Vector3(0.22f, 0.1f, 0.22f) * (0.8f + h), Gfx.Glow(caps[i % caps.Length], 1.8f), default, false);
        }
    }

    // ---------------- right side: the fuel tank ----------------

    void BuildRight()
    {
        Transform tank = Prop("Fuel Tank", 9.4f, -2.4f, 0f);
        Color white = new Color(0.86f, 0.87f, 0.9f), dark = new Color(0.2f, 0.21f, 0.24f);
        foreach (Vector2 l in new[] { new Vector2(-0.6f, -0.6f), new Vector2(0.6f, -0.6f), new Vector2(-0.6f, 0.6f), new Vector2(0.6f, 0.6f) })
            Gfx.Part(tank, PrimitiveType.Cylinder, new Vector3(l.x, 0.3f, l.y), new Vector3(0.12f, 0.3f, 0.12f), Gfx.Lit(dark, 0.5f, 0.6f));
        Gfx.Part(tank, PrimitiveType.Cylinder, new Vector3(0f, 2.0f, 0f), new Vector3(1.8f, 1.4f, 1.8f), Gfx.Lit(white, 0.6f, 0.4f));
        Gfx.Part(tank, PrimitiveType.Sphere, new Vector3(0f, 3.4f, 0f), new Vector3(1.8f, 0.6f, 1.8f), Gfx.Lit(white, 0.6f, 0.4f));
        Gfx.Part(tank, PrimitiveType.Cylinder, new Vector3(0f, 2.3f, 0f), new Vector3(1.82f, 0.18f, 1.82f), Gfx.Lit(lab.LabColor * 0.9f, 0.5f), default, false);    // lab-colour stripe
        Gfx.Part(tank, PrimitiveType.Cylinder, new Vector3(0f, 1.0f, 0f), new Vector3(1.83f, 0.04f, 1.83f), Gfx.Lit(new Color(0.9f, 0.75f, 0.1f)), default, false);
        // ladder
        foreach (float x in new[] { -0.22f, 0.22f }) Gfx.Part(tank, PrimitiveType.Cube, new Vector3(x, 2.0f, 0.95f), new Vector3(0.04f, 2.8f, 0.04f), Gfx.Lit(dark));
        for (int i = 0; i < 9; i++) Gfx.Part(tank, PrimitiveType.Cube, new Vector3(0f, 0.8f + i * 0.3f, 0.95f), new Vector3(0.44f, 0.03f, 0.03f), Gfx.Lit(dark), default, false);
        // gauge
        Gfx.Part(tank, PrimitiveType.Cylinder, new Vector3(-0.92f, 1.6f, 0f), new Vector3(0.25f, 0.03f, 0.25f), Gfx.Lit(Color.white), new Vector3(0f, 0f, 90f), false);
        Gfx.Part(tank, PrimitiveType.Cube, new Vector3(-0.95f, 1.62f, 0.03f), new Vector3(0.01f, 0.1f, 0.015f), Gfx.Lit(new Color(0.9f, 0.1f, 0.1f)), new Vector3(30f, 0f, 0f), false);
        // pipe from the tank into the lab wall
        Gfx.Part(tank, PrimitiveType.Cylinder, new Vector3(-1.55f, 1.3f, 0f), new Vector3(0.18f, 0.6f, 0.18f), Gfx.Lit(new Color(0.55f, 0.5f, 0.45f), 0.6f, 0.8f), new Vector3(0f, 0f, 90f));
        Gfx.Part(tank, PrimitiveType.Cylinder, new Vector3(-2.12f, 1.3f, 0f), new Vector3(0.26f, 0.05f, 0.26f), Gfx.Lit(dark), new Vector3(0f, 0f, 90f), false);
        Gfx.Part(tank, PrimitiveType.Cube, new Vector3(-1.4f, 1.3f, 0f), new Vector3(0.12f, 0.12f, 0.24f), Gfx.Lit(new Color(0.85f, 0.15f, 0.12f)), default, false);   // valve
        var col = new GameObject("Collider");
        col.transform.SetParent(tank, false);
        var cap = col.AddComponent<CapsuleCollider>();
        cap.center = new Vector3(0f, 2f, 0f);
        cap.radius = 0.95f;
        cap.height = 4f;
    }

    // ---------------- back: satellite dish ----------------

    void BuildBack()
    {
        Transform d = Prop("Satellite Dish", 8.6f, -7.6f, 0f);
        Color white = new Color(0.9f, 0.9f, 0.92f), dark = new Color(0.25f, 0.26f, 0.3f);
        Gfx.Part(d, PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f), new Vector3(0.8f, 0.2f, 0.8f), Gfx.Lit(new Color(0.55f, 0.55f, 0.55f)));
        Gfx.Part(d, PrimitiveType.Cylinder, new Vector3(0f, 1.4f, 0f), new Vector3(0.14f, 1.3f, 0.14f), Gfx.Lit(dark, 0.5f, 0.7f));
        dish = Gfx.Pivot(d, new Vector3(0f, 2.75f, 0f), "Dish");
        Transform tilt = Gfx.Pivot(dish, Vector3.zero, "Tilt");
        tilt.localRotation = Quaternion.Euler(-35f, 0f, 0f);
        Gfx.Part(tilt, PrimitiveType.Sphere, new Vector3(0f, 0f, 0.15f), new Vector3(1.9f, 1.9f, 0.45f), Gfx.Lit(white, 0.5f, 0.3f));
        Gfx.Part(tilt, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.6f), new Vector3(0.04f, 0.45f, 0.04f), Gfx.Lit(dark), new Vector3(90f, 0f, 0f));
        Gfx.Part(tilt, PrimitiveType.Sphere, new Vector3(0f, 0f, 1.05f), Vector3.one * 0.14f, Gfx.Glow(lab.LabColor, 3f), default, false);
        Solid(d, new Vector3(0f, 1.3f, 0f), new Vector3(0.5f, 2.6f, 0.5f));
        Gfx.SetLayer(gameObject, 2);
        // the colliders stay solid (and shootable)
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.gameObject.layer = 0;
    }

    // ---------------- moving bits ----------------

    void Update()
    {
        t += Time.deltaTime;
        if (dish != null) dish.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 0.15f) * 70f, 0f);
        if (lab == null || lab.Destroyed) return;
        // steam from the back vents (only when you're close enough to see it)
        steamCd -= Time.deltaTime;
        if (steamCd > 0f) return;
        steamCd = Random.Range(0.9f, 1.8f);
        Camera c = GameCam.I != null ? GameCam.I.cam : Camera.main;
        if (c == null || Vector3.Distance(c.transform.position, lab.transform.position) > 70f) return;
        Vector3 back = -lab.transform.forward;
        foreach (float s in new[] { -3.5f, 3.5f })
        {
            Vector3 p = lab.transform.TransformPoint(new Vector3(s, FY + 3.2f, -6.35f));
            Fx.Puff(p, new Color(0.9f, 0.9f, 0.95f, 0.35f), 2, 0.3f, 0.7f, 2.2f, Vector3.up * 1.3f + back * 0.4f);
        }
    }
}
