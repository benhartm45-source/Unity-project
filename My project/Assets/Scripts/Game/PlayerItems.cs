using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Things you can walk up to and press E on: material spots, lab stations.
public interface IInteractable
{
    Vector3 InteractPoint { get; }
    float InteractRange { get; }
    string Prompt(Player p, out bool usable);
    void Use(Player p);
}

public static class Interactables
{
    public static readonly List<IInteractable> All = new List<IInteractable>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => All.Clear();
}

// YOUR STUFF: backpack of materials, the hotbar (weapons, potions, bombs), firing, drinking, throwing,
// gathering materials and crafting at your lab's stations.
public partial class Player
{
    public const int SlotCount = 8;
    public readonly int[] backpack = new int[Items.MatCount];
    public readonly HeldItem[] slots = new HeldItem[SlotCount];
    public int selected;
    public HeldItem Held => slots[selected];

    public bool Firing { get; private set; }
    public bool Working => workLeft > 0f;
    public string PromptText { get; private set; }
    public bool PromptUsable { get; private set; }
    public float GatherProgress => gatherNode != null ? Mathf.Clamp01(gatherT / Scientist.GatherTime) : -1f;
    public float WorkProgress => workLeft > 0f ? 1f - workLeft / workTotal : -1f;

    float fireCd, freezeTick;
    float drinkAt = -1f, throwAt = -1f;
    HeldItem usingItem;
    AudioSource beamLoop;
    MaterialNode gatherNode;
    float gatherT;
    float workLeft, workTotal;
    Recipe workRecipe;
    bool wasInLab;
    int shownSlot = -1;
    Element shownEnchant;

    public void ResetLoadout()
    {
        for (int i = 0; i < SlotCount; i++) slots[i] = null;
        for (int i = 0; i < Items.MatCount; i++) backpack[i] = 0;
        slots[0] = new HeldItem { id = ItemId.Zapper };
        selected = 0;
        health = MaxHealth;
        armor = 0f; armorTier = 0; bounceBoots = false;
        speedLeft = leapLeft = ironLeft = healLeft = 0f;
        status.Clear();
        dead = false;
        RespawnAt = -1f;
        suit = null;
        ClearBuffs();
        if (Model != null) { Model.dead = false; Model.SetArmor(0, false); }
        shownSlot = -1;
        RefreshHeld();
    }

    // Rebuild the model in your hands if the selected thing changed.
    void RefreshHeld()
    {
        HeldItem h = Held;
        Element e = h != null ? h.enchant : Element.None;
        EnchantDef ed = h != null ? h.enchantDef : null;
        if (shownSlot == selected && shownItem == h && shownEnchant == e && shownEnchDef == ed) return;
        shownSlot = selected;
        shownItem = h;
        shownEnchant = e;
        shownEnchDef = ed;
        Model.SetHeld(h);
    }
    HeldItem shownItem;
    EnchantDef shownEnchDef;

    // ================================================================
    // Hotbar and using things
    // ================================================================

    void ItemsUpdate(float dt, bool control)
    {
        // weapons cool down
        foreach (HeldItem it in slots)
        {
            if (it == null) continue;
            it.cooldown = Mathf.Max(0f, it.cooldown - dt);
            if (it.reloadLeft > 0f)
            {
                it.reloadLeft -= dt;
                if (it.reloadLeft <= 0f)
                {
                    GunDef rg = Guns.Get(it.id);
                    it.ammo = rg != null ? rg.mag : 0;
                    it.reloadLeft = 0f;
                    if (it == Held) Sfx.Play("craft_done", 0.35f, 1.6f);
                }
            }
            if (!(Firing && it == Held)) it.spin = Mathf.MoveTowards(it.spin, 0f, dt * 1.2f);
            float cool = (it.overheated ? 45f : 32f) * CoolMul;
            if (!(Firing && it == Held)) it.heat = Mathf.Max(0f, it.heat - cool * dt);
            if (it.overheated && it.heat <= 25f) it.overheated = false;
        }

        if (control && !Model.Busy && !Working)
        {
            if (K != null)
            {
                Key[] keys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8 };
                for (int i = 0; i < SlotCount; i++) if (K[keys[i]].wasPressedThisFrame) Select(i);
                if (K.qKey.wasPressedThisFrame) QuickHeal();
                if (K.rKey.wasPressedThisFrame && Held != null) StartReload(Held, Guns.Get(Held.id));
                if (K.iKey.wasPressedThisFrame) GameHUD.ToggleWeaponInfo();
                if (K.vKey.wasPressedThisFrame)
                {
                    Settings.FirstPerson = !Settings.FirstPerson;
                    Settings.Save();
                    GameHUD.Toast(Settings.FirstPerson ? "Camera: 1st person" : "Camera: 3rd person", Color.white);
                }
            }
            if (M != null)
            {
                float wheel = M.scroll.ReadValue().y;
                if (wheel > 0.1f) Cycle(-1);
                else if (wheel < -0.1f) Cycle(1);
            }
            if (G != null)
            {
                if (G.rightShoulder.wasPressedThisFrame) Cycle(1);
                if (G.leftShoulder.wasPressedThisFrame) Cycle(-1);
                if (G.buttonNorth.wasPressedThisFrame) QuickHeal();
            }
        }
        RefreshHeld();

        bool firePressed = control && ((M != null && M.leftButton.isPressed) || (G != null && G.rightTrigger.ReadValue() > 0.4f));
        bool fireDown = control && ((M != null && M.leftButton.wasPressedThisFrame) || (G != null && G.rightTrigger.ReadValue() > 0.4f && !triggerWasDown));
        triggerWasDown = G != null && G.rightTrigger.ReadValue() > 0.4f;
        Firing = false;

        HeldItem h = Held;
        if (h != null && control && !Model.Busy && !Working && gatherNode == null)
        {
            ItemKind kind = h.R.kind;
            GunDef hg = kind == ItemKind.Weapon ? Guns.Get(h.id) : null;
            if (hg != null && hg.charge > 0f) Firing = ChargeWeapon(h, hg, firePressed);
            else if (kind == ItemKind.Weapon && firePressed) Firing = FireWeapon(h, fireDown);
            else if (kind == ItemKind.Potion && fireDown) StartDrink(h);
            else if (kind == ItemKind.Throwable && fireDown) StartThrow(h);
        }
        if (!Firing || h == null || h.id != ItemId.FreezeRay) StopBeamSound();
        BurstUpdate();

        // drinking / throwing finish part-way through the animation
        if (drinkAt > 0f && Time.time >= drinkAt) { drinkAt = -1f; FinishDrink(); }
        if (throwAt > 0f && Time.time >= throwAt) { throwAt = -1f; FinishThrow(); }

        // crafting at a bench
        if (workLeft > 0f)
        {
            workLeft -= dt;
            if (Random.value < dt * 6f) Sfx.Play("tinker", 0.35f, Random.Range(0.85f, 1.2f));
            if (workLeft <= 0f) FinishWork();
        }
    }
    bool triggerWasDown;

    void Select(int i)
    {
        if (i == selected) return;
        selected = i;
        Sfx.Play("select", 0.4f);
    }

    void Cycle(int dir)
    {
        // skip empty slots
        for (int k = 1; k <= SlotCount; k++)
        {
            int i = (selected + dir * k + SlotCount * 4) % SlotCount;
            if (slots[i] != null) { Select(i); return; }
        }
    }

    void QuickHeal()
    {
        for (int i = 0; i < SlotCount; i++)
            if (slots[i] != null && slots[i].id == ItemId.HealPotion)
            {
                selected = i;
                RefreshHeld();
                StartDrink(slots[i]);
                return;
            }
        GameHUD.Toast("No Healing Potions. (Maybe the Brewing Stand can make some...)", Color.white);
    }

    // ---------------- weapons ----------------

    bool FireWeapon(HeldItem h, bool fireDown)
    {
        if (h.overheated)
        {
            if (fireDown) Sfx.Play("overheat_click", 0.5f);
            return false;
        }
        GunDef gun = Guns.Get(h.id);
        if (gun != null && gun.mag > 0)
        {
            if (h.ammo < 0) h.ammo = gun.mag;
            if (h.reloadLeft > 0f) return false;
            if (h.ammo <= 0) { StartReload(h, gun); if (fireDown) Sfx.Play("overheat_click", 0.5f); return false; }
        }
        if (h.cooldown > 0f) return h.id == ItemId.FreezeRay || (gun != null && gun.mode == GunMode.Beam);
        if (gun != null && !gun.auto && !fireDown) return false;          // click for every shot
        Transform muzzle = Model.Muzzle;
        if (muzzle == null) return false;
        Vector3 from = muzzle.position;
        if (gun != null) { FireGun(h, gun, from); return true; }
        Vector3 aimPoint = GameCam.I.AimPoint(h.id == ItemId.FreezeRay ? 22f : 250f);
        Vector3 dir = (aimPoint - from).normalized;
        Color ec = h.enchant != Element.None ? Items.ElementColor(h.enchant) : h.R.color;
        switch (h.id)
        {
            case ItemId.Zapper:
                h.cooldown = 0.19f;
                AddHeat(h, 8f);
                HitScan(from, dir, 80f, 11f, h.enchant, ec, true);
                Model.Kick(0.35f);
                GameCam.Kick(0.35f);
                Sfx.Play("zap", 0.55f, Random.Range(0.92f, 1.1f));
                Fx.Flash(from, ec, 4f, 3f, 0.06f);
                break;
            case ItemId.FreezeRay:
                h.cooldown = 0.1f;
                AddHeat(h, 3.6f);
                FreezeBeam(from, dir, h.enchant, ec);
                Model.Kick(0.08f);
                if (beamLoop == null) beamLoop = Sfx.Loop("freeze_loop", transform, 0.5f, 25f);
                break;
            case ItemId.PlasmaLauncher:
                h.cooldown = 0.85f;
                AddHeat(h, 34f);
                Projectile.Fire(Projectile.Kind.Plasma, from + dir * 0.2f, dir * 34f, Team, h.enchant, true);
                Model.Kick(1.4f);
                GameCam.Kick(2.2f);
                push -= dir * 1.5f;
                Sfx.Play("plasma_fire", 0.7f, Random.Range(0.95f, 1.05f));
                Fx.Flash(from, ec, 6f, 4f, 0.1f);
                Fx.Burst(from, ec, 8, 3f, 0.08f, 0.25f, 0f);
                break;
            case ItemId.SlimeCannon:
                h.cooldown = 0.55f;
                AddHeat(h, 18f);
                Vector3 lob = (dir + Vector3.up * 0.06f).normalized * 26f;
                Projectile.Fire(Projectile.Kind.Slime, from + dir * 0.2f, lob, Team, h.enchant, true);
                Model.Kick(0.8f);
                GameCam.Kick(1f);
                Sfx.Play("slime_fire", 0.6f, Random.Range(0.9f, 1.1f));
                Fx.Burst(from, new Color(0.4f, 1f, 0.45f), 6, 2f, 0.07f, 0.3f);
                break;
        }
        return true;
    }

    // ================================================================
    // Table guns (Guns.cs): one bit of code fires all of them
    // ================================================================

    HeldItem burstItem;
    int burstLeft;
    float burstAt, beamSfxCd;
    int beamTick;

    void FireGun(HeldItem h, GunDef g0, Vector3 from, float chargeMul = 1f)
    {
        GunDef g = Guns.Effective(g0, h.enchantDef, this);     // enchant + potions change the numbers
        if (g.cycle) { g.element = CycleOrder[cycleIdx++ % CycleOrder.Length]; g.color = Items.ElementColor(g.element); }
        if (chargeMul != 1f) { g.damage *= chargeMul; g.blast *= Mathf.Lerp(1f, 1.4f, (chargeMul - 1f) / Mathf.Max(0.01f, g.chargeBonus)); g.size *= 1f + (chargeMul - 1f) * 0.2f; }
        float cd = g.cooldown;
        if (g.spinUp > 0f)
        {
            cd *= Mathf.Lerp(3f, 1f, h.spin);              // minigun: starts slow, spins up
            h.spin = Mathf.Min(1f, h.spin + Mathf.Max(0.03f, cd) / g.spinUp);
        }
        h.cooldown = cd;
        if (g.mag > 0)
        {
            h.ammo--;
            if (h.ammo <= 0) StartReload(h, g0);
        }
        else AddHeat(h, g.heat);
        if (g.burst > 1) { burstItem = h; burstLeft = g.burst - 1; burstAt = Time.time + g.burstGap; }
        AttackExtras(h, g);                                   // lunges, blinks, boosts, walls, decoys, orbs...
        if (g.mode == GunMode.Gadget) { Model.Kick(g.kick); Sfx.Play(g.sound, g.volume, g.pitch); return; }
        if (g.mode == GunMode.Melee) { MeleeSwing(g); return; }
        if (g.mode == GunMode.Wave) { WaveBlast(g); return; }
        if (g.strike > 0f) { CallStrike(g, from); return; }
        if (g.turret) { DeployTurret(g, from); return; }
        GunShot(h, g, from);
    }

    // ================================================================
    // Melee, shockwaves and the extra things some weapons do when you attack
    // ================================================================

    static readonly Element[] CycleOrder = { Element.Fire, Element.Ice, Element.Poison, Element.Shock };
    int cycleIdx;
    static readonly System.Collections.Generic.Dictionary<ItemId, PotionDef> quickBuffs = new System.Collections.Generic.Dictionary<ItemId, PotionDef>();

    // A shield is up when you hold right-click with a 'block' weapon.
    public bool Blocking
    {
        get
        {
            HeldItem h = Held;
            if (h == null || !Aiming || dead) return false;
            GunDef g = Guns.Get(h.id);
            return g != null && g.block;
        }
    }

    Vector3 FlatAim => Quaternion.Euler(0f, GameCam.I.yaw, 0f) * Vector3.forward;

    void AttackExtras(HeldItem h, GunDef g)
    {
        Vector3 fwd = FlatAim;
        float yaw = GameCam.I.yaw;
        if (g.lunge > 0f)
        {
            push += fwd * (g.lunge * 3.2f);
            velocity.y = Mathf.Max(velocity.y, 2.5f);
            Fx.Puff(transform.position + Vector3.up * 0.3f, new Color(1f, 1f, 1f, 0.35f), 4, 0.3f, 0.3f, 0.4f, -fwd);
        }
        if (g.blink > 0f) Blink(g.blink, g.color);
        if (g.buffSpeed > 1f || g.buffDamage > 1f)
        {
            if (!quickBuffs.TryGetValue(g.id, out PotionDef pd))
            {
                pd = new PotionDef { id = g.id, time = 4f, speed = g.buffSpeed, dealt = g.buffDamage };
                quickBuffs[g.id] = pd;
            }
            DrinkPotion(pd, h.R);
        }
        if (g.heal > 0f) { HealFrom(g.heal); Fx.Burst(Center, new Color(0.4f, 1f, 0.5f), 6, 2f, 0.07f, 0.5f, 2f); }
        if (g.barrier) Barrier.Spawn(transform.position + fwd * 2.6f, yaw, g.fieldTime, g.color);
        if (g.decoy) Decoy.Spawn(transform.position + fwd * 1.8f, yaw, Team, Mathf.Max(15f, g.damage), g.element, g.color, g.fieldTime);
        if (g.healZone > 0f) HealZone.Spawn(transform.position, g.healZone, g.fieldTime, Team);
        if (g.orbit > 0) Orbiters.Spawn(transform, g.orbit, g.fieldTime, Mathf.Max(4f, g.damage * 0.5f), Team, g.element, g.color);
        if (g.reveal)
        {
            foreach (IDamageable d in GunToys.Near(Center, 30f, Team)) if (!(d is Laboratory)) Marks.Add(d, Mathf.Max(0.25f, g.mark));
            Fx.Ring(Center, new Color(g.color.r, g.color.g, g.color.b, 0.5f), 30, 18f, 0.15f);
            Sfx.Play("zone", 0.5f, 1.8f);
        }
    }

    // Teleport a short way towards where you're aiming (stops before walls).
    void Blink(float dist, Color c)
    {
        Vector3 dir = GameCam.I.transform.forward;
        dir.y = Mathf.Max(dir.y, -0.2f);
        dir.Normalize();
        Vector3 from = Center;
        Vector3 to = from + dir * dist;
        if (Physics.SphereCast(from, 0.4f, dir, out RaycastHit hit, dist, Combat.NotPlayer, QueryTriggerInteraction.Ignore)) to = hit.point - dir * 0.7f;
        Vector3 feet = to - Vector3.up * 1.1f;
        float ground = MLGame.Ground(feet).y;
        if (feet.y < ground) feet.y = ground + 0.05f;
        Fx.Burst(from, c, 14, 4f, 0.1f, 0.4f, 0f);
        cc.enabled = false;
        transform.position = feet;
        cc.enabled = true;
        Fx.Burst(Center, c, 14, 4f, 0.1f, 0.4f, 0f);
        Sfx.Play("zap", 0.5f, 1.6f);
    }

    // Swing at everything in front of you (or all around you).
    void MeleeSwing(GunDef g)
    {
        Vector3 fwd = FlatAim;
        Vector3 origin = Center;
        Model.PlaySwing(g.arc >= 300f ? 0.45f : 0.28f, g.arc >= 300f);
        var done = new System.Collections.Generic.HashSet<IDamageable>();
        foreach (Collider col in Physics.OverlapSphere(origin + fwd * g.reach * 0.4f, g.reach + 0.6f, ~0, QueryTriggerInteraction.Ignore))
        {
            IDamageable d = Combat.Find(col);
            if (d == null || done.Contains(d) || !d.Alive || !Teams.Hostile(Team, d.Team)) continue;
            Vector3 cp = Combat.Closest(col, origin);
            Vector3 to = cp - origin;
            to.y = 0f;
            if (to.magnitude > g.reach + 0.5f) continue;
            if (g.arc < 300f && to.sqrMagnitude > 0.01f && Vector3.Angle(fwd, to) > g.arc * 0.5f) continue;
            done.Add(d);
            Strike(d, g, g.damage, cp, to.sqrMagnitude > 0.01f ? to.normalized : fwd, g.element);
            Fx.Burst(cp, g.color, 8, 4f, 0.08f, 0.3f);
        }
        // the swoosh
        int n = 9;
        for (int k = 0; k < n; k++)
        {
            float a = g.arc >= 300f ? k * 360f / n : -g.arc * 0.5f + g.arc * k / (n - 1);
            Vector3 p = origin + Quaternion.Euler(0f, a, 0f) * fwd * (g.reach * 0.75f) + Vector3.up * Random.Range(-0.2f, 0.2f);
            Fx.Burst(p, new Color(g.color.r, g.color.g, g.color.b, 0.8f), 1, 0.8f, 0.14f, 0.22f, 0f);
        }
        if (g.wave > 0f) Fx.Ring(transform.position + Vector3.up * 0.1f, g.color, 18, g.wave * 3f, 0.25f);
        Model.Kick(g.kick);
        GameCam.Kick(done.Count > 0 ? g.kick * 1.6f : g.kick * 0.6f);
        Sfx.Play(g.sound, g.volume, g.pitch * Random.Range(0.92f, 1.08f));
        if (done.Count > 0) Sfx.Play("hitmarker", 0.35f, 0.7f);
    }

    // A shockwave / gust / sound blast from where you stand (a cone in front, or a ring all around).
    void WaveBlast(GunDef g)
    {
        Vector3 fwd = FlatAim;
        Vector3 origin = Center;
        float r = Mathf.Max(2f, g.wave);
        Model.PlaySwing(g.arc >= 300f ? 0.45f : 0.3f, g.arc >= 300f);
        foreach (IDamageable d in GunToys.Near(origin, r, Team))
        {
            if (d is Player) continue;
            Vector3 to = d.Center - origin;
            to.y = 0f;
            if (g.arc < 300f && to.sqrMagnitude > 0.01f && Vector3.Angle(fwd, to) > g.arc * 0.5f) continue;
            Strike(d, g, g.damage, d.Center, to.sqrMagnitude > 0.01f ? to.normalized : fwd, g.element);
        }
        if (g.arc >= 300f) Fx.Ring(origin - Vector3.up * 0.8f, new Color(g.color.r, g.color.g, g.color.b, 0.7f), 32, r * 3.2f, 0.3f);
        else
            for (int k = 0; k < 12; k++)
            {
                Vector3 dir = Quaternion.Euler(0f, Random.Range(-g.arc * 0.5f, g.arc * 0.5f), 0f) * fwd;
                Fx.Puff(origin + dir * 0.8f, new Color(g.color.r, g.color.g, g.color.b, 0.4f), 1, 0.2f, 0.35f, 0.45f, dir * r * 2.2f);
            }
        Fx.Flash(origin, g.color, r * 2f, 2.5f, 0.15f);
        GameCam.Shake(origin, 0.25f);
        Model.Kick(g.kick);
        Sfx.Play(g.sound, g.volume, g.pitch * Random.Range(0.94f, 1.06f));
    }

    // ---- magazines ----
    public void StartReload(HeldItem h, GunDef g)
    {
        if (h == null || g == null || g.mag <= 0 || h.reloadLeft > 0f) return;
        if (h.ammo < 0 || h.ammo >= g.mag) return;      // already full
        h.reloadLeft = g.reload;
        Sfx.Play("tinker", 0.6f, 1.3f);
    }

    // ---- charge shots: hold to charge, let go to fire ----
    bool ChargeWeapon(HeldItem h, GunDef g, bool held)
    {
        if (h.overheated || h.reloadLeft > 0f || Model.Muzzle == null) { h.chargeT = 0f; return false; }
        if (held && h.cooldown <= 0f)
        {
            float before = h.chargeT;
            h.chargeT = Mathf.Min(g.charge, h.chargeT + Time.deltaTime);
            if (h.chargeT >= g.charge && before < g.charge) Sfx.Play("potion_ready", 0.4f, 1.6f);
            if (Random.value < 0.5f) Fx.Burst(Model.Muzzle.position, g.color, 1, 1.5f * h.chargeT / g.charge, 0.05f + 0.08f * h.chargeT / g.charge, 0.2f, 0f);
            return true;
        }
        if (!held && h.chargeT > 0f)
        {
            float k = h.chargeT / g.charge;
            h.chargeT = 0f;
            if (k < 0.15f) return false;                     // just a tap: nothing
            FireGun(h, g, Model.Muzzle.position, 1f + g.chargeBonus * k);
            return true;
        }
        return false;
    }

    // ---- airstrike: mark the spot you're aiming at ----
    void CallStrike(GunDef g, Vector3 from)
    {
        Vector3 at = GameCam.I.AimPoint(Mathf.Max(30f, g.range));
        StrikeMarker.Spawn(at, g.strike, g.damage, Mathf.Max(3f, g.blast), g.element, g.color, Team, true, g.structure);
        Fx.Beam(from, at, new Color(g.color.r, g.color.g, g.color.b, 0.5f), 0.03f, 0.15f, false);
        Model.Kick(g.kick);
        Sfx.Play(g.sound, g.volume, g.pitch);
    }

    // ---- turret gun: throw down a mini turret in front of you ----
    void DeployTurret(GunDef g, Vector3 from)
    {
        Vector3 fwd = Quaternion.Euler(0f, GameCam.I.yaw, 0f) * Vector3.forward;
        Vector3 at = transform.position + fwd * 2.2f;
        MiniTurret.Spawn(at, GameCam.I.yaw, Team, Mathf.Max(4f, g.damage), g.element, g.color, g.fieldTime > 0f ? g.fieldTime : 15f);
        Model.Kick(g.kick);
        Sfx.Play("throw", 0.6f);
    }

    // Burst guns: the rest of the burst comes out a moment later.
    void BurstUpdate()
    {
        if (burstLeft <= 0 || Time.time < burstAt) return;
        if (burstItem != Held || dead || Model.Muzzle == null) { burstLeft = 0; return; }
        GunDef g0 = Guns.Get(burstItem.id);
        if (g0 == null) { burstLeft = 0; return; }
        GunDef g = Guns.Effective(g0, burstItem.enchantDef, this);
        burstLeft--;
        burstAt = Time.time + g.burstGap;
        if (g.mode == GunMode.Melee) MeleeSwing(g);
        else if (g.mode == GunMode.Wave) WaveBlast(g);
        else if (g.mode != GunMode.Gadget) GunShot(burstItem, g, Model.Muzzle.position);
    }

    void GunShot(HeldItem h, GunDef g, Vector3 from)
    {
        Element el = g.element;
        Color c = g.color;
        float range = g.mode == GunMode.Beam ? g.range : Mathf.Max(g.range, 30f);
        Vector3 aim = GameCam.I.AimPoint(g.mode == GunMode.Projectile ? 250f : range);
        Vector3 dir = (aim - from).normalized;
        float spreadMul = Aiming ? 0.5f : 1f;
        for (int i = 0; i < Mathf.Max(1, g.pellets); i++)
        {
            Vector3 d = dir;
            if (g.spread > 0f)
            {
                Vector2 r = Random.insideUnitCircle * g.spread * spreadMul;
                d = Quaternion.AngleAxis(r.x, Vector3.up) * Quaternion.AngleAxis(r.y, Vector3.Cross(dir, Vector3.up).normalized) * dir;
            }
            switch (g.mode)
            {
                case GunMode.Hitscan: GunRay(from, d, g, el, c); break;
                case GunMode.Beam: GunBeam(from, d, g, el, c); break;
                case GunMode.Projectile: Projectile.FireGun(g, from + d * 0.2f, d * g.speed + Vector3.up * (g.gravity < -4f ? 1.5f : 0f), Team, el, c, true); break;
            }
        }
        Model.Kick(g.kick);
        GameCam.Kick(g.kick * 1.4f);
        if (g.force > 3f && g.mode != GunMode.Beam) push -= dir * g.kick;
        if (g.selfKnock > 0f)
        {
            // recoil jump: aim at the ground and fire to fly
            push -= dir * g.selfKnock;
            if (dir.y < -0.3f) velocity.y = Mathf.Max(velocity.y, g.selfKnock * -dir.y);
        }
        if (g.mode == GunMode.Beam && g.id == ItemId.FreezeRay)
        {
            if (beamLoop == null) beamLoop = Sfx.Loop("freeze_loop", transform, 0.5f, 25f);
        }
        else if (g.mode == GunMode.Beam)
        {
            beamSfxCd -= g.cooldown;
            if (beamSfxCd <= 0f) { beamSfxCd = 0.3f; Sfx.Play(g.sound, g.volume, g.pitch * Random.Range(0.92f, 1.08f)); }
        }
        else
        {
            Sfx.Play(g.sound, g.volume, g.pitch * Random.Range(0.94f, 1.06f));
            Fx.Flash(from, c, 4f, 3f, 0.06f);
            Fx.Burst(from, c, 3, 2f, 0.05f, 0.15f, 0f);
        }
    }

    Hit MakeHit(GunDef g, float damage, Vector3 point, Vector3 dir, Element el, bool dot = false)
        => new Hit { damage = damage, point = point, dir = dir, force = g.force, element = el, team = Team, structureMul = g.structure, fromPlayer = true, dot = dot };

    // One hit on something: crits, life steal and little enchant explosions happen here.
    void Strike(IDamageable d, GunDef g, float damage, Vector3 point, Vector3 dir, Element el, bool dot = false)
    {
        if (g.crit > 0f && !dot && Random.value < g.crit)
        {
            damage *= 2f;
            Fx.Burst(point, new Color(1f, 0.95f, 0.4f), 8, 4f, 0.08f, 0.3f);
        }
        damage *= Marks.Bonus(d);                         // marked targets take extra damage
        Hit hit = MakeHit(g, damage, point, dir, el, dot);
        if (g.pull != 0f && !dot)
        {
            // harpoons and hooks pull it towards you; big blasts shove it away
            Vector3 toMe = Center - d.Center;
            toMe.y = 0f;
            hit.dir = (g.pull > 0f ? toMe : -toMe).normalized;
            hit.force = Mathf.Abs(g.pull);
        }
        d.TakeHit(hit);
        if (g.mark > 0f && !dot) Marks.Add(d, g.mark);
        if (g.lifesteal > 0f && !(d is Laboratory)) HealFrom(damage * g.lifesteal);
        if (g.hitBlast > 0f && !dot) Combat.MiniBlast(point - dir * 0.2f, g.hitBlast, damage * 0.5f, Team, el, g.structure, true, g.color);
    }

    // Instant bullets / lasers. Pierce = goes through things in a line. Chain = lightning that jumps on.
    void GunRay(Vector3 from, Vector3 dir, GunDef g, Element el, Color c)
    {
        Vector3 end = from + dir * g.range;
        Vector3 to = end;
        if (Laboratory.ShieldBlocks(from, end, Team, out Vector3 sp, out Laboratory lab))
        {
            end = sp;
            to = sp;
            lab.ShieldHit(g.damage * g.structure, sp, true);
        }
        float len = Vector3.Distance(from, end);
        IDamageable first = null;
        Vector3 firstPoint = to;
        if (g.pierce || g.wallPierce)
        {
            RaycastHit[] hits = Physics.RaycastAll(from, dir, len, Combat.NotPlayer, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            var done = new System.Collections.Generic.HashSet<IDamageable>();
            int pierced = 0;
            foreach (RaycastHit hit in hits)
            {
                IDamageable d = Combat.Find(hit.collider);
                if (d == null)
                {
                    if (g.wallPierce) { Fx.Burst(hit.point, c, 4, 2f, 0.05f, 0.25f); continue; }       // X-ray: straight through walls
                    to = hit.point; Fx.Burst(hit.point + hit.normal * 0.05f, c, 10, 4f, 0.07f, 0.35f); break;   // a wall stops it
                }
                if (done.Contains(d)) continue;
                done.Add(d);
                if (!Teams.Hostile(Team, d.Team)) { to = hit.point; break; }     // your own lab's wall stops it
                if (d.Alive && Teams.Hostile(Team, d.Team))
                {
                    Strike(d, g, g.damage * (pierced == 0 ? 1f : 0.75f), hit.point, dir, el);
                    Fx.Burst(hit.point, c, 8, 4f, 0.07f, 0.3f);
                    if (first == null) { first = d; firstPoint = hit.point; }
                    pierced++;
                }
                if (pierced >= 4 || (!g.pierce && pierced >= 1)) { to = hit.point; break; }
            }
        }
        else
        {
            // one shot that can bounce off walls (ricochet) and pull you along (grapple)
            float remain = len;
            int bouncesLeft = g.ricochet;
            bool grappled = false;
            while (true)
            {
                if (!Physics.Raycast(from, dir, out RaycastHit hit, remain, Combat.NotPlayer, QueryTriggerInteraction.Ignore)) { to = from + dir * remain; break; }
                to = hit.point;
                IDamageable d = Combat.Find(hit.collider);
                if (g.grapple && !grappled) { grappled = true; GrappleTo(hit.point); }
                if (d != null && d.Alive && Teams.Hostile(Team, d.Team))
                {
                    Strike(d, g, g.damage, hit.point, dir, el);
                    first = d;
                    firstPoint = hit.point;
                    Fx.Burst(hit.point + hit.normal * 0.05f, c, g.pellets > 1 ? 3 : 7, 3.5f, 0.06f, 0.3f);
                    break;
                }
                if (d == null && bouncesLeft > 0)
                {
                    bouncesLeft--;
                    Fx.Beam(from, hit.point, c, 0.05f, 0.1f, g.lightning);
                    Fx.Burst(hit.point, c, 5, 3f, 0.05f, 0.25f);
                    remain -= hit.distance;
                    from = hit.point + hit.normal * 0.05f;
                    dir = Vector3.Reflect(dir, hit.normal);
                    continue;
                }
                if (g.hitBlast > 0f) Combat.MiniBlast(hit.point + hit.normal * 0.2f, g.hitBlast, g.damage * 0.5f, Team, el, g.structure, true, g.color);
                Fx.Burst(hit.point + hit.normal * 0.05f, c, g.pellets > 1 ? 3 : 7, 3.5f, 0.06f, 0.3f);
                break;
            }
        }
        float width = Mathf.Clamp(g.damage / 300f, 0.025f, 0.12f) + (g.pierce ? 0.08f : 0f);
        Fx.Beam(from, to, c, width + 0.03f, g.pierce ? 0.25f : 0.08f, g.lightning);
        Fx.Beam(from, to, new Color(1f, 1f, 1f, 0.8f), width * 0.4f, 0.06f, g.lightning);

        // lightning jumps to the next closest targets
        if (g.chain > 0 && first != null)
        {
            var hitSet = new System.Collections.Generic.HashSet<IDamageable> { first };
            Vector3 at = firstPoint;
            float dmg = g.damage;
            for (int j = 0; j < g.chain; j++)
            {
                IDamageable next = null;
                float best = 9f;
                foreach (Collider col in Physics.OverlapSphere(at, 9f, ~0, QueryTriggerInteraction.Ignore))
                {
                    IDamageable d = Combat.Find(col);
                    if (d == null || hitSet.Contains(d) || !d.Alive || !Teams.Hostile(Team, d.Team) || d is Laboratory) continue;
                    float dist = Vector3.Distance(at, d.Center);
                    if (dist < best) { best = dist; next = d; }
                }
                if (next == null) break;
                hitSet.Add(next);
                dmg *= 0.7f;
                Strike(next, g, dmg, next.Center, (next.Center - at).normalized, el);
                Fx.Beam(at, next.Center, c, 0.06f, 0.15f, true);
                Fx.Burst(next.Center, c, 6, 3f, 0.06f, 0.3f);
                at = next.Center;
            }
        }
    }

    // Short spray you hold down (flamethrower, sap sprayer).
    float beamFieldAt;

    void GunBeam(Vector3 from, Vector3 dir, GunDef g, Element el, Color c)
    {
        Vector3 to = from + dir * g.range;
        bool landed = false;
        beamTick++;
        if (Laboratory.ShieldBlocks(from, to, Team, out Vector3 sp, out Laboratory lab))
        {
            to = sp;
            lab.ShieldHit(g.damage * g.structure, sp, true);
        }
        else if (Physics.SphereCast(from, 0.35f, dir, out RaycastHit hit, g.range, Combat.NotPlayer, QueryTriggerInteraction.Ignore))
        {
            to = hit.point;
            landed = true;
            IDamageable d = Combat.Find(hit.collider);
            // every 3rd tick counts as a proper hit (puts the element on, makes them flinch); the rest just hurt
            if (d != null && d.Alive && Teams.Hostile(Team, d.Team)) Strike(d, g, g.damage, hit.point, dir, el, beamTick % 3 != 0);
        }
        // the spray itself
        for (int k = 0; k < 3; k++)
        {
            Vector3 p = Vector3.Lerp(from, to, Random.Range(0.1f, 1f)) + Random.insideUnitSphere * 0.25f;
            if (el == Element.Fire) Fx.Burst(p, Color.Lerp(c, new Color(1f, 0.9f, 0.3f), Random.value), 1, 1.2f, 0.18f, 0.35f, 2f);
            else Fx.Burst(p, c, 1, 0.6f, 0.12f, 0.4f, -4f);
        }
        if (el == Element.Fire && Random.value < 0.3f) Fx.Puff(to, new Color(0.25f, 0.25f, 0.25f, 0.35f), 1, 0.2f, 0.4f, 0.8f, Vector3.up);
        Fx.Beam(from, to, new Color(c.r, c.g, c.b, 0.45f), 0.22f, 0.1f, false);
        // 'field' beams leave a burning / freezing / poison patch where they hit
        if (g.field > 0f && landed && Time.time >= beamFieldAt)
        {
            beamFieldAt = Time.time + 0.45f;
            DamageField.Spawn(to, g.field, g.fieldTime, g.damage * 2f + 3f, el, c, Team, true, g.structure, g.blackHole);
        }
    }

    void AddHeat(HeldItem h, float amount)
    {
        h.heat += amount;
        if (h.heat >= 100f)
        {
            h.heat = 100f;
            h.overheated = true;
            h.cooldown = 0.3f;
            Sfx.Play("overheat", 0.6f);
            Fx.Puff(Model.Muzzle != null ? Model.Muzzle.position : Center, new Color(0.9f, 0.9f, 0.9f, 0.4f), 5, 0.1f, 0.2f, 1f, Vector3.up);
        }
    }

    // Instant shot: the zapper's lightning bolt.
    void HitScan(Vector3 from, Vector3 dir, float range, float damage, Element ench, Color c, bool lightning)
    {
        Vector3 end = from + dir * range;
        Vector3 to = end;
        if (Laboratory.ShieldBlocks(from, end, Team, out Vector3 sp, out Laboratory lab))
        {
            to = sp;
            lab.ShieldHit(damage, sp, true);
        }
        else if (Physics.Raycast(from, dir, out RaycastHit hit, range, Combat.NotPlayer, QueryTriggerInteraction.Ignore))
        {
            to = hit.point;
            IDamageable d = Combat.Find(hit.collider);
            if (d != null && d.Alive && Teams.Hostile(Team, d.Team))
                d.TakeHit(new Hit { damage = damage, point = hit.point, dir = dir, force = 0.8f, element = ench, team = Team, structureMul = 1f, fromPlayer = true });
            Fx.Burst(hit.point + hit.normal * 0.05f, c, 7, 3.5f, 0.06f, 0.3f);
        }
        Fx.Beam(from, to, c, 0.07f, 0.09f, lightning);
        Fx.Beam(from, to, new Color(1f, 1f, 1f, 0.8f), 0.025f, 0.06f, lightning);
    }

    // Freeze Ray: short spray that chills and then freezes.
    void FreezeBeam(Vector3 from, Vector3 dir, Element ench, Color c)
    {
        const float range = 22f;
        Vector3 to = from + dir * range;
        if (Laboratory.ShieldBlocks(from, to, Team, out Vector3 sp, out Laboratory lab))
        {
            to = sp;
            lab.ShieldHit(2.5f, sp, true);
        }
        else if (Physics.SphereCast(from, 0.25f, dir, out RaycastHit hit, range, Combat.NotPlayer, QueryTriggerInteraction.Ignore))
        {
            to = hit.point;
            IDamageable d = Combat.Find(hit.collider);
            if (d != null && d.Alive && Teams.Hostile(Team, d.Team))
            {
                d.TakeHit(new Hit { damage = 2.4f, point = hit.point, dir = dir, element = ench != Element.None ? ench : Element.None, team = Team, structureMul = 0.8f, fromPlayer = true, dot = ench == Element.None });
                if (d is TargetDummy td) td.status.Chill(0.09f, Team);
                else if (d is RaidDrone rd) rd.status.Chill(0.09f, Team);
            }
            Fx.Burst(hit.point, new Color(0.8f, 0.97f, 1f), 3, 2f, 0.07f, 0.4f, -2f);
        }
        Fx.Beam(from, to, new Color(c.r, c.g, c.b, 0.55f), 0.18f, 0.11f, false);
        Fx.Beam(from, to, new Color(1f, 1f, 1f, 0.6f), 0.05f, 0.08f, false);
        Fx.Puff(Vector3.Lerp(from, to, Random.value), new Color(0.85f, 0.97f, 1f, 0.35f), 1, 0.15f, 0.25f, 0.4f, Vector3.zero);
    }

    void StopBeamSound()
    {
        if (beamLoop != null) { Destroy(beamLoop.gameObject); beamLoop = null; }
    }

    // ---------------- potions and bombs ----------------

    void StartDrink(HeldItem h)
    {
        usingItem = h;
        Model.PlayDrink();
        drinkAt = Time.time + Scientist.DrinkTime * 0.6f;
        Sfx.Play("cork", 0.6f);
    }

    void FinishDrink()
    {
        HeldItem h = usingItem;
        usingItem = null;
        if (h == null || dead) return;
        Sfx.Play("gulp", 0.8f);
        Color c = h.R.color;
        Fx.Burst(Center + Vector3.up * 0.4f, c, 14, 3f, 0.08f, 0.6f, -2f);
        switch (h.id)
        {
            case ItemId.HealPotion: healLeft = 2f; status.burn = status.poison = 0f; break;
            case ItemId.SpeedPotion: speedLeft = 15f; break;
            case ItemId.LeapPotion: leapLeft = 15f; break;
            case ItemId.IronSkinPotion: ironLeft = 15f; break;
            default:
                PotionDef pd = Gear.Potion(h.id);
                if (pd != null) DrinkPotion(pd, h.R);
                break;
        }
        UseOne(h);
    }

    void StartThrow(HeldItem h)
    {
        usingItem = h;
        Model.PlayThrow();
        throwAt = Time.time + Scientist.ThrowRelease;
    }

    void FinishThrow()
    {
        HeldItem h = usingItem;
        usingItem = null;
        if (h == null || dead) return;
        Vector3 from = Model.HandPosition;
        Vector3 aim = GameCam.I.AimPoint(60f);
        Vector3 dir = (aim - from).normalized;
        PotionDef pd = Gear.Potion(h.id);
        if (pd != null && pd.bomb != null)
        {
            GunDef b = pd.bomb.Clone();
            b.damage *= DamageDealtMul;
            Projectile.FireGun(b, from, dir * b.speed + Vector3.up * 4f, Team, b.element, b.color, true);
        }
        else Projectile.Fire(Projectile.Kind.Toxic, from, dir * 18f + Vector3.up * 4f, Team, Element.Poison, true);
        Sfx.Play("throw", 0.6f);
        UseOne(h);
    }

    void UseOne(HeldItem h)
    {
        h.count--;
        if (h.count > 0) return;
        for (int i = 0; i < SlotCount; i++) if (slots[i] == h) slots[i] = null;
        if (Held == null) Cycle(-1);
    }

    void CancelUse()
    {
        drinkAt = throwAt = -1f;
        usingItem = null;
        gatherNode = null;
        workLeft = 0f;
        workRecipe = null;
        StopBeamSound();
    }

    // ================================================================
    // E: gathering, stations
    // ================================================================

    void InteractUpdate(float dt, bool control)
    {
        // walking into your own lab drops your materials into its storage
        bool inLab = InsideOwnLab;
        if (inLab && !wasInLab) StoreBackpack();
        wasInLab = inLab;

        // gathering in progress
        if (gatherNode != null)
        {
            gatherT += dt;
            if (!gatherNode.Available || Vector3.Distance(transform.position, gatherNode.InteractPoint) > gatherNode.InteractRange + 1.5f) { gatherNode = null; }
            else if (gatherT >= Scientist.GatherTime * 0.85f)
            {
                gatherNode.Harvest(this);
                gatherNode = null;
            }
        }

        PromptText = null;
        PromptUsable = false;
        if (!control || Working) return;
        IInteractable best = null;
        float bestScore = float.MaxValue;
        Vector3 fwd = GameCam.I.transform.forward;
        foreach (IInteractable it in Interactables.All)
        {
            if (it == null) continue;
            Vector3 to = it.InteractPoint - transform.position;
            to.y *= 0.5f;
            float d = to.magnitude;
            if (d > it.InteractRange) continue;
            float facing = Vector3.Dot(fwd, new Vector3(to.x, 0f, to.z).normalized);
            float score = d - facing * 1.2f;
            if (score < bestScore) { bestScore = score; best = it; }
        }
        if (best == null) return;
        PromptText = best.Prompt(this, out bool usable);
        PromptUsable = usable;
        bool press = (K != null && K.eKey.wasPressedThisFrame) || (G != null && G.buttonWest.wasPressedThisFrame);
        if (press && usable && !Model.Busy && gatherNode == null) best.Use(this);
    }

    public void StartGather(MaterialNode node)
    {
        gatherNode = node;
        gatherT = 0f;
        Model.PlayGather();
        Sfx.Play("gather", 0.5f, Random.Range(0.9f, 1.1f));
    }

    public void AddMaterial(int m, int n)
    {
        backpack[m] += n;
        GameHUD.MaterialPopup(m, n);
        if (InsideOwnLab) StoreBackpack();
    }

    void StoreBackpack()
    {
        if (MyLab == null || MyLab.Destroyed) return;
        int total = 0;
        string msg = "";
        for (int m = 0; m < Items.MatCount; m++)
        {
            if (backpack[m] <= 0) continue;
            MyLab.storage[m] += backpack[m];
            total += backpack[m];
            msg += (msg.Length > 0 ? ", " : "") + backpack[m] + " " + Items.MatNames[m];
            backpack[m] = 0;
        }
        if (total > 0)
        {
            GameHUD.Toast("Stored in your lab: " + msg, new Color(0.6f, 1f, 0.6f));
            Sfx.Play("store", 0.5f);
        }
    }

    // When you get knocked out you drop whatever you're carrying.
    void DropBackpack()
    {
        for (int m = 0; m < Items.MatCount; m++)
            for (int i = 0; i < backpack[m]; i++)
                MaterialPickup.Drop(transform.position + Vector3.up, m);
        for (int m = 0; m < Items.MatCount; m++) backpack[m] = 0;
    }

    // ---------------- crafting ----------------

    public int Have(int m) => backpack[m] + (MyLab != null && !MyLab.Destroyed ? MyLab.storage[m] : 0);

    public bool CanAfford(Recipe r)
    {
        for (int m = 0; m < Items.MatCount; m++) if (Have(m) < r.cost[m]) return false;
        return true;
    }

    void Spend(Recipe r)
    {
        for (int m = 0; m < Items.MatCount; m++)
        {
            int need = r.cost[m];
            int fromLab = MyLab != null ? Mathf.Min(need, MyLab.storage[m]) : 0;
            if (MyLab != null) MyLab.storage[m] -= fromLab;
            backpack[m] -= need - fromLab;
        }
    }

    // Why you can't make this right now (null = you can).
    public string CraftProblem(Recipe r)
    {
        if (MyLab == null || MyLab.Destroyed) return "Your lab is destroyed.";
        if (!InsideOwnLab) return "Get inside your lab.";
        if (Working) return "Busy...";
        if (r.needs.HasValue && !MyLab.Has(r.needs.Value)) return "Build " + Items.Get(r.needs.Value).name + " first.";
        switch (r.kind)
        {
            case ItemKind.Weapon:
            case ItemKind.Potion:
            case ItemKind.Throwable:
                if (!HasRoomFor(r.id)) return "Your hotbar is full.";
                if (r.kind != ItemKind.Weapon && !MyLab.BrewSlotFree) return "Cauldron is busy.";
                break;
            case ItemKind.Armor:
                if (r.id == ItemId.BounceBoots && bounceBoots) return "You're already wearing them.";
                if (suit != null && suit.id == r.id && armor >= suit.armor - 1f) return "You're already wearing it.";
                break;
            case ItemKind.Enchant:
                if (Held == null || Held.R.kind != ItemKind.Weapon) return "Hold a weapon to enchant it.";
                if (Held.enchantDef != null && Held.enchantDef.id == r.id) return "Already enchanted with that.";
                break;
            case ItemKind.Upgrade:
                string up = MyLab.UpgradeProblem(r.id);
                if (up != null) return up;
                break;
        }
        if (!CanAfford(r)) return "Not enough materials.";
        return null;
    }

    bool HasRoomFor(ItemId id)
    {
        Recipe r = Items.Get(id);
        if (r.kind != ItemKind.Weapon)
            foreach (HeldItem h in slots) if (h != null && h.id == id) return true;
        foreach (HeldItem h in slots) if (h == null) return true;
        return false;
    }

    public bool Craft(Recipe r)
    {
        if (CraftProblem(r) != null) { Sfx.Play("error", 0.5f); return false; }
        Spend(r);
        if (r.kind == ItemKind.Potion || r.kind == ItemKind.Throwable)
        {
            MyLab.StartBrew(r);
            Sfx.Play("brew_start", 0.6f);
            GameHUD.Toast("Brewing " + r.name + "... (" + Mathf.RoundToInt(r.time * MyLab.BrewSpeed) + "s) - you can walk away.", r.color);
            return true;
        }
        workRecipe = r;
        workTotal = workLeft = r.time;
        Model.PlayWork(r.time);
        Sfx.Play("craft_start", 0.5f);
        return true;
    }

    void FinishWork()
    {
        Recipe r = workRecipe;
        workRecipe = null;
        workLeft = 0f;
        if (r == null) return;
        switch (r.kind)
        {
            case ItemKind.Weapon:
                GiveItem(r.id, 1);
                break;
            case ItemKind.Armor:
                if (r.id == ItemId.BounceBoots) { bounceBoots = true; Model.SetArmor(armorTier, bounceBoots, suit); }
                else
                {
                    ArmorDef a = Gear.Armor(r.id);
                    if (a != null) Equip(a);
                }
                break;
            case ItemKind.Enchant:
                if (Held != null && Held.R.kind == ItemKind.Weapon)
                {
                    Held.enchantDef = Gear.Enchant(r.id);
                    Held.enchant = Held.enchantDef != null ? Held.enchantDef.element : Items.EnchantElement(r.id);
                }
                Fx.Burst(Model.Muzzle != null ? Model.Muzzle.position : Center, r.color, 20, 3f, 0.08f, 0.8f, 1f);
                break;
            case ItemKind.Upgrade:
                MyLab.Build(r.id);
                break;
        }
        Sfx.Play("craft_done", 0.8f);
        GameHUD.Toast(r.name + (r.kind == ItemKind.Upgrade ? " built!" : r.kind == ItemKind.Enchant ? " applied!" : " crafted!"), r.color);
    }

    // EXPERIMENTING: try a mix of materials at a station. If it's a real recipe you make it (and discover it).
    // Wrong mixes don't cost anything. Returns true if it made something.
    public bool Experiment(Station st, int[] mix)
    {
        if (MyLab == null || MyLab.Destroyed) { GameHUD.Toast("Your lab is destroyed.", new Color(1f, 0.5f, 0.4f)); return false; }
        if (!InsideOwnLab) { GameHUD.Toast("Get inside your lab.", new Color(1f, 0.5f, 0.4f)); return false; }
        int total = 0;
        for (int m = 0; m < Items.MatCount; m++)
        {
            if (mix[m] > Have(m)) { GameHUD.Toast("You don't have that much " + Items.MatNames[m] + ".", new Color(1f, 0.5f, 0.4f)); return false; }
            total += mix[m];
        }
        if (total == 0) { GameHUD.Toast("Put some materials in first.", Color.white); return false; }

        Recipe r = Items.Match(st, mix);
        if (r == null)
        {
            Sfx.Play("error", 0.5f);
            Sfx.Play("overheat", 0.35f, 1.3f);
            Fx.Puff(Center + transform.forward * 0.8f, new Color(0.6f, 0.6f, 0.6f, 0.5f), 6, 0.3f, 0.3f, 1.2f, Vector3.up * 0.8f);
            // one material away from a real recipe? say so!
            string close = RecipeHints.Close(st, mix);
            if (close != null)
            {
                Sfx.Play("select", 0.6f, close.StartsWith("SO CLOSE") ? 1.4f : 1.1f);
                GameHUD.Toast(close, new Color(1f, 0.82f, 0.35f));
                return false;
            }
            int unknown = 0;
            foreach (Recipe x in Items.For(st)) if (!Discoveries.Knows(x.id)) unknown++;
            GameHUD.Toast("*fizzle*  Nothing happened. " + (unknown > 0 ? unknown + " secret recipe" + (unknown == 1 ? "" : "s") + " left to find here." : "You've found everything here!"), new Color(0.8f, 0.8f, 0.8f));
            return false;
        }
        string problem = CraftProblem(r);
        if (problem != null)
        {
            Sfx.Play("error", 0.5f);
            GameHUD.Toast("Something's happening... but: " + problem, new Color(1f, 0.7f, 0.4f));
            return false;
        }
        bool isNew = Discoveries.Learn(r.id);
        Craft(r);
        if (isNew)
        {
            GameHUD.Banner("DISCOVERED!", r.name + "  -  " + r.desc, r.color);
            Sfx.Play("upgrade", 0.8f);
        }
        return true;
    }

    // Put something in the hotbar (potions of the same kind stack).
    public bool GiveItem(ItemId id, int count)
    {
        Recipe r = Items.Get(id);
        if (r.kind != ItemKind.Weapon)
            foreach (HeldItem h in slots)
                if (h != null && h.id == id) { h.count += count; return true; }
        for (int i = 0; i < SlotCount; i++)
            if (slots[i] == null)
            {
                slots[i] = new HeldItem { id = id, count = count };
                if (r.kind == ItemKind.Weapon) selected = i;
                return true;
            }
        return false;
    }
}
