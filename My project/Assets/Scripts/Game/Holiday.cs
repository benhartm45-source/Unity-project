using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// HOLIDAY VERSIONS OF THE MAP (Settings > Theme: AUTO / NORMAL / HALLOWEEN / WINTER).
// AUTO picks by the date: Halloween in October (and the first days of November), Winter from December to mid-January.
//
//   HALLOWEEN  spooky purple fog, carved pumpkins glowing all over the map, bats circling the sky,
//              and THE GHOST BOSS: it appears 20 seconds into MADNESS, hunts the scientists and throws ghost orbs.
//              Everyone can fight it - beat it for a pile of rare materials.
//   WINTER     snow falling, white fog, snowmen and snow piles around the map,
//              and everyone starts with a SNOWBALL GUN.
// Online, everyone uses the HOST's theme (it's sent with the match clock).
public static class Holiday
{
    public enum Theme { Normal, Halloween, Winter }
    public static readonly string[] SettingNames = { "AUTO", "NORMAL", "HALLOWEEN", "WINTER" };

    public static Theme Current { get; private set; } = Theme.Normal;

    static GameObject root;
    static bool savedLook;
    static Color fogColor, ambient, sunColor;
    static float sunIntensity;
    static Light sun;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Current = Theme.Normal; root = null; savedLook = false; sun = null; }

    // What the setting means today.
    public static Theme FromSetting(int setting)
    {
        if (setting == 1) return Theme.Normal;
        if (setting == 2) return Theme.Halloween;
        if (setting == 3) return Theme.Winter;
        System.DateTime d = System.DateTime.Now;
        if (d.Month == 10 || (d.Month == 11 && d.Day <= 2)) return Theme.Halloween;
        if (d.Month == 12 || (d.Month == 1 && d.Day <= 15)) return Theme.Winter;
        return Theme.Normal;
    }

    // Start of a match (MLGame.Begin).
    public static void Begin()
    {
        Set(FromSetting(Settings.Theme), true);
    }

    // Online: the host's clock says which theme it's using.
    public static void FollowHost(Theme t)
    {
        if (t != Current) Set(t, false);
    }

    static void Set(Theme t, bool matchStart)
    {
        Cleanup();
        Current = t;
        if (t == Theme.Normal) return;
        root = new GameObject("Holiday " + t);
        SaveLook();
        if (t == Theme.Halloween)
        {
            Tint(new Color(0.24f, 0.12f, 0.3f), 0.7f, new Color(1f, 0.6f, 0.45f), 0.65f);
            Scatter(MakePumpkin, 40, 9f);
            for (int i = 0; i < 10; i++) MakeBat(i);
            root.AddComponent<GhostBossSpawner>();
        }
        else
        {
            Tint(new Color(0.86f, 0.9f, 0.96f), 1.05f, new Color(0.85f, 0.92f, 1f), 0.9f);
            Scatter(MakeSnowman, 22, 14f);
            Scatter(MakeSnowPile, 45, 6f);
            MakeSnowfall();
            Player p = Player.Local;
            if (p != null && matchStart) p.GiveItem(ItemId.SnowballGun, 1);
        }
    }

    // End of a match / switching theme: put the map back to normal.
    public static void Cleanup()
    {
        if (root != null) Object.Destroy(root);
        root = null;
        foreach (GhostBoss b in GhostBoss.All.ToArray()) if (b != null) Object.Destroy(b.gameObject);
        RestoreLook();
    }

    // ---------------- fog and light ----------------

    static void SaveLook()
    {
        if (savedLook) return;
        savedLook = true;
        fogColor = RenderSettings.fogColor;
        ambient = RenderSettings.ambientLight;
        sun = RenderSettings.sun;
        if (sun == null) foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) { sun = l; break; }
        if (sun != null) { sunColor = sun.color; sunIntensity = sun.intensity; }
    }

    static void RestoreLook()
    {
        if (!savedLook) return;
        savedLook = false;
        RenderSettings.fogColor = fogColor;
        RenderSettings.ambientLight = ambient;
        if (sun != null) { sun.color = sunColor; sun.intensity = sunIntensity; }
    }

    static void Tint(Color fog, float ambientMul, Color sunTint, float sunMul)
    {
        RenderSettings.fogColor = fog;
        RenderSettings.ambientLight = ambient * ambientMul;
        if (sun != null) { sun.color = sunColor * sunTint; sun.intensity = sunIntensity * sunMul; }
    }

    // ---------------- props around the map ----------------

    // Puts things on the ground near the material spots, away from the labs.
    static void Scatter(System.Func<Vector3, float, Transform> make, int count, float spacing)
    {
        var spots = new List<Vector3>();
        foreach (MaterialNode n in MaterialNode.All) if (n != null) spots.Add(n.transform.position);
        Vector3 center = MLGame.ArenaCenter;
        float radius = MLGame.ArenaRadius * 0.9f;
        var placed = new List<Vector3>();
        int lights = 0;
        for (int tries = 0; tries < count * 12 && placed.Count < count; tries++)
        {
            Vector3 b;
            if (spots.Count > 0 && Random.value < 0.7f) b = spots[Random.Range(0, spots.Count)];
            else { Vector2 c = Random.insideUnitCircle * radius; b = center + new Vector3(c.x, 0f, c.y); }
            Vector2 off = Random.insideUnitCircle * 7f;
            Vector3 p = b + new Vector3(off.x, 0f, off.y);
            if (!Physics.Raycast(p + Vector3.up * 40f, Vector3.down, out RaycastHit hit, 120f, Combat.NotPlayer, QueryTriggerInteraction.Ignore)) continue;
            if (hit.normal.y < 0.75f) continue;
            p = hit.point;
            Terrain ter = Terrain.activeTerrain;
            if (ter != null && Mathf.Abs(p.y - (ter.SampleHeight(p) + ter.transform.position.y)) > 1f) continue;
            Vector3 flat = p - center; flat.y = 0f;
            if (flat.magnitude > radius) continue;
            bool ok = true;
            foreach (Laboratory l in Laboratory.All)
                if (l != null && Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(l.transform.position.x, 0f, l.transform.position.z)) < 13f) { ok = false; break; }
            foreach (Vector3 q in placed) if (Vector3.Distance(p, q) < spacing) { ok = false; break; }
            if (!ok) continue;
            placed.Add(p);
            Transform t = make(p, Random.value * 360f);
            // a few of them light up the ground (not too many - lights cost)
            if (t != null && t.name == "Pumpkin" && lights < (GraphicsQuality.Level == GraphicsQuality.Low ? 2 : 8) && Random.value < 0.35f)
            {
                lights++;
                Gfx.AddLight(t, new Vector3(0f, 0.5f, 0f), new Color(1f, 0.55f, 0.15f), 6f, 2f);
            }
        }
    }

    static Transform MakePumpkin(Vector3 at, float yaw)
    {
        Transform t = new GameObject("Pumpkin").transform;
        t.SetParent(root.transform, false);
        t.position = at;
        t.rotation = Quaternion.Euler(0f, yaw, 0f);
        float s = Random.Range(0.8f, 1.4f);
        t.localScale = Vector3.one * s;
        Color orange = new Color(1f, 0.5f, 0.08f);
        Material skin = Gfx.Lit(orange, 0.35f);
        Gfx.Part(t, PrimitiveType.Sphere, new Vector3(0f, 0.36f, 0f), new Vector3(0.8f, 0.62f, 0.75f), skin);
        Gfx.Part(t, PrimitiveType.Sphere, new Vector3(-0.2f, 0.36f, 0f), new Vector3(0.5f, 0.6f, 0.68f), skin, default, false);
        Gfx.Part(t, PrimitiveType.Sphere, new Vector3(0.2f, 0.36f, 0f), new Vector3(0.5f, 0.6f, 0.68f), skin, default, false);
        Gfx.Part(t, PrimitiveType.Cylinder, new Vector3(0f, 0.72f, 0f), new Vector3(0.08f, 0.08f, 0.08f), Gfx.Lit(new Color(0.35f, 0.25f, 0.1f)), new Vector3(0f, 0f, 12f));
        // carved face (glows)
        Material face = Gfx.Glow(new Color(1f, 0.85f, 0.25f), 3.5f);
        Gfx.Part(t, PrimitiveType.Cube, new Vector3(-0.15f, 0.46f, 0.36f), new Vector3(0.12f, 0.1f, 0.03f), face, new Vector3(0f, 0f, 45f), false);
        Gfx.Part(t, PrimitiveType.Cube, new Vector3(0.15f, 0.46f, 0.36f), new Vector3(0.12f, 0.1f, 0.03f), face, new Vector3(0f, 0f, 45f), false);
        Gfx.Part(t, PrimitiveType.Cube, new Vector3(0f, 0.27f, 0.36f), new Vector3(0.36f, 0.07f, 0.03f), face, default, false);
        for (int i = -1; i <= 1; i += 2) Gfx.Part(t, PrimitiveType.Cube, new Vector3(i * 0.09f, 0.31f, 0.36f), new Vector3(0.05f, 0.05f, 0.03f), face, new Vector3(0f, 0f, 45f), false);
        return t;
    }

    static void MakeBat(int i)
    {
        Transform t = new GameObject("Bat").transform;
        t.SetParent(root.transform, false);
        var bat = t.gameObject.AddComponent<Bat>();
        bat.center = MLGame.ArenaCenter + Vector3.up * Random.Range(20f, 35f);
        bat.radius = Random.Range(25f, MLGame.ArenaRadius * 0.7f);
        bat.speed = Random.Range(0.15f, 0.3f) * (i % 2 == 0 ? 1f : -1f);
        bat.phase = Random.value * 10f;
        Material black = Gfx.Lit(new Color(0.08f, 0.06f, 0.1f));
        Gfx.Part(t, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.35f, 0.3f, 0.5f), black, default, false);
        bat.wingL = Gfx.Pivot(t, new Vector3(-0.12f, 0f, 0f), "WingL");
        bat.wingR = Gfx.Pivot(t, new Vector3(0.12f, 0f, 0f), "WingR");
        Gfx.Part(bat.wingL, PrimitiveType.Cube, new Vector3(-0.45f, 0f, 0f), new Vector3(0.9f, 0.03f, 0.45f), black, default, false);
        Gfx.Part(bat.wingR, PrimitiveType.Cube, new Vector3(0.45f, 0f, 0f), new Vector3(0.9f, 0.03f, 0.45f), black, default, false);
        Gfx.Part(t, PrimitiveType.Sphere, new Vector3(-0.07f, 0.06f, 0.24f), Vector3.one * 0.05f, Gfx.Glow(new Color(1f, 0.2f, 0.1f), 3f), default, false);
        Gfx.Part(t, PrimitiveType.Sphere, new Vector3(0.07f, 0.06f, 0.24f), Vector3.one * 0.05f, Gfx.Glow(new Color(1f, 0.2f, 0.1f), 3f), default, false);
        Gfx.SetLayer(t.gameObject, 2);
    }

    static Transform MakeSnowman(Vector3 at, float yaw)
    {
        Transform t = new GameObject("Snowman").transform;
        t.SetParent(root.transform, false);
        t.position = at;
        t.rotation = Quaternion.Euler(0f, yaw, 0f);
        t.localScale = Vector3.one * Random.Range(0.9f, 1.3f);
        Material snow = Gfx.Lit(new Color(0.95f, 0.97f, 1f), 0.2f), coal = Gfx.Lit(new Color(0.08f, 0.08f, 0.1f));
        Gfx.Part(t, PrimitiveType.Sphere, new Vector3(0f, 0.45f, 0f), Vector3.one * 1f, snow);
        Gfx.Part(t, PrimitiveType.Sphere, new Vector3(0f, 1.2f, 0f), Vector3.one * 0.72f, snow);
        Gfx.Part(t, PrimitiveType.Sphere, new Vector3(0f, 1.78f, 0f), Vector3.one * 0.5f, snow);
        Gfx.Part(t, PrimitiveType.Sphere, new Vector3(-0.09f, 1.85f, 0.22f), Vector3.one * 0.06f, coal, default, false);
        Gfx.Part(t, PrimitiveType.Sphere, new Vector3(0.09f, 1.85f, 0.22f), Vector3.one * 0.06f, coal, default, false);
        Gfx.Part(t, PrimitiveType.Cylinder, new Vector3(0f, 1.76f, 0.32f), new Vector3(0.05f, 0.12f, 0.05f), Gfx.Lit(new Color(1f, 0.5f, 0.1f)), new Vector3(90f, 0f, 0f), false);   // carrot
        for (int i = 0; i < 3; i++) Gfx.Part(t, PrimitiveType.Sphere, new Vector3(0f, 1.05f + i * 0.14f, 0.35f), Vector3.one * 0.06f, coal, default, false);              // buttons
        Gfx.Part(t, PrimitiveType.Cylinder, new Vector3(0f, 2.05f, 0f), new Vector3(0.36f, 0.02f, 0.36f), coal);                                                           // hat
        Gfx.Part(t, PrimitiveType.Cylinder, new Vector3(0f, 2.2f, 0f), new Vector3(0.24f, 0.15f, 0.24f), coal);
        Gfx.Part(t, PrimitiveType.Cylinder, new Vector3(0f, 2.1f, 0f), new Vector3(0.25f, 0.025f, 0.25f), Gfx.Lit(new Color(0.85f, 0.15f, 0.15f)), default, false);
        Material stick = Gfx.Lit(new Color(0.4f, 0.26f, 0.14f));
        Gfx.Part(t, PrimitiveType.Cylinder, new Vector3(-0.55f, 1.35f, 0f), new Vector3(0.03f, 0.3f, 0.03f), stick, new Vector3(0f, 0f, 55f), false);
        Gfx.Part(t, PrimitiveType.Cylinder, new Vector3(0.55f, 1.35f, 0f), new Vector3(0.03f, 0.3f, 0.03f), stick, new Vector3(0f, 0f, -55f), false);
        // scarf
        Gfx.Part(t, PrimitiveType.Cylinder, new Vector3(0f, 1.52f, 0f), new Vector3(0.48f, 0.05f, 0.48f), Gfx.Lit(new Color(0.2f, 0.5f, 0.95f)), default, false);
        return t;
    }

    static Transform MakeSnowPile(Vector3 at, float yaw)
    {
        Transform t = new GameObject("Snow").transform;
        t.SetParent(root.transform, false);
        t.position = at;
        t.rotation = Quaternion.Euler(0f, yaw, 0f);
        Material snow = Gfx.Lit(new Color(0.95f, 0.97f, 1f), 0.25f);
        float s = Random.Range(1.2f, 3f);
        Gfx.Part(t, PrimitiveType.Sphere, Vector3.zero, new Vector3(s, s * 0.25f, s * 0.8f), snow, default, false);
        Gfx.Part(t, PrimitiveType.Sphere, new Vector3(s * 0.3f, 0f, s * 0.2f), new Vector3(s * 0.6f, s * 0.2f, s * 0.6f), snow, default, false);
        return t;
    }

    static void MakeSnowfall()
    {
        var go = new GameObject("Snowfall");
        go.transform.SetParent(root.transform, false);
        go.AddComponent<Snowfall>();
    }

    // The round soft dot used for snowflakes.
    static Texture2D flake;
    public static Texture2D FlakeTexture
    {
        get
        {
            if (flake != null) return flake;
            flake = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(8f, 8f)) / 8f;
                    flake.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d)));
                }
            flake.Apply();
            return flake;
        }
    }
}

// A bat flapping in circles high above the map.
public class Bat : MonoBehaviour
{
    public Vector3 center;
    public float radius, speed, phase;
    public Transform wingL, wingR;
    float t;

    void Update()
    {
        t += Time.deltaTime;
        float a = phase + t * speed;
        Vector3 p = center + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(t * 0.7f + phase) * 3f, Mathf.Sin(a) * radius);
        Vector3 dir = p - transform.position;
        transform.position = p;
        if (dir.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(dir);
        float flap = Mathf.Sin(t * 14f + phase) * 40f;
        if (wingL != null) wingL.localRotation = Quaternion.Euler(0f, 0f, flap);
        if (wingR != null) wingR.localRotation = Quaternion.Euler(0f, 0f, -flap);
    }
}

// Snow falling around the camera.
public class Snowfall : MonoBehaviour
{
    ParticleSystem ps;

    void Start()
    {
        ps = gameObject.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 9f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
        main.startColor = new Color(1f, 1f, 1f, 0.9f);
        main.gravityModifier = 0.03f;
        main.maxParticles = GraphicsQuality.Level == GraphicsQuality.Low ? 600 : 2500;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission;
        em.rateOverTime = GraphicsQuality.Level == GraphicsQuality.Low ? 60f : 260f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(60f, 1f, 60f);
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.6f;
        noise.frequency = 0.3f;
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        vel.y = new ParticleSystem.MinMaxCurve(-1.6f, -0.9f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        var r = GetComponent<ParticleSystemRenderer>();
        var m = new Material(Shader.Find("Sprites/Default")) { mainTexture = Holiday.FlakeTexture };
        r.sharedMaterial = m;
        Gfx.SetLayer(gameObject, 2);
        ps.Play();
    }

    void LateUpdate()
    {
        Camera c = GameCam.I != null ? GameCam.I.cam : Camera.main;
        if (c != null) transform.position = c.transform.position + Vector3.up * 14f;
    }
}

// ================================================================
// THE GHOST BOSS (Halloween)
// ================================================================

// Spawns the ghost 20 seconds into MADNESS (on the host's game, or when you play alone).
public class GhostBossSpawner : MonoBehaviour
{
    bool spawned;
    void Update()
    {
        MLGame gm = MLGame.I;
        if (spawned || gm == null || !MLGame.Playing || Net.IsClient) return;
        if (gm.phase == MLGame.Phase.Madness && gm.madnessTime > 20f)
        {
            spawned = true;
            GhostBoss.Spawn(MLGame.ArenaCenter + Vector3.up * 12f);
        }
    }
}

public class GhostBoss : MonoBehaviour, IDamageable
{
    public static readonly List<GhostBoss> All = new List<GhostBoss>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => All.Clear();

    static readonly Color Spook = new Color(0.75f, 0.55f, 1f);

    float hp, maxHp;
    bool dead;
    int targetSlot = -1;      // which player it's hunting (lab slot)
    int attackSeq, lastSeenSeq;
    float attackCd = 4f, retargetCd, sendCd, flash, laughCd = 6f, t;
    Vector3 netPos;
    Transform look, armL, armR;
    readonly List<Renderer> bodyParts = new List<Renderer>();
    Material bodyMat, flashMat;
    static GunDef orb;
    static bool registered;

    public int Team => Teams.Drones;
    public bool Alive => !dead;
    public Vector3 Center => transform.position;

    static NetworkManager NM => NetworkManager.Singleton;

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    public static GhostBoss Spawn(Vector3 pos)
    {
        var go = new GameObject("Ghost Boss");
        go.transform.position = pos;
        var g = go.AddComponent<GhostBoss>();
        int players = Mathf.Max(1, MLGame.OnlinePlayers);
        g.maxHp = g.hp = 1800f + 900f * (players - 1);
        g.netPos = pos;
        g.Build();
        if (!Net.IsClient)
        {
            GameHUD.Banner("THE GHOST BOSS!", "A giant ghost haunts the map. Beat it for rare materials!", Spook);
            Sfx.Play("ghost_laugh", 0.9f);
        }
        return g;
    }

    void Build()
    {
        look = new GameObject("look").transform;
        look.SetParent(transform, false);
        bodyMat = Gfx.Clear(new Color(0.92f, 0.92f, 1f, 0.82f));
        flashMat = Gfx.Clear(new Color(1f, 0.4f, 0.4f, 0.9f));
        void Body(Vector3 p, Vector3 s) => bodyParts.Add(Gfx.Part(look, PrimitiveType.Sphere, p, s, bodyMat, default, false).GetComponent<Renderer>());
        Body(new Vector3(0f, 1.2f, 0f), new Vector3(2.6f, 2.6f, 2.4f));         // head
        Body(new Vector3(0f, 0f, 0f), new Vector3(2.4f, 2.2f, 2.2f));           // body
        for (int i = 0; i < 5; i++)                                               // wavy tail
            Body(new Vector3(Mathf.Sin(i) * 0.3f, -1.1f - i * 0.5f, -i * 0.15f), Vector3.one * (1.9f - i * 0.34f));
        Material black = Gfx.Lit(new Color(0.05f, 0.03f, 0.08f));
        Gfx.Part(look, PrimitiveType.Sphere, new Vector3(-0.45f, 1.5f, 1.05f), new Vector3(0.45f, 0.65f, 0.25f), black, default, false);
        Gfx.Part(look, PrimitiveType.Sphere, new Vector3(0.45f, 1.5f, 1.05f), new Vector3(0.45f, 0.65f, 0.25f), black, default, false);
        Gfx.Part(look, PrimitiveType.Sphere, new Vector3(-0.45f, 1.6f, 1.15f), Vector3.one * 0.12f, Gfx.Glow(Spook, 4f), default, false);
        Gfx.Part(look, PrimitiveType.Sphere, new Vector3(0.45f, 1.6f, 1.15f), Vector3.one * 0.12f, Gfx.Glow(Spook, 4f), default, false);
        Gfx.Part(look, PrimitiveType.Sphere, new Vector3(0f, 0.75f, 1.12f), new Vector3(0.6f, 0.45f, 0.2f), black, default, false);     // "ooooo" mouth
        armL = Gfx.Pivot(look, new Vector3(-1.1f, 0.3f, 0f), "ArmL");
        armR = Gfx.Pivot(look, new Vector3(1.1f, 0.3f, 0f), "ArmR");
        bodyParts.Add(Gfx.Part(armL, PrimitiveType.Sphere, new Vector3(-0.5f, -0.2f, 0.2f), new Vector3(1.1f, 0.6f, 0.6f), bodyMat, default, false).GetComponent<Renderer>());
        bodyParts.Add(Gfx.Part(armR, PrimitiveType.Sphere, new Vector3(0.5f, -0.2f, 0.2f), new Vector3(1.1f, 0.6f, 0.6f), bodyMat, default, false).GetComponent<Renderer>());
        Gfx.AddLight(transform, Vector3.zero, Spook, 14f, 2.5f);
        // something for shots to hit
        var col = gameObject.AddComponent<SphereCollider>();
        col.radius = 1.7f;
        col.center = new Vector3(0f, 0.4f, 0f);
        gameObject.layer = 0;
        look.gameObject.layer = 2;
        foreach (Transform c in look) Gfx.SetLayer(c.gameObject, 2);
    }

    static GunDef Orb
    {
        get
        {
            if (orb == null)
                orb = new GunDef { mode = GunMode.Projectile, damage = 12f, speed = 20f, gravity = 0f, blast = 1.8f, size = 0.4f, life = 5f, force = 6f, star = false, color = Spook, structure = 0.5f };
            return orb;
        }
    }

    // ---------------- where is everybody? ----------------

    struct Target { public int slot; public Vector3 pos; }

    static List<Target> Targets()
    {
        var list = new List<Target>();
        Player me = Player.Local;
        if (me != null && me.Alive) list.Add(new Target { slot = me.labIndex, pos = me.Center });
        NetPlayers.ForEach((slot, pos, isDead) => { if (!isDead) list.Add(new Target { slot = slot, pos = pos + Vector3.up * 1f }); });
        return list;
    }

    bool TargetPos(int slot, out Vector3 pos)
    {
        foreach (Target tg in Targets()) if (tg.slot == slot) { pos = tg.pos; return true; }
        pos = Vector3.zero;
        return false;
    }

    // ---------------- every frame ----------------

    void Update()
    {
        if (!MLGame.Playing) return;
        float dt = Time.deltaTime;
        t += dt;
        RegisterNet();
        if (dead) return;
        bool authority = !Net.IsClient;

        if (authority)
        {
            // pick someone to haunt (the closest, re-checked every few seconds)
            retargetCd -= dt;
            if (retargetCd <= 0f || !TargetPos(targetSlot, out _))
            {
                retargetCd = 6f;
                float best = float.MaxValue;
                targetSlot = -1;
                foreach (Target tg in Targets())
                {
                    float d = Vector3.Distance(tg.pos, transform.position);
                    if (d < best) { best = d; targetSlot = tg.slot; }
                }
            }
            // float towards them (but keep a little distance), bobbing
            Vector3 goal = MLGame.ArenaCenter + Vector3.up * 10f;
            if (TargetPos(targetSlot, out Vector3 tp))
            {
                Vector3 away = transform.position - tp; away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
                goal = tp + away.normalized * 9f + Vector3.up * (4.5f + Mathf.Sin(t * 0.8f) * 1.5f);
            }
            transform.position = Vector3.MoveTowards(transform.position, goal, 6f * dt);
            // attack: everyone's game throws the orbs at its own player when the attack number changes
            attackCd -= dt;
            if (attackCd <= 0f && targetSlot >= 0)
            {
                attackCd = Random.Range(2.2f, 3.4f);
                attackSeq++;
            }
            laughCd -= dt;
            if (laughCd <= 0f) { laughCd = Random.Range(8f, 14f); Sfx.At(Random.value < 0.5f ? "ghost_laugh" : "ghost_boo", transform.position, 1f, Random.Range(0.85f, 1.05f), 90f); }
            sendCd -= dt;
            if (sendCd <= 0f && Net.IsHost) { sendCd = 0.1f; SendState(); }
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, netPos, dt * 8f);
        }

        // throw orbs at the local player when it's their turn
        if (attackSeq != lastSeenSeq)
        {
            lastSeenSeq = attackSeq;
            Player me = Player.Local;
            if (me != null && me.Alive && me.labIndex == targetSlot) ThrowAt(me.Center);
            Sfx.At("ghost_boo", transform.position, 0.6f, 1.3f, 70f);
        }

        // look at the target, wave arms, flash when hit
        Vector3 face = Vector3.forward;
        if (TargetPos(targetSlot, out Vector3 fp)) { face = fp - transform.position; face.y = 0f; }
        if (face.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), dt * 3f);
        look.localPosition = new Vector3(0f, Mathf.Sin(t * 1.3f) * 0.3f, 0f);
        look.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.9f) * 5f, 0f, Mathf.Sin(t * 0.7f) * 6f);
        armL.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.2f) * 25f + 10f);
        armR.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Sin(t * 2.2f + 1f) * 25f - 10f);
        flash = Mathf.Max(0f, flash - dt * 6f);
        Material m = flash > 0f ? flashMat : bodyMat;
        foreach (Renderer r in bodyParts) if (r != null && r.sharedMaterial != m) r.sharedMaterial = m;
    }

    void ThrowAt(Vector3 target)
    {
        for (int i = 0; i < 3; i++)
        {
            Vector3 from = transform.position + transform.forward * 1.8f + transform.right * (i - 1) * 1.2f;
            Vector3 dir = (target - from).normalized;
            dir = Quaternion.Euler(Random.Range(-4f, 4f), (i - 1) * 7f, 0f) * dir;
            Projectile.FireGun(Orb, from, dir * Orb.speed, Teams.Drones, Element.None, Spook, false);
        }
    }

    public void TakeHit(Hit h)
    {
        if (dead || !h.fromPlayer || h.team == Teams.Drones) return;
        flash = 1f;
        GameHUD.DamageNumber(h.point, h.damage, Spook, true);
        if (!h.dot) GameHUD.HitMarker(false);
        if (Random.value < 0.2f) Sfx.At("ghost_hurt", transform.position, 0.7f, Random.Range(0.9f, 1.2f), 60f);
        if (Net.IsClient) { SendHit(h.damage); return; }
        Damage(h.damage);
    }

    void Damage(float d)
    {
        if (dead) return;
        hp -= d;
        if (hp <= 0f) { hp = 0f; Die(); if (Net.IsHost) SendState(); }
    }

    void Die()
    {
        if (dead) return;
        dead = true;
        Sfx.At("ghost_die", transform.position, 1f, 1f, 150f);
        Fx.Burst(transform.position, Spook, 60, 12f, 0.25f, 1.2f, 2f);
        Fx.Puff(transform.position, new Color(1f, 1f, 1f, 0.5f), 30, 4f, 2f, 2.5f, Vector3.up * 2f);
        GameHUD.Banner("GHOST BOSS DEFEATED!", "It dropped a pile of rare materials - grab them!", Spook);
        // loot (everyone's game drops its own pile where the ghost was)
        Vector3 drop = MLGame.Ground(transform.position) + Vector3.up * 2f;
        int[] loot = { (int)Mat.Stardust, (int)Mat.Meteorite, (int)Mat.Glowstone, (int)Mat.Uranium, (int)Mat.Obsidian, (int)Mat.GoldOre };
        foreach (int m in loot) for (int i = 0; i < 3; i++) MaterialPickup.Drop(drop + Random.insideUnitSphere * 2f, m, 1);
        Destroy(gameObject, 0.1f);
    }

    // ---------------- online ----------------

    static void RegisterNet()
    {
        if (!Net.Active) { registered = false; return; }
        if (registered || NM == null || NM.CustomMessagingManager == null) return;
        registered = true;
        NM.CustomMessagingManager.UnregisterNamedMessageHandler("boss");
        NM.CustomMessagingManager.RegisterNamedMessageHandler("boss", OnState);
        NM.CustomMessagingManager.UnregisterNamedMessageHandler("bosshit");
        NM.CustomMessagingManager.RegisterNamedMessageHandler("bosshit", OnHit);
    }

    public static void ResetNet() => registered = false;

    void SendState()
    {
        var w = new FastBufferWriter(48, Allocator.Temp);
        try
        {
            Vector3 p = transform.position;
            w.WriteValueSafe(p.x); w.WriteValueSafe(p.y); w.WriteValueSafe(p.z);
            w.WriteValueSafe(hp); w.WriteValueSafe(maxHp);
            w.WriteValueSafe((sbyte)targetSlot);
            w.WriteValueSafe(attackSeq);
            foreach (ulong id in NM.ConnectedClientsIds)
                if (id != NM.LocalClientId) NM.CustomMessagingManager.SendNamedMessage("boss", id, w, NetworkDelivery.UnreliableSequenced);
        }
        finally { w.Dispose(); }
    }

    static void OnState(ulong sender, FastBufferReader r)
    {
        if (Net.IsHost) return;
        r.ReadValueSafe(out float x); r.ReadValueSafe(out float y); r.ReadValueSafe(out float z);
        r.ReadValueSafe(out float hp); r.ReadValueSafe(out float maxHp);
        r.ReadValueSafe(out sbyte target);
        r.ReadValueSafe(out int seq);
        if (!MLGame.Playing || Holiday.Current != Holiday.Theme.Halloween) return;
        GhostBoss g = All.Count > 0 ? All[0] : null;
        if (g == null)
        {
            if (hp <= 0f) return;
            g = Spawn(new Vector3(x, y, z));
            g.lastSeenSeq = seq;
            GameHUD.Banner("THE GHOST BOSS!", "A giant ghost haunts the map. Beat it for rare materials!", Spook);
            Sfx.Play("ghost_laugh", 0.9f);
        }
        g.netPos = new Vector3(x, y, z);
        g.maxHp = maxHp;
        g.hp = hp;
        g.targetSlot = target;
        g.attackSeq = seq;
        if (hp <= 0f) g.Die();
    }

    static void SendHit(float dmg)
    {
        if (NM == null || NM.CustomMessagingManager == null) return;
        var w = new FastBufferWriter(8, Allocator.Temp);
        try
        {
            w.WriteValueSafe(dmg);
            NM.CustomMessagingManager.SendNamedMessage("bosshit", NetworkManager.ServerClientId, w, NetworkDelivery.ReliableSequenced);
        }
        finally { w.Dispose(); }
    }

    static void OnHit(ulong sender, FastBufferReader r)
    {
        r.ReadValueSafe(out float dmg);
        if (!Net.IsHost || All.Count == 0 || All[0] == null) return;
        All[0].flash = 1f;
        All[0].Damage(dmg);
    }

    // ---------------- health bar ----------------

    void OnGUI()
    {
        if (dead || !MLGame.Playing || GameHUD.UIOpen) return;
        float s = Screen.height / 1080f;
        float w = 640f * s, h = 22f * s, x = (Screen.width - w) / 2f, y = 120f * s;
        var style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(22 * s), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(x - 4f * s, y - 30f * s, w + 8f * s, h + 36f * s), Texture2D.whiteTexture);
        GUI.color = Spook;
        GUI.Label(new Rect(x, y - 30f * s, w, 28f * s), "THE GHOST BOSS", style);
        GUI.color = new Color(0.2f, 0.1f, 0.25f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = Spook;
        GUI.DrawTexture(new Rect(x, y, w * Mathf.Clamp01(hp / Mathf.Max(1f, maxHp)), h), Texture2D.whiteTexture);
        GUI.color = old;
    }
}
