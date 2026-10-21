using System.Collections.Generic;
using UnityEngine;

// MATERIAL SPOTS: the glowing herb plants, slime pools, crystal clusters, scrap piles and chemical barrels on the map.
// Walk up and press E to gather (2 at a time). Each spot has 3 gathers, then it regrows after a while.
// They're prefabs in Assets/MadLab/Prefabs (Node_Herb, Node_Slime, ...) - drag more into your map anywhere you like.
// ONLINE: the host is in charge of every spot. If anyone gathers, it shrinks (and regrows) for everybody - see NetNodes.cs.
public class MaterialNode : MonoBehaviour, IInteractable
{
    public static readonly List<MaterialNode> All = new List<MaterialNode>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => All.Clear();

    public Mat mat;
    [Tooltip("How many times you can gather before it needs to regrow.")]
    public int charges = 3;
    [Tooltip("How much you get each time.")]
    public int amount = 2;
    [Tooltip("Seconds to grow back after it's used up.")]
    public float regrowTime = 35f;

    int left;
    float regrowAt = -1f;
    [System.NonSerialized] public int netId = -1;     // same number on every computer (set by NetNodes)
    Vector3 baseScale;
    Light glow;
    float pulse;

    public bool Available => left > 0;
    public int Left => left;
    public float RegrowIn => regrowAt > 0f ? Mathf.Max(0f, regrowAt - Time.time) : -1f;
    public Vector3 InteractPoint => transform.position + Vector3.up * 0.6f;
    public float InteractRange => 2.6f;

    void Awake()
    {
        baseScale = transform.localScale;
        left = charges;
        glow = GetComponentInChildren<Light>();
    }

    void OnEnable() { All.Add(this); Interactables.All.Add(this); }
    void OnDisable() { All.Remove(this); Interactables.All.Remove(this); }

    public void ResetNode()
    {
        left = charges;
        regrowAt = -1f;
        transform.localScale = baseScale;
        if (glow != null) glow.enabled = true;
    }

    public string Prompt(Player p, out bool usable)
    {
        usable = Available && p.GatherProgress < 0f;
        string n = Items.MatNames[(int)mat];
        if (!Available) return n + " is regrowing...";
        return "Gather " + n + "  (" + left + " left)";
    }

    public void Use(Player p) { if (Available) p.StartGather(this); }

    // Your scientist finished the gather animation.
    public void Harvest(Player p)
    {
        if (!Available) return;
        if (Net.IsClient && netId >= 0) { NetNodes.RequestGather(this); return; }     // ask the host
        TakeCharge();
        Give(p);
        NetNodes.Changed(this);
    }

    // One gather's worth comes off the spot (host / solo only).
    public void TakeCharge()
    {
        if (left <= 0) return;
        left--;
        if (left <= 0) regrowAt = Time.time + regrowTime;
        Burst();
    }

    public void Give(Player p)
    {
        p.AddMaterial((int)mat, amount);
        Sfx.At("pickup_" + Items.MatSound[(int)mat], InteractPoint, 0.8f);
        Sfx.Play("pickup", 0.5f, 1f + left * 0.08f);
    }

    void Burst() => Fx.Burst(InteractPoint, Items.MatColors[(int)mat], 14, 3.5f, 0.09f, 0.6f, -6f);

    // The host told us how this spot looks now.
    public void SetState(int newLeft, float regrowIn)
    {
        newLeft = Mathf.Clamp(newLeft, 0, charges);
        if (newLeft < left) Burst();                                   // someone gathered it
        else if (newLeft > left && newLeft == charges && left == 0) Fx.Burst(InteractPoint, Items.MatColors[(int)mat], 10, 2f, 0.08f, 0.6f, 1f);
        left = newLeft;
        regrowAt = regrowIn >= 0f ? Time.time + regrowIn : -1f;
    }

    void Update()
    {
        float target = left <= 0 ? 0.25f : 0.55f + 0.45f * left / Mathf.Max(1, charges);
        float s = Mathf.MoveTowards(transform.localScale.x / Mathf.Max(0.001f, baseScale.x), target, Time.deltaTime * 2f);
        transform.localScale = baseScale * s;
        if (glow != null)
        {
            pulse += Time.deltaTime;
            glow.enabled = left > 0;
            glow.intensity = 1.2f + 0.4f * Mathf.Sin(pulse * 2.5f);
        }
        if (regrowAt > 0f && Time.time >= regrowAt && !Net.IsClient)      // online, only the host regrows spots
        {
            left = charges;
            regrowAt = -1f;
            Fx.Burst(InteractPoint, Items.MatColors[(int)mat], 10, 2f, 0.08f, 0.6f, 1f);
            NetNodes.Changed(this);
        }
        // sparkle so you can spot them
        if (left > 0 && Random.value < Time.deltaTime * 1.5f)
            Fx.Burst(InteractPoint + Random.insideUnitSphere * 0.5f, Items.MatColors[(int)mat], 1, 0.6f, 0.05f, 0.9f, 0.5f);
    }
}

// A floating glowing bit of material on the ground (from a knocked-out scientist, a destroyed lab, a drone).
// Walk over it to grab it.
public class MaterialPickup : MonoBehaviour
{
    public static readonly List<MaterialPickup> All = new List<MaterialPickup>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => All.Clear();

    int mat, amount;
    Vector3 vel;
    bool landed;
    float t, ground;
    Transform look;

    public static void Drop(Vector3 pos, int mat, int amount = 1)
    {
        var go = new GameObject("Pickup " + Items.MatNames[mat]);
        go.transform.position = pos;
        var p = go.AddComponent<MaterialPickup>();
        p.mat = mat;
        p.amount = amount;
        p.vel = new Vector3(Random.Range(-3f, 3f), Random.Range(3f, 6f), Random.Range(-3f, 3f));
        p.t = Random.value * 10f;
        Color c = Items.MatColors[mat];
        p.look = new GameObject("look").transform;
        p.look.SetParent(go.transform, false);
        string snd = Items.MatSound[mat];
        PrimitiveType shape = snd == "crystal" ? PrimitiveType.Capsule : snd == "metal" ? PrimitiveType.Cube : PrimitiveType.Sphere;
        float size = 0.22f + 0.05f * amount;
        Gfx.Part(p.look, shape, Vector3.zero, Vector3.one * size, Gfx.Glow(c, 1.8f), new Vector3(20f, 0f, 30f), false);
        Gfx.Part(p.look, PrimitiveType.Sphere, Vector3.zero, Vector3.one * size * 2f, Gfx.Clear(new Color(c.r, c.g, c.b, 0.18f)), default, false);
        Gfx.SetLayer(go, 2);
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;
        if (!landed)
        {
            vel.y -= 18f * dt;
            Vector3 next = transform.position + vel * dt;
            if (Physics.Raycast(transform.position, (next - transform.position).normalized, out RaycastHit hit, (next - transform.position).magnitude + 0.3f, Combat.NotPlayer, QueryTriggerInteraction.Ignore) && vel.y < 0f)
            {
                landed = true;
                ground = hit.point.y + 0.45f;
                next = new Vector3(next.x, ground, next.z);
            }
            transform.position = next;
            if (transform.position.y < -50f) Destroy(gameObject);
        }
        else
        {
            Vector3 p = transform.position;
            p.y = ground + Mathf.Sin(t * 2.5f) * 0.12f;
            transform.position = p;
        }
        look.rotation = Quaternion.Euler(0f, t * 90f, 0f);

        Player pl = Player.Local;
        if (pl == null || !pl.Alive || t < 0.5f) return;
        Vector3 to = pl.Center - transform.position;
        float d = to.magnitude;
        if (d < 4f)
        {
            // gets pulled towards you
            transform.position += to.normalized * Mathf.Min(d, (5f - d) * 4f * dt);
            landed = true;
            ground = transform.position.y;
        }
        if (d < 1.2f)
        {
            pl.AddMaterial(mat, amount);
            Sfx.Play("pickup", 0.45f, Random.Range(1.1f, 1.3f));
            Fx.Burst(transform.position, Items.MatColors[mat], 6, 2f, 0.06f, 0.3f);
            Destroy(gameObject);
        }
    }
}
