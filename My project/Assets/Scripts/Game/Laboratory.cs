using System.Collections.Generic;
using UnityEngine;

// A LABORATORY. Every scientist has one. It's where you store materials and the ONLY place you can craft and brew.
//
// The building itself is the "Laboratory" prefab made by Tools > Mad Lab > 1. Make Lab Kit (you can edit it).
// This script finds the parts by name and brings them to life:
//   Station_Brew / Station_Workbench / Station_Enchant / Station_Storage / Station_Terminal  - the stations
//   Door_L, Door_R            - sliding doors (they only open for the owner - everyone else has to break in!)
//   Structure                 - walls and roof pieces (they fall apart when the lab is destroyed)
//   Up_WallsI, Up_WallsII, Up_Field, Up_Turret, Up_Traps, Up_Brew2 - upgrades, hidden until you build them
//   SpawnPoint                - where you appear
//   Anything using the "LabAccent" material glows in the lab's colour.
//
// Health: 1000 to start (+600 / +900 with wall upgrades). Starter shield: can't be damaged in COLLECT and EXPERIMENT.
// Forcefield: 500 extra HP as a dome that recharges. If a lab is destroyed its stations stop working, its
// storage spills out on the ground for anyone to grab, and its owner can't come back if they get knocked out.
public class Laboratory : MonoBehaviour, IDamageable
{
    public static readonly List<Laboratory> All = new List<Laboratory>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => All.Clear();

    [Tooltip("1 to 8. Lab 1 is yours when you play on your own.")]
    public int labNumber = 1;

    public int Index => Mathf.Clamp(labNumber - 1, 0, 7);
    public Color LabColor => Items.LabColors[Index];
    public string ColorName => Items.LabColorNames[Index];

    public readonly int[] storage = new int[Items.MatCount];
    public float hp = 1000f, maxHp = 1000f;
    public bool Destroyed { get; private set; }
    [HideInInspector] public bool friendly;     // a co-op friend's lab (can't be damaged)
    readonly HashSet<ItemId> built = new HashSet<ItemId>();

    // inside the walls (local space)
    static readonly Vector3 InteriorCenter = new Vector3(0f, 2.6f, 0f), InteriorSize = new Vector3(13.2f, 4.4f, 11.2f);
    const float DomeRadius = 11f;

    // forcefield
    public float fieldHp, fieldMax = 500f;
    float fieldDownUntil, lastFieldHit = -10f, fieldFlash;
    Transform dome;
    Material domeMat;

    // brewing
    public class Brew { public Recipe r; public float left, total; public bool done; }
    public readonly List<Brew> brews = new List<Brew>();
    public int BrewSlots => Has(ItemId.FastBrew) ? 2 : 1;
    public bool BrewSlotFree => brews.Count < BrewSlots;
    public float BrewSpeed => Has(ItemId.FastBrew) ? 0.7f : 1f;
    AudioSource bubbleLoop;
    Renderer[] cauldronLiquid;

    // parts
    Transform doorL, doorR, turretYaw, turretPitch, turretMuzzle, structure, enchantCrystal;
    Vector3 doorLHome, doorRHome;
    float doorOpen, turretCd, alarmCd, smokeCd, lastAttackedByPlayer = -100f;
    readonly List<Light> lamps = new List<Light>();
    readonly List<LabStation> stations = new List<LabStation>();
    readonly List<SlimePuddle> traps = new List<SlimePuddle>();

    public int Team => Index;
    public bool Alive => !Destroyed;
    public Vector3 Center => transform.position + Vector3.up * 2.5f;
    public Vector3 SpawnPoint
    {
        get
        {
            Transform s = transform.Find("SpawnPoint");
            return s != null ? s.position + Vector3.up * 0.1f : transform.TransformPoint(new Vector3(0f, 0.6f, 0f));
        }
    }
    public Vector3 DoorPoint => transform.TransformPoint(new Vector3(0f, 0.6f, 7.5f));
    public bool Has(ItemId id) => built.Contains(id);
    public bool FieldUp => !Destroyed && (MLGame.ShieldsUp || (Has(ItemId.Forcefield) && fieldHp > 0f && Time.time >= fieldDownUntil));
    public bool StarterShield => MLGame.ShieldsUp && !Destroyed;
    public bool OwnedByPlayer => Player.Local != null && Player.Local.labIndex == Index;

    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() => All.Remove(this);

    void Awake()
    {
        doorL = FindDeep("Door_L");
        doorR = FindDeep("Door_R");
        if (doorL != null) doorLHome = doorL.localPosition;
        if (doorR != null) doorRHome = doorR.localPosition;
        structure = transform.Find("Structure");
        turretYaw = FindDeep("Turret_Yaw");
        turretPitch = FindDeep("Turret_Pitch");
        turretMuzzle = FindDeep("Turret_Muzzle");
        enchantCrystal = FindDeep("Enchant_Crystal");

        // Safety: the wall piece above the door must never block the doorway
        // (an older Laboratory prefab had a collision box that reached the floor).
        Transform top = FindDeep("Wall_Front_Top");
        if (top != null)
        {
            foreach (Collider c in top.GetComponents<Collider>())
            {
                if (c is BoxCollider bc)
                {
                    const float doorTop = 3.72f;    // floor (0.5) + door height (3.2), in the lab's own space
                    float high = bc.center.y + bc.size.y / 2f;
                    if (bc.center.y - bc.size.y / 2f < doorTop && high > doorTop)
                    {
                        bc.center = new Vector3(bc.center.x, (doorTop + high) / 2f, bc.center.z);
                        bc.size = new Vector3(bc.size.x, high - doorTop, bc.size.z);
                    }
                }
            }
            // hide the old trim bars that hung across the doorway
            var mr = top.GetComponent<MeshRenderer>();
            var mf = top.GetComponent<MeshFilter>();
            if (mr != null && mf != null && mf.sharedMesh != null && mf.sharedMesh.bounds.min.y < 1.5f) StripLowParts(mf);
        }

        foreach (Light l in GetComponentsInChildren<Light>(true)) lamps.Add(l);

        // stations
        string[] names = { "Station_Brew", "Station_Workbench", "Station_Enchant", "Station_Storage", "Station_Terminal" };
        for (int i = 0; i < names.Length; i++)
        {
            Transform t = FindDeep(names[i]);
            if (t == null) continue;
            var st = t.GetComponent<LabStation>();
            if (st == null) st = t.gameObject.AddComponent<LabStation>();
            st.lab = this;
            st.type = (Station)i;
            stations.Add(st);
        }

        // cauldron liquid (it changes colour with what's brewing)
        var liquids = new List<Renderer>();
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            if (r.name.StartsWith("Liquid")) liquids.Add(r);
        cauldronLiquid = liquids.ToArray();

        ApplyColor();
        ResetLab();
    }

    // Removes the triangles that sit below the top of the door (old prefab: trim bars across the doorway).
    static void StripLowParts(MeshFilter mf)
    {
        Mesh m = Instantiate(mf.sharedMesh);
        Vector3[] v = m.vertices;
        for (int s = 0; s < m.subMeshCount; s++)
        {
            int[] t = m.GetTriangles(s);
            var keep = new List<int>();
            for (int i = 0; i + 2 < t.Length; i += 3)
            {
                if (v[t[i]].y < 3.7f && v[t[i + 1]].y < 3.7f && v[t[i + 2]].y < 3.7f) continue;
                keep.Add(t[i]); keep.Add(t[i + 1]); keep.Add(t[i + 2]);
            }
            m.SetTriangles(keep, s);
        }
        mf.sharedMesh = m;
    }

    // Make the parts with the "LabAccent" material glow in this lab's colour.
    void ApplyColor()
    {
        Material accent = Gfx.Glow(LabColor, 2f);
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            Material[] ms = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < ms.Length; i++)
                if (ms[i] != null && ms[i].name.StartsWith("LabAccent")) { ms[i] = accent; changed = true; }
            if (changed) r.sharedMaterials = ms;
        }
        foreach (Light l in lamps) if (l.name.Contains("Beacon") || (l.transform.parent != null && l.transform.parent.name.Contains("Beacon"))) l.color = LabColor;
    }

    public void ResetLab()
    {
        for (int m = 0; m < Items.MatCount; m++) storage[m] = 0;
        built.Clear();
        maxHp = hp = 1000f;
        fieldHp = 0f;
        fieldDownUntil = 0f;
        brews.Clear();
        Destroyed = false;
        lastAttackedByPlayer = -100f;
        foreach (string n in new[] { "Up_WallsI", "Up_WallsII", "Up_Field", "Up_Turret", "Up_Traps", "Up_Brew2" }) Show(n, false);
        foreach (SlimePuddle t in traps) if (t != null) Destroy(t.gameObject);
        traps.Clear();
    }

    // Rival labs (when you play alone) start with a few materials and random defences, so they're worth raiding.
    public void SetupRival(int seed)
    {
        var r = new System.Random(seed);
        for (int m = 0; m < Items.MatCount; m++) storage[m] = 1 + r.Next(5);
        if (r.Next(2) == 0) Build(ItemId.WallsI, true);
        if (r.Next(3) == 0) Build(ItemId.Forcefield, true);
        if (r.Next(3) == 0) Build(ItemId.Turret, true);
        if (r.Next(2) == 0) Build(ItemId.SlimeTraps, true);
    }

    Transform FindDeep(string n) => FindDeep(transform, n);

    static Transform FindDeep(Transform t, string n)
    {
        if (t.name == n) return t;
        foreach (Transform c in t)
        {
            Transform f = FindDeep(c, n);
            if (f != null) return f;
        }
        return null;
    }

    void Show(string part, bool on)
    {
        Transform t = FindDeep(part);
        if (t != null) t.gameObject.SetActive(on);
    }

    public bool Contains(Vector3 world)
    {
        Vector3 l = transform.InverseTransformPoint(world) - InteriorCenter;
        return Mathf.Abs(l.x) < InteriorSize.x / 2f && Mathf.Abs(l.y) < InteriorSize.y / 2f && Mathf.Abs(l.z) < InteriorSize.z / 2f;
    }

    // ================================================================
    // Upgrades
    // ================================================================

    public string UpgradeProblem(ItemId id)
    {
        switch (id)
        {
            case ItemId.Repair: return hp >= maxHp - 1f ? "Your lab isn't damaged." : null;
            case ItemId.WallsI:
            case ItemId.WallsII:
            case ItemId.Forcefield:
            case ItemId.Turret:
            case ItemId.SlimeTraps:
            case ItemId.FastBrew:
                if (id == ItemId.Forcefield && Has(id) && fieldHp < fieldMax - 1f) return null;   // rebuild = refill
                return Has(id) ? "Already built." : null;
        }
        return null;
    }

    public void Build(ItemId id, bool quiet = false)
    {
        switch (id)
        {
            case ItemId.WallsI: maxHp += 600f; hp += 600f; Show("Up_WallsI", true); break;
            case ItemId.WallsII: maxHp += 900f; hp += 900f; Show("Up_WallsII", true); break;
            case ItemId.Forcefield: fieldHp = fieldMax; fieldDownUntil = 0f; Show("Up_Field", true); break;
            case ItemId.Turret: Show("Up_Turret", true); break;
            case ItemId.FastBrew: Show("Up_Brew2", true); break;
            case ItemId.Repair:
                hp = Mathf.Min(maxHp, hp + 400f);
                Fx.Burst(Center, new Color(0.4f, 1f, 0.4f), 30, 6f, 0.1f, 0.8f, 2f);
                break;
            case ItemId.SlimeTraps:
                Show("Up_Traps", true);
                Transform tr = FindDeep("Up_Traps");
                if (tr != null)
                    foreach (Transform pad in tr)
                    {
                        SlimePuddle p = SlimePuddle.Spawn(pad.position + Vector3.up * 0.5f, Index);
                        p.permanent = true;
                        p.radius = 2.2f;
                        p.transform.SetParent(transform, true);
                        traps.Add(p);
                    }
                break;
        }
        if (id != ItemId.Repair) built.Add(id);
        if (!quiet)
        {
            Fx.Burst(Center + Vector3.up * 2f, LabColor, 24, 6f, 0.12f, 0.8f, -6f);
            Sfx.At("upgrade", Center, 0.9f);
        }
    }

    // ================================================================
    // Brewing
    // ================================================================

    public void StartBrew(Recipe r)
    {
        float t = r.time * BrewSpeed;
        brews.Add(new Brew { r = r, left = t, total = t });
    }

    void UpdateBrewing(float dt)
    {
        bool bubbling = false;
        Color liquid = new Color(0.3f, 0.8f, 0.3f);
        for (int i = brews.Count - 1; i >= 0; i--)
        {
            Brew b = brews[i];
            if (!b.done)
            {
                b.left -= dt;
                bubbling = true;
                liquid = b.r.color;
                if (b.left <= 0f) b.done = true;
            }
            if (b.done)
            {
                // hand it to the owner (if their hotbar has room)
                Player p = Player.Local;
                if (OwnedByPlayer && p.Alive && p.GiveItem(b.r.id, b.r.makes))
                {
                    brews.RemoveAt(i);
                    GameHUD.Toast(b.r.name + (b.r.makes > 1 ? " x" + b.r.makes : "") + " is ready! It's in your hotbar.", b.r.color);
                    Sfx.Play("potion_ready", 0.8f);
                    Transform st = FindDeep("Station_Brew");
                    if (st != null) Fx.Burst(st.position + Vector3.up * 1.4f, b.r.color, 20, 3f, 0.1f, 0.8f, 1f);
                }
                else if (!OwnedByPlayer) brews.RemoveAt(i);
            }
        }
        // bubbles and colour
        if (cauldronLiquid != null)
            foreach (Renderer r in cauldronLiquid)
            {
                if (r == null) continue;
                r.sharedMaterial = Gfx.Glow(bubbling ? liquid : new Color(0.25f, 0.6f, 0.3f), bubbling ? 2.2f : 0.6f);
                if (bubbling && Random.value < dt * 10f) Fx.Puff(r.transform.position + Random.insideUnitSphere * 0.25f + Vector3.up * 0.1f, new Color(liquid.r, liquid.g, liquid.b, 0.5f), 1, 0.05f, 0.12f, 0.8f, Vector3.up * 0.8f);
            }
        if (bubbling && bubbleLoop == null)
        {
            Transform st = FindDeep("Station_Brew");
            bubbleLoop = Sfx.Loop("bubble_loop", st != null ? st : transform, 0.5f, 14f);
        }
        else if (!bubbling && bubbleLoop != null) { Destroy(bubbleLoop.gameObject); bubbleLoop = null; }
    }

    // ================================================================
    // Getting hit
    // ================================================================

    public void TakeHit(Hit h)
    {
        if (Destroyed) return;
        if (StarterShield || friendly)
        {
            Fx.Burst(h.point, new Color(1f, 0.9f, 0.5f), 4, 2f, 0.06f, 0.3f);
            return;
        }
        float mul = h.structureMul > 0f ? h.structureMul : 1f;
        float dmg = h.damage * mul;
        hp -= dmg;
        if (h.fromPlayer)
        {
            lastAttackedByPlayer = Time.time;
            GameHUD.DamageNumber(h.point, dmg, new Color(1f, 0.85f, 0.4f), true);
            if (!h.dot) GameHUD.HitMarker(false);
        }
        if (OwnedByPlayer && alarmCd <= 0f)
        {
            alarmCd = 8f;
            GameHUD.Warn("YOUR LAB IS UNDER ATTACK!");
            Sfx.Play("alarm", 0.6f);
        }
        if (hp <= 0f) Collapse(h.fromPlayer);
    }

    // Shots from outside stop at the dome.
    public static bool ShieldBlocks(Vector3 a, Vector3 b, int team, out Vector3 point, out Laboratory lab)
    {
        point = b;
        lab = null;
        float best = float.MaxValue;
        foreach (Laboratory l in All)
        {
            if (l == null || !l.FieldUp || l.Team == team || l.friendly) continue;
            Vector3 c = l.transform.position + Vector3.up * 1.5f;
            if ((a - c).sqrMagnitude <= DomeRadius * DomeRadius) continue;        // already inside
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 1e-4f) continue;
            d /= len;
            Vector3 m = a - c;
            float bb = Vector3.Dot(m, d), cc = m.sqrMagnitude - DomeRadius * DomeRadius;
            float disc = bb * bb - cc;
            if (disc < 0f) continue;
            float t = -bb - Mathf.Sqrt(disc);
            if (t < 0f || t > len || t >= best) continue;
            best = t;
            point = a + d * t;
            lab = l;
        }
        return lab != null;
    }

    public void ShieldHit(float dmg, Vector3 point, bool fromPlayer)
    {
        fieldFlash = 1f;
        Fx.Burst(point, StarterShield ? new Color(1f, 0.9f, 0.5f) : new Color(0.5f, 0.9f, 1f), 6, 3f, 0.08f, 0.35f, 0f);
        Sfx.At("shield_hit", point, 0.6f, Random.Range(0.9f, 1.15f));
        if (StarterShield) return;
        fieldHp -= dmg;
        lastFieldHit = Time.time;
        if (fromPlayer) { lastAttackedByPlayer = Time.time; GameHUD.HitMarker(false); }
        if (fieldHp <= 0f)
        {
            fieldHp = 0f;
            fieldDownUntil = Time.time + 15f;
            Fx.Burst(Center, new Color(0.5f, 0.9f, 1f), 40, 10f, 0.12f, 0.8f);
            Sfx.At("shield_down", Center, 1f, 1f, 80f);
            if (OwnedByPlayer) GameHUD.Warn("YOUR FORCEFIELD IS DOWN! (back in 15s)");
        }
    }

    void Collapse(bool byPlayer)
    {
        Destroyed = true;
        hp = 0f;
        fieldHp = 0f;
        brews.Clear();
        if (bubbleLoop != null) { Destroy(bubbleLoop.gameObject); bubbleLoop = null; }
        Sfx.At("lab_collapse", Center, 1f, 1f, 150f);
        GameCam.Shake(Center, 1.2f);
        for (int i = 0; i < 5; i++)
            Combat.Blast(Center + Random.insideUnitSphere * 4f, 0.1f, 0f, 0f, Teams.Neutral, Element.None, 0f, false, i % 2 == 0 ? new Color(1f, 0.55f, 0.2f) : LabColor);
        Fx.Puff(Center, new Color(0.45f, 0.42f, 0.4f, 0.5f), 30, 5f, 2.5f, 3f, Vector3.up * 1.2f);

        // walls and roof fall apart
        if (structure != null)
        {
            var pieces = new List<Transform>();
            foreach (Transform t in structure) pieces.Add(t);
            foreach (Transform t in pieces)
            {
                if (!t.gameObject.activeInHierarchy) continue;
                if (t.GetComponent<Collider>() == null)
                {
                    // bits with nothing solid (bolted-on plates) just burst apart
                    Fx.Burst(t.position + Vector3.up * 2f, new Color(0.6f, 0.6f, 0.62f), 12, 6f, 0.15f, 0.8f);
                    Destroy(t.gameObject);
                    continue;
                }
                t.SetParent(null, true);
                var rb = t.gameObject.GetComponent<Rigidbody>();
                if (rb == null) rb = t.gameObject.AddComponent<Rigidbody>();
                rb.mass = 50f;
                rb.AddForce((t.position - transform.position).normalized * Random.Range(150f, 450f) + Vector3.up * Random.Range(100f, 400f), ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 300f, ForceMode.Impulse);
                Destroy(t.gameObject, Random.Range(14f, 20f));
            }
        }
        if (doorL != null) doorL.gameObject.SetActive(false);
        if (doorR != null) doorR.gameObject.SetActive(false);
        Show("Up_Field", false);
        Show("Up_Turret", false);
        Show("Rubble", true);
        foreach (Light l in lamps) if (l != null) l.enabled = false;
        foreach (SlimePuddle t in traps) if (t != null) Destroy(t.gameObject);
        traps.Clear();

        // everything stored spills out
        for (int m = 0; m < Items.MatCount; m++)
        {
            int left = storage[m];
            while (left > 0)
            {
                int n = Mathf.Min(left, 3);
                MaterialPickup.Drop(Center + Random.insideUnitSphere * 2f, m, n);
                left -= n;
            }
            storage[m] = 0;
        }

        if (OwnedByPlayer)
        {
            GameHUD.Warn("YOUR LAB WAS DESTROYED!");
            GameHUD.Toast("No more crafting - and if you get knocked out now, you're out!", new Color(1f, 0.4f, 0.3f));
        }
        else GameHUD.Toast(ColorName + " LAB DESTROYED!" + (byPlayer ? "  Grab the loot!" : ""), LabColor);
        MLGame.I?.LabDestroyed(this);
    }

    // ================================================================
    // Every frame
    // ================================================================

    void Update()
    {
        float dt = Time.deltaTime;
        alarmCd -= dt;
        if (!MLGame.Playing) { UpdateDome(dt); return; }
        if (Destroyed)
        {
            smokeCd -= dt;
            if (smokeCd <= 0f) { smokeCd = 0.4f; Fx.Puff(transform.position + Vector3.up + Random.insideUnitSphere * 3f, new Color(0.3f, 0.3f, 0.3f, 0.35f), 1, 0.5f, 1.4f, 3f, Vector3.up * 1.5f); }
            UpdateDome(dt);
            return;
        }

        UpdateBrewing(dt);
        UpdateDoors(dt);
        UpdateTurret(dt);
        UpdateDome(dt);

        // forcefield recharges a few seconds after it was last hit
        if (Has(ItemId.Forcefield) && Time.time >= fieldDownUntil && Time.time - lastFieldHit > 4f)
            fieldHp = Mathf.Min(fieldMax, fieldHp + 45f * dt);
        if (Has(ItemId.Forcefield) && fieldDownUntil > 0f && Time.time >= fieldDownUntil && fieldHp <= 0f) fieldHp = 1f;

        // damage smoke / sparks
        float k = hp / maxHp;
        smokeCd -= dt;
        if (smokeCd <= 0f && k < 0.6f)
        {
            smokeCd = k < 0.3f ? 0.15f : 0.4f;
            Vector3 roof = transform.TransformPoint(new Vector3(Random.Range(-5f, 5f), 5.6f, Random.Range(-4f, 4f)));
            Fx.Puff(roof, new Color(0.25f, 0.25f, 0.25f, 0.4f), 1, 0.3f, 0.9f, 2.5f, Vector3.up * 2f);
            if (k < 0.3f)
            {
                Fx.Burst(roof, new Color(1f, 0.5f, 0.1f), 3, 3f, 0.08f, 0.5f);
                if (Random.value < 0.2f) Fx.Burst(transform.TransformPoint(new Vector3(Random.Range(-7f, 7f), Random.Range(1f, 4f), 6.2f)), new Color(1f, 0.9f, 0.5f), 6, 4f, 0.05f, 0.4f);
            }
        }

        // the zone melts down labs it has passed
        if (!MLGame.ShieldsUp && MLGame.I != null && MLGame.I.OutsideZone(transform.position))
        {
            hp -= maxHp * 0.02f * dt;
            if (Random.value < dt * 4f) Fx.Burst(Center + Random.insideUnitSphere * 5f, new Color(0.9f, 0.3f, 1f), 4, 3f);
            if (hp <= 0f) Collapse(false);
            if (OwnedByPlayer && alarmCd <= 0f) { alarmCd = 6f; GameHUD.Warn("THE ZONE IS MELTING YOUR LAB!"); Sfx.Play("alarm", 0.5f); }
        }

        // little life: the enchanting crystal spins, lamps flicker when damaged
        if (enchantCrystal != null) enchantCrystal.Rotate(0f, 40f * dt, 0f, Space.World);
        if (k < 0.3f) foreach (Light l in lamps) if (l != null && !l.name.Contains("Beacon")) l.enabled = Random.value > 0.08f;
    }

    void UpdateDoors(float dt)
    {
        if (doorL == null || doorR == null) return;
        Player p = Player.Local;
        bool open = p != null && p.Alive && OwnedByPlayer && Vector3.Distance(p.transform.position, DoorPoint - transform.forward * 1.5f) < 5.5f;
        float was = doorOpen;
        doorOpen = Mathf.MoveTowards(doorOpen, open ? 1f : 0f, dt * 2.5f);
        if ((was == 0f && doorOpen > 0f) || (was == 1f && doorOpen < 1f)) Sfx.At("door", DoorPoint, 0.6f);
        float slide = Mathf.SmoothStep(0f, 1f, doorOpen) * 1.55f;
        doorL.localPosition = doorLHome + Vector3.left * slide;
        doorR.localPosition = doorRHome + Vector3.right * slide;
    }

    void UpdateTurret(float dt)
    {
        if (!Has(ItemId.Turret) || turretYaw == null || MLGame.ShieldsUp) return;
        turretCd -= dt;
        Vector3 from = turretMuzzle != null ? turretMuzzle.position : turretYaw.position + Vector3.up * 0.5f;
        IDamageable target = null;
        Vector3 aimAt = Vector3.zero;
        float best = 32f;
        foreach (RaidDrone d in RaidDrone.All)
        {
            if (d == null || !d.Alive) continue;
            float dist = Vector3.Distance(from, d.Center);
            if (dist < best && Clear(from, d.Center)) { best = dist; target = d; aimAt = d.Center + d.Velocity * (dist / 45f); }
        }
        // rival labs defend themselves against you if you've been attacking them
        Player p = Player.Local;
        if (target == null && p != null && p.Alive && p.labIndex != Index && Time.time - lastAttackedByPlayer < 20f)
        {
            float dist = Vector3.Distance(from, p.Center);
            if (dist < 30f && Clear(from, p.Center)) { target = p; aimAt = p.Center; }
        }
        Quaternion idle = Quaternion.Euler(0f, Time.time * 30f, 0f);
        if (target == null)
        {
            turretYaw.rotation = Quaternion.Slerp(turretYaw.rotation, transform.rotation * idle, dt * 2f);
            if (turretPitch != null) turretPitch.localRotation = Quaternion.Slerp(turretPitch.localRotation, Quaternion.identity, dt * 2f);
            return;
        }
        Vector3 to = aimAt - turretYaw.position;
        Vector3 flat = new Vector3(to.x, 0f, to.z);
        if (flat.sqrMagnitude > 0.01f) turretYaw.rotation = Quaternion.RotateTowards(turretYaw.rotation, Quaternion.LookRotation(flat), 240f * dt);
        if (turretPitch != null)
        {
            float pitch = -Mathf.Atan2(to.y, flat.magnitude) * Mathf.Rad2Deg;
            turretPitch.localRotation = Quaternion.RotateTowards(turretPitch.localRotation, Quaternion.Euler(pitch, 0f, 0f), 240f * dt);
        }
        if (turretCd <= 0f)
        {
            turretCd = 0.3f;
            Vector3 dir = ((aimAt - from).normalized + Random.insideUnitSphere * 0.03f).normalized;
            Projectile.Fire(Projectile.Kind.TurretBolt, from, dir * 45f, Index, Element.None, false);
            Fx.Flash(from, new Color(1f, 0.75f, 0.3f), 4f, 3f, 0.05f);
            Sfx.At("turret", from, 0.6f, Random.Range(0.95f, 1.05f));
        }
    }

    static bool Clear(Vector3 a, Vector3 b)
    {
        Vector3 d = b - a;
        if (!Physics.Raycast(a, d.normalized, out RaycastHit hit, d.magnitude, Combat.NotPlayer, QueryTriggerInteraction.Ignore)) return true;
        IDamageable t = Combat.Find(hit.collider);
        return t is RaidDrone;
    }

    // the glowing dome (starter shield = gold, forcefield = blue)
    void UpdateDome(float dt)
    {
        bool up = MLGame.Playing && FieldUp;
        if (dome == null && up)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.name = "Dome";
            dome = g.transform;
            dome.SetParent(transform, false);
            dome.localPosition = new Vector3(0f, 1.5f, 0f);
            dome.localScale = Vector3.one * DomeRadius * 2f;
            domeMat = new Material(Shader.Find("Sprites/Default"));
            domeMat.mainTexture = Gfx.HexTexture;
            domeMat.mainTextureScale = new Vector2(10f, 6f);
            domeMat.renderQueue = 3050;
            g.GetComponent<Renderer>().sharedMaterial = domeMat;
            g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Gfx.SetLayer(g, 2);
        }
        if (dome == null) return;
        dome.gameObject.SetActive(up);
        if (!up) return;
        fieldFlash = Mathf.MoveTowards(fieldFlash, 0f, dt * 3f);
        Color c = StarterShield ? new Color(1f, 0.85f, 0.4f) : Color.Lerp(new Color(0.4f, 0.85f, 1f), LabColor, 0.3f);
        float strength = StarterShield ? 1f : Mathf.Clamp01(fieldHp / fieldMax);
        float a = (0.06f + 0.05f * strength + 0.25f * fieldFlash) * (0.85f + 0.15f * Mathf.Sin(Time.time * 3f + Index));
        domeMat.color = new Color(c.r, c.g, c.b, a);
        domeMat.mainTextureOffset = new Vector2(Time.time * 0.01f, Time.time * 0.02f);
    }
}

// A crafting station in a lab. Press E next to it (inside your own lab) to open its menu.
public class LabStation : MonoBehaviour, IInteractable
{
    public Laboratory lab;
    public Station type;

    public static readonly string[] Names = { "Brewing Stand", "Workbench", "Enchanting Table", "Storage", "Lab Terminal" };
    public string Name => Names[(int)type];

    void OnEnable() { if (!Interactables.All.Contains(this)) Interactables.All.Add(this); }
    void OnDisable() => Interactables.All.Remove(this);

    public Vector3 InteractPoint => transform.position + Vector3.up * 0.5f;
    public float InteractRange => 2.8f;

    public string Prompt(Player p, out bool usable)
    {
        usable = false;
        if (lab == null) return null;
        if (lab.Destroyed) return Name + " (destroyed)";
        if (lab.Index != p.labIndex) return "Not your lab! You can only craft in yours.";
        if (!p.InsideOwnLab) return null;
        usable = true;
        return "Use " + Name;
    }

    public void Use(Player p)
    {
        p.Model.PlayPoke();
        GameHUD.OpenStation(this);
        Sfx.Play("station_open", 0.5f);
    }
}
