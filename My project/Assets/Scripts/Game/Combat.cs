using System.Collections.Generic;
using UnityEngine;

// DAMAGE: anything that can be hurt (players, labs, dummies, drones), shots, explosions, slime and poison.

public static class Teams
{
    public const int Neutral = -1;      // target dummies: everyone can hit them
    public const int Drones = 100;      // the raid drones
    public static bool Hostile(int a, int b) => a != b || a == Neutral;
}

public struct Hit
{
    public float damage;
    public Vector3 point, dir;
    public float force;                 // knockback
    public Element element;
    public int team;                    // who did it
    public float structureMul;          // extra damage to labs (explosions smash walls)
    public bool dot;                    // burn / poison ticks (no flinch)
    public bool fromPlayer;             // show a hit marker for the local player
}

public interface IDamageable
{
    int Team { get; }
    bool Alive { get; }
    Vector3 Center { get; }
    void TakeHit(Hit h);
}

// Burning, poisoned, slowed, frozen.
public class Status
{
    public float burn, poison, slow, freeze, chill;
    public int poisonStacks;
    public int lastTeam = Teams.Neutral;
    float tick;

    public bool Any => burn > 0f || poison > 0f || slow > 0f || freeze > 0f;
    public float SpeedMul => freeze > 0f ? 0.05f : slow > 0f ? 0.5f : 1f;

    public void Apply(Element e, int team)
    {
        lastTeam = team;
        switch (e)
        {
            case Element.Fire: burn = 3f; break;
            case Element.Ice: slow = Mathf.Max(slow, 2.5f); chill += 0.35f; break;
            case Element.Poison: poison = 5f; poisonStacks = Mathf.Min(3, poisonStacks + 1); break;
            case Element.Slime: slow = Mathf.Max(slow, 1.2f); break;
            case Element.Shock: slow = Mathf.Max(slow, 0.5f); break;      // a short jolt
        }
        if (chill >= 1f) { chill = 0f; freeze = 1.3f; }
    }

    // Freeze Ray: chills over time, then freezes solid.
    public void Chill(float amount, int team)
    {
        lastTeam = team;
        slow = Mathf.Max(slow, 1f);
        chill += amount;
        if (chill >= 1f) { chill = 0f; freeze = 1.3f; }
    }

    // Damage from burning / poison this frame (dealt in 0.5 s ticks).
    public float Tick(float dt)
    {
        burn = Mathf.Max(0f, burn - dt);
        poison = Mathf.Max(0f, poison - dt);
        slow = Mathf.Max(0f, slow - dt);
        freeze = Mathf.Max(0f, freeze - dt);
        chill = Mathf.Max(0f, chill - dt * 0.4f);
        if (poison <= 0f) poisonStacks = 0;
        tick -= dt;
        if (tick > 0f) return 0f;
        tick = 0.5f;
        float d = 0f;
        if (burn > 0f) d += 3f;
        if (poison > 0f) d += 1.5f * poisonStacks;
        return d;
    }

    public void Clear() { burn = poison = slow = freeze = chill = 0f; poisonStacks = 0; }

    // Little visual bits for whoever has the status.
    public void Show(Vector3 center)
    {
        if (burn > 0f && Random.value < 0.4f) Fx.Burst(center + Random.insideUnitSphere * 0.3f, new Color(1f, 0.5f, 0.1f), 1, 1.5f, 0.08f, 0.5f, 3f);
        if (poison > 0f && Random.value < 0.3f) Fx.Puff(center + Random.insideUnitSphere * 0.3f, new Color(0.5f, 1f, 0.2f, 0.5f), 1, 0.1f, 0.12f, 0.8f, Vector3.up * 0.6f);
        if ((freeze > 0f || slow > 0f) && Random.value < 0.25f) Fx.Burst(center + Random.insideUnitSphere * 0.4f, new Color(0.7f, 0.95f, 1f), 1, 0.5f, 0.06f, 0.6f, -1f);
    }
}

public static class Combat
{
    public const int NotPlayer = ~(1 << 2);      // everything except the "Ignore Raycast" layer (players)

    public static IDamageable Find(Collider c) => c == null ? null : c.GetComponentInParent<IDamageable>();

    // Nearest point on a collider (only some kinds of collider can do this exactly).
    public static Vector3 Closest(Collider c, Vector3 p)
    {
        if (c is BoxCollider || c is SphereCollider || c is CapsuleCollider || (c is MeshCollider mc && mc.convex)) return c.ClosestPoint(p);
        return c.bounds.ClosestPoint(p);
    }

    // Big boom: damages everything nearby, pushes things away.
    public static void Blast(Vector3 pos, float radius, float damage, float force, int team, Element ench, float structureMul, bool fromPlayer, Color color)
    {
        Fx.Burst(pos, color, 26, 9f, 0.16f, 0.7f);
        Fx.Burst(pos, Color.Lerp(color, Color.white, 0.6f), 10, 5f, 0.22f, 0.4f, -4f);
        Fx.Puff(pos, new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.5f, 0.45f), 8, radius * 0.25f, radius * 0.45f, 1.2f, Vector3.up * 1.5f);
        Fx.Ring(pos, new Color(color.r, color.g, color.b, 0.6f), 16, radius * 3f, 0.4f);
        Fx.Flash(pos, color, radius * 4f, 6f, 0.25f);
        Sfx.At("explode", pos, 1f, Random.Range(0.9f, 1.1f), 70f);
        GameCam.Shake(pos, 0.5f);

        var done = new HashSet<IDamageable>();
        foreach (Collider c in Physics.OverlapSphere(pos, radius, ~0, QueryTriggerInteraction.Ignore))
        {
            IDamageable d = Find(c);
            if (d != null && !done.Contains(d) && d.Alive && Teams.Hostile(team, d.Team))
            {
                done.Add(d);
                Vector3 cp = Closest(c, pos);
                float dist = Vector3.Distance(pos, cp);
                float k = 1f - Mathf.Clamp01(dist / radius) * 0.6f;
                Vector3 dir = (d.Center - pos);
                dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.up;
                d.TakeHit(new Hit { damage = damage * k, point = cp, dir = dir, force = force * k, element = ench, team = team, structureMul = structureMul, fromPlayer = fromPlayer });
            }
            Rigidbody rb = c.attachedRigidbody;
            if (rb != null && !rb.isKinematic) rb.AddExplosionForce(force * 40f, pos, radius * 1.5f, 1f);
        }
    }
}

// ---------------------------------------------------------------
// Things that fly: plasma balls, slime globs, toxic bombs, drone lasers, turret bolts.
// ---------------------------------------------------------------
public class Projectile : MonoBehaviour
{
    public enum Kind { Plasma, Slime, Toxic, Laser, TurretBolt, Gun }

    Kind kind;
    Vector3 vel;
    float gravity, life, damage, radius;
    int team;
    Element ench;
    bool fromPlayer;
    Color color;
    Transform look;
    float trailT;
    GunDef gun;                 // Kind.Gun: which gun fired it (blast, puddle, structure damage...)
    Element element;            // what it does on hit (the gun's own element, or the enchant)

    // A shot from one of the table guns (Guns.cs).
    public static void FireGun(GunDef g, Vector3 pos, Vector3 vel, int team, Element element, Color color, bool fromPlayer)
    {
        var go = new GameObject("shot " + g.id);
        go.transform.position = pos;
        var p = go.AddComponent<Projectile>();
        p.kind = Kind.Gun;
        p.gun = g;
        p.vel = vel;
        p.team = team;
        p.ench = element;
        p.element = element;
        p.fromPlayer = fromPlayer;
        p.life = g.life;
        p.damage = g.damage;
        p.radius = Mathf.Max(0.08f, g.size * 0.6f);
        p.gravity = g.gravity;
        p.color = color;
        if (g.star)
        {
            p.look = Gfx.Part(go.transform, PrimitiveType.Cube, Vector3.zero, Vector3.one * g.size, Gfx.Glow(color, 4f), new Vector3(45f, 0f, 45f), false).transform;
            Gfx.Part(p.look, PrimitiveType.Cube, Vector3.zero, Vector3.one, Gfx.Glow(Color.white, 3f), new Vector3(0f, 45f, 0f), false);
        }
        else if (g.blast > 0f)
        {
            p.look = Gfx.Part(go.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.one * g.size, Gfx.Glow(color, 3f), default, false).transform;
            Gfx.Part(go.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.one * g.size * 1.6f, Gfx.Clear(new Color(color.r, color.g, color.b, 0.3f)), default, false);
            if (g.size >= 0.3f) Gfx.AddLight(go.transform, Vector3.zero, color, 5f, 2f);
        }
        else if (g.gravity < -4f)
            p.look = Gfx.Part(go.transform, PrimitiveType.Sphere, Vector3.zero, new Vector3(g.size, g.size * 0.85f, g.size * 1.2f), Gfx.Glow(color, 1.5f), default, false).transform;
        else
            p.look = Gfx.Part(go.transform, PrimitiveType.Capsule, Vector3.zero, new Vector3(g.size * 0.7f, g.size * 2.2f, g.size * 0.7f), Gfx.Glow(color, 3.5f), new Vector3(90f, 0f, 0f), false).transform;
        Gfx.SetLayer(go, 2);
        if (vel.sqrMagnitude > 0.01f) go.transform.rotation = Quaternion.LookRotation(vel);
    }

    public static void Fire(Kind kind, Vector3 pos, Vector3 vel, int team, Element ench, bool fromPlayer)
    {
        var go = new GameObject("shot " + kind);
        go.transform.position = pos;
        var p = go.AddComponent<Projectile>();
        p.kind = kind;
        p.vel = vel;
        p.team = team;
        p.ench = ench;
        p.fromPlayer = fromPlayer;
        p.life = 4f;
        Color ec = Items.ElementColor(ench);
        bool hasE = ench != Element.None;
        switch (kind)
        {
            case Kind.Plasma:
                p.damage = 42f; p.radius = 0.25f; p.gravity = -2f;
                p.color = hasE ? ec : new Color(1f, 0.4f, 0.95f);
                p.look = Gfx.Part(go.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.38f, Gfx.Glow(p.color, 4f), default, false).transform;
                Gfx.Part(go.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.6f, Gfx.Clear(new Color(p.color.r, p.color.g, p.color.b, 0.3f)), default, false);
                Gfx.AddLight(go.transform, Vector3.zero, p.color, 6f, 3f);
                break;
            case Kind.Slime:
                p.damage = 16f; p.radius = 0.25f; p.gravity = -14f;
                p.color = hasE ? Color.Lerp(new Color(0.35f, 1f, 0.4f), ec, 0.5f) : new Color(0.35f, 1f, 0.4f);
                p.look = Gfx.Part(go.transform, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.38f, 0.32f, 0.45f), Gfx.Glow(p.color, 1.2f), default, false).transform;
                break;
            case Kind.Toxic:
                p.damage = 0f; p.radius = 0.15f; p.gravity = -15f; p.life = 6f;
                p.color = new Color(0.65f, 1f, 0.2f);
                p.look = Gfx.Part(go.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.18f, Gfx.Glow(p.color, 2f), default, false).transform;
                Gfx.Part(go.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.12f, 0f), new Vector3(0.05f, 0.04f, 0.05f), Gfx.Lit(new Color(0.55f, 0.35f, 0.2f)), default, false);
                break;
            case Kind.Laser:
                p.damage = 7f; p.radius = 0.12f; p.gravity = 0f;
                p.color = new Color(1f, 0.15f, 0.1f);
                p.look = Gfx.Part(go.transform, PrimitiveType.Capsule, Vector3.zero, new Vector3(0.1f, 0.4f, 0.1f), Gfx.Glow(p.color, 4f), new Vector3(90f, 0f, 0f), false).transform;
                break;
            case Kind.TurretBolt:
                p.damage = 9f; p.radius = 0.12f; p.gravity = 0f;
                p.color = new Color(1f, 0.75f, 0.2f);
                p.look = Gfx.Part(go.transform, PrimitiveType.Capsule, Vector3.zero, new Vector3(0.1f, 0.35f, 0.1f), Gfx.Glow(p.color, 4f), new Vector3(90f, 0f, 0f), false).transform;
                break;
        }
        Gfx.SetLayer(go, 2);
        if (vel.sqrMagnitude > 0.01f) go.transform.rotation = Quaternion.LookRotation(vel);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        life -= dt;
        if (life <= 0f) { Impact(transform.position, Vector3.up, null); return; }
        Vector3 prev = transform.position;
        vel.y += gravity * dt;
        Vector3 next = prev + vel * dt;
        Vector3 step = next - prev;
        float dist = step.magnitude;
        if (dist < 1e-5f) return;

        // forcefields stop shots from outside
        if (Laboratory.ShieldBlocks(prev, next, team, out Vector3 sp, out Laboratory lab))
        {
            lab.ShieldHit(damage * (kind == Kind.Plasma || (gun != null && gun.blast > 0f) ? 2f : 1f) + (kind == Kind.Toxic ? 15f : 0f), sp, fromPlayer);
            Fx.Burst(sp, color, 10, 4f, 0.1f, 0.4f);
            Destroy(gameObject);
            return;
        }
        // the player (who's on a layer shots don't normally see)
        Player pl = Player.Local;
        if (pl != null && pl.Alive && pl.Team != team && pl.SegmentHits(prev, next, radius + 0.1f))
        {
            Impact(pl.Center, -step.normalized, pl);
            return;
        }
        if (Physics.SphereCast(prev, radius, step / dist, out RaycastHit hit, dist, Combat.NotPlayer, QueryTriggerInteraction.Ignore))
        {
            IDamageable d = Combat.Find(hit.collider);
            if (d != null && !Teams.Hostile(team, d.Team) && d.Team != Teams.Neutral && kind != Kind.Toxic)
            {
                // hit your own lab's wall: just fizzles out, no damage
                Fx.Burst(hit.point, color, 5, 2f, 0.06f, 0.3f);
                Destroy(gameObject);
                return;
            }
            Impact(hit.point, hit.normal, d);
            return;
        }
        transform.position = next;
        if (vel.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(vel);
        if (look != null && kind == Kind.Toxic) look.Rotate(400f * dt, 0f, 0f);
        if (look != null && gun != null && gun.star) look.Rotate(0f, 0f, 720f * dt, Space.Self);

        trailT -= dt;
        if (trailT <= 0f)
        {
            trailT = kind == Kind.Plasma ? 0.02f : 0.05f;
            if (kind == Kind.Plasma) Fx.Burst(prev, color, 1, 0.5f, 0.12f, 0.35f, 0f);
            else if (kind == Kind.Slime) Fx.Burst(prev, color, 1, 0.3f, 0.08f, 0.4f, -8f);
            else if (kind == Kind.Toxic) Fx.Puff(prev, new Color(0.6f, 1f, 0.2f, 0.4f), 1, 0.05f, 0.1f, 0.5f, Vector3.zero);
            else if (kind == Kind.Gun)
            {
                trailT = gun.blast > 0f ? 0.025f : 0.04f;
                if (gun.blast > 0f && gun.element == Element.Fire) Fx.Puff(prev, new Color(0.3f, 0.3f, 0.3f, 0.4f), 1, 0.05f, gun.size * 0.8f, 0.6f, Vector3.up * 0.5f);
                Fx.Burst(prev, color, 1, 0.4f, gun.size * 0.5f, 0.3f, gun.gravity < -4f ? -6f : 0f);
            }
        }
    }

    void Impact(Vector3 point, Vector3 normal, IDamageable target)
    {
        switch (kind)
        {
            case Kind.Plasma:
                Combat.Blast(point + normal * 0.2f, 3.6f, damage, 11f, team, ench, 2.5f, fromPlayer, color);
                break;
            case Kind.Slime:
                if (target != null && Teams.Hostile(team, target.Team))
                    target.TakeHit(new Hit { damage = damage, point = point, dir = vel.normalized, force = 2f, element = ench == Element.None ? Element.Slime : ench, team = team, structureMul = 1f, fromPlayer = fromPlayer });
                Fx.Burst(point, color, 14, 4f, 0.12f, 0.6f);
                Sfx.At("slime_splat", point, 0.9f);
                SlimePuddle.Spawn(point, team);
                break;
            case Kind.Toxic:
                Sfx.At("glass_break", point, 0.9f);
                Fx.Burst(point, new Color(0.8f, 1f, 0.6f), 10, 3f, 0.06f, 0.5f);
                ToxicCloud.Spawn(point + Vector3.up * 0.5f, team, fromPlayer);
                break;
            case Kind.Gun:
                if (gun.blast > 0f)
                {
                    Combat.Blast(point + normal * 0.2f, gun.blast, damage, gun.force, team, element, gun.structure, fromPlayer, color);
                    if (element == Element.Poison) ToxicCloud.Spawn(point + Vector3.up * 0.5f, team, fromPlayer);
                }
                else
                {
                    if (target != null && Teams.Hostile(team, target.Team))
                        target.TakeHit(new Hit { damage = damage, point = point, dir = vel.normalized, force = gun.force, element = element, team = team, structureMul = gun.structure, fromPlayer = fromPlayer });
                    Fx.Burst(point, color, 8, 3f, gun.size * 0.5f, 0.35f);
                    if (gun.element == Element.Slime || gun.puddle) Sfx.At("slime_splat", point, 0.4f, 1.3f);
                    if (gun.puddle) SlimePuddle.Spawn(point, team);
                }
                break;
            default:
                if (target != null && Teams.Hostile(team, target.Team))
                    target.TakeHit(new Hit { damage = damage, point = point, dir = vel.normalized, force = 0.5f, team = team, structureMul = 1f, fromPlayer = fromPlayer });
                Fx.Burst(point, color, 6, 3f, 0.06f, 0.3f);
                break;
        }
        Destroy(gameObject);
    }
}

// Slime puddle: anyone (not from the team that made it) who walks in is slowed.
public class SlimePuddle : MonoBehaviour
{
    public static readonly List<SlimePuddle> All = new List<SlimePuddle>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => All.Clear();

    public int team;
    public float radius = 1.8f;
    public bool permanent;      // slime traps around labs
    float life = 7f;
    Transform disc;

    public static SlimePuddle Spawn(Vector3 point, int team)
    {
        if (Physics.Raycast(point + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 4f, Combat.NotPlayer, QueryTriggerInteraction.Ignore)) point = hit.point;
        var go = new GameObject("Slime Puddle");
        go.transform.position = point + Vector3.up * 0.03f;
        var p = go.AddComponent<SlimePuddle>();
        p.team = team;
        p.disc = Gfx.Part(go.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(3.4f, 0.02f, 3.4f), Gfx.Glow(new Color(0.3f, 0.9f, 0.35f), 0.5f), default, false).transform;
        for (int i = 0; i < 4; i++)
            Gfx.Part(go.transform, PrimitiveType.Sphere, new Vector3(Random.Range(-1f, 1f), 0.02f, Random.Range(-1f, 1f)), new Vector3(0.5f, 0.12f, 0.5f) * Random.Range(0.6f, 1.2f), Gfx.Glow(new Color(0.35f, 1f, 0.4f), 0.8f), default, false);
        return p;
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    public bool Catches(Vector3 pos, int victimTeam)
    {
        if (!Teams.Hostile(team, victimTeam)) return false;
        Vector3 d = pos - transform.position;
        return Mathf.Abs(d.y) < 1.2f && new Vector2(d.x, d.z).sqrMagnitude < radius * radius;
    }

    void Update()
    {
        if (permanent) return;
        life -= Time.deltaTime;
        if (life < 1f) transform.localScale = Vector3.one * Mathf.Max(0.01f, life);
        if (life <= 0f) Destroy(gameObject);
    }
}

// Toxic cloud from a Toxic Bomb: poisons everyone else inside and eats away at labs.
public class ToxicCloud : MonoBehaviour
{
    int team;
    bool fromPlayer;
    float life = 6f, tick;
    const float Radius = 3.8f;

    public static void Spawn(Vector3 pos, int team, bool fromPlayer)
    {
        var go = new GameObject("Toxic Cloud");
        go.transform.position = pos;
        var c = go.AddComponent<ToxicCloud>();
        c.team = team;
        c.fromPlayer = fromPlayer;
        Sfx.At("toxic_hiss", pos, 0.8f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        life -= dt;
        if (life <= 0f) { Destroy(gameObject); return; }
        if (Random.value < 0.6f)
            Fx.Puff(transform.position + new Vector3(Random.Range(-Radius, Radius) * 0.7f, Random.Range(-0.3f, 1f), Random.Range(-Radius, Radius) * 0.7f),
                new Color(0.55f, 0.95f, 0.15f, 0.35f), 1, 0.2f, 1.3f, 1.6f, Vector3.up * 0.3f);
        tick -= dt;
        if (tick > 0f) return;
        tick = 0.5f;
        var done = new HashSet<IDamageable>();
        foreach (Collider col in Physics.OverlapSphere(transform.position, Radius, ~0, QueryTriggerInteraction.Ignore))
        {
            IDamageable d = Combat.Find(col);
            if (d == null || done.Contains(d) || !d.Alive || !Teams.Hostile(team, d.Team)) continue;
            done.Add(d);
            d.TakeHit(new Hit { damage = 4f, point = Combat.Closest(col, transform.position), dir = Vector3.up, element = Element.Poison, team = team, structureMul = 3f, dot = true, fromPlayer = fromPlayer });
        }
    }
}

// ---------------------------------------------------------------
// TARGET DUMMY: for trying out weapons and enchants. Gets back up after a while.
// ---------------------------------------------------------------
public class TargetDummy : MonoBehaviour, IDamageable
{
    public const float MaxHp = 250f;
    public float hp = MaxHp;
    public readonly Status status = new Status();
    float lastHit = -10f, downUntil, wobble, wobbleVel;
    Transform body;
    Vector3 home;
    bool down;

    public int Team => Teams.Neutral;
    public bool Alive => !down;
    public Vector3 Center => transform.position + Vector3.up * 1.2f;
    public float Fraction => hp / MaxHp;
    public bool ShowBar => Time.time - lastHit < 4f && !down;

    public static TargetDummy Create(Vector3 pos, float yaw)
    {
        var go = new GameObject("Target Dummy");
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        var d = go.AddComponent<TargetDummy>();
        d.home = pos;
        d.Build();
        return d;
    }

    void Build()
    {
        Material wood = Gfx.Lit(new Color(0.5f, 0.36f, 0.22f), 0.2f);
        Material sack = Gfx.Lit(new Color(0.82f, 0.72f, 0.5f), 0.1f);
        Material red = Gfx.Lit(new Color(0.85f, 0.15f, 0.12f), 0.3f);
        Material white = Gfx.Lit(Color.white, 0.3f);
        Gfx.Part(transform, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.8f, 0.05f, 0.8f), Gfx.Lit(new Color(0.3f, 0.3f, 0.32f), 0.3f));
        Gfx.Part(transform, PrimitiveType.Cylinder, new Vector3(0f, 0.5f, 0f), new Vector3(0.1f, 0.5f, 0.1f), wood);
        body = Gfx.Pivot(transform, new Vector3(0f, 0.6f, 0f), "Body");
        Gfx.Part(body, PrimitiveType.Capsule, new Vector3(0f, 0.6f, 0f), new Vector3(0.5f, 0.5f, 0.35f), sack);
        Gfx.Part(body, PrimitiveType.Sphere, new Vector3(0f, 1.25f, 0f), new Vector3(0.34f, 0.36f, 0.34f), sack);
        Gfx.Part(body, PrimitiveType.Cylinder, new Vector3(0f, 0.75f, 0f), new Vector3(1.1f, 0.05f, 0.08f), wood, new Vector3(0f, 0f, 90f));   // arms
        Gfx.Part(body, PrimitiveType.Cylinder, new Vector3(0f, 0.65f, 0.17f), new Vector3(0.36f, 0.01f, 0.36f), red, new Vector3(90f, 0f, 0f));
        Gfx.Part(body, PrimitiveType.Cylinder, new Vector3(0f, 0.65f, 0.18f), new Vector3(0.25f, 0.01f, 0.25f), white, new Vector3(90f, 0f, 0f));
        Gfx.Part(body, PrimitiveType.Cylinder, new Vector3(0f, 0.65f, 0.19f), new Vector3(0.12f, 0.01f, 0.12f), red, new Vector3(90f, 0f, 0f));
        Gfx.Part(body, PrimitiveType.Cube, new Vector3(-0.06f, 1.3f, 0.16f), new Vector3(0.06f, 0.015f, 0.01f), Gfx.Lit(Color.black), new Vector3(0f, 0f, 45f));   // X eyes
        Gfx.Part(body, PrimitiveType.Cube, new Vector3(-0.06f, 1.3f, 0.16f), new Vector3(0.06f, 0.015f, 0.01f), Gfx.Lit(Color.black), new Vector3(0f, 0f, -45f));
        Gfx.Part(body, PrimitiveType.Cube, new Vector3(0.06f, 1.3f, 0.16f), new Vector3(0.06f, 0.015f, 0.01f), Gfx.Lit(Color.black), new Vector3(0f, 0f, 45f));
        Gfx.Part(body, PrimitiveType.Cube, new Vector3(0.06f, 1.3f, 0.16f), new Vector3(0.06f, 0.015f, 0.01f), Gfx.Lit(Color.black), new Vector3(0f, 0f, -45f));
        var col = gameObject.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 1.1f, 0f);
        col.height = 2.2f;
        col.radius = 0.4f;
    }

    public void TakeHit(Hit h)
    {
        if (down) return;
        hp -= h.damage;
        lastHit = Time.time;
        if (h.element != Element.None && !h.dot) status.Apply(h.element, h.team);
        if (!h.dot) wobbleVel += Mathf.Clamp(h.damage, 3f, 30f) * 12f * (Random.value < 0.5f ? 1f : -1f);
        GameHUD.DamageNumber(h.point, h.damage, h.dot ? Items.ElementColor(h.element) : Color.white, h.fromPlayer);
        if (h.fromPlayer && !h.dot) GameHUD.HitMarker(false);
        if (hp <= 0f)
        {
            down = true;
            downUntil = Time.time + 4f;
            Fx.Burst(Center, new Color(0.85f, 0.75f, 0.5f), 18, 5f, 0.1f, 0.8f);
            Sfx.At("dummy_down", transform.position, 0.9f);
            if (h.fromPlayer) GameHUD.HitMarker(true);
        }
    }

    public void ResetDummy()
    {
        hp = MaxHp;
        down = false;
        status.Clear();
        body.localRotation = Quaternion.identity;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (down)
        {
            body.localRotation = Quaternion.Slerp(body.localRotation, Quaternion.Euler(-85f, 0f, 0f), dt * 6f);
            if (Time.time > downUntil) { ResetDummy(); Fx.Burst(Center, Color.white, 8, 2f); Sfx.At("pop", transform.position, 0.6f); }
            return;
        }
        float dot = status.Tick(dt);
        if (dot > 0f) TakeHit(new Hit { damage = dot, point = Center + Vector3.up * 0.3f, element = status.burn > 0f ? Element.Fire : Element.Poison, team = status.lastTeam, dot = true, fromPlayer = true });
        status.Show(Center);
        if (Time.time - lastHit > 5f) hp = Mathf.MoveTowards(hp, MaxHp, dt * 60f);
        // springy wobble when hit
        wobbleVel += (-wobble * 90f - wobbleVel * 6f) * dt;
        wobble += wobbleVel * dt;
        if (status.freeze > 0f) wobbleVel = 0f;
        body.localRotation = Quaternion.Euler(wobble * 0.5f, 0f, wobble);
    }
}

// TOXIC POOL (the glowing green swamp on the map): poisons you while you stand in it.
// It's on the Toxic_Pool prefab - change the size with the radius below.
public class HazardPool : MonoBehaviour
{
    [Tooltip("How far from the middle it hurts (metres, before scaling).")]
    public float radius = 7f;
    float tick;

    void Update()
    {
        // bubbles
        if (Random.value < Time.deltaTime * 6f)
        {
            float r = radius * transform.lossyScale.x;
            Vector2 c = Random.insideUnitCircle * r * 0.9f;
            Vector3 p = transform.position + new Vector3(c.x, 0.1f, c.y);
            Fx.Puff(p, new Color(0.55f, 1f, 0.25f, 0.45f), 1, 0.05f, 0.25f, 0.9f, Vector3.up * 0.6f);
        }
        Player pl = Player.Local;
        if (!MLGame.Playing || pl == null || !pl.Alive) return;
        Vector3 d = pl.transform.position - transform.position;
        float rad = radius * transform.lossyScale.x;
        if (Mathf.Abs(d.y) > 2.5f || new Vector2(d.x, d.z).magnitude > rad) return;
        tick -= Time.deltaTime;
        if (tick > 0f) return;
        tick = 0.6f;
        pl.TakeHit(new Hit { damage = 3f, point = pl.Center, dir = Vector3.up, element = Element.Poison, team = Teams.Neutral, structureMul = 1f });
    }
}
