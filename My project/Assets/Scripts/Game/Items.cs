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
    public Element enchant;
    public float heat;          // weapons overheat if you fire non-stop
    public bool overheated;
    public float cooldown;
    public Recipe R => Items.Get(id);
}
