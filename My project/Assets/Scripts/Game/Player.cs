using UnityEngine;
using UnityEngine.InputSystem;

// YOU: moving, jumping, the camera, health and armour, potion effects, dying and coming back at your lab.
// (Items, weapons and using stations are in PlayerItems.cs.)
//
// Keyboard + mouse:  WASD move · Shift sprint · Space jump · Mouse look · Left click use / fire · Right click aim
//                    E use / gather (hold) · 1-8 or wheel pick item · Q quick-heal · Esc pause
// Controller:        L-stick move · L3 sprint · A jump · R-stick look · RT fire · LT aim · X use (hold)
//                    LB / RB pick item · Y quick-heal · Start pause
public partial class Player : MonoBehaviour, IDamageable
{
    public static Player Local { get; private set; }

    public const float MaxHealth = 100f;
    public float health = MaxHealth;
    public float armor;                 // soaks damage first
    public int armorTier;               // 0 none, 1 scrap, 2 crystal
    public bool bounceBoots;
    public readonly Status status = new Status();

    // potion effects (seconds left)
    public float speedLeft, leapLeft, ironLeft, healLeft;

    public int labIndex;
    public Laboratory MyLab { get; private set; }
    public Scientist Model { get; private set; }

    CharacterController cc;
    Vector3 velocity, push;
    bool grounded, doubleJumpUsed;
    float lastGrounded, jumpBuffer, fallSpeed;
    float lastHurt = -10f, regenDelay;
    public float RespawnAt { get; private set; } = -1f;
    bool dead;

    public bool Alive => !dead;
    public int Team => labIndex;
    public Vector3 Center => transform.position + Vector3.up * 1.1f;
    public bool Aiming { get; private set; }
    public bool Sprinting { get; private set; }
    public bool InsideOwnLab => MyLab != null && !MyLab.Destroyed && MyLab.Contains(transform.position);
    public float HurtFlash => Mathf.Clamp01(1f - (Time.time - lastHurt) / 0.5f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Local = null;

    public static Player Spawn(Laboratory lab, int index)
    {
        var go = new GameObject("Player");
        go.layer = 2;
        var p = go.AddComponent<Player>();
        p.labIndex = index;
        p.MyLab = lab;
        p.cc = go.AddComponent<CharacterController>();
        p.cc.height = 1.8f;
        p.cc.radius = 0.38f;
        p.cc.center = new Vector3(0f, 0.9f, 0f);
        p.cc.slopeLimit = 50f;
        p.cc.stepOffset = 0.45f;
        p.cc.skinWidth = 0.05f;
        p.Model = Scientist.Create(go.transform, Items.LabColors[index % Items.LabColors.Length], index * 7 + Random.Range(0, 3));
        p.Model.Step += p.OnStep;
        Local = p;
        p.ResetLoadout();
        p.PlaceAtLab();
        return p;
    }

    void OnDestroy() { if (Local == this) Local = null; }

    public void PlaceAtLab()
    {
        Vector3 pos;
        float yaw;
        if (MyLab != null) { pos = MyLab.SpawnPoint; yaw = MyLab.transform.eulerAngles.y; }
        else { pos = MLGame.ArenaCenter + Vector3.up * 2f; yaw = 0f; }
        cc.enabled = false;
        transform.position = pos;
        cc.enabled = true;
        velocity = push = Vector3.zero;
        GameCam.Ensure().yaw = yaw;
        GameCam.I.pitch = 8f;
    }

    // ================================================================
    // Every frame
    // ================================================================

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        if (GameCam.I == null) { if (GameCam.Ensure() == null) return; }   // (lost if scripts recompile while playing)
        bool control = MLGame.Playing && !GameHUD.UIOpen && !dead && !LoadingScreen.Active;

        if (dead)
        {
            Model.dead = true;
            Model.velocity = Vector3.zero;
            if (RespawnAt > 0f && Time.time >= RespawnAt) Respawn();
            Gravity(dt);
            return;
        }

        Look(control);
        Move(dt, control);
        Effects(dt);
        ItemsUpdate(dt, control);
        InteractUpdate(dt, control);

        // the model copies what we're doing
        Model.velocity = cc.velocity;
        Model.grounded = grounded;
        Model.aimYaw = GameCam.I.yaw;
        Model.aimPitch = -GameCam.I.pitch;
        Model.aiming = Aiming;
        Model.sprinting = Sprinting;

        // fell out of the world
        if (transform.position.y < -30f) { TakeHit(new Hit { damage = 999f, team = Teams.Neutral }); }
    }

    // ---------------- input helpers ----------------
    static Keyboard K => Keyboard.current;
    static Mouse M => Mouse.current;
    static Gamepad G => Gamepad.current;

    void Look(bool control)
    {
        if (!control) return;
        GameCam c = GameCam.I;
        float sens = Settings.Sensitivity * (Aiming ? 0.55f : 1f);
        if (M != null)
        {
            Vector2 d = M.delta.ReadValue();
            c.yaw += d.x * 0.06f * sens;
            c.pitch -= d.y * 0.06f * sens;
        }
        if (G != null)
        {
            Vector2 r = G.rightStick.ReadValue();
            c.yaw += r.x * 160f * sens * 0.6f * Time.deltaTime;
            c.pitch -= r.y * 110f * sens * 0.6f * Time.deltaTime;
        }
        c.pitch = Mathf.Clamp(c.pitch, -70f, 75f);
    }

    Vector2 MoveInput()
    {
        Vector2 v = Vector2.zero;
        if (K != null)
        {
            if (K.wKey.isPressed) v.y += 1f;
            if (K.sKey.isPressed) v.y -= 1f;
            if (K.dKey.isPressed) v.x += 1f;
            if (K.aKey.isPressed) v.x -= 1f;
        }
        if (G != null) v += G.leftStick.ReadValue();
        return Vector2.ClampMagnitude(v, 1f);
    }

    bool sprintToggle;

    void Move(float dt, bool control)
    {
        Vector2 input = control ? MoveInput() : Vector2.zero;
        if (G != null && G.leftStickButton.wasPressedThisFrame) sprintToggle = !sprintToggle;
        if (input.magnitude < 0.2f) sprintToggle = false;
        bool wantSprint = control && ((K != null && K.leftShiftKey.isPressed) || sprintToggle) && input.y > 0.3f;
        Aiming = control && ((M != null && M.rightButton.isPressed) || (G != null && G.leftTrigger.ReadValue() > 0.4f)) && !Model.Busy;
        Sprinting = wantSprint && !Aiming && !Firing && !Model.Busy;

        float speed = Sprinting ? 8.6f : Aiming ? 3.4f : 5.6f;
        if (Model.Busy) speed = 2.2f;
        if (speedLeft > 0f) speed *= 1.45f;
        speed *= SpeedMul;                      // armour and potions
        speed *= status.SpeedMul * SlimeFactor();
        if (Working) speed = 0f;

        Quaternion yawRot = Quaternion.Euler(0f, GameCam.I.yaw, 0f);
        Vector3 want = yawRot * new Vector3(input.x, 0f, input.y) * speed;
        float accel = grounded ? 14f : 4f;
        Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
        flat = Vector3.MoveTowards(flat, want, accel * speed * dt + 0.5f);
        velocity.x = flat.x;
        velocity.z = flat.z;

        // jumping (a moment of "coyote time" after walking off an edge, and pressing slightly early works too)
        bool jumpPressed = control && ((K != null && K.spaceKey.wasPressedThisFrame) || (G != null && G.buttonSouth.wasPressedThisFrame));
        if (jumpPressed) jumpBuffer = 0.15f;
        jumpBuffer -= dt;
        float jumpV = (leapLeft > 0f ? 13f : 7f) * JumpMul;
        if (jumpBuffer > 0f && Time.time - lastGrounded < 0.15f && status.freeze <= 0f)
        {
            velocity.y = jumpV;
            jumpBuffer = 0f;
            lastGrounded = -10f;
            Sfx.Play("jump", 0.5f, Random.Range(0.95f, 1.08f));
        }
        else if (jumpBuffer > 0f && !grounded && DoubleJump && !doubleJumpUsed && status.freeze <= 0f)
        {
            velocity.y = jumpV * 0.95f;
            doubleJumpUsed = true;
            jumpBuffer = 0f;
            Fx.Ring(transform.position, new Color(0.4f, 1f, 0.5f, 0.6f), 12, 4f, 0.2f);
            Sfx.Play("boing", 0.6f, Random.Range(0.95f, 1.1f));
        }
        Gravity(dt);
    }

    void Gravity(float dt)
    {
        velocity.y += -22f * GravityMul * dt;
        if (velocity.y < -45f) velocity.y = -45f;
        push = Vector3.MoveTowards(push, Vector3.zero, (grounded ? 22f : 6f) * dt);
        Vector3 motion = (velocity + push) * dt;
        bool wasGrounded = grounded;
        float vyBefore = velocity.y;
        CollisionFlags flags = cc.Move(motion);
        grounded = (flags & CollisionFlags.Below) != 0;
        if ((flags & CollisionFlags.Above) != 0 && velocity.y > 0f) velocity.y = 0f;
        if (grounded)
        {
            lastGrounded = Time.time;
            doubleJumpUsed = false;
            if (!wasGrounded) Landed(vyBefore);
            if (velocity.y < -2f) velocity.y = -2f;
        }
    }

    void Landed(float vy)
    {
        if (vy < -9f)
        {
            Sfx.Play("land", Mathf.Clamp01(-vy / 25f), Random.Range(0.9f, 1.05f));
            Fx.Ring(transform.position + Vector3.up * 0.05f, new Color(0.7f, 0.65f, 0.55f, 0.5f), 10, 3f, 0.25f);
        }
        // big falls hurt (unless you have Bounce Boots or a Leap Potion)
        if (vy < -17f && !bounceBoots && leapLeft <= 0f && !NoFall && !dead)
            TakeHit(new Hit { damage = (-vy - 17f) * 5f, team = Teams.Neutral, point = transform.position, dir = Vector3.down });
    }

    void OnStep()
    {
        if (!grounded) return;
        Sfx.Play("step", Sprinting ? 0.35f : 0.22f, Random.Range(0.85f, 1.15f));
    }

    float SlimeFactor()
    {
        if (Immune(Element.Slime)) return 1f;
        foreach (SlimePuddle s in SlimePuddle.All)
            if (s != null && s.Catches(transform.position, Team)) return s.permanent ? 0.3f : 0.45f;
        return 1f;
    }

    // ---------------- potions, burning, poison, health regen ----------------

    void Effects(float dt)
    {
        speedLeft = Mathf.Max(0f, speedLeft - dt);
        leapLeft = Mathf.Max(0f, leapLeft - dt);
        ironLeft = Mathf.Max(0f, ironLeft - dt);
        if (healLeft > 0f)
        {
            float k = Mathf.Min(dt, healLeft);
            healLeft -= k;
            health = Mathf.Min(MaxHealth, health + 30f * k);
        }
        GearUpdate(dt);
        float dot = status.Tick(dt);
        if (dot > 0f) TakeHit(new Hit { damage = dot, team = status.lastTeam, element = status.burn > 0f ? Element.Fire : Element.Poison, dot = true, point = Center });
        status.Show(Center);
        if (speedLeft > 0f && cc.velocity.magnitude > 3f && Random.value < 0.3f) Fx.Burst(transform.position + Vector3.up * 0.2f, new Color(1f, 0.85f, 0.2f), 1, 1f, 0.07f, 0.4f, 0f);
        // slow regen when you haven't been hurt for a while (only in your lab it's quick)
        regenDelay -= dt;
        if (regenDelay <= 0f && health < MaxHealth) health = Mathf.Min(MaxHealth, health + (InsideOwnLab ? 8f : 1.5f) * dt);

        // outside the zone
        if (MLGame.I != null && MLGame.I.OutsideZone(transform.position))
        {
            zoneTick -= dt;
            if (zoneTick <= 0f) { zoneTick = 0.5f; TakeHit(new Hit { damage = MLGame.I.ZoneDamage * 0.5f * ZoneMul, team = Teams.Neutral, dot = true, point = Center }); }
        }
    }
    float zoneTick;

    // ================================================================
    // Getting hurt
    // ================================================================

    public void TakeHit(Hit h)
    {
        if (dead || !MLGame.Playing) return;
        NoteAttacker(h);                        // who hit us last gets the credit (PlayerDeath.cs)
        float dmg = h.damage;
        if (ironLeft > 0f) dmg *= 0.5f;
        dmg *= DamageTakenMul;                  // armour and potions
        if (armor > 0f && !h.dot)
        {
            float soak = Mathf.Min(armor, dmg * 0.7f);
            armor -= soak;
            dmg -= soak;
            if (armor <= 0f) { ArmorBroke(); Sfx.Play("armor_break", 0.8f); GameHUD.Toast("Your armour broke!", new Color(1f, 0.5f, 0.3f)); }
        }
        health -= dmg;
        if (h.element != Element.None && !h.dot && !Immune(h.element)) status.Apply(h.element, h.team);
        if (h.force > 0f) push += (h.dir + Vector3.up * 0.4f).normalized * h.force * (ironLeft > 0f ? 0.5f : 1f) * KnockMul;
        if (h.force > 4f) velocity.y = Mathf.Max(velocity.y, h.force * 0.5f);
        lastHurt = Time.time;
        regenDelay = 6f;
        if (!h.dot)
        {
            Model.Hurt();
            Sfx.Play("hurt", 0.7f, Random.Range(0.9f, 1.1f));
            GameHUD.DamageFrom(h.point - h.dir * 3f);
        }
        if (health <= 0f) Die();
    }

    public void Knock(Vector3 v) => push += v;

    void Die()
    {
        dead = true;
        health = 0f;
        status.Clear();
        speedLeft = leapLeft = ironLeft = healLeft = 0f;
        ClearBuffs();
        Model.dead = true;
        Sfx.Play("death", 0.9f);
        CancelUse();
        DropBackpack();
        bool canRespawn = MyLab != null && !MyLab.Destroyed;
        OnKnockedOut(canRespawn);               // lose your gun, get some materials back, tell everyone (PlayerDeath.cs)
        RespawnAt = canRespawn ? Time.time + 6f : -1f;
        GameHUD.Toast(canRespawn ? "You got knocked out! Your lab will patch you up..." : "ELIMINATED - your lab is gone, so there's no coming back.", new Color(1f, 0.35f, 0.3f));
        MLGame.I?.PlayerDied(canRespawn);
    }

    void Respawn()
    {
        dead = false;
        Model.dead = false;
        health = MaxHealth;
        RespawnAt = -1f;
        PlaceAtLab();
        Fx.Burst(transform.position + Vector3.up, Items.LabColors[labIndex % 8], 20, 4f);
        Sfx.Play("respawn", 0.8f);
    }

    // Shots use this to see if they went through you (you're on a layer they don't normally hit).
    public bool SegmentHits(Vector3 a, Vector3 b, float r)
    {
        Vector3 p0 = transform.position + Vector3.up * 0.4f, p1 = transform.position + Vector3.up * 1.6f;
        return SegSegDist(a, b, p0, p1) < 0.42f + r;
    }

    static float SegSegDist(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
    {
        Vector3 d1 = q1 - p1, d2 = q2 - p2, r = p1 - p2;
        float a = Vector3.Dot(d1, d1), e = Vector3.Dot(d2, d2), f = Vector3.Dot(d2, r);
        float s, t;
        if (a <= 1e-6f && e <= 1e-6f) return Vector3.Distance(p1, p2);
        if (a <= 1e-6f) { s = 0f; t = Mathf.Clamp01(f / e); }
        else
        {
            float c = Vector3.Dot(d1, r);
            if (e <= 1e-6f) { t = 0f; s = Mathf.Clamp01(-c / a); }
            else
            {
                float b = Vector3.Dot(d1, d2), denom = a * e - b * b;
                s = denom != 0f ? Mathf.Clamp01((b * f - c * e) / denom) : 0f;
                t = (b * s + f) / e;
                if (t < 0f) { t = 0f; s = Mathf.Clamp01(-c / a); }
                else if (t > 1f) { t = 1f; s = Mathf.Clamp01((b - c) / a); }
            }
        }
        return Vector3.Distance(p1 + d1 * s, p2 + d2 * t);
    }
}
