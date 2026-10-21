using System.Collections.Generic;
using UnityEngine;

// ARMOUR, POTIONS AND ENCHANTS - the stat tables.
// Like Guns.cs: every item is one line. Change a number and the item changes.
// Recipes (what to mix) are in Items.cs. Icons are in Resources/Icons.

// ---------------- ARMOUR ----------------
//   armor      how much damage it soaks up before it breaks (it soaks 70% of each hit)
//   speed      running speed (1 = normal, 1.1 = 10% faster, 0.9 = 10% slower)
//   jump       jump height multiplier
//   regen      health healed every second
//   resist     less damage taken while worn (0.1 = 10% less; negative = more)
//   immune     this element (Fire / Ice / Poison / Slime / Shock) can't affect you
//   knock      how much you get pushed around (0.5 = half)
//   zone       how much less the zone hurts (0.6 = 60% less)
//   cool       how fast your guns cool down (1.3 = 30% faster)
//   noFall / doubleJump
// When the armour breaks, its perks go too.
public enum ArmorStyle { Plates, Heavy, Light }

public class ArmorDef
{
    public ItemId id;
    public ArmorStyle style;
    public Color color = Color.gray;
    public bool glow;
    public float armor = 50f, speed = 1f, jump = 1f, regen, resist, knock = 1f, zone, cool = 1f;
    public Element immune;
    public bool noFall, doubleJump;
}

// ---------------- POTIONS ----------------
// Drinks: instant ones (heal, shield, cleanse) and timed ones that last 'time' seconds:
//   heal / healOverTime / shield (extra armour) / cleanse (removes burning, poison, freezing)
//   speed, jump, gravity (0.5 = floaty), taken (damage you take), dealt (damage you deal),
//   rate (time between shots, 0.7 = 30% faster), heat (gun heat), regen, immune, noFall, hidden (drones and turrets ignore you)
// Bombs: 'bomb' is a little GunDef - the thrown flask explodes like a grenade.
public class PotionDef
{
    public ItemId id;
    public float time = 15f;
    public float heal, healOverTime, shield;
    public bool cleanse;
    public float speed = 1f, jump = 1f, gravity = 1f, taken = 1f, dealt = 1f, rate = 1f, heat = 1f, regen;
    public Element immune;
    public bool noFall, hidden;
    public GunDef bomb;
    public bool Timed => speed != 1f || jump != 1f || gravity != 1f || taken != 1f || dealt != 1f || rate != 1f || heat != 1f || regen > 0f || immune != Element.None || noFall || hidden;
}

// ---------------- ENCHANTS ----------------
// Go on the weapon you're holding (one enchant per weapon; a new one replaces the old).
//   element    Fire / Ice / Poison / Slime / Shock on every hit
//   damage     damage multiplier          rate      time between shots (0.75 = 25% faster)
//   heat       heat multiplier            range     range / bullet speed multiplier
//   lifesteal  heal this much of the damage you deal (0.15 = 15%)
//   crit       chance to do double damage
//   hitBlast   little explosion of this size on every hit
//   chain      lightning jumps to this many more targets      pierce   goes through targets
//   pellets    extra bullets per shot     spread    spread multiplier (0.3 = much tighter)
//   knock      extra knockback            structure extra damage multiplier against labs
public class EnchantDef
{
    public ItemId id;
    public Color color = Color.white;
    public Element element;
    public float damage = 1f, rate = 1f, heat = 1f, range = 1f, lifesteal, crit, hitBlast, spread = 1f, knock, structure = 1f;
    public int chain, pellets;
    public bool pierce;
}

public static class Gear
{
    static Dictionary<ItemId, ArmorDef> armorById;
    static Dictionary<ItemId, PotionDef> potionById;
    static Dictionary<ItemId, EnchantDef> enchantById;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { armorById = null; potionById = null; enchantById = null; }

    public static ArmorDef Armor(ItemId id)
    {
        if (armorById == null) { armorById = new Dictionary<ItemId, ArmorDef>(); foreach (ArmorDef a in Armors) armorById[a.id] = a; }
        return armorById.TryGetValue(id, out ArmorDef d) ? d : null;
    }

    public static PotionDef Potion(ItemId id)
    {
        if (potionById == null) { potionById = new Dictionary<ItemId, PotionDef>(); foreach (PotionDef p in Potions) potionById[p.id] = p; }
        return potionById.TryGetValue(id, out PotionDef d) ? d : null;
    }

    public static EnchantDef Enchant(ItemId id)
    {
        if (enchantById == null) { enchantById = new Dictionary<ItemId, EnchantDef>(); foreach (EnchantDef e in Enchants) enchantById[e.id] = e; }
        return enchantById.TryGetValue(id, out EnchantDef d) ? d : null;
    }

    public static readonly ArmorDef[] Armors =
    {
        // the originals
        new ArmorDef { id = ItemId.ScrapArmor, style = ArmorStyle.Plates, color = new Color(0.55f, 0.56f, 0.58f), armor = 50 },
        new ArmorDef { id = ItemId.CrystalArmor, style = ArmorStyle.Plates, color = new Color(0.35f, 0.85f, 1f), glow = true, armor = 100 },
        new ArmorDef { id = ItemId.LeafVest, style = ArmorStyle.Light, color = new Color(0.4f, 0.95f, 0.3f), glow = false, armor = 30, regen = 1.5f },
        new ArmorDef { id = ItemId.SlimeSuit, style = ArmorStyle.Light, color = new Color(0.3f, 0.95f, 0.75f), glow = true, armor = 40, immune = Element.Slime, jump = 1.2f, noFall = true },
        new ArmorDef { id = ItemId.CrystalPlate, style = ArmorStyle.Heavy, color = new Color(0.4f, 0.85f, 1f), glow = true, armor = 120, speed = 0.92f },
        new ArmorDef { id = ItemId.HeavyMetalPlate, style = ArmorStyle.Heavy, color = new Color(0.75f, 0.78f, 0.82f), glow = false, armor = 150, speed = 0.85f, knock = 0.4f },
        new ArmorDef { id = ItemId.HazmatSuit, style = ArmorStyle.Light, color = new Color(0.8f, 0.4f, 1f), glow = false, armor = 50, immune = Element.Poison, zone = 0.6f },
        new ArmorDef { id = ItemId.BrambleVest, style = ArmorStyle.Light, color = new Color(0.95f, 0.25f, 0.45f), glow = false, armor = 45, regen = 1f },
        new ArmorDef { id = ItemId.WoodenArmour, style = ArmorStyle.Plates, color = new Color(0.72f, 0.5f, 0.3f), glow = false, armor = 60 },
        new ArmorDef { id = ItemId.AmberArmour, style = ArmorStyle.Plates, color = new Color(1f, 0.7f, 0.2f), glow = true, armor = 80, resist = 0.1f },
        new ArmorDef { id = ItemId.MushroomCapArmour, style = ArmorStyle.Plates, color = new Color(1f, 0.45f, 0.4f), glow = false, armor = 55, immune = Element.Poison, regen = 0.5f },
        new ArmorDef { id = ItemId.MossCloak, style = ArmorStyle.Light, color = new Color(0.45f, 0.7f, 0.3f), glow = false, armor = 35, speed = 1.1f, regen = 1 },
        new ArmorDef { id = ItemId.PetalRobe, style = ArmorStyle.Light, color = new Color(1f, 0.62f, 0.85f), glow = true, armor = 30, speed = 1.15f, jump = 1.15f },
        new ArmorDef { id = ItemId.CoalArmour, style = ArmorStyle.Plates, color = new Color(0.3f, 0.3f, 0.33f), glow = false, armor = 90, immune = Element.Fire },
        new ArmorDef { id = ItemId.IronArmour, style = ArmorStyle.Plates, color = new Color(0.85f, 0.52f, 0.4f), glow = false, armor = 110, speed = 0.95f },
        new ArmorDef { id = ItemId.GoldArmour, style = ArmorStyle.Heavy, color = new Color(1f, 0.82f, 0.25f), glow = true, armor = 100, resist = 0.15f, speed = 0.92f },
        new ArmorDef { id = ItemId.FireproofSuit, style = ArmorStyle.Light, color = new Color(0.95f, 0.95f, 0.3f), glow = false, armor = 60, immune = Element.Fire, cool = 1.3f },
        new ArmorDef { id = ItemId.GlowstoneArmour, style = ArmorStyle.Plates, color = new Color(1f, 0.95f, 0.65f), glow = true, armor = 80, regen = 1.5f },
        new ArmorDef { id = ItemId.UraniumArmour, style = ArmorStyle.Heavy, color = new Color(0.55f, 1f, 0.2f), glow = true, armor = 130, immune = Element.Poison, regen = 1, speed = 0.9f },
        new ArmorDef { id = ItemId.ObsidianArmour, style = ArmorStyle.Heavy, color = new Color(0.45f, 0.3f, 0.65f), glow = true, armor = 160, speed = 0.82f, knock = 0.3f },
        new ArmorDef { id = ItemId.CircuitExosuit, style = ArmorStyle.Light, color = new Color(0.25f, 0.85f, 0.45f), glow = true, armor = 70, speed = 1.12f, jump = 1.2f },
        new ArmorDef { id = ItemId.CopperArmour, style = ArmorStyle.Plates, color = new Color(0.95f, 0.55f, 0.3f), glow = false, armor = 75, immune = Element.Shock },
        new ArmorDef { id = ItemId.PowerSuit, style = ArmorStyle.Heavy, color = new Color(0.4f, 0.6f, 1f), glow = true, armor = 70, cool = 1.6f, speed = 1.05f },
        new ArmorDef { id = ItemId.GlassArmour, style = ArmorStyle.Plates, color = new Color(0.75f, 0.95f, 1f), glow = true, armor = 140, resist = -0.2f },
        new ArmorDef { id = ItemId.PlasticRaincoat, style = ArmorStyle.Light, color = new Color(0.95f, 0.95f, 0.95f), glow = false, armor = 40, immune = Element.Slime, speed = 1.08f },
        new ArmorDef { id = ItemId.MeteoriteArmour, style = ArmorStyle.Heavy, color = new Color(1f, 0.4f, 0.2f), glow = true, armor = 150, immune = Element.Fire, speed = 0.9f },
        new ArmorDef { id = ItemId.StardustRobe, style = ArmorStyle.Light, color = new Color(0.85f, 0.7f, 1f), glow = true, armor = 40, jump = 1.4f, noFall = true, speed = 1.1f },
        new ArmorDef { id = ItemId.FrostArmour, style = ArmorStyle.Plates, color = new Color(0.4f, 0.85f, 1f), glow = true, armor = 90, immune = Element.Ice },
        new ArmorDef { id = ItemId.ScrapKnightArmour, style = ArmorStyle.Heavy, color = new Color(0.75f, 0.78f, 0.82f), glow = false, armor = 85, knock = 0.6f },
        new ArmorDef { id = ItemId.FeatherSuit, style = ArmorStyle.Light, color = new Color(1f, 0.62f, 0.85f), glow = false, armor = 30, jump = 1.3f, noFall = true, doubleJump = true },
        new ArmorDef { id = ItemId.ZoneSuit, style = ArmorStyle.Light, color = new Color(0.75f, 0.95f, 1f), glow = false, armor = 40, zone = 0.9f },
        new ArmorDef { id = ItemId.KingsArmour, style = ArmorStyle.Heavy, color = new Color(1f, 0.82f, 0.25f), glow = true, armor = 180, speed = 0.85f, resist = 0.1f },

        // ---- batch 2 ----
        new ArmorDef { id = ItemId.HerbRobe, style = ArmorStyle.Light, color = new Color(0.4f, 0.95f, 0.3f), glow = false, armor = 25, regen = 2 },
        new ArmorDef { id = ItemId.SlimeArmour, style = ArmorStyle.Plates, color = new Color(0.3f, 0.95f, 0.75f), glow = true, armor = 70, immune = Element.Slime, knock = 0.7f },
        new ArmorDef { id = ItemId.CrystalVest, style = ArmorStyle.Light, color = new Color(0.4f, 0.85f, 1f), glow = true, armor = 60, speed = 1.05f },
        new ArmorDef { id = ItemId.RiotArmour, style = ArmorStyle.Heavy, color = new Color(0.75f, 0.78f, 0.82f), glow = false, armor = 140, knock = 0.2f, speed = 0.9f },
        new ArmorDef { id = ItemId.AcidArmour, style = ArmorStyle.Plates, color = new Color(0.8f, 0.4f, 1f), glow = true, armor = 80, immune = Element.Poison },
        new ArmorDef { id = ItemId.BerryRobe, style = ArmorStyle.Light, color = new Color(0.95f, 0.25f, 0.45f), glow = false, armor = 20, regen = 2.5f, speed = 1.05f },
        new ArmorDef { id = ItemId.BarkArmour, style = ArmorStyle.Plates, color = new Color(0.72f, 0.5f, 0.3f), glow = false, armor = 75, regen = 0.5f },
        new ArmorDef { id = ItemId.SapShell, style = ArmorStyle.Heavy, color = new Color(1f, 0.7f, 0.2f), glow = true, armor = 100, immune = Element.Slime },
        new ArmorDef { id = ItemId.MushroomRobe, style = ArmorStyle.Light, color = new Color(1f, 0.45f, 0.4f), glow = false, armor = 40, regen = 1.5f, immune = Element.Poison },
        new ArmorDef { id = ItemId.MossArmour, style = ArmorStyle.Plates, color = new Color(0.45f, 0.7f, 0.3f), glow = false, armor = 70, regen = 1 },
        new ArmorDef { id = ItemId.PetalArmour, style = ArmorStyle.Plates, color = new Color(1f, 0.62f, 0.85f), glow = true, armor = 55, speed = 1.08f },
        new ArmorDef { id = ItemId.CoalSuit, style = ArmorStyle.Light, color = new Color(0.3f, 0.3f, 0.33f), glow = false, armor = 50, immune = Element.Fire, speed = 1.05f },
        new ArmorDef { id = ItemId.IronKnightArmour, style = ArmorStyle.Heavy, color = new Color(0.85f, 0.52f, 0.4f), glow = false, armor = 140, speed = 0.88f, resist = 0.05f },
        new ArmorDef { id = ItemId.GoldRobe, style = ArmorStyle.Light, color = new Color(1f, 0.82f, 0.25f), glow = true, armor = 45, resist = 0.1f, speed = 1.05f },
        new ArmorDef { id = ItemId.SulfurArmour, style = ArmorStyle.Plates, color = new Color(0.95f, 0.95f, 0.3f), glow = false, armor = 85, immune = Element.Fire, cool = 1.2f },
        new ArmorDef { id = ItemId.GlowstoneRobe, style = ArmorStyle.Light, color = new Color(1f, 0.95f, 0.65f), glow = true, armor = 50, regen = 2 },
        new ArmorDef { id = ItemId.UraniumSuit, style = ArmorStyle.Light, color = new Color(0.55f, 1f, 0.2f), glow = true, armor = 60, immune = Element.Poison, zone = 0.7f },
        new ArmorDef { id = ItemId.ObsidianVest, style = ArmorStyle.Light, color = new Color(0.45f, 0.3f, 0.65f), glow = true, armor = 80, knock = 0.5f },
        new ArmorDef { id = ItemId.CircuitArmour, style = ArmorStyle.Plates, color = new Color(0.25f, 0.85f, 0.45f), glow = true, armor = 90, cool = 1.4f },
        new ArmorDef { id = ItemId.CopperSuit, style = ArmorStyle.Light, color = new Color(0.95f, 0.55f, 0.3f), glow = false, armor = 50, immune = Element.Shock, speed = 1.1f },
        new ArmorDef { id = ItemId.JetSuit, style = ArmorStyle.Light, color = new Color(0.4f, 0.6f, 1f), glow = true, armor = 40, jump = 1.6f, doubleJump = true, noFall = true },
        new ArmorDef { id = ItemId.GlassVest, style = ArmorStyle.Light, color = new Color(0.75f, 0.95f, 1f), glow = true, armor = 90, resist = -0.15f, speed = 1.1f },
        new ArmorDef { id = ItemId.PlasticArmour, style = ArmorStyle.Plates, color = new Color(0.95f, 0.95f, 0.95f), glow = false, armor = 65, knock = 0.8f },
        new ArmorDef { id = ItemId.MeteorSuit, style = ArmorStyle.Light, color = new Color(1f, 0.4f, 0.2f), glow = true, armor = 70, immune = Element.Fire, speed = 1.05f },
        new ArmorDef { id = ItemId.StardustArmour, style = ArmorStyle.Plates, color = new Color(0.85f, 0.7f, 1f), glow = true, armor = 80, jump = 1.3f, noFall = true },
        new ArmorDef { id = ItemId.NinjaSuit, style = ArmorStyle.Light, color = new Color(0.45f, 0.3f, 0.65f), glow = false, armor = 35, speed = 1.2f, jump = 1.2f },
        new ArmorDef { id = ItemId.TankArmour, style = ArmorStyle.Heavy, color = new Color(0.75f, 0.78f, 0.82f), glow = false, armor = 220, speed = 0.75f, knock = 0.1f },
        new ArmorDef { id = ItemId.MedicSuit, style = ArmorStyle.Light, color = new Color(0.4f, 0.95f, 0.3f), glow = true, armor = 35, regen = 3 },
        new ArmorDef { id = ItemId.StormSuit, style = ArmorStyle.Plates, color = new Color(0.4f, 0.6f, 1f), glow = true, armor = 60, immune = Element.Shock, cool = 1.5f },
        new ArmorDef { id = ItemId.RoyalRobe, style = ArmorStyle.Light, color = new Color(1f, 0.82f, 0.25f), glow = true, armor = 40, resist = 0.2f, speed = 1.05f },
    };

    public static readonly PotionDef[] Potions =
    {
        // (the original Healing / Speed / Leap / Iron Skin potions and the Toxic Bomb have their own code)
        new PotionDef { id = ItemId.BerryJuice, heal = 35f },
        new PotionDef { id = ItemId.BigHealingPotion, healOverTime = 100f, time = 3f },
        new PotionDef { id = ItemId.RegenPotion, regen = 4f, time = 20f },
        new PotionDef { id = ItemId.MushroomBrew, dealt = 1.3f, time = 20f },
        new PotionDef { id = ItemId.SapSyrup, speed = 1.25f, jump = 1.2f, time = 20f },
        new PotionDef { id = ItemId.GhostPotion, hidden = true, time = 12f },
        new PotionDef { id = ItemId.ShieldPotion, shield = 60f },
        new PotionDef { id = ItemId.FireResistance, immune = Element.Fire, time = 30f, cleanse = true },
        new PotionDef { id = ItemId.IronGutPotion, taken = 0.6f, speed = 0.9f, time = 15f },
        new PotionDef { id = ItemId.GoldenElixir, heal = 60f, taken = 0.8f, time = 10f },
        new PotionDef { id = ItemId.UraniumRage, dealt = 1.6f, taken = 1.2f, time = 12f },
        new PotionDef { id = ItemId.ObsidianSkin, taken = 0.4f, speed = 0.75f, time = 12f },
        new PotionDef { id = ItemId.OverclockPotion, rate = 0.7f, time = 15f },
        new PotionDef { id = ItemId.Coolant, heat = 0.4f, time = 20f },
        new PotionDef { id = ItemId.BatteryBoost, speed = 1.6f, time = 8f },
        new PotionDef { id = ItemId.FeatherPotion, gravity = 0.45f, noFall = true, time = 20f },
        new PotionDef { id = ItemId.Antidote, cleanse = true, immune = Element.Poison, time = 20f },
        new PotionDef { id = ItemId.WarmingPotion, cleanse = true, immune = Element.Ice, time = 20f },
        new PotionDef { id = ItemId.StarJumpPotion, jump = 1.8f, noFall = true, gravity = 0.7f, time = 15f },
        new PotionDef { id = ItemId.MeteorRage, dealt = 1.4f, rate = 0.85f, time = 12f },
        new PotionDef { id = ItemId.GlassCannonPotion, dealt = 2f, taken = 1.5f, time = 10f },
        new PotionDef { id = ItemId.GroundingPotion, cleanse = true, immune = Element.Shock, taken = 0.85f, time = 20f },
        new PotionDef { id = ItemId.FireBomb, bomb = new GunDef { mode = GunMode.Projectile, damage = 28f, blast = 3.2f, size = 0.2f, speed = 18f, gravity = -15f, force = 7f, structure = 1.8f, element = Element.Fire, color = new Color(1f, 1f, 0.33f) } },
        new PotionDef { id = ItemId.IceBomb, bomb = new GunDef { mode = GunMode.Projectile, damage = 18f, blast = 3.4f, size = 0.2f, speed = 18f, gravity = -15f, force = 7f, structure = 1f, element = Element.Ice, color = new Color(0.44f, 0.935f, 1f) } },
        new PotionDef { id = ItemId.SlimeBomb, bomb = new GunDef { mode = GunMode.Projectile, damage = 14f, blast = 3f, size = 0.2f, speed = 18f, gravity = -15f, force = 7f, structure = 1f, element = Element.Slime, puddle = true, color = new Color(0.33f, 1f, 0.825f) } },
        new PotionDef { id = ItemId.ShockBomb, bomb = new GunDef { mode = GunMode.Projectile, damage = 24f, blast = 3.2f, size = 0.2f, speed = 18f, gravity = -15f, force = 7f, structure = 1.4f, element = Element.Shock, color = new Color(0.44f, 0.66f, 1f) } },
        new PotionDef { id = ItemId.StickySapBomb, bomb = new GunDef { mode = GunMode.Projectile, damage = 10f, blast = 2.6f, size = 0.2f, speed = 18f, gravity = -15f, force = 7f, structure = 1f, element = Element.Slime, puddle = true, color = new Color(1f, 0.77f, 0.22f) } },
        new PotionDef { id = ItemId.MeteorBomb, bomb = new GunDef { mode = GunMode.Projectile, damage = 50f, blast = 4.5f, size = 0.2f, speed = 18f, gravity = -15f, force = 7f, structure = 3f, element = Element.Fire, color = new Color(1f, 0.44f, 0.22f) } },
        new PotionDef { id = ItemId.UraniumBomb, bomb = new GunDef { mode = GunMode.Projectile, damage = 20f, blast = 3.4f, size = 0.2f, speed = 18f, gravity = -15f, force = 7f, structure = 2f, element = Element.Poison, color = new Color(0.605f, 1f, 0.22f) } },
        new PotionDef { id = ItemId.BerryGrenade, bomb = new GunDef { mode = GunMode.Projectile, damage = 22f, blast = 2.8f, size = 0.2f, speed = 18f, gravity = -15f, force = 7f, structure = 1.5f, color = new Color(1f, 0.275f, 0.495f) } },

        // ---- batch 2 ----
        new PotionDef { id = ItemId.BerrySmoothie, heal = 50f },
        new PotionDef { id = ItemId.HerbTea, healOverTime = 60f, time = 6f, cleanse = true },
        new PotionDef { id = ItemId.SlimeShake, jump = 1.4f, time = 20f },
        new PotionDef { id = ItemId.CrystalWater, shield = 40f },
        new PotionDef { id = ItemId.SuperShieldPotion, shield = 120f },
        new PotionDef { id = ItemId.SpeedDemon, speed = 1.8f, taken = 1.2f, time = 10f },
        new PotionDef { id = ItemId.TurtlePotion, taken = 0.3f, speed = 0.6f, time = 10f },
        new PotionDef { id = ItemId.MegaRegen, regen = 8f, time = 10f },
        new PotionDef { id = ItemId.BerserkerBrew, dealt = 1.5f, rate = 0.85f, taken = 1.3f, time = 12f },
        new PotionDef { id = ItemId.FocusPotion, dealt = 1.25f, time = 25f },
        new PotionDef { id = ItemId.ColdBrew, heat = 0.3f, rate = 0.9f, time = 20f },
        new PotionDef { id = ItemId.BouncyPotion, jump = 1.6f, noFall = true, time = 20f },
        new PotionDef { id = ItemId.MoonPotion, gravity = 0.35f, noFall = true, jump = 1.3f, time = 20f },
        new PotionDef { id = ItemId.IronWill, taken = 0.7f, time = 20f },
        new PotionDef { id = ItemId.GoldenShield, shield = 90f },
        new PotionDef { id = ItemId.HazmatPotion, immune = Element.Poison, cleanse = true, time = 30f },
        new PotionDef { id = ItemId.StormPotion, rate = 0.6f, heat = 1.4f, time = 12f },
        new PotionDef { id = ItemId.PhantomPotion, hidden = true, speed = 1.1f, time = 20f },
        new PotionDef { id = ItemId.LavaPotion, immune = Element.Fire, dealt = 1.2f, time = 20f },
        new PotionDef { id = ItemId.WoodElixir, healOverTime = 80f, time = 4f },
        new PotionDef { id = ItemId.RocketFuel, speed = 2f, time = 5f },
        new PotionDef { id = ItemId.UraniumShake, dealt = 1.3f, regen = 2f, time = 15f },
        new PotionDef { id = ItemId.FragGrenade, bomb = new GunDef { mode = GunMode.Projectile, damage = 35f, blast = 3.5f, size = 0.2f, speed = 18f, gravity = -15f, force = 7f, structure = 2f, color = new Color(0.825f, 0.858f, 0.902f) } },
        new PotionDef { id = ItemId.FreezeGrenade, bomb = new GunDef { mode = GunMode.Projectile, damage = 16f, blast = 4f, size = 0.2f, speed = 18f, gravity = -15f, force = 7f, structure = 1f, element = Element.Ice, color = new Color(0.44f, 0.935f, 1f) } },
        new PotionDef { id = ItemId.LightningGrenade, bomb = new GunDef { mode = GunMode.Projectile, damage = 26f, blast = 4f, size = 0.2f, speed = 18f, gravity = -15f, force = 7f, structure = 1.4f, element = Element.Shock, color = new Color(1f, 0.605f, 0.33f) } },
        new PotionDef { id = ItemId.AcidBomb, bomb = new GunDef { mode = GunMode.Projectile, damage = 18f, blast = 3f, size = 0.2f, speed = 18f, gravity = -15f, force = 7f, structure = 3f, element = Element.Poison, color = new Color(0.88f, 0.44f, 1f) } },
        new PotionDef { id = ItemId.SporeBomb, bomb = new GunDef { mode = GunMode.Projectile, damage = 14f, blast = 4.2f, size = 0.2f, speed = 18f, gravity = -15f, force = 7f, structure = 1f, element = Element.Poison, color = new Color(1f, 0.495f, 0.44f) } },
        new PotionDef { id = ItemId.SulfurFirebomb, bomb = new GunDef { mode = GunMode.Projectile, damage = 32f, blast = 3.6f, size = 0.2f, speed = 18f, gravity = -15f, force = 7f, structure = 1.8f, element = Element.Fire, color = new Color(1f, 1f, 0.33f) } },
        new PotionDef { id = ItemId.StarBomb, bomb = new GunDef { mode = GunMode.Projectile, damage = 30f, blast = 3.5f, size = 0.2f, speed = 18f, gravity = -15f, force = 7f, structure = 1.5f, color = new Color(0.935f, 0.77f, 1f) } },
        new PotionDef { id = ItemId.GlueBomb, bomb = new GunDef { mode = GunMode.Projectile, damage = 8f, blast = 3.4f, size = 0.2f, speed = 18f, gravity = -15f, force = 7f, structure = 1f, element = Element.Slime, puddle = true, color = new Color(1f, 0.77f, 0.22f) } },
    };

    public static readonly EnchantDef[] Enchants =
    {
        // the originals
        new EnchantDef { id = ItemId.FireEnchant, element = Element.Fire, color = new Color(1f, 0.5f, 0.1f) },
        new EnchantDef { id = ItemId.IceEnchant, element = Element.Ice, color = new Color(0.55f, 0.92f, 1f) },
        new EnchantDef { id = ItemId.PoisonEnchant, element = Element.Poison, color = new Color(0.55f, 1f, 0.2f) },
        new EnchantDef { id = ItemId.SharpnessEnchant, color = new Color(0.85f, 0.52f, 0.4f), damage = 1.25f },
        new EnchantDef { id = ItemId.RapidFireEnchant, color = new Color(0.4f, 0.6f, 1f), rate = 0.75f },
        new EnchantDef { id = ItemId.CoolingEnchant, color = new Color(0.4f, 0.85f, 1f), heat = 0.5f },
        new EnchantDef { id = ItemId.VampireEnchant, color = new Color(0.95f, 0.25f, 0.45f), lifesteal = 0.15f },
        new EnchantDef { id = ItemId.ExplosiveEnchant, color = new Color(0.3f, 0.3f, 0.33f), hitBlast = 1.8f, damage = 0.9f },
        new EnchantDef { id = ItemId.ChainLightningEnchant, color = new Color(0.5f, 0.75f, 1f), chain = 2, element = Element.Shock },
        new EnchantDef { id = ItemId.PiercingEnchant, color = new Color(0.45f, 0.3f, 0.65f), pierce = true },
        new EnchantDef { id = ItemId.MultishotEnchant, color = new Color(0.85f, 0.7f, 1f), pellets = 2, spread = 4, damage = 0.65f },
        new EnchantDef { id = ItemId.LongRangeEnchant, color = new Color(0.75f, 0.95f, 1f), range = 1.6f },
        new EnchantDef { id = ItemId.KnockbackEnchant, color = new Color(0.72f, 0.5f, 0.3f), knock = 5 },
        new EnchantDef { id = ItemId.CriticalEnchant, color = new Color(1f, 0.82f, 0.25f), crit = 0.25f },
        new EnchantDef { id = ItemId.SlimeEnchant, color = new Color(0.35f, 1f, 0.45f), element = Element.Slime },
        new EnchantDef { id = ItemId.ShockEnchant, color = new Color(0.5f, 0.75f, 1f), element = Element.Shock },
        new EnchantDef { id = ItemId.RadioactiveEnchant, color = new Color(0.55f, 1f, 0.2f), element = Element.Poison, damage = 1.15f },
        new EnchantDef { id = ItemId.InfernoEnchant, color = new Color(1f, 0.5f, 0.1f), element = Element.Fire, damage = 1.1f },
        new EnchantDef { id = ItemId.MeteorStrikeEnchant, color = new Color(1f, 0.5f, 0.1f), element = Element.Fire, hitBlast = 2.2f },
        new EnchantDef { id = ItemId.SiegeEnchant, color = new Color(0.75f, 0.78f, 0.82f), structure = 2f },
        new EnchantDef { id = ItemId.MossLeechEnchant, color = new Color(0.45f, 0.7f, 0.3f), lifesteal = 0.08f, damage = 1.05f },
        new EnchantDef { id = ItemId.GlowingEnchant, color = new Color(1f, 0.95f, 0.65f), damage = 1.15f, rate = 0.9f },
        new EnchantDef { id = ItemId.HerbalEnchant, color = new Color(0.4f, 0.95f, 0.3f), lifesteal = 0.1f },
        new EnchantDef { id = ItemId.FeatherweightEnchant, color = new Color(0.95f, 0.95f, 0.95f), heat = 0.6f, rate = 0.9f },
        new EnchantDef { id = ItemId.OverchargeEnchant, color = new Color(0.55f, 1f, 0.2f), damage = 1.4f, heat = 1.5f },
        new EnchantDef { id = ItemId.FrostbiteEnchant, color = new Color(0.55f, 0.92f, 1f), element = Element.Ice, damage = 1.1f },
        new EnchantDef { id = ItemId.ToxicSporesEnchant, color = new Color(0.55f, 1f, 0.2f), element = Element.Poison, hitBlast = 1.2f },
        new EnchantDef { id = ItemId.PrecisionEnchant, color = new Color(0.75f, 0.95f, 1f), crit = 0.4f, spread = 0.3f },
        new EnchantDef { id = ItemId.BouncyEnchant, color = new Color(0.35f, 1f, 0.45f), knock = 3, element = Element.Slime },
        new EnchantDef { id = ItemId.StardustSparkleEnchant, color = new Color(0.85f, 0.7f, 1f), damage = 1.2f },
        new EnchantDef { id = ItemId.ObsidianEdgeEnchant, color = new Color(0.45f, 0.3f, 0.65f), damage = 1.35f, rate = 1.15f },
        new EnchantDef { id = ItemId.ConductorEnchant, color = new Color(0.95f, 0.55f, 0.3f), chain = 1, rate = 0.85f },
        new EnchantDef { id = ItemId.AmberSapEnchant, color = new Color(0.35f, 1f, 0.45f), element = Element.Slime, heat = 0.8f },

        // ---- batch 2 ----
        new EnchantDef { id = ItemId.SharpnessIIEnchant, color = new Color(0.85f, 0.52f, 0.4f), damage = 1.4f },
        new EnchantDef { id = ItemId.RapidFireIIEnchant, color = new Color(0.4f, 0.6f, 1f), rate = 0.6f, heat = 1.2f },
        new EnchantDef { id = ItemId.IceColdEnchant, color = new Color(0.4f, 0.85f, 1f), heat = 0.35f },
        new EnchantDef { id = ItemId.VampireIIEnchant, color = new Color(0.95f, 0.25f, 0.45f), lifesteal = 0.25f, damage = 0.9f },
        new EnchantDef { id = ItemId.BigBoomEnchant, color = new Color(0.3f, 0.3f, 0.33f), hitBlast = 2.6f, damage = 0.85f },
        new EnchantDef { id = ItemId.StormChainEnchant, color = new Color(0.5f, 0.75f, 1f), chain = 4, element = Element.Shock, damage = 0.85f },
        new EnchantDef { id = ItemId.DeepPierceEnchant, color = new Color(0.45f, 0.3f, 0.65f), pierce = true, damage = 1.1f },
        new EnchantDef { id = ItemId.TripleShotEnchant, color = new Color(0.85f, 0.7f, 1f), pellets = 3, damage = 0.55f },
        new EnchantDef { id = ItemId.SniperScopeEnchant, color = new Color(0.75f, 0.95f, 1f), range = 2, spread = 0.2f },
        new EnchantDef { id = ItemId.HeavyHitterEnchant, color = new Color(0.72f, 0.5f, 0.3f), knock = 8, damage = 1.1f },
        new EnchantDef { id = ItemId.LuckyEnchant, color = new Color(1f, 0.82f, 0.25f), crit = 0.4f },
        new EnchantDef { id = ItemId.GlueEnchant, color = new Color(0.35f, 1f, 0.45f), element = Element.Slime, knock = 2 },
        new EnchantDef { id = ItemId.StaticEnchant, color = new Color(0.5f, 0.75f, 1f), element = Element.Shock, rate = 0.9f },
        new EnchantDef { id = ItemId.PlagueEnchant, color = new Color(0.55f, 1f, 0.2f), element = Element.Poison, hitBlast = 1.6f },
        new EnchantDef { id = ItemId.WildfireEnchant, color = new Color(1f, 0.5f, 0.1f), element = Element.Fire, hitBlast = 1.6f },
        new EnchantDef { id = ItemId.CometEnchant, color = new Color(1f, 0.5f, 0.1f), element = Element.Fire, hitBlast = 3, damage = 1.1f, heat = 1.3f },
        new EnchantDef { id = ItemId.DemolitionEnchant, color = new Color(0.75f, 0.78f, 0.82f), structure = 3 },
        new EnchantDef { id = ItemId.RegrowthEnchant, color = new Color(0.45f, 0.7f, 0.3f), lifesteal = 0.12f },
        new EnchantDef { id = ItemId.RadiantEnchant, color = new Color(1f, 0.95f, 0.65f), damage = 1.25f, rate = 0.85f },
        new EnchantDef { id = ItemId.HealingHerbsEnchant, color = new Color(0.4f, 0.95f, 0.3f), lifesteal = 0.15f },
        new EnchantDef { id = ItemId.LightweightEnchant, color = new Color(0.95f, 0.95f, 0.95f), heat = 0.5f, rate = 0.85f },
        new EnchantDef { id = ItemId.SuperchargeEnchant, color = new Color(0.4f, 0.6f, 1f), damage = 1.6f, heat = 1.8f },
        new EnchantDef { id = ItemId.PermafrostEnchant, color = new Color(0.55f, 0.92f, 1f), element = Element.Ice, damage = 1.2f },
        new EnchantDef { id = ItemId.FungalEnchant, color = new Color(0.55f, 1f, 0.2f), element = Element.Poison, lifesteal = 0.08f },
        new EnchantDef { id = ItemId.DeadeyeEnchant, color = new Color(0.75f, 0.95f, 1f), crit = 0.55f, spread = 0.2f, rate = 1.1f },
        new EnchantDef { id = ItemId.RubberEnchant, color = new Color(0.95f, 0.95f, 0.95f), knock = 6 },
        new EnchantDef { id = ItemId.StarfallEnchant, color = new Color(0.85f, 0.7f, 1f), hitBlast = 2, damage = 1.15f },
        new EnchantDef { id = ItemId.VolcanicEnchant, color = new Color(1f, 0.5f, 0.1f), element = Element.Fire, damage = 1.3f },
        new EnchantDef { id = ItemId.SuperconductorEnchant, color = new Color(0.95f, 0.55f, 0.3f), chain = 2, rate = 0.8f },
        new EnchantDef { id = ItemId.PetalPowerEnchant, color = new Color(1f, 0.62f, 0.85f), damage = 1.1f, rate = 0.9f, lifesteal = 0.05f },
    };
}
