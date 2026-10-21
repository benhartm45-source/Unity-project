using System.Collections.Generic;
using UnityEngine;

// EVERYTHING YOU CAN COLLECT AND CRAFT.
// Want to change a recipe, a brew time or a description? It's all in the big list below.
//
// Materials (25): the first 5 (Herb, Slime, Crystal, Metal, Chemical) are what today's recipes use.
//   The other 20 come from the bigger map (forests, caves, the office building, the meteor crater) and are
//   there for the recipes still to come. To add a material: add it to Mat, MatNames, MatColors and MatSound.
// Recipes are SECRET: players find them by putting materials into a station and pressing COMBINE.
// Stations (in your lab only):
//   Brewing Stand  - potions (they take a few seconds to brew, you can walk away meanwhile)
//   Workbench      - weapons and armour
//   Enchanting     - puts Fire / Ice / Poison on the weapon you're holding
//   Lab Terminal   - lab upgrades: stronger walls, forcefield, turret, slime traps, repairs
//   Storage        - where your materials go when you walk into your lab

public enum Mat
{
    Herb, Slime, Crystal, Metal, Chemical,
    Berry, Wood, Sap, Mushroom, Moss, Petal,                                    // forests and fields
    Coal, IronOre, GoldOre, Sulfur, Glowstone, Uranium, Obsidian,               // caves
    Circuit, CopperWire, Battery, Glass, Plastic,                               // the office building
    Meteorite, Stardust,                                                        // the meteor crater
}
public enum ItemKind { Weapon, Potion, Throwable, Armor, Enchant, Upgrade }
public enum Station { Brew, Workbench, Enchant, Storage, Terminal }
public enum Element { None, Fire, Ice, Poison, Slime, Shock }

public enum ItemId
{
    Zapper, FreezeRay, PlasmaLauncher, SlimeCannon,
    HealPotion, SpeedPotion, LeapPotion, IronSkinPotion, ToxicBomb,
    ScrapArmor, CrystalArmor, BounceBoots,
    FireEnchant, IceEnchant, PoisonEnchant,
    WallsI, WallsII, Forcefield, Turret, SlimeTraps, Repair, FastBrew,
    // more guns (stats in Guns.cs) - always add new things at the END so saved discoveries stay right
    BerryBlaster, WoodPistol, SapSprayer, SporeGun, MossMusket, PetalPopper, CoalCannon, IronRifle, GoldRevolver, SulfurFlamer,
    GlowstoneLaser, UraniumRailgun, ObsidianShotgun, CircuitBlaster, CopperCoil, BatterySMG, GlassSniper, PlasticSquirter, MeteorLauncher, StarShooter,
    // 30 more guns
    HerbDartGun, SlimeShotgun, CrystalRepeater, MetalMinigun, AcidSprayer, BerryBomber,
    WoodCrossbow, SapCannon, MushroomMortar, MossSniper, PetalStorm, CoalFlamethrower,
    IronShotgun, GoldBlaster, SulfurGrenadeLauncher, GlowstoneBeam, UraniumCannon, ObsidianRifle,
    CircuitPistol, TeslaRifle, BatteryCannon, GlassShardLauncher, PlasticBubbleGun, MeteorMinigun,
    StardustNova, FrostCrystalCannon, SlimeLauncher, PlasmaRifle, GoldSniper, IronHandCannon,
    // 30 armours
    LeafVest, SlimeSuit, CrystalPlate, HeavyMetalPlate, HazmatSuit, BrambleVest,
    WoodenArmour, AmberArmour, MushroomCapArmour, MossCloak, PetalRobe, CoalArmour,
    IronArmour, GoldArmour, FireproofSuit, GlowstoneArmour, UraniumArmour, ObsidianArmour,
    CircuitExosuit, CopperArmour, PowerSuit, GlassArmour, PlasticRaincoat, MeteoriteArmour,
    StardustRobe, FrostArmour, ScrapKnightArmour, FeatherSuit, ZoneSuit, KingsArmour,
    // 30 potions and bombs
    BerryJuice, BigHealingPotion, RegenPotion, MushroomBrew, SapSyrup, GhostPotion,
    ShieldPotion, FireResistance, IronGutPotion, GoldenElixir, UraniumRage, ObsidianSkin,
    OverclockPotion, Coolant, BatteryBoost, FeatherPotion, Antidote, WarmingPotion,
    StarJumpPotion, MeteorRage, GlassCannonPotion, GroundingPotion, FireBomb, IceBomb,
    SlimeBomb, ShockBomb, StickySapBomb, MeteorBomb, UraniumBomb, BerryGrenade,
    // 30 enchants
    SharpnessEnchant, RapidFireEnchant, CoolingEnchant, VampireEnchant, ExplosiveEnchant, ChainLightningEnchant,
    PiercingEnchant, MultishotEnchant, LongRangeEnchant, KnockbackEnchant, CriticalEnchant, SlimeEnchant,
    ShockEnchant, RadioactiveEnchant, InfernoEnchant, MeteorStrikeEnchant, SiegeEnchant, MossLeechEnchant,
    GlowingEnchant, HerbalEnchant, FeatherweightEnchant, OverchargeEnchant, FrostbiteEnchant, ToxicSporesEnchant,
    PrecisionEnchant, BouncyEnchant, StardustSparkleEnchant, ObsidianEdgeEnchant, ConductorEnchant, AmberSapEnchant,
    // batch 2: 30 guns
    HerbLeechRifle, SlimeMinigun, CrystalShotgun, RivetGun, GrenadeRifle, BerryShotgun,
    WoodLongbow, SapSniper, SporeSprayer, MossBlaster, PetalPistol, CoalMortar,
    IronRevolver, GoldMinigun, SulfurShotgun, GlowstoneSniper, UraniumSMG, ObsidianCannon,
    CircuitRailgun, CopperShotgun, BatteryBeam, GlassRailgun, FoamDartBlaster, MeteorShotgun,
    StarlightRifle, FrostBeam, ToxicMortar, ThunderCannon, VampirePistol, LabSmasher,
    // batch 2: 30 armours
    HerbRobe, SlimeArmour, CrystalVest, RiotArmour, AcidArmour, BerryRobe,
    BarkArmour, SapShell, MushroomRobe, MossArmour, PetalArmour, CoalSuit,
    IronKnightArmour, GoldRobe, SulfurArmour, GlowstoneRobe, UraniumSuit, ObsidianVest,
    CircuitArmour, CopperSuit, JetSuit, GlassVest, PlasticArmour, MeteorSuit,
    StardustArmour, NinjaSuit, TankArmour, MedicSuit, StormSuit, RoyalRobe,
    // batch 2: 30 potions and bombs
    BerrySmoothie, HerbTea, SlimeShake, CrystalWater, SuperShieldPotion, SpeedDemon,
    TurtlePotion, MegaRegen, BerserkerBrew, FocusPotion, ColdBrew, BouncyPotion,
    MoonPotion, IronWill, GoldenShield, HazmatPotion, StormPotion, PhantomPotion,
    LavaPotion, WoodElixir, RocketFuel, UraniumShake, FragGrenade, FreezeGrenade,
    LightningGrenade, AcidBomb, SporeBomb, SulfurFirebomb, StarBomb, GlueBomb,
    // batch 2: 30 enchants
    SharpnessIIEnchant, RapidFireIIEnchant, IceColdEnchant, VampireIIEnchant, BigBoomEnchant, StormChainEnchant,
    DeepPierceEnchant, TripleShotEnchant, SniperScopeEnchant, HeavyHitterEnchant, LuckyEnchant, GlueEnchant,
    StaticEnchant, PlagueEnchant, WildfireEnchant, CometEnchant, DemolitionEnchant, RegrowthEnchant,
    RadiantEnchant, HealingHerbsEnchant, LightweightEnchant, SuperchargeEnchant, PermafrostEnchant, FungalEnchant,
    DeadeyeEnchant, RubberEnchant, StarfallEnchant, VolcanicEnchant, SuperconductorEnchant, PetalPowerEnchant,
}

public class Recipe
{
    public ItemId id;
    public string name, desc;
    public ItemKind kind;
    public Station station;
    public int[] cost;          // how many of each material (in Mat order)
    public float time;          // brewing seconds (potions) or work seconds (everything else)
    public int makes = 1;       // how many you get
    public Color color;
    public string icon;         // picture in Resources/Icons
    public ItemId? needs;       // must have built this first (lab upgrades)
}

public static class Items
{
    public const int MatCount = 25;
    public const int BaseMatCount = 5;          // the original five (lab storage bins, rival lab loot)
    public static readonly string[] MatNames =
    {
        "Herb", "Slime", "Crystal", "Metal", "Chemical",
        "Berry", "Wood", "Sap", "Mushroom", "Moss", "Petal",
        "Coal", "Iron Ore", "Gold Ore", "Sulfur", "Glowstone", "Uranium", "Obsidian",
        "Circuit Board", "Copper Wire", "Battery", "Glass", "Plastic",
        "Meteorite", "Stardust",
    };
    public static readonly Color[] MatColors =
    {
        new Color(0.40f, 0.95f, 0.30f),     // herb - leafy green
        new Color(0.30f, 0.95f, 0.75f),     // slime - teal
        new Color(0.40f, 0.85f, 1.00f),     // crystal - ice blue
        new Color(0.75f, 0.78f, 0.82f),     // metal - steel
        new Color(0.80f, 0.40f, 1.00f),     // chemical - purple
        new Color(0.95f, 0.25f, 0.45f),     // berry - red
        new Color(0.72f, 0.50f, 0.30f),     // wood - brown
        new Color(1.00f, 0.70f, 0.20f),     // sap - amber
        new Color(1.00f, 0.45f, 0.40f),     // mushroom - red cap
        new Color(0.45f, 0.70f, 0.30f),     // moss - mossy green
        new Color(1.00f, 0.62f, 0.85f),     // petal - pink
        new Color(0.50f, 0.50f, 0.55f),     // coal - dark grey
        new Color(0.85f, 0.52f, 0.40f),     // iron ore - rusty
        new Color(1.00f, 0.82f, 0.25f),     // gold ore - gold
        new Color(0.95f, 0.95f, 0.30f),     // sulfur - yellow
        new Color(1.00f, 0.95f, 0.65f),     // glowstone - warm white
        new Color(0.55f, 1.00f, 0.20f),     // uranium - radioactive green
        new Color(0.60f, 0.40f, 0.85f),     // obsidian - dark purple
        new Color(0.25f, 0.85f, 0.45f),     // circuit board - pcb green
        new Color(0.95f, 0.55f, 0.30f),     // copper wire - copper
        new Color(0.40f, 0.60f, 1.00f),     // battery - blue
        new Color(0.75f, 0.95f, 1.00f),     // glass - pale blue
        new Color(0.95f, 0.95f, 0.95f),     // plastic - white
        new Color(1.00f, 0.40f, 0.20f),     // meteorite - glowing orange
        new Color(0.85f, 0.70f, 1.00f),     // stardust - lilac
    };
    // which pickup sound each material uses (Resources/Audio/pickup_<name>)
    public static readonly string[] MatSound =
    {
        "herb", "slime", "crystal", "metal", "chemical",
        "herb", "metal", "slime", "herb", "herb", "herb",
        "metal", "metal", "crystal", "chemical", "crystal", "chemical", "crystal",
        "metal", "metal", "metal", "crystal", "metal",
        "crystal", "crystal",
    };

    // Costs: C(herb: 2, metal: 1) for the first five materials...
    static int[] C(int herb = 0, int slime = 0, int crystal = 0, int metal = 0, int chem = 0)
    {
        var c = new int[MatCount];
        c[0] = herb; c[1] = slime; c[2] = crystal; c[3] = metal; c[4] = chem;
        return c;
    }

    // ...or Cost(Mat.Berry, 3, Mat.Metal, 1) for any materials (pairs of material, how many).
    static int[] Cost(params object[] pairs)
    {
        var c = new int[MatCount];
        for (int i = 0; i + 1 < pairs.Length; i += 2) c[(int)(Mat)pairs[i]] += (int)pairs[i + 1];
        return c;
    }

    static Recipe Gun(ItemId id, string name, string desc, int[] cost, float time, string icon)
    {
        GunDef g = Guns.Get(id);
        return new Recipe { id = id, name = name, kind = ItemKind.Weapon, station = Station.Workbench, cost = cost, time = time, desc = desc, color = g != null ? g.color : Color.white, icon = icon };
    }

    public static readonly Recipe[] All =
    {
        // ---------------- Weapons (Workbench) ----------------
        new Recipe { id = ItemId.Zapper, name = "Zapper", kind = ItemKind.Weapon, station = Station.Workbench, cost = C(metal: 1), time = 1.2f,
            desc = "Your trusty starter gun. Quick electric bolts.", color = new Color(0.4f, 0.7f, 1f), icon = "zapper" },
        new Recipe { id = ItemId.FreezeRay, name = "Freeze Ray", kind = ItemKind.Weapon, station = Station.Workbench, cost = C(crystal: 2, metal: 1), time = 1.5f,
            desc = "Hold to spray a freezing beam. Slows, then freezes solid.", color = new Color(0.55f, 0.95f, 1f), icon = "freeze" },
        new Recipe { id = ItemId.PlasmaLauncher, name = "Plasma Launcher", kind = ItemKind.Weapon, station = Station.Workbench, cost = C(crystal: 1, metal: 2, chem: 2), time = 1.8f,
            desc = "Big exploding plasma balls. Knocks people flying and smashes labs.", color = new Color(1f, 0.45f, 0.9f), icon = "plasma" },
        new Recipe { id = ItemId.SlimeCannon, name = "Slime Cannon", kind = ItemKind.Weapon, station = Station.Workbench, cost = C(slime: 2, metal: 1), time = 1.5f,
            desc = "Lobs sticky slime. Leaves puddles that slow everyone down.", color = new Color(0.4f, 1f, 0.45f), icon = "slime_cannon" },

        // ---------------- Potions (Brewing Stand) ----------------
        new Recipe { id = ItemId.HealPotion, name = "Healing Potion", kind = ItemKind.Potion, station = Station.Brew, cost = C(herb: 2), time = 6f,
            desc = "Heals 60 health over 2 seconds.", color = new Color(1f, 0.3f, 0.35f), icon = "potion_heal" },
        new Recipe { id = ItemId.SpeedPotion, name = "Speed Potion", kind = ItemKind.Potion, station = Station.Brew, cost = C(herb: 1, slime: 1), time = 7f,
            desc = "Run 45% faster for 15 seconds.", color = new Color(1f, 0.85f, 0.2f), icon = "potion_speed" },
        new Recipe { id = ItemId.LeapPotion, name = "Leap Potion", kind = ItemKind.Potion, station = Station.Brew, cost = C(slime: 1, crystal: 1), time = 7f,
            desc = "Jump super high for 15 seconds. No fall damage.", color = new Color(0.45f, 1f, 0.6f), icon = "potion_leap" },
        new Recipe { id = ItemId.IronSkinPotion, name = "Iron Skin Potion", kind = ItemKind.Potion, station = Station.Brew, cost = C(herb: 1, metal: 1, chem: 1), time = 9f,
            desc = "Take half damage for 15 seconds.", color = new Color(0.7f, 0.75f, 0.85f), icon = "potion_iron" },
        new Recipe { id = ItemId.ToxicBomb, name = "Toxic Bomb", kind = ItemKind.Throwable, station = Station.Brew, cost = C(slime: 1, chem: 1), time = 8f, makes = 2,
            desc = "Throw it! Makes a poison cloud. Eats away at labs too.", color = new Color(0.65f, 1f, 0.2f), icon = "toxic_bomb" },

        // ---------------- Armour (Workbench) ----------------
        new Recipe { id = ItemId.ScrapArmor, name = "Scrap Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = C(metal: 3), time = 1.5f,
            desc = "+50 armour. Bolted-together metal plates.", color = new Color(0.65f, 0.65f, 0.68f), icon = "armor_scrap" },
        new Recipe { id = ItemId.CrystalArmor, name = "Crystal Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = C(crystal: 2, metal: 3), time = 2f,
            desc = "+100 armour. Glowing crystal plates.", color = new Color(0.4f, 0.9f, 1f), icon = "armor_crystal" },
        new Recipe { id = ItemId.BounceBoots, name = "Bounce Boots", kind = ItemKind.Armor, station = Station.Workbench, cost = C(slime: 3, metal: 1), time = 1.5f,
            desc = "Slimy springy boots: double jump and no fall damage.", color = new Color(0.4f, 1f, 0.5f), icon = "boots" },

        // ---------------- Enchants (Enchanting Table) ----------------
        new Recipe { id = ItemId.FireEnchant, name = "Fire Enchant", kind = ItemKind.Enchant, station = Station.Enchant, cost = C(herb: 1, chem: 1), time = 1.5f,
            desc = "Your weapon sets things on fire (burn damage).", color = new Color(1f, 0.5f, 0.1f), icon = "enchant_fire" },
        new Recipe { id = ItemId.IceEnchant, name = "Ice Enchant", kind = ItemKind.Enchant, station = Station.Enchant, cost = C(slime: 1, crystal: 1), time = 1.5f,
            desc = "Your weapon slows whatever it hits.", color = new Color(0.5f, 0.9f, 1f), icon = "enchant_ice" },
        new Recipe { id = ItemId.PoisonEnchant, name = "Poison Enchant", kind = ItemKind.Enchant, station = Station.Enchant, cost = C(chem: 2), time = 1.5f,
            desc = "Your weapon poisons (stacks up!).", color = new Color(0.55f, 1f, 0.2f), icon = "enchant_poison" },

        // ---------------- Lab upgrades (Lab Terminal) ----------------
        new Recipe { id = ItemId.WallsI, name = "Reinforced Walls", kind = ItemKind.Upgrade, station = Station.Terminal, cost = C(metal: 4), time = 2f,
            desc = "Steel plates on the walls. Lab max HP +600.", color = new Color(0.7f, 0.72f, 0.75f), icon = "up_walls" },
        new Recipe { id = ItemId.WallsII, name = "Fortified Walls", kind = ItemKind.Upgrade, station = Station.Terminal, cost = C(crystal: 2, metal: 6), time = 2.5f, needs = ItemId.WallsI,
            desc = "Crystal-steel armour and spikes. Lab max HP +900 more.", color = new Color(0.5f, 0.85f, 1f), icon = "up_walls2" },
        new Recipe { id = ItemId.Forcefield, name = "Forcefield", kind = ItemKind.Upgrade, station = Station.Terminal, cost = C(crystal: 3, chem: 2), time = 2.5f,
            desc = "A glowing dome that blocks shots. Recharges after a few seconds.", color = new Color(0.4f, 0.8f, 1f), icon = "up_field" },
        new Recipe { id = ItemId.Turret, name = "Auto-Turret", kind = ItemKind.Upgrade, station = Station.Terminal, cost = C(crystal: 1, metal: 4, chem: 1), time = 2.5f,
            desc = "Roof turret that shoots anyone attacking your lab.", color = new Color(1f, 0.55f, 0.3f), icon = "up_turret" },
        new Recipe { id = ItemId.SlimeTraps, name = "Slime Traps", kind = ItemKind.Upgrade, station = Station.Terminal, cost = C(slime: 3), time = 1.5f,
            desc = "Sticky pads around your lab. Enemies get stuck.", color = new Color(0.4f, 1f, 0.5f), icon = "up_traps" },
        new Recipe { id = ItemId.Repair, name = "Repair Lab", kind = ItemKind.Upgrade, station = Station.Terminal, cost = C(herb: 1, metal: 2), time = 2f,
            desc = "Fix 400 HP of damage.", color = new Color(0.4f, 1f, 0.4f), icon = "up_repair" },
        new Recipe { id = ItemId.FastBrew, name = "Second Cauldron", kind = ItemKind.Upgrade, station = Station.Terminal, cost = C(herb: 1, metal: 2, chem: 1), time = 2f,
            desc = "Brew two potions at once, 30% faster.", color = new Color(0.8f, 0.5f, 1f), icon = "up_brew" },

        // ---------------- More guns (Workbench) - stats in Guns.cs ----------------
        Gun(ItemId.BerryBlaster, "Berry Blaster", "Sprays squishy berry bolts. Fast and fun.", Cost(Mat.Berry, 3, Mat.Metal, 1), 1.5f, "gun_berry_blaster"),
        Gun(ItemId.WoodPistol, "Wood Pistol", "A simple wooden pistol. Click for every shot.", Cost(Mat.Wood, 2, Mat.Metal, 1), 1.2f, "gun_wood_pistol"),
        Gun(ItemId.SapSprayer, "Sap Sprayer", "Hold to spray sticky sap. Slows whatever it hits.", Cost(Mat.Sap, 2, Mat.Metal, 1), 1.5f, "gun_sap_sprayer"),
        Gun(ItemId.SporeGun, "Spore Gun", "Lobs mushroom spore pods that burst into poison.", Cost(Mat.Mushroom, 2, Mat.Chemical, 1), 1.6f, "gun_spore_gun"),
        Gun(ItemId.MossMusket, "Moss Musket", "Old, slow and mossy - but one shot hits HARD.", Cost(Mat.Moss, 2, Mat.Wood, 2), 1.8f, "gun_moss_musket"),
        Gun(ItemId.PetalPopper, "Petal Popper", "A flowery shotgun. Deadly up close.", Cost(Mat.Petal, 2, Mat.Metal, 1), 1.5f, "gun_petal_popper"),
        Gun(ItemId.CoalCannon, "Coal Cannon", "Lobs burning coal that explodes. Great against labs.", Cost(Mat.Coal, 3, Mat.Metal, 2), 2f, "gun_coal_cannon"),
        Gun(ItemId.IronRifle, "Iron Rifle", "A full-auto rifle. Hold to fire.", Cost(Mat.IronOre, 3, Mat.Wood, 1), 1.8f, "gun_iron_rifle"),
        Gun(ItemId.GoldRevolver, "Gold Revolver", "Shiny and powerful. Six-shooter style.", Cost(Mat.GoldOre, 2, Mat.Metal, 1), 1.6f, "gun_gold_revolver"),
        Gun(ItemId.SulfurFlamer, "Sulfur Flamer", "A flamethrower. Sets everything nearby on fire.", Cost(Mat.Sulfur, 2, Mat.Chemical, 1, Mat.Metal, 1), 2f, "gun_sulfur_flamer"),
        Gun(ItemId.GlowstoneLaser, "Glowstone Laser", "A rapid, long-range laser beam.", Cost(Mat.Glowstone, 2, Mat.Crystal, 1), 1.6f, "gun_glowstone_laser"),
        Gun(ItemId.UraniumRailgun, "Uranium Railgun", "Charges a radioactive slug that goes through everything in a line.", Cost(Mat.Uranium, 2, Mat.Metal, 2, Mat.Battery, 1), 2.5f, "gun_uranium_railgun"),
        Gun(ItemId.ObsidianShotgun, "Obsidian Shotgun", "Fires a wide blast of razor-sharp shards.", Cost(Mat.Obsidian, 2, Mat.Metal, 2), 1.8f, "gun_obsidian_shotgun"),
        Gun(ItemId.CircuitBlaster, "Circuit Blaster", "Fires 3 electric bolts per click. Shocks (slows) targets.", Cost(Mat.Circuit, 2, Mat.Battery, 1), 1.6f, "gun_circuit_blaster"),
        Gun(ItemId.CopperCoil, "Copper Coil Gun", "Lightning that jumps between up to 4 targets.", Cost(Mat.CopperWire, 2, Mat.Battery, 1), 1.8f, "gun_copper_coil"),
        Gun(ItemId.BatterySMG, "Battery SMG", "Super fast electric bullets. Overheats quickly.", Cost(Mat.Battery, 3, Mat.Plastic, 1), 1.6f, "gun_battery_smg"),
        Gun(ItemId.GlassSniper, "Glass Sniper", "Long-range sniper rifle. Aim (right click) for a closer look.", Cost(Mat.Glass, 2, Mat.Metal, 1, Mat.Wood, 1), 2f, "gun_glass_sniper"),
        Gun(ItemId.PlasticSquirter, "Plastic Squirter", "A squirt gun full of slime. Slows people down.", Cost(Mat.Plastic, 2, Mat.Slime, 1), 1.2f, "gun_plastic_squirter"),
        Gun(ItemId.MeteorLauncher, "Meteor Launcher", "Fires a flaming chunk of meteor. HUGE explosion.", Cost(Mat.Meteorite, 2, Mat.Metal, 2), 2.5f, "gun_meteor_launcher"),
        Gun(ItemId.StarShooter, "Star Shooter", "Shoots 3 sparkly stars at once.", Cost(Mat.Stardust, 2, Mat.Crystal, 1), 1.8f, "gun_star_shooter"),

        // ---------------- 30 more guns (Workbench) - stats in Guns.cs ----------------
        Gun(ItemId.HerbDartGun, "Herb Dart Gun", "Fires little poison darts.", Cost(Mat.Herb, 3, Mat.Metal, 1), 1.6f, "gun_herb_dart_gun"),
        Gun(ItemId.SlimeShotgun, "Slime Shotgun", "A shotgun full of sticky slime. Slows everything it hits.", Cost(Mat.Slime, 3, Mat.Metal, 2), 1.6f, "gun_slime_shotgun"),
        Gun(ItemId.CrystalRepeater, "Crystal Repeater", "Fast icy crystal bolts. Hold to fire.", Cost(Mat.Crystal, 3, Mat.Metal, 1), 1.6f, "gun_crystal_repeater"),
        Gun(ItemId.MetalMinigun, "Metal Minigun", "Spins up and shreds. Overheats if you hold it forever.", Cost(Mat.Metal, 5, Mat.Battery, 1), 1.6f, "gun_metal_minigun"),
        Gun(ItemId.AcidSprayer, "Acid Sprayer", "Sprays acid that poisons people and melts labs.", Cost(Mat.Chemical, 3, Mat.Metal, 1), 1.6f, "gun_acid_sprayer"),
        Gun(ItemId.BerryBomber, "Berry Bomber", "Lobs exploding berry bombs.", Cost(Mat.Berry, 4, Mat.Chemical, 1), 1.6f, "gun_berry_bomber"),
        Gun(ItemId.WoodCrossbow, "Wood Crossbow", "Silent and deadly bolts. Aim a little high far away.", Cost(Mat.Wood, 3, Mat.Metal, 1), 1.6f, "gun_wood_crossbow"),
        Gun(ItemId.SapCannon, "Sap Cannon", "Fires a big gob of sap that leaves a sticky puddle.", Cost(Mat.Sap, 3, Mat.Metal, 2), 1.6f, "gun_sap_cannon"),
        Gun(ItemId.MushroomMortar, "Mushroom Mortar", "Lobs spore bombs high into the air. Poison cloud on landing.", Cost(Mat.Mushroom, 3, Mat.Metal, 2), 1.6f, "gun_mushroom_mortar"),
        Gun(ItemId.MossSniper, "Moss Sniper", "A camo sniper rifle. Poisons whoever it hits.", Cost(Mat.Moss, 3, Mat.Glass, 1), 1.6f, "gun_moss_sniper"),
        Gun(ItemId.PetalStorm, "Petal Storm", "A burst of spinning razor petals.", Cost(Mat.Petal, 4, Mat.Stardust, 1), 1.6f, "gun_petal_storm"),
        Gun(ItemId.CoalFlamethrower, "Coal Flamethrower", "A bigger, hotter flamethrower.", Cost(Mat.Coal, 3, Mat.Chemical, 1), 1.6f, "gun_coal_flamethrower"),
        Gun(ItemId.IronShotgun, "Iron Shotgun", "A classic pump shotgun.", Cost(Mat.IronOre, 3, Mat.Metal, 1), 1.6f, "gun_iron_shotgun"),
        Gun(ItemId.GoldBlaster, "Gold Blaster", "Fires two golden bolts per click.", Cost(Mat.GoldOre, 3, Mat.Crystal, 1), 1.6f, "gun_gold_blaster"),
        Gun(ItemId.SulfurGrenadeLauncher, "Sulfur Grenade Launcher", "Bouncy fire grenades. Boom.", Cost(Mat.Sulfur, 3, Mat.Metal, 1), 1.6f, "gun_sulfur_grenade_launcher"),
        Gun(ItemId.GlowstoneBeam, "Glowstone Beam", "A steady golden beam. Melts things slowly.", Cost(Mat.Glowstone, 3, Mat.Battery, 1), 1.6f, "gun_glowstone_beam"),
        Gun(ItemId.UraniumCannon, "Uranium Cannon", "A radioactive cannonball. Leaves a poison cloud.", Cost(Mat.Uranium, 3, Mat.Metal, 2), 1.6f, "gun_uranium_cannon"),
        Gun(ItemId.ObsidianRifle, "Obsidian Rifle", "Shards that go straight through targets.", Cost(Mat.Obsidian, 3, Mat.Metal, 1), 1.6f, "gun_obsidian_rifle"),
        Gun(ItemId.CircuitPistol, "Circuit Pistol", "A full-auto electric pistol.", Cost(Mat.Circuit, 3, Mat.Metal, 1), 1.6f, "gun_circuit_pistol"),
        Gun(ItemId.TeslaRifle, "Tesla Rifle", "Auto lightning rifle. Jumps to 2 more targets.", Cost(Mat.CopperWire, 3, Mat.Battery, 2), 1.6f, "gun_tesla_rifle"),
        Gun(ItemId.BatteryCannon, "Battery Cannon", "Fires a ball of pure electricity that explodes.", Cost(Mat.Battery, 3, Mat.Metal, 2), 1.6f, "gun_battery_cannon"),
        Gun(ItemId.GlassShardLauncher, "Glass Shard Launcher", "Sprays sharp glass shards.", Cost(Mat.Glass, 3, Mat.Metal, 1), 1.6f, "gun_glass_shard_launcher"),
        Gun(ItemId.PlasticBubbleGun, "Plastic Bubble Gun", "Floaty slime bubbles. Silly, but sticky.", Cost(Mat.Plastic, 3, Mat.Slime, 2), 1.6f, "gun_plastic_bubble_gun"),
        Gun(ItemId.MeteorMinigun, "Meteor Minigun", "Rapid-fire burning meteor chunks.", Cost(Mat.Meteorite, 3, Mat.Battery, 1), 1.6f, "gun_meteor_minigun"),
        Gun(ItemId.StardustNova, "Stardust Nova", "Fires a burst of 4 shooting stars.", Cost(Mat.Stardust, 3, Mat.Crystal, 2), 1.6f, "gun_stardust_nova"),
        Gun(ItemId.FrostCrystalCannon, "Frost Crystal Cannon", "An icy blast that freezes everything nearby.", Cost(Mat.Crystal, 4, Mat.Chemical, 1), 1.6f, "gun_frost_crystal_cannon"),
        Gun(ItemId.SlimeLauncher, "Slime Launcher", "A huge exploding slime ball. Leaves a puddle.", Cost(Mat.Slime, 4, Mat.Metal, 1), 1.6f, "gun_slime_launcher"),
        Gun(ItemId.PlasmaRifle, "Plasma Rifle", "Auto rifle with tiny exploding plasma shots.", Cost(Mat.Chemical, 3, Mat.Crystal, 1, Mat.Metal, 1), 1.7f, "gun_plasma_rifle"),
        Gun(ItemId.GoldSniper, "Gold Sniper", "The hardest-hitting single shot in the game.", Cost(Mat.GoldOre, 3, Mat.Glass, 1), 1.6f, "gun_gold_sniper"),
        Gun(ItemId.IronHandCannon, "Iron Hand Cannon", "A pistol that fires small cannonballs. Smashes labs.", Cost(Mat.IronOre, 4, Mat.Coal, 1), 1.6f, "gun_iron_hand_cannon"),

        // ---------------- 30 armours (Workbench) - stats in Gear.cs ----------------
        new Recipe { id = ItemId.LeafVest, name = "Leaf Vest", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Herb, 3, Mat.Moss, 1), time = 1.8f, desc = "Leaves woven together. Slowly heals you.", color = new Color(0.4f, 0.95f, 0.3f), icon = "armor_leaf_vest" },
        new Recipe { id = ItemId.SlimeSuit, name = "Slime Suit", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Slime, 3, Mat.Plastic, 1), time = 1.8f, desc = "Squishy! Jump higher, no fall damage, slime can't slow you.", color = new Color(0.3f, 0.95f, 0.75f), icon = "armor_slime_suit" },
        new Recipe { id = ItemId.CrystalPlate, name = "Crystal Plate", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Crystal, 4, Mat.Metal, 2), time = 1.8f, desc = "Thick glowing crystal plates. A little heavy.", color = new Color(0.4f, 0.85f, 1f), icon = "armor_crystal_plate" },
        new Recipe { id = ItemId.HeavyMetalPlate, name = "Heavy Metal Plate", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Metal, 6, Mat.IronOre, 1), time = 1.8f, desc = "Huge steel armour. Slow, but almost unstoppable.", color = new Color(0.75f, 0.78f, 0.82f), icon = "armor_heavy_metal_plate" },
        new Recipe { id = ItemId.HazmatSuit, name = "Hazmat Suit", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Chemical, 3, Mat.Plastic, 2), time = 1.8f, desc = "Poison can't touch you and the zone hurts much less.", color = new Color(0.8f, 0.4f, 1f), icon = "armor_hazmat_suit" },
        new Recipe { id = ItemId.BrambleVest, name = "Bramble Vest", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Berry, 3, Mat.Wood, 1), time = 1.8f, desc = "Thorny berry bushes. Heals you a bit.", color = new Color(0.95f, 0.25f, 0.45f), icon = "armor_bramble_vest" },
        new Recipe { id = ItemId.WoodenArmour, name = "Wooden Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Wood, 4, Mat.Metal, 1), time = 1.8f, desc = "Simple planks of wood. Better than nothing!", color = new Color(0.72f, 0.5f, 0.3f), icon = "armor_wooden_armour" },
        new Recipe { id = ItemId.AmberArmour, name = "Amber Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Sap, 3, Mat.Wood, 2), time = 1.8f, desc = "Hardened sap. Takes 10% less damage.", color = new Color(1f, 0.7f, 0.2f), icon = "armor_amber_armour" },
        new Recipe { id = ItemId.MushroomCapArmour, name = "Mushroom Cap Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Mushroom, 3, Mat.Moss, 1), time = 1.8f, desc = "Spongy caps. Poison-proof.", color = new Color(1f, 0.45f, 0.4f), icon = "armor_mushroom_cap_armour" },
        new Recipe { id = ItemId.MossCloak, name = "Moss Cloak", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Moss, 3, Mat.Herb, 1), time = 1.8f, desc = "Light and green. Run faster, heal slowly.", color = new Color(0.45f, 0.7f, 0.3f), icon = "armor_moss_cloak" },
        new Recipe { id = ItemId.PetalRobe, name = "Petal Robe", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Petal, 3, Mat.Plastic, 1), time = 1.8f, desc = "Light as a flower. Run and jump further.", color = new Color(1f, 0.62f, 0.85f), icon = "armor_petal_robe" },
        new Recipe { id = ItemId.CoalArmour, name = "Coal Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Coal, 4, Mat.Metal, 1), time = 1.8f, desc = "Already burnt, so it can't burn again. Fireproof.", color = new Color(0.3f, 0.3f, 0.33f), icon = "armor_coal_armour" },
        new Recipe { id = ItemId.IronArmour, name = "Iron Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.IronOre, 4, Mat.Metal, 1), time = 1.8f, desc = "Solid iron plates.", color = new Color(0.85f, 0.52f, 0.4f), icon = "armor_iron_armour" },
        new Recipe { id = ItemId.GoldArmour, name = "Gold Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.GoldOre, 4, Mat.Metal, 1), time = 1.8f, desc = "Shiny gold. Takes 15% less damage.", color = new Color(1f, 0.82f, 0.25f), icon = "armor_gold_armour" },
        new Recipe { id = ItemId.FireproofSuit, name = "Fireproof Suit", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Sulfur, 3, Mat.Plastic, 1), time = 1.8f, desc = "Fireproof, and your guns cool down faster.", color = new Color(0.95f, 0.95f, 0.3f), icon = "armor_fireproof_suit" },
        new Recipe { id = ItemId.GlowstoneArmour, name = "Glowstone Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Glowstone, 4, Mat.Metal, 1), time = 1.8f, desc = "Warm and glowing. Heals you over time.", color = new Color(1f, 0.95f, 0.65f), icon = "armor_glowstone_armour" },
        new Recipe { id = ItemId.UraniumArmour, name = "Uranium Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Uranium, 3, Mat.Metal, 3), time = 1.8f, desc = "Radioactive and tough. Poison-proof.", color = new Color(0.55f, 1f, 0.2f), icon = "armor_uranium_armour" },
        new Recipe { id = ItemId.ObsidianArmour, name = "Obsidian Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Obsidian, 4, Mat.Metal, 2), time = 1.8f, desc = "Volcanic glass. Super tough, super heavy.", color = new Color(0.45f, 0.3f, 0.65f), icon = "armor_obsidian_armour" },
        new Recipe { id = ItemId.CircuitExosuit, name = "Circuit Exosuit", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Circuit, 3, Mat.Battery, 2), time = 1.8f, desc = "Robot legs! Faster and jumpier.", color = new Color(0.25f, 0.85f, 0.45f), icon = "armor_circuit_exosuit" },
        new Recipe { id = ItemId.CopperArmour, name = "Copper Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.CopperWire, 3, Mat.Metal, 2), time = 1.8f, desc = "Grounded copper. Shock-proof.", color = new Color(0.95f, 0.55f, 0.3f), icon = "armor_copper_armour" },
        new Recipe { id = ItemId.PowerSuit, name = "Power Suit", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Battery, 3, Mat.Circuit, 1, Mat.Metal, 1), time = 1.9f, desc = "Battery powered. Your guns cool down much faster.", color = new Color(0.4f, 0.6f, 1f), icon = "armor_power_suit" },
        new Recipe { id = ItemId.GlassArmour, name = "Glass Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Glass, 4, Mat.Metal, 1), time = 1.8f, desc = "Lots of armour... but you take 20% more damage. Glass breaks!", color = new Color(0.75f, 0.95f, 1f), icon = "armor_glass_armour" },
        new Recipe { id = ItemId.PlasticRaincoat, name = "Plastic Raincoat", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Plastic, 3, Mat.Herb, 1), time = 1.8f, desc = "Slime slides right off.", color = new Color(0.95f, 0.95f, 0.95f), icon = "armor_plastic_raincoat" },
        new Recipe { id = ItemId.MeteoriteArmour, name = "Meteorite Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Meteorite, 3, Mat.Metal, 2), time = 1.8f, desc = "Space rock armour. Fireproof and tough.", color = new Color(1f, 0.4f, 0.2f), icon = "armor_meteorite_armour" },
        new Recipe { id = ItemId.StardustRobe, name = "Stardust Robe", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Stardust, 3, Mat.Petal, 1), time = 1.8f, desc = "Float like a star. Big jumps, no fall damage.", color = new Color(0.85f, 0.7f, 1f), icon = "armor_stardust_robe" },
        new Recipe { id = ItemId.FrostArmour, name = "Frost Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Crystal, 3, Mat.Herb, 1), time = 1.8f, desc = "Ice can't slow or freeze you.", color = new Color(0.4f, 0.85f, 1f), icon = "armor_frost_armour" },
        new Recipe { id = ItemId.ScrapKnightArmour, name = "Scrap Knight Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Metal, 4, Mat.Wood, 2), time = 1.8f, desc = "Bolted together from junk. Hard to knock over.", color = new Color(0.75f, 0.78f, 0.82f), icon = "armor_scrap_knight_armour" },
        new Recipe { id = ItemId.FeatherSuit, name = "Feather Suit", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Petal, 2, Mat.Plastic, 2, Mat.Slime, 1), time = 1.9f, desc = "Double jump and no fall damage.", color = new Color(1f, 0.62f, 0.85f), icon = "armor_feather_suit" },
        new Recipe { id = ItemId.ZoneSuit, name = "Zone Suit", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Chemical, 2, Mat.Glass, 2), time = 1.8f, desc = "Walk around in the zone almost for free.", color = new Color(0.75f, 0.95f, 1f), icon = "armor_zone_suit" },
        new Recipe { id = ItemId.KingsArmour, name = "King's Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.GoldOre, 4, Mat.Crystal, 2), time = 1.8f, desc = "The best armour money can buy.", color = new Color(1f, 0.82f, 0.25f), icon = "armor_king_s_armour" },

        // ---------------- 30 potions and bombs (Brewing Stand) - stats in Gear.cs ----------------
        new Recipe { id = ItemId.BerryJuice, name = "Berry Juice", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Berry, 2), time = 7f, desc = "Heals 35 health right away.", color = new Color(0.95f, 0.25f, 0.45f), icon = "potion_berry_juice" },
        new Recipe { id = ItemId.BigHealingPotion, name = "Big Healing Potion", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Herb, 2, Mat.Berry, 2), time = 8f, desc = "Heals 100 health over 3 seconds.", color = new Color(0.95f, 0.25f, 0.45f), icon = "potion_big_healing_potion" },
        new Recipe { id = ItemId.RegenPotion, name = "Regen Potion", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Moss, 3), time = 7f, desc = "Heal 4 health every second for 20 seconds.", color = new Color(0.45f, 0.7f, 0.3f), icon = "potion_regen_potion" },
        new Recipe { id = ItemId.MushroomBrew, name = "Mushroom Brew", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Mushroom, 2, Mat.Herb, 1), time = 8f, desc = "Your weapons do 30% more damage for 20 seconds.", color = new Color(1f, 0.45f, 0.4f), icon = "potion_mushroom_brew" },
        new Recipe { id = ItemId.SapSyrup, name = "Sap Syrup", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Sap, 2, Mat.Berry, 1), time = 8f, desc = "Run faster and jump higher for 20 seconds.", color = new Color(1f, 0.7f, 0.2f), icon = "potion_sap_syrup" },
        new Recipe { id = ItemId.GhostPotion, name = "Ghost Potion", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Petal, 2, Mat.Chemical, 1), time = 8f, desc = "Drones and turrets can't see you for 12 seconds.", color = new Color(1f, 0.62f, 0.85f), icon = "potion_ghost_potion" },
        new Recipe { id = ItemId.ShieldPotion, name = "Shield Potion", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Glowstone, 2, Mat.Crystal, 1), time = 8f, desc = "Gives you 60 extra armour.", color = new Color(1f, 0.95f, 0.65f), icon = "potion_shield_potion" },
        new Recipe { id = ItemId.FireResistance, name = "Fire Resistance", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Coal, 2, Mat.Herb, 1), time = 8f, desc = "Fire can't hurt you for 30 seconds.", color = new Color(0.3f, 0.3f, 0.33f), icon = "potion_fire_resistance" },
        new Recipe { id = ItemId.IronGutPotion, name = "Iron Gut Potion", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.IronOre, 2, Mat.Herb, 1), time = 8f, desc = "Take 40% less damage for 15 seconds (a bit slower).", color = new Color(0.85f, 0.52f, 0.4f), icon = "potion_iron_gut_potion" },
        new Recipe { id = ItemId.GoldenElixir, name = "Golden Elixir", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.GoldOre, 2, Mat.Berry, 1), time = 8f, desc = "Heals 60 and takes less damage for 10 seconds.", color = new Color(1f, 0.82f, 0.25f), icon = "potion_golden_elixir" },
        new Recipe { id = ItemId.UraniumRage, name = "Uranium Rage", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Uranium, 2, Mat.Chemical, 1), time = 8f, desc = "60% more damage... but you take 20% more too.", color = new Color(0.55f, 1f, 0.2f), icon = "potion_uranium_rage" },
        new Recipe { id = ItemId.ObsidianSkin, name = "Obsidian Skin", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Obsidian, 2, Mat.Herb, 1), time = 8f, desc = "Take 60% less damage, but you're slow.", color = new Color(0.45f, 0.3f, 0.65f), icon = "potion_obsidian_skin" },
        new Recipe { id = ItemId.OverclockPotion, name = "Overclock Potion", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Circuit, 2, Mat.Battery, 1), time = 8f, desc = "Shoot 30% faster for 15 seconds.", color = new Color(0.25f, 0.85f, 0.45f), icon = "potion_overclock_potion" },
        new Recipe { id = ItemId.Coolant, name = "Coolant", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.CopperWire, 1, Mat.Crystal, 2), time = 8f, desc = "Your guns barely heat up for 20 seconds.", color = new Color(0.4f, 0.85f, 1f), icon = "potion_coolant" },
        new Recipe { id = ItemId.BatteryBoost, name = "Battery Boost", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Battery, 2, Mat.Slime, 1), time = 8f, desc = "SUPER fast for 8 seconds.", color = new Color(0.4f, 0.6f, 1f), icon = "potion_battery_boost" },
        new Recipe { id = ItemId.FeatherPotion, name = "Feather Potion", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Petal, 2, Mat.Slime, 1), time = 8f, desc = "Float down gently. No fall damage.", color = new Color(1f, 0.62f, 0.85f), icon = "potion_feather_potion" },
        new Recipe { id = ItemId.Antidote, name = "Antidote", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Herb, 1, Mat.Chemical, 1, Mat.Moss, 1), time = 9f, desc = "Cures poison and stops it for 20 seconds.", color = new Color(0.4f, 0.95f, 0.3f), icon = "potion_antidote" },
        new Recipe { id = ItemId.WarmingPotion, name = "Warming Potion", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Crystal, 1, Mat.Herb, 1, Mat.Coal, 1), time = 9f, desc = "Thaws you out. Ice can't freeze you for 20 seconds.", color = new Color(0.3f, 0.3f, 0.33f), icon = "potion_warming_potion" },
        new Recipe { id = ItemId.StarJumpPotion, name = "Star Jump Potion", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Stardust, 2, Mat.Slime, 1), time = 8f, desc = "Moon jumps for 15 seconds!", color = new Color(0.85f, 0.7f, 1f), icon = "potion_star_jump_potion" },
        new Recipe { id = ItemId.MeteorRage, name = "Meteor Rage", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Meteorite, 2, Mat.Chemical, 1), time = 8f, desc = "More damage and faster shooting for 12 seconds.", color = new Color(1f, 0.4f, 0.2f), icon = "potion_meteor_rage" },
        new Recipe { id = ItemId.GlassCannonPotion, name = "Glass Cannon Potion", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Glass, 2, Mat.Chemical, 1), time = 8f, desc = "DOUBLE damage... but you take 50% more.", color = new Color(0.75f, 0.95f, 1f), icon = "potion_glass_cannon_potion" },
        new Recipe { id = ItemId.GroundingPotion, name = "Grounding Potion", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.CopperWire, 2, Mat.Herb, 1), time = 8f, desc = "Shock-proof for 20 seconds.", color = new Color(0.95f, 0.55f, 0.3f), icon = "potion_grounding_potion" },
        new Recipe { id = ItemId.FireBomb, name = "Fire Bomb", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Sulfur, 2, Mat.Coal, 1), time = 8f, makes = 2, desc = "Throw it. A fiery explosion that sets things alight.", color = new Color(0.95f, 0.95f, 0.3f), icon = "potion_fire_bomb" },
        new Recipe { id = ItemId.IceBomb, name = "Ice Bomb", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Crystal, 2, Mat.Glass, 1), time = 8f, makes = 2, desc = "Throw it. Freezes everyone nearby.", color = new Color(0.4f, 0.85f, 1f), icon = "potion_ice_bomb" },
        new Recipe { id = ItemId.SlimeBomb, name = "Slime Bomb", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Slime, 2, Mat.Plastic, 1), time = 8f, makes = 2, desc = "Throw it. Sticky slime everywhere.", color = new Color(0.3f, 0.95f, 0.75f), icon = "potion_slime_bomb" },
        new Recipe { id = ItemId.ShockBomb, name = "Shock Bomb", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Battery, 1, Mat.CopperWire, 1, Mat.Chemical, 1), time = 9f, makes = 2, desc = "Throw it. An electric blast.", color = new Color(0.4f, 0.6f, 1f), icon = "potion_shock_bomb" },
        new Recipe { id = ItemId.StickySapBomb, name = "Sticky Sap Bomb", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Sap, 2, Mat.Slime, 1), time = 8f, makes = 2, desc = "Throw it. A puddle of sap that glues people down.", color = new Color(1f, 0.7f, 0.2f), icon = "potion_sticky_sap_bomb" },
        new Recipe { id = ItemId.MeteorBomb, name = "Meteor Bomb", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Meteorite, 2, Mat.Chemical, 2), time = 8f, makes = 2, desc = "Throw it. A HUGE explosion. Destroys labs.", color = new Color(1f, 0.4f, 0.2f), icon = "potion_meteor_bomb" },
        new Recipe { id = ItemId.UraniumBomb, name = "Uranium Bomb", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Uranium, 2, Mat.Slime, 1), time = 8f, makes = 2, desc = "Throw it. A radioactive poison cloud.", color = new Color(0.55f, 1f, 0.2f), icon = "potion_uranium_bomb" },
        new Recipe { id = ItemId.BerryGrenade, name = "Berry Grenade", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Berry, 3, Mat.Chemical, 1), time = 8f, makes = 2, desc = "Throw it. A cheap, juicy explosion.", color = new Color(0.95f, 0.25f, 0.45f), icon = "potion_berry_grenade" },

        // ---------------- 30 enchants (Enchanting Table) - stats in Gear.cs ----------------
        new Recipe { id = ItemId.SharpnessEnchant, name = "Sharpness", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.IronOre, 2), time = 1.5f, desc = "+25% damage.", color = new Color(0.85f, 0.52f, 0.4f), icon = "ench_sharpness" },
        new Recipe { id = ItemId.RapidFireEnchant, name = "Rapid Fire", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Battery, 2), time = 1.5f, desc = "Shoot 25% faster.", color = new Color(0.4f, 0.6f, 1f), icon = "ench_rapid_fire" },
        new Recipe { id = ItemId.CoolingEnchant, name = "Cooling", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Crystal, 1, Mat.Glass, 1), time = 1.5f, desc = "Your weapon heats up half as fast.", color = new Color(0.4f, 0.85f, 1f), icon = "ench_cooling" },
        new Recipe { id = ItemId.VampireEnchant, name = "Vampire", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Berry, 2, Mat.Chemical, 1), time = 1.5f, desc = "Heal 15% of the damage you deal.", color = new Color(0.95f, 0.25f, 0.45f), icon = "ench_vampire" },
        new Recipe { id = ItemId.ExplosiveEnchant, name = "Explosive", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Coal, 2, Mat.Chemical, 1), time = 1.5f, desc = "Every hit makes a little explosion.", color = new Color(0.3f, 0.3f, 0.33f), icon = "ench_explosive" },
        new Recipe { id = ItemId.ChainLightningEnchant, name = "Chain Lightning", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.CopperWire, 2), time = 1.5f, desc = "Hits jump to 2 more targets.", color = new Color(0.5f, 0.75f, 1f), icon = "ench_chain_lightning" },
        new Recipe { id = ItemId.PiercingEnchant, name = "Piercing", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Obsidian, 2), time = 1.5f, desc = "Shots go straight through targets.", color = new Color(0.45f, 0.3f, 0.65f), icon = "ench_piercing" },
        new Recipe { id = ItemId.MultishotEnchant, name = "Multishot", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Stardust, 2), time = 1.5f, desc = "Fire 2 extra shots (each a bit weaker).", color = new Color(0.85f, 0.7f, 1f), icon = "ench_multishot" },
        new Recipe { id = ItemId.LongRangeEnchant, name = "Long Range", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Glass, 2), time = 1.5f, desc = "60% more range, faster bullets.", color = new Color(0.75f, 0.95f, 1f), icon = "ench_long_range" },
        new Recipe { id = ItemId.KnockbackEnchant, name = "Knockback", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Wood, 2, Mat.Slime, 1), time = 1.5f, desc = "Sends people flying.", color = new Color(0.72f, 0.5f, 0.3f), icon = "ench_knockback" },
        new Recipe { id = ItemId.CriticalEnchant, name = "Critical", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.GoldOre, 2), time = 1.5f, desc = "25% chance to do DOUBLE damage.", color = new Color(1f, 0.82f, 0.25f), icon = "ench_critical" },
        new Recipe { id = ItemId.SlimeEnchant, name = "Slime Enchant", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Slime, 2, Mat.Sap, 1), time = 1.5f, desc = "Your shots slow people down.", color = new Color(0.35f, 1f, 0.45f), icon = "ench_slime_enchant" },
        new Recipe { id = ItemId.ShockEnchant, name = "Shock Enchant", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Battery, 1, Mat.Circuit, 1), time = 1.5f, desc = "Your shots shock (jolt) people.", color = new Color(0.5f, 0.75f, 1f), icon = "ench_shock_enchant" },
        new Recipe { id = ItemId.RadioactiveEnchant, name = "Radioactive", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Uranium, 2), time = 1.5f, desc = "Poison and +15% damage.", color = new Color(0.55f, 1f, 0.2f), icon = "ench_radioactive" },
        new Recipe { id = ItemId.InfernoEnchant, name = "Inferno", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Sulfur, 2), time = 1.5f, desc = "Fire and +10% damage.", color = new Color(1f, 0.5f, 0.1f), icon = "ench_inferno" },
        new Recipe { id = ItemId.MeteorStrikeEnchant, name = "Meteor Strike", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Meteorite, 2), time = 1.5f, desc = "Burning explosions on every hit.", color = new Color(1f, 0.5f, 0.1f), icon = "ench_meteor_strike" },
        new Recipe { id = ItemId.SiegeEnchant, name = "Siege", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Metal, 2, Mat.Coal, 1), time = 1.5f, desc = "Double damage against labs.", color = new Color(0.75f, 0.78f, 0.82f), icon = "ench_siege" },
        new Recipe { id = ItemId.MossLeechEnchant, name = "Moss Leech", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Moss, 2), time = 1.5f, desc = "Heal a little from every hit.", color = new Color(0.45f, 0.7f, 0.3f), icon = "ench_moss_leech" },
        new Recipe { id = ItemId.GlowingEnchant, name = "Glowing", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Glowstone, 2), time = 1.5f, desc = "+15% damage, shoot a bit faster.", color = new Color(1f, 0.95f, 0.65f), icon = "ench_glowing" },
        new Recipe { id = ItemId.HerbalEnchant, name = "Herbal", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Herb, 2, Mat.Petal, 1), time = 1.5f, desc = "Heal 10% of the damage you deal.", color = new Color(0.4f, 0.95f, 0.3f), icon = "ench_herbal" },
        new Recipe { id = ItemId.FeatherweightEnchant, name = "Featherweight", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Plastic, 2), time = 1.5f, desc = "Less heat, a bit faster.", color = new Color(0.95f, 0.95f, 0.95f), icon = "ench_featherweight" },
        new Recipe { id = ItemId.OverchargeEnchant, name = "Overcharge", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Battery, 1, Mat.Uranium, 1), time = 1.5f, desc = "+40% damage, but it overheats faster.", color = new Color(0.55f, 1f, 0.2f), icon = "ench_overcharge" },
        new Recipe { id = ItemId.FrostbiteEnchant, name = "Frostbite", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Crystal, 1, Mat.Moss, 1), time = 1.5f, desc = "Ice and +10% damage.", color = new Color(0.55f, 0.92f, 1f), icon = "ench_frostbite" },
        new Recipe { id = ItemId.ToxicSporesEnchant, name = "Toxic Spores", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Mushroom, 2), time = 1.5f, desc = "Poison puffs on every hit.", color = new Color(0.55f, 1f, 0.2f), icon = "ench_toxic_spores" },
        new Recipe { id = ItemId.PrecisionEnchant, name = "Precision", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Glass, 1, Mat.GoldOre, 1), time = 1.5f, desc = "Very accurate, 40% crit chance.", color = new Color(0.75f, 0.95f, 1f), icon = "ench_precision" },
        new Recipe { id = ItemId.BouncyEnchant, name = "Bouncy", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Slime, 1, Mat.Plastic, 1), time = 1.5f, desc = "Slime and knockback.", color = new Color(0.35f, 1f, 0.45f), icon = "ench_bouncy" },
        new Recipe { id = ItemId.StardustSparkleEnchant, name = "Stardust Sparkle", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Stardust, 1, Mat.Crystal, 1), time = 1.5f, desc = "+20% damage. Sparkly!", color = new Color(0.85f, 0.7f, 1f), icon = "ench_stardust_sparkle" },
        new Recipe { id = ItemId.ObsidianEdgeEnchant, name = "Obsidian Edge", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Obsidian, 1, Mat.IronOre, 1), time = 1.5f, desc = "+35% damage, but slower.", color = new Color(0.45f, 0.3f, 0.65f), icon = "ench_obsidian_edge" },
        new Recipe { id = ItemId.ConductorEnchant, name = "Conductor", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.CopperWire, 1, Mat.Circuit, 1), time = 1.5f, desc = "Hits jump to 1 more target and you shoot faster.", color = new Color(0.95f, 0.55f, 0.3f), icon = "ench_conductor" },
        new Recipe { id = ItemId.AmberSapEnchant, name = "Amber Sap", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Sap, 2), time = 1.5f, desc = "Sticky shots that run cooler.", color = new Color(0.35f, 1f, 0.45f), icon = "ench_amber_sap" },

        // ================ BATCH 2 ================
        // 30 guns (Workbench)
        Gun(ItemId.HerbLeechRifle, "Herb Leech Rifle", "An auto rifle that heals you for 20% of its damage.", Cost(Mat.Herb, 3, Mat.IronOre, 1), 1.6f, "gun_herb_leech_rifle"),
        Gun(ItemId.SlimeMinigun, "Slime Minigun", "Hoses everything in slime.", Cost(Mat.Slime, 4, Mat.Battery, 1), 1.6f, "gun_slime_minigun"),
        Gun(ItemId.CrystalShotgun, "Crystal Shotgun", "Icy shards that slow and freeze.", Cost(Mat.Crystal, 3, Mat.Metal, 2), 1.6f, "gun_crystal_shotgun"),
        Gun(ItemId.RivetGun, "Rivet Gun", "Nails that go through two or three targets.", Cost(Mat.Metal, 3, Mat.IronOre, 1), 1.6f, "gun_rivet_gun"),
        Gun(ItemId.GrenadeRifle, "Grenade Rifle", "An auto rifle that fires little grenades.", Cost(Mat.Chemical, 2, Mat.Metal, 2, Mat.Coal, 1), 1.7f, "gun_grenade_rifle"),
        Gun(ItemId.BerryShotgun, "Berry Shotgun", "Splattery berry buckshot.", Cost(Mat.Berry, 3, Mat.Metal, 2), 1.6f, "gun_berry_shotgun"),
        Gun(ItemId.WoodLongbow, "Wood Longbow", "Big heavy arrows. Lead your shots.", Cost(Mat.Wood, 4, Mat.Moss, 1), 1.6f, "gun_wood_longbow"),
        Gun(ItemId.SapSniper, "Sap Sniper", "A sniper that glues people in place.", Cost(Mat.Sap, 3, Mat.Glass, 1), 1.6f, "gun_sap_sniper"),
        Gun(ItemId.SporeSprayer, "Spore Sprayer", "Sprays clouds of poison spores.", Cost(Mat.Mushroom, 3, Mat.Metal, 1), 1.6f, "gun_spore_sprayer"),
        Gun(ItemId.MossBlaster, "Moss Blaster", "Three poison bolts per click.", Cost(Mat.Moss, 3, Mat.Battery, 1), 1.6f, "gun_moss_blaster"),
        Gun(ItemId.PetalPistol, "Petal Pistol", "Pretty, and it crits 30% of the time.", Cost(Mat.Petal, 3, Mat.GoldOre, 1), 1.6f, "gun_petal_pistol"),
        Gun(ItemId.CoalMortar, "Coal Mortar", "Lobs huge burning coals over walls.", Cost(Mat.Coal, 4, Mat.Metal, 2), 1.6f, "gun_coal_mortar"),
        Gun(ItemId.IronRevolver, "Iron Revolver", "A heavy six-shooter.", Cost(Mat.IronOre, 3, Mat.Wood, 1, Mat.Metal, 1), 1.7f, "gun_iron_revolver"),
        Gun(ItemId.GoldMinigun, "Gold Minigun", "A golden bullet hose. Sometimes crits.", Cost(Mat.GoldOre, 4, Mat.Battery, 1), 1.6f, "gun_gold_minigun"),
        Gun(ItemId.SulfurShotgun, "Sulfur Shotgun", "Dragon's breath shells. Sets people on fire.", Cost(Mat.Sulfur, 3, Mat.Metal, 2), 1.6f, "gun_sulfur_shotgun"),
        Gun(ItemId.GlowstoneSniper, "Glowstone Sniper", "A laser sniper that goes through targets.", Cost(Mat.Glowstone, 3, Mat.Glass, 1), 1.6f, "gun_glowstone_sniper"),
        Gun(ItemId.UraniumSMG, "Uranium SMG", "Rapid radioactive pellets.", Cost(Mat.Uranium, 3, Mat.Metal, 1), 1.6f, "gun_uranium_smg"),
        Gun(ItemId.ObsidianCannon, "Obsidian Cannon", "A cannonball of volcanic glass. Wrecks labs.", Cost(Mat.Obsidian, 4, Mat.Metal, 1, Mat.Coal, 1), 1.7f, "gun_obsidian_cannon"),
        Gun(ItemId.CircuitRailgun, "Circuit Railgun", "An electric rail that pierces everything.", Cost(Mat.Circuit, 3, Mat.Battery, 2, Mat.Metal, 1), 1.7f, "gun_circuit_railgun"),
        Gun(ItemId.CopperShotgun, "Copper Shotgun", "Lightning buckshot. Every pellet can jump.", Cost(Mat.CopperWire, 3, Mat.Metal, 1, Mat.Battery, 1), 1.7f, "gun_copper_shotgun"),
        Gun(ItemId.BatteryBeam, "Battery Beam", "A crackling electric beam.", Cost(Mat.Battery, 3, Mat.Glass, 1), 1.6f, "gun_battery_beam"),
        Gun(ItemId.GlassRailgun, "Glass Railgun", "Crystal-clear rail shot. Huge damage.", Cost(Mat.Glass, 3, Mat.Battery, 1, Mat.Metal, 1), 1.7f, "gun_glass_railgun"),
        Gun(ItemId.FoamDartBlaster, "Foam Dart Blaster", "Soft darts. Not much damage, lots of knockback.", Cost(Mat.Plastic, 3, Mat.Metal, 1), 1.6f, "gun_foam_dart_blaster"),
        Gun(ItemId.MeteorShotgun, "Meteor Shotgun", "Burning space-rock pellets.", Cost(Mat.Meteorite, 3, Mat.Metal, 1, Mat.Coal, 1), 1.7f, "gun_meteor_shotgun"),
        Gun(ItemId.StarlightRifle, "Starlight Rifle", "Starlight bolts that bounce to a second target.", Cost(Mat.Stardust, 3, Mat.Glass, 1), 1.6f, "gun_starlight_rifle"),
        Gun(ItemId.FrostBeam, "Frost Beam", "A long freezing beam.", Cost(Mat.Crystal, 2, Mat.Glowstone, 2), 1.6f, "gun_frost_beam"),
        Gun(ItemId.ToxicMortar, "Toxic Mortar", "Lobs toxic waste. Leaves a poison cloud.", Cost(Mat.Chemical, 2, Mat.Uranium, 2), 1.6f, "gun_toxic_mortar"),
        Gun(ItemId.ThunderCannon, "Thunder Cannon", "A ball of thunder. Big shocking boom.", Cost(Mat.Battery, 2, Mat.CopperWire, 2, Mat.Metal, 1), 1.7f, "gun_thunder_cannon"),
        Gun(ItemId.VampirePistol, "Vampire Pistol", "Heals you for 30% of every hit.", Cost(Mat.Berry, 2, Mat.GoldOre, 2), 1.6f, "gun_vampire_pistol"),
        Gun(ItemId.LabSmasher, "Lab Smasher", "Made for one job: knocking down labs (5x damage to them).", Cost(Mat.Metal, 3, Mat.Coal, 2, Mat.Chemical, 1), 1.7f, "gun_lab_smasher"),

        // 30 armours (Workbench)
        new Recipe { id = ItemId.HerbRobe, name = "Herb Robe", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Herb, 4, Mat.Plastic, 1), time = 1.8f, desc = "Smells nice. Heals you 2 health a second.", color = new Color(0.4f, 0.95f, 0.3f), icon = "armor_herb_robe" },
        new Recipe { id = ItemId.SlimeArmour, name = "Slime Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Slime, 4, Mat.Metal, 2), time = 1.8f, desc = "Wobbly but tough. Slime can't slow you.", color = new Color(0.3f, 0.95f, 0.75f), icon = "armor_slime_armour" },
        new Recipe { id = ItemId.CrystalVest, name = "Crystal Vest", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Crystal, 3, Mat.Plastic, 1), time = 1.8f, desc = "Light crystal panels.", color = new Color(0.4f, 0.85f, 1f), icon = "armor_crystal_vest" },
        new Recipe { id = ItemId.RiotArmour, name = "Riot Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Metal, 4, Mat.Plastic, 2), time = 1.8f, desc = "Police-style riot gear. Barely moves when hit.", color = new Color(0.75f, 0.78f, 0.82f), icon = "armor_riot_armour" },
        new Recipe { id = ItemId.AcidArmour, name = "Acid Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Chemical, 3, Mat.Metal, 2), time = 1.8f, desc = "Acid-proof plates.", color = new Color(0.8f, 0.4f, 1f), icon = "armor_acid_armour" },
        new Recipe { id = ItemId.BerryRobe, name = "Berry Robe", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Berry, 4, Mat.Plastic, 1), time = 1.8f, desc = "Juicy. Heals you fast.", color = new Color(0.95f, 0.25f, 0.45f), icon = "armor_berry_robe" },
        new Recipe { id = ItemId.BarkArmour, name = "Bark Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Wood, 3, Mat.Moss, 2), time = 1.8f, desc = "Living tree bark.", color = new Color(0.72f, 0.5f, 0.3f), icon = "armor_bark_armour" },
        new Recipe { id = ItemId.SapShell, name = "Sap Shell", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Sap, 3, Mat.Plastic, 2), time = 1.8f, desc = "A hard amber shell.", color = new Color(1f, 0.7f, 0.2f), icon = "armor_sap_shell" },
        new Recipe { id = ItemId.MushroomRobe, name = "Mushroom Robe", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Mushroom, 3, Mat.Plastic, 1), time = 1.8f, desc = "Spore-proof and soft.", color = new Color(1f, 0.45f, 0.4f), icon = "armor_mushroom_robe" },
        new Recipe { id = ItemId.MossArmour, name = "Moss Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Moss, 4, Mat.Metal, 1), time = 1.8f, desc = "Mossy plates. Heals you slowly.", color = new Color(0.45f, 0.7f, 0.3f), icon = "armor_moss_armour" },
        new Recipe { id = ItemId.PetalArmour, name = "Petal Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Petal, 4, Mat.Metal, 1), time = 1.8f, desc = "Flowery and light.", color = new Color(1f, 0.62f, 0.85f), icon = "armor_petal_armour" },
        new Recipe { id = ItemId.CoalSuit, name = "Coal Suit", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Coal, 3, Mat.Plastic, 1), time = 1.8f, desc = "Fireproof and quick.", color = new Color(0.3f, 0.3f, 0.33f), icon = "armor_coal_suit" },
        new Recipe { id = ItemId.IronKnightArmour, name = "Iron Knight Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.IronOre, 5, Mat.Metal, 1), time = 1.8f, desc = "A full suit of knight's armour.", color = new Color(0.85f, 0.52f, 0.4f), icon = "armor_iron_knight_armour" },
        new Recipe { id = ItemId.GoldRobe, name = "Gold Robe", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.GoldOre, 3, Mat.Plastic, 1), time = 1.8f, desc = "Fancy. Takes 10% less damage.", color = new Color(1f, 0.82f, 0.25f), icon = "armor_gold_robe" },
        new Recipe { id = ItemId.SulfurArmour, name = "Sulfur Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Sulfur, 4, Mat.Metal, 1), time = 1.8f, desc = "Fireproof, and guns cool faster.", color = new Color(0.95f, 0.95f, 0.3f), icon = "armor_sulfur_armour" },
        new Recipe { id = ItemId.GlowstoneRobe, name = "Glowstone Robe", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Glowstone, 3, Mat.Plastic, 1), time = 1.8f, desc = "Glows warmly and heals you.", color = new Color(1f, 0.95f, 0.65f), icon = "armor_glowstone_robe" },
        new Recipe { id = ItemId.UraniumSuit, name = "Uranium Suit", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Uranium, 3, Mat.Plastic, 1), time = 1.8f, desc = "Built for the zone. Poison-proof.", color = new Color(0.55f, 1f, 0.2f), icon = "armor_uranium_suit" },
        new Recipe { id = ItemId.ObsidianVest, name = "Obsidian Vest", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Obsidian, 3, Mat.Plastic, 1), time = 1.8f, desc = "Light but tough volcanic glass.", color = new Color(0.45f, 0.3f, 0.65f), icon = "armor_obsidian_vest" },
        new Recipe { id = ItemId.CircuitArmour, name = "Circuit Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Circuit, 4, Mat.Metal, 1), time = 1.8f, desc = "Built-in cooling fans for your guns.", color = new Color(0.25f, 0.85f, 0.45f), icon = "armor_circuit_armour" },
        new Recipe { id = ItemId.CopperSuit, name = "Copper Suit", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.CopperWire, 3, Mat.Plastic, 1), time = 1.8f, desc = "Shock-proof and fast.", color = new Color(0.95f, 0.55f, 0.3f), icon = "armor_copper_suit" },
        new Recipe { id = ItemId.JetSuit, name = "Jet Suit", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Battery, 2, Mat.Plastic, 2, Mat.Metal, 1), time = 1.9f, desc = "Rocket boosters: huge jumps and a double jump.", color = new Color(0.4f, 0.6f, 1f), icon = "armor_jet_suit" },
        new Recipe { id = ItemId.GlassVest, name = "Glass Vest", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Glass, 3, Mat.Plastic, 1), time = 1.8f, desc = "Fast and lots of armour, but fragile.", color = new Color(0.75f, 0.95f, 1f), icon = "armor_glass_vest" },
        new Recipe { id = ItemId.PlasticArmour, name = "Plastic Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Plastic, 4, Mat.Metal, 1), time = 1.8f, desc = "Cheap and cheerful.", color = new Color(0.95f, 0.95f, 0.95f), icon = "armor_plastic_armour" },
        new Recipe { id = ItemId.MeteorSuit, name = "Meteor Suit", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Meteorite, 3, Mat.Plastic, 1), time = 1.8f, desc = "Light space-rock suit. Fireproof.", color = new Color(1f, 0.4f, 0.2f), icon = "armor_meteor_suit" },
        new Recipe { id = ItemId.StardustArmour, name = "Stardust Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Stardust, 4, Mat.Metal, 1), time = 1.8f, desc = "Sparkly plates. Jump higher, no fall damage.", color = new Color(0.85f, 0.7f, 1f), icon = "armor_stardust_armour" },
        new Recipe { id = ItemId.NinjaSuit, name = "Ninja Suit", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Petal, 2, Mat.Obsidian, 2), time = 1.8f, desc = "Super fast and jumpy.", color = new Color(0.45f, 0.3f, 0.65f), icon = "armor_ninja_suit" },
        new Recipe { id = ItemId.TankArmour, name = "Tank Armour", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Metal, 3, Mat.IronOre, 2, Mat.Obsidian, 2), time = 1.9f, desc = "Walking fortress. VERY slow.", color = new Color(0.75f, 0.78f, 0.82f), icon = "armor_tank_armour" },
        new Recipe { id = ItemId.MedicSuit, name = "Medic Suit", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Herb, 2, Mat.Berry, 2, Mat.Plastic, 1), time = 1.9f, desc = "Heals you 3 health every second.", color = new Color(0.4f, 0.95f, 0.3f), icon = "armor_medic_suit" },
        new Recipe { id = ItemId.StormSuit, name = "Storm Suit", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Battery, 2, Mat.CopperWire, 2, Mat.Glass, 1), time = 1.9f, desc = "Shock-proof, and your guns cool fast.", color = new Color(0.4f, 0.6f, 1f), icon = "armor_storm_suit" },
        new Recipe { id = ItemId.RoyalRobe, name = "Royal Robe", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.GoldOre, 2, Mat.Petal, 2), time = 1.8f, desc = "Takes 20% less damage.", color = new Color(1f, 0.82f, 0.25f), icon = "armor_royal_robe" },

        // 30 potions and bombs (Brewing Stand)
        new Recipe { id = ItemId.BerrySmoothie, name = "Berry Smoothie", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Berry, 2, Mat.Petal, 1), time = 8f, desc = "Heals 50 health right away.", color = new Color(0.95f, 0.25f, 0.45f), icon = "potion_berry_smoothie" },
        new Recipe { id = ItemId.HerbTea, name = "Herb Tea", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Herb, 2, Mat.Moss, 1), time = 8f, desc = "Heals 60 over 6 seconds and cures effects.", color = new Color(0.4f, 0.95f, 0.3f), icon = "potion_herb_tea" },
        new Recipe { id = ItemId.SlimeShake, name = "Slime Shake", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Slime, 2, Mat.Berry, 1), time = 8f, desc = "Bouncy! Jump higher for 20 seconds.", color = new Color(0.3f, 0.95f, 0.75f), icon = "potion_slime_shake" },
        new Recipe { id = ItemId.CrystalWater, name = "Crystal Water", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Crystal, 2, Mat.Herb, 1), time = 8f, desc = "Gives you 40 extra armour.", color = new Color(0.4f, 0.85f, 1f), icon = "potion_crystal_water" },
        new Recipe { id = ItemId.SuperShieldPotion, name = "Super Shield Potion", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Glowstone, 1, Mat.Metal, 1, Mat.Crystal, 2), time = 9f, desc = "Gives you 120 extra armour!", color = new Color(1f, 0.95f, 0.65f), icon = "potion_super_shield_potion" },
        new Recipe { id = ItemId.SpeedDemon, name = "Speed Demon", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Battery, 1, Mat.Sap, 1, Mat.Chemical, 1), time = 9f, desc = "Insanely fast... but you take 20% more damage.", color = new Color(0.4f, 0.6f, 1f), icon = "potion_speed_demon" },
        new Recipe { id = ItemId.TurtlePotion, name = "Turtle Potion", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Obsidian, 2, Mat.Moss, 1), time = 8f, desc = "Take 70% less damage, but crawl.", color = new Color(0.45f, 0.3f, 0.65f), icon = "potion_turtle_potion" },
        new Recipe { id = ItemId.MegaRegen, name = "Mega Regen", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Moss, 2, Mat.Glowstone, 1), time = 8f, desc = "Heal 8 health every second for 10 seconds.", color = new Color(0.45f, 0.7f, 0.3f), icon = "potion_mega_regen" },
        new Recipe { id = ItemId.BerserkerBrew, name = "Berserker Brew", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Meteorite, 1, Mat.Berry, 2), time = 8f, desc = "Way more damage, a bit faster, but fragile.", color = new Color(1f, 0.4f, 0.2f), icon = "potion_berserker_brew" },
        new Recipe { id = ItemId.FocusPotion, name = "Focus Potion", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Glass, 1, Mat.Herb, 2), time = 8f, desc = "+25% damage for 25 seconds.", color = new Color(0.75f, 0.95f, 1f), icon = "potion_focus_potion" },
        new Recipe { id = ItemId.ColdBrew, name = "Cold Brew", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Crystal, 1, Mat.Sap, 2), time = 8f, desc = "Guns barely heat up and shoot a bit faster.", color = new Color(0.4f, 0.85f, 1f), icon = "potion_cold_brew" },
        new Recipe { id = ItemId.BouncyPotion, name = "Bouncy Potion", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Slime, 1, Mat.Plastic, 1, Mat.Herb, 1), time = 9f, desc = "Big bouncy jumps, no fall damage.", color = new Color(0.3f, 0.95f, 0.75f), icon = "potion_bouncy_potion" },
        new Recipe { id = ItemId.MoonPotion, name = "Moon Potion", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Stardust, 2, Mat.Petal, 1), time = 8f, desc = "Low gravity for 20 seconds.", color = new Color(0.85f, 0.7f, 1f), icon = "potion_moon_potion" },
        new Recipe { id = ItemId.IronWill, name = "Iron Will", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.IronOre, 2, Mat.Metal, 1), time = 8f, desc = "Take 30% less damage for 20 seconds.", color = new Color(0.85f, 0.52f, 0.4f), icon = "potion_iron_will" },
        new Recipe { id = ItemId.GoldenShield, name = "Golden Shield", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.GoldOre, 2, Mat.Crystal, 1), time = 8f, desc = "Gives you 90 extra armour.", color = new Color(1f, 0.82f, 0.25f), icon = "potion_golden_shield" },
        new Recipe { id = ItemId.HazmatPotion, name = "Hazmat Potion", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Plastic, 2, Mat.Chemical, 1), time = 8f, desc = "Poison can't touch you for 30 seconds.", color = new Color(0.95f, 0.95f, 0.95f), icon = "potion_hazmat_potion" },
        new Recipe { id = ItemId.StormPotion, name = "Storm Potion", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Battery, 2, Mat.Circuit, 1), time = 8f, desc = "Shoot 40% faster (but hotter).", color = new Color(0.4f, 0.6f, 1f), icon = "potion_storm_potion" },
        new Recipe { id = ItemId.PhantomPotion, name = "Phantom Potion", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Petal, 1, Mat.Glass, 2), time = 8f, desc = "Invisible to drones and turrets for 20 seconds.", color = new Color(1f, 0.62f, 0.85f), icon = "potion_phantom_potion" },
        new Recipe { id = ItemId.LavaPotion, name = "Lava Potion", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Sulfur, 2, Mat.Meteorite, 1), time = 8f, desc = "Fireproof and +20% damage.", color = new Color(0.95f, 0.95f, 0.3f), icon = "potion_lava_potion" },
        new Recipe { id = ItemId.WoodElixir, name = "Wood Elixir", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Wood, 2, Mat.Herb, 1, Mat.Berry, 1), time = 9f, desc = "Heals 80 health over 4 seconds.", color = new Color(0.72f, 0.5f, 0.3f), icon = "potion_wood_elixir" },
        new Recipe { id = ItemId.RocketFuel, name = "Rocket Fuel", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Chemical, 1, Mat.Coal, 2, Mat.Battery, 1), time = 9f, desc = "DOUBLE speed for 5 seconds.", color = new Color(0.3f, 0.3f, 0.33f), icon = "potion_rocket_fuel" },
        new Recipe { id = ItemId.UraniumShake, name = "Uranium Shake", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Uranium, 2, Mat.Berry, 1), time = 8f, desc = "+30% damage and slow healing.", color = new Color(0.55f, 1f, 0.2f), icon = "potion_uranium_shake" },
        new Recipe { id = ItemId.FragGrenade, name = "Frag Grenade", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Metal, 2, Mat.Chemical, 1, Mat.Coal, 1), time = 9f, makes = 2, desc = "Throw it. A classic grenade.", color = new Color(0.75f, 0.78f, 0.82f), icon = "potion_frag_grenade" },
        new Recipe { id = ItemId.FreezeGrenade, name = "Freeze Grenade", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Crystal, 2, Mat.Chemical, 1, Mat.Glass, 1), time = 9f, makes = 2, desc = "Throw it. Freezes a big area.", color = new Color(0.4f, 0.85f, 1f), icon = "potion_freeze_grenade" },
        new Recipe { id = ItemId.LightningGrenade, name = "Lightning Grenade", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.CopperWire, 2, Mat.Battery, 1, Mat.Glass, 1), time = 9f, makes = 2, desc = "Throw it. A big electric blast.", color = new Color(0.95f, 0.55f, 0.3f), icon = "potion_lightning_grenade" },
        new Recipe { id = ItemId.AcidBomb, name = "Acid Bomb", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Chemical, 2, Mat.Sap, 1), time = 8f, makes = 2, desc = "Throw it. Melts labs.", color = new Color(0.8f, 0.4f, 1f), icon = "potion_acid_bomb" },
        new Recipe { id = ItemId.SporeBomb, name = "Spore Bomb", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Mushroom, 2, Mat.Chemical, 1), time = 8f, makes = 2, desc = "Throw it. A huge poison cloud.", color = new Color(1f, 0.45f, 0.4f), icon = "potion_spore_bomb" },
        new Recipe { id = ItemId.SulfurFirebomb, name = "Sulfur Firebomb", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Sulfur, 2, Mat.Chemical, 1), time = 8f, makes = 2, desc = "Throw it. Burning explosion.", color = new Color(0.95f, 0.95f, 0.3f), icon = "potion_sulfur_firebomb" },
        new Recipe { id = ItemId.StarBomb, name = "Star Bomb", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Stardust, 2, Mat.Chemical, 1), time = 8f, makes = 2, desc = "Throw it. A sparkly explosion.", color = new Color(0.85f, 0.7f, 1f), icon = "potion_star_bomb" },
        new Recipe { id = ItemId.GlueBomb, name = "Glue Bomb", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Sap, 2, Mat.Plastic, 1), time = 8f, makes = 2, desc = "Throw it. Glues everyone down.", color = new Color(1f, 0.7f, 0.2f), icon = "potion_glue_bomb" },

        // 30 enchants (Enchanting Table)
        new Recipe { id = ItemId.SharpnessIIEnchant, name = "Sharpness II", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.IronOre, 3), time = 1.5f, desc = "+40% damage.", color = new Color(0.85f, 0.52f, 0.4f), icon = "ench_sharpness_ii" },
        new Recipe { id = ItemId.RapidFireIIEnchant, name = "Rapid Fire II", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Battery, 3), time = 1.5f, desc = "Shoot 40% faster (a bit hotter).", color = new Color(0.4f, 0.6f, 1f), icon = "ench_rapid_fire_ii" },
        new Recipe { id = ItemId.IceColdEnchant, name = "Ice Cold", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Crystal, 2, Mat.Glass, 1), time = 1.5f, desc = "Your weapon barely heats up.", color = new Color(0.4f, 0.85f, 1f), icon = "ench_ice_cold" },
        new Recipe { id = ItemId.VampireIIEnchant, name = "Vampire II", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Berry, 3, Mat.Chemical, 1), time = 1.5f, desc = "Heal 25% of the damage you deal.", color = new Color(0.95f, 0.25f, 0.45f), icon = "ench_vampire_ii" },
        new Recipe { id = ItemId.BigBoomEnchant, name = "Big Boom", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Coal, 3, Mat.Chemical, 1), time = 1.5f, desc = "Bigger explosions on every hit.", color = new Color(0.3f, 0.3f, 0.33f), icon = "ench_big_boom" },
        new Recipe { id = ItemId.StormChainEnchant, name = "Storm Chain", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.CopperWire, 3, Mat.Battery, 1), time = 1.5f, desc = "Lightning jumps to 4 more targets.", color = new Color(0.5f, 0.75f, 1f), icon = "ench_storm_chain" },
        new Recipe { id = ItemId.DeepPierceEnchant, name = "Deep Pierce", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Obsidian, 3), time = 1.5f, desc = "Pierce through targets, +10% damage.", color = new Color(0.45f, 0.3f, 0.65f), icon = "ench_deep_pierce" },
        new Recipe { id = ItemId.TripleShotEnchant, name = "Triple Shot", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Stardust, 3), time = 1.5f, desc = "Fire 3 extra shots.", color = new Color(0.85f, 0.7f, 1f), icon = "ench_triple_shot" },
        new Recipe { id = ItemId.SniperScopeEnchant, name = "Sniper Scope", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Glass, 3), time = 1.5f, desc = "Double range, super accurate.", color = new Color(0.75f, 0.95f, 1f), icon = "ench_sniper_scope" },
        new Recipe { id = ItemId.HeavyHitterEnchant, name = "Heavy Hitter", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Wood, 3, Mat.Metal, 1), time = 1.5f, desc = "Massive knockback.", color = new Color(0.72f, 0.5f, 0.3f), icon = "ench_heavy_hitter" },
        new Recipe { id = ItemId.LuckyEnchant, name = "Lucky", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.GoldOre, 3), time = 1.5f, desc = "40% chance of double damage.", color = new Color(1f, 0.82f, 0.25f), icon = "ench_lucky" },
        new Recipe { id = ItemId.GlueEnchant, name = "Glue", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Sap, 2, Mat.Slime, 2), time = 1.5f, desc = "Super sticky shots.", color = new Color(0.35f, 1f, 0.45f), icon = "ench_glue" },
        new Recipe { id = ItemId.StaticEnchant, name = "Static", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Circuit, 2), time = 1.5f, desc = "Shocking shots, a bit faster.", color = new Color(0.5f, 0.75f, 1f), icon = "ench_static" },
        new Recipe { id = ItemId.PlagueEnchant, name = "Plague", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Uranium, 2, Mat.Mushroom, 1), time = 1.5f, desc = "Poison puffs on every hit.", color = new Color(0.55f, 1f, 0.2f), icon = "ench_plague" },
        new Recipe { id = ItemId.WildfireEnchant, name = "Wildfire", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Sulfur, 2, Mat.Coal, 1), time = 1.5f, desc = "Fiery puffs on every hit.", color = new Color(1f, 0.5f, 0.1f), icon = "ench_wildfire" },
        new Recipe { id = ItemId.CometEnchant, name = "Comet", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Meteorite, 3), time = 1.5f, desc = "Every hit is a mini meteor strike.", color = new Color(1f, 0.5f, 0.1f), icon = "ench_comet" },
        new Recipe { id = ItemId.DemolitionEnchant, name = "Demolition", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Metal, 3, Mat.Coal, 1), time = 1.5f, desc = "Triple damage against labs.", color = new Color(0.75f, 0.78f, 0.82f), icon = "ench_demolition" },
        new Recipe { id = ItemId.RegrowthEnchant, name = "Regrowth", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Moss, 3), time = 1.5f, desc = "Heal 12% of the damage you deal.", color = new Color(0.45f, 0.7f, 0.3f), icon = "ench_regrowth" },
        new Recipe { id = ItemId.RadiantEnchant, name = "Radiant", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Glowstone, 3), time = 1.5f, desc = "+25% damage and faster.", color = new Color(1f, 0.95f, 0.65f), icon = "ench_radiant" },
        new Recipe { id = ItemId.HealingHerbsEnchant, name = "Healing Herbs", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Herb, 3), time = 1.5f, desc = "Heal 15% of the damage you deal.", color = new Color(0.4f, 0.95f, 0.3f), icon = "ench_healing_herbs" },
        new Recipe { id = ItemId.LightweightEnchant, name = "Lightweight", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Plastic, 3), time = 1.5f, desc = "Much less heat and faster.", color = new Color(0.95f, 0.95f, 0.95f), icon = "ench_lightweight" },
        new Recipe { id = ItemId.SuperchargeEnchant, name = "Supercharge", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Battery, 2, Mat.Uranium, 1), time = 1.5f, desc = "+60% damage, overheats fast.", color = new Color(0.4f, 0.6f, 1f), icon = "ench_supercharge" },
        new Recipe { id = ItemId.PermafrostEnchant, name = "Permafrost", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Crystal, 2, Mat.Moss, 1), time = 1.5f, desc = "Ice and +20% damage.", color = new Color(0.55f, 0.92f, 1f), icon = "ench_permafrost" },
        new Recipe { id = ItemId.FungalEnchant, name = "Fungal", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Mushroom, 3), time = 1.5f, desc = "Poison and a little life steal.", color = new Color(0.55f, 1f, 0.2f), icon = "ench_fungal" },
        new Recipe { id = ItemId.DeadeyeEnchant, name = "Deadeye", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Glass, 2, Mat.GoldOre, 1), time = 1.5f, desc = "55% crit chance, pinpoint accurate.", color = new Color(0.75f, 0.95f, 1f), icon = "ench_deadeye" },
        new Recipe { id = ItemId.RubberEnchant, name = "Rubber", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Plastic, 1, Mat.Slime, 2), time = 1.5f, desc = "Bouncy knockback.", color = new Color(0.95f, 0.95f, 0.95f), icon = "ench_rubber" },
        new Recipe { id = ItemId.StarfallEnchant, name = "Starfall", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Stardust, 2, Mat.Meteorite, 1), time = 1.5f, desc = "Star explosions on every hit.", color = new Color(0.85f, 0.7f, 1f), icon = "ench_starfall" },
        new Recipe { id = ItemId.VolcanicEnchant, name = "Volcanic", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Obsidian, 2, Mat.Sulfur, 1), time = 1.5f, desc = "Fire and +30% damage.", color = new Color(1f, 0.5f, 0.1f), icon = "ench_volcanic" },
        new Recipe { id = ItemId.SuperconductorEnchant, name = "Superconductor", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.CopperWire, 2, Mat.Circuit, 1), time = 1.5f, desc = "Chain to 2 more targets and shoot faster.", color = new Color(0.95f, 0.55f, 0.3f), icon = "ench_superconductor" },
        new Recipe { id = ItemId.PetalPowerEnchant, name = "Petal Power", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Petal, 3), time = 1.5f, desc = "A little of everything.", color = new Color(1f, 0.62f, 0.85f), icon = "ench_petal_power" },

    };

    static Dictionary<ItemId, Recipe> byId;

    public static Recipe Get(ItemId id)
    {
        if (byId == null)
        {
            byId = new Dictionary<ItemId, Recipe>();
            foreach (Recipe r in All) byId[r.id] = r;
        }
        return byId[id];
    }

    public static List<Recipe> For(Station s)
    {
        var list = new List<Recipe>();
        foreach (Recipe r in All)
            if (r.station == s && r.id != ItemId.Zapper) list.Add(r);
        return list;
    }

    public static bool IsWeapon(ItemId id) => Get(id).kind == ItemKind.Weapon;

    // EXPERIMENTING: the recipe at this station whose cost is exactly this mix (null = nothing happens).
    public static Recipe Match(Station s, int[] mix)
    {
        foreach (Recipe r in For(s))
        {
            bool same = true;
            for (int m = 0; m < MatCount && same; m++) if (r.cost[m] != mix[m]) same = false;
            if (same) return r;
        }
        return null;
    }

    public static Element EnchantElement(ItemId id)
    {
        switch (id)
        {
            case ItemId.FireEnchant: return Element.Fire;
            case ItemId.IceEnchant: return Element.Ice;
            case ItemId.PoisonEnchant: return Element.Poison;
            default: return Element.None;
        }
    }

    public static Color ElementColor(Element e)
    {
        switch (e)
        {
            case Element.Fire: return new Color(1f, 0.5f, 0.1f);
            case Element.Ice: return new Color(0.55f, 0.92f, 1f);
            case Element.Poison: return new Color(0.55f, 1f, 0.2f);
            case Element.Slime: return new Color(0.35f, 1f, 0.45f);
            case Element.Shock: return new Color(0.5f, 0.75f, 1f);
            default: return Color.white;
        }
    }

    public static string ElementName(Element e) => e == Element.None ? "" : e.ToString().ToUpperInvariant();

    // Lab colours: lab 1 green, lab 2 purple, ...
    public static readonly Color[] LabColors =
    {
        new Color(0.35f, 1f, 0.3f), new Color(0.7f, 0.4f, 1f), new Color(1f, 0.55f, 0.15f), new Color(0.2f, 0.9f, 1f),
        new Color(1f, 0.35f, 0.7f), new Color(1f, 0.9f, 0.2f), new Color(1f, 0.25f, 0.25f), new Color(0.3f, 0.5f, 1f),
    };
    public static readonly string[] LabColorNames = { "GREEN", "PURPLE", "ORANGE", "CYAN", "PINK", "YELLOW", "RED", "BLUE" };

    // Icons are pictures in Assets/Resources/Icons (made for this game).
    static readonly Dictionary<string, Texture2D> icons = new Dictionary<string, Texture2D>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { icons.Clear(); byId = null; }

    public static Texture2D Icon(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (!icons.TryGetValue(name, out Texture2D t))
        {
            t = Resources.Load<Texture2D>("Icons/" + name);
            icons[name] = t;
        }
        return t;
    }

    public static Texture2D MatIcon(int m) => Icon("mat_" + MatNames[m].ToLowerInvariant().Replace(' ', '_'));
}

// The recipes you've figured out (saved on this computer, so they stay found between matches).
public static class Discoveries
{
    static HashSet<ItemId> known;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => known = null;

    static void Load()
    {
        if (known != null) return;
        known = new HashSet<ItemId>();
        foreach (Recipe r in Items.All)
            if (PlayerPrefs.GetInt("found." + r.id, 0) == 1) known.Add(r.id);
    }

    public static bool Knows(ItemId id) { Load(); return known.Contains(id); }

    // Returns true the first time.
    public static bool Learn(ItemId id)
    {
        Load();
        if (!known.Add(id)) return false;
        PlayerPrefs.SetInt("found." + id, 1);
        PlayerPrefs.Save();
        return true;
    }

    public static List<Recipe> Known(Station s)
    {
        var list = new List<Recipe>();
        foreach (Recipe r in Items.For(s)) if (Knows(r.id)) list.Add(r);
        return list;
    }

    // Wipe them all (Settings > Forget recipes).
    public static void Forget()
    {
        Load();
        foreach (Recipe r in Items.All) PlayerPrefs.DeleteKey("found." + r.id);
        PlayerPrefs.Save();
        known.Clear();
    }
}

// One thing in your hotbar.
public class HeldItem
{
    public ItemId id;
    public int count = 1;
    public Element enchant;     // the enchant's element (for colours)
    public EnchantDef enchantDef;   // the enchant on this weapon (null = none)
    public float heat;          // weapons overheat if you fire non-stop
    public bool overheated;
    public float cooldown;
    public Recipe R => Items.Get(id);
    public bool Enchanted => enchantDef != null || enchant != Element.None;
    public Color EnchantColor => enchantDef != null ? enchantDef.color : Items.ElementColor(enchant);
    public string EnchantName => enchantDef != null ? Items.Get(enchantDef.id).name.ToUpperInvariant() : Items.ElementName(enchant);
}
