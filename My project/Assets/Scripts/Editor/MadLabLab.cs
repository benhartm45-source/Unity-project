using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// THE LABORATORY prefab (and the reactor in the middle of the map).
//
// Lab layout (looking down, the door is at the front = +Z = the blue arrow):
//
//      back wall:   [Brewing Stand]   [Lab Terminal]   [Workbench]
//      left wall:   [Enchanting Table]                  [Storage]   :right wall
//                   [Tesla coil]        (you spawn here)  [lab table]
//      front:                      ===  DOOR  ===
//
// 14 x 12 m, 4.5 m high walls, on a concrete pad. All the parts have names the game looks for
// (see Laboratory.cs) - you can move them around or restyle them, just keep the names.
public static partial class MadLabKitMenu
{
    const float FY = 0.5f;                  // floor height (top of the concrete pad)
    const float WallH = 4.5f;               // walls go from FY to FY + WallH
    const float HX = 6.8f, HZ = 5.8f;       // wall centre lines
    const float WT = 0.4f;                  // wall thickness
    const float DoorW = 3.2f, DoorH = 3.2f;

    static void MakeLaboratory()
    {
        var root = new GameObject("Laboratory");
        var lab = root.AddComponent<Laboratory>();
        lab.labNumber = 1;
        Transform r = root.transform;

        // ---------------- concrete pad + floor (stays when the lab is destroyed) ----------------
        var pad = new List<Part>();
        pad.Add(B(new Vector3(0f, FY / 2f, 0f), new Vector3(15.4f, FY, 13.4f), concrete));
        pad.Add(B(new Vector3(0f, FY / 2f - 0.05f, 7.25f), new Vector3(4.6f, FY - 0.1f, 1.2f), concrete));             // front step
        pad.Add(B(new Vector3(0f, FY / 4f - 0.02f, 8.05f), new Vector3(4.6f, FY / 2f - 0.04f, 0.5f), concrete));
        pad.Add(B(new Vector3(0f, FY + 0.012f, 0f), new Vector3(13.2f, 0.025f, 11.2f), labFloor));                  // tiles
        pad.Add(B(new Vector3(0f, FY + 0.012f, 6.3f), new Vector3(15.4f, 0.025f, 0.5f), hazard));                   // stripe out front
        pad.Add(B(new Vector3(0f, FY + 0.028f, 0.6f), new Vector3(1.4f, 0.01f, 1.4f), grate));                      // drain
        foreach (float s in new[] { -1f, 1f })
        {
            pad.Add(B(new Vector3(s * 7.71f, FY * 0.5f, 0f), new Vector3(0.02f, 0.08f, 12.6f), accent));              // glow strips on the pad sides
            pad.Add(B(new Vector3(0f, FY * 0.5f, s * 6.71f), new Vector3(14.6f, 0.08f, 0.02f), accent));
        }
        MeshChild(r, "Lab_Pad", "Foundation", pad, Solid.Shape, Vector3.zero);

        // ---------------- the building (falls apart when destroyed) ----------------
        Transform st = Empty(r, "Structure", Vector3.zero).transform;
        float wallY = FY + WallH / 2f;

        // back wall, two halves (big vent grilles and pipes on the outside)
        for (int s = -1; s <= 1; s += 2)
        {
            var p = WallPiece(new Vector3(s * 3.5f, wallY, -HZ), new Vector3(7f, WallH, WT), Vector3.back);
            p.Add(B(new Vector3(s * 3.5f, FY + 3.2f, -HZ - WT / 2f - 0.03f), new Vector3(1.8f, 1.1f, 0.06f), grate));
            p.Add(B(new Vector3(s * 3.5f, FY + 3.2f, -HZ - WT / 2f - 0.02f), new Vector3(2f, 1.3f, 0.04f), metalDark));
            p.Add(Cy(new Vector3(s * 5.6f, FY, -HZ - 0.5f), 0.14f, 0.14f, WallH + 0.6f, metal, 10));               // drain pipe
            MeshChild(st, "Lab_Wall_Back_" + (s < 0 ? "L" : "R"), "Wall_Back_" + (s < 0 ? "L" : "R"), p, Solid.Box, Vector3.zero);
        }
        // side walls, two halves each, with windows
        for (int s = -1; s <= 1; s += 2)
            for (int h = -1; h <= 1; h += 2)
            {
                var p = WallPiece(new Vector3(s * HX, wallY, h * 2.8f), new Vector3(WT, WallH, 5.6f), new Vector3(s, 0f, 0f));
                Window(p, new Vector3(s * HX, FY + 2.6f, h * 2.8f), 2.4f, 1.3f, s);
                MeshChild(st, "Lab_Wall_" + (s < 0 ? "L" : "R") + (h < 0 ? "B" : "F"), "Wall_" + (s < 0 ? "Left_" : "Right_") + (h < 0 ? "Back" : "Front"), p, Solid.Box, Vector3.zero);
            }
        // front wall: left and right of the door, and the bit above it with the sign
        float side = (14f - DoorW) / 2f;
        for (int s = -1; s <= 1; s += 2)
        {
            float cx = s * (DoorW / 2f + side / 2f);
            var p = WallPiece(new Vector3(cx, wallY, HZ), new Vector3(side, WallH, WT), Vector3.forward);
            p.Add(B(new Vector3(s * (DoorW / 2f + 0.14f), FY + DoorH / 2f + 0.05f, HZ), new Vector3(0.28f, DoorH + 0.1f, WT + 0.12f), hazard));   // door frame
            Window(p, new Vector3(s * 4.6f, FY + 2.6f, HZ), 2f, 1.2f, 0, true);
            p.Add(B(new Vector3(s * 4.6f, FY + 0.9f, HZ + WT / 2f + 0.25f), new Vector3(2.2f, 0.12f, 0.5f), metalDark));                          // window box
            MeshChild(st, "Lab_Wall_Front_" + (s < 0 ? "L" : "R"), "Wall_Front_" + (s < 0 ? "L" : "R"), p, Solid.Box, Vector3.zero);
        }
        {
            float topH = WallH - DoorH;
            var p = WallPiece(new Vector3(0f, FY + DoorH + topH / 2f, HZ), new Vector3(DoorW, topH, WT), Vector3.forward);
            p.Add(B(new Vector3(0f, FY + DoorH + 0.12f, HZ), new Vector3(DoorW + 0.56f, 0.24f, WT + 0.12f), hazard));
            // sign: a glowing flask
            float sy = FY + DoorH + 0.75f, sz = HZ + WT / 2f + 0.08f;
            p.Add(B(new Vector3(0f, sy, sz), new Vector3(2.8f, 0.9f, 0.14f), metalDark));
            p.Add(B(new Vector3(0f, sy + 0.38f, sz + 0.08f), new Vector3(2.6f, 0.07f, 0.04f), accent));
            p.Add(B(new Vector3(0f, sy - 0.38f, sz + 0.08f), new Vector3(2.6f, 0.07f, 0.04f), accent));
            p.Add(Ball(new Vector3(0f, sy - 0.08f, sz + 0.12f), new Vector3(0.5f, 0.45f, 0.12f), accent));
            p.Add(B(new Vector3(0f, sy + 0.2f, sz + 0.12f), new Vector3(0.16f, 0.3f, 0.1f), accent));
            p.Add(B(new Vector3(-0.85f, sy, sz + 0.1f), new Vector3(0.6f, 0.08f, 0.05f), chalk));
            p.Add(B(new Vector3(0.85f, sy, sz + 0.1f), new Vector3(0.6f, 0.08f, 0.05f), chalk));
            MeshChild(st, "Lab_Wall_Front_Top", "Wall_Front_Top", p, Solid.Box, Vector3.zero);
        }
        // corner pillars
        for (int a = -1; a <= 1; a += 2)
            for (int b = -1; b <= 1; b += 2)
            {
                var p = new List<Part>();
                p.Add(B(new Vector3(a * 7.0f, FY + 2.55f, b * 6.0f), new Vector3(0.8f, 5.1f, 0.8f), metalDark));
                p.Add(B(new Vector3(a * 7.0f, FY + 2.3f, b * 6.0f + b * 0.41f), new Vector3(0.12f, 3.8f, 0.03f), accent));
                p.Add(B(new Vector3(a * 7.0f + a * 0.41f, FY + 2.3f, b * 6.0f), new Vector3(0.03f, 3.8f, 0.12f), accent));
                p.Add(B(new Vector3(a * 7.0f, FY + 0.3f, b * 6.0f), new Vector3(0.9f, 0.6f, 0.9f), hazard));
                MeshChild(st, "Lab_Pillar_" + (a < 0 ? "L" : "R") + (b < 0 ? "B" : "F"), "Pillar_" + (a < 0 ? "L" : "R") + (b < 0 ? "B" : "F"), p, Solid.Box, Vector3.zero);
            }
        // roof: four panels with stuff on top, lamps underneath
        float roofY = FY + WallH + 0.2f;
        for (int a = -1; a <= 1; a += 2)
            for (int b = -1; b <= 1; b += 2)
            {
                var p = new List<Part>();
                Vector3 c = new Vector3(a * 3.6f, roofY, b * 3.2f);
                p.Add(B(c, new Vector3(7.25f, 0.4f, 6.45f), concrete));
                p.Add(B(new Vector3(a * 7.1f, roofY + 0.35f, b * 3.2f), new Vector3(0.2f, 0.4f, 6.45f), metalDark));     // low wall round the edge
                p.Add(B(new Vector3(a * 3.6f, roofY + 0.35f, b * 6.3f), new Vector3(7.25f, 0.4f, 0.2f), metalDark));
                p.Add(B(new Vector3(a * 3.3f, roofY - 0.24f, b * 2.4f), new Vector3(2.2f, 0.08f, 0.45f), white));       // ceiling lamp
                p.Add(B(new Vector3(a * 3.3f, roofY - 0.2f, b * 2.4f), new Vector3(2.4f, 0.06f, 0.6f), metalDark));
                if (a < 0 && b > 0)
                {
                    for (int i = 0; i < 3; i++) p.Add(B(new Vector3(-2f - i * 1.5f, roofY + 0.5f, 3f), new Vector3(0.8f, 0.6f, 0.8f), metal));   // vents
                    for (int i = 0; i < 3; i++) p.Add(B(new Vector3(-2f - i * 1.5f, roofY + 0.81f, 3f), new Vector3(0.7f, 0.04f, 0.7f), grate));
                }
                else if (a > 0 && b > 0)
                {
                    p.Add(B(new Vector3(4.5f, roofY + 0.7f, 2.5f), new Vector3(2f, 1f, 1.4f), metal));                    // air-con unit
                    p.Add(Cy(new Vector3(4.5f, roofY + 1.2f, 2.5f), 0.55f, 0.55f, 0.02f, rubber, 14));
                    p.Add(B(new Vector3(4.5f, roofY + 1.23f, 2.5f), new Vector3(1f, 0.02f, 0.12f), metalDark, new Vector3(0f, 30f, 0f)));
                }
                else if (a < 0 && b < 0)
                {
                    p.Add(Cy(new Vector3(-4.6f, roofY + 0.2f, -3f), 0.45f, 0.38f, 2.2f, metalDark, 12));                  // chimney
                    p.Add(new Part(Tube(12, 0.5f, 0.3f, 0.15f), TRS(new Vector3(-4.6f, roofY + 2.4f, -3f), Vector3.zero, Vector3.one), hazard));
                    p.Add(Cy(new Vector3(-2.2f, roofY + 0.2f, -2.2f), 0.3f, 0.3f, 1.2f, metal, 10));                       // tank
                    p.Add(Ball(new Vector3(-2.2f, roofY + 1.4f, -2.2f), new Vector3(0.6f, 0.3f, 0.6f), metal));
                }
                else
                {
                    p.Add(B(new Vector3(3.4f, roofY + 0.9f, -3.2f), new Vector3(3.2f, 0.08f, 2f), glass, new Vector3(-25f, 0f, 0f)));   // solar panel
                    p.Add(B(new Vector3(3.4f, roofY + 0.85f, -3.2f), new Vector3(3.3f, 0.06f, 2.1f), metalDark, new Vector3(-25f, 0f, 0f)));
                    p.Add(B(new Vector3(3.4f, roofY + 0.5f, -3.9f), new Vector3(0.1f, 0.7f, 0.1f), metalDark));
                    // beacon pole (the light on top is a separate object)
                    p.Add(Cy(new Vector3(6.1f, roofY + 0.2f, -5.1f), 0.08f, 0.08f, 1.7f, metalDark, 8));
                    p.Add(Ball(new Vector3(6.1f, roofY + 2.05f, -5.1f), 0.4f, accent));
                }
                GameObject piece = MeshChild(st, "Lab_Roof_" + (a < 0 ? "L" : "R") + (b < 0 ? "B" : "F"), "Roof_" + (a < 0 ? "L" : "R") + (b < 0 ? "B" : "F"), p, Solid.Box, Vector3.zero);
                if ((a < 0 && b > 0) || (a > 0 && b < 0))
                    AddLight(piece.transform, "Lamp", new Vector3(a * 3.3f, roofY - 0.6f, b * 2.4f), new Color(1f, 0.95f, 0.85f), 11f, 1.8f);
                if (a > 0 && b < 0)
                    AddLight(piece.transform, "Beacon Light", new Vector3(6.1f, roofY + 2.05f, -5.1f), new Color(0.35f, 1f, 0.3f), 9f, 2.5f);
            }

        // ---------------- doors (only open for the lab's owner) ----------------
        for (int s = -1; s <= 1; s += 2)
        {
            var p = new List<Part>();
            p.Add(B(Vector3.zero, new Vector3(DoorW / 2f, DoorH, 0.16f), metal));
            p.Add(B(new Vector3(0f, 0.55f, 0f), new Vector3(0.9f, 0.9f, 0.18f), glass));
            p.Add(B(new Vector3(0f, -DoorH / 2f + 0.2f, 0f), new Vector3(DoorW / 2f, 0.3f, 0.18f), hazard));
            p.Add(B(new Vector3(-s * (DoorW / 4f - 0.05f), 0f, 0f), new Vector3(0.08f, DoorH - 0.2f, 0.2f), accent));   // glowing edge where they meet
            p.Add(B(new Vector3(-s * 0.4f, 0f, 0.1f), new Vector3(0.06f, 0.5f, 0.06f), metalDark));                   // handle
            MeshChild(r, "Lab_Door_" + (s < 0 ? "L" : "R"), s < 0 ? "Door_L" : "Door_R", p, Solid.Box, new Vector3(s * DoorW / 4f, FY + DoorH / 2f, HZ));
        }

        // ---------------- stations ----------------
        MeshChild(r, "Lab_Brew", "Station_Brew", BrewParts(), Solid.Box, new Vector3(-4.7f, FY, -3.7f));
        GameObject brew = r.Find("Station_Brew").gameObject;
        MeshChild(brew.transform, "Lab_Brew_Liquid", "Liquid", new List<Part> { new Part(Disc(16, 0.66f), TRS(new Vector3(0f, 1.12f, 0f), Vector3.zero, Vector3.one), toxic) }, Solid.None, Vector3.zero);
        AddLight(brew.transform, "Fire Glow", new Vector3(0f, 0.4f, 0f), new Color(1f, 0.55f, 0.2f), 4f, 1.2f);

        GameObject brew2 = MeshChild(r, "Lab_Brew2", "Up_Brew2", Cauldron(0.8f, false), Solid.Box, new Vector3(-2.6f, FY, -2.7f));
        MeshChild(brew2.transform, "Lab_Brew2_Liquid", "Liquid_2", new List<Part> { new Part(Disc(16, 0.66f * 0.8f), TRS(new Vector3(0f, 1.12f * 0.8f, 0f), Vector3.zero, Vector3.one), toxic) }, Solid.None, Vector3.zero);
        brew2.SetActive(false);

        MeshChild(r, "Lab_Workbench", "Station_Workbench", WorkbenchParts(), Solid.Box, new Vector3(4.4f, FY, -4.3f));
        MeshChild(r, "Lab_Terminal", "Station_Terminal", TerminalParts(), Solid.Box, new Vector3(0f, FY, -4.9f));

        GameObject ench = MeshChild(r, "Lab_Enchant", "Station_Enchant", EnchantParts(), Solid.None, new Vector3(-5.1f, FY, 1.4f));
        var ec = ench.AddComponent<BoxCollider>();
        ec.center = new Vector3(0f, 0.5f, 0f);
        ec.size = new Vector3(1f, 1f, 1f);
        var crystalParts = new List<Part> { CrystalPart(new Vector3(0f, -0.45f, 0f), 0.2f, 0.9f, Vector3.zero, crystalPink, true) };
        MeshChild(ench.transform, "Lab_Enchant_Crystal", "Enchant_Crystal", crystalParts, Solid.None, new Vector3(0f, 1.65f, 0f));
        AddLight(ench.transform, "Rune Glow", new Vector3(0f, 1.4f, 0f), new Color(0.85f, 0.45f, 1f), 5f, 1.6f);

        MeshChild(r, "Lab_Storage", "Station_Storage", StorageParts(), Solid.Box, new Vector3(5.85f, FY, 1.4f), new Vector3(0f, -90f, 0f));

        // decoration inside (doesn't do anything)
        MeshChild(r, "Lab_Interior", "Interior", InteriorParts(), Solid.Shape, Vector3.zero);

        Empty(r, "SpawnPoint", new Vector3(0f, FY + 0.05f, 1.2f));

        // ---------------- upgrades (hidden until you build them) ----------------
        GameObject w1 = MeshChild(st, "Lab_Up_Walls1", "Up_WallsI", WallsIParts(), Solid.None, Vector3.zero);
        w1.SetActive(false);
        GameObject w2 = MeshChild(st, "Lab_Up_Walls2", "Up_WallsII", WallsIIParts(), Solid.None, Vector3.zero);
        w2.SetActive(false);
        GameObject field = MeshChild(st, "Lab_Up_Field", "Up_Field", FieldParts(), Solid.None, Vector3.zero);
        field.SetActive(false);
        MakeTurret(r);
        MakeTraps(r);
        GameObject rubble = MeshChild(r, "Lab_Rubble", "Rubble", RubbleParts(), Solid.Shape, Vector3.zero);
        rubble.SetActive(false);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/Laboratory.prefab");
        Object.DestroyImmediate(root);
    }

    // A wall slab with a darker skirting and a top trim. 'outward' points out of the building.
    static List<Part> WallPiece(Vector3 center, Vector3 size, Vector3 outward)
    {
        var p = new List<Part>();
        p.Add(B(center, size, labWall));
        Vector3 skirt = new Vector3(size.x + (outward.x != 0f ? 0.1f : 0f), 0.45f, size.z + (outward.z != 0f ? 0.1f : 0f));
        float bottom = center.y - size.y / 2f, top = center.y + size.y / 2f;
        bool reachesFloor = bottom <= FY + 0.05f;          // (the bit above the door doesn't - it must not get a floor trim,
                                                           //  or its collision box fills the whole doorway)
        if (reachesFloor) p.Add(B(new Vector3(center.x, FY + 0.22f, center.z), skirt, metalDark));
        if (top >= FY + WallH - 0.05f) p.Add(B(new Vector3(center.x, FY + WallH - 0.12f, center.z), skirt + new Vector3(0f, -0.2f, 0f), metalDark));
        // a glowing stripe in the lab colour around the outside
        Vector3 o = outward * (Mathf.Abs(outward.x) > 0f ? size.x / 2f + 0.02f : size.z / 2f + 0.02f);
        Vector3 stripe = Mathf.Abs(outward.x) > 0f ? new Vector3(0.03f, 0.1f, size.z) : new Vector3(size.x, 0.1f, 0.03f);
        if (reachesFloor) p.Add(B(new Vector3(center.x, FY + 1.1f, center.z) + o, stripe, accent));
        return p;
    }

    // Glass window (both sides) with a frame. side = -1/1 for side walls (window faces X), 0 for front/back (faces Z).
    static void Window(List<Part> p, Vector3 c, float w, float h, int side, bool front = false)
    {
        bool x = side != 0;
        Vector3 glassSize = x ? new Vector3(WT + 0.06f, h, w) : new Vector3(w, h, WT + 0.06f);
        p.Add(B(c, glassSize, glass));
        Vector3 frameH = x ? new Vector3(WT + 0.14f, 0.12f, w + 0.24f) : new Vector3(w + 0.24f, 0.12f, WT + 0.14f);
        Vector3 frameV = x ? new Vector3(WT + 0.14f, h, 0.12f) : new Vector3(0.12f, h, WT + 0.14f);
        p.Add(B(c + Vector3.up * (h / 2f + 0.06f), frameH, metalDark));
        p.Add(B(c - Vector3.up * (h / 2f + 0.06f), frameH, metalDark));
        Vector3 along = x ? Vector3.forward : Vector3.right;
        p.Add(B(c + along * (w / 2f + 0.06f), frameV, metalDark));
        p.Add(B(c - along * (w / 2f + 0.06f), frameV, metalDark));
        p.Add(B(c, x ? new Vector3(WT + 0.1f, h, 0.06f) : new Vector3(0.06f, h, WT + 0.1f), metalDark));   // middle bar
    }

    // ---------------- stations ----------------

    static List<Part> Cauldron(float s, bool withShelf)
    {
        var p = new List<Part>();
        p.Add(Cy(Vector3.zero, 0.75f * s, 0.7f * s, 0.25f * s, metalDark, 12));                        // burner
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Mathf.Deg2Rad;
            p.Add(new Part(Cone(5, 0.09f * s, 0.35f * s, i), TRS(new Vector3(Mathf.Cos(a) * 0.3f * s, 0.25f * s, Mathf.Sin(a) * 0.3f * s), Vector3.zero, Vector3.one), fire));
        }
        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f;
            p.Add(B(Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.55f * s, 0.62f * s), new Vector3(0.08f, 0.6f, 0.08f) * s, iron, new Vector3(-12f, a, 0f)));   // legs
        }
        p.Add(Cy(new Vector3(0f, 0.35f * s, 0f), 0.42f * s, 0.78f * s, 0.45f * s, iron, 14));
        p.Add(Cy(new Vector3(0f, 0.8f * s, 0f), 0.78f * s, 0.7f * s, 0.38f * s, iron, 14, default, false));
        p.Add(new Part(Tube(14, 0.78f * s, 0.64f * s, 0.08f * s), TRS(new Vector3(0f, 1.15f * s, 0f), Vector3.zero, Vector3.one), iron));
        if (withShelf)
        {
            // shelf of flasks next to it
            p.Add(B(new Vector3(1.35f, 1.2f, -0.55f), new Vector3(1.2f, 0.06f, 0.35f), wood));
            p.Add(B(new Vector3(1.35f, 0.6f, -0.55f), new Vector3(1.2f, 0.06f, 0.35f), wood));
            foreach (float x in new[] { 0.8f, 1.9f }) p.Add(B(new Vector3(x, 0.9f, -0.55f), new Vector3(0.06f, 1.8f, 0.35f), woodDark));
            Material[] liq = { glowGreen, glowPurple, glowOrange, glowCyan, redGlow };
            for (int i = 0; i < 5; i++)
            {
                Vector3 b = new Vector3(0.95f + i * 0.2f, 1.23f, -0.55f);
                p.Add(Ball(b + Vector3.up * 0.1f, 0.17f, glass));
                p.Add(Ball(b + Vector3.up * 0.08f, 0.13f, liq[i]));
                p.Add(Cy(b + Vector3.up * 0.17f, 0.025f, 0.025f, 0.1f, glass, 6));
                Vector3 b2 = new Vector3(1.0f + i * 0.2f, 0.63f, -0.5f);
                p.Add(Cy(b2, 0.05f, 0.05f, 0.25f, liq[(i + 2) % 5], 8));
            }
            // fume hood above, going up through the roof
            p.Add(Cy(new Vector3(0f, 2.5f, 0f), 1.05f, 0.3f, 0.8f, metalDark, 12, default, false));
            p.Add(Cy(new Vector3(0f, 3.3f, 0f), 0.28f, 0.28f, 1.3f, metal, 10, default, false));
        }
        return p;
    }

    static List<Part> BrewParts() => Cauldron(1f, true);

    static List<Part> WorkbenchParts()
    {
        var p = new List<Part>();
        p.Add(B(new Vector3(0f, 0.95f, 0f), new Vector3(2.8f, 0.12f, 1.1f), wood));
        p.Add(B(new Vector3(0f, 0.86f, 0f), new Vector3(2.7f, 0.08f, 1f), metalDark));
        foreach (float x in new[] { -1.3f, 1.3f })
            foreach (float z in new[] { -0.45f, 0.45f })
                p.Add(B(new Vector3(x, 0.43f, z), new Vector3(0.1f, 0.86f, 0.1f), metalDark));
        p.Add(B(new Vector3(0f, 0.3f, 0f), new Vector3(2.6f, 0.05f, 0.9f), woodDark));                // lower shelf
        p.Add(B(new Vector3(-0.6f, 0.4f, 0f), new Vector3(0.6f, 0.15f, 0.4f), metal));                // toolbox
        p.Add(B(new Vector3(-0.6f, 0.4f, 0f), new Vector3(0.62f, 0.05f, 0.42f), redGlow));
        // vise
        p.Add(B(new Vector3(1.05f, 1.1f, 0.3f), new Vector3(0.3f, 0.18f, 0.25f), metalDark));
        p.Add(B(new Vector3(1.05f, 1.22f, 0.42f), new Vector3(0.3f, 0.12f, 0.05f), metal));
        p.Add(Cy(new Vector3(1.05f, 1.1f, 0.42f), 0.02f, 0.02f, 0.35f, metal, 6, new Vector3(90f, 0f, 0f)));
        // hammer, wrench, gears, bits of a gun being built
        p.Add(Cy(new Vector3(-0.2f, 1.03f, 0.25f), 0.025f, 0.025f, 0.42f, wood, 6, new Vector3(0f, 0f, 90f)));
        p.Add(B(new Vector3(-0.44f, 1.05f, 0.25f), new Vector3(0.08f, 0.08f, 0.18f), metalDark));
        p.Add(B(new Vector3(0.3f, 1.02f, 0.3f), new Vector3(0.35f, 0.02f, 0.05f), metal, new Vector3(0f, 30f, 0f)));
        for (int i = 0; i < 3; i++) p.Add(Cy(new Vector3(-0.9f + i * 0.18f, 1.01f, -0.25f), 0.08f + i * 0.02f, 0.08f + i * 0.02f, 0.03f, brass, 10));
        p.Add(B(new Vector3(0.2f, 1.06f, -0.15f), new Vector3(0.5f, 0.1f, 0.12f), metalDark));
        p.Add(Cy(new Vector3(0.45f, 1.06f, -0.15f), 0.04f, 0.04f, 0.2f, glowCyan, 8, new Vector3(0f, 0f, -90f)));
        p.Add(B(new Vector3(0.4f, 1.015f, 0.05f), new Vector3(0.7f, 0.005f, 0.5f), screen));           // glowing blueprint
        // pegboard on the wall behind
        p.Add(B(new Vector3(0f, 2.0f, -0.62f), new Vector3(2.6f, 1.3f, 0.05f), woodDark));
        for (int i = 0; i < 6; i++)
            p.Add(B(new Vector3(-1.0f + i * 0.4f, 2.0f + (i % 2) * 0.2f, -0.58f), new Vector3(0.06f, 0.5f - (i % 3) * 0.1f, 0.03f), i % 2 == 0 ? metal : redGlow));
        // desk lamp
        p.Add(Cy(new Vector3(-1.15f, 1.01f, 0.35f), 0.1f, 0.1f, 0.04f, metalDark, 8));
        p.Add(B(new Vector3(-1.15f, 1.25f, 0.3f), new Vector3(0.04f, 0.5f, 0.04f), metalDark, new Vector3(-15f, 0f, 0f)));
        p.Add(B(new Vector3(-1.15f, 1.48f, 0.12f), new Vector3(0.04f, 0.04f, 0.35f), metalDark));
        p.Add(B(new Vector3(-1.15f, 1.42f, -0.05f), new Vector3(0.18f, 0.08f, 0.18f), white));
        return p;
    }

    static List<Part> TerminalParts()
    {
        var p = new List<Part>();
        p.Add(B(new Vector3(0f, 0.86f, 0.1f), new Vector3(2.4f, 0.08f, 0.9f), metalDark));
        foreach (float x in new[] { -1.1f, 1.1f }) p.Add(B(new Vector3(x, 0.42f, 0.1f), new Vector3(0.08f, 0.84f, 0.8f), metalDark));
        p.Add(B(new Vector3(0f, 0.92f, 0.35f), new Vector3(0.9f, 0.03f, 0.3f), rubber));               // keyboard
        for (int i = 0; i < 12; i++) p.Add(B(new Vector3(-0.4f + (i % 6) * 0.16f, 0.94f, 0.3f + (i / 6) * 0.1f), new Vector3(0.12f, 0.015f, 0.07f), metal));
        // three monitors
        float[] mx = { -0.8f, 0f, 0.8f }, my = { 20f, 0f, -20f };
        for (int i = 0; i < 3; i++)
        {
            Vector3 c = new Vector3(mx[i], 1.32f, -0.1f + Mathf.Abs(mx[i]) * 0.12f);
            p.Add(B(c, new Vector3(0.74f, 0.48f, 0.05f), metalDark, new Vector3(-8f, my[i], 0f)));
            p.Add(B(c + Quaternion.Euler(-8f, my[i], 0f) * new Vector3(0f, 0f, 0.03f), new Vector3(0.66f, 0.4f, 0.01f), screen, new Vector3(-8f, my[i], 0f)));
            p.Add(B(new Vector3(mx[i], 1.0f, c.z - 0.05f), new Vector3(0.06f, 0.2f, 0.06f), metalDark));
        }
        // big screen on the wall
        p.Add(B(new Vector3(0f, 2.75f, -0.42f), new Vector3(2.5f, 1.1f, 0.06f), metalDark));
        p.Add(B(new Vector3(0f, 2.75f, -0.385f), new Vector3(2.35f, 0.95f, 0.01f), screen));
        p.Add(B(new Vector3(0f, 2.95f, -0.37f), new Vector3(1.8f, 0.08f, 0.01f), accent));
        p.Add(B(new Vector3(-0.4f, 2.6f, -0.37f), new Vector3(0.9f, 0.05f, 0.01f), chalk));
        p.Add(B(new Vector3(0.6f, 2.55f, -0.37f), new Vector3(0.5f, 0.3f, 0.01f), glowGreen));
        // server racks with blinking lights
        foreach (float x in new[] { -1.75f, 1.75f })
        {
            p.Add(B(new Vector3(x, 1.1f, -0.05f), new Vector3(0.8f, 2.2f, 0.75f), metalDark));
            for (int row = 0; row < 7; row++)
            {
                p.Add(B(new Vector3(x, 0.4f + row * 0.27f, 0.33f), new Vector3(0.7f, 0.2f, 0.01f), rubber));
                p.Add(B(new Vector3(x - 0.25f + (row % 3) * 0.06f, 0.4f + row * 0.27f, 0.34f), new Vector3(0.04f, 0.04f, 0.01f), row % 4 == 0 ? redGlow : accent));
                p.Add(B(new Vector3(x + 0.2f, 0.4f + row * 0.27f, 0.34f), new Vector3(0.2f, 0.02f, 0.01f), glowCyan));
            }
        }
        p.Add(Cy(new Vector3(0f, 0f, 0.8f), 0.25f, 0.25f, 0.45f, metalDark, 10));     // stool
        p.Add(Cy(new Vector3(0f, 0.45f, 0.8f), 0.28f, 0.28f, 0.08f, rubber, 10));
        return p;
    }

    static List<Part> EnchantParts()
    {
        var p = new List<Part>();
        p.Add(new Part(Tube(24, 1.4f, 1.22f, 0.02f), TRS(new Vector3(0f, 0.01f, 0f), Vector3.zero, Vector3.one), glowPurple));
        p.Add(new Part(Tube(24, 0.95f, 0.88f, 0.02f), TRS(new Vector3(0f, 0.01f, 0f), Vector3.zero, Vector3.one), glowPurple));
        for (int i = 0; i < 8; i++)
            p.Add(B(Quaternion.Euler(0f, i * 45f, 0f) * new Vector3(0f, 0.012f, 1.08f), new Vector3(0.12f, 0.02f, 0.2f), glowPurple, new Vector3(0f, i * 45f, 0f)));   // runes
        p.Add(Cy(Vector3.zero, 0.5f, 0.36f, 0.95f, stone, 8));
        p.Add(Cy(new Vector3(0f, 0.95f, 0f), 0.55f, 0.55f, 0.1f, stone, 8));
        p.Add(new Part(Tube(8, 0.5f, 0.42f, 0.03f), TRS(new Vector3(0f, 1.05f, 0f), Vector3.zero, Vector3.one), brass));
        for (int i = 0; i < 4; i++)
        {
            Vector3 b = Quaternion.Euler(0f, 45f + i * 90f, 0f) * new Vector3(0f, 0f, 1.3f);
            p.Add(Cy(b, 0.1f, 0.08f, 0.55f, stone, 6));
            p.Add(Cy(b + Vector3.up * 0.55f, 0.05f, 0.05f, 0.12f, chalk, 6));
            p.Add(Ball(b + Vector3.up * 0.74f, new Vector3(0.08f, 0.13f, 0.08f), glowPurple));
        }
        return p;
    }

    static List<Part> StorageParts()
    {
        var p = new List<Part>();
        foreach (float x in new[] { -1.3f, 1.3f })
            foreach (float z in new[] { -0.3f, 0.3f })
                p.Add(B(new Vector3(x, 1.1f, z), new Vector3(0.08f, 2.2f, 0.08f), metalDark));
        for (int s = 0; s < 3; s++) p.Add(B(new Vector3(0f, 0.25f + s * 0.75f, 0f), new Vector3(2.7f, 0.06f, 0.7f), metal));
        // a bin for each material, with a glowing lid in its colour
        for (int m = 0; m < Items.MatCount; m++)
        {
            int shelf = m < 3 ? 0 : 1;
            float x = shelf == 0 ? -0.85f + m * 0.85f : -0.45f + (m - 3) * 0.9f;
            float y = 0.28f + shelf * 0.75f;
            p.Add(B(new Vector3(x, y + 0.2f, 0f), new Vector3(0.65f, 0.4f, 0.55f), metalDark));
            p.Add(B(new Vector3(x, y + 0.42f, 0f), new Vector3(0.6f, 0.05f, 0.5f), binGlow[m]));
            p.Add(B(new Vector3(x, y + 0.2f, 0.28f), new Vector3(0.3f, 0.12f, 0.01f), binGlow[m]));
        }
        // big jar on the top shelf
        p.Add(Cy(new Vector3(0.7f, 1.78f, 0f), 0.25f, 0.25f, 0.55f, glass, 12));
        p.Add(Cy(new Vector3(0.7f, 1.8f, 0f), 0.22f, 0.22f, 0.35f, slime, 12));
        p.Add(Cy(new Vector3(-0.6f, 1.78f, 0f), 0.2f, 0.2f, 0.4f, glass, 12));
        p.Add(Cy(new Vector3(-0.6f, 1.8f, 0f), 0.17f, 0.17f, 0.25f, chem, 12));
        return p;
    }

    static List<Part> InteriorParts()
    {
        var p = new List<Part>();
        // pipes along the back wall near the ceiling
        for (int i = 0; i < 2; i++)
            p.Add(Cy(new Vector3(-6.5f, FY + 4.0f + i * 0.3f, -5.35f + i * 0.05f), 0.12f, 0.12f, 13f, i == 0 ? metal : rust, 8, new Vector3(0f, 0f, -90f)));
        p.Add(Cy(new Vector3(6.3f, FY, -5.3f), 0.12f, 0.12f, 4.2f, metal, 8));
        // lab table with beakers
        Vector3 t = new Vector3(2.3f, FY, 2.4f);
        p.Add(B(t + new Vector3(0f, 0.95f, 0f), new Vector3(2.6f, 0.08f, 1f), chalk));
        foreach (float x in new[] { -1.2f, 1.2f })
            foreach (float z in new[] { -0.4f, 0.4f })
                p.Add(B(t + new Vector3(x, 0.47f, z), new Vector3(0.07f, 0.94f, 0.07f), metalDark));
        Material[] liq = { glowGreen, glowPurple, glowCyan, glowOrange };
        for (int i = 0; i < 4; i++)
        {
            Vector3 b = t + new Vector3(-0.9f + i * 0.55f, 0.99f, (i % 2) * 0.2f - 0.1f);
            p.Add(Cy(b, 0.12f, 0.08f, 0.22f, glass, 10));
            p.Add(Cy(b, 0.11f, 0.09f, 0.14f, liq[i], 10));
        }
        p.Add(B(t + new Vector3(0.9f, 1.05f, 0.25f), new Vector3(0.5f, 0.1f, 0.15f), woodDark));     // test tube rack
        for (int i = 0; i < 5; i++) p.Add(Cy(t + new Vector3(0.7f + i * 0.1f, 1.0f, 0.25f), 0.025f, 0.025f, 0.28f, liq[i % 4], 6));
        // tesla coil in the corner
        Vector3 tc = new Vector3(-5.7f, FY, 4.6f);
        p.Add(Cy(tc, 0.45f, 0.4f, 0.4f, metalDark, 10));
        p.Add(Cy(tc + Vector3.up * 0.4f, 0.16f, 0.16f, 1.5f, brass, 10));
        for (int i = 0; i < 8; i++) p.Add(new Part(Tube(10, 0.2f, 0.15f, 0.04f), TRS(tc + Vector3.up * (0.5f + i * 0.17f), Vector3.zero, Vector3.one), metalDark));
        p.Add(new Part(Tube(14, 0.55f, 0.25f, 0.2f), TRS(tc + Vector3.up * 1.9f, Vector3.zero, Vector3.one), metal));
        p.Add(Ball(tc + Vector3.up * 2.15f, 0.25f, glowCyan));
        // chalkboard full of equations on the left wall
        p.Add(B(new Vector3(-6.56f, FY + 2.3f, -2.4f), new Vector3(0.05f, 1.4f, 2.8f), board));
        p.Add(B(new Vector3(-6.54f, FY + 2.3f, -2.4f), new Vector3(0.04f, 1.55f, 2.95f), woodDark));
        for (int i = 0; i < 7; i++)
            p.Add(B(new Vector3(-6.52f, FY + 2.75f - i * 0.15f, -3.3f + (i % 3) * 0.5f), new Vector3(0.01f, 0.03f, 0.5f + (i % 4) * 0.3f), chalk));
        // plant by the door, fire extinguisher, hazard tape
        p.Add(Cy(new Vector3(2.5f, FY, 5.0f), 0.25f, 0.3f, 0.45f, rust, 10));
        p.Add(Ball(new Vector3(2.5f, FY + 0.75f, 5.0f), new Vector3(0.7f, 0.65f, 0.7f), herbLeaf, 5, 0.25f));
        p.Add(Cy(new Vector3(-2.3f, FY, 5.35f), 0.12f, 0.12f, 0.55f, redGlow, 10));
        p.Add(B(new Vector3(0f, FY + 0.03f, 4.6f), new Vector3(3.2f, 0.01f, 0.25f), hazard));
        // lockers by the door
        for (int i = 0; i < 2; i++)
        {
            p.Add(B(new Vector3(5.6f - i * 0.75f, FY + 1f, 5.2f), new Vector3(0.7f, 2f, 0.6f), metal));
            p.Add(B(new Vector3(5.6f - i * 0.75f, FY + 1.6f, 4.89f), new Vector3(0.4f, 0.05f, 0.02f), metalDark));
        }
        return p;
    }

    // ---------------- upgrades ----------------

    static List<Part> WallsIParts()
    {
        var p = new List<Part>();
        // steel plates bolted on the outside
        for (int s = -1; s <= 1; s += 2)
            for (int h = -1; h <= 1; h += 2)
            {
                p.Add(B(new Vector3(s * 7.08f, FY + 1.0f, h * 2.8f), new Vector3(0.12f, 1.4f, 5.0f), rust));
                p.Add(B(new Vector3(s * 7.1f, FY + 2.6f, h * 2.8f), new Vector3(0.1f, 0.18f, 5.2f), metalDark, new Vector3(h * 18f, 0f, 0f)));
                for (int b = 0; b < 4; b++) p.Add(B(new Vector3(s * 7.16f, FY + 1.5f, h * 2.8f - 2f + b * 1.33f), Vector3.one * 0.09f, metal));
            }
        for (int s = -1; s <= 1; s += 2)
        {
            p.Add(B(new Vector3(s * 3.5f, FY + 1.0f, -6.08f), new Vector3(6.4f, 1.4f, 0.12f), rust));
            p.Add(B(new Vector3(s * 4.4f, FY + 1.0f, 6.08f), new Vector3(4.6f, 1.4f, 0.12f), rust));
            for (int b = 0; b < 4; b++) p.Add(B(new Vector3(s * (2.6f + b * 1.2f), FY + 1.5f, 6.16f), Vector3.one * 0.09f, metal));
        }
        return p;
    }

    static List<Part> WallsIIParts()
    {
        var p = new List<Part>();
        // crystal-steel panels higher up
        for (int s = -1; s <= 1; s += 2)
            for (int h = -1; h <= 1; h += 2)
                p.Add(B(new Vector3(s * 7.12f, FY + 3.5f, h * 2.8f), new Vector3(0.1f, 1.2f, 4.6f), crystal));
        for (int s = -1; s <= 1; s += 2)
            p.Add(B(new Vector3(s * 3.5f, FY + 3.5f, -6.12f), new Vector3(6f, 1.2f, 0.1f), crystal));
        // spikes all round the bottom
        for (int i = 0; i < 28; i++)
        {
            float k = i / 28f;
            Vector3 pos; Vector3 outward;
            if (k < 0.25f) { pos = new Vector3(-7.4f, FY, Mathf.Lerp(-6f, 6f, k * 4f)); outward = Vector3.left; }
            else if (k < 0.5f) { pos = new Vector3(7.4f, FY, Mathf.Lerp(-6f, 6f, (k - 0.25f) * 4f)); outward = Vector3.right; }
            else if (k < 0.75f) { pos = new Vector3(Mathf.Lerp(-7f, 7f, (k - 0.5f) * 4f), FY, -6.4f); outward = Vector3.back; }
            else
            {
                float x = Mathf.Lerp(-7f, 7f, (k - 0.75f) * 4f);
                if (Mathf.Abs(x) < 2.4f) continue;                     // not in front of the door
                pos = new Vector3(x, FY, 6.4f); outward = Vector3.forward;
            }
            Vector3 euler = Quaternion.FromToRotation(Vector3.up, (outward + Vector3.up).normalized).eulerAngles;
            p.Add(Cy(pos, 0.12f, 0f, 0.9f, metalDark, 6, euler));
        }
        return p;
    }

    static List<Part> FieldParts()
    {
        var p = new List<Part>();
        float y = FY + WallH + 0.4f;
        foreach (float x in new[] { -6.5f, 6.5f })
            foreach (float z in new[] { -5.5f, 5.5f })
            {
                p.Add(Cy(new Vector3(x, y, z), 0.2f, 0.12f, 1.3f, metalDark, 8));
                p.Add(new Part(Tube(10, 0.3f, 0.15f, 0.08f), TRS(new Vector3(x, y + 0.9f, z), Vector3.zero, Vector3.one), brass));
                p.Add(Ball(new Vector3(x, y + 1.45f, z), 0.4f, glowCyan));
            }
        p.Add(Cy(new Vector3(0f, y, 0f), 0.6f, 0.5f, 0.6f, metalDark, 12));
        p.Add(new Part(Dome(14, 1.1f), TRS(new Vector3(0f, y + 0.6f, 0f), Vector3.zero, new Vector3(1f, 0.6f, 1f)), crystal));
        return p;
    }

    static void MakeTurret(Transform r)
    {
        float y = FY + WallH + 0.4f;
        var baseParts = new List<Part>
        {
            Cy(Vector3.zero, 0.7f, 0.6f, 0.3f, metalDark, 12),
            new Part(Tube(12, 0.62f, 0.5f, 0.05f), TRS(new Vector3(0f, 0.3f, 0f), Vector3.zero, Vector3.one), hazard),
        };
        GameObject up = MeshChild(r, "Lab_Turret_Base", "Up_Turret", baseParts, Solid.None, new Vector3(0f, y, 4.3f));
        var yawParts = new List<Part>
        {
            B(new Vector3(0f, 0.3f, 0f), new Vector3(0.9f, 0.45f, 0.9f), metalDark),
            B(new Vector3(0f, 0.3f, 0.46f), new Vector3(0.6f, 0.1f, 0.02f), accent),
            B(new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.1f, 0.5f, 0.5f), metal),
            B(new Vector3(0.5f, 0.5f, 0f), new Vector3(0.1f, 0.5f, 0.5f), metal),
        };
        GameObject yaw = MeshChild(up.transform, "Lab_Turret_Yaw", "Turret_Yaw", yawParts, Solid.None, new Vector3(0f, 0.3f, 0f));
        var pitchParts = new List<Part>
        {
            B(new Vector3(0f, 0f, 0.1f), new Vector3(0.5f, 0.32f, 0.6f), metal),
            Cy(new Vector3(-0.12f, 0f, 0.3f), 0.07f, 0.06f, 0.75f, metalDark, 8, new Vector3(90f, 0f, 0f)),
            Cy(new Vector3(0.12f, 0f, 0.3f), 0.07f, 0.06f, 0.75f, metalDark, 8, new Vector3(90f, 0f, 0f)),
            Ball(new Vector3(0f, 0.08f, 0.38f), new Vector3(0.18f, 0.1f, 0.06f), redGlow),
        };
        GameObject pitch = MeshChild(yaw.transform, "Lab_Turret_Pitch", "Turret_Pitch", pitchParts, Solid.None, new Vector3(0f, 0.6f, 0f));
        Empty(pitch.transform, "Turret_Muzzle", new Vector3(0f, 0f, 1.12f));
        up.SetActive(false);
    }

    static void MakeTraps(Transform r)
    {
        GameObject traps = Empty(r, "Up_Traps", Vector3.zero);
        Vector3[] spots = { new Vector3(-3.8f, 0.03f, 9.4f), new Vector3(3.8f, 0.03f, 9.4f), new Vector3(-9.3f, 0.03f, 1.5f), new Vector3(9.3f, 0.03f, 1.5f) };
        for (int i = 0; i < spots.Length; i++)
        {
            var p = new List<Part>
            {
                new Part(Tube(14, 1.0f, 0.82f, 0.08f), Matrix4x4.identity, metalDark),
                new Part(Disc(14, 0.84f), TRS(new Vector3(0f, 0.04f, 0f), Vector3.zero, Vector3.one), slime),
                B(new Vector3(0f, 0.09f, 0f), new Vector3(0.5f, 0.02f, 0.1f), hazard),
            };
            MeshChild(traps.transform, "Lab_Trap", "Trap_" + (i + 1), p, Solid.None, spots[i]);
        }
        traps.SetActive(false);
    }

    static List<Part> RubbleParts()
    {
        var p = new List<Part>();
        var r = new System.Random(123);
        for (int i = 0; i < 26; i++)
        {
            Vector3 pos = new Vector3(((float)r.NextDouble() - 0.5f) * 13f, FY + (float)r.NextDouble() * 0.4f, ((float)r.NextDouble() - 0.5f) * 11f);
            Vector3 e = new Vector3(r.Next(60) - 30, r.Next(360), r.Next(60) - 30);
            if (i % 3 == 0) p.Add(new Part(Lumpy(200 + i, 0.35f), TRS(pos, e, new Vector3(1.4f, 0.7f, 1.1f) * (0.6f + (float)r.NextDouble())), concrete));
            else p.Add(B(pos, new Vector3(1.2f + (float)r.NextDouble() * 1.5f, 0.15f, 0.8f + (float)r.NextDouble()), i % 2 == 0 ? labWall : metalDark, e));
        }
        return p;
    }

    // ================================================================
    // The reactor in the middle of the map
    // ================================================================

    static void MakeReactor()
    {
        var root = new GameObject("Reactor");
        var p = new List<Part>();
        p.Add(Cy(Vector3.zero, 7.5f, 7.2f, 0.8f, concrete, 8));
        p.Add(Cy(new Vector3(0f, 0.8f, 0f), 5.2f, 5f, 0.4f, metalDark, 8));
        p.Add(new Part(Tube(8, 7.25f, 6.9f, 0.05f), TRS(new Vector3(0f, 0.8f, 0f), Vector3.zero, Vector3.one), hazard));
        // the core: a glowing column in a cage
        p.Add(Cy(new Vector3(0f, 1.2f, 0f), 1.9f, 1.9f, 0.6f, metalDark, 12));
        p.Add(Cy(new Vector3(0f, 1.8f, 0f), 1.45f, 1.45f, 6.2f, toxic, 16));
        for (int i = 0; i < 6; i++) p.Add(new Part(Tube(16, 1.75f, 1.5f, 0.18f), TRS(new Vector3(0f, 2.2f + i * 1.1f, 0f), Vector3.zero, Vector3.one), metalDark));
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f;
            p.Add(B(Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 5f, 1.7f), new Vector3(0.22f, 6.4f, 0.22f), metal, new Vector3(0f, a, 0f)));
        }
        p.Add(Cy(new Vector3(0f, 8f, 0f), 2f, 0.8f, 1.1f, metalDark, 12));
        p.Add(Cy(new Vector3(0f, 9.1f, 0f), 0.6f, 0.4f, 1.6f, metal, 10));
        p.Add(Ball(new Vector3(0f, 10.9f, 0f), 0.6f, glowGreen));
        // four tanks with pipes running to the core
        for (int i = 0; i < 4; i++)
        {
            Quaternion q = Quaternion.Euler(0f, 45f + i * 90f, 0f);
            Vector3 c = q * new Vector3(0f, 0f, 5.2f);
            p.Add(Cy(c + Vector3.up * 0.8f, 1.1f, 1.1f, 3.2f, i % 2 == 0 ? barrelGreen : metal, 14));
            p.Add(new Part(Tube(14, 1.15f, 1.0f, 0.3f), TRS(c + Vector3.up * 2.0f, Vector3.zero, Vector3.one), hazard));
            p.Add(new Part(Dome(14, 1.1f), TRS(c + Vector3.up * 4f, Vector3.zero, new Vector3(1f, 0.5f, 1f)), metal));
            Vector3 mid = q * new Vector3(0f, 3.4f, 3.1f);
            p.Add(Cy(mid - q * Vector3.forward * 1.2f, 0.25f, 0.25f, 2.4f, metal, 10, (q * Quaternion.Euler(90f, 0f, 0f)).eulerAngles));
            p.Add(B(c + Vector3.up * 1.6f + q * new Vector3(0f, 0f, 1.12f), new Vector3(0.5f, 0.3f, 0.02f), screen, new Vector3(0f, 45f + i * 90f, 0f)));
        }
        // stairs up to the platform
        for (int i = 0; i < 4; i++)
        {
            Quaternion q = Quaternion.Euler(0f, i * 90f, 0f);
            p.Add(B(q * new Vector3(0f, 0.2f, 7.6f), new Vector3(2.5f, 0.4f, 0.8f), concrete, new Vector3(0f, i * 90f, 0f)));
        }
        Material[] mats;
        Mesh mesh = BuildMesh("Reactor", p, out mats);
        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        root.AddComponent<MeshRenderer>().sharedMaterials = mats;
        root.AddComponent<MeshCollider>().sharedMesh = mesh;
        AddLight(root.transform, "Core Glow", new Vector3(0f, 4.5f, 0f), new Color(0.5f, 1f, 0.35f), 26f, 3f);
        AddLight(root.transform, "Top Glow", new Vector3(0f, 11f, 0f), new Color(0.5f, 1f, 0.35f), 12f, 2f);
        PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/Reactor.prefab");
        Object.DestroyImmediate(root);
    }
}
