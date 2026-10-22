using System.Collections.Generic;
using UnityEngine;

// THINGS GUNS LEAVE BEHIND (see the 'field', 'strike' and 'turret' settings in Guns.cs):
//   DamageField   a patch on the ground (fire, acid, ice, a black hole...) that hurts enemies standing in it
//   StrikeMarker  a glowing target circle - after a moment, something big lands on it (airstrike / lightning)
//   MiniTurret    a little turret you throw down; it shoots enemies near it for a while

public static class GunToys
{
    // Everything hostile near a point (dummies, labs, other players online, drones) + the local player if hostile.
    public static List<IDamageable> Near(Vector3 pos, float radius, int team)
    {
        var list = new List<IDamageable>();
        foreach (Collider c in Physics.OverlapSphere(pos, radius, ~0, QueryTriggerInteraction.Ignore))
        {
            IDamageable d = Combat.Find(c);
            if (d == null || list.Contains(d) || !d.Alive || !Teams.Hostile(team, d.Team)) continue;
            list.Add(d);
        }
        Player p = Player.Local;
        if (p != null && p.Alive && Teams.Hostile(team, p.Team) && !list.Contains(p) && Vector3.Distance(p.Center, pos) < radius + 0.5f) list.Add(p);
        return list;
    }

    // Where a point on the ground is (for things that land).
    public static Vector3 Ground(Vector3 p)
    {
        if (Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 30f, Combat.NotPlayer, QueryTriggerInteraction.Ignore)) return hit.point;
        return MLGame.Ground(p);
    }
}

// A damaging patch on the ground.
public class DamageField : MonoBehaviour
{
    int team;
    float radius, dps, life, tick, pulse, structure;
    bool fromPlayer, pull;
    Element element;
    Color color;
    Transform disc;
    int ticks;

    public static void Spawn(Vector3 pos, float radius, float time, float dps, Element element, Color color, int team, bool fromPlayer, float structure, bool pull = false)
    {
        var go = new GameObject("Damage Field");
        go.transform.position = GunToys.Ground(pos) + Vector3.up * 0.05f;
        var f = go.AddComponent<DamageField>();
        f.team = team; f.radius = radius; f.life = time; f.dps = dps; f.element = element; f.color = color;
        f.fromPlayer = fromPlayer; f.structure = structure; f.pull = pull;
        f.disc = Gfx.Part(go.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(radius * 2f, 0.02f, radius * 2f), Gfx.Clear(new Color(color.r, color.g, color.b, 0.35f)), default, false).transform;
        Gfx.Part(go.transform, PrimitiveType.Cylinder, Vector3.up * 0.01f, new Vector3(radius * 1.2f, 0.02f, radius * 1.2f), Gfx.Glow(color, 1.2f), default, false);
        Gfx.SetLayer(go, 2);
        if (pull) Sfx.At("zone", go.transform.position, 0.6f, 1.6f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        life -= dt;
        pulse += dt;
        if (life <= 0f) { Destroy(gameObject); return; }
        float s = 1f + Mathf.Sin(pulse * 6f) * 0.04f;
        disc.localScale = new Vector3(radius * 2f * s, 0.02f, radius * 2f * s);
        if (life < 0.6f) transform.localScale = Vector3.one * Mathf.Max(0.01f, life / 0.6f);
        // look: flames / bubbles / sparkles rising from it
        if (Random.value < dt * 18f)
        {
            Vector3 p = transform.position + new Vector3(Random.Range(-radius, radius), 0.1f, Random.Range(-radius, radius)) * 0.8f;
            if (element == Element.Fire) Fx.Burst(p, Color.Lerp(color, new Color(1f, 0.9f, 0.3f), Random.value), 1, 1.5f, 0.2f, 0.6f, 3f);
            else Fx.Puff(p, new Color(color.r, color.g, color.b, 0.4f), 1, 0.1f, 0.25f, 0.9f, Vector3.up * 0.8f);
        }
        if (pull)
        {
            // black hole: things spiral in
            for (int i = 0; i < 2; i++)
            {
                Vector3 a = transform.position + Quaternion.Euler(0f, Random.value * 360f, 0f) * Vector3.forward * radius + Vector3.up * Random.Range(0.2f, 2f);
                Fx.Beam(a, transform.position + Vector3.up, new Color(color.r, color.g, color.b, 0.4f), 0.03f, 0.1f, false);
            }
            Player me = Player.Local;
            if (me != null && me.Alive && Teams.Hostile(team, me.Team) && Vector3.Distance(me.transform.position, transform.position) < radius * 1.6f)
                me.Knock((transform.position - me.transform.position).normalized * 12f * dt);
        }
        tick -= dt;
        if (tick > 0f) return;
        tick = 0.5f;
        ticks++;
        foreach (IDamageable d in GunToys.Near(transform.position + Vector3.up * 0.8f, radius, team))
            d.TakeHit(new Hit { damage = dps * 0.5f, point = d.Center, dir = Vector3.up, force = 0f, element = element, team = team, structureMul = structure, fromPlayer = fromPlayer, dot = ticks % 2 == 1 });
    }
}

// "Something is coming down here!" - then a big hit.
public class StrikeMarker : MonoBehaviour
{
    float delay, damage, radius, structure;
    int team;
    bool fromPlayer;
    Element element;
    Color color;
    float t;
    Transform ring;

    public static void Spawn(Vector3 pos, float delay, float damage, float radius, Element element, Color color, int team, bool fromPlayer, float structure)
    {
        var go = new GameObject("Strike Marker");
        go.transform.position = GunToys.Ground(pos) + Vector3.up * 0.06f;
        var m = go.AddComponent<StrikeMarker>();
        m.delay = delay; m.damage = damage; m.radius = radius; m.element = element; m.color = color; m.team = team; m.fromPlayer = fromPlayer; m.structure = structure;
        m.ring = Gfx.Part(go.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(radius * 2f, 0.02f, radius * 2f), Gfx.Clear(new Color(color.r, color.g, color.b, 0.3f)), default, false).transform;
        Gfx.Part(go.transform, PrimitiveType.Cylinder, Vector3.up * 0.01f, new Vector3(0.6f, 0.02f, 0.6f), Gfx.Glow(color, 3f), default, false);
        Gfx.SetLayer(go, 2);
        Sfx.At("drone_alarm", go.transform.position, 0.5f, 1.5f);
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / delay);
        ring.localScale = new Vector3(radius * 2f * (1f - k * 0.85f), 0.02f, radius * 2f * (1f - k * 0.85f));
        if (Random.value < 0.4f) Fx.Beam(transform.position + Vector3.up * 40f, transform.position, new Color(color.r, color.g, color.b, 0.15f + 0.3f * k), 0.05f + 0.1f * k, 0.05f, false);
        if (t < delay) return;
        Vector3 p = transform.position;
        Fx.Beam(p + Vector3.up * 60f, p, color, 0.6f, 0.35f, element == Element.Shock);
        Fx.Beam(p + Vector3.up * 60f, p, Color.white, 0.25f, 0.25f, element == Element.Shock);
        Combat.Blast(p + Vector3.up * 0.4f, radius, damage, 12f, team, element, structure, fromPlayer, color);
        if (element == Element.Shock) Sfx.At("zap", p, 1f, 0.5f, 80f);
        Destroy(gameObject);
    }
}

// A little turret that shoots enemies near it.
public class MiniTurret : MonoBehaviour
{
    int team;
    float life, cd, damage;
    Element element;
    Color color;
    Transform head;
    static GunDef bolt;

    public static void Spawn(Vector3 pos, float yaw, int team, float damage, Element element, Color color, float life)
    {
        var go = new GameObject("Mini Turret");
        go.transform.position = GunToys.Ground(pos);
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        var t = go.AddComponent<MiniTurret>();
        t.team = team; t.damage = damage; t.element = element; t.color = color; t.life = life;
        Material dark = Gfx.Lit(new Color(0.2f, 0.22f, 0.25f), 0.5f, 0.6f);
        Gfx.Part(go.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.15f, 0f), new Vector3(0.7f, 0.15f, 0.7f), dark);
        Gfx.Part(go.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f), new Vector3(0.18f, 0.2f, 0.18f), dark);
        t.head = Gfx.Pivot(go.transform, new Vector3(0f, 0.75f, 0f), "Head");
        Gfx.Part(t.head, PrimitiveType.Cube, Vector3.zero, new Vector3(0.35f, 0.25f, 0.4f), Gfx.Lit(color * 0.6f, 0.5f, 0.5f));
        Gfx.Part(t.head, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.35f), new Vector3(0.07f, 0.2f, 0.07f), dark, new Vector3(90f, 0f, 0f));
        Gfx.Part(t.head, PrimitiveType.Sphere, new Vector3(0f, 0.15f, 0f), Vector3.one * 0.1f, Gfx.Glow(color, 3f));
        Gfx.SetLayer(go, 2);
        Fx.Burst(go.transform.position + Vector3.up * 0.5f, color, 14, 3f, 0.1f, 0.5f);
        Sfx.At("upgrade", go.transform.position, 0.6f, 1.3f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        life -= dt;
        if (life <= 0f)
        {
            Fx.Burst(transform.position + Vector3.up * 0.5f, color, 12, 3f, 0.1f, 0.5f);
            Destroy(gameObject);
            return;
        }
        // shields up = labs can't be hurt yet, so only aim at things that can
        IDamageable best = null;
        float bestD = 26f;
        foreach (IDamageable d in GunToys.Near(head.position, 26f, team))
        {
            if (d is Laboratory && MLGame.ShieldsUp) continue;
            float dist = Vector3.Distance(head.position, d.Center);
            if (dist < bestD) { bestD = dist; best = d; }
        }
        if (best == null) { head.Rotate(0f, 40f * dt, 0f, Space.World); return; }
        Vector3 to = best.Center - head.position;
        head.rotation = Quaternion.RotateTowards(head.rotation, Quaternion.LookRotation(to), 360f * dt);
        cd -= dt;
        if (cd > 0f) return;
        cd = 0.35f;
        if (bolt == null) bolt = new GunDef { mode = GunMode.Projectile, speed = 50f, size = 0.12f, force = 0.5f };
        GunDef b = bolt.Clone();
        b.damage = damage;
        b.element = element;
        Vector3 from = head.position + head.forward * 0.45f;
        Projectile.FireGun(b, from, (best.Center - from).normalized * b.speed, team, element, color, team == (Player.Local != null ? Player.Local.Team : -99));
        Fx.Flash(from, color, 3f, 2f, 0.05f);
        Sfx.At("turret", from, 0.4f, 1.4f);
    }
}

// ================================================================
// MORE THINGS WEAPONS MAKE (the big weapon list): marks, walls, decoys, heal circles, orbiting orbs
// ================================================================

// MARKED targets take extra damage for a while (spotter scopes, "marks the target", reveal pulses...).
public static class Marks
{
    class Mark { public float until, bonus; }
    static readonly Dictionary<IDamageable, Mark> marks = new Dictionary<IDamageable, Mark>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => marks.Clear();

    public static void Add(IDamageable d, float bonus, float time = 6f)
    {
        if (d == null || bonus <= 0f) return;
        if (marks.TryGetValue(d, out Mark m)) { m.until = Mathf.Max(m.until, Time.time + time); m.bonus = Mathf.Max(m.bonus, bonus); }
        else marks[d] = new Mark { until = Time.time + time, bonus = bonus };
        Fx.Ring(d.Center + Vector3.up * 0.9f, new Color(1f, 0.3f, 0.3f, 0.8f), 10, 2f, 0.08f);
    }

    // Damage multiplier for hitting this target (1 = not marked).
    public static float Bonus(IDamageable d)
    {
        if (d == null || !marks.TryGetValue(d, out Mark m)) return 1f;
        if (Time.time > m.until || !d.Alive) { marks.Remove(d); return 1f; }
        if (Random.value < 0.3f) Fx.Burst(d.Center + Vector3.up * 1.1f, new Color(1f, 0.25f, 0.25f), 1, 0.5f, 0.08f, 0.3f, 0f);
        return 1f + m.bonus;
    }
}

// A wall that pops up in front of you and blocks shots (and people) for a while.
public class Barrier : MonoBehaviour
{
    float life, total;
    Material mat;
    Color color;

    public static void Spawn(Vector3 pos, float yaw, float time, Color c)
    {
        var go = new GameObject("Barrier");
        go.transform.position = GunToys.Ground(pos);
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        var b = go.AddComponent<Barrier>();
        b.life = b.total = Mathf.Max(2f, time);
        b.color = c;
        b.mat = Gfx.Clear(new Color(c.r, c.g, c.b, 0.45f));
        GameObject wall = Gfx.Part(go.transform, PrimitiveType.Cube, new Vector3(0f, 1.3f, 0f), new Vector3(3.4f, 2.6f, 0.3f), b.mat, default, false);
        wall.AddComponent<BoxCollider>();
        Gfx.Part(go.transform, PrimitiveType.Cube, new Vector3(0f, 2.62f, 0f), new Vector3(3.5f, 0.08f, 0.34f), Gfx.Glow(c, 2f), default, false);
        Gfx.Part(go.transform, PrimitiveType.Cube, new Vector3(0f, 0.04f, 0f), new Vector3(3.5f, 0.08f, 0.34f), Gfx.Glow(c, 2f), default, false);
        Sfx.At("shield_hit", go.transform.position, 0.6f, 1.3f);
        Fx.Burst(go.transform.position + Vector3.up, c, 20, 4f, 0.1f, 0.5f, 0f);
    }

    void Update()
    {
        life -= Time.deltaTime;
        if (mat != null) mat.color = new Color(color.r, color.g, color.b, 0.25f + 0.2f * Mathf.Clamp01(life / 1f) + 0.05f * Mathf.Sin(Time.time * 6f));
        if (life <= 0f)
        {
            Fx.Burst(transform.position + Vector3.up * 1.3f, color, 16, 3f, 0.1f, 0.4f);
            Destroy(gameObject);
        }
    }
}

// A fake scientist that stands there... and then explodes.
public class Decoy : MonoBehaviour
{
    float life;
    int team;
    float damage;
    Element element;
    Color color;

    public static void Spawn(Vector3 pos, float yaw, int team, float damage, Element element, Color c, float time)
    {
        var go = new GameObject("Decoy");
        go.transform.position = GunToys.Ground(pos);
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        var d = go.AddComponent<Decoy>();
        d.life = Mathf.Max(1.5f, time);
        d.team = team; d.damage = damage; d.element = element; d.color = c;
        Scientist s = Scientist.Create(go.transform, c, Random.Range(0, 99));
        s.aimYaw = yaw;
        Fx.Puff(go.transform.position + Vector3.up, new Color(c.r, c.g, c.b, 0.5f), 10, 0.6f, 0.6f, 0.8f, Vector3.up * 0.5f);
        Sfx.At("pop", go.transform.position, 0.6f, 0.8f);
    }

    void Update()
    {
        life -= Time.deltaTime;
        if (life < 1f && Random.value < 0.4f) Fx.Burst(transform.position + Vector3.up, color, 2, 2f, 0.08f, 0.3f, 0f);
        if (life <= 0f)
        {
            Combat.Blast(transform.position + Vector3.up, 3.2f, damage, 7f, team, element, 1.5f, true, color);
            Destroy(gameObject);
        }
    }
}

// A glowing circle that heals the player who made it while they stand in it.
public class HealZone : MonoBehaviour
{
    float life, radius;
    int team;

    public static void Spawn(Vector3 pos, float radius, float time, int team)
    {
        var go = new GameObject("Heal Zone");
        go.transform.position = GunToys.Ground(pos) + Vector3.up * 0.05f;
        var z = go.AddComponent<HealZone>();
        z.life = Mathf.Max(2f, time); z.radius = radius; z.team = team;
        Color c = new Color(0.4f, 1f, 0.5f);
        Gfx.Part(go.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(radius * 2f, 0.02f, radius * 2f), Gfx.Clear(new Color(c.r, c.g, c.b, 0.3f)), default, false);
        Gfx.Part(go.transform, PrimitiveType.Cylinder, Vector3.up * 0.01f, new Vector3(radius * 0.6f, 0.02f, radius * 0.6f), Gfx.Glow(c, 1.5f), default, false);
        Gfx.SetLayer(go, 2);
        Sfx.At("potion_ready", go.transform.position, 0.6f, 1.2f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        life -= dt;
        Player p = Player.Local;
        if (p != null && p.Alive && p.Team == team)
        {
            Vector3 d = p.transform.position - transform.position;
            if (new Vector2(d.x, d.z).magnitude < radius && Mathf.Abs(d.y) < 2f) p.HealFrom(9f * dt);
        }
        if (Random.value < 0.3f) Fx.Burst(transform.position + new Vector3(Random.Range(-radius, radius) * 0.7f, 0.1f, Random.Range(-radius, radius) * 0.7f), new Color(0.5f, 1f, 0.6f), 1, 1f, 0.07f, 0.8f, 2f);
        if (life <= 0f) Destroy(gameObject);
    }
}

// Glowing orbs that circle around you and hurt enemies they touch.
public class Orbiters : MonoBehaviour
{
    readonly List<Transform> orbs = new List<Transform>();
    Transform owner;
    float life, damage, angle, hitCd;
    int team;
    Element element;
    Color color;

    public static void Spawn(Transform owner, int count, float time, float damage, int team, Element element, Color c)
    {
        var go = new GameObject("Orbiters");
        var o = go.AddComponent<Orbiters>();
        o.owner = owner; o.life = Mathf.Max(3f, time); o.damage = damage; o.team = team; o.element = element; o.color = c;
        for (int i = 0; i < Mathf.Clamp(count, 1, 8); i++)
            o.orbs.Add(Gfx.Part(go.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.3f, Gfx.Glow(c, 3f), default, false).transform);
        Gfx.SetLayer(go, 2);
        Sfx.Play("potion_ready", 0.5f, 1.5f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        life -= dt;
        if (owner == null || life <= 0f) { Destroy(gameObject); return; }
        angle += dt * 220f;
        Vector3 c = owner.position + Vector3.up * 1.1f;
        for (int i = 0; i < orbs.Count; i++)
        {
            float a = (angle + i * 360f / orbs.Count) * Mathf.Deg2Rad;
            orbs[i].position = c + new Vector3(Mathf.Cos(a), Mathf.Sin(a * 2f) * 0.15f, Mathf.Sin(a)) * 1.7f;
        }
        hitCd -= dt;
        if (hitCd > 0f) return;
        hitCd = 0.25f;
        foreach (Transform orb in orbs)
            foreach (IDamageable d in GunToys.Near(orb.position, 0.9f, team))
            {
                if (d is Player) continue;
                d.TakeHit(new Hit { damage = damage, point = orb.position, dir = (d.Center - c).normalized, force = 1.5f, element = element, team = team, structureMul = 1f, fromPlayer = true });
                Fx.Burst(orb.position, color, 5, 2f, 0.06f, 0.25f);
            }
    }
}
