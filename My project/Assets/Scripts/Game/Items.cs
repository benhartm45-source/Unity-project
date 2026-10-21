using System.Collections.Generic;
using UnityEngine;

// EVERYTHING YOU CAN COLLECT AND CRAFT.
// Want to change a recipe, a brew time or a description? It's all in the big list below.
//
// Materials (5):  Herb, Slime, Crystal, Metal, Chemical - gathered from glowing spots around the map.
// Stations (in your lab only):
//   Brewing Stand  - potions (they take a few seconds to brew, you can walk away meanwhile)
//   Workbench      - weapons and armour
//   Enchanting     - puts Fire / Ice / Poison on the weapon you're holding
//   Lab Terminal   - lab upgrades: stronger walls, forcefield, turret, slime traps, repairs
//   Storage        - where your materials go when you walk into your lab

public enum Mat { Herb, Slime, Crystal, Metal, Chemical }
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
}

public class Recipe
{
    public ItemId id;
    public string name, desc;
    public ItemKind kind;
    public Station station;
    public int[] cost;          // Herb, Slime, Crystal, Metal, Chemical
    public float time;          // brewing seconds (potions) or work seconds (everything else)
    public int makes = 1;       // how many you get
    public Color color;
    public string icon;         // picture in Resources/Icons
    public ItemId? needs;       // must have built this first (lab upgrades)
}

public static class Items
{
    public const int MatCount = 5;
    public static readonly string[] MatNames = { "Herb", "Slime", "Crystal", "Metal", "Chemical" };
    public static readonly Color[] MatColors =
    {
        new Color(0.40f, 0.95f, 0.30f),     // herb - leafy green
        new Color(0.30f, 0.95f, 0.75f),     // slime - teal
        new Color(0.40f, 0.85f, 1.00f),     // crystal - ice blue
        new Color(0.75f, 0.78f, 0.82f),     // metal - steel
        new Color(0.80f, 0.40f, 1.00f),     // chemical - purple
    };

    static int[] C(int herb = 0, int slime = 0, int crystal = 0, int metal = 0, int chem = 0) => new[] { herb, slime, crystal, metal, chem };

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
        new Recipe { id = ItemId.BounceBoots, name = "Bounce Boots", kind = ItemKind.Armor, station = Station.Workbench, cost = C(slime: 2, metal: 1), time = 1.5f,
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

    public static Texture2D MatIcon(int m) => Icon("mat_" + MatNames[m].ToLowerInvariant());
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
