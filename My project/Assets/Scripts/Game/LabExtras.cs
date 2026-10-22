using System.Collections.Generic;
using UnityEngine;

// EXTRA LAB LIFE (in the players' labs, made fresh at the start of each match):
//   - Lab pets: a slime bouncing in a fish tank, and a ROBOT CAT that wanders around the lab.
//     The cat hisses at anyone who walks into a lab that isn't theirs (and warns the owner).
//   - Lab levels: YOUR lab gets fancier the more recipes you've discovered (ever):
//       level 1 (10 found)   neon strips along the ceiling
//       level 2 (30 found)   a glowing MAD LAB neon sign over the door
//       level 3 (75 found)   a humming generator with spinning coils
//       level 4 (150 found)  neon floor strips and brighter, coloured lights
//       level 5 (300 found)  a spinning hologram atom over the lab table
//   - The lab radio (on top of the lockers): music that gets louder as you walk up to it. Press E to change song.
//   - The security screen hanging from the ceiling: live views of your front door, outside and inside.
// All of it hangs on the lab building, so it blows apart with the lab.
public static class LabExtras
{
    public const float FY = 0.5f;
    public static readonly int[] LevelAt = { 0, 10, 30, 75, 150, 300 };
    public static readonly string[] LevelReward =
    {
        "", "Neon strips on the ceiling!", "A MAD LAB neon sign over the door!", "A humming generator!", "Neon floor lights!", "A hologram atom over your lab table!",
    };

    public static int DiscoveredCount()
    {
        int n = 0;
        foreach (Recipe r in Items.All) if (Discoveries.Knows(r.id)) n++;
        return n;
    }

    public static int Level(int discovered)
    {
        int lv = 0;
        for (int i = 1; i < LevelAt.Length; i++) if (discovered >= LevelAt[i]) lv = i;
        return lv;
    }

    // "LAB LEVEL 2  (next level at 75)" - shown in the recipe book
    public static string LevelText()
    {
        int n = DiscoveredCount(), lv = Level(n);
        return "LAB LEVEL " + lv + (lv < LevelAt.Length - 1 ? "  (level " + (lv + 1) + " at " + LevelAt[lv + 1] + ")" : "  (MAX)");
    }

    // Called by MLGame at the start of a match for every lab.
    public static void Build(Laboratory lab)
    {
        if (lab == null) return;
        bool mine = lab.OwnedByPlayer;
        // pets live in labs that belong to players (and in yours when you play alone)
        bool playerLab = mine || lab.Index < MLGame.OnlinePlayers;
        if (!playerLab) return;
        Transform structure = lab.transform.Find("Structure");
        Transform parent = structure != null ? structure : lab.transform;
        if (parent.Find("Extras") != null) return;
        var root = new GameObject("Extras");
        root.transform.SetParent(lab.transform, false);

        SlimeTank.Make(root.transform, lab);
        RobotCat.Make(root.transform, lab);
        if (mine)
        {
            var lv = root.AddComponent<LabLevels>();
            lv.Init(lab);
            LabRadio.Make(root.transform, lab);
            if (GraphicsQuality.Level != GraphicsQuality.Low) SecurityScreen.Make(root.transform, lab);
        }
        root.transform.SetParent(parent, true);
    }

    // Is this world point inside the lab's walls?
    public static bool Inside(Laboratory lab, Vector3 world)
    {
        Vector3 l = lab.transform.InverseTransformPoint(world);
        return Mathf.Abs(l.x) < 6.5f && Mathf.Abs(l.z) < 5.5f && l.y > -1f && l.y < 5f;
    }
}

// ================================================================
// Slime in a tank
// ================================================================
public class SlimeTank : MonoBehaviour
{
    Transform slime, eyes;
    float t, hopCd, hop, blubCd;

    public static void Make(Transform parent, Laboratory lab)
    {
        var go = new GameObject("Slime Tank");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(4.6f, LabExtras.FY, 3.7f);
        go.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        var s = go.AddComponent<SlimeTank>();
        Transform r = go.transform;
        Color metal = new Color(0.3f, 0.32f, 0.36f);
        // stand
        Gfx.Part(r, PrimitiveType.Cube, new Vector3(0f, 0.3f, 0f), new Vector3(1f, 0.6f, 0.7f), Gfx.Lit(metal, 0.5f, 0.6f));
        Gfx.Part(r, PrimitiveType.Cube, new Vector3(0f, 0.62f, 0f), new Vector3(1.04f, 0.04f, 0.74f), Gfx.Glow(lab.LabColor, 1.4f), default, false);
        // gravel, water, glass
        Gfx.Part(r, PrimitiveType.Cube, new Vector3(0f, 0.68f, 0f), new Vector3(0.92f, 0.06f, 0.62f), Gfx.Lit(new Color(0.55f, 0.5f, 0.42f)));
        Gfx.Part(r, PrimitiveType.Cube, new Vector3(0f, 0.98f, 0f), new Vector3(0.92f, 0.6f, 0.62f), Gfx.Clear(new Color(0.4f, 0.85f, 0.7f, 0.18f)), default, false);
        Gfx.Part(r, PrimitiveType.Cube, new Vector3(0f, 0.98f, 0f), new Vector3(0.96f, 0.64f, 0.66f), Gfx.Clear(new Color(1f, 1f, 1f, 0.1f)), default, false);
        Gfx.Part(r, PrimitiveType.Cube, new Vector3(0f, 1.32f, 0f), new Vector3(0.98f, 0.04f, 0.68f), Gfx.Lit(metal, 0.5f, 0.6f));
        // a little plant and a castle
        Gfx.Part(r, PrimitiveType.Cylinder, new Vector3(0.32f, 0.78f, 0.15f), new Vector3(0.12f, 0.08f, 0.12f), Gfx.Lit(new Color(0.55f, 0.55f, 0.6f)));
        Gfx.Part(r, PrimitiveType.Cube, new Vector3(0.32f, 0.9f, 0.15f), new Vector3(0.1f, 0.12f, 0.1f), Gfx.Lit(new Color(0.55f, 0.55f, 0.6f)));
        Gfx.Part(r, PrimitiveType.Capsule, new Vector3(-0.33f, 0.85f, -0.15f), new Vector3(0.05f, 0.12f, 0.05f), Gfx.Lit(new Color(0.3f, 0.8f, 0.35f)), new Vector3(0f, 0f, 12f));
        Gfx.Part(r, PrimitiveType.Capsule, new Vector3(-0.28f, 0.82f, -0.12f), new Vector3(0.04f, 0.09f, 0.04f), Gfx.Lit(new Color(0.3f, 0.8f, 0.35f)), new Vector3(0f, 0f, -15f));
        // the slime himself
        s.slime = new GameObject("Slime").transform;
        s.slime.SetParent(r, false);
        Color green = new Color(0.35f, 1f, 0.5f);
        Gfx.Part(s.slime, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.3f, 0.24f, 0.3f), Gfx.Glow(green, 0.8f), default, false);
        s.eyes = new GameObject("Eyes").transform;
        s.eyes.SetParent(s.slime, false);
        foreach (float x in new[] { -0.06f, 0.06f })
        {
            Gfx.Part(s.eyes, PrimitiveType.Sphere, new Vector3(x, 0.04f, -0.13f), Vector3.one * 0.07f, Gfx.Lit(Color.white), default, false);
            Gfx.Part(s.eyes, PrimitiveType.Sphere, new Vector3(x, 0.04f, -0.16f), Vector3.one * 0.035f, Gfx.Lit(new Color(0.05f, 0.05f, 0.08f)), default, false);
        }
        Gfx.AddLight(r, new Vector3(0f, 1f, 0f), green, 2.2f, 0.8f);
        s.hopCd = 1f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;
        hopCd -= dt;
        if (hopCd <= 0f) { hop = 1f; hopCd = Random.Range(1.2f, 4f); }
        hop = Mathf.Max(0f, hop - dt * 1.8f);
        float jump = Mathf.Sin(hop * Mathf.PI) * 0.22f;
        float squish = 1f + 0.12f * Mathf.Sin(t * 3f) - jump * 0.6f;
        slime.localPosition = new Vector3(Mathf.Sin(t * 0.4f) * 0.25f, 0.83f + jump, Mathf.Sin(t * 0.3f) * 0.1f);
        slime.localScale = new Vector3(1f / Mathf.Sqrt(Mathf.Max(0.5f, squish)), squish, 1f / Mathf.Sqrt(Mathf.Max(0.5f, squish)));
        slime.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 0.5f) * 40f, 0f);
        // blink
        eyes.localScale = new Vector3(1f, Mathf.Repeat(t, 4f) < 0.12f ? 0.15f : 1f, 1f);
        // blub when you're close
        blubCd -= dt;
        Player p = Player.Local;
        if (blubCd <= 0f && p != null && Vector3.Distance(p.transform.position, transform.position) < 3f && hop > 0.9f)
        {
            blubCd = 3f;
            Sfx.At("slime_blub", transform.position + Vector3.up, 0.5f, Random.Range(0.9f, 1.2f), 10f);
        }
    }
}

// ================================================================
// Robot cat
// ================================================================
public class RobotCat : MonoBehaviour
{
    static readonly Vector2[] Spots =
    {
        new Vector2(0f, 1.2f), new Vector2(1.2f, -0.4f), new Vector2(-1.4f, 0.3f), new Vector2(-3.4f, 2.6f), new Vector2(-0.6f, 3.9f),
        new Vector2(3.0f, 4.0f), new Vector2(4.2f, 0.4f), new Vector2(2.6f, -2.2f), new Vector2(-1.0f, -2.2f), new Vector2(-3.8f, -1.0f),
    };

    Laboratory lab;
    Transform body, head, tail, earL, earR;
    readonly List<Transform> legs = new List<Transform>();
    Renderer[] eyes;
    Material eyeCalm, eyeAngry;
    Vector3 target;
    float wait, t, hissT, hissCd, warnCd, meowCd;
    float blinkOffset;                            // so two cats don't blink at the same moment
    Vector3 intruder;
    bool angry;

    public static void Make(Transform parent, Laboratory lab)
    {
        var go = new GameObject("Robot Cat");
        go.transform.SetParent(parent, false);
        var c = go.AddComponent<RobotCat>();
        c.lab = lab;
        go.transform.localPosition = new Vector3(Spots[0].x, LabExtras.FY, Spots[0].y);
        Color metal = new Color(0.72f, 0.74f, 0.78f), dark = new Color(0.2f, 0.21f, 0.24f);
        c.eyeCalm = Gfx.Glow(new Color(0.3f, 0.9f, 1f), 3f);
        c.eyeAngry = Gfx.Glow(new Color(1f, 0.15f, 0.1f), 4f);
        Transform r = go.transform;
        c.body = new GameObject("Body").transform;
        c.body.SetParent(r, false);
        c.body.localPosition = new Vector3(0f, 0.28f, 0f);
        Gfx.Part(c.body, PrimitiveType.Cube, Vector3.zero, new Vector3(0.24f, 0.2f, 0.46f), Gfx.Lit(metal, 0.7f, 0.8f));
        Gfx.Part(c.body, PrimitiveType.Cube, new Vector3(0f, 0.105f, 0f), new Vector3(0.14f, 0.01f, 0.36f), Gfx.Glow(lab.LabColor, 2f), default, false);
        // head with ears and glowing eyes
        c.head = new GameObject("Head").transform;
        c.head.SetParent(c.body, false);
        c.head.localPosition = new Vector3(0f, 0.12f, 0.27f);
        Gfx.Part(c.head, PrimitiveType.Cube, Vector3.zero, new Vector3(0.22f, 0.18f, 0.18f), Gfx.Lit(metal, 0.7f, 0.8f));
        Gfx.Part(c.head, PrimitiveType.Cube, new Vector3(0f, -0.02f, 0.092f), new Vector3(0.18f, 0.1f, 0.01f), Gfx.Lit(dark));
        c.earL = Gfx.Part(c.head, PrimitiveType.Cube, new Vector3(-0.07f, 0.11f, 0f), new Vector3(0.06f, 0.08f, 0.03f), Gfx.Lit(metal, 0.7f, 0.8f), new Vector3(0f, 0f, 20f)).transform;
        c.earR = Gfx.Part(c.head, PrimitiveType.Cube, new Vector3(0.07f, 0.11f, 0f), new Vector3(0.06f, 0.08f, 0.03f), Gfx.Lit(metal, 0.7f, 0.8f), new Vector3(0f, 0f, -20f)).transform;
        var e1 = Gfx.Part(c.head, PrimitiveType.Cube, new Vector3(-0.045f, -0.01f, 0.1f), new Vector3(0.04f, 0.025f, 0.01f), c.eyeCalm, default, false);
        var e2 = Gfx.Part(c.head, PrimitiveType.Cube, new Vector3(0.045f, -0.01f, 0.1f), new Vector3(0.04f, 0.025f, 0.01f), c.eyeCalm, default, false);
        c.eyes = new[] { e1.GetComponent<Renderer>(), e2.GetComponent<Renderer>() };
        // whiskers (antennas)
        Gfx.Part(c.head, PrimitiveType.Cube, new Vector3(-0.13f, -0.03f, 0.08f), new Vector3(0.1f, 0.006f, 0.006f), Gfx.Lit(dark), new Vector3(0f, 15f, 10f), false);
        Gfx.Part(c.head, PrimitiveType.Cube, new Vector3(0.13f, -0.03f, 0.08f), new Vector3(0.1f, 0.006f, 0.006f), Gfx.Lit(dark), new Vector3(0f, -15f, -10f), false);
        // tail (bends up)
        c.tail = new GameObject("Tail").transform;
        c.tail.SetParent(c.body, false);
        c.tail.localPosition = new Vector3(0f, 0.06f, -0.23f);
        Gfx.Part(c.tail, PrimitiveType.Cylinder, new Vector3(0f, 0.14f, -0.04f), new Vector3(0.035f, 0.14f, 0.035f), Gfx.Lit(metal, 0.7f, 0.8f), new Vector3(-20f, 0f, 0f));
        Gfx.Part(c.tail, PrimitiveType.Sphere, new Vector3(0f, 0.28f, -0.09f), Vector3.one * 0.06f, Gfx.Glow(lab.LabColor, 2f), default, false);
        // legs
        foreach (Vector2 l in new[] { new Vector2(-0.08f, 0.16f), new Vector2(0.08f, 0.16f), new Vector2(-0.08f, -0.16f), new Vector2(0.08f, -0.16f) })
        {
            Transform leg = new GameObject("Leg").transform;
            leg.SetParent(c.body, false);
            leg.localPosition = new Vector3(l.x, -0.08f, l.y);
            Gfx.Part(leg, PrimitiveType.Cylinder, new Vector3(0f, -0.1f, 0f), new Vector3(0.05f, 0.1f, 0.05f), Gfx.Lit(dark, 0.6f, 0.5f));
            c.legs.Add(leg);
        }
        c.PickSpot();
        c.wait = Random.Range(0.5f, 2f);
        c.blinkOffset = Random.value * 3.5f;
    }

    void PickSpot()
    {
        Vector2 s = Spots[Random.Range(0, Spots.Length)];
        target = new Vector3(s.x + Random.Range(-0.4f, 0.4f), LabExtras.FY, s.y + Random.Range(-0.4f, 0.4f));
    }

    // Someone who doesn't belong in this lab?
    bool FindIntruder(out Vector3 at, out bool isLocal)
    {
        at = Vector3.zero;
        isLocal = false;
        Player me = Player.Local;
        if (me != null && me.Alive && me.labIndex != lab.Index && LabExtras.Inside(lab, me.transform.position)) { at = me.transform.position; isLocal = true; return true; }
        if (lab.OwnedByPlayer)
        {
            bool found = false;
            Vector3 f = Vector3.zero;
            NetPlayers.ForEach((slot, pos, dead) =>
            {
                if (!found && !dead && slot != lab.Index && LabExtras.Inside(lab, pos)) { found = true; f = pos; }
            });
            at = f;
            return found;
        }
        return false;
    }

    void Update()
    {
        if (lab == null || lab.Destroyed) return;
        float dt = Time.deltaTime;
        t += dt;
        hissCd -= dt; warnCd -= dt; meowCd -= dt;

        angry = FindIntruder(out intruder, out bool isLocal);
        Vector3 pos = transform.localPosition;
        float speed = 0f;
        if (angry)
        {
            // face the intruder, arch up, hiss
            Vector3 to = lab.transform.InverseTransformPoint(intruder) - pos;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f) transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.LookRotation(to), dt * 8f);
            if (hissCd <= 0f)
            {
                hissCd = 2.5f;
                hissT = 1f;
                Sfx.At("cat_hiss", transform.position + Vector3.up * 0.4f, 0.9f, Random.Range(0.9f, 1.1f), 18f);
            }
            if (lab.OwnedByPlayer && !isLocal && warnCd <= 0f)
            {
                warnCd = 8f;
                GameHUD.Warn("YOUR ROBOT CAT IS HISSING - SOMEONE IS IN YOUR LAB!");
            }
        }
        else
        {
            // wander from spot to spot, sit and wait a bit
            Vector3 to = target - pos;
            to.y = 0f;
            if (wait > 0f) wait -= dt;
            else if (to.magnitude < 0.15f) { wait = Random.Range(1.5f, 5f); PickSpot(); }
            else
            {
                speed = 1.1f;
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.LookRotation(to), dt * 6f);
                pos += transform.localRotation * Vector3.forward * speed * dt;
                pos.y = LabExtras.FY;
                transform.localPosition = pos;
            }
            // the occasional beep-meow when you're near
            Player p = Player.Local;
            if (meowCd <= 0f && p != null && Vector3.Distance(p.transform.position, transform.position) < 4f && Random.value < dt * 0.4f)
            {
                meowCd = 6f;
                Sfx.At(Random.value < 0.5f ? "cat_meow" : "cat_beep", transform.position + Vector3.up * 0.4f, 0.6f, Random.Range(1.1f, 1.4f), 12f);
            }
        }

        // animation: legs, tail, ears, eyes, arched back
        hissT = Mathf.Max(0f, hissT - dt);
        float walk = speed > 0f ? Mathf.Sin(t * 14f) : 0f;
        for (int i = 0; i < legs.Count; i++)
            legs[i].localRotation = Quaternion.Euler(walk * 30f * ((i == 0 || i == 3) ? 1f : -1f), 0f, 0f);
        float arch = angry ? 1f : 0f;
        body.localPosition = new Vector3(0f, 0.28f + arch * 0.05f + (speed > 0f ? Mathf.Abs(walk) * 0.015f : 0f), 0f);
        body.localScale = new Vector3(1f, 1f + arch * 0.15f, 1f);
        tail.localRotation = Quaternion.Euler(angry ? -10f : -30f + Mathf.Sin(t * 2f) * 10f, Mathf.Sin(t * (angry ? 20f : 3f)) * (angry ? 8f : 25f), 0f);
        float earBack = angry ? 45f : 0f;
        earL.localRotation = Quaternion.Euler(-earBack, 0f, 20f);
        earR.localRotation = Quaternion.Euler(-earBack, 0f, -20f);
        head.localRotation = Quaternion.Euler(angry ? -10f : Mathf.Sin(t * 0.7f) * 8f, wait > 0f && !angry ? Mathf.Sin(t * 0.9f) * 30f : 0f, 0f);
        Material em = angry ? eyeAngry : eyeCalm;
        foreach (Renderer r in eyes) if (r != null && r.sharedMaterial != em) r.sharedMaterial = em;
        // blink
        float blink = !angry && Mathf.Repeat(t + blinkOffset, 3.5f) < 0.1f ? 0.2f : 1f;
        foreach (Renderer r in eyes) if (r != null) r.transform.localScale = new Vector3(0.04f, 0.025f * blink, 0.01f);
    }
}

// ================================================================
// Lab levels (your lab only)
// ================================================================
public class LabLevels : MonoBehaviour
{
    Laboratory lab;
    int built = 0;
    float checkCd;
    Transform atom;
    readonly List<Transform> coils = new List<Transform>();
    float t;

    public void Init(Laboratory l)
    {
        lab = l;
        int lv = LabExtras.Level(LabExtras.DiscoveredCount());
        for (int i = 1; i <= lv; i++) BuildLevel(i);
    }

    void BuildLevel(int lv)
    {
        built = lv;
        Transform r = transform;
        float fy = LabExtras.FY;
        Color c = lab.LabColor;
        switch (lv)
        {
            case 1:     // neon strips along the top of the inside walls
                Material neon = Gfx.Glow(c, 3f);
                Gfx.Part(r, PrimitiveType.Cube, new Vector3(0f, fy + 4.25f, -5.55f), new Vector3(12.8f, 0.05f, 0.05f), neon, default, false);
                Gfx.Part(r, PrimitiveType.Cube, new Vector3(0f, fy + 4.25f, 5.55f), new Vector3(12.8f, 0.05f, 0.05f), neon, default, false);
                Gfx.Part(r, PrimitiveType.Cube, new Vector3(-6.55f, fy + 4.25f, 0f), new Vector3(0.05f, 0.05f, 10.8f), neon, default, false);
                Gfx.Part(r, PrimitiveType.Cube, new Vector3(6.55f, fy + 4.25f, 0f), new Vector3(0.05f, 0.05f, 10.8f), neon, default, false);
                break;
            case 2:     // MAD LAB neon sign above the door (inside)
                {
                    var tex = Resources.Load<Texture2D>("Decor/neon_madlab");
                    Transform s = new GameObject("Neon Sign").transform;
                    s.SetParent(r, false);
                    s.localPosition = new Vector3(0f, fy + 3.85f, 5.55f);
                    Gfx.Part(s, PrimitiveType.Cube, new Vector3(0f, 0f, 0.02f), new Vector3(2.6f, 0.7f, 0.04f), Gfx.Lit(new Color(0.06f, 0.06f, 0.08f)));
                    if (tex != null)
                    {
                        var m = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex };
                        Gfx.Part(s, PrimitiveType.Quad, Vector3.zero, new Vector3(2.5f, 0.62f, 1f), m, default, false);
                    }
                    Gfx.AddLight(s, new Vector3(0f, 0f, -0.6f), new Color(1f, 0.3f, 0.8f), 4f, 1.2f);
                    break;
                }
            case 3:     // the generator in the back-right corner
                {
                    Transform g = new GameObject("Generator").transform;
                    g.SetParent(r, false);
                    g.localPosition = new Vector3(6.05f, fy, -2.8f);
                    Color metal = new Color(0.35f, 0.37f, 0.42f);
                    Gfx.Part(g, PrimitiveType.Cube, new Vector3(0f, 0.25f, 0f), new Vector3(0.75f, 0.5f, 0.75f), Gfx.Lit(metal, 0.5f, 0.7f));
                    Gfx.Part(g, PrimitiveType.Cube, new Vector3(0f, 0.2f, -0.38f), new Vector3(0.5f, 0.06f, 0.01f), Gfx.Glow(new Color(1f, 0.85f, 0.2f), 2f), default, false);
                    Gfx.Part(g, PrimitiveType.Cylinder, new Vector3(0f, 1.0f, 0f), new Vector3(0.14f, 0.5f, 0.14f), Gfx.Lit(new Color(0.7f, 0.45f, 0.25f), 0.7f, 0.9f));
                    for (int i = 0; i < 3; i++)
                    {
                        Transform coil = Gfx.Part(g, PrimitiveType.Cylinder, new Vector3(0f, 0.75f + i * 0.28f, 0f), new Vector3(0.5f - i * 0.08f, 0.025f, 0.5f - i * 0.08f), Gfx.Glow(c, 2.5f), default, false).transform;
                        coils.Add(coil);
                    }
                    Gfx.Part(g, PrimitiveType.Sphere, new Vector3(0f, 1.6f, 0f), Vector3.one * 0.22f, Gfx.Glow(new Color(0.6f, 0.9f, 1f), 3f), default, false);
                    Gfx.AddLight(g, new Vector3(0f, 1.4f, 0f), c, 4f, 1.4f);
                    Sfx.Loop("drone_hum", g, 0.12f, 6f);
                    break;
                }
            case 4:     // floor neon + brighter coloured ceiling lights
                {
                    Material floor = Gfx.Glow(c, 2f);
                    Gfx.Part(r, PrimitiveType.Cube, new Vector3(0f, fy + 0.03f, -5.35f), new Vector3(12.6f, 0.02f, 0.06f), floor, default, false);
                    Gfx.Part(r, PrimitiveType.Cube, new Vector3(-6.35f, fy + 0.03f, 0f), new Vector3(0.06f, 0.02f, 10.6f), floor, default, false);
                    Gfx.Part(r, PrimitiveType.Cube, new Vector3(6.35f, fy + 0.03f, 0f), new Vector3(0.06f, 0.02f, 10.6f), floor, default, false);
                    Gfx.Part(r, PrimitiveType.Cube, new Vector3(-4.2f, fy + 0.03f, 5.35f), new Vector3(4.4f, 0.02f, 0.06f), floor, default, false);
                    Gfx.Part(r, PrimitiveType.Cube, new Vector3(4.2f, fy + 0.03f, 5.35f), new Vector3(4.4f, 0.02f, 0.06f), floor, default, false);
                    foreach (Light l in lab.GetComponentsInChildren<Light>())
                        if (l != null && l.name == "Lamp") { l.intensity *= 1.3f; l.color = Color.Lerp(l.color, c, 0.25f); }
                    break;
                }
            case 5:     // hologram atom over the lab table
                {
                    atom = new GameObject("Hologram Atom").transform;
                    atom.SetParent(r, false);
                    atom.localPosition = new Vector3(2.3f, fy + 1.75f, 2.4f);
                    Color holo = new Color(0.4f, 0.9f, 1f);
                    Gfx.Part(atom, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.14f, Gfx.Glow(holo, 3f), default, false);
                    for (int i = 0; i < 3; i++)
                    {
                        Transform orbit = Gfx.Pivot(atom, Vector3.zero, "Orbit");
                        orbit.localRotation = Quaternion.Euler(i * 60f, i * 40f, 70f);
                        Gfx.Part(orbit, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.7f, 0.002f, 0.7f), Gfx.Clear(new Color(holo.r, holo.g, holo.b, 0.18f)), default, false);
                        Gfx.Part(orbit, PrimitiveType.Sphere, new Vector3(0.35f, 0f, 0f), Vector3.one * 0.06f, Gfx.Glow(Color.white, 3f), default, false);
                    }
                    // the projector on the table
                    Gfx.Part(r, PrimitiveType.Cylinder, new Vector3(2.3f, fy + 1.03f, 2.4f), new Vector3(0.22f, 0.03f, 0.22f), Gfx.Lit(new Color(0.2f, 0.2f, 0.25f), 0.6f, 0.6f));
                    Gfx.Part(r, PrimitiveType.Cylinder, new Vector3(2.3f, fy + 1.35f, 2.4f), new Vector3(0.3f, 0.3f, 0.3f), Gfx.Clear(new Color(holo.r, holo.g, holo.b, 0.06f)), default, false);
                    break;
                }
        }
        Gfx.SetLayer(gameObject, 2);
    }

    void Update()
    {
        t += Time.deltaTime;
        for (int i = 0; i < coils.Count; i++)
            if (coils[i] != null) coils[i].localRotation = Quaternion.Euler(0f, t * (90f + i * 60f), Mathf.Sin(t * 2f + i) * 8f);
        if (atom != null)
        {
            atom.localRotation = Quaternion.Euler(0f, t * 40f, 0f);
            int k = 0;
            foreach (Transform o in atom)
            {
                if (o.name != "Orbit") continue;
                o.localRotation *= Quaternion.Euler(0f, Time.deltaTime * (120f + k * 50f), 0f);
                k++;
            }
            atom.localPosition = new Vector3(2.3f, LabExtras.FY + 1.75f + Mathf.Sin(t * 1.5f) * 0.05f, 2.4f);
        }
        checkCd -= Time.deltaTime;
        if (checkCd > 0f) return;
        checkCd = 2f;
        int lv = LabExtras.Level(LabExtras.DiscoveredCount());
        while (built < lv)
        {
            BuildLevel(built + 1);
            GameHUD.Banner("LAB LEVEL " + built + "!", LabExtras.LevelReward[built], lab.LabColor);
            Sfx.Play("upgrade", 0.7f, 1.2f);
        }
    }
}

// ================================================================
// The lab radio
// ================================================================
public class LabRadio : MonoBehaviour, IInteractable
{
    static readonly string[] Songs = { "radio_funk", "radio_chill", "radio_spooky", "radio_snow" };
    static readonly string[] SongNames = { "LAB FUNK", "BUBBLE CHILL", "SPOOKY LAB", "SNOW DAY" };

    AudioSource src;
    int song;                  // -1 = off
    Transform dial, needle;
    Light glow;
    float t;

    public Vector3 InteractPoint => transform.position;
    public float InteractRange => 3f;

    void OnEnable() => Interactables.All.Add(this);
    void OnDisable() => Interactables.All.Remove(this);

    public static void Make(Transform parent, Laboratory lab)
    {
        var go = new GameObject("Lab Radio");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(5.2f, LabExtras.FY + 2.0f, 5.2f);
        go.transform.localRotation = Quaternion.Euler(0f, 180f + 12f, 0f);
        var r = go.AddComponent<LabRadio>();
        Transform t = go.transform;
        Color body = new Color(0.65f, 0.22f, 0.18f), cream = new Color(0.95f, 0.9f, 0.78f);
        Gfx.Part(t, PrimitiveType.Cube, new Vector3(0f, 0.17f, 0f), new Vector3(0.56f, 0.32f, 0.22f), Gfx.Lit(body, 0.5f));
        Gfx.Part(t, PrimitiveType.Cube, new Vector3(-0.12f, 0.17f, 0.112f), new Vector3(0.26f, 0.24f, 0.01f), Gfx.Lit(new Color(0.15f, 0.12f, 0.1f)));        // speaker
        for (int i = 0; i < 5; i++)
            Gfx.Part(t, PrimitiveType.Cube, new Vector3(-0.12f, 0.08f + i * 0.045f, 0.118f), new Vector3(0.24f, 0.012f, 0.005f), Gfx.Lit(cream), default, false);
        r.dial = Gfx.Part(t, PrimitiveType.Cube, new Vector3(0.14f, 0.22f, 0.112f), new Vector3(0.2f, 0.08f, 0.01f), Gfx.Glow(new Color(1f, 0.75f, 0.3f), 1.5f), default, false).transform;
        r.needle = Gfx.Part(t, PrimitiveType.Cube, new Vector3(0.1f, 0.22f, 0.118f), new Vector3(0.008f, 0.07f, 0.005f), Gfx.Lit(new Color(0.8f, 0.1f, 0.1f)), default, false).transform;
        foreach (float x in new[] { 0.09f, 0.19f })
            Gfx.Part(t, PrimitiveType.Cylinder, new Vector3(x, 0.1f, 0.12f), new Vector3(0.05f, 0.015f, 0.05f), Gfx.Lit(cream), new Vector3(90f, 0f, 0f));
        Gfx.Part(t, PrimitiveType.Cylinder, new Vector3(0.22f, 0.5f, -0.05f), new Vector3(0.01f, 0.2f, 0.01f), Gfx.Lit(new Color(0.75f, 0.75f, 0.8f), 0.8f, 1f), new Vector3(0f, 0f, -15f));    // antenna
        Gfx.Part(t, PrimitiveType.Cube, new Vector3(0f, 0.35f, 0f), new Vector3(0.3f, 0.02f, 0.03f), Gfx.Lit(new Color(0.2f, 0.2f, 0.2f)));         // handle
        r.glow = Gfx.AddLight(t, new Vector3(0f, 0.2f, 0.4f), new Color(1f, 0.7f, 0.35f), 2f, 0.6f);
        // music that comes from the radio (louder the closer you are)
        r.src = go.AddComponent<AudioSource>();
        r.src.loop = true;
        r.src.spatialBlend = 1f;
        r.src.minDistance = 1.5f;
        r.src.maxDistance = 16f;
        r.src.rolloffMode = AudioRolloffMode.Linear;
        r.src.dopplerLevel = 0f;
        r.src.playOnAwake = false;
        r.song = Holiday.Current == Holiday.Theme.Halloween ? 2 : Holiday.Current == Holiday.Theme.Winter ? 3 : 0;
        r.PlaySong();
    }

    void PlaySong()
    {
        if (song < 0) { src.Stop(); return; }
        src.clip = Sfx.Clip(Songs[song]);
        if (src.clip != null) src.Play();
    }

    public string Prompt(Player p, out bool usable)
    {
        usable = true;
        return "Radio: " + (song < 0 ? "OFF" : SongNames[song]) + "   (change song)";
    }

    public void Use(Player p)
    {
        song++;
        if (song >= Songs.Length) song = -1;
        Sfx.Play("radio_static", 0.5f);
        PlaySong();
        GameHUD.Toast(song < 0 ? "Radio off." : "Now playing: " + SongNames[song], new Color(1f, 0.75f, 0.35f));
    }

    void Update()
    {
        t += Time.deltaTime;
        if (src != null) src.volume = 0.55f * Settings.Music;
        bool on = song >= 0;
        if (glow != null) glow.enabled = on;
        if (needle != null) needle.localPosition = new Vector3(0.06f + (on ? song : 0) * 0.04f, 0.22f, 0.118f);
        // bounce a little to the beat
        transform.localScale = Vector3.one * (on ? 1f + Mathf.Max(0f, Mathf.Sin(t * 11.7f)) * 0.025f : 1f);
    }
}

// ================================================================
// Security camera screen
// ================================================================
public class SecurityScreen : MonoBehaviour
{
    Laboratory lab;
    Camera cam;
    RenderTexture rt;
    Transform cam1, cam2;
    int feed;
    float switchCd = 6f, renderCd;
    bool renderedThisFrame;
    Renderer rec;
    float t;

    public static void Make(Transform parent, Laboratory lab)
    {
        var go = new GameObject("Security Screen");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, LabExtras.FY + 3.45f, 1.6f);
        var s = go.AddComponent<SecurityScreen>();
        s.lab = lab;
        Transform r = go.transform;
        Color dark = new Color(0.08f, 0.08f, 0.1f);
        // the TV hanging from the ceiling (a picture on both sides)
        Gfx.Part(r, PrimitiveType.Cube, Vector3.zero, new Vector3(1.36f, 0.82f, 0.1f), Gfx.Lit(dark, 0.5f));
        foreach (float x in new[] { -0.45f, 0.45f })
            Gfx.Part(r, PrimitiveType.Cylinder, new Vector3(x, 0.75f, 0f), new Vector3(0.025f, 0.35f, 0.025f), Gfx.Lit(new Color(0.4f, 0.4f, 0.45f), 0.6f, 0.8f));
        s.rt = new RenderTexture(320, 180, 16) { name = "Security Camera" };
        var mat = new Material(Shader.Find("Sprites/Default")) { mainTexture = s.rt };
        Gfx.Part(r, PrimitiveType.Quad, new Vector3(0f, 0f, -0.052f), new Vector3(1.24f, 0.7f, 1f), mat, default, false);
        Gfx.Part(r, PrimitiveType.Quad, new Vector3(0f, 0f, 0.052f), new Vector3(1.24f, 0.7f, 1f), mat, new Vector3(0f, 180f, 0f), false);
        s.rec = Gfx.Part(r, PrimitiveType.Sphere, new Vector3(0.58f, 0.33f, -0.06f), Vector3.one * 0.04f, Gfx.Glow(new Color(1f, 0.1f, 0.1f), 3f), default, false).GetComponent<Renderer>();
        Gfx.Part(r, PrimitiveType.Sphere, new Vector3(0.58f, 0.33f, 0.06f), Vector3.one * 0.04f, Gfx.Glow(new Color(1f, 0.1f, 0.1f), 3f), default, false);

        // the two cameras (little boxes with a lens): above the door outside, and in the corner inside
        s.cam1 = MakeCamBox(lab.transform, new Vector3(1.2f, LabExtras.FY + 3.9f, 6.15f), new Vector3(15f, 0f, 0f));
        s.cam2 = MakeCamBox(lab.transform, new Vector3(5.9f, LabExtras.FY + 3.9f, 4.9f), Quaternion.LookRotation(new Vector3(-5.9f, -2.6f, 0.6f)).eulerAngles);
        s.cam1.SetParent(r, true);
        s.cam2.SetParent(r, true);

        var cgo = new GameObject("Security Camera");
        cgo.transform.SetParent(r, false);
        s.cam = cgo.AddComponent<Camera>();
        s.cam.targetTexture = s.rt;
        s.cam.fieldOfView = 70f;
        s.cam.nearClipPlane = 0.2f;
        s.cam.farClipPlane = 90f;
        s.cam.allowHDR = false;
        s.cam.allowMSAA = false;
        s.cam.depth = -10f;
        s.cam.enabled = false;
        s.Place();
    }

    static Transform MakeCamBox(Transform lab, Vector3 local, Vector3 euler)
    {
        Transform c = new GameObject("CCTV").transform;
        c.SetParent(lab, false);
        c.localPosition = local;
        c.localRotation = Quaternion.Euler(euler);
        Gfx.Part(c, PrimitiveType.Cube, new Vector3(0f, 0f, -0.1f), new Vector3(0.16f, 0.14f, 0.3f), Gfx.Lit(new Color(0.85f, 0.85f, 0.88f), 0.5f));
        Gfx.Part(c, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.06f), new Vector3(0.1f, 0.03f, 0.1f), Gfx.Lit(new Color(0.05f, 0.05f, 0.08f), 0.9f), new Vector3(90f, 0f, 0f));
        Gfx.Part(c, PrimitiveType.Sphere, new Vector3(0.06f, 0.06f, 0f), Vector3.one * 0.03f, Gfx.Glow(new Color(1f, 0.1f, 0.1f), 3f), default, false);
        Gfx.SetLayer(c.gameObject, 2);
        return c;
    }

    void Place()
    {
        Transform from = feed == 0 ? cam1 : cam2;
        if (from == null || cam == null) return;
        cam.transform.position = from.position + from.forward * 0.25f;
        cam.transform.rotation = from.rotation;
    }

    void Update()
    {
        if (cam == null) return;
        t += Time.deltaTime;
        if (rec != null) rec.enabled = Mathf.Repeat(t, 1f) < 0.6f;
        // switch views every few seconds
        switchCd -= Time.deltaTime;
        if (switchCd <= 0f) { switchCd = 6f; feed = 1 - feed; Place(); }
        // it only films 10 times a second (an old CCTV - and much cheaper to draw)
        if (renderedThisFrame) { cam.enabled = false; renderedThisFrame = false; }
        renderCd -= Time.deltaTime;
        if (renderCd <= 0f && lab != null && !lab.Destroyed)
        {
            renderCd = 0.1f;
            cam.enabled = true;
            renderedThisFrame = true;
        }
    }

    void OnDestroy()
    {
        if (rt != null) { rt.Release(); Destroy(rt); }
    }
}
