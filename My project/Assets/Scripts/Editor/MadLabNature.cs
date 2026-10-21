using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Trees, bushes, mushrooms, rocks, props and the glowing material spots for the Mad Lab kit.
public static partial class MadLabKitMenu
{
    // ================================================================
    // Nature
    // ================================================================

    static void MakeNature()
    {
        // trees (paint these with "Paint Trees")
        SavePrefab("Oak_A", OakParts(4, leaves), Solid.Trunk, 0.45f, 4f);
        SavePrefab("Oak_B", OakParts(5, leavesDark), Solid.Trunk, 0.45f, 4f);
        SavePrefab("Pine_A", PineParts(1, 5, 8f), Solid.Trunk, 0.32f, 6f);
        SavePrefab("Pine_B", PineParts(2, 6, 10.5f), Solid.Trunk, 0.36f, 8f);
        SavePrefab("GlowTree_A", GlowTreeParts(7, glowCyan), Solid.Trunk, 0.4f, 4f);
        SavePrefab("GlowTree_B", GlowTreeParts(8, glowPurple), Solid.Trunk, 0.4f, 4f);
        SavePrefab("Bush_A", BushParts(6, false), Solid.None);
        SavePrefab("Bush_B", BushParts(9, true), Solid.None);
        SavePrefab("Mushroom_Giant", GiantMushroomParts(10, capPurple), Solid.Trunk, 0.35f, 3.2f);
        SavePrefab("Mushroom_Giant_Orange", GiantMushroomParts(11, capOrange), Solid.Trunk, 0.35f, 3.2f);

        // rocks and crystals (drag these in)
        SavePrefab("Rock_Small", RockParts(12, new Vector3(1.3f, 0.8f, 1.1f)), Solid.Mesh);
        SavePrefab("Rock_Big", RockParts(13, new Vector3(2.8f, 1.8f, 2.4f)), Solid.Mesh);
        SavePrefab("Boulder", RockParts(14, new Vector3(5f, 3.6f, 4.4f)), Solid.Mesh);
        SavePrefab("Crystal_Cluster", CrystalClusterParts(15, crystal, 1f), Solid.Shape);
        SavePrefab("Crystal_Cluster_Pink", CrystalClusterParts(16, crystalPink, 0.8f), Solid.Shape);
        SavePrefab("Crystal_Spire", CrystalSpireParts(17), Solid.Shape);
    }

    static List<Part> OakParts(int seed, Material leaf)
    {
        var r = new System.Random(seed);
        var p = new List<Part>();
        p.Add(Cy(Vector3.zero, 0.5f, 0.28f, 3.4f, bark, 9, default, false));
        p.Add(Cy(Vector3.zero, 0.62f, 0.45f, 0.4f, bark, 9, default, false));   // root flare
        p.Add(Cy(new Vector3(0.1f, 2.6f, 0f), 0.18f, 0.08f, 1.8f, bark, 7, new Vector3(0f, 0f, -45f), false));
        p.Add(Cy(new Vector3(-0.1f, 2.9f, 0.05f), 0.16f, 0.07f, 1.6f, bark, 7, new Vector3(15f, 0f, 42f), false));
        p.Add(Cy(new Vector3(0f, 3.0f, -0.1f), 0.15f, 0.07f, 1.5f, bark, 7, new Vector3(-40f, 0f, 5f), false));
        Vector3 center = new Vector3(0f, 4.7f, 0f);
        p.Add(new Part(Lumpy(seed * 7, 0.12f), TRS(center, Vector3.zero, new Vector3(3.6f, 2.8f, 3.6f)), leaf));
        int clumps = 7 + r.Next(3);
        for (int c = 0; c < clumps; c++)
        {
            float a = c * Mathf.PI * 2f / clumps + (float)r.NextDouble() * 0.5f;
            float d = 1.5f + (float)r.NextDouble() * 0.6f;
            Vector3 pos = center + new Vector3(Mathf.Cos(a) * d, (float)r.NextDouble() * 1.2f - 0.4f, Mathf.Sin(a) * d);
            float s = 1.8f + (float)r.NextDouble() * 0.9f;
            p.Add(new Part(Lumpy(seed * 7 + c + 1, 0.15f), TRS(pos, new Vector3(0f, c * 40f, 0f), new Vector3(s, s * 0.82f, s)), r.Next(4) == 0 ? leavesDark : leaf));
        }
        p.Add(new Part(Lumpy(seed * 7 + 99, 0.12f), TRS(center + new Vector3(0.2f, 1.4f, -0.1f), Vector3.zero, new Vector3(2.4f, 1.9f, 2.4f)), leaf));
        return p;
    }

    static List<Part> PineParts(int seed, int tiers, float height)
    {
        var r = new System.Random(seed);
        var p = new List<Part>();
        p.Add(Cy(Vector3.zero, 0.3f, 0.08f, height * 0.75f, bark, 8, default, false));
        float y = height * 0.18f, width = height * 0.42f, step = (height - y) / (tiers + 0.6f);
        for (int t = 0; t < tiers; t++)
        {
            float lean = (float)(r.NextDouble() - 0.5) * 6f;
            p.Add(new Part(Cone(9, width / 2f, step * 1.9f, seed * 10 + t), TRS(new Vector3(0f, y, 0f), new Vector3(lean, t * 23f, lean), Vector3.one), needles));
            y += step;
            width *= 0.78f;
        }
        return p;
    }

    // A mutant tree: a twisted trunk, droopy teal leaves and glowing bulbs hanging underneath.
    static List<Part> GlowTreeParts(int seed, Material bulb)
    {
        var r = new System.Random(seed);
        var p = new List<Part>();
        Vector3 at = Vector3.zero;
        float rad = 0.42f;
        float[] tilt = { 10f, -14f, 12f, -8f };
        for (int i = 0; i < 4; i++)
        {
            float len = 1.1f;
            Vector3 e = new Vector3(tilt[i], i * 70f, 0f);
            p.Add(Cy(at, rad, rad * 0.78f, len + 0.1f, barkDark, 8, e, false));
            at += Quaternion.Euler(e) * Vector3.up * len;
            rad *= 0.78f;
        }
        Vector3 top = at;
        // canopy blobs, flattened and drooping
        for (int c = 0; c < 6; c++)
        {
            float a = c * Mathf.PI * 2f / 6f + (float)r.NextDouble() * 0.4f;
            float d = c == 0 ? 0f : 1.4f + (float)r.NextDouble() * 0.5f;
            Vector3 pos = top + new Vector3(Mathf.Cos(a) * d, c == 0 ? 0.5f : -0.2f - (float)r.NextDouble() * 0.5f, Mathf.Sin(a) * d);
            float s = c == 0 ? 3.4f : 2f + (float)r.NextDouble() * 0.6f;
            p.Add(new Part(Lumpy(seed * 13 + c, 0.18f), TRS(pos, new Vector3(0f, c * 33f, 0f), new Vector3(s, s * 0.55f, s)), leavesTeal));
            if (c > 0)
            {
                // a vine with a glowing bulb
                Vector3 hang = pos + new Vector3(Mathf.Cos(a) * 0.5f, -0.2f, Mathf.Sin(a) * 0.5f);
                float l = 0.8f + (float)r.NextDouble() * 0.9f;
                p.Add(Cy(hang - Vector3.up * l, 0.025f, 0.035f, l, leavesTeal, 5, default, false));
                p.Add(Ball(hang - Vector3.up * (l + 0.18f), new Vector3(0.36f, 0.42f, 0.36f), bulb, seed + c, 0.05f));
            }
        }
        // roots
        for (int i = 0; i < 4; i++)
            p.Add(Cy(new Vector3(0f, 0.25f, 0f), 0.16f, 0.05f, 1.1f, barkDark, 6, new Vector3(100f, i * 90f + 30f, 0f), false));
        return p;
    }

    static List<Part> BushParts(int seed, bool berries)
    {
        var r = new System.Random(seed);
        var p = new List<Part>();
        int n = 5;
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            float d = i == 0 ? 0f : 0.5f;
            float s = i == 0 ? 1.5f : 0.95f + (float)r.NextDouble() * 0.35f;
            Vector3 pos = new Vector3(Mathf.Cos(a) * d, s * 0.35f, Mathf.Sin(a) * d);
            p.Add(new Part(Lumpy(seed * 5 + i, 0.16f), TRS(pos, new Vector3(0f, i * 50f, 0f), new Vector3(s, s * 0.75f, s)), i % 2 == 0 ? leaves : leavesDark));
        }
        if (berries)
            for (int i = 0; i < 9; i++)
            {
                float a = i * 2.4f;
                Vector3 pos = new Vector3(Mathf.Cos(a) * 0.75f, 0.5f + (i % 3) * 0.18f, Mathf.Sin(a) * 0.75f);
                p.Add(Ball(pos, 0.13f, glowPurple));
            }
        return p;
    }

    static List<Part> GiantMushroomParts(int seed, Material cap)
    {
        var r = new System.Random(seed);
        var p = new List<Part>();
        // slightly bent stem
        p.Add(Cy(Vector3.zero, 0.45f, 0.32f, 1.7f, stem, 10, new Vector3(4f, 0f, 0f), false));
        p.Add(Cy(new Vector3(0f, 1.65f, 0.1f), 0.32f, 0.28f, 1.8f, stem, 10, new Vector3(-6f, 0f, 0f), false));
        Vector3 top = new Vector3(0f, 3.4f, 0f);
        p.Add(new Part(Dome(14, 2.3f), TRS(top, Vector3.zero, new Vector3(1f, 0.55f, 1f)), cap));
        p.Add(new Part(Disc(14, 2.25f), TRS(top + Vector3.up * 0.01f, new Vector3(180f, 0f, 0f), Vector3.one), stem));   // underside
        p.Add(Cy(top - Vector3.up * 0.25f, 0.28f, 0.6f, 0.26f, stem, 10, default, false));                                // ring under the cap
        // glowing spots
        for (int i = 0; i < 9; i++)
        {
            float a = i * 2.39f, d = 0.4f + (i % 3) * 0.55f;
            float y = Mathf.Sqrt(Mathf.Max(0f, 2.3f * 2.3f - d * d)) * 0.55f;
            p.Add(Ball(top + new Vector3(Mathf.Cos(a) * d, y - 0.02f, Mathf.Sin(a) * d), new Vector3(0.3f, 0.12f, 0.3f), i % 2 == 0 ? glowCyan : chalk));
        }
        // baby mushrooms at the foot
        for (int i = 0; i < 4; i++)
        {
            float a = i * 1.7f + (float)r.NextDouble();
            Vector3 b = new Vector3(Mathf.Cos(a) * 0.9f, 0f, Mathf.Sin(a) * 0.9f);
            float h = 0.25f + (float)r.NextDouble() * 0.3f;
            p.Add(Cy(b, 0.06f, 0.05f, h, stem, 6, default, false));
            p.Add(new Part(Dome(8, 0.2f + h * 0.4f), TRS(b + Vector3.up * h, Vector3.zero, new Vector3(1f, 0.7f, 1f)), cap));
        }
        return p;
    }

    static List<Part> RockParts(int seed, Vector3 size)
        => new List<Part> { new Part(Lumpy(seed, 0.28f), TRS(new Vector3(0f, size.y * 0.25f, 0f), new Vector3(0f, seed * 37f, 0f), size), rock) };

    static List<Part> CrystalClusterParts(int seed, Material m, float scale)
    {
        var r = new System.Random(seed);
        var p = new List<Part>();
        p.Add(new Part(Lumpy(seed, 0.3f), TRS(new Vector3(0f, 0.15f, 0f), Vector3.zero, new Vector3(2.2f, 0.8f, 2f) * scale), rockDark));
        for (int i = 0; i < 7; i++)
        {
            float a = i * 0.9f + (float)r.NextDouble();
            float d = i == 0 ? 0f : 0.35f + (float)r.NextDouble() * 0.45f;
            float h = (i == 0 ? 2.4f : 0.9f + (float)r.NextDouble() * 1.2f) * scale;
            Vector3 pos = new Vector3(Mathf.Cos(a) * d, 0.1f, Mathf.Sin(a) * d) * scale;
            Vector3 tilt = new Vector3(Mathf.Sin(a) * 25f * (i == 0 ? 0.3f : 1f), 0f, -Mathf.Cos(a) * 25f * (i == 0 ? 0.3f : 1f));
            p.Add(CrystalPart(pos, (0.12f + h * 0.08f) * scale, h, tilt, m));
        }
        return p;
    }

    static List<Part> CrystalSpireParts(int seed)
    {
        var p = CrystalClusterParts(seed, crystal, 1.6f);
        p.Add(CrystalPart(Vector3.zero, 0.55f, 6f, new Vector3(4f, 0f, -3f), crystal));
        return p;
    }

    // ================================================================
    // Props
    // ================================================================

    static void MakeProps()
    {
        SavePrefab("Crate", CrateParts(Vector3.zero, 1f, 0f), Solid.Box);
        var stack = CrateParts(Vector3.zero, 1.1f, 0f);
        stack.AddRange(CrateParts(new Vector3(1.15f, 0f, 0.1f), 1f, 8f));
        stack.AddRange(CrateParts(new Vector3(0.5f, 1.1f, 0f), 0.9f, -12f));
        SavePrefab("Crate_Stack", stack, Solid.Shape);
        SavePrefab("Barrel_Toxic", BarrelParts(Vector3.zero, barrelGreen, true), Solid.Box);
        SavePrefab("Barrel_Blue", BarrelParts(Vector3.zero, barrelBlue, false), Solid.Box);
        var group = BarrelParts(Vector3.zero, barrelGreen, true);
        group.AddRange(BarrelParts(new Vector3(0.9f, 0f, 0.2f), barrelBlue, false));
        group.AddRange(BarrelParts(new Vector3(0.4f, 0f, -0.75f), barrelGreen, true));
        SavePrefab("Barrel_Group", group, Solid.Shape);
        SavePrefab("Fence", FenceParts(), Solid.Box);
        SavePrefab("Pipe_Run", PipeParts(), Solid.Shape);
        SavePrefab("Scrap_Pile", ScrapParts(21, 1.6f), Solid.Shape);
        SavePrefab("Generator", GeneratorParts(), Solid.Box);
        SavePrefab("Tesla_Tower", TeslaParts(), Solid.Trunk, 0.6f, 7f);
        SavePrefab("Satellite_Dish", DishParts(), Solid.Shape);
        SavePrefab("Lamp_Post", LampParts(), Solid.Trunk, 0.15f, 4.6f, go =>
        {
            Light l = AddLight(go.transform, "Light", new Vector3(0.9f, 4.3f, 0f), new Color(1f, 0.9f, 0.7f), 13f, 1.6f);
            l.shadows = LightShadows.None;
        });
        SavePrefab("Danger_Sign", SignParts(), Solid.Box);
        SavePrefab("Pond", PondParts(), Solid.None);
        SavePrefab("Toxic_Pool", ToxicPoolParts(), Solid.None, 0f, 0f, go =>
        {
            go.AddComponent<HazardPool>().radius = 7.5f;
            AddLight(go.transform, "Glow", new Vector3(0f, 1.5f, 0f), new Color(0.5f, 1f, 0.3f), 16f, 2f);
        });
    }

    static List<Part> CrateParts(Vector3 at, float s, float yaw)
    {
        var p = new List<Part>();
        Matrix4x4 m = TRS(at + Vector3.up * s / 2f, new Vector3(0f, yaw, 0f), Vector3.one);
        System.Action<Vector3, Vector3, Material> add = (c, size, mat) => p.Add(new Part(Box(size), m * TRS(c, Vector3.zero, Vector3.one), mat));
        add(Vector3.zero, Vector3.one * s * 0.96f, wood);
        float e = s / 2f, t = s * 0.08f;
        for (int a = -1; a <= 1; a += 2)
            for (int b = -1; b <= 1; b += 2)
            {
                add(new Vector3(a * e, 0f, b * e), new Vector3(t, s, t), woodDark);       // vertical edges
                add(new Vector3(a * e, b * e, 0f), new Vector3(t, t, s), woodDark);       // edges along z
                add(new Vector3(0f, a * e, b * e), new Vector3(s, t, t), woodDark);       // edges along x
            }
        add(new Vector3(0f, 0f, e + 0.01f), new Vector3(s * 0.1f, s * 1.15f, 0.02f), woodDark);    // diagonal-ish brace
        return p;
    }

    static List<Part> BarrelParts(Vector3 at, Material body, bool toxicTop)
    {
        var p = new List<Part>();
        p.Add(Cy(at, 0.42f, 0.42f, 1.15f, body, 14));
        for (int i = 0; i < 3; i++) p.Add(new Part(Tube(14, 0.45f, 0.4f, 0.05f), TRS(at + Vector3.up * (0.05f + i * 0.52f), Vector3.zero, Vector3.one), metalDark));
        p.Add(new Part(Tube(14, 0.432f, 0.41f, 0.22f), TRS(at + Vector3.up * 0.45f, Vector3.zero, Vector3.one), hazard));
        if (toxicTop)
        {
            p.Add(Ball(at + new Vector3(0.12f, 1.15f, 0.05f), new Vector3(0.5f, 0.12f, 0.45f), toxic));
            p.Add(Ball(at + new Vector3(0.38f, 0.9f, 0.1f), new Vector3(0.1f, 0.4f, 0.1f), toxic));   // drip
        }
        return p;
    }

    static List<Part> FenceParts()
    {
        var p = new List<Part>();
        foreach (float x in new[] { -1.5f, 1.5f })
        {
            p.Add(Cy(new Vector3(x, 0f, 0f), 0.07f, 0.07f, 1.3f, metalDark, 8));
            p.Add(Ball(new Vector3(x, 1.32f, 0f), 0.14f, metalDark));
        }
        p.Add(B(new Vector3(0f, 1.05f, 0f), new Vector3(3f, 0.16f, 0.06f), hazard));
        p.Add(B(new Vector3(0f, 0.55f, 0f), new Vector3(3f, 0.1f, 0.05f), metalDark));
        p.Add(B(new Vector3(0f, 0.8f, 0.04f), new Vector3(0.7f, 0.45f, 0.03f), chalk));     // warning sign
        p.Add(B(new Vector3(0f, 0.8f, 0.06f), new Vector3(0.3f, 0.3f, 0.02f), redGlow, new Vector3(0f, 0f, 45f)));
        return p;
    }

    static List<Part> PipeParts()
    {
        var p = new List<Part>();
        p.Add(Cy(new Vector3(-4f, 1.6f, 0f), 0.45f, 0.45f, 8f, metal, 14, new Vector3(0f, 0f, -90f)));
        for (int i = 0; i < 5; i++)
            p.Add(new Part(Tube(14, 0.55f, 0.43f, 0.18f), TRS(new Vector3(-4f + i * 2f, 1.6f, 0f), new Vector3(0f, 0f, -90f), Vector3.one), metalDark));
        foreach (float x in new[] { -3f, 0f, 3f })
        {
            p.Add(B(new Vector3(x - 0.45f, 0.6f, 0f), new Vector3(0.14f, 1.4f, 0.14f), metalDark, new Vector3(0f, 0f, -18f)));
            p.Add(B(new Vector3(x + 0.45f, 0.6f, 0f), new Vector3(0.14f, 1.4f, 0.14f), metalDark, new Vector3(0f, 0f, 18f)));
            p.Add(B(new Vector3(x, 1.1f, 0f), new Vector3(1.0f, 0.12f, 0.16f), metalDark));
        }
        // valve wheel
        p.Add(Cy(new Vector3(1f, 2.05f, 0f), 0.06f, 0.06f, 0.35f, metalDark, 6));
        p.Add(new Part(Tube(12, 0.32f, 0.26f, 0.06f), TRS(new Vector3(1f, 2.4f, 0f), Vector3.zero, Vector3.one), redGlow));
        p.Add(B(new Vector3(1f, 2.43f, 0f), new Vector3(0.6f, 0.04f, 0.05f), metalDark));
        p.Add(B(new Vector3(1f, 2.43f, 0f), new Vector3(0.05f, 0.04f, 0.6f), metalDark));
        // leaking goo
        p.Add(Ball(new Vector3(-2f, 1.1f, 0.3f), new Vector3(0.15f, 0.6f, 0.15f), toxic));
        p.Add(Ball(new Vector3(-2f, 0.02f, 0.4f), new Vector3(1.2f, 0.06f, 0.9f), toxic));
        return p;
    }

    static List<Part> ScrapParts(int seed, float s)
    {
        var r = new System.Random(seed);
        var p = new List<Part>();
        for (int i = 0; i < 14; i++)
        {
            Vector3 pos = new Vector3(((float)r.NextDouble() - 0.5f) * 2f, (float)r.NextDouble() * 0.6f, ((float)r.NextDouble() - 0.5f) * 2f) * s;
            Vector3 e = new Vector3(r.Next(360), r.Next(360), r.Next(360));
            Material m = r.Next(3) == 0 ? metal : r.Next(2) == 0 ? rust : metalDark;
            switch (i % 4)
            {
                case 0: p.Add(B(pos, new Vector3(0.9f, 0.06f, 0.6f) * s, m, e)); break;                              // plate
                case 1: p.Add(Cy(pos, 0.12f * s, 0.12f * s, 1.2f * s, m, 8, e)); break;                             // pipe
                case 2: p.Add(B(pos, new Vector3(0.4f, 0.3f, 0.35f) * s, m, e)); break;                              // chunk
                default: p.Add(Cy(pos, 0.3f * s, 0.3f * s, 0.08f * s, brass, 10, e)); break;                        // gear
            }
        }
        p.Add(new Part(Lumpy(seed, 0.3f), TRS(new Vector3(0f, 0.1f, 0f), Vector3.zero, new Vector3(2.2f, 0.7f, 2f) * s), rust));
        return p;
    }

    static List<Part> GeneratorParts()
    {
        var p = new List<Part>();
        p.Add(B(new Vector3(0f, 0.15f, 0f), new Vector3(3.2f, 0.3f, 2f), concrete));
        p.Add(B(new Vector3(0f, 1.1f, 0f), new Vector3(2.8f, 1.6f, 1.6f), metalDark));
        p.Add(B(new Vector3(0f, 1.1f, 0.81f), new Vector3(1.6f, 1f, 0.02f), grate));
        p.Add(B(new Vector3(0f, 0.38f, 0.81f), new Vector3(2.8f, 0.14f, 0.03f), hazard));
        p.Add(B(new Vector3(1.0f, 1.55f, 0.82f), new Vector3(0.4f, 0.25f, 0.02f), screen));
        p.Add(Cy(new Vector3(-1f, 1.9f, -0.3f), 0.16f, 0.16f, 1.2f, metal, 10));
        p.Add(Cy(new Vector3(-1f, 3.05f, -0.3f), 0.2f, 0.22f, 0.12f, metalDark, 10));
        for (int i = 0; i < 3; i++) p.Add(Ball(new Vector3(-0.4f + i * 0.3f, 1.95f, 0.5f), 0.12f, i == 0 ? redGlow : glowGreen));
        p.Add(Cy(new Vector3(1.6f, 0.9f, 0f), 0.08f, 0.08f, 2.2f, rubber, 8, new Vector3(0f, 0f, -80f), false));   // cable
        return p;
    }

    static List<Part> TeslaParts()
    {
        var p = new List<Part>();
        p.Add(Cy(Vector3.zero, 1.1f, 0.9f, 0.4f, concrete, 10));
        p.Add(Cy(new Vector3(0f, 0.4f, 0f), 0.55f, 0.45f, 1f, metalDark, 10));
        p.Add(Cy(new Vector3(0f, 1.4f, 0f), 0.3f, 0.3f, 3.6f, brass, 12));
        for (int i = 0; i < 14; i++) p.Add(new Part(Tube(12, 0.36f, 0.29f, 0.06f), TRS(new Vector3(0f, 1.6f + i * 0.24f, 0f), Vector3.zero, Vector3.one), metalDark));
        p.Add(new Part(Tube(16, 1.2f, 0.5f, 0.45f), TRS(new Vector3(0f, 5.2f, 0f), Vector3.zero, Vector3.one), metal));
        p.Add(Ball(new Vector3(0f, 5.6f, 0f), 0.8f, metal));
        p.Add(Ball(new Vector3(0f, 6.3f, 0f), 0.35f, glowCyan));
        p.Add(Cy(new Vector3(0f, 5.9f, 0f), 0.05f, 0.03f, 0.5f, metal, 6));
        return p;
    }

    static List<Part> DishParts()
    {
        var p = new List<Part>();
        p.Add(B(new Vector3(0f, 0.2f, 0f), new Vector3(2f, 0.4f, 2f), concrete));
        p.Add(Cy(new Vector3(0f, 0.4f, 0f), 0.25f, 0.2f, 3.2f, metal, 10));
        p.Add(B(new Vector3(0f, 3.6f, 0f), new Vector3(0.6f, 0.6f, 0.6f), metalDark));
        Matrix4x4 dish = TRS(new Vector3(0f, 4.4f, 0.4f), new Vector3(-55f, 0f, 0f), Vector3.one);
        p.Add(new Part(Dome(16, 2.4f), dish * TRS(Vector3.zero, new Vector3(180f, 0f, 0f), new Vector3(1f, 0.35f, 1f)), metal));
        p.Add(new Part(Disc(16, 2.38f), dish * TRS(new Vector3(0f, 0.01f, 0f), Vector3.zero, Vector3.one), chalk));
        p.Add(new Part(Cyl(6, 0.05f, 0.05f, 1.8f, false), dish, metalDark));
        p.Add(new Part(Lumpy(3, 0f), dish * TRS(new Vector3(0f, 1.85f, 0f), Vector3.zero, Vector3.one * 0.3f), redGlow));
        return p;
    }

    static List<Part> LampParts()
    {
        var p = new List<Part>();
        p.Add(Cy(Vector3.zero, 0.25f, 0.2f, 0.4f, concrete, 8));
        p.Add(Cy(new Vector3(0f, 0.4f, 0f), 0.09f, 0.07f, 4.2f, metalDark, 8));
        p.Add(B(new Vector3(0.45f, 4.55f, 0f), new Vector3(1f, 0.08f, 0.08f), metalDark));
        p.Add(B(new Vector3(0.9f, 4.45f, 0f), new Vector3(0.5f, 0.15f, 0.3f), metalDark));
        p.Add(B(new Vector3(0.9f, 4.36f, 0f), new Vector3(0.42f, 0.03f, 0.24f), white));
        p.Add(B(new Vector3(0f, 1.4f, 0.08f), new Vector3(0.18f, 0.6f, 0.02f), hazard));
        return p;
    }

    static List<Part> SignParts()
    {
        var p = new List<Part>();
        foreach (float x in new[] { -0.6f, 0.6f }) p.Add(Cy(new Vector3(x, 0f, 0f), 0.05f, 0.05f, 1.6f, metalDark, 6));
        p.Add(B(new Vector3(0f, 1.4f, 0f), new Vector3(1.6f, 0.8f, 0.05f), chalk));
        p.Add(B(new Vector3(0f, 1.4f, 0.03f), new Vector3(1.5f, 0.12f, 0.02f), hazard));
        p.Add(B(new Vector3(0f, 1.62f, 0.03f), new Vector3(1.3f, 0.08f, 0.02f), redGlow));
        p.Add(B(new Vector3(0f, 1.2f, 0.03f), new Vector3(0.3f, 0.3f, 0.02f), glowGreen, new Vector3(0f, 0f, 45f)));
        return p;
    }

    static List<Part> PondParts()
    {
        var p = new List<Part>();
        p.Add(new Part(Disc(24, 7.5f), Matrix4x4.identity, water));
        var r = new System.Random(31);
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI * 2f / 16f + (float)r.NextDouble() * 0.2f;
            float s = 0.7f + (float)r.NextDouble() * 0.8f;
            p.Add(new Part(Lumpy(40 + i, 0.3f), TRS(new Vector3(Mathf.Cos(a) * 7.4f, 0f, Mathf.Sin(a) * 7.4f), Vector3.zero, new Vector3(s * 1.4f, s * 0.6f, s)), rock));
        }
        // lily pads
        for (int i = 0; i < 6; i++)
        {
            float a = i * 1.1f;
            p.Add(new Part(Disc(8, 0.45f), TRS(new Vector3(Mathf.Cos(a) * (2f + i * 0.6f), 0.02f, Mathf.Sin(a) * (2f + i * 0.6f)), Vector3.zero, Vector3.one), leaves));
        }
        return p;
    }

    static List<Part> ToxicPoolParts()
    {
        var p = new List<Part>();
        p.Add(new Part(Disc(24, 8f), Matrix4x4.identity, toxic));
        var r = new System.Random(77);
        for (int i = 0; i < 18; i++)
        {
            float a = i * Mathf.PI * 2f / 18f + (float)r.NextDouble() * 0.2f;
            float s = 0.6f + (float)r.NextDouble() * 0.7f;
            p.Add(new Part(Lumpy(80 + i, 0.3f), TRS(new Vector3(Mathf.Cos(a) * 7.9f, 0f, Mathf.Sin(a) * 7.9f), Vector3.zero, new Vector3(s * 1.4f, s * 0.6f, s)), rockDark));
        }
        // half-sunk barrels and bubbles
        p.AddRange(BarrelParts(new Vector3(3f, -0.5f, 2f), barrelGreen, false));
        p.Add(Ball(new Vector3(-2f, 0.05f, -3f), new Vector3(0.6f, 0.3f, 0.6f), toxic));
        p.Add(Ball(new Vector3(1f, 0.05f, -1f), new Vector3(0.4f, 0.2f, 0.4f), toxic));
        return p;
    }

    // ================================================================
    // Material spots (press E in the game to gather)
    // ================================================================

    static void MakeNodes()
    {
        SaveNode("Node_Herb", Mat.Herb, HerbParts(), Solid.None, new Color(0.5f, 1f, 0.4f));
        SaveNode("Node_Slime", Mat.Slime, SlimeParts(), Solid.None, new Color(0.3f, 1f, 0.75f));
        SaveNode("Node_Crystal", Mat.Crystal, CrystalNodeParts(), Solid.Shape, new Color(0.4f, 0.85f, 1f));
        SaveNode("Node_Metal", Mat.Metal, MetalNodeParts(), Solid.Shape, new Color(0.8f, 0.85f, 1f));
        SaveNode("Node_Chemical", Mat.Chemical, ChemicalNodeParts(), Solid.Shape, new Color(0.8f, 0.4f, 1f));
    }

    static void SaveNode(string name, Mat mat, List<Part> parts, Solid col, Color light)
    {
        SavePrefab(name, parts, col, 0f, 0f, go =>
        {
            var n = go.AddComponent<MaterialNode>();
            n.mat = mat;     // (no light: they glow by themselves, and 70 lights would slow the game down)
        });
    }

    static List<Part> HerbParts()
    {
        var p = new List<Part>();
        var r = new System.Random(51);
        p.Add(new Part(Lumpy(50, 0.2f), TRS(new Vector3(0f, 0.02f, 0f), Vector3.zero, new Vector3(1.1f, 0.2f, 1.1f)), MakeMat("Soil", "Dirt", new Color(0.6f, 0.5f, 0.4f))));
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 2f / 8f + (float)r.NextDouble() * 0.3f;
            float tilt = 35f + (float)r.NextDouble() * 25f;
            Matrix4x4 leaf = TRS(new Vector3(0f, 0.05f, 0f), new Vector3(0f, -a * Mathf.Rad2Deg, 0f), Vector3.one) * TRS(Vector3.zero, new Vector3(0f, 0f, -tilt), Vector3.one);
            p.Add(new Part(Lumpy(60 + i, 0.1f), leaf * TRS(new Vector3(0f, 0.38f, 0f), Vector3.zero, new Vector3(0.22f, 0.8f, 0.07f)), herbLeaf));
        }
        for (int i = 0; i < 5; i++)
        {
            float a = i * 1.3f, d = 0.12f + (i % 2) * 0.1f, h = 0.6f + (float)r.NextDouble() * 0.3f;
            Vector3 b = new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
            p.Add(Cy(b, 0.02f, 0.015f, h, herbLeaf, 5, default, false));
            p.Add(Ball(b + Vector3.up * (h + 0.05f), 0.16f, glowGreen));
        }
        return p;
    }

    static List<Part> SlimeParts()
    {
        var p = new List<Part>();
        p.Add(new Part(Disc(16, 1.2f), TRS(new Vector3(0f, 0.03f, 0f), Vector3.zero, Vector3.one), slime));
        float[,] blobs = { { 0f, 0f, 0.9f }, { 0.55f, 0.3f, 0.55f }, { -0.5f, 0.35f, 0.5f }, { 0.2f, -0.6f, 0.45f }, { -0.3f, -0.4f, 0.35f } };
        for (int i = 0; i < 5; i++)
        {
            float s = blobs[i, 2];
            p.Add(Ball(new Vector3(blobs[i, 0], s * 0.3f, blobs[i, 1]), new Vector3(s, s * 0.75f, s), slime, 70 + i, 0.12f));
        }
        p.Add(Ball(new Vector3(0.1f, 0.65f, 0.1f), 0.18f, chalk));   // a little shine
        return p;
    }

    static List<Part> CrystalNodeParts()
    {
        var p = new List<Part>();
        p.Add(new Part(Lumpy(90, 0.3f), TRS(new Vector3(0f, 0.1f, 0f), Vector3.zero, new Vector3(1.4f, 0.5f, 1.2f)), rockDark));
        float[,] c = { { 0f, 0f, 1.4f, 0f, 0f }, { 0.35f, 0.1f, 0.9f, 20f, -15f }, { -0.3f, 0.15f, 0.8f, -10f, 25f }, { 0.05f, -0.35f, 0.7f, -25f, 5f }, { -0.15f, 0.4f, 0.6f, 25f, 10f } };
        for (int i = 0; i < 5; i++)
            p.Add(CrystalPart(new Vector3(c[i, 0], 0.15f, c[i, 1]), 0.1f + c[i, 2] * 0.07f, c[i, 2], new Vector3(c[i, 3], 0f, c[i, 4]), crystal));
        return p;
    }

    static List<Part> MetalNodeParts()
    {
        var p = ScrapParts(95, 0.8f);
        // a big gear on top so it reads as "metal"
        p.Add(Cy(new Vector3(0f, 0.5f, 0f), 0.45f, 0.45f, 0.1f, brass, 12, new Vector3(70f, 20f, 0f)));
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f;
            p.Add(B(new Vector3(0f, 0.55f, 0f) + Quaternion.Euler(70f, 20f, 0f) * (Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.05f, 0.5f)),
                new Vector3(0.12f, 0.1f, 0.14f), brass, new Vector3(70f, 20f, 0f) + new Vector3(0f, a, 0f)));
        }
        return p;
    }

    static List<Part> ChemicalNodeParts()
    {
        var p = new List<Part>();
        p.Add(Cy(Vector3.zero, 0.55f, 0.6f, 0.8f, metalDark, 12));
        p.Add(new Part(Tube(12, 0.62f, 0.5f, 0.08f), TRS(new Vector3(0f, 0.78f, 0f), Vector3.zero, Vector3.one), hazard));
        p.Add(new Part(Disc(12, 0.52f), TRS(new Vector3(0f, 0.75f, 0f), Vector3.zero, Vector3.one), chem));
        p.Add(Ball(new Vector3(0.2f, 0.82f, -0.1f), new Vector3(0.25f, 0.15f, 0.25f), chem));
        p.Add(Ball(new Vector3(0.6f, 0.4f, 0.2f), new Vector3(0.12f, 0.6f, 0.12f), chem));                // leak
        p.Add(new Part(Disc(12, 0.9f), TRS(new Vector3(0.8f, 0.02f, 0.3f), Vector3.zero, new Vector3(1f, 1f, 0.7f)), chem));   // puddle
        // flasks
        for (int i = 0; i < 3; i++)
        {
            Vector3 b = new Vector3(-0.85f + i * 0.25f, 0f, 0.55f + (i % 2) * 0.2f);
            p.Add(Ball(b + Vector3.up * 0.14f, 0.26f, glass));
            p.Add(Ball(b + Vector3.up * 0.12f, 0.2f, i == 1 ? glowGreen : chem));
            p.Add(Cy(b + Vector3.up * 0.25f, 0.04f, 0.04f, 0.15f, glass, 6));
        }
        return p;
    }
}
