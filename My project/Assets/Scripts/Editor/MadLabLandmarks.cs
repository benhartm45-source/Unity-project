using System.Collections.Generic;
using UnityEngine;

// The bigger map's extra stuff for the Mad Lab kit (made by Tools > Mad Lab > 1. Make Lab Kit):
//
//   - 20 more material spots: Node_Berry, Node_Wood, Node_Sap, Node_Mushroom, Node_Moss, Node_Petal (out in the wild),
//     Node_Coal, Node_IronOre, Node_GoldOre, Node_Sulfur, Node_Glowstone, Node_Uranium, Node_Obsidian (caves),
//     Node_Circuit, Node_CopperWire, Node_Battery, Node_Glass, Node_Plastic (office), Node_Meteorite, Node_Stardust (crater)
//   - Office_Building: an abandoned two-storey office with cubicles, stairs, a server room and a caved-in roof
//   - Cave: a rocky cave you can walk into (stalactites, glowing mushrooms, a few lights)
//   - Meteor and Meteor_Shard: the glowing space rock in the crater
//
// Like everything else in the kit, they're normal prefabs in Assets/MadLab/Prefabs - drag them in anywhere.
public static partial class MadLabKitMenu
{
    static Material coal, obsidian, gold, copper, pcb, plasticW, plasticR, plasticB, fabric, officeWall, carpet, meteorRock, moss, capRed, windowGlass;

    static Material G(Mat m) => binGlow[(int)m];

    static void MakeMoreMaterials()
    {
        coal = MakeMat("Coal", "Rock", new Color(0.12f, 0.12f, 0.13f), 0.6f, 0.3f);
        obsidian = MakeMat("Obsidian", null, new Color(0.09f, 0.05f, 0.14f), 0.95f, 0.3f, 0.35f);
        gold = MakeMat("Gold", null, new Color(1f, 0.78f, 0.25f), 0.85f, 1f, 0.6f);
        copper = MakeMat("Copper", null, new Color(0.9f, 0.5f, 0.28f), 0.8f, 1f);
        pcb = MakeMat("PCB", null, new Color(0.1f, 0.45f, 0.2f), 0.5f);
        plasticW = MakeMat("Plastic_White", null, new Color(0.92f, 0.92f, 0.9f), 0.6f);
        plasticR = MakeMat("Plastic_Red", null, new Color(0.85f, 0.2f, 0.2f), 0.6f);
        plasticB = MakeMat("Plastic_Blue", null, new Color(0.2f, 0.4f, 0.85f), 0.6f);
        fabric = MakeMat("Office_Fabric", null, new Color(0.35f, 0.38f, 0.45f), 0.05f);
        officeWall = MakeMat("Office_Wall", "Lab_Wall", new Color(0.78f, 0.75f, 0.68f), 0.2f);
        carpet = MakeMat("Carpet", "Gravel", new Color(0.38f, 0.38f, 0.44f), 0.05f);
        meteorRock = MakeMat("Meteor_Rock", "Rock", new Color(0.28f, 0.22f, 0.22f), 0.3f, 0.3f);
        moss = MakeMat("Moss", "Leaves", new Color(0.45f, 0.75f, 0.3f), 0.1f, 0f, 0.25f);
        capRed = MakeMat("Mushroom_Cap_Red", "Mushroom", new Color(1f, 0.4f, 0.35f), 0.35f);
        windowGlass = MakeMat("Window_Glass", null, new Color(0.45f, 0.62f, 0.72f), 0.95f, 0.2f, 0.12f);
    }

    // ================================================================
    // 20 more material spots
    // ================================================================

    static void MakeMoreNodes()
    {
        SaveNode("Node_Berry", Mat.Berry, BerryParts(), Solid.None, Color.white);
        SaveNode("Node_Wood", Mat.Wood, WoodParts(), Solid.Box, Color.white);
        SaveNode("Node_Sap", Mat.Sap, SapParts(), Solid.Box, Color.white);
        SaveNode("Node_Mushroom", Mat.Mushroom, MushroomParts(), Solid.None, Color.white);
        SaveNode("Node_Moss", Mat.Moss, MossParts(), Solid.Mesh, Color.white);
        SaveNode("Node_Petal", Mat.Petal, PetalParts(), Solid.None, Color.white);
        SaveNode("Node_Coal", Mat.Coal, OreParts(201, coal, false), Solid.Mesh, Color.white);
        SaveNode("Node_IronOre", Mat.IronOre, OreParts(202, G(Mat.IronOre), false), Solid.Mesh, Color.white);
        SaveNode("Node_GoldOre", Mat.GoldOre, OreParts(203, gold, true), Solid.Mesh, Color.white);
        SaveNode("Node_Sulfur", Mat.Sulfur, SulfurParts(), Solid.None, Color.white);
        SaveNode("Node_Glowstone", Mat.Glowstone, GlowstoneParts(), Solid.Mesh, Color.white);
        SaveNode("Node_Uranium", Mat.Uranium, UraniumParts(), Solid.Box, Color.white);
        SaveNode("Node_Obsidian", Mat.Obsidian, ObsidianParts(), Solid.Box, Color.white);
        SaveNode("Node_Circuit", Mat.Circuit, CircuitParts(), Solid.Box, Color.white);
        SaveNode("Node_CopperWire", Mat.CopperWire, WireParts(), Solid.Box, Color.white);
        SaveNode("Node_Battery", Mat.Battery, BatteryParts(), Solid.Box, Color.white);
        SaveNode("Node_Glass", Mat.Glass, GlassParts(), Solid.None, Color.white);
        SaveNode("Node_Plastic", Mat.Plastic, PlasticParts(), Solid.Box, Color.white);
        SaveNode("Node_Meteorite", Mat.Meteorite, MeteoriteParts(301, 1f), Solid.Mesh, Color.white);
        SaveNode("Node_Stardust", Mat.Stardust, StardustParts(), Solid.None, Color.white);
    }

    static List<Part> BerryParts()
    {
        var p = BushParts(41, false);
        for (int i = 0; i < 14; i++)
        {
            float a = i * 2.4f, d = 0.55f + (i % 3) * 0.15f;
            p.Add(Ball(new Vector3(Mathf.Cos(a) * d, 0.35f + (i % 4) * 0.17f, Mathf.Sin(a) * d), 0.16f, G(Mat.Berry)));
        }
        return p;
    }

    static List<Part> WoodParts()
    {
        var p = new List<Part>();
        // three logs in a little pile
        p.Add(Cy(new Vector3(-0.8f, 0.22f, -0.25f), 0.22f, 0.22f, 1.6f, bark, 8, new Vector3(0f, 0f, -90f)));
        p.Add(Cy(new Vector3(-0.8f, 0.22f, 0.25f), 0.21f, 0.21f, 1.6f, bark, 8, new Vector3(0f, 0f, -90f)));
        p.Add(Cy(new Vector3(-0.75f, 0.58f, 0f), 0.2f, 0.2f, 1.5f, bark, 8, new Vector3(0f, 0f, -90f)));
        foreach (Vector3 end in new[] { new Vector3(0.81f, 0.22f, -0.25f), new Vector3(0.81f, 0.22f, 0.25f), new Vector3(0.76f, 0.58f, 0f) })
            p.Add(new Part(Disc(8, 0.19f), TRS(end, new Vector3(0f, 0f, -90f), Vector3.one), wood));
        // a stump with an axe in it
        p.Add(Cy(new Vector3(1.2f, 0f, 0.6f), 0.4f, 0.35f, 0.5f, bark, 9));
        p.Add(new Part(Disc(9, 0.34f), TRS(new Vector3(1.2f, 0.51f, 0.6f), Vector3.zero, Vector3.one), wood));
        p.Add(B(new Vector3(1.2f, 0.75f, 0.6f), new Vector3(0.05f, 0.6f, 0.06f), woodDark, new Vector3(0f, 0f, 20f)));
        p.Add(B(new Vector3(1.12f, 0.55f, 0.6f), new Vector3(0.25f, 0.14f, 0.03f), metal, new Vector3(0f, 0f, 20f)));
        p.Add(Ball(new Vector3(0f, 0.85f, 0f), 0.12f, G(Mat.Wood)));     // a little glow so you can spot it
        return p;
    }

    static List<Part> SapParts()
    {
        var p = new List<Part>();
        p.Add(Cy(Vector3.zero, 0.55f, 0.42f, 1.6f, barkDark, 9));
        p.Add(new Part(Disc(9, 0.4f), TRS(new Vector3(0f, 1.61f, 0f), Vector3.zero, Vector3.one), wood));
        // tap and bucket
        p.Add(Cy(new Vector3(0f, 0.9f, 0.45f), 0.04f, 0.04f, 0.3f, metal, 6, new Vector3(90f, 0f, 0f)));
        p.Add(Cy(new Vector3(0f, 0f, 0.85f), 0.2f, 0.24f, 0.4f, metalDark, 10));
        p.Add(new Part(Disc(10, 0.21f), TRS(new Vector3(0f, 0.36f, 0.85f), Vector3.zero, Vector3.one), G(Mat.Sap)));
        // drips of glowing amber down the trunk
        for (int i = 0; i < 6; i++)
        {
            float a = i * 1.1f;
            p.Add(Ball(new Vector3(Mathf.Cos(a) * 0.5f, 0.5f + i * 0.17f, Mathf.Sin(a) * 0.5f), new Vector3(0.14f, 0.26f, 0.14f), G(Mat.Sap)));
        }
        return p;
    }

    static List<Part> MushroomParts()
    {
        var p = new List<Part>();
        var r = new System.Random(77);
        for (int i = 0; i < 6; i++)
        {
            float a = i * 1.05f + (float)r.NextDouble() * 0.4f, d = i == 0 ? 0f : 0.45f + (float)r.NextDouble() * 0.25f;
            Vector3 b = new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
            float h = i == 0 ? 0.75f : 0.3f + (float)r.NextDouble() * 0.3f;
            p.Add(Cy(b, 0.08f, 0.06f, h, stem, 7, default, false));
            float cap = i == 0 ? 0.42f : 0.2f + h * 0.3f;
            p.Add(new Part(Dome(10, cap), TRS(b + Vector3.up * h, Vector3.zero, new Vector3(1f, 0.7f, 1f)), capRed));
            for (int k = 0; k < 3; k++)
            {
                float sa = k * 2.1f + i;
                p.Add(Ball(b + Vector3.up * (h + cap * 0.55f) + new Vector3(Mathf.Cos(sa), 0f, Mathf.Sin(sa)) * cap * 0.45f, cap * 0.22f, G(Mat.Mushroom)));
            }
        }
        return p;
    }

    static List<Part> MossParts()
    {
        var p = new List<Part>();
        p.Add(new Part(Lumpy(110, 0.3f), TRS(new Vector3(0f, 0.3f, 0f), Vector3.zero, new Vector3(1.6f, 1.0f, 1.3f)), rockDark));
        p.Add(new Part(Lumpy(111, 0.25f), TRS(new Vector3(0.1f, 0.62f, 0f), Vector3.zero, new Vector3(1.3f, 0.45f, 1.1f)), moss));
        p.Add(new Part(Lumpy(112, 0.25f), TRS(new Vector3(-0.6f, 0.15f, 0.4f), Vector3.zero, new Vector3(0.7f, 0.25f, 0.6f)), moss));
        for (int i = 0; i < 5; i++) p.Add(Ball(new Vector3(-0.4f + i * 0.2f, 0.8f, (i % 2) * 0.2f - 0.1f), 0.08f, G(Mat.Moss)));
        return p;
    }

    static List<Part> PetalParts()
    {
        var p = new List<Part>();
        var r = new System.Random(91);
        p.Add(new Part(Lumpy(90, 0.2f), TRS(new Vector3(0f, 0.02f, 0f), Vector3.zero, new Vector3(1.4f, 0.15f, 1.4f)), leaves));
        for (int i = 0; i < 7; i++)
        {
            float a = i * 0.9f, d = i == 0 ? 0f : 0.35f + (float)r.NextDouble() * 0.3f;
            Vector3 b = new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
            float h = 0.45f + (float)r.NextDouble() * 0.35f;
            p.Add(Cy(b, 0.02f, 0.015f, h, herbLeaf, 5, default, false));
            Vector3 top = b + Vector3.up * h;
            for (int k = 0; k < 5; k++)
            {
                float pa = k * 72f + i * 20f;
                p.Add(Ball(top + Quaternion.Euler(0f, pa, 0f) * new Vector3(0.11f, 0f, 0f), new Vector3(0.16f, 0.04f, 0.1f), G(Mat.Petal)));
            }
            p.Add(Ball(top + Vector3.up * 0.02f, 0.08f, G(Mat.Sulfur)));
        }
        return p;
    }

    // A lump of rock with shiny bits sticking out of it (coal, iron, gold).
    static List<Part> OreParts(int seed, Material ore, bool nuggets)
    {
        var r = new System.Random(seed);
        var p = new List<Part>();
        p.Add(new Part(Lumpy(seed, 0.3f), TRS(new Vector3(0f, 0.35f, 0f), new Vector3(0f, seed * 13f, 0f), new Vector3(1.6f, 1.1f, 1.4f)), rockDark));
        for (int i = 0; i < 9; i++)
        {
            Vector3 d = new Vector3((float)r.NextDouble() - 0.5f, (float)r.NextDouble() * 0.6f + 0.3f, (float)r.NextDouble() - 0.5f).normalized;
            Vector3 pos = new Vector3(0f, 0.35f, 0f) + Vector3.Scale(d, new Vector3(0.78f, 0.52f, 0.68f));
            Vector3 e = new Vector3(r.Next(360), r.Next(360), r.Next(360));
            if (nuggets) p.Add(Ball(pos, 0.16f + (float)r.NextDouble() * 0.1f, ore));
            else p.Add(B(pos, Vector3.one * (0.18f + (float)r.NextDouble() * 0.14f), ore, e));
        }
        return p;
    }

    static List<Part> SulfurParts()
    {
        var p = new List<Part>();
        p.Add(new Part(Disc(14, 0.9f), TRS(new Vector3(0f, 0.03f, 0f), Vector3.zero, new Vector3(1f, 1f, 0.8f)), G(Mat.Sulfur)));
        p.Add(new Part(Lumpy(120, 0.3f), TRS(new Vector3(0f, 0.1f, 0f), Vector3.zero, new Vector3(1.1f, 0.4f, 0.9f)), rock));
        float[,] c = { { 0f, 0f, 0.9f, 0f, 0f }, { 0.3f, 0.1f, 0.6f, 25f, -10f }, { -0.25f, 0.15f, 0.55f, -15f, 25f }, { 0.05f, -0.3f, 0.5f, -25f, 5f } };
        for (int i = 0; i < 4; i++)
            p.Add(CrystalPart(new Vector3(c[i, 0], 0.12f, c[i, 1]), 0.09f + c[i, 2] * 0.07f, c[i, 2], new Vector3(c[i, 3], 0f, c[i, 4]), G(Mat.Sulfur)));
        return p;
    }

    static List<Part> GlowstoneParts()
    {
        var p = new List<Part>();
        p.Add(new Part(Lumpy(130, 0.35f), TRS(new Vector3(0f, 0.4f, 0f), Vector3.zero, new Vector3(1.1f, 0.9f, 1f)), G(Mat.Glowstone)));
        p.Add(new Part(Lumpy(131, 0.35f), TRS(new Vector3(0.55f, 0.2f, 0.3f), Vector3.zero, new Vector3(0.6f, 0.5f, 0.6f)), G(Mat.Glowstone)));
        p.Add(new Part(Lumpy(132, 0.35f), TRS(new Vector3(-0.5f, 0.15f, -0.3f), Vector3.zero, new Vector3(0.5f, 0.4f, 0.45f)), G(Mat.Glowstone)));
        p.Add(new Part(Lumpy(133, 0.3f), TRS(new Vector3(0f, 0.08f, 0f), Vector3.zero, new Vector3(1.7f, 0.3f, 1.5f)), rockDark));
        return p;
    }

    static List<Part> UraniumParts()
    {
        var p = new List<Part>();
        p.Add(new Part(Lumpy(140, 0.3f), TRS(new Vector3(0f, 0.15f, 0f), Vector3.zero, new Vector3(1.5f, 0.5f, 1.3f)), rockDark));
        for (int i = 0; i < 5; i++)
        {
            float a = i * 72f;
            Vector3 b = Quaternion.Euler(0f, a, 0f) * new Vector3(0.35f, 0.2f, 0f);
            p.Add(Cy(b, 0.1f, 0.1f, 0.7f + (i % 2) * 0.3f, G(Mat.Uranium), 6, new Vector3(Mathf.Sin(a) * 15f, 0f, Mathf.Cos(a) * 15f)));
        }
        p.Add(Cy(new Vector3(0f, 0.2f, 0f), 0.13f, 0.13f, 1.1f, G(Mat.Uranium), 6));
        p.Add(new Part(Tube(16, 0.95f, 0.85f, 0.06f), TRS(new Vector3(0f, 0.02f, 0f), Vector3.zero, Vector3.one), hazard));    // "keep out" ring
        return p;
    }

    static List<Part> ObsidianParts()
    {
        var p = new List<Part>();
        var r = new System.Random(150);
        p.Add(new Part(Lumpy(150, 0.3f), TRS(new Vector3(0f, 0.1f, 0f), Vector3.zero, new Vector3(1.3f, 0.35f, 1.2f)), rockDark));
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f + r.Next(30);
            Vector3 pos = Quaternion.Euler(0f, a, 0f) * new Vector3(i == 0 ? 0f : 0.35f, 0.1f, 0f);
            float h = i == 0 ? 1.3f : 0.6f + (float)r.NextDouble() * 0.5f;
            p.Add(CrystalPart(pos, 0.14f, h, new Vector3((float)r.NextDouble() * 40f - 20f, a, (float)r.NextDouble() * 40f - 20f), obsidian));
        }
        p.Add(Ball(new Vector3(0f, 0.6f, 0f), 0.1f, G(Mat.Obsidian)));
        return p;
    }

    static List<Part> CircuitParts()
    {
        var p = new List<Part>();
        // a smashed computer with boards spilling out
        p.Add(B(new Vector3(0f, 0.4f, 0f), new Vector3(0.45f, 0.8f, 0.9f), metalDark, new Vector3(0f, 0f, -12f)));
        p.Add(B(new Vector3(0.24f, 0.45f, 0f), new Vector3(0.02f, 0.6f, 0.7f), pcb, new Vector3(0f, 0f, -12f)));
        for (int i = 0; i < 4; i++)
        {
            Vector3 c = new Vector3(0.5f + i * 0.2f, 0.03f + i * 0.02f, -0.3f + (i % 2) * 0.5f);
            Vector3 e = new Vector3(0f, i * 35f, 0f);
            p.Add(B(c, new Vector3(0.5f, 0.02f, 0.35f), pcb, e));
            p.Add(B(c + Vector3.up * 0.025f, new Vector3(0.12f, 0.03f, 0.12f), metalDark, e));
            p.Add(B(c + new Vector3(0.12f, 0.025f, 0.08f), new Vector3(0.06f, 0.02f, 0.06f), G(Mat.Circuit), e));
        }
        return p;
    }

    static List<Part> WireParts()
    {
        var p = new List<Part>();
        // a big cable drum on its side
        foreach (float z in new[] { -0.4f, 0.4f }) p.Add(Cy(new Vector3(0f, 0.6f, z), 0.6f, 0.6f, 0.06f, wood, 14, new Vector3(90f, 0f, 0f)));
        p.Add(Cy(new Vector3(0f, 0.6f, -0.37f), 0.45f, 0.45f, 0.74f, G(Mat.CopperWire), 14, new Vector3(90f, 0f, 0f)));
        // loose wire on the ground
        for (int i = 0; i < 5; i++)
            p.Add(Cy(new Vector3(0.3f + i * 0.25f, 0.04f, 0.1f + Mathf.Sin(i) * 0.3f), 0.03f, 0.03f, 0.35f, copper, 5, new Vector3(0f, i * 40f, 90f)));
        return p;
    }

    static List<Part> BatteryParts()
    {
        var p = new List<Part>();
        Vector3[] at = { new Vector3(0f, 0f, 0f), new Vector3(0.32f, 0f, 0.05f), new Vector3(0.15f, 0f, 0.3f), new Vector3(-0.2f, 0f, 0.28f), new Vector3(0.12f, 0.6f, 0.15f) };
        for (int i = 0; i < at.Length; i++)
        {
            Vector3 e = i == 4 ? new Vector3(0f, 0f, 90f) : Vector3.zero;
            Vector3 b = i == 4 ? at[i] + new Vector3(-0.3f, 0f, 0f) : at[i];
            p.Add(Cy(b, 0.14f, 0.14f, 0.55f, i == 2 ? G(Mat.Battery) : plasticB, 10, e));
            Vector3 top = b + Quaternion.Euler(e) * Vector3.up * 0.55f;
            p.Add(Cy(top, 0.06f, 0.06f, 0.06f, metal, 8, e));
        }
        p.Add(B(new Vector3(-0.5f, 0.15f, -0.3f), new Vector3(0.6f, 0.3f, 0.4f), plasticB));          // a car battery
        p.Add(B(new Vector3(-0.5f, 0.32f, -0.3f), new Vector3(0.5f, 0.05f, 0.3f), G(Mat.Battery)));
        return p;
    }

    static List<Part> GlassParts()
    {
        var p = new List<Part>();
        var r = new System.Random(170);
        for (int i = 0; i < 6; i++)
        {
            Vector3 c = new Vector3(((float)r.NextDouble() - 0.5f) * 1.2f, 0.05f + (float)r.NextDouble() * 0.3f, ((float)r.NextDouble() - 0.5f) * 1.2f);
            p.Add(B(c, new Vector3(0.5f + (float)r.NextDouble() * 0.3f, 0.02f, 0.35f), G(Mat.Glass), new Vector3(r.Next(70) - 35, r.Next(360), r.Next(70) - 35)));
        }
        // bottles
        for (int i = 0; i < 3; i++)
        {
            Vector3 b = new Vector3(0.6f - i * 0.25f, 0f, -0.5f + i * 0.1f);
            p.Add(Cy(b, 0.09f, 0.09f, 0.3f, windowGlass, 8));
            p.Add(Cy(b + Vector3.up * 0.3f, 0.09f, 0.03f, 0.15f, windowGlass, 8));
        }
        return p;
    }

    static List<Part> PlasticParts()
    {
        var p = new List<Part>();
        // a pile of plastic crates and bottles
        p.Add(B(new Vector3(0f, 0.25f, 0f), new Vector3(0.7f, 0.5f, 0.5f), plasticR));
        p.Add(B(new Vector3(0f, 0.51f, 0f), new Vector3(0.6f, 0.02f, 0.4f), G(Mat.Plastic)));
        p.Add(B(new Vector3(0.55f, 0.2f, 0.4f), new Vector3(0.6f, 0.4f, 0.45f), plasticB, new Vector3(0f, 25f, 0f)));
        p.Add(B(new Vector3(0.2f, 0.75f, 0.1f), new Vector3(0.6f, 0.45f, 0.45f), plasticW, new Vector3(0f, -15f, 8f)));
        for (int i = 0; i < 4; i++)
        {
            Vector3 b = new Vector3(-0.6f + i * 0.08f, 0.08f, 0.5f - i * 0.25f);
            p.Add(Cy(b, 0.08f, 0.08f, 0.35f, i % 2 == 0 ? plasticW : plasticB, 8, new Vector3(0f, i * 50f, 90f)));
        }
        return p;
    }

    static List<Part> MeteoriteParts(int seed, float s)
    {
        var r = new System.Random(seed);
        var p = new List<Part>();
        p.Add(new Part(Lumpy(seed, 0.35f), TRS(new Vector3(0f, 0.4f * s, 0f), new Vector3(0f, seed * 7f, 0f), new Vector3(1.3f, 1f, 1.2f) * s), meteorRock));
        for (int i = 0; i < 8; i++)
        {
            Vector3 d = new Vector3((float)r.NextDouble() - 0.5f, (float)r.NextDouble() * 0.7f + 0.2f, (float)r.NextDouble() - 0.5f).normalized;
            Vector3 pos = new Vector3(0f, 0.4f * s, 0f) + Vector3.Scale(d, new Vector3(0.63f, 0.48f, 0.58f)) * s;
            p.Add(Ball(pos, new Vector3(0.22f, 0.06f, 0.12f) * s, G(Mat.Meteorite)));
        }
        return p;
    }

    static List<Part> StardustParts()
    {
        var p = new List<Part>();
        p.Add(new Part(Dome(14, 0.8f), TRS(Vector3.zero, Vector3.zero, new Vector3(1f, 0.45f, 1f)), G(Mat.Stardust)));
        var r = new System.Random(180);
        for (int i = 0; i < 10; i++)
        {
            float a = (float)r.NextDouble() * Mathf.PI * 2f, d = (float)r.NextDouble() * 0.9f;
            p.Add(CrystalPart(new Vector3(Mathf.Cos(a) * d, 0.05f, Mathf.Sin(a) * d), 0.04f, 0.15f + (float)r.NextDouble() * 0.2f, new Vector3(r.Next(40) - 20, 0f, r.Next(40) - 20), chalk, true));
        }
        return p;
    }

    // ================================================================
    // Landmarks
    // ================================================================

    // Where the material spots go inside each landmark (in the landmark's own space; +z faces the middle of the map).
    static readonly Vector3[] CaveSpots =
    {
        new Vector3(-2.4f, 0.25f, -3.5f), new Vector3(2.4f, 0.25f, -6.5f), new Vector3(-2.4f, 0.25f, -9.5f),
        new Vector3(2.4f, 0.25f, -12.5f), new Vector3(0f, 0.25f, -14.6f),
    };
    const float OfficeFloor2 = 4.6f;
    static readonly Vector3[] OfficeSpots =
    {
        // ground floor
        new Vector3(-9.5f, 0.3f, 5.8f), new Vector3(-2.5f, 0.3f, -7.2f), new Vector3(5f, 0.3f, 1.2f), new Vector3(-8.5f, 0.3f, -6.6f), new Vector3(0.6f, 0.3f, -2.5f),
        // upstairs
        new Vector3(-10.5f, OfficeFloor2, 6f), new Vector3(-1f, OfficeFloor2, 6.6f), new Vector3(6.5f, OfficeFloor2, -5.8f), new Vector3(-11f, OfficeFloor2, -7f), new Vector3(3f, OfficeFloor2, 2f),
    };

    static void MakeLandmarks()
    {
        SavePrefab("Office_Building", OfficeParts(), Solid.Shape, 0f, 0f, go =>
        {
            foreach (Vector3 l in new[] { new Vector3(-6f, 3.6f, 0f), new Vector3(5f, 3.6f, -3f), new Vector3(-6f, 7.9f, 2f), new Vector3(6f, 7.9f, -2f) })
            {
                Light li = AddLight(go.transform, "Light", l, new Color(0.85f, 0.95f, 1f), 11f, 1.1f);
                li.shadows = LightShadows.None;
            }
        });
        SavePrefab("Cave", CaveParts(), Solid.Shape, 0f, 0f, go =>
        {
            AddLight(go.transform, "Glow 1", new Vector3(0f, 3f, -3.5f), new Color(1f, 0.75f, 0.45f), 9f, 1.4f);
            AddLight(go.transform, "Glow 2", new Vector3(0f, 3f, -9f), new Color(0.45f, 0.95f, 1f), 9f, 1.6f);
            AddLight(go.transform, "Glow 3", new Vector3(0f, 3f, -14f), new Color(0.75f, 0.5f, 1f), 9f, 1.6f);
        });
        SavePrefab("Meteor", MeteorParts(), Solid.Mesh, 0f, 0f, go =>
            AddLight(go.transform, "Glow", new Vector3(0f, 3.5f, 0f), new Color(1f, 0.5f, 0.2f), 20f, 2.6f));
        SavePrefab("Meteor_Shard", MeteoriteParts(310, 1.4f), Solid.Mesh);
    }

    // ---------------- the office building ----------------
    //   26 x 18 m, two floors (ground floor 0.3, upstairs 4.6), front door at +z, stairs on the right side,
    //   cubicles downstairs, a server room upstairs and a hole in the roof where it caved in.

    struct Hole { public float c, w, b, t; public Hole(float c, float w, float b, float t) { this.c = c; this.w = w; this.b = b; this.t = t; } }

    // A wall from 'from' to 'to' along x (alongX) or z, at 'at' on the other axis, between heights y0 and y1, with holes cut in.
    static void Wall(List<Part> p, bool alongX, float at, float from, float to, float y0, float y1, float thick, Material m, params Hole[] holes)
    {
        var hs = new List<Hole>(holes);
        hs.Sort((a, b) => a.c.CompareTo(b.c));
        System.Action<float, float, float, float> piece = (a, b, lo, hi) =>
        {
            if (b - a < 0.01f || hi - lo < 0.01f) return;
            float mid = (a + b) / 2f, len = b - a, ym = (lo + hi) / 2f, h = hi - lo;
            if (alongX) p.Add(B(new Vector3(mid, ym, at), new Vector3(len, h, thick), m));
            else p.Add(B(new Vector3(at, ym, mid), new Vector3(thick, h, len), m));
        };
        float cur = from;
        foreach (Hole h in hs)
        {
            float a = h.c - h.w / 2f, b = h.c + h.w / 2f;
            piece(cur, a, y0, y1);                 // solid bit before the hole
            piece(a, b, y0, h.b);                  // under it
            piece(a, b, h.t, y1);                  // over it
            cur = b;
        }
        piece(cur, to, y0, y1);
    }

    static void Desk(List<Part> p, Vector3 at, float yaw, bool computer)
    {
        Matrix4x4 m = TRS(at, new Vector3(0f, yaw, 0f), Vector3.one);
        System.Action<Vector3, Vector3, Material> add = (c, size, mat) => p.Add(new Part(Box(size), m * TRS(c, Vector3.zero, Vector3.one), mat));
        add(new Vector3(0f, 0.75f, 0f), new Vector3(1.6f, 0.05f, 0.8f), woodDark);
        foreach (float x in new[] { -0.75f, 0.75f }) add(new Vector3(x, 0.37f, 0f), new Vector3(0.05f, 0.74f, 0.75f), metalDark);
        if (computer)
        {
            add(new Vector3(0f, 1.0f, 0.22f), new Vector3(0.6f, 0.4f, 0.04f), metalDark);
            add(new Vector3(0f, 1.0f, 0.195f), new Vector3(0.54f, 0.34f, 0.01f), screen);
            add(new Vector3(0f, 0.8f, 0.25f), new Vector3(0.2f, 0.06f, 0.15f), metalDark);
            add(new Vector3(0f, 0.785f, -0.1f), new Vector3(0.45f, 0.02f, 0.15f), plasticW);      // keyboard
            add(new Vector3(0.6f, 0.23f, 0.1f), new Vector3(0.2f, 0.45f, 0.45f), metalDark);       // tower
        }
        // chair
        add(new Vector3(0f, 0.48f, -0.7f), new Vector3(0.5f, 0.08f, 0.5f), fabric);
        add(new Vector3(0f, 0.8f, -0.95f), new Vector3(0.5f, 0.6f, 0.06f), fabric);
        add(new Vector3(0f, 0.22f, -0.7f), new Vector3(0.06f, 0.44f, 0.06f), metalDark);
    }

    static List<Part> OfficeParts()
    {
        var p = new List<Part>();
        const float HX = 13f, HZ = 9f, T = 0.3f, F1 = 0.3f, F2 = 4.6f, Roof = 8.9f;
        float ix = HX - T / 2f, iz = HZ - T / 2f;        // wall centre lines

        // floors
        p.Add(B(new Vector3(0f, 0.15f - 0.4f, 0f), new Vector3(HX * 2f + 1f, 1.1f, HZ * 2f + 1f), concrete));   // foundation (goes into the ground)
        p.Add(B(new Vector3(0f, F1 - 0.01f, 0f), new Vector3(HX * 2f - 0.6f, 0.02f, HZ * 2f - 0.6f), carpet));
        // upstairs floor, with a hole for the stairs (x 8.4..13, z -5.2..0.6)
        p.Add(B(new Vector3((-HX + 8.4f) / 2f, F2 - 0.15f, 0f), new Vector3(8.4f + HX, 0.3f, HZ * 2f), concrete));
        p.Add(B(new Vector3((8.4f + HX) / 2f, F2 - 0.15f, (0.6f + HZ) / 2f), new Vector3(HX - 8.4f, 0.3f, HZ - 0.6f), concrete));
        p.Add(B(new Vector3((8.4f + HX) / 2f, F2 - 0.15f, (-HZ - 5.2f) / 2f), new Vector3(HX - 8.4f, 0.3f, HZ - 5.2f), concrete));
        p.Add(B(new Vector3((-HX + 8.4f) / 2f, F2 + 0.005f, 0f), new Vector3(8.4f + HX - 0.6f, 0.01f, HZ * 2f - 0.6f), carpet));
        // railing round the stair hole
        p.Add(B(new Vector3(8.4f, F2 + 0.5f, -2.3f), new Vector3(0.08f, 1f, 5.8f), metal));
        p.Add(B(new Vector3(10.7f, F2 + 0.5f, 0.6f), new Vector3(4.6f, 1f, 0.08f), metal));
        // roof, caved in at x -8..-3, z -4..2
        p.Add(B(new Vector3((-HX - 8f) / 2f, Roof - 0.15f, 0f), new Vector3(HX - 8f, 0.3f, HZ * 2f), concrete));
        p.Add(B(new Vector3((HX - 3f) / 2f, Roof - 0.15f, 0f), new Vector3(HX + 3f, 0.3f, HZ * 2f), concrete));
        p.Add(B(new Vector3(-5.5f, Roof - 0.15f, (2f + HZ) / 2f), new Vector3(5f, 0.3f, HZ - 2f), concrete));
        p.Add(B(new Vector3(-5.5f, Roof - 0.15f, (-HZ - 4f) / 2f), new Vector3(5f, 0.3f, HZ - 4f), concrete));
        // roof edge, aircon units
        Wall(p, true, iz, -HX, HX, Roof, Roof + 0.7f, T, officeWall);
        Wall(p, true, -iz, -HX, HX, Roof, Roof + 0.7f, T, officeWall);
        Wall(p, false, ix, -HZ, HZ, Roof, Roof + 0.7f, T, officeWall);
        Wall(p, false, -ix, -HZ, HZ, Roof, Roof + 0.7f, T, officeWall);
        p.Add(B(new Vector3(6f, Roof + 0.6f, -4f), new Vector3(2.4f, 1.2f, 1.6f), metal));
        p.Add(B(new Vector3(6f, Roof + 0.6f, -3.18f), new Vector3(1.4f, 0.9f, 0.02f), grate));

        // walls: windows all round, a door at the front, a stripe between the floors
        float wb1 = F1 + 1f, wt1 = F1 + 2.8f, wb2 = F2 + 1f, wt2 = F2 + 2.8f;
        float[] longWin = { -10.5f, -6.5f, 6.5f, 10.5f };
        var front1 = new List<Hole> { new Hole(0f, 3.2f, F1, F1 + 3f) };
        foreach (float c in longWin) front1.Add(new Hole(c, 2.6f, wb1, wt1));
        var front2 = new List<Hole> { new Hole(0f, 2.6f, wb2, wt2) };
        foreach (float c in longWin) front2.Add(new Hole(c, 2.6f, wb2, wt2));
        var back = new List<Hole> { new Hole(0f, 2.6f, wb1, wt1) };
        foreach (float c in longWin) back.Add(new Hole(c, 2.6f, wb1, wt1));
        var back2 = new List<Hole>();
        foreach (Hole h in back) back2.Add(new Hole(h.c, h.w, wb2, wt2));
        float[] sideWin = { -5f, 0f, 5f };
        var side1 = new List<Hole>(); var side2 = new List<Hole>();
        foreach (float c in sideWin) { side1.Add(new Hole(c, 2.4f, wb1, wt1)); side2.Add(new Hole(c, 2.4f, wb2, wt2)); }

        Wall(p, true, iz, -HX, HX, 0f, F2, T, officeWall, front1.ToArray());
        Wall(p, true, iz, -HX, HX, F2, Roof, T, officeWall, front2.ToArray());
        Wall(p, true, -iz, -HX, HX, 0f, F2, T, officeWall, back.ToArray());
        Wall(p, true, -iz, -HX, HX, F2, Roof, T, officeWall, back2.ToArray());
        Wall(p, false, ix, -HZ, HZ, 0f, F2, T, officeWall, side1.ToArray());
        Wall(p, false, ix, -HZ, HZ, F2, Roof, T, officeWall, side2.ToArray());
        Wall(p, false, -ix, -HZ, HZ, 0f, F2, T, officeWall, side1.ToArray());
        Wall(p, false, -ix, -HZ, HZ, F2, Roof, T, officeWall, side2.ToArray());
        // stripes and window sills on the outside
        p.Add(B(new Vector3(0f, F2 - 0.1f, HZ + 0.02f), new Vector3(HX * 2f + 0.1f, 0.35f, 0.08f), metalDark));
        p.Add(B(new Vector3(0f, F2 - 0.1f, -HZ - 0.02f), new Vector3(HX * 2f + 0.1f, 0.35f, 0.08f), metalDark));
        // a few windows still have glass (the rest are smashed)
        p.Add(B(new Vector3(-10.5f, (wb2 + wt2) / 2f, iz), new Vector3(2.6f, wt2 - wb2, 0.05f), windowGlass));
        p.Add(B(new Vector3(6.5f, (wb1 + wt1) / 2f, -iz), new Vector3(2.6f, wt1 - wb1, 0.05f), windowGlass));
        p.Add(B(new Vector3(-ix, (wb2 + wt2) / 2f, 5f), new Vector3(0.05f, wt2 - wb2, 2.4f), windowGlass));
        // sign over the door and a little porch roof
        p.Add(B(new Vector3(0f, F1 + 3.4f, HZ + 0.12f), new Vector3(5f, 0.6f, 0.12f), board));
        p.Add(B(new Vector3(0f, F1 + 3.4f, HZ + 0.19f), new Vector3(4.6f, 0.14f, 0.02f), screen));
        p.Add(B(new Vector3(0f, F1 + 3.15f, HZ + 1.1f), new Vector3(4.4f, 0.12f, 2f), metalDark));
        foreach (float x in new[] { -2f, 2f }) p.Add(B(new Vector3(x, (F1 + 3.1f) / 2f, HZ + 1.9f), new Vector3(0.12f, F1 + 3.1f, 0.12f), metal));

        // stairs: 13 steps up the right side, from the front (z 6.5) towards the back
        const int steps = 13;
        float rise = (F2 - F1) / steps, run = 0.9f;
        for (int i = 0; i < steps; i++)
        {
            float z1 = 6.5f - i * run, z0 = z1 - run, top = F1 + (i + 1) * rise;
            p.Add(B(new Vector3(10.6f, (F1 + top) / 2f, (z0 + z1) / 2f), new Vector3(4.2f, top - F1, run), i % 2 == 0 ? concrete : stone));
        }
        p.Add(B(new Vector3(8.45f, F1 + 0.5f + 2f, 0.6f), new Vector3(0.08f, 4.6f, 11.8f), metal));    // banister wall

        // ---- downstairs: cubicles, reception, filing cabinets, water cooler ----
        foreach (float x in new[] { -10.5f, -7f })
            foreach (float z in new[] { -4f, 0f })
                Desk(p, new Vector3(x, F1, z), 180f, true);
        p.Add(B(new Vector3(-8.75f, F1 + 0.65f, -2f), new Vector3(5.2f, 1.3f, 0.08f), fabric));       // cubicle walls
        p.Add(B(new Vector3(-8.75f, F1 + 0.65f, 1.0f), new Vector3(5.2f, 1.3f, 0.08f), fabric));
        p.Add(B(new Vector3(-8.75f, F1 + 0.65f, -5.0f), new Vector3(5.2f, 1.3f, 0.08f), fabric));
        foreach (float x in new[] { 3.5f, 6.5f })
            foreach (float z in new[] { -6f, -2.5f })
                Desk(p, new Vector3(x, F1, z), 0f, x < 5f);
        p.Add(B(new Vector3(5f, F1 + 0.65f, -4.25f), new Vector3(4.6f, 1.3f, 0.08f), fabric));
        // reception desk facing the door
        p.Add(B(new Vector3(-4f, F1 + 0.55f, 5.2f), new Vector3(3.4f, 1.1f, 0.9f), woodDark));
        p.Add(B(new Vector3(-4f, F1 + 1.12f, 5.2f), new Vector3(3.6f, 0.05f, 1.1f), plasticW));
        p.Add(B(new Vector3(-4.6f, F1 + 1.35f, 4.95f), new Vector3(0.5f, 0.35f, 0.04f), screen));
        // filing cabinets on the back wall
        for (int i = 0; i < 4; i++) p.Add(B(new Vector3(-12.2f + i * 0.6f, F1 + 0.65f, -8.35f), new Vector3(0.55f, 1.3f, 0.6f), i == 2 ? rust : metal));
        for (int i = 0; i < 3; i++) p.Add(B(new Vector3(1.2f + i * 0.6f, F1 + 0.65f, -8.35f), new Vector3(0.55f, 1.3f, 0.6f), metal));
        p.Add(B(new Vector3(-5f, F1 + 0.3f, -7.9f), new Vector3(0.6f, 0.6f, 1.3f), metal, new Vector3(0f, 0f, 90f)));   // one fell over
        // water cooler and a pot plant
        p.Add(Cy(new Vector3(2.2f, F1, 7.8f), 0.22f, 0.22f, 1f, plasticW, 10));
        p.Add(Ball(new Vector3(2.2f, F1 + 1.25f, 7.8f), new Vector3(0.38f, 0.5f, 0.38f), water));
        p.Add(Cy(new Vector3(-12f, F1, 8f), 0.3f, 0.22f, 0.5f, rust, 10));
        p.Add(new Part(Lumpy(5, 0.2f), TRS(new Vector3(-12f, F1 + 0.95f, 8f), Vector3.zero, new Vector3(0.9f, 1f, 0.9f)), leaves));

        // ---- upstairs: open office, server room, rubble under the roof hole ----
        Desk(p, new Vector3(-6f, F2, 6f), 180f, true);
        Desk(p, new Vector3(-3f, F2, 6f), 180f, false);
        Desk(p, new Vector3(5f, F2, 5.5f), 180f, true);
        Desk(p, new Vector3(-10f, F2, -1f), 90f, true);
        // server racks with blinking lights
        for (int i = 0; i < 4; i++)
        {
            float x = 0.8f + i * 1.05f;
            p.Add(B(new Vector3(x, F2 + 1f, -8.2f), new Vector3(0.9f, 2f, 0.9f), metalDark));
            for (int k = 0; k < 5; k++) p.Add(B(new Vector3(x - 0.25f + (k % 2) * 0.2f, F2 + 0.4f + k * 0.32f, -7.74f), new Vector3(0.08f, 0.05f, 0.01f), k % 3 == 0 ? redGlow : glowGreen));
        }
        // a glass wall in front of the server room
        p.Add(B(new Vector3(2.4f, F2 + 1.4f, -6.9f), new Vector3(4.6f, 2.8f, 0.06f), windowGlass));
        // rubble and fallen ceiling under the hole
        var r = new System.Random(66);
        for (int i = 0; i < 8; i++)
        {
            Vector3 c = new Vector3(-5.5f + ((float)r.NextDouble() - 0.5f) * 4f, F2 + 0.2f, -1f + ((float)r.NextDouble() - 0.5f) * 5f);
            p.Add(new Part(Lumpy(200 + i, 0.3f), TRS(c, new Vector3(0f, r.Next(360), 0f), new Vector3(1.2f, 0.6f, 1f) * (0.6f + (float)r.NextDouble() * 0.7f)), concrete));
        }
        p.Add(B(new Vector3(-5.2f, F2 + 1.4f, -1f), new Vector3(4.8f, 0.25f, 3.2f), concrete, new Vector3(28f, 10f, 15f)));   // a big slab, leaning
        return p;
    }

    // ---------------- a cave ----------------
    //   The mouth is at (0,0,0) facing +z; inside goes back to z = -16. About 9 m wide and 4.5 m high inside.

    static List<Part> CaveParts()
    {
        var p = new List<Part>();
        int seed = 400;
        System.Action<Vector3, Vector3, Material> lump = (c, size, m) => p.Add(new Part(Lumpy(seed++, 0.16f), TRS(c, new Vector3(0f, seed * 23f % 360f, 0f), size), m));
        // floor
        p.Add(B(new Vector3(0f, 0.12f, -8f), new Vector3(10f, 0.25f, 18f), rockDark));
        // side walls
        foreach (float z in new[] { -1.5f, -5f, -8.5f, -12f, -15.5f })
            foreach (float sx in new[] { -1f, 1f })
                lump(new Vector3(sx * 8.7f, 2.5f, z), new Vector3(7.5f, 8f, 6.5f), seed % 2 == 0 ? rock : rockDark);
        // back wall
        foreach (float x in new[] { -5f, 0f, 5f }) lump(new Vector3(x, 2.5f, -20.2f), new Vector3(7f, 8f, 7.5f), rock);
        // roof
        foreach (float z in new[] { -2f, -6f, -10f, -14f, -17.5f }) lump(new Vector3(0f, 9.8f, z), new Vector3(20f, 9f, 6f), rockDark);
        // the hill on top
        lump(new Vector3(0f, 12.5f, -9f), new Vector3(24f, 10f, 26f), rock);
        lump(new Vector3(-6f, 10f, -15f), new Vector3(12f, 9f, 12f), rock);
        lump(new Vector3(7f, 9.5f, -5f), new Vector3(11f, 8f, 12f), rock);
        // the mouth
        foreach (float sx in new[] { -1f, 1f }) lump(new Vector3(sx * 6.8f, 2.8f, 1.6f), new Vector3(5f, 7f, 4f), rock);
        lump(new Vector3(0f, 8.2f, 1.6f), new Vector3(14f, 5f, 4f), rock);
        // stalactites hanging from the roof
        var r = new System.Random(410);
        for (int i = 0; i < 14; i++)
        {
            Vector3 at = new Vector3(((float)r.NextDouble() - 0.5f) * 7f, 5.4f, -1.5f - (float)r.NextDouble() * 14f);
            p.Add(new Part(Cone(6, 0.25f + (float)r.NextDouble() * 0.2f, 0.9f + (float)r.NextDouble() * 0.9f, 420 + i), TRS(at, new Vector3(180f, 0f, 0f), Vector3.one), rockDark));
        }
        // glowing mushrooms and crystals along the walls
        for (int i = 0; i < 10; i++)
        {
            float sx = i % 2 == 0 ? -1f : 1f, z = -2f - i * 1.4f;
            Vector3 b = new Vector3(sx * (3.6f + (float)r.NextDouble() * 0.4f), 0.25f, z);
            if (i % 3 == 0) p.Add(CrystalPart(b, 0.12f, 0.7f + (float)r.NextDouble() * 0.6f, new Vector3(0f, 0f, -sx * 20f), i % 2 == 0 ? glowPurple : glowCyan));
            else
            {
                p.Add(Cy(b, 0.05f, 0.04f, 0.35f, stem, 6, default, false));
                p.Add(new Part(Dome(8, 0.2f), TRS(b + Vector3.up * 0.35f, Vector3.zero, new Vector3(1f, 0.6f, 1f)), glowCyan));
            }
        }
        // old mining stuff: a cart track and a lantern
        p.Add(B(new Vector3(-0.6f, 0.27f, -7f), new Vector3(0.08f, 0.06f, 13f), iron));
        p.Add(B(new Vector3(0.6f, 0.27f, -7f), new Vector3(0.08f, 0.06f, 13f), iron));
        for (int i = 0; i < 12; i++) p.Add(B(new Vector3(0f, 0.25f, -1.2f - i * 1.1f), new Vector3(1.6f, 0.06f, 0.2f), woodDark));
        p.Add(B(new Vector3(0f, 0.75f, -10.5f), new Vector3(1.1f, 0.7f, 1.6f), rust));
        foreach (float x in new[] { -0.6f, 0.6f })
            foreach (float z in new[] { -10f, -11f })
                p.Add(Cy(new Vector3(x, 0.4f, z), 0.18f, 0.18f, 0.08f, iron, 8, new Vector3(0f, 0f, 90f)));
        return p;
    }

    // ---------------- the meteor ----------------

    static List<Part> MeteorParts()
    {
        var p = MeteoriteParts(300, 4.5f);
        var r = new System.Random(302);
        // smoking hot cracks all over
        for (int i = 0; i < 14; i++)
        {
            Vector3 d = new Vector3((float)r.NextDouble() - 0.5f, (float)r.NextDouble() * 0.8f + 0.1f, (float)r.NextDouble() - 0.5f).normalized;
            Vector3 pos = new Vector3(0f, 1.8f, 0f) + Vector3.Scale(d, new Vector3(2.85f, 2.1f, 2.6f));
            p.Add(B(pos, new Vector3(0.9f, 0.08f, 0.15f), G(Mat.Meteorite), new Vector3(r.Next(360), r.Next(360), r.Next(360))));
        }
        // chunks it threw up when it hit
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f + r.Next(20);
            Vector3 c = Quaternion.Euler(0f, a, 0f) * new Vector3(3.6f, 0.2f, 0f);
            p.Add(new Part(Lumpy(320 + i, 0.3f), TRS(c, new Vector3(0f, a, 0f), new Vector3(1.4f, 0.8f, 1.1f)), meteorRock));
        }
        return p;
    }
}
