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
