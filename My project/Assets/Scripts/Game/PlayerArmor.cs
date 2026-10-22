using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// ARMOUR ABILITIES (the big armour list).
// Every suit can have:
//   - a POWER on F (controller: right stick click) - blink, dash, walls, decoys, turrets, heals... with a cooldown
//   - PASSIVE PERKS that are always on - thorns, life steal, gliding, trails, scans, a second life...
// The numbers for each suit are in Gear.cs (ArmorDef). Lose the armour (it breaks) and you lose its perks too.
public partial class Player
{
    float powerCdLeft;
    int powerCharges;
    ArmorDef powerFor;                      // which suit the charges belong to
    bool riftSet;
    Vector3 riftPoint;
    readonly Queue<(float, Vector3)> trailHistory = new Queue<(float, Vector3)>();
    float stillTime, scanT, cleanseT, gambleT, spectrumT, trailT, sprintCharge, hasteLeft, toughLeft, curseLeft, hideLeft;
    int curseStacks;
    bool phoenixUsed;
    Element spectrumEl = Element.Fire;
    static readonly Element[] SpectrumOrder = { Element.Fire, Element.Ice, Element.Poison, Element.Slime, Element.Shock };
    static PotionDef gambleSpeed, gambleDamage, gambleRate, gambleSlow;

    static string PowerName(ArmorPower p)
    {
        switch (p)
        {
            case ArmorPower.SuperJump: return "SUPER JUMP";
            case ArmorPower.HealZone: return "HEAL ZONE";
            case ArmorPower.ShockPulse: return "SHOCK PULSE";
            default: return p.ToString().ToUpperInvariant();
        }
    }

    // ---------------- every frame (from Player.Update) ----------------
    void ArmorUpdate(float dt, bool control)
    {
        ArmorDef a = suit;
        hasteLeft -= dt; toughLeft -= dt; hideLeft -= dt;
        curseLeft -= dt; if (curseLeft <= 0f) curseStacks = 0;
        // standing still?
        Vector3 flat = new Vector3(cc.velocity.x, 0f, cc.velocity.z);
        stillTime = flat.magnitude < 0.4f && grounded ? stillTime + dt : 0f;
        // where we were (for Rewind)
        trailHistory.Enqueue((Time.time, transform.position));
        while (trailHistory.Count > 0 && Time.time - trailHistory.Peek().Item1 > 3.2f) trailHistory.Dequeue();

        if (a == null) return;
        if (powerFor != a) { powerFor = a; powerCharges = Mathf.Max(1, a.charges); powerCdLeft = 0f; riftSet = false; }

        // ---- the power on F ----
        if (a.power != ArmorPower.None)
        {
            if (powerCharges < Mathf.Max(1, a.charges))
            {
                powerCdLeft -= dt;
                if (powerCdLeft <= 0f) { powerCharges++; powerCdLeft = powerCharges < a.charges ? a.powerCd : 0f; if (powerCharges == 1) Sfx.Play("tick", 0.35f, 1.6f); }
            }
            bool pressed = control && ((K != null && K.fKey.wasPressedThisFrame) || (G != null && G.rightStickButton.wasPressedThisFrame));
            if (pressed)
            {
                if (powerCharges > 0)
                {
                    bool used = UsePower(a);
                    if (used)
                    {
                        if (powerCharges == Mathf.Max(1, a.charges)) powerCdLeft = a.powerCd;
                        powerCharges--;
                    }
                }
                else Sfx.Play("overheat_click", 0.4f);
            }
        }

        // ---- passives ----
        if (a.glide && !grounded && velocity.y < -3f && ((K != null && K.spaceKey.isPressed) || (G != null && G.buttonSouth.isPressed)))
        {
            velocity.y = -3f;
            if (Random.value < 0.3f) Fx.Puff(transform.position + Vector3.up * 1.8f, new Color(1f, 1f, 1f, 0.3f), 1, 0.4f, 0.3f, 0.5f, Vector3.up);
        }
        if (a.oocRegen > 0f && Time.time - lastHurt > 4f && health < MaxHealth) GiveHealth(a.oocRegen * dt);
        if (a.scan > 0f)
        {
            scanT -= dt;
            if (scanT <= 0f) { scanT = a.scan; ScanPulse(25f, 0.15f, a.color); }
        }
        if (a.cleanse > 0f)
        {
            cleanseT -= dt;
            if (cleanseT <= 0f && (status.burn > 0f || status.poison > 0f || status.freeze > 0f || status.slow > 0f))
            {
                cleanseT = a.cleanse;
                status.Clear();
                Fx.Burst(Center, new Color(0.7f, 1f, 1f), 12, 3f, 0.08f, 0.4f, 0f);
                Sfx.Play("gulp", 0.4f, 1.5f);
            }
        }
        if (a.gamble)
        {
            gambleT -= dt;
            if (gambleT <= 0f) { gambleT = 12f; Gamble(); }
        }
        if (a.spectrum)
        {
            spectrumT -= dt;
            if (spectrumT <= 0f)
            {
                spectrumT = 15f;
                spectrumEl = SpectrumOrder[(System.Array.IndexOf(SpectrumOrder, spectrumEl) + 1) % SpectrumOrder.Length];
                GameHUD.Toast("Your armour now blocks " + Items.ElementName(spectrumEl) + ".", Items.ElementColor(spectrumEl));
            }
        }
        if (a.trail != Element.None && flat.magnitude > 2f && grounded)
        {
            trailT -= dt;
            if (trailT <= 0f)
            {
                trailT = 0.45f;
                if (a.trail == Element.Slime) SlimePuddle.Spawn(transform.position - flat.normalized * 1.2f, Team);
                else DamageField.Spawn(transform.position - flat.normalized * 1.2f, 1.1f, 2.5f, 4f, a.trail, Items.ElementColor(a.trail), Team, true, 0.5f);
            }
        }
        if (a.sprintShock)
        {
            if (Sprinting) sprintCharge = Mathf.Min(1f, sprintCharge + dt / 2.5f);
            else if (sprintCharge >= 1f)
            {
                sprintCharge = 0f;
                Combat.Blast(Center, 4.5f, 22f * a.powerAmt, 6f, Team, Element.Shock, 0.5f, true, new Color(0.5f, 0.75f, 1f));
                Fx.Ring(transform.position + Vector3.up * 0.2f, new Color(0.5f, 0.75f, 1f), 24, 12f, 0.2f);
            }
            else sprintCharge = Mathf.Max(0f, sprintCharge - dt);
            if (sprintCharge >= 1f && Random.value < 0.4f) Fx.Burst(Center + Random.insideUnitSphere * 0.5f, new Color(0.5f, 0.75f, 1f), 1, 2f, 0.06f, 0.2f, 0f);
        }
        if (a.magnet)
        {
            foreach (MaterialPickup m in MaterialPickup.All)
            {
                if (m == null) continue;
                Vector3 d = Center - m.transform.position;
                if (d.sqrMagnitude < 12f * 12f && d.sqrMagnitude > 0.5f) m.transform.position += d.normalized * Mathf.Min(d.magnitude, 9f * dt);
            }
        }
        if (a.capacitor && health >= MaxHealth && healLeft <= 0f && Time.time - lastHurt > 4f && armor < armorMax)
            armor = Mathf.Min(armorMax, armor + 2f * dt);
        if (ArmorHidden && Random.value < 0.08f) Fx.Puff(Center + Random.insideUnitSphere * 0.4f, new Color(0.8f, 0.9f, 1f, 0.2f), 1, 0.1f, 0.25f, 0.6f, Vector3.up * 0.3f);
    }

    void GiveHealth(float n) => health = Mathf.Min(MaxHealth, health + n * (curseStacks > 0 ? 0.5f : 1f));

    // ---------------- the powers ----------------
    bool UsePower(ArmorDef a)
    {
        float k = a.powerAmt;
        Vector3 fwd = Quaternion.Euler(0f, GameCam.I.yaw, 0f) * Vector3.forward;
        float yaw = GameCam.I.yaw;
        Color c = Color.Lerp(a.color, Color.white, 0.3f);
        switch (a.power)
        {
            case ArmorPower.Blink:
                Blink(7f * Mathf.Sqrt(k), c);
                break;
            case ArmorPower.Dash:
            {
                Vector2 mi = MoveInput();
                Vector3 dir = mi.sqrMagnitude > 0.05f ? Quaternion.Euler(0f, yaw, 0f) * new Vector3(mi.x, 0f, mi.y).normalized : fwd;
                push += dir * 16f * Mathf.Sqrt(k);
                velocity.y = Mathf.Max(velocity.y, 3f);
                hideLeft = 0.4f;
                Fx.Puff(transform.position + Vector3.up * 0.5f, new Color(1f, 1f, 1f, 0.4f), 6, 0.4f, 0.4f, 0.4f, -dir * 2f);
                Sfx.Play("phase", 0.5f, 1.4f);
                break;
            }
            case ArmorPower.Rift:
                if (!riftSet)
                {
                    riftSet = true; riftPoint = transform.position;
                    HealZoneMarker(riftPoint, c);
                    GameHUD.Toast("Return point set - press F again to warp back.", c);
                    Sfx.Play("zone", 0.4f, 1.8f);
                    powerCharges++;              // setting the point is free
                    return true;
                }
                riftSet = false;
                TeleportTo(riftPoint, c);
                break;
            case ArmorPower.Rewind:
                if (trailHistory.Count == 0) return false;
                TeleportTo(trailHistory.Peek().Item2, c);
                GiveHealth(10f * k);
                break;
            case ArmorPower.Grapple:
            {
                Vector3 from = GameCam.I.transform.position, dir = GameCam.I.transform.forward;
                if (!Physics.Raycast(from, dir, out RaycastHit hit, 35f, Combat.NotPlayer, QueryTriggerInteraction.Ignore))
                {
                    Sfx.Play("error", 0.3f);
                    return false;
                }
                Vector3 to = hit.point - Center;
                push += to.normalized * Mathf.Min(24f, 8f + to.magnitude) * Mathf.Sqrt(k);
                velocity.y = Mathf.Max(velocity.y, 5f + Mathf.Max(0f, to.y) * 0.6f);
                Fx.Beam(Center, hit.point, c, 0.05f, 0.25f, false);
                Sfx.Play("throw", 0.5f, 0.8f);
                break;
            }
            case ArmorPower.SuperJump:
                velocity.y = 16f * Mathf.Sqrt(k);
                lastGrounded = -10f;
                Fx.Ring(transform.position + Vector3.up * 0.1f, c, 16, 6f, 0.2f);
                Sfx.Play("boing", 0.7f, 0.8f);
                break;
            case ArmorPower.Lift:
                velocity.y = Mathf.Max(velocity.y, 9f);
                hideLeft = 0.2f;
                Fx.Puff(transform.position, new Color(c.r, c.g, c.b, 0.5f), 8, 0.6f, 0.4f, 0.6f, Vector3.down * 2f);
                Sfx.Play("jump", 0.6f, 1.4f);
                break;
            case ArmorPower.Barrier:
                Barrier.Spawn(transform.position + fwd * 2.6f, yaw, 6f * k, c);
                break;
            case ArmorPower.Shield:
                armor += 30f * k;
                armorMax = Mathf.Max(armorMax, armor);
                Fx.Burst(Center, c, 20, 3f, 0.1f, 0.5f, 0f);
                Fx.Ring(Center, c, 16, 4f, 0.15f);
                Sfx.Play("shield_hit", 0.7f, 0.8f);
                break;
            case ArmorPower.Decoy:
                Decoy.Spawn(transform.position + fwd * 1.5f, yaw, Team, 18f * k, Element.None, c, 4f);
                hideLeft = 2f;
                break;
            case ArmorPower.Turret:
                MiniTurret.Spawn(transform.position + fwd * 2.2f, yaw, Team, 5f * k, Element.None, c, 12f);
                Sfx.Play("throw", 0.6f);
                break;
            case ArmorPower.Trap:
            {
                SlimePuddle.Spawn(transform.position + fwd * 3f, Team);
                DamageField.Spawn(transform.position + fwd * 3f, 1.6f, 6f, 3f * k, Element.Slime, new Color(0.35f, 1f, 0.45f), Team, true, 0.5f);
                Sfx.Play("slime_fire", 0.5f, 0.8f);
                break;
            }
            case ArmorPower.Smoke:
                for (int i = 0; i < 4; i++) Fx.Puff(Center, new Color(0.75f, 0.75f, 0.8f, 0.55f), 8, 2.2f, 1.2f, 3f, Random.insideUnitSphere * 0.6f);
                hideLeft = 4f * k;
                Sfx.Play("toxic_hiss", 0.5f, 0.7f);
                break;
            case ArmorPower.Repulse:
                foreach (IDamageable d in GunToys.Near(Center, 6f * Mathf.Sqrt(k), Team))
                {
                    if (d is Laboratory) continue;
                    Vector3 dir = d.Center - Center; dir.y = 0f;
                    d.TakeHit(new Hit { damage = 6f * k, point = d.Center, dir = dir.sqrMagnitude > 0.01f ? dir.normalized : fwd, force = 14f, team = Team, structureMul = 0.2f, fromPlayer = true });
                }
                Fx.Ring(transform.position + Vector3.up * 0.3f, c, 28, 14f, 0.25f);
                Sfx.Play("phase", 0.7f, 0.7f);
                break;
            case ArmorPower.Jam:
                foreach (IDamageable d in GunToys.Near(Center, 14f, Team))
                {
                    if (d is Laboratory) continue;
                    Marks.Add(d, 0.25f * k, 5f);
                    d.TakeHit(new Hit { damage = 3f, point = d.Center, dir = Vector3.up, element = Element.Shock, team = Team, structureMul = 0.2f, fromPlayer = true });
                }
                Fx.Ring(Center, new Color(0.5f, 0.75f, 1f), 30, 18f, 0.15f);
                Sfx.Play("zap", 0.6f, 0.6f);
                break;
            case ArmorPower.ShockPulse:
                Combat.Blast(Center, 5f, 25f * k, 7f, Team, Element.Shock, 0.6f, true, new Color(0.5f, 0.75f, 1f));
                Fx.Ring(transform.position + Vector3.up * 0.2f, new Color(0.5f, 0.75f, 1f), 24, 12f, 0.2f);
                break;
            case ArmorPower.Repair:
                armor = Mathf.Min(Mathf.Max(armorMax, a.armor), armor + 35f * k);
                Fx.Burst(Center, new Color(0.6f, 0.85f, 1f), 16, 2.5f, 0.08f, 0.6f, 1f);
                Sfx.Play("tinker", 0.7f, 1.1f);
                break;
            case ArmorPower.Heal:
                GiveHealth(35f * k);
                Fx.Burst(Center, new Color(0.4f, 1f, 0.5f), 20, 3f, 0.09f, 0.6f, 2f);
                Sfx.Play("gulp", 0.6f, 1.2f);
                break;
            case ArmorPower.HealZone:
                HealZone.Spawn(transform.position, 4f, 6f * k, Team);
                break;
            case ArmorPower.Cleanse:
                status.Clear();
                hasteLeft = 2.5f;
                Fx.Burst(Center, new Color(0.7f, 1f, 1f), 20, 4f, 0.08f, 0.5f, 0f);
                Sfx.Play("gulp", 0.5f, 1.6f);
                break;
            default: return false;
        }
        return true;
    }

    void TeleportTo(Vector3 pos, Color c)
    {
        Fx.Burst(Center, c, 16, 4f, 0.1f, 0.4f, 0f);
        cc.enabled = false;
        transform.position = pos;
        cc.enabled = true;
        velocity = push = Vector3.zero;
        Fx.Burst(Center, c, 16, 4f, 0.1f, 0.4f, 0f);
        Sfx.Play("phase", 0.6f, 1.2f);
    }

    static void HealZoneMarker(Vector3 pos, Color c) => Fx.Ring(pos + Vector3.up * 0.1f, c, 16, 3f, 0.2f);

    void ScanPulse(float radius, float bonus, Color c)
    {
        int n = 0;
        foreach (IDamageable d in GunToys.Near(Center, radius, Team))
        {
            if (d is Laboratory) continue;
            Marks.Add(d, bonus, 4f);
            n++;
        }
        Fx.Ring(Center, new Color(c.r, c.g, c.b, 0.35f), 24, radius * 0.7f, 0.12f);
        if (n > 0) Sfx.Play("tick", 0.4f, 1.3f);
    }

    void Gamble()
    {
        if (gambleSpeed == null)
        {
            gambleSpeed = new PotionDef { time = 5f, speed = 1.3f };
            gambleDamage = new PotionDef { time = 5f, dealt = 1.25f };
            gambleRate = new PotionDef { time = 5f, rate = 0.8f };
            gambleSlow = new PotionDef { time = 3f, speed = 0.85f };
        }
        int roll = Random.Range(0, 5);
        Recipe r = Items.Get(suit.id);
        switch (roll)
        {
            case 0: DrinkPotion(gambleSpeed, r); GameHUD.Toast("Lucky roll: SPEED!", new Color(1f, 0.85f, 0.2f)); break;
            case 1: DrinkPotion(gambleDamage, r); GameHUD.Toast("Lucky roll: POWER!", new Color(1f, 0.4f, 0.3f)); break;
            case 2: DrinkPotion(gambleRate, r); GameHUD.Toast("Lucky roll: QUICK HANDS!", new Color(0.5f, 0.9f, 1f)); break;
            case 3: GiveHealth(15f); GameHUD.Toast("Lucky roll: +15 HEALTH!", new Color(0.4f, 1f, 0.5f)); break;
            default: DrinkPotion(gambleSlow, r); GameHUD.Toast("Unlucky roll: heavy feet...", new Color(0.7f, 0.7f, 0.7f)); break;
        }
        Sfx.Play("tick", 0.5f, 1.8f);
    }

    // ---------------- getting hurt (from Player.TakeHit) ----------------

    float ArmorTakenMul(Hit h)
    {
        ArmorDef a = suit;
        if (a == null) return 1f;
        float m = 1f;
        if (a.stillResist > 0f && stillTime > 0.4f) m *= 1f - a.stillResist;
        if (a.frontBlock > 0f && !h.dot && Model != null && Vector3.Dot(h.dir, Model.transform.forward) < -0.2f) m *= 1f - a.frontBlock;
        if (a.berserk > 0f) m *= 1f + 0.15f * (1f - health / MaxHealth);
        if (a.toughen && toughLeft > 0f) m *= 0.6f;
        return m;
    }

    bool ArmorLucky(Element e)
    {
        ArmorDef a = suit;
        if (a == null) return false;
        if (a.spectrum && e == spectrumEl) return true;
        return a.luck > 0f && Random.value < a.luck;
    }

    void ArmorHurt(Hit h, float dmg)
    {
        ArmorDef a = suit;
        if (a == null || h.dot) return;
        if (a.curse) { curseStacks = Mathf.Min(8, curseStacks + 1); curseLeft = 8f; }
        if (a.toughen && h.damage >= 20f) { toughLeft = 4f; Fx.Burst(Center, new Color(0.5f, 0.3f, 0.7f), 10, 2f, 0.1f, 0.4f, 0f); }
        if (a.hurtHaste) hasteLeft = 2f;
        IDamageable attacker = null;
        if ((a.thorns > 0f || a.sentinel) && h.team != Team && h.team != Teams.Neutral)
        {
            float best = 9f;
            foreach (IDamageable d in GunToys.Near(Center, 9f, Team))
            {
                if (d.Team != h.team || d is Laboratory) continue;
                float dist = Vector3.Distance(d.Center, Center);
                if (dist < best) { best = dist; attacker = d; }
            }
        }
        if (attacker != null)
        {
            if (a.sentinel) Marks.Add(attacker, 0.3f, 6f);
            if (a.thorns > 0f && Vector3.Distance(attacker.Center, Center) < 5f)
            {
                attacker.TakeHit(new Hit { damage = h.damage * a.thorns, point = attacker.Center, dir = (attacker.Center - Center).normalized, force = 2f, team = Team, structureMul = 0f, fromPlayer = true });
                Fx.Beam(Center, attacker.Center, new Color(0.6f, 1f, 0.4f), 0.04f, 0.15f, true);
            }
        }
    }

    // A second life (Phoenix). Returns true if it saved you.
    bool ArmorSaves()
    {
        if (suit == null || !suit.phoenix || phoenixUsed) return false;
        phoenixUsed = true;
        health = 1f;
        hideLeft = 2f;
        status.Clear();
        push += Vector3.up * 4f;
        Fx.Burst(Center, new Color(1f, 0.6f, 0.15f), 40, 6f, 0.12f, 0.8f, 1f);
        Combat.Blast(Center, 4f, 15f, 9f, Team, Element.Fire, 0.3f, true, new Color(1f, 0.6f, 0.15f));
        GameHUD.Toast("PHOENIX! Your armour saved you - get out of there!", new Color(1f, 0.6f, 0.15f));
        Sfx.Play("respawn", 0.8f, 1.3f);
        return true;
    }

    void ArmorLanded(float vy)
    {
        if (suit == null || suit.landBlast <= 0f || vy > -8f || dead) return;
        float k = Mathf.Clamp01((-vy - 8f) / 12f);
        Combat.Blast(transform.position + Vector3.up * 0.3f, 3.5f + 2f * k, suit.landBlast * (0.5f + k), 8f, Team, Element.None, 0.5f, true, new Color(0.75f, 0.65f, 0.5f));
        Fx.Ring(transform.position + Vector3.up * 0.1f, new Color(0.75f, 0.65f, 0.5f), 24, 10f, 0.3f);
        GameCam.Shake(transform.position, 0.3f);
    }

    void ArmorReset()
    {
        phoenixUsed = false;
        curseStacks = 0;
        riftSet = false;
        sprintCharge = 0f;
        trailHistory.Clear();
    }

    // ---------------- stats the rest of the game asks about ----------------

    float ArmorDealtMul
    {
        get
        {
            ArmorDef a = suit;
            if (a == null) return 1f;
            float m = a.dealt;
            if (a.berserk > 0f) m *= 1f + a.berserk * Mathf.Clamp01(1f - health / MaxHealth);
            if (a.curse) m *= 1f + 0.05f * curseStacks;
            return m;
        }
    }

    float ArmorSpeedMul => hasteLeft > 0f ? 1.35f : 1f;

    bool ArmorHidden
    {
        get
        {
            if (hideLeft > 0f) return true;
            ArmorDef a = suit;
            return a != null && (a.hidden || (a.hideStill && stillTime > 1f));
        }
    }

    // Extra material when gathering (Scavenger, Fortune...).
    int ArmorGatherBonus() => suit != null && suit.gather > 0f && Random.value < suit.gather ? 1 : 0;

    // For the HUD: the F power ("F  BLINK" when ready, seconds left when not).
    void ArmorChips(List<(string, float, Color)> into)
    {
        ArmorDef a = suit;
        if (a == null) return;
        Color c = Color.Lerp(a.color, Color.white, 0.4f);
        if (a.power != ArmorPower.None)
        {
            string key = Gamepad.current != null ? "R3" : "F";
            string n = key + "  " + PowerName(a.power) + (a.charges > 1 ? " x" + powerCharges : "") + (a.power == ArmorPower.Rift && riftSet ? " (back)" : "");
            if (powerCharges > 0) into.Add((n, -1f, c));
            else into.Add((n, Mathf.Max(0.1f, powerCdLeft), new Color(c.r, c.g, c.b, 0.55f)));
        }
        if (a.curse && curseStacks > 0) into.Add(("CURSED x" + curseStacks, curseLeft, new Color(0.7f, 0.3f, 0.9f)));
        if (a.toughen && toughLeft > 0f) into.Add(("HARDENED", toughLeft, new Color(0.6f, 0.4f, 0.8f)));
        if (a.sprintShock && sprintCharge >= 1f) into.Add(("CHARGED", -1f, new Color(0.5f, 0.75f, 1f)));
        if (a.spectrum) into.Add(("BLOCKS " + Items.ElementName(spectrumEl), spectrumT, Items.ElementColor(spectrumEl)));
        if (hideLeft > 0f || (a.hideStill && stillTime > 1f)) into.Add(("HIDDEN", hideLeft > 0f ? hideLeft : -1f, new Color(0.8f, 0.9f, 1f)));
    }
}
