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
    // batch 3: 100 guns
    HerbPistol, MetalRevolver, WoodSMG, MossRifle, CircuitMinigun, GlassLauncher,
    StardustMortar, CrystalRailgun, MushroomBeam, CoalSprayer, SulfurBurstRifle, ObsidianRepeater,
    SapPistol, PetalRevolver, GoldSMG, UraniumRifle, PlasticSniper, HerbMinigun,
    MetalLauncher, WoodMortar, MossRailgun, IronBlaster, CircuitSprayer, GlassBurstRifle,
    StardustRepeater, SulfurPistol, ObsidianRevolver, MeteorRifle, ChemicalSniper, SapMinigun,
    PetalLauncher, GoldMortar, CopperBlaster, PlasticBeam, HerbSprayer, MetalBurstRifle,
    WoodRepeater, GlassPistol, StardustRevolver, CrystalSMG, BerryRifle, MushroomShotgun,
    CoalSniper, SulfurMinigun, ObsidianLauncher, BatteryMortar, MeteorRailgun, SlimeBlaster,
    ChemicalBeam, PetalBurstRifle, GoldRepeater, MetalPistol, WoodRevolver, MossSMG,
    GlowstoneShotgun, CircuitSniper, GlassMinigun, StardustLauncher, CrystalMortar, BerryRailgun,
    MushroomBlaster, CoalBeam, SulfurSprayer, ObsidianBurstRifle, BatteryRepeater, CopperRifle,
    PlasticShotgun, HerbSniper, WoodLauncher, MossMortar, IronRailgun, GlowstoneBlaster,
    CircuitBeam, GlassSprayer, StardustBurstRifle, ObsidianPistol, BatteryRevolver, MeteorSMG,
    SlimeRifle, ChemicalShotgun, PetalMinigun, GoldLauncher, UraniumMortar, CopperRailgun,
    PlasticBlaster, HerbBeam, MetalSprayer, WoodBurstRifle, MossRepeater, StardustPistol,
    CrystalRevolver, BerrySMG, MushroomRifle, CoalShotgun, SulfurSniper, ObsidianMinigun,
    BatteryLauncher, MeteorMortar, SlimeRailgun, ChemicalBlaster,
    // batch 3: 100 armours
    HerbVest, MetalPlate, WoodSuit, MossMail, IronGuard, GlowstoneCloak,
    CircuitRobe, GlassShell, StardustHarness, CrystalBulwark, SapVest, PetalPlate,
    GoldSuit, UraniumMail, CopperGuard, PlasticCloak, MetalShell, WoodHarness,
    MossBulwark, SulfurVest, ObsidianPlate, BatterySuit, MeteorMail, SlimeGuard,
    ChemicalCloak, SapRobe, PetalShell, GoldHarness, UraniumBulwark, StardustPlate,
    CrystalSuit, BerryMail, MushroomGuard, CoalCloak, SulfurRobe, ObsidianShell,
    BatteryHarness, MeteorBulwark, MetalVest, WoodPlate, MossSuit, IronMail,
    GlowstoneGuard, CircuitCloak, GlassRobe, StardustShell, CrystalHarness, BerryBulwark,
    PetalVest, GoldPlate, CopperMail, PlasticGuard, HerbCloak, MetalRobe,
    WoodShell, MossHarness, IronBulwark, BatteryPlate, SlimeMail, ChemicalGuard,
    SapCloak, GoldShell, UraniumHarness, CopperBulwark, StardustVest, BerrySuit,
    MushroomMail, CoalGuard, SulfurCloak, ObsidianRobe, BatteryShell, MeteorHarness,
    SlimeBulwark, WoodVest, MossPlate, IronSuit, GlowstoneMail, CircuitGuard,
    GlassCloak, CrystalShell, BerryHarness, MushroomBulwark, GoldVest, UraniumPlate,
    PlasticMail, HerbGuard, MetalCloak, WoodRobe, MossShell, IronHarness,
    GlowstoneBulwark, BatteryVest, MeteorPlate, ChemicalMail, SapGuard, PetalCloak,
    UraniumShell, CopperHarness, PlasticBulwark, BerryPlate,
    // batch 3: 100 potions and bombs
    HerbTonic, MetalElixir, WoodDraught, MossBrew, IronSerum, GlowstoneShot,
    CircuitJuice, GlassMist, StardustFlask, CrystalSyrup, BerryBomb, MushroomGrenade,
    SapTonic, PetalElixir, GoldDraught, UraniumBrew, CopperSerum, PlasticShot,
    HerbJuice, MetalMist, WoodFlask, MossSyrup, IronBomb, GlowstoneGrenade,
    SulfurTonic, ObsidianElixir, BatteryDraught, MeteorBrew, SlimeSerum, ChemicalShot,
    SapJuice, PetalMist, GoldFlask, UraniumSyrup, CopperBomb, PlasticGrenade,
    GlassTonic, StardustElixir, CrystalDraught, BerryBrew, MushroomSerum, CoalShot,
    SulfurJuice, ObsidianMist, BatteryFlask, MeteorSyrup, ChemicalGrenade, MetalTonic,
    MossDraught, IronBrew, GlowstoneSerum, CircuitShot, GlassJuice, StardustMist,
    CrystalFlask, BerrySyrup, MushroomBomb, CoalGrenade, PetalTonic, GoldElixir,
    UraniumDraught, CopperBrew, PlasticSerum, HerbShot, MetalJuice, WoodMist,
    MossFlask, IronSyrup, GlowstoneBomb, CircuitGrenade, ObsidianTonic, BatteryElixir,
    MeteorDraught, SlimeBrew, ChemicalSerum, SapShot, PetalJuice, GoldMist,
    UraniumFlask, CopperSyrup, PlasticBomb, HerbGrenade, StardustTonic, CrystalElixir,
    BerryDraught, CoalSerum, SulfurShot, ObsidianJuice, BatteryMist, MeteorFlask,
    SlimeSyrup, ChemicalBomb, SapGrenade, WoodTonic, MossElixir, IronDraught,
    GlowstoneBrew, CircuitSerum, GlassShot, StardustJuice,
    // batch 3: 100 enchants
    HerbEdgeEnchant, MetalFuryEnchant, WoodChillEnchant, IronBurstEnchant, GlowstoneEchoEnchant, CircuitLanceEnchant,
    GlassScatterEnchant, StardustForceEnchant, CrystalFortuneEnchant, BerryBreakerEnchant, MushroomReachEnchant, SapEdgeEnchant,
    PetalFuryEnchant, GoldChillEnchant, UraniumLeechEnchant, CopperBurstEnchant, PlasticEchoEnchant, HerbLanceEnchant,
    MetalScatterEnchant, WoodForceEnchant, MossFortuneEnchant, IronBreakerEnchant, GlowstoneReachEnchant, SulfurEdgeEnchant,
    ObsidianFuryEnchant, BatteryChillEnchant, MeteorLeechEnchant, SlimeBurstEnchant, ChemicalEchoEnchant, SapLanceEnchant,
    PetalScatterEnchant, GoldForceEnchant, UraniumFortuneEnchant, CopperBreakerEnchant, PlasticReachEnchant, GlassEdgeEnchant,
    StardustFuryEnchant, CrystalChillEnchant, BerryLeechEnchant, MushroomBurstEnchant, CoalEchoEnchant, SulfurLanceEnchant,
    ObsidianScatterEnchant, BatteryForceEnchant, MeteorFortuneEnchant, SlimeBreakerEnchant, ChemicalReachEnchant, MetalEdgeEnchant,
    WoodFuryEnchant, MossChillEnchant, IronLeechEnchant, GlowstoneBurstEnchant, CircuitEchoEnchant, GlassLanceEnchant,
    StardustScatterEnchant, CrystalForceEnchant, BerryFortuneEnchant, MushroomBreakerEnchant, CoalReachEnchant, PetalEdgeEnchant,
    GoldFuryEnchant, UraniumChillEnchant, CopperLeechEnchant, PlasticBurstEnchant, HerbEchoEnchant, MetalLanceEnchant,
    WoodScatterEnchant, MossForceEnchant, IronFortuneEnchant, GlowstoneBreakerEnchant, CircuitReachEnchant, BatteryFuryEnchant,
    MeteorChillEnchant, SlimeLeechEnchant, ChemicalBurstEnchant, SapEchoEnchant, PetalLanceEnchant, GoldScatterEnchant,
    UraniumForceEnchant, CopperFortuneEnchant, PlasticBreakerEnchant, HerbReachEnchant, StardustEdgeEnchant, CrystalFuryEnchant,
    BerryChillEnchant, MushroomLeechEnchant, CoalBurstEnchant, SulfurEchoEnchant, ObsidianLanceEnchant, BatteryScatterEnchant,
    MeteorForceEnchant, SlimeFortuneEnchant, ChemicalBreakerEnchant, SapReachEnchant, WoodEdgeEnchant, MossFuryEnchant,
    IronChillEnchant, GlowstoneLeechEnchant, CircuitBurstEnchant, GlassEchoEnchant,
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

        // ================ BATCH 3 (made from material traits x item types) ================
        // 100 guns (Workbench)
        Gun(ItemId.HerbPistol, "Herb Pistol", "A Herb pistol. Click for every shot. Heals you as you hit.", Cost(Mat.Herb, 4, Mat.Metal, 1), 1.6f, "gun_herb_pistol"),
        Gun(ItemId.MetalRevolver, "Metal Revolver", "A heavy Metal six-shooter.", Cost(Mat.Metal, 4, Mat.Wood, 1), 1.6f, "gun_metal_revolver"),
        Gun(ItemId.WoodSMG, "Wood SMG", "A fast-firing Wood SMG. Hold to fire.", Cost(Mat.Wood, 3, Mat.Metal, 1, Mat.Plastic, 1), 1.7f, "gun_wood_smg"),
        Gun(ItemId.MossRifle, "Moss Rifle", "A Moss auto rifle. Heals you as you hit.", Cost(Mat.Moss, 3, Mat.Wood, 2, Mat.Metal, 1), 1.7f, "gun_moss_rifle"),
        Gun(ItemId.CircuitMinigun, "Circuit Minigun", "A spinning Circuit minigun. Shocks (jolts) people.", Cost(Mat.Circuit, 3, Mat.Metal, 2, Mat.Battery, 1), 1.7f, "gun_circuit_minigun"),
        Gun(ItemId.GlassLauncher, "Glass Launcher", "Fires exploding Glass rockets. Can crit.", Cost(Mat.Glass, 3, Mat.Metal, 2, Mat.Chemical, 1), 1.7f, "gun_glass_launcher"),
        Gun(ItemId.StardustMortar, "Stardust Mortar", "Lobs Stardust shells high over walls.", Cost(Mat.Stardust, 3, Mat.Metal, 2, Mat.Coal, 1), 1.7f, "gun_stardust_mortar"),
        Gun(ItemId.CrystalRailgun, "Crystal Railgun", "A Crystal railgun. Goes through everything. Slows and freezes. Pierces targets.", Cost(Mat.Crystal, 3, Mat.Battery, 2, Mat.Metal, 1), 1.7f, "gun_crystal_railgun"),
        Gun(ItemId.MushroomBeam, "Mushroom Beam", "A steady Mushroom beam. Hold to fire. Poisons.", Cost(Mat.Mushroom, 3, Mat.Battery, 1, Mat.Glass, 1), 1.7f, "gun_mushroom_beam"),
        Gun(ItemId.CoalSprayer, "Coal Sprayer", "Sprays Coal at close range. Sets things on fire.", Cost(Mat.Coal, 3, Mat.Metal, 1, Mat.Chemical, 1), 1.7f, "gun_coal_sprayer"),
        Gun(ItemId.SulfurBurstRifle, "Sulfur Burst Rifle", "Fires three Sulfur shots per click. Sets things on fire.", Cost(Mat.Sulfur, 3, Mat.Metal, 1, Mat.Battery, 1), 1.7f, "gun_sulfur_burst_rifle"),
        Gun(ItemId.ObsidianRepeater, "Obsidian Repeater", "Rapid Obsidian pellets.", Cost(Mat.Obsidian, 3, Mat.Wood, 1, Mat.Battery, 1), 1.7f, "gun_obsidian_repeater"),
        Gun(ItemId.SapPistol, "Sap Pistol", "A Sap pistol. Click for every shot. Slows people down.", Cost(Mat.Sap, 3, Mat.Metal, 1), 1.6f, "gun_sap_pistol"),
        Gun(ItemId.PetalRevolver, "Petal Revolver", "A heavy Petal six-shooter. Can crit.", Cost(Mat.Petal, 3, Mat.Metal, 1, Mat.Wood, 1), 1.7f, "gun_petal_revolver"),
        Gun(ItemId.GoldSMG, "Gold SMG", "A fast-firing Gold SMG. Hold to fire. Can crit.", Cost(Mat.GoldOre, 3, Mat.Metal, 1, Mat.Plastic, 1), 1.7f, "gun_gold_smg"),
        Gun(ItemId.UraniumRifle, "Uranium Rifle", "A Uranium auto rifle. Poisons.", Cost(Mat.Uranium, 3, Mat.Wood, 2, Mat.Metal, 1), 1.7f, "gun_uranium_rifle"),
        Gun(ItemId.PlasticSniper, "Plastic Sniper", "A long-range Plastic sniper rifle. Slows people down.", Cost(Mat.Plastic, 3, Mat.Glass, 1, Mat.Metal, 1), 1.7f, "gun_plastic_sniper"),
        Gun(ItemId.HerbMinigun, "Herb Minigun", "A spinning Herb minigun. Heals you as you hit.", Cost(Mat.Herb, 3, Mat.Metal, 2, Mat.Battery, 1), 1.7f, "gun_herb_minigun"),
        Gun(ItemId.MetalLauncher, "Metal Launcher", "Fires exploding Metal rockets.", Cost(Mat.Metal, 5, Mat.Chemical, 1), 1.6f, "gun_metal_launcher"),
        Gun(ItemId.WoodMortar, "Wood Mortar", "Lobs Wood shells high over walls.", Cost(Mat.Wood, 3, Mat.Metal, 2, Mat.Coal, 1), 1.7f, "gun_wood_mortar"),
        Gun(ItemId.MossRailgun, "Moss Railgun", "A Moss railgun. Goes through everything. Heals you as you hit. Pierces targets.", Cost(Mat.Moss, 3, Mat.Battery, 2, Mat.Metal, 1), 1.7f, "gun_moss_railgun"),
        Gun(ItemId.IronBlaster, "Iron Blaster", "Fires glowing Iron bolts.", Cost(Mat.IronOre, 3, Mat.Battery, 1, Mat.Crystal, 1), 1.7f, "gun_iron_blaster"),
        Gun(ItemId.CircuitSprayer, "Circuit Sprayer", "Sprays Circuit at close range. Shocks (jolts) people.", Cost(Mat.Circuit, 3, Mat.Metal, 1, Mat.Chemical, 1), 1.7f, "gun_circuit_sprayer"),
        Gun(ItemId.GlassBurstRifle, "Glass Burst Rifle", "Fires three Glass shots per click. Can crit.", Cost(Mat.Glass, 4, Mat.Metal, 1, Mat.Battery, 1), 1.7f, "gun_glass_burst_rifle"),
        Gun(ItemId.StardustRepeater, "Stardust Repeater", "Rapid Stardust pellets.", Cost(Mat.Stardust, 3, Mat.Wood, 1, Mat.Battery, 1), 1.7f, "gun_stardust_repeater"),
        Gun(ItemId.SulfurPistol, "Sulfur Pistol", "A Sulfur pistol. Click for every shot. Sets things on fire.", Cost(Mat.Sulfur, 5, Mat.Metal, 1), 1.6f, "gun_sulfur_pistol"),
        Gun(ItemId.ObsidianRevolver, "Obsidian Revolver", "A heavy Obsidian six-shooter. Pierces targets.", Cost(Mat.Obsidian, 3, Mat.Metal, 1, Mat.Wood, 1), 1.7f, "gun_obsidian_revolver"),
        Gun(ItemId.MeteorRifle, "Meteor Rifle", "A Meteor auto rifle. Sets things on fire. Explodes on hit.", Cost(Mat.Meteorite, 3, Mat.Wood, 2, Mat.Metal, 1), 1.7f, "gun_meteor_rifle"),
        Gun(ItemId.ChemicalSniper, "Chemical Sniper", "A long-range Chemical sniper rifle. Poisons.", Cost(Mat.Chemical, 3, Mat.Glass, 1, Mat.Metal, 1), 1.7f, "gun_chemical_sniper"),
        Gun(ItemId.SapMinigun, "Sap Minigun", "A spinning Sap minigun. Slows people down.", Cost(Mat.Sap, 3, Mat.Metal, 2, Mat.Battery, 1), 1.7f, "gun_sap_minigun"),
        Gun(ItemId.PetalLauncher, "Petal Launcher", "Fires exploding Petal rockets. Can crit.", Cost(Mat.Petal, 3, Mat.Metal, 2, Mat.Chemical, 1), 1.7f, "gun_petal_launcher"),
        Gun(ItemId.GoldMortar, "Gold Mortar", "Lobs Gold shells high over walls. Can crit.", Cost(Mat.GoldOre, 3, Mat.Metal, 2, Mat.Coal, 1), 1.7f, "gun_gold_mortar"),
        Gun(ItemId.CopperBlaster, "Copper Blaster", "Fires glowing Copper bolts. Shocks (jolts) people.", Cost(Mat.CopperWire, 3, Mat.Battery, 1, Mat.Crystal, 1), 1.7f, "gun_copper_blaster"),
        Gun(ItemId.PlasticBeam, "Plastic Beam", "A steady Plastic beam. Hold to fire. Slows people down.", Cost(Mat.Plastic, 3, Mat.Battery, 1, Mat.Glass, 1), 1.7f, "gun_plastic_beam"),
        Gun(ItemId.HerbSprayer, "Herb Sprayer", "Sprays Herb at close range. Heals you as you hit.", Cost(Mat.Herb, 3, Mat.Metal, 1, Mat.Chemical, 1), 1.7f, "gun_herb_sprayer"),
        Gun(ItemId.MetalBurstRifle, "Metal Burst Rifle", "Fires three Metal shots per click.", Cost(Mat.Metal, 4, Mat.Battery, 1), 1.6f, "gun_metal_burst_rifle"),
        Gun(ItemId.WoodRepeater, "Wood Repeater", "Rapid Wood pellets.", Cost(Mat.Wood, 4, Mat.Battery, 1), 1.6f, "gun_wood_repeater"),
        Gun(ItemId.GlassPistol, "Glass Pistol", "A Glass pistol. Click for every shot. Can crit.", Cost(Mat.Glass, 5, Mat.Metal, 1), 1.6f, "gun_glass_pistol"),
        Gun(ItemId.StardustRevolver, "Stardust Revolver", "A heavy Stardust six-shooter.", Cost(Mat.Stardust, 3, Mat.Metal, 1, Mat.Wood, 1), 1.7f, "gun_stardust_revolver"),
        Gun(ItemId.CrystalSMG, "Crystal SMG", "A fast-firing Crystal SMG. Hold to fire. Slows and freezes.", Cost(Mat.Crystal, 3, Mat.Metal, 1, Mat.Plastic, 1), 1.7f, "gun_crystal_smg"),
        Gun(ItemId.BerryRifle, "Berry Rifle", "A Berry auto rifle. Heals you as you hit.", Cost(Mat.Berry, 3, Mat.Wood, 2, Mat.Metal, 1), 1.7f, "gun_berry_rifle"),
        Gun(ItemId.MushroomShotgun, "Mushroom Shotgun", "A Mushroom shotgun. Deadly up close. Poisons.", Cost(Mat.Mushroom, 4, Mat.Metal, 2), 1.6f, "gun_mushroom_shotgun"),
        Gun(ItemId.CoalSniper, "Coal Sniper", "A long-range Coal sniper rifle. Sets things on fire.", Cost(Mat.Coal, 3, Mat.Glass, 1, Mat.Metal, 1), 1.7f, "gun_coal_sniper"),
        Gun(ItemId.SulfurMinigun, "Sulfur Minigun", "A spinning Sulfur minigun. Sets things on fire.", Cost(Mat.Sulfur, 3, Mat.Metal, 2, Mat.Battery, 1), 1.7f, "gun_sulfur_minigun"),
        Gun(ItemId.ObsidianLauncher, "Obsidian Launcher", "Fires exploding Obsidian rockets.", Cost(Mat.Obsidian, 3, Mat.Metal, 2, Mat.Chemical, 1), 1.7f, "gun_obsidian_launcher"),
        Gun(ItemId.BatteryMortar, "Battery Mortar", "Lobs Battery shells high over walls. Shocks (jolts) people.", Cost(Mat.Battery, 3, Mat.Metal, 2, Mat.Coal, 1), 1.7f, "gun_battery_mortar"),
        Gun(ItemId.MeteorRailgun, "Meteor Railgun", "A Meteor railgun. Goes through everything. Sets things on fire. Pierces targets. Explodes on hit.", Cost(Mat.Meteorite, 3, Mat.Battery, 2, Mat.Metal, 1), 1.7f, "gun_meteor_railgun"),
        Gun(ItemId.SlimeBlaster, "Slime Blaster", "Fires glowing Slime bolts. Slows people down. Leaves slime puddles.", Cost(Mat.Slime, 3, Mat.Battery, 1, Mat.Crystal, 1), 1.7f, "gun_slime_blaster"),
        Gun(ItemId.ChemicalBeam, "Chemical Beam", "A steady Chemical beam. Hold to fire. Poisons.", Cost(Mat.Chemical, 3, Mat.Battery, 1, Mat.Glass, 1), 1.7f, "gun_chemical_beam"),
        Gun(ItemId.PetalBurstRifle, "Petal Burst Rifle", "Fires three Petal shots per click. Can crit.", Cost(Mat.Petal, 3, Mat.Metal, 1, Mat.Battery, 1), 1.7f, "gun_petal_burst_rifle"),
        Gun(ItemId.GoldRepeater, "Gold Repeater", "Rapid Gold pellets. Can crit.", Cost(Mat.GoldOre, 3, Mat.Wood, 1, Mat.Battery, 1), 1.7f, "gun_gold_repeater"),
        Gun(ItemId.MetalPistol, "Metal Pistol", "A Metal pistol. Click for every shot.", Cost(Mat.Metal, 4), 1.5f, "gun_metal_pistol"),
        Gun(ItemId.WoodRevolver, "Wood Revolver", "A heavy Wood six-shooter.", Cost(Mat.Wood, 5, Mat.Metal, 1), 1.6f, "gun_wood_revolver"),
        Gun(ItemId.MossSMG, "Moss SMG", "A fast-firing Moss SMG. Hold to fire. Heals you as you hit.", Cost(Mat.Moss, 3, Mat.Metal, 1, Mat.Plastic, 1), 1.7f, "gun_moss_smg"),
        Gun(ItemId.GlowstoneShotgun, "Glowstone Shotgun", "A Glowstone shotgun. Deadly up close.", Cost(Mat.Glowstone, 3, Mat.Metal, 2), 1.6f, "gun_glowstone_shotgun"),
        Gun(ItemId.CircuitSniper, "Circuit Sniper", "A long-range Circuit sniper rifle. Shocks (jolts) people.", Cost(Mat.Circuit, 3, Mat.Glass, 1, Mat.Metal, 1), 1.7f, "gun_circuit_sniper"),
        Gun(ItemId.GlassMinigun, "Glass Minigun", "A spinning Glass minigun. Can crit.", Cost(Mat.Glass, 3, Mat.Metal, 2, Mat.Battery, 1), 1.7f, "gun_glass_minigun"),
        Gun(ItemId.StardustLauncher, "Stardust Launcher", "Fires exploding Stardust rockets.", Cost(Mat.Stardust, 3, Mat.Metal, 2, Mat.Chemical, 1), 1.7f, "gun_stardust_launcher"),
        Gun(ItemId.CrystalMortar, "Crystal Mortar", "Lobs Crystal shells high over walls. Slows and freezes.", Cost(Mat.Crystal, 3, Mat.Metal, 2, Mat.Coal, 1), 1.7f, "gun_crystal_mortar"),
        Gun(ItemId.BerryRailgun, "Berry Railgun", "A Berry railgun. Goes through everything. Heals you as you hit. Pierces targets.", Cost(Mat.Berry, 3, Mat.Battery, 2, Mat.Metal, 1), 1.7f, "gun_berry_railgun"),
        Gun(ItemId.MushroomBlaster, "Mushroom Blaster", "Fires glowing Mushroom bolts. Poisons.", Cost(Mat.Mushroom, 3, Mat.Battery, 1, Mat.Crystal, 1), 1.7f, "gun_mushroom_blaster"),
        Gun(ItemId.CoalBeam, "Coal Beam", "A steady Coal beam. Hold to fire. Sets things on fire.", Cost(Mat.Coal, 3, Mat.Battery, 1, Mat.Glass, 1), 1.7f, "gun_coal_beam"),
        Gun(ItemId.SulfurSprayer, "Sulfur Sprayer", "Sprays Sulfur at close range. Sets things on fire.", Cost(Mat.Sulfur, 3, Mat.Metal, 1, Mat.Chemical, 1), 1.7f, "gun_sulfur_sprayer"),
        Gun(ItemId.ObsidianBurstRifle, "Obsidian Burst Rifle", "Fires three Obsidian shots per click. Pierces targets.", Cost(Mat.Obsidian, 3, Mat.Metal, 1, Mat.Battery, 1), 1.7f, "gun_obsidian_burst_rifle"),
        Gun(ItemId.BatteryRepeater, "Battery Repeater", "Rapid Battery pellets. Shocks (jolts) people.", Cost(Mat.Battery, 4, Mat.Wood, 1), 1.6f, "gun_battery_repeater"),
        Gun(ItemId.CopperRifle, "Copper Rifle", "A Copper auto rifle. Shocks (jolts) people. Lightning jumps to more targets.", Cost(Mat.CopperWire, 3, Mat.Wood, 2, Mat.Metal, 1), 1.7f, "gun_copper_rifle"),
        Gun(ItemId.PlasticShotgun, "Plastic Shotgun", "A Plastic shotgun. Deadly up close. Slows people down.", Cost(Mat.Plastic, 3, Mat.Metal, 2), 1.6f, "gun_plastic_shotgun"),
        Gun(ItemId.HerbSniper, "Herb Sniper", "A long-range Herb sniper rifle. Heals you as you hit.", Cost(Mat.Herb, 3, Mat.Glass, 1, Mat.Metal, 1), 1.7f, "gun_herb_sniper"),
        Gun(ItemId.WoodLauncher, "Wood Launcher", "Fires exploding Wood rockets.", Cost(Mat.Wood, 3, Mat.Metal, 2, Mat.Chemical, 1), 1.7f, "gun_wood_launcher"),
        Gun(ItemId.MossMortar, "Moss Mortar", "Lobs Moss shells high over walls. Heals you as you hit.", Cost(Mat.Moss, 3, Mat.Metal, 2, Mat.Coal, 1), 1.7f, "gun_moss_mortar"),
        Gun(ItemId.IronRailgun, "Iron Railgun", "A Iron railgun. Goes through everything. Pierces targets.", Cost(Mat.IronOre, 3, Mat.Battery, 2, Mat.Metal, 1), 1.7f, "gun_iron_railgun"),
        Gun(ItemId.GlowstoneBlaster, "Glowstone Blaster", "Fires glowing Glowstone bolts.", Cost(Mat.Glowstone, 3, Mat.Battery, 1, Mat.Crystal, 1), 1.7f, "gun_glowstone_blaster"),
        Gun(ItemId.CircuitBeam, "Circuit Beam", "A steady Circuit beam. Hold to fire. Shocks (jolts) people.", Cost(Mat.Circuit, 3, Mat.Battery, 1, Mat.Glass, 1), 1.7f, "gun_circuit_beam"),
        Gun(ItemId.GlassSprayer, "Glass Sprayer", "Sprays Glass at close range. Can crit.", Cost(Mat.Glass, 3, Mat.Metal, 1, Mat.Chemical, 1), 1.7f, "gun_glass_sprayer"),
        Gun(ItemId.StardustBurstRifle, "Stardust Burst Rifle", "Fires three Stardust shots per click.", Cost(Mat.Stardust, 3, Mat.Metal, 1, Mat.Battery, 1), 1.7f, "gun_stardust_burst_rifle"),
        Gun(ItemId.ObsidianPistol, "Obsidian Pistol", "A Obsidian pistol. Click for every shot. Pierces targets.", Cost(Mat.Obsidian, 4, Mat.Metal, 1), 1.6f, "gun_obsidian_pistol"),
        Gun(ItemId.BatteryRevolver, "Battery Revolver", "A heavy Battery six-shooter. Shocks (jolts) people.", Cost(Mat.Battery, 3, Mat.Metal, 1, Mat.Wood, 1), 1.7f, "gun_battery_revolver"),
        Gun(ItemId.MeteorSMG, "Meteor SMG", "A fast-firing Meteor SMG. Hold to fire. Sets things on fire. Explodes on hit.", Cost(Mat.Meteorite, 3, Mat.Metal, 1, Mat.Plastic, 1), 1.7f, "gun_meteor_smg"),
        Gun(ItemId.SlimeRifle, "Slime Rifle", "A Slime auto rifle. Slows people down.", Cost(Mat.Slime, 3, Mat.Wood, 2, Mat.Metal, 1), 1.7f, "gun_slime_rifle"),
        Gun(ItemId.ChemicalShotgun, "Chemical Shotgun", "A Chemical shotgun. Deadly up close. Poisons.", Cost(Mat.Chemical, 4, Mat.Metal, 2), 1.6f, "gun_chemical_shotgun"),
        Gun(ItemId.PetalMinigun, "Petal Minigun", "A spinning Petal minigun. Can crit.", Cost(Mat.Petal, 3, Mat.Metal, 2, Mat.Battery, 1), 1.7f, "gun_petal_minigun"),
        Gun(ItemId.GoldLauncher, "Gold Launcher", "Fires exploding Gold rockets. Can crit.", Cost(Mat.GoldOre, 3, Mat.Metal, 2, Mat.Chemical, 1), 1.7f, "gun_gold_launcher"),
        Gun(ItemId.UraniumMortar, "Uranium Mortar", "Lobs Uranium shells high over walls. Poisons.", Cost(Mat.Uranium, 3, Mat.Metal, 2, Mat.Coal, 1), 1.7f, "gun_uranium_mortar"),
        Gun(ItemId.CopperRailgun, "Copper Railgun", "A Copper railgun. Goes through everything. Shocks (jolts) people. Pierces targets. Lightning jumps to more targets.", Cost(Mat.CopperWire, 3, Mat.Battery, 2, Mat.Metal, 1), 1.7f, "gun_copper_railgun"),
        Gun(ItemId.PlasticBlaster, "Plastic Blaster", "Fires glowing Plastic bolts. Slows people down.", Cost(Mat.Plastic, 3, Mat.Battery, 1, Mat.Crystal, 1), 1.7f, "gun_plastic_blaster"),
        Gun(ItemId.HerbBeam, "Herb Beam", "A steady Herb beam. Hold to fire. Heals you as you hit.", Cost(Mat.Herb, 3, Mat.Battery, 1, Mat.Glass, 1), 1.7f, "gun_herb_beam"),
        Gun(ItemId.MetalSprayer, "Metal Sprayer", "Sprays Metal at close range.", Cost(Mat.Metal, 4, Mat.Chemical, 1), 1.6f, "gun_metal_sprayer"),
        Gun(ItemId.WoodBurstRifle, "Wood Burst Rifle", "Fires three Wood shots per click.", Cost(Mat.Wood, 3, Mat.Metal, 1, Mat.Battery, 1), 1.7f, "gun_wood_burst_rifle"),
        Gun(ItemId.MossRepeater, "Moss Repeater", "Rapid Moss pellets. Heals you as you hit.", Cost(Mat.Moss, 3, Mat.Wood, 1, Mat.Battery, 1), 1.7f, "gun_moss_repeater"),
        Gun(ItemId.StardustPistol, "Stardust Pistol", "A Stardust pistol. Click for every shot.", Cost(Mat.Stardust, 3, Mat.Metal, 1), 1.6f, "gun_stardust_pistol"),
        Gun(ItemId.CrystalRevolver, "Crystal Revolver", "A heavy Crystal six-shooter. Slows and freezes.", Cost(Mat.Crystal, 3, Mat.Metal, 1, Mat.Wood, 1), 1.7f, "gun_crystal_revolver"),
        Gun(ItemId.BerrySMG, "Berry SMG", "A fast-firing Berry SMG. Hold to fire. Heals you as you hit.", Cost(Mat.Berry, 3, Mat.Metal, 1, Mat.Plastic, 1), 1.7f, "gun_berry_smg"),
        Gun(ItemId.MushroomRifle, "Mushroom Rifle", "A Mushroom auto rifle. Poisons.", Cost(Mat.Mushroom, 3, Mat.Wood, 2, Mat.Metal, 1), 1.7f, "gun_mushroom_rifle"),
        Gun(ItemId.CoalShotgun, "Coal Shotgun", "A Coal shotgun. Deadly up close. Sets things on fire.", Cost(Mat.Coal, 5, Mat.Metal, 2), 1.6f, "gun_coal_shotgun"),
        Gun(ItemId.SulfurSniper, "Sulfur Sniper", "A long-range Sulfur sniper rifle. Sets things on fire.", Cost(Mat.Sulfur, 3, Mat.Glass, 1, Mat.Metal, 1), 1.7f, "gun_sulfur_sniper"),
        Gun(ItemId.ObsidianMinigun, "Obsidian Minigun", "A spinning Obsidian minigun. Pierces targets.", Cost(Mat.Obsidian, 3, Mat.Metal, 2, Mat.Battery, 1), 1.7f, "gun_obsidian_minigun"),
        Gun(ItemId.BatteryLauncher, "Battery Launcher", "Fires exploding Battery rockets. Shocks (jolts) people.", Cost(Mat.Battery, 3, Mat.Metal, 2, Mat.Chemical, 1), 1.7f, "gun_battery_launcher"),
        Gun(ItemId.MeteorMortar, "Meteor Mortar", "Lobs Meteor shells high over walls. Sets things on fire.", Cost(Mat.Meteorite, 3, Mat.Metal, 2, Mat.Coal, 1), 1.7f, "gun_meteor_mortar"),
        Gun(ItemId.SlimeRailgun, "Slime Railgun", "A Slime railgun. Goes through everything. Slows people down. Pierces targets.", Cost(Mat.Slime, 3, Mat.Battery, 2, Mat.Metal, 1), 1.7f, "gun_slime_railgun"),
        Gun(ItemId.ChemicalBlaster, "Chemical Blaster", "Fires glowing Chemical bolts. Poisons.", Cost(Mat.Chemical, 3, Mat.Battery, 1, Mat.Crystal, 1), 1.7f, "gun_chemical_blaster"),

        // 100 armours (Workbench)
        new Recipe { id = ItemId.HerbVest, name = "Herb Vest", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Herb, 3, Mat.Plastic, 1), time = 1.8f, desc = "A light Herb vest. 50 armour. Heals 1.2/s.", color = new Color(0.4f, 0.95f, 0.3f), icon = "armor_herb_vest" },
        new Recipe { id = ItemId.MetalPlate, name = "Metal Plate", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Metal, 5), time = 1.7f, desc = "Solid Metal plates. 102 armour. Slower.", color = new Color(0.75f, 0.78f, 0.82f), icon = "armor_metal_plate" },
        new Recipe { id = ItemId.WoodSuit, name = "Wood Suit", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Wood, 3, Mat.Plastic, 2), time = 1.8f, desc = "A sleek Wood suit. 45 armour. Faster.", color = new Color(0.72f, 0.5f, 0.3f), icon = "armor_wood_suit" },
        new Recipe { id = ItemId.MossMail, name = "Moss Mail", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Moss, 3, Mat.Metal, 2), time = 1.8f, desc = "Moss chain mail. 100 armour. Heals 1.2/s. Slower.", color = new Color(0.45f, 0.7f, 0.3f), icon = "armor_moss_mail" },
        new Recipe { id = ItemId.IronGuard, name = "Iron Guard", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.IronOre, 4, Mat.Metal, 2), time = 1.8f, desc = "Heavy Iron guard armour. 168 armour. Slower.", color = new Color(0.85f, 0.52f, 0.4f), icon = "armor_iron_guard" },
        new Recipe { id = ItemId.GlowstoneCloak, name = "Glowstone Cloak", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Glowstone, 3, Mat.Moss, 1, Mat.Plastic, 1), time = 1.9f, desc = "A swishy Glowstone cloak. Fast. 35 armour. Heals 1.2/s. Faster.", color = new Color(1f, 0.95f, 0.65f), icon = "armor_glowstone_cloak" },
        new Recipe { id = ItemId.CircuitRobe, name = "Circuit Robe", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Circuit, 3, Mat.Herb, 1, Mat.Plastic, 1), time = 1.9f, desc = "A comfy Circuit robe. 30 armour. Shock-proof. Heals 1/s. Faster. Guns cool faster.", color = new Color(0.25f, 0.85f, 0.45f), icon = "armor_circuit_robe" },
        new Recipe { id = ItemId.GlassShell, name = "Glass Shell", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Glass, 3, Mat.Obsidian, 1, Mat.Metal, 1), time = 1.9f, desc = "A massive Glass shell. 224 armour. Slower. Fragile: takes 15% more damage.", color = new Color(0.75f, 0.95f, 1f), icon = "armor_glass_shell" },
        new Recipe { id = ItemId.StardustHarness, name = "Stardust Harness", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Stardust, 4, Mat.Wood, 1, Mat.Metal, 1), time = 1.9f, desc = "A Stardust climbing harness. Jump higher. 55 armour. Jump higher. No fall damage.", color = new Color(0.85f, 0.7f, 1f), icon = "armor_stardust_harness" },
        new Recipe { id = ItemId.CrystalBulwark, name = "Crystal Bulwark", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Crystal, 3, Mat.Metal, 3), time = 1.8f, desc = "A walking Crystal wall. Very slow. 190 armour. Ice-proof. Slower.", color = new Color(0.4f, 0.85f, 1f), icon = "armor_crystal_bulwark" },
        new Recipe { id = ItemId.SapVest, name = "Sap Vest", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Sap, 3, Mat.Plastic, 1), time = 1.8f, desc = "A light Sap vest. 50 armour. Slime-proof.", color = new Color(1f, 0.7f, 0.2f), icon = "armor_sap_vest" },
        new Recipe { id = ItemId.PetalPlate, name = "Petal Plate", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Petal, 3, Mat.Metal, 1), time = 1.8f, desc = "Solid Petal plates. 85 armour. Faster.", color = new Color(1f, 0.62f, 0.85f), icon = "armor_petal_plate" },
        new Recipe { id = ItemId.GoldSuit, name = "Gold Suit", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.GoldOre, 3, Mat.Plastic, 2), time = 1.8f, desc = "A sleek Gold suit. 45 armour. Faster. Takes 12% less damage.", color = new Color(1f, 0.82f, 0.25f), icon = "armor_gold_suit" },
        new Recipe { id = ItemId.UraniumMail, name = "Uranium Mail", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Uranium, 4, Mat.Metal, 2), time = 1.8f, desc = "Uranium chain mail. 100 armour. Poison-proof. Slower. Less zone damage.", color = new Color(0.55f, 1f, 0.2f), icon = "armor_uranium_mail" },
        new Recipe { id = ItemId.CopperGuard, name = "Copper Guard", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.CopperWire, 3, Mat.Metal, 2, Mat.IronOre, 1), time = 1.9f, desc = "Heavy Copper guard armour. 140 armour. Shock-proof. Slower.", color = new Color(0.95f, 0.55f, 0.3f), icon = "armor_copper_guard" },
        new Recipe { id = ItemId.PlasticCloak, name = "Plastic Cloak", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Plastic, 4, Mat.Moss, 1), time = 1.8f, desc = "A swishy Plastic cloak. Fast. 35 armour. Slime-proof. Faster.", color = new Color(0.95f, 0.95f, 0.95f), icon = "armor_plastic_cloak" },
        new Recipe { id = ItemId.MetalShell, name = "Metal Shell", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Metal, 4, Mat.Obsidian, 1), time = 1.8f, desc = "A massive Metal shell. 192 armour. Slower.", color = new Color(0.75f, 0.78f, 0.82f), icon = "armor_metal_shell" },
        new Recipe { id = ItemId.WoodHarness, name = "Wood Harness", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Wood, 6, Mat.Metal, 1), time = 1.8f, desc = "A Wood climbing harness. Jump higher. 55 armour. Jump higher.", color = new Color(0.72f, 0.5f, 0.3f), icon = "armor_wood_harness" },
        new Recipe { id = ItemId.MossBulwark, name = "Moss Bulwark", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Moss, 3, Mat.Metal, 3), time = 1.8f, desc = "A walking Moss wall. Very slow. 190 armour. Heals 1.2/s. Slower.", color = new Color(0.45f, 0.7f, 0.3f), icon = "armor_moss_bulwark" },
        new Recipe { id = ItemId.SulfurVest, name = "Sulfur Vest", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Sulfur, 4, Mat.Plastic, 1), time = 1.8f, desc = "A light Sulfur vest. 50 armour. Fire-proof. Guns cool faster.", color = new Color(0.95f, 0.95f, 0.3f), icon = "armor_sulfur_vest" },
        new Recipe { id = ItemId.ObsidianPlate, name = "Obsidian Plate", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Obsidian, 5, Mat.Metal, 1), time = 1.8f, desc = "Solid Obsidian plates. 102 armour. Slower.", color = new Color(0.45f, 0.3f, 0.65f), icon = "armor_obsidian_plate" },
        new Recipe { id = ItemId.BatterySuit, name = "Battery Suit", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Battery, 3, Mat.Plastic, 2), time = 1.8f, desc = "A sleek Battery suit. 45 armour. Shock-proof. Faster. Guns cool faster.", color = new Color(0.4f, 0.6f, 1f), icon = "armor_battery_suit" },
        new Recipe { id = ItemId.MeteorMail, name = "Meteor Mail", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Meteorite, 4, Mat.Metal, 2), time = 1.8f, desc = "Meteor chain mail. 110 armour. Fire-proof. Slower.", color = new Color(1f, 0.4f, 0.2f), icon = "armor_meteor_mail" },
        new Recipe { id = ItemId.SlimeGuard, name = "Slime Guard", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Slime, 3, Mat.Metal, 2, Mat.IronOre, 1), time = 1.9f, desc = "Heavy Slime guard armour. 140 armour. Slime-proof. Slower. Jump higher. No fall damage.", color = new Color(0.3f, 0.95f, 0.75f), icon = "armor_slime_guard" },
        new Recipe { id = ItemId.ChemicalCloak, name = "Chemical Cloak", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Chemical, 3, Mat.Moss, 1, Mat.Plastic, 1), time = 1.9f, desc = "A swishy Chemical cloak. Fast. 35 armour. Poison-proof. Faster. Less zone damage.", color = new Color(0.8f, 0.4f, 1f), icon = "armor_chemical_cloak" },
        new Recipe { id = ItemId.SapRobe, name = "Sap Robe", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Sap, 3, Mat.Herb, 1, Mat.Plastic, 1), time = 1.9f, desc = "A comfy Sap robe. 30 armour. Slime-proof. Heals 1/s.", color = new Color(1f, 0.7f, 0.2f), icon = "armor_sap_robe" },
        new Recipe { id = ItemId.PetalShell, name = "Petal Shell", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Petal, 3, Mat.Obsidian, 1, Mat.Metal, 1), time = 1.9f, desc = "A massive Petal shell. 160 armour. Slower.", color = new Color(1f, 0.62f, 0.85f), icon = "armor_petal_shell" },
        new Recipe { id = ItemId.GoldHarness, name = "Gold Harness", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.GoldOre, 3, Mat.Wood, 1, Mat.Metal, 1), time = 1.9f, desc = "A Gold climbing harness. Jump higher. 55 armour. Jump higher. Takes 12% less damage.", color = new Color(1f, 0.82f, 0.25f), icon = "armor_gold_harness" },
        new Recipe { id = ItemId.UraniumBulwark, name = "Uranium Bulwark", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Uranium, 4, Mat.Metal, 3), time = 1.8f, desc = "A walking Uranium wall. Very slow. 190 armour. Poison-proof. Slower. Less zone damage.", color = new Color(0.55f, 1f, 0.2f), icon = "armor_uranium_bulwark" },
        new Recipe { id = ItemId.StardustPlate, name = "Stardust Plate", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Stardust, 5, Mat.Metal, 1), time = 1.8f, desc = "Solid Stardust plates. 85 armour. Slower. Jump higher. No fall damage.", color = new Color(0.85f, 0.7f, 1f), icon = "armor_stardust_plate" },
        new Recipe { id = ItemId.CrystalSuit, name = "Crystal Suit", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Crystal, 3, Mat.Plastic, 2), time = 1.8f, desc = "A sleek Crystal suit. 45 armour. Ice-proof. Faster.", color = new Color(0.4f, 0.85f, 1f), icon = "armor_crystal_suit" },
        new Recipe { id = ItemId.BerryMail, name = "Berry Mail", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Berry, 4, Mat.Metal, 2), time = 1.8f, desc = "Berry chain mail. 100 armour. Heals 1.2/s. Slower.", color = new Color(0.95f, 0.25f, 0.45f), icon = "armor_berry_mail" },
        new Recipe { id = ItemId.MushroomGuard, name = "Mushroom Guard", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Mushroom, 3, Mat.Metal, 2, Mat.IronOre, 1), time = 1.9f, desc = "Heavy Mushroom guard armour. 140 armour. Poison-proof. Slower.", color = new Color(1f, 0.45f, 0.4f), icon = "armor_mushroom_guard" },
        new Recipe { id = ItemId.CoalCloak, name = "Coal Cloak", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Coal, 3, Mat.Moss, 1, Mat.Plastic, 1), time = 1.9f, desc = "A swishy Coal cloak. Fast. 35 armour. Fire-proof. Faster.", color = new Color(0.3f, 0.3f, 0.33f), icon = "armor_coal_cloak" },
        new Recipe { id = ItemId.SulfurRobe, name = "Sulfur Robe", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Sulfur, 3, Mat.Herb, 1, Mat.Plastic, 1), time = 1.9f, desc = "A comfy Sulfur robe. 30 armour. Fire-proof. Heals 1/s. Guns cool faster.", color = new Color(0.95f, 0.95f, 0.3f), icon = "armor_sulfur_robe" },
        new Recipe { id = ItemId.ObsidianShell, name = "Obsidian Shell", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Obsidian, 6, Mat.Metal, 1), time = 1.8f, desc = "A massive Obsidian shell. 192 armour. Slower.", color = new Color(0.45f, 0.3f, 0.65f), icon = "armor_obsidian_shell" },
        new Recipe { id = ItemId.BatteryHarness, name = "Battery Harness", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Battery, 4, Mat.Wood, 1, Mat.Metal, 1), time = 1.9f, desc = "A Battery climbing harness. Jump higher. 55 armour. Shock-proof. Jump higher. Guns cool faster.", color = new Color(0.4f, 0.6f, 1f), icon = "armor_battery_harness" },
        new Recipe { id = ItemId.MeteorBulwark, name = "Meteor Bulwark", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Meteorite, 3, Mat.Metal, 3), time = 1.8f, desc = "A walking Meteor wall. Very slow. 209 armour. Fire-proof. Slower.", color = new Color(1f, 0.4f, 0.2f), icon = "armor_meteor_bulwark" },
        new Recipe { id = ItemId.MetalVest, name = "Metal Vest", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Metal, 3, Mat.Plastic, 1), time = 1.8f, desc = "A light Metal vest. 60 armour.", color = new Color(0.75f, 0.78f, 0.82f), icon = "armor_metal_vest" },
        new Recipe { id = ItemId.WoodPlate, name = "Wood Plate", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Wood, 7, Mat.Metal, 1), time = 1.8f, desc = "Solid Wood plates. 85 armour. Slower.", color = new Color(0.72f, 0.5f, 0.3f), icon = "armor_wood_plate" },
        new Recipe { id = ItemId.MossSuit, name = "Moss Suit", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Moss, 3, Mat.Plastic, 2), time = 1.8f, desc = "A sleek Moss suit. 45 armour. Heals 1.2/s. Faster.", color = new Color(0.45f, 0.7f, 0.3f), icon = "armor_moss_suit" },
        new Recipe { id = ItemId.IronMail, name = "Iron Mail", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.IronOre, 3, Mat.Metal, 2), time = 1.8f, desc = "Iron chain mail. 120 armour. Slower.", color = new Color(0.85f, 0.52f, 0.4f), icon = "armor_iron_mail" },
        new Recipe { id = ItemId.GlowstoneGuard, name = "Glowstone Guard", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Glowstone, 3, Mat.Metal, 2, Mat.IronOre, 1), time = 1.9f, desc = "Heavy Glowstone guard armour. 140 armour. Heals 1.2/s. Slower.", color = new Color(1f, 0.95f, 0.65f), icon = "armor_glowstone_guard" },
        new Recipe { id = ItemId.CircuitCloak, name = "Circuit Cloak", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Circuit, 3, Mat.Moss, 1, Mat.Plastic, 1), time = 1.9f, desc = "A swishy Circuit cloak. Fast. 35 armour. Shock-proof. Faster. Guns cool faster.", color = new Color(0.25f, 0.85f, 0.45f), icon = "armor_circuit_cloak" },
        new Recipe { id = ItemId.GlassRobe, name = "Glass Robe", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Glass, 3, Mat.Herb, 1, Mat.Plastic, 1), time = 1.9f, desc = "A comfy Glass robe. 42 armour. Heals 1/s. Fragile: takes 15% more damage.", color = new Color(0.75f, 0.95f, 1f), icon = "armor_glass_robe" },
        new Recipe { id = ItemId.StardustShell, name = "Stardust Shell", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Stardust, 3, Mat.Obsidian, 1, Mat.Metal, 1), time = 1.9f, desc = "A massive Stardust shell. 160 armour. Slower. Jump higher. No fall damage.", color = new Color(0.85f, 0.7f, 1f), icon = "armor_stardust_shell" },
        new Recipe { id = ItemId.CrystalHarness, name = "Crystal Harness", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Crystal, 4, Mat.Wood, 1, Mat.Metal, 1), time = 1.9f, desc = "A Crystal climbing harness. Jump higher. 55 armour. Ice-proof. Jump higher.", color = new Color(0.4f, 0.85f, 1f), icon = "armor_crystal_harness" },
        new Recipe { id = ItemId.BerryBulwark, name = "Berry Bulwark", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Berry, 3, Mat.Metal, 3), time = 1.8f, desc = "A walking Berry wall. Very slow. 190 armour. Heals 1.2/s. Slower.", color = new Color(0.95f, 0.25f, 0.45f), icon = "armor_berry_bulwark" },
        new Recipe { id = ItemId.PetalVest, name = "Petal Vest", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Petal, 4, Mat.Plastic, 1), time = 1.8f, desc = "A light Petal vest. 50 armour. Faster.", color = new Color(1f, 0.62f, 0.85f), icon = "armor_petal_vest" },
        new Recipe { id = ItemId.GoldPlate, name = "Gold Plate", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.GoldOre, 3, Mat.Metal, 1), time = 1.8f, desc = "Solid Gold plates. 85 armour. Slower. Takes 12% less damage.", color = new Color(1f, 0.82f, 0.25f), icon = "armor_gold_plate" },
        new Recipe { id = ItemId.CopperMail, name = "Copper Mail", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.CopperWire, 4, Mat.Metal, 2), time = 1.8f, desc = "Copper chain mail. 100 armour. Shock-proof. Slower.", color = new Color(0.95f, 0.55f, 0.3f), icon = "armor_copper_mail" },
        new Recipe { id = ItemId.PlasticGuard, name = "Plastic Guard", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Plastic, 3, Mat.Metal, 2, Mat.IronOre, 1), time = 1.9f, desc = "Heavy Plastic guard armour. 140 armour. Slime-proof. Slower.", color = new Color(0.95f, 0.95f, 0.95f), icon = "armor_plastic_guard" },
        new Recipe { id = ItemId.HerbCloak, name = "Herb Cloak", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Herb, 3, Mat.Moss, 1, Mat.Plastic, 1), time = 1.9f, desc = "A swishy Herb cloak. Fast. 35 armour. Heals 1.2/s. Faster.", color = new Color(0.4f, 0.95f, 0.3f), icon = "armor_herb_cloak" },
        new Recipe { id = ItemId.MetalRobe, name = "Metal Robe", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Metal, 3, Mat.Herb, 1, Mat.Plastic, 1), time = 1.9f, desc = "A comfy Metal robe. 36 armour. Heals 1/s.", color = new Color(0.75f, 0.78f, 0.82f), icon = "armor_metal_robe" },
        new Recipe { id = ItemId.WoodShell, name = "Wood Shell", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Wood, 3, Mat.Obsidian, 1, Mat.Metal, 1), time = 1.9f, desc = "A massive Wood shell. 160 armour. Slower.", color = new Color(0.72f, 0.5f, 0.3f), icon = "armor_wood_shell" },
        new Recipe { id = ItemId.MossHarness, name = "Moss Harness", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Moss, 3, Mat.Wood, 1, Mat.Metal, 1), time = 1.9f, desc = "A Moss climbing harness. Jump higher. 55 armour. Heals 1.2/s. Jump higher.", color = new Color(0.45f, 0.7f, 0.3f), icon = "armor_moss_harness" },
        new Recipe { id = ItemId.IronBulwark, name = "Iron Bulwark", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.IronOre, 3, Mat.Metal, 3), time = 1.8f, desc = "A walking Iron wall. Very slow. 228 armour. Slower.", color = new Color(0.85f, 0.52f, 0.4f), icon = "armor_iron_bulwark" },
        new Recipe { id = ItemId.BatteryPlate, name = "Battery Plate", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Battery, 3, Mat.Metal, 1), time = 1.8f, desc = "Solid Battery plates. 85 armour. Shock-proof. Slower. Guns cool faster.", color = new Color(0.4f, 0.6f, 1f), icon = "armor_battery_plate" },
        new Recipe { id = ItemId.SlimeMail, name = "Slime Mail", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Slime, 5, Mat.Metal, 2), time = 1.8f, desc = "Slime chain mail. 100 armour. Slime-proof. Slower. Jump higher. No fall damage.", color = new Color(0.3f, 0.95f, 0.75f), icon = "armor_slime_mail" },
        new Recipe { id = ItemId.ChemicalGuard, name = "Chemical Guard", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Chemical, 3, Mat.Metal, 2, Mat.IronOre, 1), time = 1.9f, desc = "Heavy Chemical guard armour. 140 armour. Poison-proof. Slower. Less zone damage.", color = new Color(0.8f, 0.4f, 1f), icon = "armor_chemical_guard" },
        new Recipe { id = ItemId.SapCloak, name = "Sap Cloak", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Sap, 3, Mat.Moss, 1, Mat.Plastic, 1), time = 1.9f, desc = "A swishy Sap cloak. Fast. 35 armour. Slime-proof. Faster.", color = new Color(1f, 0.7f, 0.2f), icon = "armor_sap_cloak" },
        new Recipe { id = ItemId.GoldShell, name = "Gold Shell", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.GoldOre, 3, Mat.Obsidian, 1, Mat.Metal, 1), time = 1.9f, desc = "A massive Gold shell. 160 armour. Slower. Takes 12% less damage.", color = new Color(1f, 0.82f, 0.25f), icon = "armor_gold_shell" },
        new Recipe { id = ItemId.UraniumHarness, name = "Uranium Harness", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Uranium, 3, Mat.Wood, 1, Mat.Metal, 1), time = 1.9f, desc = "A Uranium climbing harness. Jump higher. 55 armour. Poison-proof. Jump higher. Less zone damage.", color = new Color(0.55f, 1f, 0.2f), icon = "armor_uranium_harness" },
        new Recipe { id = ItemId.CopperBulwark, name = "Copper Bulwark", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.CopperWire, 3, Mat.Metal, 3), time = 1.8f, desc = "A walking Copper wall. Very slow. 190 armour. Shock-proof. Slower.", color = new Color(0.95f, 0.55f, 0.3f), icon = "armor_copper_bulwark" },
        new Recipe { id = ItemId.StardustVest, name = "Stardust Vest", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Stardust, 3, Mat.Plastic, 1), time = 1.8f, desc = "A light Stardust vest. 50 armour. Jump higher. No fall damage.", color = new Color(0.85f, 0.7f, 1f), icon = "armor_stardust_vest" },
        new Recipe { id = ItemId.BerrySuit, name = "Berry Suit", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Berry, 3, Mat.Plastic, 2), time = 1.8f, desc = "A sleek Berry suit. 45 armour. Heals 1.2/s. Faster.", color = new Color(0.95f, 0.25f, 0.45f), icon = "armor_berry_suit" },
        new Recipe { id = ItemId.MushroomMail, name = "Mushroom Mail", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Mushroom, 5, Mat.Metal, 2), time = 1.8f, desc = "Mushroom chain mail. 100 armour. Poison-proof. Slower.", color = new Color(1f, 0.45f, 0.4f), icon = "armor_mushroom_mail" },
        new Recipe { id = ItemId.CoalGuard, name = "Coal Guard", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Coal, 3, Mat.Metal, 2, Mat.IronOre, 1), time = 1.9f, desc = "Heavy Coal guard armour. 140 armour. Fire-proof. Slower.", color = new Color(0.3f, 0.3f, 0.33f), icon = "armor_coal_guard" },
        new Recipe { id = ItemId.SulfurCloak, name = "Sulfur Cloak", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Sulfur, 3, Mat.Moss, 1, Mat.Plastic, 1), time = 1.9f, desc = "A swishy Sulfur cloak. Fast. 35 armour. Fire-proof. Faster. Guns cool faster.", color = new Color(0.95f, 0.95f, 0.3f), icon = "armor_sulfur_cloak" },
        new Recipe { id = ItemId.ObsidianRobe, name = "Obsidian Robe", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Obsidian, 3, Mat.Herb, 1, Mat.Plastic, 1), time = 1.9f, desc = "A comfy Obsidian robe. 36 armour. Heals 1/s.", color = new Color(0.45f, 0.3f, 0.65f), icon = "armor_obsidian_robe" },
        new Recipe { id = ItemId.BatteryShell, name = "Battery Shell", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Battery, 3, Mat.Obsidian, 1, Mat.Metal, 1), time = 1.9f, desc = "A massive Battery shell. 160 armour. Shock-proof. Slower. Guns cool faster.", color = new Color(0.4f, 0.6f, 1f), icon = "armor_battery_shell" },
        new Recipe { id = ItemId.MeteorHarness, name = "Meteor Harness", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Meteorite, 3, Mat.Wood, 1, Mat.Metal, 1), time = 1.9f, desc = "A Meteor climbing harness. Jump higher. 61 armour. Fire-proof. Jump higher.", color = new Color(1f, 0.4f, 0.2f), icon = "armor_meteor_harness" },
        new Recipe { id = ItemId.SlimeBulwark, name = "Slime Bulwark", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Slime, 3, Mat.Metal, 3), time = 1.8f, desc = "A walking Slime wall. Very slow. 190 armour. Slime-proof. Slower. Jump higher. No fall damage.", color = new Color(0.3f, 0.95f, 0.75f), icon = "armor_slime_bulwark" },
        new Recipe { id = ItemId.WoodVest, name = "Wood Vest", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Wood, 3, Mat.Plastic, 1), time = 1.8f, desc = "A light Wood vest. 50 armour.", color = new Color(0.72f, 0.5f, 0.3f), icon = "armor_wood_vest" },
        new Recipe { id = ItemId.MossPlate, name = "Moss Plate", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Moss, 3, Mat.Metal, 1), time = 1.8f, desc = "Solid Moss plates. 85 armour. Heals 1.2/s. Slower.", color = new Color(0.45f, 0.7f, 0.3f), icon = "armor_moss_plate" },
        new Recipe { id = ItemId.IronSuit, name = "Iron Suit", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.IronOre, 3, Mat.Plastic, 2), time = 1.8f, desc = "A sleek Iron suit. 54 armour. Faster.", color = new Color(0.85f, 0.52f, 0.4f), icon = "armor_iron_suit" },
        new Recipe { id = ItemId.GlowstoneMail, name = "Glowstone Mail", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Glowstone, 4, Mat.Metal, 2), time = 1.8f, desc = "Glowstone chain mail. 100 armour. Heals 1.2/s. Slower.", color = new Color(1f, 0.95f, 0.65f), icon = "armor_glowstone_mail" },
        new Recipe { id = ItemId.CircuitGuard, name = "Circuit Guard", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Circuit, 3, Mat.Metal, 2, Mat.IronOre, 1), time = 1.9f, desc = "Heavy Circuit guard armour. 140 armour. Shock-proof. Slower. Guns cool faster.", color = new Color(0.25f, 0.85f, 0.45f), icon = "armor_circuit_guard" },
        new Recipe { id = ItemId.GlassCloak, name = "Glass Cloak", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Glass, 3, Mat.Moss, 1, Mat.Plastic, 1), time = 1.9f, desc = "A swishy Glass cloak. Fast. 49 armour. Faster. Fragile: takes 15% more damage.", color = new Color(0.75f, 0.95f, 1f), icon = "armor_glass_cloak" },
        new Recipe { id = ItemId.CrystalShell, name = "Crystal Shell", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Crystal, 3, Mat.Obsidian, 1, Mat.Metal, 1), time = 1.9f, desc = "A massive Crystal shell. 160 armour. Ice-proof. Slower.", color = new Color(0.4f, 0.85f, 1f), icon = "armor_crystal_shell" },
        new Recipe { id = ItemId.BerryHarness, name = "Berry Harness", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Berry, 3, Mat.Wood, 1, Mat.Metal, 1), time = 1.9f, desc = "A Berry climbing harness. Jump higher. 55 armour. Heals 1.2/s. Jump higher.", color = new Color(0.95f, 0.25f, 0.45f), icon = "armor_berry_harness" },
        new Recipe { id = ItemId.MushroomBulwark, name = "Mushroom Bulwark", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Mushroom, 3, Mat.Metal, 3), time = 1.8f, desc = "A walking Mushroom wall. Very slow. 190 armour. Poison-proof. Slower.", color = new Color(1f, 0.45f, 0.4f), icon = "armor_mushroom_bulwark" },
        new Recipe { id = ItemId.GoldVest, name = "Gold Vest", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.GoldOre, 4, Mat.Plastic, 1), time = 1.8f, desc = "A light Gold vest. 50 armour. Takes 12% less damage.", color = new Color(1f, 0.82f, 0.25f), icon = "armor_gold_vest" },
        new Recipe { id = ItemId.UraniumPlate, name = "Uranium Plate", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Uranium, 4, Mat.Metal, 1), time = 1.8f, desc = "Solid Uranium plates. 85 armour. Poison-proof. Slower. Less zone damage.", color = new Color(0.55f, 1f, 0.2f), icon = "armor_uranium_plate" },
        new Recipe { id = ItemId.PlasticMail, name = "Plastic Mail", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Plastic, 4, Mat.Metal, 2), time = 1.8f, desc = "Plastic chain mail. 100 armour. Slime-proof. Slower.", color = new Color(0.95f, 0.95f, 0.95f), icon = "armor_plastic_mail" },
        new Recipe { id = ItemId.HerbGuard, name = "Herb Guard", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Herb, 3, Mat.Metal, 2, Mat.IronOre, 1), time = 1.9f, desc = "Heavy Herb guard armour. 140 armour. Heals 1.2/s. Slower.", color = new Color(0.4f, 0.95f, 0.3f), icon = "armor_herb_guard" },
        new Recipe { id = ItemId.MetalCloak, name = "Metal Cloak", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Metal, 3, Mat.Moss, 1, Mat.Plastic, 1), time = 1.9f, desc = "A swishy Metal cloak. Fast. 42 armour. Faster.", color = new Color(0.75f, 0.78f, 0.82f), icon = "armor_metal_cloak" },
        new Recipe { id = ItemId.WoodRobe, name = "Wood Robe", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Wood, 3, Mat.Herb, 1, Mat.Plastic, 1), time = 1.9f, desc = "A comfy Wood robe. 30 armour. Heals 1/s.", color = new Color(0.72f, 0.5f, 0.3f), icon = "armor_wood_robe" },
        new Recipe { id = ItemId.MossShell, name = "Moss Shell", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Moss, 3, Mat.Obsidian, 1, Mat.Metal, 1), time = 1.9f, desc = "A massive Moss shell. 160 armour. Heals 1.2/s. Slower.", color = new Color(0.45f, 0.7f, 0.3f), icon = "armor_moss_shell" },
        new Recipe { id = ItemId.IronHarness, name = "Iron Harness", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.IronOre, 4, Mat.Wood, 1, Mat.Metal, 1), time = 1.9f, desc = "A Iron climbing harness. Jump higher. 66 armour. Jump higher.", color = new Color(0.85f, 0.52f, 0.4f), icon = "armor_iron_harness" },
        new Recipe { id = ItemId.GlowstoneBulwark, name = "Glowstone Bulwark", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Glowstone, 3, Mat.Metal, 3), time = 1.8f, desc = "A walking Glowstone wall. Very slow. 190 armour. Heals 1.2/s. Slower.", color = new Color(1f, 0.95f, 0.65f), icon = "armor_glowstone_bulwark" },
        new Recipe { id = ItemId.BatteryVest, name = "Battery Vest", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Battery, 4, Mat.Plastic, 1), time = 1.8f, desc = "A light Battery vest. 50 armour. Shock-proof. Guns cool faster.", color = new Color(0.4f, 0.6f, 1f), icon = "armor_battery_vest" },
        new Recipe { id = ItemId.MeteorPlate, name = "Meteor Plate", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Meteorite, 3, Mat.Metal, 1), time = 1.8f, desc = "Solid Meteor plates. 94 armour. Fire-proof. Slower.", color = new Color(1f, 0.4f, 0.2f), icon = "armor_meteor_plate" },
        new Recipe { id = ItemId.ChemicalMail, name = "Chemical Mail", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Chemical, 5, Mat.Metal, 2), time = 1.8f, desc = "Chemical chain mail. 100 armour. Poison-proof. Slower. Less zone damage.", color = new Color(0.8f, 0.4f, 1f), icon = "armor_chemical_mail" },
        new Recipe { id = ItemId.SapGuard, name = "Sap Guard", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Sap, 3, Mat.Metal, 2, Mat.IronOre, 1), time = 1.9f, desc = "Heavy Sap guard armour. 140 armour. Slime-proof. Slower.", color = new Color(1f, 0.7f, 0.2f), icon = "armor_sap_guard" },
        new Recipe { id = ItemId.PetalCloak, name = "Petal Cloak", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Petal, 3, Mat.Moss, 1, Mat.Plastic, 1), time = 1.9f, desc = "A swishy Petal cloak. Fast. 35 armour. Faster.", color = new Color(1f, 0.62f, 0.85f), icon = "armor_petal_cloak" },
        new Recipe { id = ItemId.UraniumShell, name = "Uranium Shell", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Uranium, 3, Mat.Obsidian, 1, Mat.Metal, 1), time = 1.9f, desc = "A massive Uranium shell. 160 armour. Poison-proof. Slower. Less zone damage.", color = new Color(0.55f, 1f, 0.2f), icon = "armor_uranium_shell" },
        new Recipe { id = ItemId.CopperHarness, name = "Copper Harness", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.CopperWire, 3, Mat.Wood, 1, Mat.Metal, 1), time = 1.9f, desc = "A Copper climbing harness. Jump higher. 55 armour. Shock-proof. Jump higher.", color = new Color(0.95f, 0.55f, 0.3f), icon = "armor_copper_harness" },
        new Recipe { id = ItemId.PlasticBulwark, name = "Plastic Bulwark", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Plastic, 3, Mat.Metal, 3), time = 1.8f, desc = "A walking Plastic wall. Very slow. 190 armour. Slime-proof. Slower.", color = new Color(0.95f, 0.95f, 0.95f), icon = "armor_plastic_bulwark" },
        new Recipe { id = ItemId.BerryPlate, name = "Berry Plate", kind = ItemKind.Armor, station = Station.Workbench, cost = Cost(Mat.Berry, 4, Mat.Metal, 1), time = 1.8f, desc = "Solid Berry plates. 85 armour. Heals 1.2/s. Slower.", color = new Color(0.95f, 0.25f, 0.45f), icon = "armor_berry_plate" },

        // 100 potions and bombs (Brewing Stand)
        new Recipe { id = ItemId.HerbTonic, name = "Herb Tonic", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Herb, 3), time = 7f, desc = "A herb tonic. Heals 45 right away.", color = new Color(0.4f, 0.95f, 0.3f), icon = "potion_herb_tonic" },
        new Recipe { id = ItemId.MetalElixir, name = "Metal Elixir", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Metal, 2, Mat.Herb, 2, Mat.Berry, 1), time = 9f, desc = "A metal elixir. Heals 90 over 4 seconds.", color = new Color(0.75f, 0.78f, 0.82f), icon = "potion_metal_elixir" },
        new Recipe { id = ItemId.WoodDraught, name = "Wood Draught", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Wood, 2, Mat.Slime, 1, Mat.Sap, 1), time = 9f, desc = "A wood draught. Run 35% faster for 15 seconds.", color = new Color(0.72f, 0.5f, 0.3f), icon = "potion_wood_draught" },
        new Recipe { id = ItemId.MossBrew, name = "Moss Brew", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Moss, 2, Mat.Chemical, 1, Mat.Mushroom, 1), time = 9f, desc = "A moss brew. +30% damage for 15 seconds.", color = new Color(0.45f, 0.7f, 0.3f), icon = "potion_moss_brew" },
        new Recipe { id = ItemId.IronSerum, name = "Iron Serum", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.IronOre, 2, Mat.Metal, 1, Mat.Herb, 1), time = 9f, desc = "A iron serum. Take 30% less damage for 15 seconds.", color = new Color(0.85f, 0.52f, 0.4f), icon = "potion_iron_serum" },
        new Recipe { id = ItemId.GlowstoneShot, name = "Glowstone Shot", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Glowstone, 2, Mat.Battery, 1, Mat.Chemical, 1), time = 9f, desc = "A glowstone energy shot. Shoot 25% faster for 12 seconds.", color = new Color(1f, 0.95f, 0.65f), icon = "potion_glowstone_shot" },
        new Recipe { id = ItemId.CircuitJuice, name = "Circuit Juice", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Circuit, 2, Mat.Berry, 1, Mat.Slime, 1), time = 9f, desc = "Circuit juice. Big jumps, no fall damage for 15 seconds. Shock can't hurt you.", color = new Color(0.25f, 0.85f, 0.45f), icon = "potion_circuit_juice" },
        new Recipe { id = ItemId.GlassMist, name = "Glass Mist", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Glass, 2, Mat.Petal, 1, Mat.Chemical, 1), time = 9f, desc = "Glass mist. Drones and turrets can't see you for 12 seconds.", color = new Color(0.75f, 0.95f, 1f), icon = "potion_glass_mist" },
        new Recipe { id = ItemId.StardustFlask, name = "Stardust Flask", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Stardust, 2, Mat.Crystal, 1, Mat.Metal, 1), time = 9f, desc = "A stardust flask. Gives you 70 extra armour.", color = new Color(0.85f, 0.7f, 1f), icon = "potion_stardust_flask" },
        new Recipe { id = ItemId.CrystalSyrup, name = "Crystal Syrup", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Crystal, 2, Mat.Sap, 1, Mat.Moss, 1), time = 9f, desc = "Crystal syrup. Heal 4 a second for 15 seconds. Ice can't hurt you.", color = new Color(0.4f, 0.85f, 1f), icon = "potion_crystal_syrup" },
        new Recipe { id = ItemId.BerryBomb, name = "Berry Bomb", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Berry, 2, Mat.Chemical, 2), time = 8f, makes = 2, desc = "Throw it! A berry bomb.", color = new Color(0.95f, 0.25f, 0.45f), icon = "potion_berry_bomb" },
        new Recipe { id = ItemId.MushroomGrenade, name = "Mushroom Grenade", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Mushroom, 2, Mat.Metal, 1, Mat.Chemical, 1, Mat.Coal, 1), time = 10f, makes = 2, desc = "Throw it! A mushroom grenade. Poisons.", color = new Color(1f, 0.45f, 0.4f), icon = "potion_mushroom_grenade" },
        new Recipe { id = ItemId.SapTonic, name = "Sap Tonic", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Sap, 2, Mat.Herb, 1), time = 8f, desc = "A sap tonic. Heals 45 right away. Slime can't hurt you.", color = new Color(1f, 0.7f, 0.2f), icon = "potion_sap_tonic" },
        new Recipe { id = ItemId.PetalElixir, name = "Petal Elixir", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Petal, 2, Mat.Herb, 2, Mat.Berry, 1), time = 9f, desc = "A petal elixir. Heals 90 over 4 seconds.", color = new Color(1f, 0.62f, 0.85f), icon = "potion_petal_elixir" },
        new Recipe { id = ItemId.GoldDraught, name = "Gold Draught", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.GoldOre, 2, Mat.Slime, 1, Mat.Sap, 1), time = 9f, desc = "A gold draught. Run 35% faster for 15 seconds.", color = new Color(1f, 0.82f, 0.25f), icon = "potion_gold_draught" },
        new Recipe { id = ItemId.UraniumBrew, name = "Uranium Brew", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Uranium, 2, Mat.Chemical, 1, Mat.Mushroom, 1), time = 9f, desc = "A uranium brew. +30% damage for 15 seconds. Poison can't hurt you.", color = new Color(0.55f, 1f, 0.2f), icon = "potion_uranium_brew" },
        new Recipe { id = ItemId.CopperSerum, name = "Copper Serum", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.CopperWire, 2, Mat.Metal, 1, Mat.Herb, 1), time = 9f, desc = "A copper serum. Take 30% less damage for 15 seconds. Shock can't hurt you.", color = new Color(0.95f, 0.55f, 0.3f), icon = "potion_copper_serum" },
        new Recipe { id = ItemId.PlasticShot, name = "Plastic Shot", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Plastic, 2, Mat.Battery, 1, Mat.Chemical, 1), time = 9f, desc = "A plastic energy shot. Shoot 25% faster for 12 seconds. Slime can't hurt you.", color = new Color(0.95f, 0.95f, 0.95f), icon = "potion_plastic_shot" },
        new Recipe { id = ItemId.HerbJuice, name = "Herb Juice", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Herb, 2, Mat.Berry, 1, Mat.Slime, 1), time = 9f, desc = "Herb juice. Big jumps, no fall damage for 15 seconds.", color = new Color(0.4f, 0.95f, 0.3f), icon = "potion_herb_juice" },
        new Recipe { id = ItemId.MetalMist, name = "Metal Mist", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Metal, 2, Mat.Petal, 1, Mat.Chemical, 1), time = 9f, desc = "Metal mist. Drones and turrets can't see you for 12 seconds.", color = new Color(0.75f, 0.78f, 0.82f), icon = "potion_metal_mist" },
        new Recipe { id = ItemId.WoodFlask, name = "Wood Flask", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Wood, 2, Mat.Crystal, 1, Mat.Metal, 1), time = 9f, desc = "A wood flask. Gives you 70 extra armour.", color = new Color(0.72f, 0.5f, 0.3f), icon = "potion_wood_flask" },
        new Recipe { id = ItemId.MossSyrup, name = "Moss Syrup", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Moss, 3, Mat.Sap, 1), time = 8f, desc = "Moss syrup. Heal 4 a second for 15 seconds.", color = new Color(0.45f, 0.7f, 0.3f), icon = "potion_moss_syrup" },
        new Recipe { id = ItemId.IronBomb, name = "Iron Bomb", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.IronOre, 2, Mat.Chemical, 2), time = 8f, makes = 2, desc = "Throw it! A iron bomb.", color = new Color(0.85f, 0.52f, 0.4f), icon = "potion_iron_bomb" },
        new Recipe { id = ItemId.GlowstoneGrenade, name = "Glowstone Grenade", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Glowstone, 2, Mat.Metal, 1, Mat.Chemical, 1, Mat.Coal, 1), time = 10f, makes = 2, desc = "Throw it! A glowstone grenade.", color = new Color(1f, 0.95f, 0.65f), icon = "potion_glowstone_grenade" },
        new Recipe { id = ItemId.SulfurTonic, name = "Sulfur Tonic", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Sulfur, 2, Mat.Herb, 1), time = 8f, desc = "A sulfur tonic. Heals 45 right away. Fire can't hurt you.", color = new Color(0.95f, 0.95f, 0.3f), icon = "potion_sulfur_tonic" },
        new Recipe { id = ItemId.ObsidianElixir, name = "Obsidian Elixir", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Obsidian, 2, Mat.Herb, 2, Mat.Berry, 1), time = 9f, desc = "A obsidian elixir. Heals 90 over 4 seconds.", color = new Color(0.45f, 0.3f, 0.65f), icon = "potion_obsidian_elixir" },
        new Recipe { id = ItemId.BatteryDraught, name = "Battery Draught", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Battery, 2, Mat.Slime, 1, Mat.Sap, 1), time = 9f, desc = "A battery draught. Run 35% faster for 15 seconds. Shock can't hurt you.", color = new Color(0.4f, 0.6f, 1f), icon = "potion_battery_draught" },
        new Recipe { id = ItemId.MeteorBrew, name = "Meteor Brew", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Meteorite, 2, Mat.Chemical, 1, Mat.Mushroom, 1), time = 9f, desc = "A meteor brew. +30% damage for 15 seconds. Fire can't hurt you.", color = new Color(1f, 0.4f, 0.2f), icon = "potion_meteor_brew" },
        new Recipe { id = ItemId.SlimeSerum, name = "Slime Serum", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Slime, 2, Mat.Metal, 1, Mat.Herb, 1), time = 9f, desc = "A slime serum. Take 30% less damage for 15 seconds. Slime can't hurt you.", color = new Color(0.3f, 0.95f, 0.75f), icon = "potion_slime_serum" },
        new Recipe { id = ItemId.ChemicalShot, name = "Chemical Shot", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Chemical, 3, Mat.Battery, 1), time = 8f, desc = "A chemical energy shot. Shoot 25% faster for 12 seconds. Poison can't hurt you.", color = new Color(0.8f, 0.4f, 1f), icon = "potion_chemical_shot" },
        new Recipe { id = ItemId.SapJuice, name = "Sap Juice", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Sap, 2, Mat.Berry, 1, Mat.Slime, 1), time = 9f, desc = "Sap juice. Big jumps, no fall damage for 15 seconds. Slime can't hurt you.", color = new Color(1f, 0.7f, 0.2f), icon = "potion_sap_juice" },
        new Recipe { id = ItemId.PetalMist, name = "Petal Mist", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Petal, 3, Mat.Chemical, 1), time = 8f, desc = "Petal mist. Drones and turrets can't see you for 12 seconds.", color = new Color(1f, 0.62f, 0.85f), icon = "potion_petal_mist" },
        new Recipe { id = ItemId.GoldFlask, name = "Gold Flask", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.GoldOre, 2, Mat.Crystal, 1, Mat.Metal, 1), time = 9f, desc = "A gold flask. Gives you 70 extra armour.", color = new Color(1f, 0.82f, 0.25f), icon = "potion_gold_flask" },
        new Recipe { id = ItemId.UraniumSyrup, name = "Uranium Syrup", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Uranium, 2, Mat.Sap, 1, Mat.Moss, 1), time = 9f, desc = "Uranium syrup. Heal 4 a second for 15 seconds. Poison can't hurt you.", color = new Color(0.55f, 1f, 0.2f), icon = "potion_uranium_syrup" },
        new Recipe { id = ItemId.CopperBomb, name = "Copper Bomb", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.CopperWire, 2, Mat.Chemical, 2), time = 8f, makes = 2, desc = "Throw it! A copper bomb. Shocks (jolts) people.", color = new Color(0.95f, 0.55f, 0.3f), icon = "potion_copper_bomb" },
        new Recipe { id = ItemId.PlasticGrenade, name = "Plastic Grenade", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Plastic, 2, Mat.Metal, 1, Mat.Chemical, 1, Mat.Coal, 1), time = 10f, makes = 2, desc = "Throw it! A plastic grenade. Slows people down. Leaves a sticky puddle.", color = new Color(0.95f, 0.95f, 0.95f), icon = "potion_plastic_grenade" },
        new Recipe { id = ItemId.GlassTonic, name = "Glass Tonic", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Glass, 2, Mat.Herb, 1), time = 8f, desc = "A glass tonic. Heals 45 right away.", color = new Color(0.75f, 0.95f, 1f), icon = "potion_glass_tonic" },
        new Recipe { id = ItemId.StardustElixir, name = "Stardust Elixir", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Stardust, 2, Mat.Herb, 2, Mat.Berry, 1), time = 9f, desc = "A stardust elixir. Heals 90 over 4 seconds.", color = new Color(0.85f, 0.7f, 1f), icon = "potion_stardust_elixir" },
        new Recipe { id = ItemId.CrystalDraught, name = "Crystal Draught", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Crystal, 2, Mat.Slime, 1, Mat.Sap, 1), time = 9f, desc = "A crystal draught. Run 35% faster for 15 seconds. Ice can't hurt you.", color = new Color(0.4f, 0.85f, 1f), icon = "potion_crystal_draught" },
        new Recipe { id = ItemId.BerryBrew, name = "Berry Brew", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Berry, 2, Mat.Chemical, 1, Mat.Mushroom, 1), time = 9f, desc = "A berry brew. +30% damage for 15 seconds.", color = new Color(0.95f, 0.25f, 0.45f), icon = "potion_berry_brew" },
        new Recipe { id = ItemId.MushroomSerum, name = "Mushroom Serum", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Mushroom, 2, Mat.Metal, 1, Mat.Herb, 1), time = 9f, desc = "A mushroom serum. Take 30% less damage for 15 seconds. Poison can't hurt you.", color = new Color(1f, 0.45f, 0.4f), icon = "potion_mushroom_serum" },
        new Recipe { id = ItemId.CoalShot, name = "Coal Shot", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Coal, 3, Mat.Battery, 1, Mat.Chemical, 1), time = 9f, desc = "A coal energy shot. Shoot 25% faster for 12 seconds. Fire can't hurt you.", color = new Color(0.3f, 0.3f, 0.33f), icon = "potion_coal_shot" },
        new Recipe { id = ItemId.SulfurJuice, name = "Sulfur Juice", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Sulfur, 2, Mat.Berry, 1, Mat.Slime, 1), time = 9f, desc = "Sulfur juice. Big jumps, no fall damage for 15 seconds. Fire can't hurt you.", color = new Color(0.95f, 0.95f, 0.3f), icon = "potion_sulfur_juice" },
        new Recipe { id = ItemId.ObsidianMist, name = "Obsidian Mist", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Obsidian, 2, Mat.Petal, 1, Mat.Chemical, 1), time = 9f, desc = "Obsidian mist. Drones and turrets can't see you for 12 seconds.", color = new Color(0.45f, 0.3f, 0.65f), icon = "potion_obsidian_mist" },
        new Recipe { id = ItemId.BatteryFlask, name = "Battery Flask", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Battery, 2, Mat.Crystal, 1, Mat.Metal, 1), time = 9f, desc = "A battery flask. Gives you 70 extra armour. Shock can't hurt you.", color = new Color(0.4f, 0.6f, 1f), icon = "potion_battery_flask" },
        new Recipe { id = ItemId.MeteorSyrup, name = "Meteor Syrup", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Meteorite, 2, Mat.Sap, 1, Mat.Moss, 1), time = 9f, desc = "Meteor syrup. Heal 4 a second for 15 seconds. Fire can't hurt you.", color = new Color(1f, 0.4f, 0.2f), icon = "potion_meteor_syrup" },
        new Recipe { id = ItemId.ChemicalGrenade, name = "Chemical Grenade", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Chemical, 3, Mat.Metal, 1, Mat.Coal, 1), time = 9f, makes = 2, desc = "Throw it! A chemical grenade. Poisons.", color = new Color(0.8f, 0.4f, 1f), icon = "potion_chemical_grenade" },
        new Recipe { id = ItemId.MetalTonic, name = "Metal Tonic", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Metal, 2, Mat.Herb, 1), time = 8f, desc = "A metal tonic. Heals 45 right away.", color = new Color(0.75f, 0.78f, 0.82f), icon = "potion_metal_tonic" },
        new Recipe { id = ItemId.MossDraught, name = "Moss Draught", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Moss, 2, Mat.Slime, 1, Mat.Sap, 1), time = 9f, desc = "A moss draught. Run 35% faster for 15 seconds.", color = new Color(0.45f, 0.7f, 0.3f), icon = "potion_moss_draught" },
        new Recipe { id = ItemId.IronBrew, name = "Iron Brew", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.IronOre, 2, Mat.Chemical, 1, Mat.Mushroom, 1), time = 9f, desc = "A iron brew. +30% damage for 15 seconds.", color = new Color(0.85f, 0.52f, 0.4f), icon = "potion_iron_brew" },
        new Recipe { id = ItemId.GlowstoneSerum, name = "Glowstone Serum", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Glowstone, 2, Mat.Metal, 1, Mat.Herb, 1), time = 9f, desc = "A glowstone serum. Take 30% less damage for 15 seconds.", color = new Color(1f, 0.95f, 0.65f), icon = "potion_glowstone_serum" },
        new Recipe { id = ItemId.CircuitShot, name = "Circuit Shot", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Circuit, 2, Mat.Battery, 1, Mat.Chemical, 1), time = 9f, desc = "A circuit energy shot. Shoot 25% faster for 12 seconds. Shock can't hurt you.", color = new Color(0.25f, 0.85f, 0.45f), icon = "potion_circuit_shot" },
        new Recipe { id = ItemId.GlassJuice, name = "Glass Juice", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Glass, 2, Mat.Berry, 1, Mat.Slime, 1), time = 9f, desc = "Glass juice. Big jumps, no fall damage for 15 seconds.", color = new Color(0.75f, 0.95f, 1f), icon = "potion_glass_juice" },
        new Recipe { id = ItemId.StardustMist, name = "Stardust Mist", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Stardust, 2, Mat.Petal, 1, Mat.Chemical, 1), time = 9f, desc = "Stardust mist. Drones and turrets can't see you for 12 seconds.", color = new Color(0.85f, 0.7f, 1f), icon = "potion_stardust_mist" },
        new Recipe { id = ItemId.CrystalFlask, name = "Crystal Flask", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Crystal, 3, Mat.Metal, 1), time = 8f, desc = "A crystal flask. Gives you 70 extra armour. Ice can't hurt you.", color = new Color(0.4f, 0.85f, 1f), icon = "potion_crystal_flask" },
        new Recipe { id = ItemId.BerrySyrup, name = "Berry Syrup", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Berry, 2, Mat.Sap, 1, Mat.Moss, 1), time = 9f, desc = "Berry syrup. Heal 4 a second for 15 seconds.", color = new Color(0.95f, 0.25f, 0.45f), icon = "potion_berry_syrup" },
        new Recipe { id = ItemId.MushroomBomb, name = "Mushroom Bomb", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Mushroom, 2, Mat.Chemical, 2), time = 8f, makes = 2, desc = "Throw it! A mushroom bomb. Poisons.", color = new Color(1f, 0.45f, 0.4f), icon = "potion_mushroom_bomb" },
        new Recipe { id = ItemId.CoalGrenade, name = "Coal Grenade", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Coal, 3, Mat.Metal, 1, Mat.Chemical, 1), time = 9f, makes = 2, desc = "Throw it! A coal grenade. Sets things on fire.", color = new Color(0.3f, 0.3f, 0.33f), icon = "potion_coal_grenade" },
        new Recipe { id = ItemId.PetalTonic, name = "Petal Tonic", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Petal, 2, Mat.Herb, 1), time = 8f, desc = "A petal tonic. Heals 45 right away.", color = new Color(1f, 0.62f, 0.85f), icon = "potion_petal_tonic" },
        new Recipe { id = ItemId.GoldElixir, name = "Gold Elixir", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.GoldOre, 2, Mat.Herb, 2, Mat.Berry, 1), time = 9f, desc = "A gold elixir. Heals 90 over 4 seconds.", color = new Color(1f, 0.82f, 0.25f), icon = "potion_gold_elixir" },
        new Recipe { id = ItemId.UraniumDraught, name = "Uranium Draught", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Uranium, 2, Mat.Slime, 1, Mat.Sap, 1), time = 9f, desc = "A uranium draught. Run 35% faster for 15 seconds. Poison can't hurt you.", color = new Color(0.55f, 1f, 0.2f), icon = "potion_uranium_draught" },
        new Recipe { id = ItemId.CopperBrew, name = "Copper Brew", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.CopperWire, 2, Mat.Chemical, 1, Mat.Mushroom, 1), time = 9f, desc = "A copper brew. +30% damage for 15 seconds. Shock can't hurt you.", color = new Color(0.95f, 0.55f, 0.3f), icon = "potion_copper_brew" },
        new Recipe { id = ItemId.PlasticSerum, name = "Plastic Serum", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Plastic, 2, Mat.Metal, 1, Mat.Herb, 1), time = 9f, desc = "A plastic serum. Take 30% less damage for 15 seconds. Slime can't hurt you.", color = new Color(0.95f, 0.95f, 0.95f), icon = "potion_plastic_serum" },
        new Recipe { id = ItemId.HerbShot, name = "Herb Shot", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Herb, 2, Mat.Battery, 1, Mat.Chemical, 1), time = 9f, desc = "A herb energy shot. Shoot 25% faster for 12 seconds.", color = new Color(0.4f, 0.95f, 0.3f), icon = "potion_herb_shot" },
        new Recipe { id = ItemId.MetalJuice, name = "Metal Juice", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Metal, 2, Mat.Berry, 1, Mat.Slime, 1), time = 9f, desc = "Metal juice. Big jumps, no fall damage for 15 seconds.", color = new Color(0.75f, 0.78f, 0.82f), icon = "potion_metal_juice" },
        new Recipe { id = ItemId.WoodMist, name = "Wood Mist", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Wood, 2, Mat.Petal, 1, Mat.Chemical, 1), time = 9f, desc = "Wood mist. Drones and turrets can't see you for 12 seconds.", color = new Color(0.72f, 0.5f, 0.3f), icon = "potion_wood_mist" },
        new Recipe { id = ItemId.MossFlask, name = "Moss Flask", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Moss, 2, Mat.Crystal, 1, Mat.Metal, 1), time = 9f, desc = "A moss flask. Gives you 70 extra armour.", color = new Color(0.45f, 0.7f, 0.3f), icon = "potion_moss_flask" },
        new Recipe { id = ItemId.IronSyrup, name = "Iron Syrup", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.IronOre, 2, Mat.Sap, 1, Mat.Moss, 1), time = 9f, desc = "Iron syrup. Heal 4 a second for 15 seconds.", color = new Color(0.85f, 0.52f, 0.4f), icon = "potion_iron_syrup" },
        new Recipe { id = ItemId.GlowstoneBomb, name = "Glowstone Bomb", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Glowstone, 2, Mat.Chemical, 2), time = 8f, makes = 2, desc = "Throw it! A glowstone bomb.", color = new Color(1f, 0.95f, 0.65f), icon = "potion_glowstone_bomb" },
        new Recipe { id = ItemId.CircuitGrenade, name = "Circuit Grenade", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Circuit, 2, Mat.Metal, 1, Mat.Chemical, 1, Mat.Coal, 1), time = 10f, makes = 2, desc = "Throw it! A circuit grenade. Shocks (jolts) people.", color = new Color(0.25f, 0.85f, 0.45f), icon = "potion_circuit_grenade" },
        new Recipe { id = ItemId.ObsidianTonic, name = "Obsidian Tonic", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Obsidian, 3, Mat.Herb, 1), time = 8f, desc = "A obsidian tonic. Heals 45 right away.", color = new Color(0.45f, 0.3f, 0.65f), icon = "potion_obsidian_tonic" },
        new Recipe { id = ItemId.BatteryElixir, name = "Battery Elixir", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Battery, 2, Mat.Herb, 2, Mat.Berry, 1), time = 9f, desc = "A battery elixir. Heals 90 over 4 seconds. Shock can't hurt you.", color = new Color(0.4f, 0.6f, 1f), icon = "potion_battery_elixir" },
        new Recipe { id = ItemId.MeteorDraught, name = "Meteor Draught", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Meteorite, 2, Mat.Slime, 1, Mat.Sap, 1), time = 9f, desc = "A meteor draught. Run 35% faster for 15 seconds. Fire can't hurt you.", color = new Color(1f, 0.4f, 0.2f), icon = "potion_meteor_draught" },
        new Recipe { id = ItemId.SlimeBrew, name = "Slime Brew", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Slime, 2, Mat.Chemical, 1, Mat.Mushroom, 1), time = 9f, desc = "A slime brew. +30% damage for 15 seconds. Slime can't hurt you.", color = new Color(0.3f, 0.95f, 0.75f), icon = "potion_slime_brew" },
        new Recipe { id = ItemId.ChemicalSerum, name = "Chemical Serum", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Chemical, 2, Mat.Metal, 1, Mat.Herb, 1), time = 9f, desc = "A chemical serum. Take 30% less damage for 15 seconds. Poison can't hurt you.", color = new Color(0.8f, 0.4f, 1f), icon = "potion_chemical_serum" },
        new Recipe { id = ItemId.SapShot, name = "Sap Shot", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Sap, 2, Mat.Battery, 1, Mat.Chemical, 1), time = 9f, desc = "A sap energy shot. Shoot 25% faster for 12 seconds. Slime can't hurt you.", color = new Color(1f, 0.7f, 0.2f), icon = "potion_sap_shot" },
        new Recipe { id = ItemId.PetalJuice, name = "Petal Juice", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Petal, 2, Mat.Berry, 1, Mat.Slime, 1), time = 9f, desc = "Petal juice. Big jumps, no fall damage for 15 seconds.", color = new Color(1f, 0.62f, 0.85f), icon = "potion_petal_juice" },
        new Recipe { id = ItemId.GoldMist, name = "Gold Mist", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.GoldOre, 2, Mat.Petal, 1, Mat.Chemical, 1), time = 9f, desc = "Gold mist. Drones and turrets can't see you for 12 seconds.", color = new Color(1f, 0.82f, 0.25f), icon = "potion_gold_mist" },
        new Recipe { id = ItemId.UraniumFlask, name = "Uranium Flask", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Uranium, 2, Mat.Crystal, 1, Mat.Metal, 1), time = 9f, desc = "A uranium flask. Gives you 70 extra armour. Poison can't hurt you.", color = new Color(0.55f, 1f, 0.2f), icon = "potion_uranium_flask" },
        new Recipe { id = ItemId.CopperSyrup, name = "Copper Syrup", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.CopperWire, 2, Mat.Sap, 1, Mat.Moss, 1), time = 9f, desc = "Copper syrup. Heal 4 a second for 15 seconds. Shock can't hurt you.", color = new Color(0.95f, 0.55f, 0.3f), icon = "potion_copper_syrup" },
        new Recipe { id = ItemId.PlasticBomb, name = "Plastic Bomb", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Plastic, 2, Mat.Chemical, 2), time = 8f, makes = 2, desc = "Throw it! A plastic bomb. Slows people down. Leaves a sticky puddle.", color = new Color(0.95f, 0.95f, 0.95f), icon = "potion_plastic_bomb" },
        new Recipe { id = ItemId.HerbGrenade, name = "Herb Grenade", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Herb, 2, Mat.Metal, 1, Mat.Chemical, 1, Mat.Coal, 1), time = 10f, makes = 2, desc = "Throw it! A herb grenade.", color = new Color(0.4f, 0.95f, 0.3f), icon = "potion_herb_grenade" },
        new Recipe { id = ItemId.StardustTonic, name = "Stardust Tonic", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Stardust, 2, Mat.Herb, 1), time = 8f, desc = "A stardust tonic. Heals 45 right away.", color = new Color(0.85f, 0.7f, 1f), icon = "potion_stardust_tonic" },
        new Recipe { id = ItemId.CrystalElixir, name = "Crystal Elixir", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Crystal, 2, Mat.Herb, 2, Mat.Berry, 1), time = 9f, desc = "A crystal elixir. Heals 90 over 4 seconds. Ice can't hurt you.", color = new Color(0.4f, 0.85f, 1f), icon = "potion_crystal_elixir" },
        new Recipe { id = ItemId.BerryDraught, name = "Berry Draught", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Berry, 2, Mat.Slime, 1, Mat.Sap, 1), time = 9f, desc = "A berry draught. Run 35% faster for 15 seconds.", color = new Color(0.95f, 0.25f, 0.45f), icon = "potion_berry_draught" },
        new Recipe { id = ItemId.CoalSerum, name = "Coal Serum", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Coal, 2, Mat.Metal, 1, Mat.Herb, 1), time = 9f, desc = "A coal serum. Take 30% less damage for 15 seconds. Fire can't hurt you.", color = new Color(0.3f, 0.3f, 0.33f), icon = "potion_coal_serum" },
        new Recipe { id = ItemId.SulfurShot, name = "Sulfur Shot", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Sulfur, 2, Mat.Battery, 1, Mat.Chemical, 1), time = 9f, desc = "A sulfur energy shot. Shoot 25% faster for 12 seconds. Fire can't hurt you.", color = new Color(0.95f, 0.95f, 0.3f), icon = "potion_sulfur_shot" },
        new Recipe { id = ItemId.ObsidianJuice, name = "Obsidian Juice", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Obsidian, 2, Mat.Berry, 1, Mat.Slime, 1), time = 9f, desc = "Obsidian juice. Big jumps, no fall damage for 15 seconds.", color = new Color(0.45f, 0.3f, 0.65f), icon = "potion_obsidian_juice" },
        new Recipe { id = ItemId.BatteryMist, name = "Battery Mist", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Battery, 2, Mat.Petal, 1, Mat.Chemical, 1), time = 9f, desc = "Battery mist. Drones and turrets can't see you for 12 seconds. Shock can't hurt you.", color = new Color(0.4f, 0.6f, 1f), icon = "potion_battery_mist" },
        new Recipe { id = ItemId.MeteorFlask, name = "Meteor Flask", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Meteorite, 2, Mat.Crystal, 1, Mat.Metal, 1), time = 9f, desc = "A meteor flask. Gives you 70 extra armour. Fire can't hurt you.", color = new Color(1f, 0.4f, 0.2f), icon = "potion_meteor_flask" },
        new Recipe { id = ItemId.SlimeSyrup, name = "Slime Syrup", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Slime, 2, Mat.Sap, 1, Mat.Moss, 1), time = 9f, desc = "Slime syrup. Heal 4 a second for 15 seconds. Slime can't hurt you.", color = new Color(0.3f, 0.95f, 0.75f), icon = "potion_slime_syrup" },
        new Recipe { id = ItemId.ChemicalBomb, name = "Chemical Bomb", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Chemical, 4), time = 7f, makes = 2, desc = "Throw it! A chemical bomb. Poisons.", color = new Color(0.8f, 0.4f, 1f), icon = "potion_chemical_bomb" },
        new Recipe { id = ItemId.SapGrenade, name = "Sap Grenade", kind = ItemKind.Throwable, station = Station.Brew, cost = Cost(Mat.Sap, 2, Mat.Metal, 1, Mat.Chemical, 1, Mat.Coal, 1), time = 10f, makes = 2, desc = "Throw it! A sap grenade. Slows people down. Leaves a sticky puddle.", color = new Color(1f, 0.7f, 0.2f), icon = "potion_sap_grenade" },
        new Recipe { id = ItemId.WoodTonic, name = "Wood Tonic", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Wood, 2, Mat.Herb, 1), time = 8f, desc = "A wood tonic. Heals 45 right away.", color = new Color(0.72f, 0.5f, 0.3f), icon = "potion_wood_tonic" },
        new Recipe { id = ItemId.MossElixir, name = "Moss Elixir", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Moss, 2, Mat.Herb, 2, Mat.Berry, 1), time = 9f, desc = "A moss elixir. Heals 90 over 4 seconds.", color = new Color(0.45f, 0.7f, 0.3f), icon = "potion_moss_elixir" },
        new Recipe { id = ItemId.IronDraught, name = "Iron Draught", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.IronOre, 2, Mat.Slime, 1, Mat.Sap, 1), time = 9f, desc = "A iron draught. Run 35% faster for 15 seconds.", color = new Color(0.85f, 0.52f, 0.4f), icon = "potion_iron_draught" },
        new Recipe { id = ItemId.GlowstoneBrew, name = "Glowstone Brew", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Glowstone, 2, Mat.Chemical, 1, Mat.Mushroom, 1), time = 9f, desc = "A glowstone brew. +30% damage for 15 seconds.", color = new Color(1f, 0.95f, 0.65f), icon = "potion_glowstone_brew" },
        new Recipe { id = ItemId.CircuitSerum, name = "Circuit Serum", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Circuit, 2, Mat.Metal, 1, Mat.Herb, 1), time = 9f, desc = "A circuit serum. Take 30% less damage for 15 seconds. Shock can't hurt you.", color = new Color(0.25f, 0.85f, 0.45f), icon = "potion_circuit_serum" },
        new Recipe { id = ItemId.GlassShot, name = "Glass Shot", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Glass, 2, Mat.Battery, 1, Mat.Chemical, 1), time = 9f, desc = "A glass energy shot. Shoot 25% faster for 12 seconds.", color = new Color(0.75f, 0.95f, 1f), icon = "potion_glass_shot" },
        new Recipe { id = ItemId.StardustJuice, name = "Stardust Juice", kind = ItemKind.Potion, station = Station.Brew, cost = Cost(Mat.Stardust, 2, Mat.Berry, 1, Mat.Slime, 1), time = 9f, desc = "Stardust juice. Big jumps, no fall damage for 15 seconds.", color = new Color(0.85f, 0.7f, 1f), icon = "potion_stardust_juice" },

        // 100 enchants (Enchanting Table)
        new Recipe { id = ItemId.HerbEdgeEnchant, name = "Herb Edge", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Herb, 2, Mat.IronOre, 1), time = 1.5f, desc = "+20% damage.", color = new Color(0.4f, 0.95f, 0.3f), icon = "ench_herb_edge" },
        new Recipe { id = ItemId.MetalFuryEnchant, name = "Metal Fury", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Metal, 2, Mat.Battery, 1), time = 1.5f, desc = "Shoot 20% faster.", color = new Color(0.75f, 0.78f, 0.82f), icon = "ench_metal_fury" },
        new Recipe { id = ItemId.WoodChillEnchant, name = "Wood Chill", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Wood, 2, Mat.Crystal, 1), time = 1.5f, desc = "40% less heat.", color = new Color(0.72f, 0.5f, 0.3f), icon = "ench_wood_chill" },
        new Recipe { id = ItemId.IronBurstEnchant, name = "Iron Burst", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.IronOre, 2, Mat.Coal, 1), time = 1.5f, desc = "A little explosion on every hit.", color = new Color(0.85f, 0.52f, 0.4f), icon = "ench_iron_burst" },
        new Recipe { id = ItemId.GlowstoneEchoEnchant, name = "Glowstone Echo", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Glowstone, 2, Mat.CopperWire, 1), time = 1.5f, desc = "Hits jump to 1 more target.", color = new Color(1f, 0.95f, 0.65f), icon = "ench_glowstone_echo" },
        new Recipe { id = ItemId.CircuitLanceEnchant, name = "Circuit Lance", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Circuit, 2, Mat.Obsidian, 1), time = 1.5f, desc = "Shots go through targets. Adds shock.", color = new Color(0.5f, 0.75f, 1f), icon = "ench_circuit_lance" },
        new Recipe { id = ItemId.GlassScatterEnchant, name = "Glass Scatter", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Glass, 2, Mat.Stardust, 1), time = 1.5f, desc = "One extra bullet per shot.", color = new Color(0.75f, 0.95f, 1f), icon = "ench_glass_scatter" },
        new Recipe { id = ItemId.StardustForceEnchant, name = "Stardust Force", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Stardust, 2, Mat.Wood, 1), time = 1.5f, desc = "Big knockback.", color = new Color(0.85f, 0.7f, 1f), icon = "ench_stardust_force" },
        new Recipe { id = ItemId.CrystalFortuneEnchant, name = "Crystal Fortune", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Crystal, 2, Mat.GoldOre, 1), time = 1.5f, desc = "20% chance of double damage. Adds ice.", color = new Color(0.55f, 0.92f, 1f), icon = "ench_crystal_fortune" },
        new Recipe { id = ItemId.BerryBreakerEnchant, name = "Berry Breaker", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Berry, 2, Mat.Metal, 1), time = 1.5f, desc = "80% more damage to labs.", color = new Color(0.95f, 0.25f, 0.45f), icon = "ench_berry_breaker" },
        new Recipe { id = ItemId.MushroomReachEnchant, name = "Mushroom Reach", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Mushroom, 2, Mat.Glass, 1), time = 1.5f, desc = "More range, tighter aim. Adds poison.", color = new Color(0.55f, 1f, 0.2f), icon = "ench_mushroom_reach" },
        new Recipe { id = ItemId.SapEdgeEnchant, name = "Sap Edge", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Sap, 2, Mat.IronOre, 1), time = 1.5f, desc = "+20% damage. Adds slime.", color = new Color(0.35f, 1f, 0.45f), icon = "ench_sap_edge" },
        new Recipe { id = ItemId.PetalFuryEnchant, name = "Petal Fury", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Petal, 2, Mat.Battery, 1), time = 1.5f, desc = "Shoot 20% faster.", color = new Color(1f, 0.62f, 0.85f), icon = "ench_petal_fury" },
        new Recipe { id = ItemId.GoldChillEnchant, name = "Gold Chill", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.GoldOre, 2, Mat.Crystal, 1), time = 1.5f, desc = "40% less heat.", color = new Color(1f, 0.82f, 0.25f), icon = "ench_gold_chill" },
        new Recipe { id = ItemId.UraniumLeechEnchant, name = "Uranium Leech", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Uranium, 2, Mat.Berry, 1), time = 1.5f, desc = "Heal 12% of the damage you deal. Adds poison.", color = new Color(0.55f, 1f, 0.2f), icon = "ench_uranium_leech" },
        new Recipe { id = ItemId.CopperBurstEnchant, name = "Copper Burst", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.CopperWire, 2, Mat.Coal, 1), time = 1.5f, desc = "A little explosion on every hit. Adds shock.", color = new Color(0.5f, 0.75f, 1f), icon = "ench_copper_burst" },
        new Recipe { id = ItemId.PlasticEchoEnchant, name = "Plastic Echo", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Plastic, 2, Mat.CopperWire, 1), time = 1.5f, desc = "Hits jump to 1 more target. Adds slime.", color = new Color(0.35f, 1f, 0.45f), icon = "ench_plastic_echo" },
        new Recipe { id = ItemId.HerbLanceEnchant, name = "Herb Lance", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Herb, 2, Mat.Obsidian, 1), time = 1.5f, desc = "Shots go through targets.", color = new Color(0.4f, 0.95f, 0.3f), icon = "ench_herb_lance" },
        new Recipe { id = ItemId.MetalScatterEnchant, name = "Metal Scatter", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Metal, 2, Mat.Stardust, 1), time = 1.5f, desc = "One extra bullet per shot.", color = new Color(0.75f, 0.78f, 0.82f), icon = "ench_metal_scatter" },
        new Recipe { id = ItemId.WoodForceEnchant, name = "Wood Force", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Wood, 3), time = 1.5f, desc = "Big knockback.", color = new Color(0.72f, 0.5f, 0.3f), icon = "ench_wood_force" },
        new Recipe { id = ItemId.MossFortuneEnchant, name = "Moss Fortune", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Moss, 2, Mat.GoldOre, 1), time = 1.5f, desc = "20% chance of double damage.", color = new Color(0.45f, 0.7f, 0.3f), icon = "ench_moss_fortune" },
        new Recipe { id = ItemId.IronBreakerEnchant, name = "Iron Breaker", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.IronOre, 2, Mat.Metal, 1), time = 1.5f, desc = "80% more damage to labs.", color = new Color(0.85f, 0.52f, 0.4f), icon = "ench_iron_breaker" },
        new Recipe { id = ItemId.GlowstoneReachEnchant, name = "Glowstone Reach", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Glowstone, 2, Mat.Glass, 1), time = 1.5f, desc = "More range, tighter aim.", color = new Color(1f, 0.95f, 0.65f), icon = "ench_glowstone_reach" },
        new Recipe { id = ItemId.SulfurEdgeEnchant, name = "Sulfur Edge", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Sulfur, 2, Mat.IronOre, 1), time = 1.5f, desc = "+20% damage. Adds fire.", color = new Color(1f, 0.5f, 0.1f), icon = "ench_sulfur_edge" },
        new Recipe { id = ItemId.ObsidianFuryEnchant, name = "Obsidian Fury", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Obsidian, 2, Mat.Battery, 1), time = 1.5f, desc = "Shoot 20% faster.", color = new Color(0.45f, 0.3f, 0.65f), icon = "ench_obsidian_fury" },
        new Recipe { id = ItemId.BatteryChillEnchant, name = "Battery Chill", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Battery, 2, Mat.Crystal, 1), time = 1.5f, desc = "40% less heat. Adds shock.", color = new Color(0.5f, 0.75f, 1f), icon = "ench_battery_chill" },
        new Recipe { id = ItemId.MeteorLeechEnchant, name = "Meteor Leech", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Meteorite, 2, Mat.Berry, 1), time = 1.5f, desc = "Heal 12% of the damage you deal. Adds fire.", color = new Color(1f, 0.5f, 0.1f), icon = "ench_meteor_leech" },
        new Recipe { id = ItemId.SlimeBurstEnchant, name = "Slime Burst", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Slime, 2, Mat.Coal, 1), time = 1.5f, desc = "A little explosion on every hit. Adds slime.", color = new Color(0.35f, 1f, 0.45f), icon = "ench_slime_burst" },
        new Recipe { id = ItemId.ChemicalEchoEnchant, name = "Chemical Echo", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Chemical, 2, Mat.CopperWire, 1), time = 1.5f, desc = "Hits jump to 1 more target. Adds poison.", color = new Color(0.55f, 1f, 0.2f), icon = "ench_chemical_echo" },
        new Recipe { id = ItemId.SapLanceEnchant, name = "Sap Lance", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Sap, 2, Mat.Obsidian, 1), time = 1.5f, desc = "Shots go through targets. Adds slime.", color = new Color(0.35f, 1f, 0.45f), icon = "ench_sap_lance" },
        new Recipe { id = ItemId.PetalScatterEnchant, name = "Petal Scatter", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Petal, 2, Mat.Stardust, 1), time = 1.5f, desc = "One extra bullet per shot.", color = new Color(1f, 0.62f, 0.85f), icon = "ench_petal_scatter" },
        new Recipe { id = ItemId.GoldForceEnchant, name = "Gold Force", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.GoldOre, 2, Mat.Wood, 1), time = 1.5f, desc = "Big knockback.", color = new Color(1f, 0.82f, 0.25f), icon = "ench_gold_force" },
        new Recipe { id = ItemId.UraniumFortuneEnchant, name = "Uranium Fortune", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Uranium, 2, Mat.GoldOre, 1), time = 1.5f, desc = "20% chance of double damage. Adds poison.", color = new Color(0.55f, 1f, 0.2f), icon = "ench_uranium_fortune" },
        new Recipe { id = ItemId.CopperBreakerEnchant, name = "Copper Breaker", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.CopperWire, 2, Mat.Metal, 1), time = 1.5f, desc = "80% more damage to labs. Adds shock.", color = new Color(0.5f, 0.75f, 1f), icon = "ench_copper_breaker" },
        new Recipe { id = ItemId.PlasticReachEnchant, name = "Plastic Reach", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Plastic, 2, Mat.Glass, 1), time = 1.5f, desc = "More range, tighter aim. Adds slime.", color = new Color(0.35f, 1f, 0.45f), icon = "ench_plastic_reach" },
        new Recipe { id = ItemId.GlassEdgeEnchant, name = "Glass Edge", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Glass, 2, Mat.IronOre, 1), time = 1.5f, desc = "+20% damage.", color = new Color(0.75f, 0.95f, 1f), icon = "ench_glass_edge" },
        new Recipe { id = ItemId.StardustFuryEnchant, name = "Stardust Fury", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Stardust, 2, Mat.Battery, 1), time = 1.5f, desc = "Shoot 20% faster.", color = new Color(0.85f, 0.7f, 1f), icon = "ench_stardust_fury" },
        new Recipe { id = ItemId.CrystalChillEnchant, name = "Crystal Chill", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Crystal, 3), time = 1.5f, desc = "40% less heat. Adds ice.", color = new Color(0.55f, 0.92f, 1f), icon = "ench_crystal_chill" },
        new Recipe { id = ItemId.BerryLeechEnchant, name = "Berry Leech", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Berry, 3), time = 1.5f, desc = "Heal 12% of the damage you deal.", color = new Color(0.95f, 0.25f, 0.45f), icon = "ench_berry_leech" },
        new Recipe { id = ItemId.MushroomBurstEnchant, name = "Mushroom Burst", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Mushroom, 2, Mat.Coal, 1), time = 1.5f, desc = "A little explosion on every hit. Adds poison.", color = new Color(0.55f, 1f, 0.2f), icon = "ench_mushroom_burst" },
        new Recipe { id = ItemId.CoalEchoEnchant, name = "Coal Echo", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Coal, 2, Mat.CopperWire, 1), time = 1.5f, desc = "Hits jump to 1 more target. Adds fire.", color = new Color(1f, 0.5f, 0.1f), icon = "ench_coal_echo" },
        new Recipe { id = ItemId.SulfurLanceEnchant, name = "Sulfur Lance", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Sulfur, 2, Mat.Obsidian, 1), time = 1.5f, desc = "Shots go through targets. Adds fire.", color = new Color(1f, 0.5f, 0.1f), icon = "ench_sulfur_lance" },
        new Recipe { id = ItemId.ObsidianScatterEnchant, name = "Obsidian Scatter", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Obsidian, 2, Mat.Stardust, 1), time = 1.5f, desc = "One extra bullet per shot.", color = new Color(0.45f, 0.3f, 0.65f), icon = "ench_obsidian_scatter" },
        new Recipe { id = ItemId.BatteryForceEnchant, name = "Battery Force", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Battery, 2, Mat.Wood, 1), time = 1.5f, desc = "Big knockback. Adds shock.", color = new Color(0.5f, 0.75f, 1f), icon = "ench_battery_force" },
        new Recipe { id = ItemId.MeteorFortuneEnchant, name = "Meteor Fortune", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Meteorite, 2, Mat.GoldOre, 1), time = 1.5f, desc = "20% chance of double damage. Adds fire.", color = new Color(1f, 0.5f, 0.1f), icon = "ench_meteor_fortune" },
        new Recipe { id = ItemId.SlimeBreakerEnchant, name = "Slime Breaker", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Slime, 2, Mat.Metal, 1), time = 1.5f, desc = "80% more damage to labs. Adds slime.", color = new Color(0.35f, 1f, 0.45f), icon = "ench_slime_breaker" },
        new Recipe { id = ItemId.ChemicalReachEnchant, name = "Chemical Reach", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Chemical, 2, Mat.Glass, 1), time = 1.5f, desc = "More range, tighter aim. Adds poison.", color = new Color(0.55f, 1f, 0.2f), icon = "ench_chemical_reach" },
        new Recipe { id = ItemId.MetalEdgeEnchant, name = "Metal Edge", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Metal, 2, Mat.IronOre, 1), time = 1.5f, desc = "+20% damage.", color = new Color(0.75f, 0.78f, 0.82f), icon = "ench_metal_edge" },
        new Recipe { id = ItemId.WoodFuryEnchant, name = "Wood Fury", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Wood, 2, Mat.Battery, 1), time = 1.5f, desc = "Shoot 20% faster.", color = new Color(0.72f, 0.5f, 0.3f), icon = "ench_wood_fury" },
        new Recipe { id = ItemId.MossChillEnchant, name = "Moss Chill", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Moss, 2, Mat.Crystal, 1), time = 1.5f, desc = "40% less heat.", color = new Color(0.45f, 0.7f, 0.3f), icon = "ench_moss_chill" },
        new Recipe { id = ItemId.IronLeechEnchant, name = "Iron Leech", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.IronOre, 2, Mat.Berry, 1), time = 1.5f, desc = "Heal 12% of the damage you deal.", color = new Color(0.85f, 0.52f, 0.4f), icon = "ench_iron_leech" },
        new Recipe { id = ItemId.GlowstoneBurstEnchant, name = "Glowstone Burst", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Glowstone, 2, Mat.Coal, 1), time = 1.5f, desc = "A little explosion on every hit.", color = new Color(1f, 0.95f, 0.65f), icon = "ench_glowstone_burst" },
        new Recipe { id = ItemId.CircuitEchoEnchant, name = "Circuit Echo", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Circuit, 2, Mat.CopperWire, 1), time = 1.5f, desc = "Hits jump to 1 more target. Adds shock.", color = new Color(0.5f, 0.75f, 1f), icon = "ench_circuit_echo" },
        new Recipe { id = ItemId.GlassLanceEnchant, name = "Glass Lance", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Glass, 2, Mat.Obsidian, 1), time = 1.5f, desc = "Shots go through targets.", color = new Color(0.75f, 0.95f, 1f), icon = "ench_glass_lance" },
        new Recipe { id = ItemId.StardustScatterEnchant, name = "Stardust Scatter", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Stardust, 4), time = 1.5f, desc = "One extra bullet per shot.", color = new Color(0.85f, 0.7f, 1f), icon = "ench_stardust_scatter" },
        new Recipe { id = ItemId.CrystalForceEnchant, name = "Crystal Force", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Crystal, 2, Mat.Wood, 1), time = 1.5f, desc = "Big knockback. Adds ice.", color = new Color(0.55f, 0.92f, 1f), icon = "ench_crystal_force" },
        new Recipe { id = ItemId.BerryFortuneEnchant, name = "Berry Fortune", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Berry, 2, Mat.GoldOre, 1), time = 1.5f, desc = "20% chance of double damage.", color = new Color(0.95f, 0.25f, 0.45f), icon = "ench_berry_fortune" },
        new Recipe { id = ItemId.MushroomBreakerEnchant, name = "Mushroom Breaker", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Mushroom, 2, Mat.Metal, 1), time = 1.5f, desc = "80% more damage to labs. Adds poison.", color = new Color(0.55f, 1f, 0.2f), icon = "ench_mushroom_breaker" },
        new Recipe { id = ItemId.CoalReachEnchant, name = "Coal Reach", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Coal, 2, Mat.Glass, 1), time = 1.5f, desc = "More range, tighter aim. Adds fire.", color = new Color(1f, 0.5f, 0.1f), icon = "ench_coal_reach" },
        new Recipe { id = ItemId.PetalEdgeEnchant, name = "Petal Edge", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Petal, 2, Mat.IronOre, 1), time = 1.5f, desc = "+20% damage.", color = new Color(1f, 0.62f, 0.85f), icon = "ench_petal_edge" },
        new Recipe { id = ItemId.GoldFuryEnchant, name = "Gold Fury", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.GoldOre, 2, Mat.Battery, 1), time = 1.5f, desc = "Shoot 20% faster.", color = new Color(1f, 0.82f, 0.25f), icon = "ench_gold_fury" },
        new Recipe { id = ItemId.UraniumChillEnchant, name = "Uranium Chill", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Uranium, 2, Mat.Crystal, 1), time = 1.5f, desc = "40% less heat. Adds poison.", color = new Color(0.55f, 1f, 0.2f), icon = "ench_uranium_chill" },
        new Recipe { id = ItemId.CopperLeechEnchant, name = "Copper Leech", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.CopperWire, 2, Mat.Berry, 1), time = 1.5f, desc = "Heal 12% of the damage you deal. Adds shock.", color = new Color(0.5f, 0.75f, 1f), icon = "ench_copper_leech" },
        new Recipe { id = ItemId.PlasticBurstEnchant, name = "Plastic Burst", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Plastic, 2, Mat.Coal, 1), time = 1.5f, desc = "A little explosion on every hit. Adds slime.", color = new Color(0.35f, 1f, 0.45f), icon = "ench_plastic_burst" },
        new Recipe { id = ItemId.HerbEchoEnchant, name = "Herb Echo", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Herb, 2, Mat.CopperWire, 1), time = 1.5f, desc = "Hits jump to 1 more target.", color = new Color(0.4f, 0.95f, 0.3f), icon = "ench_herb_echo" },
        new Recipe { id = ItemId.MetalLanceEnchant, name = "Metal Lance", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Metal, 2, Mat.Obsidian, 1), time = 1.5f, desc = "Shots go through targets.", color = new Color(0.75f, 0.78f, 0.82f), icon = "ench_metal_lance" },
        new Recipe { id = ItemId.WoodScatterEnchant, name = "Wood Scatter", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Wood, 2, Mat.Stardust, 1), time = 1.5f, desc = "One extra bullet per shot.", color = new Color(0.72f, 0.5f, 0.3f), icon = "ench_wood_scatter" },
        new Recipe { id = ItemId.MossForceEnchant, name = "Moss Force", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Moss, 2, Mat.Wood, 1), time = 1.5f, desc = "Big knockback.", color = new Color(0.45f, 0.7f, 0.3f), icon = "ench_moss_force" },
        new Recipe { id = ItemId.IronFortuneEnchant, name = "Iron Fortune", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.IronOre, 2, Mat.GoldOre, 1), time = 1.5f, desc = "20% chance of double damage.", color = new Color(0.85f, 0.52f, 0.4f), icon = "ench_iron_fortune" },
        new Recipe { id = ItemId.GlowstoneBreakerEnchant, name = "Glowstone Breaker", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Glowstone, 2, Mat.Metal, 1), time = 1.5f, desc = "80% more damage to labs.", color = new Color(1f, 0.95f, 0.65f), icon = "ench_glowstone_breaker" },
        new Recipe { id = ItemId.CircuitReachEnchant, name = "Circuit Reach", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Circuit, 2, Mat.Glass, 1), time = 1.5f, desc = "More range, tighter aim. Adds shock.", color = new Color(0.5f, 0.75f, 1f), icon = "ench_circuit_reach" },
        new Recipe { id = ItemId.BatteryFuryEnchant, name = "Battery Fury", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Battery, 4), time = 1.5f, desc = "Shoot 20% faster. Adds shock.", color = new Color(0.5f, 0.75f, 1f), icon = "ench_battery_fury" },
        new Recipe { id = ItemId.MeteorChillEnchant, name = "Meteor Chill", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Meteorite, 2, Mat.Crystal, 1), time = 1.5f, desc = "40% less heat. Adds fire.", color = new Color(1f, 0.5f, 0.1f), icon = "ench_meteor_chill" },
        new Recipe { id = ItemId.SlimeLeechEnchant, name = "Slime Leech", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Slime, 2, Mat.Berry, 1), time = 1.5f, desc = "Heal 12% of the damage you deal. Adds slime.", color = new Color(0.35f, 1f, 0.45f), icon = "ench_slime_leech" },
        new Recipe { id = ItemId.ChemicalBurstEnchant, name = "Chemical Burst", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Chemical, 2, Mat.Coal, 1), time = 1.5f, desc = "A little explosion on every hit. Adds poison.", color = new Color(0.55f, 1f, 0.2f), icon = "ench_chemical_burst" },
        new Recipe { id = ItemId.SapEchoEnchant, name = "Sap Echo", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Sap, 2, Mat.CopperWire, 1), time = 1.5f, desc = "Hits jump to 1 more target. Adds slime.", color = new Color(0.35f, 1f, 0.45f), icon = "ench_sap_echo" },
        new Recipe { id = ItemId.PetalLanceEnchant, name = "Petal Lance", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Petal, 2, Mat.Obsidian, 1), time = 1.5f, desc = "Shots go through targets.", color = new Color(1f, 0.62f, 0.85f), icon = "ench_petal_lance" },
        new Recipe { id = ItemId.GoldScatterEnchant, name = "Gold Scatter", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.GoldOre, 2, Mat.Stardust, 1), time = 1.5f, desc = "One extra bullet per shot.", color = new Color(1f, 0.82f, 0.25f), icon = "ench_gold_scatter" },
        new Recipe { id = ItemId.UraniumForceEnchant, name = "Uranium Force", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Uranium, 2, Mat.Wood, 1), time = 1.5f, desc = "Big knockback. Adds poison.", color = new Color(0.55f, 1f, 0.2f), icon = "ench_uranium_force" },
        new Recipe { id = ItemId.CopperFortuneEnchant, name = "Copper Fortune", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.CopperWire, 2, Mat.GoldOre, 1), time = 1.5f, desc = "20% chance of double damage. Adds shock.", color = new Color(0.5f, 0.75f, 1f), icon = "ench_copper_fortune" },
        new Recipe { id = ItemId.PlasticBreakerEnchant, name = "Plastic Breaker", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Plastic, 2, Mat.Metal, 1), time = 1.5f, desc = "80% more damage to labs. Adds slime.", color = new Color(0.35f, 1f, 0.45f), icon = "ench_plastic_breaker" },
        new Recipe { id = ItemId.HerbReachEnchant, name = "Herb Reach", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Herb, 2, Mat.Glass, 1), time = 1.5f, desc = "More range, tighter aim.", color = new Color(0.4f, 0.95f, 0.3f), icon = "ench_herb_reach" },
        new Recipe { id = ItemId.StardustEdgeEnchant, name = "Stardust Edge", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Stardust, 2, Mat.IronOre, 1), time = 1.5f, desc = "+20% damage.", color = new Color(0.85f, 0.7f, 1f), icon = "ench_stardust_edge" },
        new Recipe { id = ItemId.CrystalFuryEnchant, name = "Crystal Fury", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Crystal, 2, Mat.Battery, 1), time = 1.5f, desc = "Shoot 20% faster. Adds ice.", color = new Color(0.55f, 0.92f, 1f), icon = "ench_crystal_fury" },
        new Recipe { id = ItemId.BerryChillEnchant, name = "Berry Chill", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Berry, 2, Mat.Crystal, 1), time = 1.5f, desc = "40% less heat.", color = new Color(0.95f, 0.25f, 0.45f), icon = "ench_berry_chill" },
        new Recipe { id = ItemId.MushroomLeechEnchant, name = "Mushroom Leech", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Mushroom, 2, Mat.Berry, 1), time = 1.5f, desc = "Heal 12% of the damage you deal. Adds poison.", color = new Color(0.55f, 1f, 0.2f), icon = "ench_mushroom_leech" },
        new Recipe { id = ItemId.CoalBurstEnchant, name = "Coal Burst", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Coal, 3), time = 1.5f, desc = "A little explosion on every hit. Adds fire.", color = new Color(1f, 0.5f, 0.1f), icon = "ench_coal_burst" },
        new Recipe { id = ItemId.SulfurEchoEnchant, name = "Sulfur Echo", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Sulfur, 2, Mat.CopperWire, 1), time = 1.5f, desc = "Hits jump to 1 more target. Adds fire.", color = new Color(1f, 0.5f, 0.1f), icon = "ench_sulfur_echo" },
        new Recipe { id = ItemId.ObsidianLanceEnchant, name = "Obsidian Lance", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Obsidian, 4), time = 1.5f, desc = "Shots go through targets.", color = new Color(0.45f, 0.3f, 0.65f), icon = "ench_obsidian_lance" },
        new Recipe { id = ItemId.BatteryScatterEnchant, name = "Battery Scatter", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Battery, 2, Mat.Stardust, 1), time = 1.5f, desc = "One extra bullet per shot. Adds shock.", color = new Color(0.5f, 0.75f, 1f), icon = "ench_battery_scatter" },
        new Recipe { id = ItemId.MeteorForceEnchant, name = "Meteor Force", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Meteorite, 2, Mat.Wood, 1), time = 1.5f, desc = "Big knockback. Adds fire.", color = new Color(1f, 0.5f, 0.1f), icon = "ench_meteor_force" },
        new Recipe { id = ItemId.SlimeFortuneEnchant, name = "Slime Fortune", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Slime, 2, Mat.GoldOre, 1), time = 1.5f, desc = "20% chance of double damage. Adds slime.", color = new Color(0.35f, 1f, 0.45f), icon = "ench_slime_fortune" },
        new Recipe { id = ItemId.ChemicalBreakerEnchant, name = "Chemical Breaker", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Chemical, 2, Mat.Metal, 1), time = 1.5f, desc = "80% more damage to labs. Adds poison.", color = new Color(0.55f, 1f, 0.2f), icon = "ench_chemical_breaker" },
        new Recipe { id = ItemId.SapReachEnchant, name = "Sap Reach", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Sap, 2, Mat.Glass, 1), time = 1.5f, desc = "More range, tighter aim. Adds slime.", color = new Color(0.35f, 1f, 0.45f), icon = "ench_sap_reach" },
        new Recipe { id = ItemId.WoodEdgeEnchant, name = "Wood Edge", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Wood, 2, Mat.IronOre, 1), time = 1.5f, desc = "+20% damage.", color = new Color(0.72f, 0.5f, 0.3f), icon = "ench_wood_edge" },
        new Recipe { id = ItemId.MossFuryEnchant, name = "Moss Fury", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Moss, 2, Mat.Battery, 1), time = 1.5f, desc = "Shoot 20% faster.", color = new Color(0.45f, 0.7f, 0.3f), icon = "ench_moss_fury" },
        new Recipe { id = ItemId.IronChillEnchant, name = "Iron Chill", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.IronOre, 2, Mat.Crystal, 1), time = 1.5f, desc = "40% less heat.", color = new Color(0.85f, 0.52f, 0.4f), icon = "ench_iron_chill" },
        new Recipe { id = ItemId.GlowstoneLeechEnchant, name = "Glowstone Leech", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Glowstone, 2, Mat.Berry, 1), time = 1.5f, desc = "Heal 12% of the damage you deal.", color = new Color(1f, 0.95f, 0.65f), icon = "ench_glowstone_leech" },
        new Recipe { id = ItemId.CircuitBurstEnchant, name = "Circuit Burst", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Circuit, 2, Mat.Coal, 1), time = 1.5f, desc = "A little explosion on every hit. Adds shock.", color = new Color(0.5f, 0.75f, 1f), icon = "ench_circuit_burst" },
        new Recipe { id = ItemId.GlassEchoEnchant, name = "Glass Echo", kind = ItemKind.Enchant, station = Station.Enchant, cost = Cost(Mat.Glass, 2, Mat.CopperWire, 1), time = 1.5f, desc = "Hits jump to 1 more target.", color = new Color(0.75f, 0.95f, 1f), icon = "ench_glass_echo" },

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
