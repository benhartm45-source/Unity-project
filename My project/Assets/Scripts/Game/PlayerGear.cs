using System.Collections.Generic;
using UnityEngine;

// YOUR ARMOUR AND POTION EFFECTS.
// The suit you're wearing (Gear.Armors) and the potions you've drunk (Gear.Potions) change your stats.
// Everything else asks this file: "how fast can I run right now?", "how much damage do I take?" ...
public partial class Player
{
    public ArmorDef suit;               // the armour you're wearing (null = none)
    public float armorMax = 100f;       // for the armour bar

    class Buff { public PotionDef def; public string name; public Color color; public float left; }
    readonly List<Buff> buffs = new List<Buff>();
    float healRate, healRateLeft;

    // ---------------- stats ----------------

    public float SpeedMul
    {
        get
        {
            float m = suit != null ? suit.speed : 1f;
            foreach (Buff b in buffs) m *= b.def.speed;
            return m;
        }
    }

    public float JumpMul
    {
        get
        {
            float m = suit != null ? suit.jump : 1f;
            foreach (Buff b in buffs) m *= b.def.jump;
            return m;
        }
    }

    public float GravityMul
    {
        get { float m = 1f; foreach (Buff b in buffs) m *= b.def.gravity; return m; }
    }

    public float DamageTakenMul
    {
        get
        {
            float m = suit != null ? 1f - suit.resist : 1f;
            foreach (Buff b in buffs) m *= b.def.taken;
            return Mathf.Max(0.1f, m);
        }
    }

    public float DamageDealtMul
    {
        get { float m = 1f; foreach (Buff b in buffs) m *= b.def.dealt; return m; }
    }

    public float FireRateMul
    {
        get { float m = 1f; foreach (Buff b in buffs) m *= b.def.rate; return Mathf.Max(0.2f, m); }
    }

    public float HeatMul
    {
        get { float m = 1f; foreach (Buff b in buffs) m *= b.def.heat; return m; }
    }

    public float CoolMul => suit != null ? suit.cool : 1f;
    public float KnockMul => suit != null ? suit.knock : 1f;
    public float ZoneMul => suit != null ? 1f - suit.zone : 1f;

    public float RegenPerSec
    {
        get
        {
            float r = suit != null ? suit.regen : 0f;
            foreach (Buff b in buffs) r += b.def.regen;
            return r;
        }
    }

    public bool Immune(Element e)
    {
        if (e == Element.None) return false;
        if (suit != null && suit.immune == e) return true;
        foreach (Buff b in buffs) if (b.def.immune == e) return true;
        return false;
    }

    public bool NoFall
    {
        get
        {
            if (suit != null && suit.noFall) return true;
            foreach (Buff b in buffs) if (b.def.noFall) return true;
            return false;
        }
    }

    public bool DoubleJump => bounceBoots || (suit != null && suit.doubleJump);

    // Drones and turrets can't see you (Ghost Potion).
    public bool Hidden
    {
        get { foreach (Buff b in buffs) if (b.def.hidden) return true; return false; }
    }

    // ---------------- armour ----------------

    public void Equip(ArmorDef a)
    {
        suit = a;
        armor = a.armor;
        armorMax = a.armor;
        armorTier = a.glow ? 2 : 1;          // (what friends see online)
        Model.SetArmor(armorTier, bounceBoots, a);
    }

    // The armour ran out: it falls off, and so do its perks.
    void ArmorBroke()
    {
        suit = null;
        armorTier = 0;
        Model.SetArmor(0, bounceBoots);
    }

    // ---------------- potions ----------------

    public void DrinkPotion(PotionDef d, Recipe r)
    {
        if (d.heal > 0f) health = Mathf.Min(MaxHealth, health + d.heal);
        if (d.healOverTime > 0f) { healRate = d.healOverTime / Mathf.Max(0.1f, d.time); healRateLeft = d.time; }
        if (d.shield > 0f)
        {
            armor += d.shield;
            armorMax = Mathf.Max(armorMax, armor);
            if (armorTier == 0) { armorTier = 1; Model.SetArmor(1, bounceBoots, suit); }
        }
        if (d.cleanse) status.Clear();
        if (!d.Timed) return;
        foreach (Buff b in buffs)
            if (b.def == d) { b.left = d.time; return; }        // drinking it again tops it up
        buffs.Add(new Buff { def = d, name = r.name.ToUpperInvariant(), color = r.color, left = d.time });
    }

    void ClearBuffs()
    {
        buffs.Clear();
        healRate = healRateLeft = 0f;
    }

    // Life steal and the like.
    public void HealFrom(float amount)
    {
        if (dead || amount <= 0f) return;
        health = Mathf.Min(MaxHealth, health + amount);
    }

    // Every frame (from Player.Effects).
    void GearUpdate(float dt)
    {
        for (int i = buffs.Count - 1; i >= 0; i--)
        {
            buffs[i].left -= dt;
            if (buffs[i].left <= 0f)
            {
                GameHUD.Toast(buffs[i].name + " wore off.", new Color(1f, 1f, 1f, 0.8f));
                buffs.RemoveAt(i);
            }
        }
        if (healRateLeft > 0f)
        {
            float k = Mathf.Min(dt, healRateLeft);
            healRateLeft -= k;
            health = Mathf.Min(MaxHealth, health + healRate * k);
        }
        float regen = RegenPerSec;
        if (regen > 0f && health < MaxHealth) health = Mathf.Min(MaxHealth, health + regen * dt);
        // immunities wash the matching effect off straight away
        if (Immune(Element.Fire)) status.burn = 0f;
        if (Immune(Element.Poison)) { status.poison = 0f; status.poisonStacks = 0; }
        if (Immune(Element.Ice)) { status.freeze = 0f; status.chill = 0f; }
        if ((Immune(Element.Ice) || Immune(Element.Slime) || Immune(Element.Shock)) && status.freeze <= 0f) status.slow = 0f;
        if (Hidden && Random.value < 0.15f) Fx.Puff(Center + Random.insideUnitSphere * 0.4f, new Color(0.9f, 0.8f, 1f, 0.25f), 1, 0.1f, 0.25f, 0.6f, Vector3.up * 0.3f);
    }

    // For the HUD: (name, seconds left, colour).
    public void BuffChips(List<(string, float, Color)> into)
    {
        foreach (Buff b in buffs) into.Add((b.name, b.left, b.color));
        if (healRateLeft > 0f) into.Add(("HEALING", healRateLeft, new Color(1f, 0.35f, 0.4f)));
    }
}
