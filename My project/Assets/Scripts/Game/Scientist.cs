using UnityEngine;

// THE SCIENTIST: the player model, built from simple shapes, with all its animations done in code.
//   - lab coat (the tails flap when you run), goggles, rubber gloves, boots, a glowing chem tank on the back
//   - idle breathing, walk, run, sprint, strafing (legs turn toward where you're going), jumping, landing
//   - holding weapons properly: the gun points where you aim and both hands stay on it (arm IK)
//   - aiming down the barrel, recoil, sprinting with the gun across the chest
//   - drinking potions, throwing bombs, gathering materials, working at a bench, getting hurt, falling over
// The colour of the gloves / tie / tank matches your lab's colour.
public class Scientist : MonoBehaviour
{
    public enum Hold { None, Rifle, Heavy, Flask }

    // ---- set by whoever controls this scientist, every frame ----
    [HideInInspector] public Vector3 velocity;        // world velocity
    [HideInInspector] public bool grounded = true;
    [HideInInspector] public float aimPitch;          // degrees, + = looking up
    [HideInInspector] public float aimYaw;            // world yaw the body faces
    [HideInInspector] public bool aiming, sprinting;
    [HideInInspector] public bool dead;

    public Hold hold { get; private set; }
    public Transform Muzzle { get; private set; }
    public event System.Action Step;                    // a foot hit the ground

    // body parts
    Transform body, pelvis, hipL, hipR, kneeL, kneeR, spine, chest, head, armL, armR, elbowL, elbowR, gunRoot;
    Transform[] flaps;
    Transform tankGlow;
    GameObject armorRoot, bootsRoot;
    Vector3 gripR, gripL;
    bool twoHanded;

    // animation state
    float phase, twist, lean, sideLean, landDip, flinch, deadK, recoil, breath, prevPhaseSin;
    float drinkT = -1f, throwT = -1f, gatherT = -1f, workT = -1f, workLen, pokeT = -1f;
    bool wasGrounded = true;
    float airTime;
    Vector3 gunPos;
    Quaternion gunRot = Quaternion.identity;
    bool slung;

    const float Upper = 0.31f, Lower = 0.31f;

    static Color Skin(int seed)
    {
        Color[] skins = { new Color(0.96f, 0.8f, 0.69f), new Color(0.87f, 0.68f, 0.52f), new Color(0.71f, 0.5f, 0.36f), new Color(0.5f, 0.34f, 0.24f), new Color(0.98f, 0.86f, 0.76f) };
        return skins[seed % skins.Length];
    }

    static Color Hair(int seed)
    {
        Color[] hairs = { new Color(0.92f, 0.92f, 0.9f), new Color(0.15f, 0.1f, 0.08f), new Color(0.55f, 0.3f, 0.12f), new Color(0.75f, 0.75f, 0.75f), new Color(0.95f, 0.75f, 0.3f), new Color(0.9f, 0.3f, 0.2f) };
        return hairs[seed % hairs.Length];
    }

    public static Scientist Create(Transform parent, Color accent, int seed)
    {
        var go = new GameObject("Scientist");
        go.transform.SetParent(parent, false);
        var s = go.AddComponent<Scientist>();
        s.Build(accent, seed);
        return s;
    }

    // ================================================================
    // Building the model
    // ================================================================

    void Build(Color accent, int seed)
    {
        const PrimitiveType Box = PrimitiveType.Cube, Ball = PrimitiveType.Sphere, Cap = PrimitiveType.Capsule, Cyl = PrimitiveType.Cylinder;
        Color skin = Skin(seed), hair = Hair(seed / 5 + seed);
        Material coat = Gfx.Lit(new Color(0.95f, 0.96f, 0.97f), 0.25f);
        Material coatShade = Gfx.Lit(new Color(0.82f, 0.85f, 0.88f), 0.2f);
        Material pants = Gfx.Lit(new Color(0.18f, 0.2f, 0.26f), 0.15f);
        Material bootM = Gfx.Lit(new Color(0.1f, 0.09f, 0.09f), 0.35f);
        Material sole = Gfx.Lit(new Color(0.25f, 0.24f, 0.22f), 0.1f);
        Material glove = Gfx.Lit(Color.Lerp(accent, new Color(0.2f, 0.2f, 0.2f), 0.15f), 0.65f);
        Material skinM = Gfx.Lit(skin, 0.3f);
        Material hairM = Gfx.Lit(hair, 0.2f);
        Material dark = Gfx.Lit(new Color(0.08f, 0.08f, 0.09f), 0.4f);
        Material metal = Gfx.Lit(new Color(0.6f, 0.62f, 0.65f), 0.7f, 0.8f);
        Material brass = Gfx.Lit(new Color(0.8f, 0.6f, 0.25f), 0.7f, 0.8f);
        Material shirt = Gfx.Lit(Color.Lerp(accent, Color.white, 0.55f), 0.2f);
        Material tie = Gfx.Lit(Color.Lerp(accent, Color.black, 0.35f), 0.3f);
        Material glowAcc = Gfx.Glow(accent, 2.2f);
        Material lens = Gfx.Glow(Color.Lerp(accent, new Color(0.6f, 0.9f, 1f), 0.6f), 0.8f);
        Material eye = Gfx.Lit(new Color(0.1f, 0.08f, 0.06f), 0.8f);

        body = Gfx.Pivot(transform, Vector3.zero, "Body");

        // ---------- legs ----------
        pelvis = Gfx.Pivot(body, new Vector3(0f, 0.95f, 0f), "Pelvis");
        hipL = Gfx.Pivot(pelvis, new Vector3(-0.1f, 0f, 0f), "HipL");
        hipR = Gfx.Pivot(pelvis, new Vector3(0.1f, 0f, 0f), "HipR");
        for (int i = 0; i < 2; i++)
        {
            Transform hip = i == 0 ? hipL : hipR;
            Gfx.Part(hip, Cap, new Vector3(0f, -0.21f, 0f), new Vector3(0.165f, 0.24f, 0.175f), pants);
            Transform knee = Gfx.Pivot(hip, new Vector3(0f, -0.44f, 0f), "Knee");
            Gfx.Part(knee, Ball, new Vector3(0f, 0f, 0.01f), new Vector3(0.14f, 0.12f, 0.15f), pants);
            Gfx.Part(knee, Cap, new Vector3(0f, -0.2f, 0f), new Vector3(0.135f, 0.22f, 0.14f), pants);
            Gfx.Part(knee, Cyl, new Vector3(0f, -0.3f, 0f), new Vector3(0.145f, 0.07f, 0.15f), bootM);                 // boot top
            Gfx.Part(knee, Cap, new Vector3(0f, -0.41f, 0.045f), new Vector3(0.13f, 0.14f, 0.11f), bootM, new Vector3(90f, 0f, 0f));
            Gfx.Part(knee, Box, new Vector3(0f, -0.47f, 0.045f), new Vector3(0.13f, 0.035f, 0.29f), sole);
            if (i == 0) kneeL = knee; else kneeR = knee;
        }

        // ---------- body ----------
        spine = Gfx.Pivot(body, new Vector3(0f, 0.97f, 0f), "Spine");
        Gfx.Part(spine, Cyl, new Vector3(0f, 0f, 0f), new Vector3(0.34f, 0.05f, 0.23f), dark);                          // belt
        Gfx.Part(spine, Box, new Vector3(0f, 0f, 0.115f), new Vector3(0.07f, 0.05f, 0.02f), brass);                    // buckle
        Gfx.Part(spine, Box, new Vector3(0f, 0.14f, 0f), new Vector3(0.36f, 0.26f, 0.23f), coat);                      // coat waist
        Gfx.Part(spine, Box, new Vector3(0f, 0.12f, 0.112f), new Vector3(0.1f, 0.22f, 0.012f), shirt);                // shirt showing between the coat
        for (int s = -1; s <= 1; s += 2)
            for (int b = 0; b < 2; b++)
                Gfx.Part(spine, Ball, new Vector3(s * 0.065f, 0.07f + b * 0.1f, 0.118f), Vector3.one * 0.018f, dark);   // buttons

        // coat tails: four panels hanging from the waist (they swing)
        flaps = new Transform[4];
        Vector3[] flapPos = { new Vector3(-0.1f, 0f, 0.1f), new Vector3(0.1f, 0f, 0.1f), new Vector3(-0.1f, 0f, -0.1f), new Vector3(0.1f, 0f, -0.1f) };
        for (int i = 0; i < 4; i++)
        {
            flaps[i] = Gfx.Pivot(spine, flapPos[i], "CoatTail");
            Gfx.Part(flaps[i], Box, new Vector3(0f, -0.25f, 0f), new Vector3(0.19f, 0.5f, 0.03f), i < 2 ? coat : coatShade);
            Gfx.Part(flaps[i], Box, new Vector3(0f, -0.49f, 0f), new Vector3(0.19f, 0.025f, 0.035f), coatShade);       // hem
        }

        chest = Gfx.Pivot(spine, new Vector3(0f, 0.25f, 0f), "Chest");
        Gfx.Part(chest, Box, new Vector3(0f, 0.11f, 0f), new Vector3(0.4f, 0.28f, 0.25f), coat);
        Gfx.Part(chest, Cap, new Vector3(0f, 0.24f, 0f), new Vector3(0.2f, 0.22f, 0.24f), coat, new Vector3(0f, 0f, 90f));   // shoulders
        Gfx.Part(chest, Box, new Vector3(0f, 0.15f, 0.126f), new Vector3(0.11f, 0.22f, 0.012f), shirt);              // shirt V
        Gfx.Part(chest, Box, new Vector3(0f, 0.13f, 0.134f), new Vector3(0.04f, 0.2f, 0.012f), tie);                 // tie
        Gfx.Part(chest, Box, new Vector3(0f, 0.24f, 0.134f), new Vector3(0.05f, 0.03f, 0.014f), tie);                // tie knot
        Gfx.Part(chest, Box, new Vector3(-0.07f, 0.17f, 0.128f), new Vector3(0.03f, 0.24f, 0.01f), coatShade, new Vector3(0f, 0f, -12f));   // lapels
        Gfx.Part(chest, Box, new Vector3(0.07f, 0.17f, 0.128f), new Vector3(0.03f, 0.24f, 0.01f), coatShade, new Vector3(0f, 0f, 12f));
        Gfx.Part(chest, Box, new Vector3(-0.12f, 0.07f, 0.128f), new Vector3(0.1f, 0.08f, 0.008f), coatShade);       // pocket
        Gfx.Part(chest, Cyl, new Vector3(-0.145f, 0.12f, 0.13f), new Vector3(0.012f, 0.035f, 0.012f), Gfx.Lit(new Color(0.9f, 0.2f, 0.2f)));   // pens
        Gfx.Part(chest, Cyl, new Vector3(-0.12f, 0.12f, 0.13f), new Vector3(0.012f, 0.035f, 0.012f), Gfx.Lit(new Color(0.2f, 0.4f, 0.9f)));
        Gfx.Part(chest, Box, new Vector3(0.12f, 0.12f, 0.13f), new Vector3(0.07f, 0.05f, 0.008f), Gfx.Lit(Color.white, 0.6f));   // name badge
        Gfx.Part(chest, Box, new Vector3(0.12f, 0.135f, 0.134f), new Vector3(0.07f, 0.015f, 0.004f), glowAcc);
        Gfx.Part(chest, Cyl, new Vector3(0f, 0.33f, 0f), new Vector3(0.1f, 0.05f, 0.1f), skinM);                     // neck
        Gfx.Part(chest, Cyl, new Vector3(0f, 0.3f, -0.01f), new Vector3(0.17f, 0.03f, 0.15f), coat);                 // collar

        // chem tank on the back
        Transform tank = Gfx.Pivot(chest, new Vector3(0f, 0.12f, -0.2f), "Tank");
        Gfx.Part(tank, Cyl, Vector3.zero, new Vector3(0.17f, 0.15f, 0.17f), metal);
        Gfx.Part(tank, Cyl, new Vector3(0f, 0f, -0.01f), new Vector3(0.155f, 0.13f, 0.155f), Gfx.Clear(new Color(accent.r, accent.g, accent.b, 0.45f)), default, false);
        tankGlow = Gfx.Part(tank, Cyl, new Vector3(0f, -0.02f, -0.01f), new Vector3(0.12f, 0.1f, 0.12f), glowAcc).transform;
        Gfx.Part(tank, Cyl, new Vector3(0f, 0.16f, 0f), new Vector3(0.12f, 0.02f, 0.12f), brass);
        Gfx.Part(tank, Cyl, new Vector3(0f, -0.16f, 0f), new Vector3(0.12f, 0.02f, 0.12f), brass);
        Gfx.Part(tank, Cyl, new Vector3(0.06f, 0.2f, 0.05f), new Vector3(0.025f, 0.08f, 0.025f), metal, new Vector3(-30f, 0f, 0f));   // hose
        for (int s = -1; s <= 1; s += 2)
            Gfx.Part(chest, Box, new Vector3(s * 0.11f, 0.17f, 0f), new Vector3(0.035f, 0.3f, 0.27f), dark);      // straps

        // ---------- arms ----------
        armL = Gfx.Pivot(chest, new Vector3(-0.215f, 0.25f, 0f), "ArmL");
        armR = Gfx.Pivot(chest, new Vector3(0.215f, 0.25f, 0f), "ArmR");
        for (int i = 0; i < 2; i++)
        {
            Transform arm = i == 0 ? armL : armR;
            float side = i == 0 ? -1f : 1f;
            Gfx.Part(arm, Ball, Vector3.zero, new Vector3(0.15f, 0.14f, 0.15f), coat);
            Gfx.Part(arm, Cap, new Vector3(0f, -0.15f, 0f), new Vector3(0.12f, 0.17f, 0.12f), coat);
            Transform elbow = Gfx.Pivot(arm, new Vector3(0f, -Upper, 0f), "Elbow");
            Gfx.Part(elbow, Ball, Vector3.zero, Vector3.one * 0.115f, coat);
            Gfx.Part(elbow, Cap, new Vector3(0f, -0.11f, 0f), new Vector3(0.105f, 0.13f, 0.105f), coat);
            Gfx.Part(elbow, Cyl, new Vector3(0f, -0.215f, 0f), new Vector3(0.112f, 0.025f, 0.112f), glowAcc);           // cuff stripe
            Gfx.Part(elbow, Cyl, new Vector3(0f, -0.245f, 0f), new Vector3(0.085f, 0.035f, 0.085f), glove);           // glove cuff
            Gfx.Part(elbow, Ball, new Vector3(0f, -Lower, 0.005f), new Vector3(0.08f, 0.1f, 0.06f), glove);          // hand
            Gfx.Part(elbow, Ball, new Vector3(-side * 0.035f, -0.29f, 0.03f), new Vector3(0.03f, 0.055f, 0.03f), glove, new Vector3(0f, 0f, side * 30f));   // thumb
            if (i == 0) elbowL = elbow; else elbowR = elbow;
        }

        // ---------- head ----------
        head = Gfx.Pivot(chest, new Vector3(0f, 0.36f, 0.01f), "Head");
        Gfx.Part(head, Ball, new Vector3(0f, 0.13f, 0f), new Vector3(0.21f, 0.25f, 0.23f), skinM);
        Gfx.Part(head, Ball, new Vector3(0f, 0.055f, 0.025f), new Vector3(0.16f, 0.1f, 0.15f), skinM);
        Gfx.Part(head, Ball, new Vector3(-0.105f, 0.12f, 0f), new Vector3(0.035f, 0.06f, 0.035f), skinM);
        Gfx.Part(head, Ball, new Vector3(0.105f, 0.12f, 0f), new Vector3(0.035f, 0.06f, 0.035f), skinM);
        Gfx.Part(head, Box, new Vector3(0f, 0.11f, 0.115f), new Vector3(0.032f, 0.05f, 0.04f), Gfx.Lit(Color.Lerp(skin, Color.black, 0.07f)));   // nose
        Gfx.Part(head, Box, new Vector3(0f, 0.065f, 0.105f), new Vector3(0.05f, 0.012f, 0.012f), Gfx.Lit(Color.Lerp(skin, new Color(0.5f, 0.2f, 0.2f), 0.45f)));   // mouth
        Gfx.Part(head, Box, new Vector3(-0.046f, 0.195f, 0.103f), new Vector3(0.05f, 0.013f, 0.014f), hairM, new Vector3(0f, 0f, -8f));   // eyebrows
        Gfx.Part(head, Box, new Vector3(0.046f, 0.195f, 0.103f), new Vector3(0.05f, 0.013f, 0.014f), hairM, new Vector3(0f, 0f, 8f));
        // goggles over the eyes
        Gfx.Part(head, Cyl, new Vector3(0f, 0.15f, 0f), new Vector3(0.225f, 0.018f, 0.245f), dark);
        for (int s = -1; s <= 1; s += 2)
        {
            Gfx.Part(head, Cyl, new Vector3(s * 0.047f, 0.15f, 0.108f), new Vector3(0.075f, 0.025f, 0.075f), brass, new Vector3(90f, 0f, 0f));
            Gfx.Part(head, Cyl, new Vector3(s * 0.047f, 0.15f, 0.122f), new Vector3(0.06f, 0.006f, 0.06f), lens, new Vector3(90f, 0f, 0f));
            Gfx.Part(head, Ball, new Vector3(s * 0.047f, 0.15f, 0.116f), new Vector3(0.02f, 0.02f, 0.01f), eye);
        }
        Gfx.Part(head, Box, new Vector3(0f, 0.15f, 0.112f), new Vector3(0.02f, 0.015f, 0.015f), brass);              // bridge
        // mad-scientist hair
        switch (seed % 4)
        {
            case 0:     // big wild tufts sticking out the sides and back
                for (int i = 0; i < 9; i++)
                {
                    float a = Mathf.Lerp(-150f, 150f, i / 8f) * Mathf.Deg2Rad;
                    Vector3 p = new Vector3(Mathf.Sin(a) * 0.11f, 0.2f + Mathf.Cos(i * 1.7f) * 0.03f, -Mathf.Cos(a) * 0.1f - 0.01f);
                    Gfx.Part(head, Ball, p, new Vector3(0.09f, 0.07f, 0.09f) * (1f + (i % 3) * 0.15f), hairM);
                }
                Gfx.Part(head, Ball, new Vector3(0f, 0.22f, -0.03f), new Vector3(0.19f, 0.1f, 0.19f), hairM);
                break;
            case 1:     // spiky
                Gfx.Part(head, Ball, new Vector3(0f, 0.2f, -0.012f), new Vector3(0.215f, 0.13f, 0.235f), hairM);
                for (int i = 0; i < 7; i++)
                    Gfx.Part(head, Cap, new Vector3((i - 3) * 0.028f, 0.27f, -0.02f - Mathf.Abs(i - 3) * 0.01f), new Vector3(0.04f, 0.06f, 0.04f), hairM, new Vector3(-25f, 0f, (i - 3) * 14f));
                break;
            case 2:     // bald on top, fluffy at the sides, with a moustache
                Gfx.Part(head, Ball, new Vector3(-0.1f, 0.15f, -0.03f), new Vector3(0.07f, 0.1f, 0.13f), hairM);
                Gfx.Part(head, Ball, new Vector3(0.1f, 0.15f, -0.03f), new Vector3(0.07f, 0.1f, 0.13f), hairM);
                Gfx.Part(head, Ball, new Vector3(0f, 0.15f, -0.1f), new Vector3(0.16f, 0.1f, 0.06f), hairM);
                Gfx.Part(head, Ball, new Vector3(0f, 0.085f, 0.11f), new Vector3(0.08f, 0.022f, 0.03f), hairM);
                break;
            default:    // neat bun
                Gfx.Part(head, Ball, new Vector3(0f, 0.2f, -0.012f), new Vector3(0.218f, 0.14f, 0.24f), hairM);
                Gfx.Part(head, Ball, new Vector3(0f, 0.24f, -0.11f), new Vector3(0.09f, 0.09f, 0.09f), hairM);
                break;
        }

        // ---------- the gun goes here ----------
        gunRoot = Gfx.Pivot(chest, new Vector3(0.17f, 0f, 0.38f), "Held");
        gunPos = gunRoot.localPosition;

        Gfx.SetLayer(gameObject, 2);   // "Ignore Raycast": our own shots never hit our own body
    }

    // ---------- armour you can see ----------

    public void SetArmor(int tier, bool boots, ArmorDef def = null)
    {
        if (armorRoot != null) Destroy(armorRoot);
        if (helmetRoot != null) Destroy(helmetRoot);
        if (bootsRoot != null) Destroy(bootsRoot);
        foreach (GameObject g in bootParts) if (g != null) Destroy(g);
        bootParts.Clear();
        if (tier > 0)
        {
            armorRoot = new GameObject("Armour");
            armorRoot.transform.SetParent(chest, false);
            Color pc = def != null ? def.color : tier == 1 ? new Color(0.55f, 0.56f, 0.58f) : new Color(0.35f, 0.85f, 1f);
            bool glowy = def != null ? def.glow : tier == 2;
            ArmorStyle style = def != null ? def.style : ArmorStyle.Plates;
            Material plate = glowy ? Gfx.Glow(pc, 0.9f) : Gfx.Lit(pc, 0.55f, 0.6f);
            Material trim = glowy ? Gfx.Lit(Color.Lerp(pc, Color.white, 0.6f), 0.9f, 0.5f) : Gfx.Lit(new Color(pc.r * 0.55f, pc.g * 0.5f, pc.b * 0.45f), 0.3f);
            Transform a = armorRoot.transform;
            if (style == ArmorStyle.Light)
            {
                // a light vest with glowing stripes
                Gfx.Part(a, PrimitiveType.Cube, new Vector3(0f, 0.11f, 0.13f), new Vector3(0.36f, 0.22f, 0.02f), plate, new Vector3(-6f, 0f, 0f));
                Gfx.Part(a, PrimitiveType.Cube, new Vector3(0f, 0.11f, -0.13f), new Vector3(0.36f, 0.22f, 0.02f), plate);
                Gfx.Part(a, PrimitiveType.Cube, new Vector3(0f, 0.05f, 0.142f), new Vector3(0.34f, 0.02f, 0.01f), Gfx.Glow(Color.Lerp(pc, Color.white, 0.4f), 1.6f));
                Gfx.Part(a, PrimitiveType.Cube, new Vector3(0f, 0.15f, 0.142f), new Vector3(0.34f, 0.02f, 0.01f), Gfx.Glow(Color.Lerp(pc, Color.white, 0.4f), 1.6f));
                for (int s = -1; s <= 1; s += 2) Gfx.Part(a, PrimitiveType.Cube, new Vector3(s * 0.2f, 0.26f, 0f), new Vector3(0.1f, 0.03f, 0.22f), trim);
            }
            else
            {
                bool heavy = style == ArmorStyle.Heavy;
                float t = heavy ? 0.08f : 0.05f, sh = heavy ? 1.35f : 1f;
                Gfx.Part(a, PrimitiveType.Cube, new Vector3(0f, 0.12f, 0.135f + (heavy ? 0.015f : 0f)), new Vector3(heavy ? 0.4f : 0.34f, heavy ? 0.28f : 0.24f, t), plate, new Vector3(-6f, 0f, 0f));
                Gfx.Part(a, PrimitiveType.Cube, new Vector3(0f, 0.12f, -0.14f - (heavy ? 0.015f : 0f)), new Vector3(heavy ? 0.4f : 0.34f, heavy ? 0.28f : 0.24f, t * 0.8f), plate);
                Gfx.Part(a, PrimitiveType.Cube, new Vector3(0f, 0.12f, 0.165f + (heavy ? 0.03f : 0f)), new Vector3(0.06f, 0.22f, 0.012f), trim);
                for (int s = -1; s <= 1; s += 2)
                {
                    Gfx.Part(a, PrimitiveType.Sphere, new Vector3(s * 0.23f, 0.27f, 0f), new Vector3(0.17f, 0.09f, 0.18f) * sh, plate, new Vector3(0f, 0f, s * 20f));
                    Gfx.Part(a, PrimitiveType.Sphere, new Vector3(s * 0.12f, 0.2f, 0.165f + (heavy ? 0.03f : 0f)), Vector3.one * 0.025f, trim);   // bolts
                    Gfx.Part(a, PrimitiveType.Sphere, new Vector3(s * 0.12f, 0.04f, 0.163f + (heavy ? 0.03f : 0f)), Vector3.one * 0.025f, trim);
                }
                if (heavy)
                {
                    // a helmet with a visor
                    helmetRoot = new GameObject("Helmet");
                    helmetRoot.transform.SetParent(head, false);
                    Transform hm = helmetRoot.transform;
                    Gfx.Part(hm, PrimitiveType.Sphere, new Vector3(0f, 0.11f, -0.01f), new Vector3(0.3f, 0.26f, 0.31f), plate);
                    Gfx.Part(hm, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0f), new Vector3(0.04f, 0.06f, 0.3f), trim);
                    Gfx.Part(hm, PrimitiveType.Cube, new Vector3(0f, 0.09f, 0.14f), new Vector3(0.22f, 0.05f, 0.04f), Gfx.Glow(Color.Lerp(pc, Color.white, 0.5f), 2f));
                    Gfx.SetLayer(helmetRoot, 2);
                    if (firstPerson)      // don't block your own view
                        foreach (Renderer hr in helmetRoot.GetComponentsInChildren<Renderer>()) hr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                }
            }
            Gfx.SetLayer(armorRoot, 2);
        }
        if (boots)
        {
            Material slime = Gfx.Glow(new Color(0.3f, 1f, 0.4f), 0.6f);
            Material spring = Gfx.Lit(new Color(0.8f, 0.8f, 0.85f), 0.8f, 0.9f);
            bootsRoot = new GameObject("BounceBoots");
            bootsRoot.transform.SetParent(transform, false);
            foreach (Transform k in new[] { kneeL, kneeR })
            {
                var b = new GameObject("Boot").transform;
                b.SetParent(k, false);
                bootParts.Add(b.gameObject);
                Gfx.Part(b, PrimitiveType.Sphere, new Vector3(0f, -0.4f, 0.04f), new Vector3(0.17f, 0.15f, 0.3f), slime);
                for (int i = 0; i < 3; i++)
                    Gfx.Part(b, PrimitiveType.Cylinder, new Vector3(0f, -0.48f - i * 0.025f, 0.04f), new Vector3(0.1f, 0.008f, 0.1f), spring);
                Gfx.SetLayer(b.gameObject, 2);
            }
        }
    }
    readonly System.Collections.Generic.List<GameObject> bootParts = new System.Collections.Generic.List<GameObject>();
    GameObject helmetRoot;

    // ---------- what's in your hands ----------

    public void SetHeld(HeldItem item)
    {
        foreach (Transform c in gunRoot) Destroy(c.gameObject);
        Muzzle = null;
        if (item == null) { hold = Hold.None; return; }
        Recipe r = item.R;
        Color ench = item.EnchantColor;
        bool enchanted = item.Enchanted;
        var model = new GameObject(r.name).transform;
        model.SetParent(gunRoot, false);
        Material body = Gfx.Lit(new Color(0.22f, 0.24f, 0.27f), 0.5f, 0.6f);
        Material bodyLight = Gfx.Lit(new Color(0.75f, 0.77f, 0.8f), 0.6f, 0.7f);
        Material grip = Gfx.Lit(new Color(0.12f, 0.1f, 0.09f), 0.2f);
        Material brass = Gfx.Lit(new Color(0.8f, 0.6f, 0.25f), 0.7f, 0.8f);
        Material glow = Gfx.Glow(enchanted ? ench : r.color, 2.4f);
        const PrimitiveType Box = PrimitiveType.Cube, Ball = PrimitiveType.Sphere, Cyl = PrimitiveType.Cylinder;
        twoHanded = true;
        switch (item.id)
        {
            case ItemId.Zapper:
                hold = Hold.Rifle;
                Gfx.Part(model, Box, new Vector3(0f, 0.02f, 0.12f), new Vector3(0.07f, 0.09f, 0.38f), body);
                Gfx.Part(model, Box, new Vector3(0f, -0.06f, -0.01f), new Vector3(0.05f, 0.12f, 0.06f), grip, new Vector3(15f, 0f, 0f));
                Gfx.Part(model, Box, new Vector3(0f, -0.03f, 0.2f), new Vector3(0.05f, 0.06f, 0.1f), grip);
                for (int i = 0; i < 4; i++)
                    Gfx.Part(model, Cyl, new Vector3(0f, 0.02f, 0.3f + i * 0.04f), new Vector3(0.1f, 0.012f, 0.1f), brass, new Vector3(90f, 0f, 0f));
                Gfx.Part(model, Cyl, new Vector3(0f, 0.02f, 0.36f), new Vector3(0.04f, 0.09f, 0.04f), bodyLight, new Vector3(90f, 0f, 0f));
                Gfx.Part(model, Ball, new Vector3(0f, 0.02f, 0.46f), Vector3.one * 0.06f, glow);
                Gfx.Part(model, Box, new Vector3(0f, 0.085f, 0.08f), new Vector3(0.03f, 0.03f, 0.16f), glow);
                gripR = new Vector3(0f, -0.07f, -0.01f); gripL = new Vector3(0f, -0.05f, 0.16f);
                Muzzle = Gfx.Pivot(model, new Vector3(0f, 0.02f, 0.5f), "Muzzle");
                break;
            case ItemId.FreezeRay:
                hold = Hold.Rifle;
                Gfx.Part(model, Box, new Vector3(0f, 0.02f, 0.1f), new Vector3(0.08f, 0.1f, 0.4f), bodyLight);
                Gfx.Part(model, Cyl, new Vector3(0f, 0.02f, 0.42f), new Vector3(0.05f, 0.12f, 0.05f), body, new Vector3(90f, 0f, 0f));
                Gfx.Part(model, Cyl, new Vector3(0f, 0.11f, 0.08f), new Vector3(0.09f, 0.13f, 0.09f), glow, new Vector3(90f, 0f, 0f));   // ice canister
                for (int i = 0; i < 3; i++)
                    Gfx.Part(model, Box, new Vector3(0f, 0.02f, 0.3f + i * 0.06f), new Vector3(0.14f, 0.14f, 0.012f), Gfx.Lit(new Color(0.75f, 0.92f, 1f), 0.9f), new Vector3(0f, 0f, 45f));
                Gfx.Part(model, Box, new Vector3(0f, -0.07f, -0.02f), new Vector3(0.05f, 0.13f, 0.06f), grip, new Vector3(15f, 0f, 0f));
                Gfx.Part(model, Box, new Vector3(0f, -0.04f, 0.22f), new Vector3(0.05f, 0.07f, 0.09f), grip);
                Gfx.Part(model, Ball, new Vector3(0f, 0.02f, 0.55f), Vector3.one * 0.05f, glow);
                gripR = new Vector3(0f, -0.08f, -0.02f); gripL = new Vector3(0f, -0.06f, 0.18f);
                Muzzle = Gfx.Pivot(model, new Vector3(0f, 0.02f, 0.58f), "Muzzle");
                break;
            case ItemId.PlasmaLauncher:
                hold = Hold.Heavy;
                Gfx.Part(model, Cyl, new Vector3(0f, 0.05f, 0.15f), new Vector3(0.16f, 0.42f, 0.16f), body, new Vector3(90f, 0f, 0f));
                Gfx.Part(model, Cyl, new Vector3(0f, 0.05f, 0.58f), new Vector3(0.2f, 0.03f, 0.2f), brass, new Vector3(90f, 0f, 0f));
                for (int i = 0; i < 3; i++)
                    Gfx.Part(model, Cyl, new Vector3(0f, 0.05f, 0.0f + i * 0.17f), new Vector3(0.175f, 0.02f, 0.175f), glow, new Vector3(90f, 0f, 0f));
                Gfx.Part(model, Cyl, new Vector3(0f, 0.05f, -0.27f), new Vector3(0.13f, 0.03f, 0.13f), bodyLight, new Vector3(90f, 0f, 0f));
                Gfx.Part(model, Box, new Vector3(0f, -0.09f, 0.02f), new Vector3(0.05f, 0.13f, 0.06f), grip, new Vector3(15f, 0f, 0f));
                Gfx.Part(model, Box, new Vector3(0f, -0.08f, 0.3f), new Vector3(0.05f, 0.11f, 0.06f), grip);
                Gfx.Part(model, Box, new Vector3(0.1f, 0.12f, 0.1f), new Vector3(0.05f, 0.05f, 0.12f), body);       // sight
                Gfx.Part(model, Ball, new Vector3(0.1f, 0.12f, 0.17f), Vector3.one * 0.03f, glow);
                gripR = new Vector3(0f, -0.11f, 0.02f); gripL = new Vector3(0f, -0.11f, 0.26f);
                Muzzle = Gfx.Pivot(model, new Vector3(0f, 0.05f, 0.62f), "Muzzle");
                break;
            case ItemId.SlimeCannon:
                hold = Hold.Rifle;
                Gfx.Part(model, Box, new Vector3(0f, 0f, 0.1f), new Vector3(0.09f, 0.1f, 0.34f), body);
                Gfx.Part(model, Ball, new Vector3(0f, 0.13f, 0.08f), new Vector3(0.17f, 0.15f, 0.2f), Gfx.Clear(new Color(0.4f, 1f, 0.45f, 0.5f)), default, false);
                Gfx.Part(model, Ball, new Vector3(0f, 0.12f, 0.08f), new Vector3(0.13f, 0.1f, 0.15f), glow);
                Gfx.Part(model, Cyl, new Vector3(0f, 0f, 0.36f), new Vector3(0.11f, 0.08f, 0.11f), bodyLight, new Vector3(90f, 0f, 0f));
                Gfx.Part(model, Cyl, new Vector3(0f, 0f, 0.45f), new Vector3(0.15f, 0.02f, 0.15f), glow, new Vector3(90f, 0f, 0f));
                Gfx.Part(model, Box, new Vector3(0f, -0.08f, -0.03f), new Vector3(0.05f, 0.13f, 0.06f), grip, new Vector3(15f, 0f, 0f));
                Gfx.Part(model, Box, new Vector3(0f, -0.06f, 0.22f), new Vector3(0.05f, 0.08f, 0.08f), grip);
                gripR = new Vector3(0f, -0.09f, -0.03f); gripL = new Vector3(0f, -0.08f, 0.18f);
                Muzzle = Gfx.Pivot(model, new Vector3(0f, 0f, 0.48f), "Muzzle");
                break;
            default:
                GunDef gd = Guns.Get(item.id);
                if (gd != null) { BuildGun(model, gd, enchanted ? Gfx.Glow(ench, 2.4f) : Gfx.Glow(gd.color, 2.4f)); break; }
                // potions and bombs: a glass flask
                hold = Hold.Flask;
                twoHanded = false;
                Material glass = Gfx.Clear(new Color(0.85f, 0.95f, 1f, 0.35f));
                Material liquid = Gfx.Glow(r.color, 1.8f);
                bool bomb = r.kind == ItemKind.Throwable;
                Gfx.Part(model, Ball, new Vector3(0f, 0.06f, 0f), Vector3.one * (bomb ? 0.13f : 0.11f), glass, default, false);
                Gfx.Part(model, Ball, new Vector3(0f, 0.05f, 0f), Vector3.one * (bomb ? 0.105f : 0.09f), liquid);
                Gfx.Part(model, Cyl, new Vector3(0f, 0.15f, 0f), new Vector3(0.04f, 0.04f, 0.04f), glass, default, false);
                Gfx.Part(model, Cyl, new Vector3(0f, 0.2f, 0f), new Vector3(0.045f, 0.02f, 0.045f), Gfx.Lit(new Color(0.55f, 0.35f, 0.2f)));   // cork
                if (bomb)
                {
                    Gfx.Part(model, Cyl, new Vector3(0f, 0.24f, 0f), new Vector3(0.01f, 0.03f, 0.01f), Gfx.Lit(Color.white));   // fuse
                    Gfx.Part(model, Ball, new Vector3(0f, 0.275f, 0f), Vector3.one * 0.025f, Gfx.Glow(new Color(1f, 0.6f, 0.1f), 3f));
                }
                gripR = new Vector3(0f, 0.03f, -0.06f); gripL = gripR;
                Muzzle = Gfx.Pivot(model, new Vector3(0f, 0.1f, 0f), "Muzzle");
                break;
        }
        if (enchanted && hold != Hold.Flask)
        {
            // enchant glow trail along the barrel
            Gfx.Part(model, PrimitiveType.Cube, new Vector3(0.045f, 0.03f, 0.15f), new Vector3(0.01f, 0.02f, 0.3f), Gfx.Glow(ench, 3f));
            Gfx.Part(model, PrimitiveType.Cube, new Vector3(-0.045f, 0.03f, 0.15f), new Vector3(0.01f, 0.02f, 0.3f), Gfx.Glow(ench, 3f));
        }
        Gfx.SetLayer(model.gameObject, 2);
    }

    // The table guns (Guns.cs) are built from a few simple shapes, depending on their 'look'.
    void BuildGun(Transform m, GunDef g, Material glow)
    {
        const PrimitiveType Box = PrimitiveType.Cube, Ball = PrimitiveType.Sphere, Cyl = PrimitiveType.Cylinder;
        Material body = Gfx.Lit(g.body, 0.45f, 0.4f);
        Material trim = Gfx.Lit(g.trim, 0.6f, 0.6f);
        Material dark = Gfx.Lit(new Color(0.15f, 0.16f, 0.18f), 0.5f, 0.7f);
        Material grip = Gfx.Lit(new Color(0.12f, 0.1f, 0.09f), 0.2f);
        Vector3 X90 = new Vector3(90f, 0f, 0f);
        hold = Hold.Rifle;
        twoHanded = true;
        gripR = new Vector3(0f, -0.08f, -0.01f);
        float muzzleZ = 0.5f, muzzleY = 0.03f;
        System.Action pistolGrip = () => Gfx.Part(m, Box, new Vector3(0f, -0.07f, -0.01f), new Vector3(0.05f, 0.13f, 0.06f), grip, new Vector3(15f, 0f, 0f));
        switch (g.look)
        {
            case GunLook.Pistol:
                Gfx.Part(m, Box, new Vector3(0f, 0.03f, 0.08f), new Vector3(0.055f, 0.07f, 0.26f), body);
                Gfx.Part(m, Cyl, new Vector3(0f, 0.03f, 0.24f), new Vector3(0.035f, 0.04f, 0.035f), trim, X90);
                Gfx.Part(m, Box, new Vector3(0f, -0.07f, -0.01f), new Vector3(0.05f, 0.13f, 0.06f), trim, new Vector3(15f, 0f, 0f));
                Gfx.Part(m, Box, new Vector3(0f, -0.03f, 0.05f), new Vector3(0.02f, 0.03f, 0.07f), dark);
                Gfx.Part(m, Box, new Vector3(0f, 0.068f, 0.08f), new Vector3(0.02f, 0.012f, 0.18f), glow);
                gripL = new Vector3(0f, -0.07f, 0.03f);
                muzzleZ = 0.3f;
                break;
            case GunLook.Revolver:
                Gfx.Part(m, Box, new Vector3(0f, 0.035f, 0.04f), new Vector3(0.05f, 0.07f, 0.14f), body);
                Gfx.Part(m, Cyl, new Vector3(0f, 0.035f, 0.05f), new Vector3(0.085f, 0.035f, 0.085f), body, X90);   // the drum
                Gfx.Part(m, Cyl, new Vector3(0f, 0.05f, 0.22f), new Vector3(0.035f, 0.11f, 0.035f), body, X90);
                Gfx.Part(m, Box, new Vector3(0f, 0.075f, 0.3f), new Vector3(0.01f, 0.02f, 0.02f), glow);
                Gfx.Part(m, Box, new Vector3(0f, -0.07f, -0.03f), new Vector3(0.05f, 0.13f, 0.06f), trim, new Vector3(20f, 0f, 0f));
                Gfx.Part(m, Box, new Vector3(0f, -0.02f, 0.04f), new Vector3(0.02f, 0.03f, 0.06f), dark);
                gripR = new Vector3(0f, -0.08f, -0.03f); gripL = new Vector3(0f, -0.08f, 0.01f);
                muzzleZ = 0.34f; muzzleY = 0.05f;
                break;
            case GunLook.SMG:
                Gfx.Part(m, Box, new Vector3(0f, 0.02f, 0.1f), new Vector3(0.07f, 0.1f, 0.32f), body);
                Gfx.Part(m, Box, new Vector3(0f, -0.09f, 0.12f), new Vector3(0.04f, 0.14f, 0.05f), trim, new Vector3(-8f, 0f, 0f));
                Gfx.Part(m, Cyl, new Vector3(0f, 0.03f, 0.3f), new Vector3(0.035f, 0.06f, 0.035f), dark, X90);
                Gfx.Part(m, Box, new Vector3(0f, -0.05f, 0.21f), new Vector3(0.04f, 0.07f, 0.05f), grip);
                Gfx.Part(m, Box, new Vector3(0.037f, 0.03f, 0.1f), new Vector3(0.006f, 0.02f, 0.22f), glow);
                Gfx.Part(m, Box, new Vector3(-0.037f, 0.03f, 0.1f), new Vector3(0.006f, 0.02f, 0.22f), glow);
                pistolGrip();
                gripL = new Vector3(0f, -0.08f, 0.21f);
                muzzleZ = 0.37f;
                break;
            case GunLook.Rifle:
                Gfx.Part(m, Box, new Vector3(0f, 0.02f, 0.15f), new Vector3(0.07f, 0.09f, 0.5f), body);
                Gfx.Part(m, Box, new Vector3(0f, -0.01f, -0.17f), new Vector3(0.06f, 0.1f, 0.2f), trim);
                Gfx.Part(m, Cyl, new Vector3(0f, 0.03f, 0.48f), new Vector3(0.035f, 0.1f, 0.035f), dark, X90);
                Gfx.Part(m, Box, new Vector3(0f, -0.08f, 0.12f), new Vector3(0.045f, 0.12f, 0.06f), dark, new Vector3(-10f, 0f, 0f));
                Gfx.Part(m, Box, new Vector3(0f, 0.08f, 0.1f), new Vector3(0.03f, 0.03f, 0.08f), dark);
                Gfx.Part(m, Box, new Vector3(0f, 0.068f, 0.3f), new Vector3(0.02f, 0.012f, 0.2f), glow);
                pistolGrip();
                gripL = new Vector3(0f, -0.04f, 0.3f);
                muzzleZ = 0.59f;
                break;
            case GunLook.Musket:
                Gfx.Part(m, Box, new Vector3(0f, -0.01f, 0.12f), new Vector3(0.06f, 0.08f, 0.78f), body);
                Gfx.Part(m, Box, new Vector3(0f, -0.04f, -0.22f), new Vector3(0.055f, 0.12f, 0.12f), body, new Vector3(-15f, 0f, 0f));
                Gfx.Part(m, Cyl, new Vector3(0f, 0.045f, 0.38f), new Vector3(0.04f, 0.32f, 0.04f), dark, X90);
                for (int i = 0; i < 3; i++) Gfx.Part(m, Cyl, new Vector3(0f, 0.03f, 0.12f + i * 0.2f), new Vector3(0.075f, 0.012f, 0.075f), trim, X90);   // mossy bands
                Gfx.Part(m, Cyl, new Vector3(0f, 0.045f, 0.71f), new Vector3(0.06f, 0.02f, 0.06f), dark, X90);
                Gfx.Part(m, Ball, new Vector3(0f, 0.06f, 0.0f), new Vector3(0.06f, 0.04f, 0.1f), glow);
                gripR = new Vector3(0f, -0.06f, -0.06f); gripL = new Vector3(0f, -0.04f, 0.34f);
                muzzleZ = 0.74f; muzzleY = 0.045f;
                break;
            case GunLook.Shotgun:
                Gfx.Part(m, Box, new Vector3(0f, 0.01f, 0.05f), new Vector3(0.08f, 0.1f, 0.3f), body);
                foreach (float x in new[] { -0.022f, 0.022f }) Gfx.Part(m, Cyl, new Vector3(x, 0.04f, 0.34f), new Vector3(0.042f, 0.17f, 0.042f), dark, X90);
                Gfx.Part(m, Box, new Vector3(0f, -0.03f, 0.3f), new Vector3(0.065f, 0.05f, 0.14f), trim);
                Gfx.Part(m, Box, new Vector3(0f, -0.02f, -0.17f), new Vector3(0.06f, 0.1f, 0.18f), trim);
                Gfx.Part(m, Box, new Vector3(0.042f, 0.02f, 0.05f), new Vector3(0.006f, 0.03f, 0.2f), glow);
                pistolGrip();
                gripL = new Vector3(0f, -0.06f, 0.3f);
                muzzleZ = 0.52f; muzzleY = 0.04f;
                break;
            case GunLook.Sniper:
                Gfx.Part(m, Box, new Vector3(0f, 0.02f, 0.12f), new Vector3(0.06f, 0.08f, 0.45f), body);
                Gfx.Part(m, Cyl, new Vector3(0f, 0.03f, 0.56f), new Vector3(0.03f, 0.22f, 0.03f), dark, X90);
                Gfx.Part(m, Cyl, new Vector3(0f, 0.1f, 0.12f), new Vector3(0.05f, 0.13f, 0.05f), dark, X90);       // scope
                Gfx.Part(m, Ball, new Vector3(0f, 0.1f, 0.25f), Vector3.one * 0.045f, glow);
                Gfx.Part(m, Box, new Vector3(0f, 0.06f, 0.12f), new Vector3(0.02f, 0.04f, 0.04f), dark);
                Gfx.Part(m, Box, new Vector3(0f, -0.01f, -0.17f), new Vector3(0.06f, 0.11f, 0.2f), trim);
                Gfx.Part(m, Box, new Vector3(0f, -0.07f, 0.12f), new Vector3(0.04f, 0.08f, 0.05f), dark);
                pistolGrip();
                gripL = new Vector3(0f, -0.04f, 0.3f);
                muzzleZ = 0.79f;
                break;
            case GunLook.Railgun:
                hold = Hold.Heavy;
                Gfx.Part(m, Box, new Vector3(0f, 0.02f, 0.02f), new Vector3(0.11f, 0.13f, 0.32f), body);
                foreach (float x in new[] { -0.045f, 0.045f }) Gfx.Part(m, Box, new Vector3(x, 0.04f, 0.33f), new Vector3(0.025f, 0.07f, 0.62f), dark);
                for (int i = 0; i < 5; i++) Gfx.Part(m, Cyl, new Vector3(0f, 0.04f, 0.12f + i * 0.11f), new Vector3(0.08f, 0.012f, 0.08f), glow, X90);
                Gfx.Part(m, Box, new Vector3(0f, 0.08f, -0.16f), new Vector3(0.09f, 0.09f, 0.12f), trim);
                Gfx.Part(m, Box, new Vector3(0f, 0.08f, -0.16f), new Vector3(0.095f, 0.03f, 0.06f), glow);
                Gfx.Part(m, Box, new Vector3(0f, -0.09f, 0.02f), new Vector3(0.05f, 0.13f, 0.06f), grip, new Vector3(15f, 0f, 0f));
                Gfx.Part(m, Box, new Vector3(0f, -0.07f, 0.26f), new Vector3(0.05f, 0.1f, 0.06f), grip);
                gripR = new Vector3(0f, -0.11f, 0.02f); gripL = new Vector3(0f, -0.1f, 0.26f);
                muzzleZ = 0.66f; muzzleY = 0.04f;
                break;
            case GunLook.Launcher:
                hold = Hold.Heavy;
                Gfx.Part(m, Cyl, new Vector3(0f, 0.05f, 0.15f), new Vector3(0.16f, 0.38f, 0.16f), body, X90);
                Gfx.Part(m, Cyl, new Vector3(0f, 0.05f, 0.53f), new Vector3(0.19f, 0.025f, 0.19f), trim, X90);
                Gfx.Part(m, Cyl, new Vector3(0f, 0.05f, -0.22f), new Vector3(0.18f, 0.025f, 0.18f), trim, X90);
                Gfx.Part(m, Cyl, new Vector3(0f, 0.05f, 0.52f), new Vector3(0.12f, 0.03f, 0.12f), glow, X90);
                Gfx.Part(m, Box, new Vector3(0.08f, 0.05f, 0.15f), new Vector3(0.02f, 0.06f, 0.22f), glow);
                Gfx.Part(m, Box, new Vector3(0.1f, 0.13f, 0.08f), new Vector3(0.04f, 0.05f, 0.1f), dark);
                Gfx.Part(m, Box, new Vector3(0f, -0.09f, 0.02f), new Vector3(0.05f, 0.13f, 0.06f), grip, new Vector3(15f, 0f, 0f));
                Gfx.Part(m, Box, new Vector3(0f, -0.08f, 0.3f), new Vector3(0.05f, 0.11f, 0.06f), grip);
                gripR = new Vector3(0f, -0.11f, 0.02f); gripL = new Vector3(0f, -0.11f, 0.28f);
                muzzleZ = 0.58f; muzzleY = 0.05f;
                break;
            case GunLook.Sprayer:
                Gfx.Part(m, Box, new Vector3(0f, 0.01f, 0.1f), new Vector3(0.07f, 0.09f, 0.3f), dark);
                Gfx.Part(m, Cyl, new Vector3(0f, 0.12f, 0.06f), new Vector3(0.1f, 0.12f, 0.1f), body, X90);     // fuel tank
                Gfx.Part(m, Cyl, new Vector3(0f, 0.12f, 0.06f), new Vector3(0.104f, 0.02f, 0.104f), glow, X90);
                Gfx.Part(m, Cyl, new Vector3(0f, 0.02f, 0.33f), new Vector3(0.04f, 0.1f, 0.04f), trim, X90);
                Gfx.Part(m, Cyl, new Vector3(0f, 0.02f, 0.44f), new Vector3(0.07f, 0.02f, 0.07f), dark, X90);
                Gfx.Part(m, Ball, new Vector3(0f, -0.02f, 0.45f), Vector3.one * 0.03f, glow);                  // pilot light
                Gfx.Part(m, Box, new Vector3(0f, -0.05f, 0.22f), new Vector3(0.04f, 0.07f, 0.05f), grip);
                pistolGrip();
                gripL = new Vector3(0f, -0.08f, 0.22f);
                muzzleZ = 0.47f; muzzleY = 0.02f;
                break;
            case GunLook.Coil:
                Gfx.Part(m, Box, new Vector3(0f, 0.02f, 0.05f), new Vector3(0.07f, 0.09f, 0.25f), body);
                Gfx.Part(m, Cyl, new Vector3(0f, 0.03f, 0.3f), new Vector3(0.03f, 0.15f, 0.03f), dark, X90);
                for (int i = 0; i < 6; i++) Gfx.Part(m, Cyl, new Vector3(0f, 0.03f, 0.19f + i * 0.045f), new Vector3(0.09f, 0.015f, 0.09f), trim, X90);   // copper coils
                Gfx.Part(m, Ball, new Vector3(0f, 0.03f, 0.47f), Vector3.one * 0.065f, glow);
                Gfx.Part(m, Box, new Vector3(0f, -0.08f, 0.12f), new Vector3(0.05f, 0.1f, 0.07f), glow);     // battery pack
                pistolGrip();
                gripL = new Vector3(0f, -0.05f, 0.2f);
                muzzleZ = 0.5f;
                break;
            default:    // Blaster
                Gfx.Part(m, Box, new Vector3(0f, 0.02f, 0.12f), new Vector3(0.08f, 0.1f, 0.36f), body);
                for (int i = 0; i < 3; i++) Gfx.Part(m, Box, new Vector3(0f, 0.02f, 0.27f + i * 0.05f), new Vector3(0.13f, 0.13f, 0.012f), trim, new Vector3(0f, 0f, 45f));
                Gfx.Part(m, Ball, new Vector3(0f, 0.02f, 0.43f), Vector3.one * 0.07f, glow);
                Gfx.Part(m, Box, new Vector3(0f, 0.08f, 0.08f), new Vector3(0.03f, 0.03f, 0.18f), glow);
                Gfx.Part(m, Box, new Vector3(0f, -0.04f, 0.2f), new Vector3(0.05f, 0.06f, 0.1f), grip);
                pistolGrip();
                gripL = new Vector3(0f, -0.06f, 0.18f);
                muzzleZ = 0.47f; muzzleY = 0.02f;
                break;
        }
        Muzzle = Gfx.Pivot(m, new Vector3(0f, muzzleY, muzzleZ), "Muzzle");
    }

    // ---------- one-off actions ----------
    public void Kick(float amount) => recoil = Mathf.Min(1.5f, recoil + amount);
    public void PlayDrink() => drinkT = 0f;
    public void PlayThrow() => throwT = 0f;
    public void PlayGather() => gatherT = 0f;
    public void PlayWork(float seconds) { workT = 0f; workLen = seconds; }
    public void PlayPoke() => pokeT = 0f;
    public void Hurt() => flinch = 1f;
    public bool Busy => drinkT >= 0f || throwT >= 0f || gatherT >= 0f || workT >= 0f;
    public const float DrinkTime = 0.9f, ThrowTime = 0.55f, ThrowRelease = 0.3f, GatherTime = 0.75f;

    public float ThrowProgress => throwT;

    // ================================================================
    // Animation
    // ================================================================

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        transform.rotation = Quaternion.Euler(0f, aimYaw, 0f);
        Vector3 local = Quaternion.Euler(0f, -aimYaw, 0f) * new Vector3(velocity.x, 0f, velocity.z);
        float speed = local.magnitude;
        float run = Mathf.Clamp01(speed / 6.5f);
        float sprintK = Mathf.Clamp01((speed - 6f) / 3f);

        // timers
        Advance(ref drinkT, DrinkTime, dt);
        Advance(ref throwT, ThrowTime, dt);
        Advance(ref gatherT, GatherTime, dt);
        Advance(ref workT, workLen, dt);
        Advance(ref pokeT, 0.35f, dt);
        flinch = Mathf.MoveTowards(flinch, 0f, dt * 4f);
        recoil = Mathf.MoveTowards(recoil, 0f, dt * 7f);
        breath += dt * 1.6f;
        deadK = Mathf.MoveTowards(deadK, dead ? 1f : 0f, dt * 2.5f);

        // landing
        if (!grounded) airTime += dt;
        if (grounded && !wasGrounded && airTime > 0.25f) landDip = Mathf.Clamp(airTime * 0.25f, 0.08f, 0.22f);
        if (grounded) airTime = 0f;
        wasGrounded = grounded;
        landDip = Mathf.MoveTowards(landDip, 0f, dt * 0.9f);

        // ---------- legs: turn the hips toward where you're walking (strafing / backpedalling) ----------
        float moveAng = speed > 0.3f ? Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg : 0f;
        float dirSign = 1f, wantTwist = 0f;
        if (speed > 0.3f)
        {
            if (Mathf.Abs(moveAng) <= 100f) wantTwist = Mathf.Clamp(moveAng, -65f, 65f);
            else { dirSign = -1f; wantTwist = Mathf.Clamp(Mathf.DeltaAngle(0f, moveAng - 180f), -65f, 65f); }
        }
        twist = Mathf.LerpAngle(twist, wantTwist, Mathf.Clamp01(dt * 10f));

        phase += dt * (speed < 0.3f ? 0f : 4.5f + speed * 1.25f) * dirSign;
        float s = Mathf.Sin(phase);
        if (grounded && speed > 0.5f && Mathf.Sign(s) != Mathf.Sign(prevPhaseSin)) Step?.Invoke();
        prevPhaseSin = s;
        float swing = s * (14f + 34f * run);
        float legL = swing, legR = -swing;
        float kL = 5f + run * (10f + 60f * Mathf.Max(0f, Mathf.Cos(phase)));
        float kR = 5f + run * (10f + 60f * Mathf.Max(0f, -Mathf.Cos(phase)));
        float bob = Mathf.Abs(s) * 0.05f * run;
        float crouch = landDip;

        if (!grounded)
        {
            // tuck the legs in the air
            float up = Mathf.Clamp01(velocity.y / 6f);
            legL = Mathf.Lerp(-20f, 45f, up); legR = Mathf.Lerp(15f, 20f, up);
            kL = Mathf.Lerp(40f, 80f, up); kR = Mathf.Lerp(20f, 60f, up);
            bob = 0f;
        }
        if (gatherT >= 0f)
        {
            // kneel down to grab stuff
            float g = Mathf.Sin(Mathf.Clamp01(gatherT / GatherTime) * Mathf.PI);
            crouch += 0.32f * g;
            legL = 55f * g; legR = 10f * g; kL = 70f * g; kR = 85f * g;
        }
        else if (speed < 0.3f && grounded)
        {
            // idle: shift the weight a little
            legL = 2f; legR = -3f; kL = 4f + Mathf.Sin(breath * 0.5f) * 2f; kR = 6f;
        }

        // ---------- body ----------
        float wantLean = (grounded ? local.z / 9f * 12f : 0f) + sprintK * 6f;
        lean = Mathf.Lerp(lean, wantLean, Mathf.Clamp01(dt * 6f));
        sideLean = Mathf.Lerp(sideLean, -local.x / 9f * 6f, Mathf.Clamp01(dt * 6f));
        float pitch = dead ? 0f : aimPitch;

        body.localPosition = new Vector3(0f, bob - crouch + Mathf.Sin(breath) * 0.004f, 0f);
        body.localRotation = Quaternion.Euler(-85f * EaseOut(deadK), 0f, 0f);
        body.localPosition += new Vector3(0f, 0.12f * deadK, -0.3f * deadK);
        pelvis.localRotation = Quaternion.Euler(0f, twist, 0f);
        hipL.localRotation = Quaternion.Euler(-legL, 0f, -2f);
        hipR.localRotation = Quaternion.Euler(-legR, 0f, 2f);
        kneeL.localRotation = Quaternion.Euler(kL, 0f, 0f);
        kneeR.localRotation = Quaternion.Euler(kR, 0f, 0f);

        float bend = gatherT >= 0f ? 30f * Mathf.Sin(Mathf.Clamp01(gatherT / GatherTime) * Mathf.PI) : 0f;
        if (workT >= 0f) bend += 12f;
        spine.localRotation = Quaternion.Euler(lean * 0.6f - pitch * 0.25f + bend * 0.5f - flinch * 10f, twist * 0.25f, sideLean);
        chest.localRotation = Quaternion.Euler(lean * 0.4f - pitch * 0.35f + bend * 0.5f - flinch * 6f + Mathf.Sin(breath) * 0.8f, s * run * 4f, 0f);
        float headPitch = -pitch * 0.4f + (drinkT >= 0f ? -35f * Mathf.Sin(Mathf.Clamp01(drinkT / DrinkTime) * Mathf.PI) : 0f) + (bend > 0f ? -bend * 0.4f : 0f);
        head.localRotation = Quaternion.Euler(headPitch - lean * 0.3f, -twist * 0.15f, 0f);

        // coat tails swing back when running and float up when falling
        float flapBase = speed * 4.5f + (grounded ? 0f : Mathf.Clamp(-velocity.y * 6f, -20f, 40f));
        for (int i = 0; i < 4; i++)
        {
            bool front = i < 2;
            float wob = Mathf.Sin(phase * 2f + i) * 4f * run;
            float side = (i % 2 == 0 ? 1f : -1f);
            float legSwing = (i % 2 == 0 ? legL : legR) * 0.5f;
            float ang = front ? Mathf.Max(-legSwing, 0f) * -1f - 4f + wob - flapBase * 0.2f : flapBase + 6f + wob + Mathf.Max(0f, -legSwing) * 0.3f;
            flaps[i].localRotation = Quaternion.Euler(front ? ang : ang, 0f, side * (3f + run * 4f));
        }

        if (tankGlow != null) tankGlow.localScale = new Vector3(0.12f, 0.08f + 0.03f * Mathf.Sin(Time.time * 3f), 0.12f);

        PoseHands(dt, run, sprintK, s);
    }

    static void Advance(ref float t, float len, float dt)
    {
        if (t < 0f) return;
        t += dt;
        if (t >= len) t = -1f;
    }

    static float EaseOut(float k) => 1f - (1f - k) * (1f - k);

    // Places the gun / flask, then bends the arms so the hands are on it.
    void PoseHands(float dt, float run, float sprintK, float s)
    {
        Quaternion aimRot = Quaternion.Euler(-aimPitch, aimYaw, 0f);
        Quaternion chestInv = Quaternion.Inverse(chest.rotation);
        Vector3 wantPos;
        Quaternion wantRot;
        bool handsOnGun = hold != Hold.None;
        bool busyHands = gatherT >= 0f || workT >= 0f;
        slung = busyHands || dead;

        if (hold == Hold.Rifle || hold == Hold.Heavy)
        {
            if (slung)
            {
                // gun on the back while your hands are busy
                wantPos = new Vector3(-0.05f, 0.15f, -0.27f);
                wantRot = Quaternion.Euler(0f, 90f, 55f);
                handsOnGun = false;
            }
            else if (sprinting && !aiming && sprintK > 0.3f)
            {
                // across the chest while sprinting
                wantPos = new Vector3(0.02f, 0.02f, 0.3f);
                wantRot = Quaternion.Euler(-30f + s * 4f, -55f, 30f);
            }
            else if (aiming)
            {
                wantPos = hold == Hold.Heavy ? new Vector3(0.2f, 0.2f, 0.12f) : new Vector3(0.07f, 0.12f, 0.34f);
                wantRot = chestInv * aimRot;
            }
            else
            {
                wantPos = hold == Hold.Heavy ? new Vector3(0.16f, 0.1f, 0.12f) : new Vector3(0.08f, 0.0f, 0.28f);
                wantRot = chestInv * aimRot * Quaternion.Euler(7f, -3f, 0f);
                wantPos.y += s * run * 0.015f;
            }
            if (recoil > 0f && !slung)
            {
                wantPos += new Vector3(0f, 0.01f, -0.07f) * recoil;
                wantRot = wantRot * Quaternion.Euler(-7f * recoil, 0f, 0f);
            }
        }
        else if (hold == Hold.Flask)
        {
            wantPos = new Vector3(0.24f, -0.1f, 0.3f);
            wantRot = Quaternion.identity;
            if (drinkT >= 0f)
            {
                // up to the mouth, tip it back, then down
                float k = Mathf.Sin(Mathf.Clamp01(drinkT / DrinkTime) * Mathf.PI);
                Vector3 mouth = chest.InverseTransformPoint(head.TransformPoint(new Vector3(0f, 0.07f, 0.16f)));
                wantPos = Vector3.Lerp(wantPos, mouth + new Vector3(0.03f, -0.05f, 0.02f), Mathf.Clamp01(k * 1.6f));
                wantRot = Quaternion.Euler(-100f * Mathf.Clamp01(k * 1.4f), 0f, 0f);
            }
            else if (throwT >= 0f)
            {
                float k = throwT / ThrowTime;
                if (k < 0.5f) { wantPos = Vector3.Lerp(wantPos, new Vector3(0.3f, 0.35f, -0.25f), k / 0.5f); wantRot = Quaternion.Euler(60f, 0f, 0f); }
                else { wantPos = Vector3.Lerp(new Vector3(0.3f, 0.35f, -0.25f), new Vector3(0.2f, 0.15f, 0.6f), (k - 0.5f) / 0.2f); wantRot = Quaternion.Euler(-40f, 0f, 0f); }
            }
            else if (sprintK > 0.3f) wantPos += new Vector3(0f, -0.05f, -0.1f);
            if (busyHands) wantPos = new Vector3(0.2f, -0.35f, 0.05f);
        }
        else { wantPos = gunRoot.localPosition; wantRot = gunRoot.localRotation; }

        float follow = (aiming || throwT >= 0f || drinkT >= 0f) ? 22f : 12f;
        gunPos = Vector3.Lerp(gunPos, wantPos, Mathf.Clamp01(dt * follow));
        gunRot = Quaternion.Slerp(gunRot, wantRot, Mathf.Clamp01(dt * follow));
        gunRoot.localPosition = gunPos;
        gunRoot.localRotation = gunRot;
        gunRoot.gameObject.SetActive(hold != Hold.None && (hold != Hold.Flask || !(throwT >= 0f && throwT > ThrowRelease)));

        // ---------- hand targets ----------
        Vector3 shR = armR.position, shL = armL.position;
        Vector3 tR, tL, poleR, poleL, fwdR, fwdL;
        Vector3 fwd = chest.forward, right = chest.right, up = chest.up;

        // default: free arms swinging as you walk
        float armSwing = s * (0.12f + 0.2f * run);
        float lift = 0.12f * run + 0.2f * sprintK;
        tR = chest.TransformPoint(new Vector3(0.25f, -0.32f + lift, -armSwing * 0.9f + 0.05f));
        tL = chest.TransformPoint(new Vector3(-0.25f, -0.32f + lift, armSwing * 0.9f + 0.05f));
        poleR = -fwd - up * 0.3f + right * 0.3f;
        poleL = -fwd - up * 0.3f - right * 0.3f;
        fwdR = fwd; fwdL = fwd;

        if (!grounded && !dead)
        {
            tR = chest.TransformPoint(new Vector3(0.38f, 0.05f, 0.05f));
            tL = chest.TransformPoint(new Vector3(-0.38f, 0.05f, 0.05f));
        }
        if (dead)
        {
            tR = chest.TransformPoint(new Vector3(0.45f, 0.1f, 0f));
            tL = chest.TransformPoint(new Vector3(-0.45f, 0.1f, 0f));
        }

        if (handsOnGun && !dead)
        {
            tR = gunRoot.TransformPoint(gripR);
            fwdR = gunRoot.forward;
            poleR = right * 0.8f - up * 0.9f - fwd * 0.2f;
            if (twoHanded && drinkT < 0f && throwT < 0f)
            {
                tL = gunRoot.TransformPoint(gripL);
                fwdL = gunRoot.forward;
                poleL = -right * 0.7f - up * 1f;
            }
        }
        if (gatherT >= 0f)
        {
            float k = Mathf.Clamp01(gatherT / GatherTime);
            float reach = Mathf.Sin(k * Mathf.PI);
            float pump = Mathf.Sin(k * Mathf.PI * 4f) * 0.06f;
            tR = transform.TransformPoint(new Vector3(0.12f, Mathf.Lerp(0.9f, 0.25f, reach) + pump, Mathf.Lerp(0.3f, 0.55f, reach)));
            tL = transform.TransformPoint(new Vector3(-0.12f, Mathf.Lerp(0.9f, 0.3f, reach) - pump, Mathf.Lerp(0.3f, 0.5f, reach)));
            poleR = -fwd + right; poleL = -fwd - right;
        }
        if (workT >= 0f)
        {
            // tinkering at the bench: hands at waist height, taking turns
            float a = Mathf.Sin(workT * 14f) * 0.07f, b = Mathf.Cos(workT * 11f) * 0.06f;
            tR = transform.TransformPoint(new Vector3(0.14f + b, 1.0f + a, 0.48f));
            tL = transform.TransformPoint(new Vector3(-0.14f - a, 1.0f - b, 0.5f));
            poleR = right - up; poleL = -right - up;
        }
        if (pokeT >= 0f && !busyHands && hold == Hold.None)
        {
            float k = Mathf.Sin(Mathf.Clamp01(pokeT / 0.35f) * Mathf.PI);
            tR = Vector3.Lerp(tR, chest.TransformPoint(new Vector3(0.12f, 0.1f, 0.55f)), k);
        }

        SolveArm(armR, elbowR, tR, poleR, fwdR);
        SolveArm(armL, elbowL, tL, poleL, fwdL);
    }

    // Two-bone IK: bends the arm at the elbow so the hand reaches the target.
    static void SolveArm(Transform shoulder, Transform elbow, Vector3 target, Vector3 pole, Vector3 handFwd)
    {
        Vector3 s = shoulder.position;
        Vector3 to = target - s;
        float d = Mathf.Clamp(to.magnitude, 0.05f, (Upper + Lower) * 0.999f);
        Vector3 dir = to.sqrMagnitude > 1e-6f ? to.normalized : Vector3.down;
        float a = (Upper * Upper - Lower * Lower + d * d) / (2f * d);
        float h = Mathf.Sqrt(Mathf.Max(0f, Upper * Upper - a * a));
        Vector3 bend = Vector3.ProjectOnPlane(pole, dir);
        if (bend.sqrMagnitude < 1e-6f) bend = Vector3.ProjectOnPlane(Vector3.down, dir);
        bend.Normalize();
        Vector3 e = s + dir * a + bend * h;
        shoulder.rotation = Limb(e - s, -bend);
        elbow.rotation = Limb((s + dir * d) - e, handFwd);
    }

    // Rotation whose -Y axis points along 'down' (our limbs hang along -Y).
    static Quaternion Limb(Vector3 down, Vector3 fwdHint)
    {
        Vector3 upAxis = -down.normalized;
        Vector3 f = Vector3.ProjectOnPlane(fwdHint, upAxis);
        if (f.sqrMagnitude < 1e-6f) f = Vector3.ProjectOnPlane(Vector3.forward, upAxis);
        if (f.sqrMagnitude < 1e-6f) f = Vector3.ProjectOnPlane(Vector3.right, upAxis);
        return Quaternion.LookRotation(f.normalized, upAxis);
    }

    // Where shots come from if there's no gun (e.g. throwing).
    public Vector3 HandPosition => elbowR.TransformPoint(new Vector3(0f, -Lower, 0f));
    public Vector3 HeadPosition => head.TransformPoint(new Vector3(0f, 0.13f, 0f));
    public Vector3 EyePosition => head.TransformPoint(new Vector3(0f, 0.15f, 0.14f));

    // 1st person: your own head is hidden (it would block the camera) but still casts a shadow.
    bool firstPerson;
    public void SetFirstPerson(bool on)
    {
        if (on == firstPerson) return;
        firstPerson = on;
        foreach (Renderer r in head.GetComponentsInChildren<Renderer>(true))
            r.shadowCastingMode = on ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
    }
}
