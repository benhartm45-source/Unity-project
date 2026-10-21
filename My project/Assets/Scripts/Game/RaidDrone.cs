using System.Collections.Generic;
using UnityEngine;

// RAID DRONES: when you play on your own, MADNESS sends waves of little robot drones at your lab
// (and at you, if you get close). They give your turret, forcefield and walls something to do.
// Shoot them down - they sometimes drop scrap metal.
public class RaidDrone : MonoBehaviour, IDamageable
{
    public static readonly List<RaidDrone> All = new List<RaidDrone>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => All.Clear();

    public float hp = 60f;
    public readonly Status status = new Status();
    public Vector3 Velocity { get; private set; }

    Laboratory target;
    Transform body, eye;
    Transform[] rotors;
    float orbit, fireCd, height, hurtFlash, bob;
    bool dead;
    AudioSource hum;

    public int Team => Teams.Drones;
    public bool Alive => !dead;
    public Vector3 Center => transform.position;

    public static RaidDrone Spawn(Vector3 pos, Laboratory target)
    {
        var go = new GameObject("Raid Drone");
        go.transform.position = pos;
        var d = go.AddComponent<RaidDrone>();
        d.target = target;
        d.orbit = Random.value * 360f;
        d.height = Random.Range(5f, 9f);
        d.fireCd = Random.Range(1f, 2.5f);
        d.Build();
        return d;
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Build()
    {
        Material shell = Gfx.Lit(new Color(0.22f, 0.23f, 0.26f), 0.6f, 0.7f);
        Material plate = Gfx.Lit(new Color(0.75f, 0.2f, 0.15f), 0.5f, 0.3f);
        Material red = Gfx.Glow(new Color(1f, 0.15f, 0.1f), 4f);
        body = Gfx.Pivot(transform, Vector3.zero, "Body");
        Gfx.Part(body, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.9f, 0.55f, 0.9f), shell);
        Gfx.Part(body, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(1.05f, 0.06f, 1.05f), plate);
        eye = Gfx.Part(body, PrimitiveType.Sphere, new Vector3(0f, -0.05f, 0.4f), new Vector3(0.28f, 0.2f, 0.12f), red).transform;
        Gfx.Part(body, PrimitiveType.Cylinder, new Vector3(0f, -0.3f, 0.1f), new Vector3(0.1f, 0.15f, 0.1f), shell, new Vector3(70f, 0f, 0f));   // gun
        rotors = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f + 45f;
            Vector3 arm = Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0f, 0.75f);
            Gfx.Part(body, PrimitiveType.Cube, arm * 0.55f, new Vector3(0.07f, 0.05f, 0.6f), shell, new Vector3(0f, a, 0f));
            rotors[i] = Gfx.Pivot(body, arm + Vector3.up * 0.12f, "Rotor");
            Gfx.Part(rotors[i], PrimitiveType.Cube, Vector3.zero, new Vector3(0.6f, 0.015f, 0.08f), Gfx.Clear(new Color(0.8f, 0.8f, 0.8f, 0.5f)), default, false);
            Gfx.Part(rotors[i], PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.08f, 0.04f, 0.08f), shell);
        }
        Gfx.AddLight(body, new Vector3(0f, -0.1f, 0.45f), new Color(1f, 0.2f, 0.1f), 4f, 1.5f);
        var col = gameObject.AddComponent<SphereCollider>();
        col.radius = 0.7f;
        hum = Sfx.Loop("drone_hum", transform, 0.35f, 30f);
        if (hum != null) hum.pitch = Random.Range(0.9f, 1.15f);
    }

    public void TakeHit(Hit h)
    {
        if (dead) return;
        hp -= h.damage;
        hurtFlash = 1f;
        if (h.element != Element.None && !h.dot) status.Apply(h.element, h.team);
        Velocity += h.dir * h.force * 0.8f;
        if (h.fromPlayer)
        {
            GameHUD.DamageNumber(h.point, h.damage, h.dot ? Items.ElementColor(h.element) : Color.white, true);
            if (!h.dot) GameHUD.HitMarker(false);
        }
        Fx.Burst(h.point, new Color(1f, 0.8f, 0.4f), 4, 3f, 0.05f, 0.3f);
        if (hp <= 0f) Die(h.fromPlayer);
    }

    void Die(bool byPlayer)
    {
        dead = true;
        if (byPlayer) GameHUD.HitMarker(true);
        Combat.Blast(transform.position, 2f, 10f, 4f, Teams.Drones, Element.None, 1f, false, new Color(1f, 0.5f, 0.2f));
        if (Random.value < 0.6f) MaterialPickup.Drop(transform.position, (int)Mat.Metal, 1);
        if (Random.value < 0.25f) MaterialPickup.Drop(transform.position, Random.Range(0, Items.MatCount), 1);
        MLGame.I?.DroneKilled();
        Destroy(gameObject);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (!MLGame.Playing) return;
        float dot = status.Tick(dt);
        if (dot > 0f) TakeHit(new Hit { damage = dot, point = Center, element = status.burn > 0f ? Element.Fire : Element.Poison, team = status.lastTeam, dot = true, fromPlayer = true });
        if (dead) return;
        status.Show(Center);
        float speedMul = status.SpeedMul;

        // who to go for: the lab, or the player if they're close
        Player p = Player.Local;
        Vector3 focus;
        IDamageable shootAt = null;
        bool canSee = p != null && p.Alive && !p.Hidden;          // (a Ghost Potion hides you)
        bool chasePlayer = canSee && Vector3.Distance(p.transform.position, transform.position) < 18f;
        if (target == null || target.Destroyed) chasePlayer = canSee;
        if (chasePlayer) { focus = p.transform.position; shootAt = p; }
        else if (target != null) { focus = target.transform.position; shootAt = target; }
        else focus = MLGame.ArenaCenter;

        // circle around the target
        orbit += dt * 25f * speedMul;
        float radius = chasePlayer ? 9f : 15f;
        Vector3 want = focus + Quaternion.Euler(0f, orbit, 0f) * new Vector3(0f, 0f, radius);
        float groundY = Terrain.activeTerrain != null ? Terrain.activeTerrain.SampleHeight(want) + Terrain.activeTerrain.transform.position.y : focus.y;
        want.y = Mathf.Max(groundY, focus.y) + height;
        Vector3 to = want - transform.position;
        Velocity = Vector3.MoveTowards(Velocity, Vector3.ClampMagnitude(to, 9f) * speedMul, 14f * dt);
        transform.position += Velocity * dt;
        bob += dt;

        // face the target and tilt with movement
        Vector3 look = (shootAt != null ? shootAt.Center : focus) - transform.position;
        if (look.sqrMagnitude > 0.01f)
        {
            Quaternion face = Quaternion.LookRotation(new Vector3(look.x, 0f, look.z).normalized + Vector3.up * 0.001f);
            transform.rotation = Quaternion.Slerp(transform.rotation, face, dt * 4f);
        }
        Vector3 localV = transform.InverseTransformDirection(Velocity);
        body.localRotation = Quaternion.Euler(localV.z * 3f, 0f, -localV.x * 3f);
        body.localPosition = new Vector3(0f, Mathf.Sin(bob * 3f) * 0.08f, 0f);
        foreach (Transform r in rotors) r.Rotate(0f, 1800f * dt, 0f);
        hurtFlash = Mathf.MoveTowards(hurtFlash, 0f, dt * 4f);
        if (eye != null) eye.localScale = new Vector3(0.28f, 0.2f, 0.12f) * (1f + hurtFlash * 0.5f);

        // shoot
        fireCd -= dt;
        if (shootAt != null && fireCd <= 0f && status.freeze <= 0f && Vector3.Distance(transform.position, shootAt.Center) < 30f)
        {
            fireCd = Random.Range(1.1f, 1.8f);
            Vector3 from = transform.TransformPoint(new Vector3(0f, -0.35f, 0.4f));
            Vector3 aim = shootAt.Center + Random.insideUnitSphere * (chasePlayer ? 0.8f : 2.5f);
            Projectile.Fire(Projectile.Kind.Laser, from, (aim - from).normalized * 32f, Teams.Drones, Element.None, false);
            Fx.Flash(from, new Color(1f, 0.2f, 0.1f), 4f, 3f, 0.05f);
            Sfx.At("laser", from, 0.6f, Random.Range(0.9f, 1.1f));
        }
    }
}
