using System.Collections.Generic;
using UnityEngine;

// THE GUN TABLE.
// Every gun (except the 4 originals, which have their own code) is ONE entry below. Change a number and the gun changes.
// To add a new gun:
//   1. add its name to ItemId (Items.cs, at the END of the list)
//   2. add its recipe to Items.All (Items.cs, "More guns")
//   3. add a GunDef here
// That's it - firing, damage, the model in your hands and the hotbar all come from this.
//
//   mode       Hitscan = instant bullet/laser.  Beam = short spray you hold (flamethrower).  Projectile = a flying shot.
//   look       which kind of model is built in your scientist's hands (Pistol, Rifle, Shotgun, ...)
//   damage     per bullet / per pellet / per beam tick
//   cooldown   seconds between shots (lower = faster)
//   heat       heat added per shot; 100 = overheated
//   auto       true = hold to keep firing, false = click for every shot
//   pellets    bullets per shot (shotguns)      spread  how wide they go (degrees)
//   burst      shots fired by one click (burst guns)
//   pierce     goes through targets              chain   jumps to this many more targets (lightning)
//   speed / gravity / blast / size / puddle      for Projectile guns (blast > 0 = it explodes)
//   structure  extra damage against labs (1 = normal)
//   element    Fire / Ice / Poison / Slime / Shock built in (an enchant replaces it)

public enum GunMode { Hitscan, Beam, Projectile }
public enum GunLook { Pistol, Revolver, SMG, Rifle, Musket, Shotgun, Sniper, Railgun, Launcher, Sprayer, Blaster, Coil }

public class GunDef
{
    public ItemId id;
    public GunMode mode;
    public GunLook look;
    public float damage = 10f, cooldown = 0.2f, heat = 8f, range = 90f;
    public bool auto = true;
    public int pellets = 1;
    public float spread;
    public int burst = 1;
    public float burstGap = 0.08f;
    public bool pierce;
    public int chain;
    public float force = 0.8f;
    public float speed = 60f, gravity, blast, size = 0.14f, life = 3f;
    public bool puddle, star;
    public float structure = 1f;
    public Element element;
    public Color color = Color.white;           // shots and glowing bits
    public Color body = new Color(0.22f, 0.24f, 0.27f);   // the gun itself
    public Color trim = new Color(0.75f, 0.77f, 0.8f);
    public string sound = "zap";
    public float pitch = 1f, volume = 0.55f, kick = 0.4f;
    public bool lightning;                      // zig-zag tracer
}

public static class Guns
{
    static readonly Color Wood = new Color(0.55f, 0.36f, 0.2f), Steel = new Color(0.22f, 0.24f, 0.27f), Light = new Color(0.75f, 0.77f, 0.8f);

    public static readonly GunDef[] All =
    {
        new GunDef { id = ItemId.BerryBlaster, mode = GunMode.Projectile, look = GunLook.SMG,
            damage = 8f, cooldown = 0.13f, heat = 7f, speed = 55f, gravity = -3f, size = 0.16f,
            color = new Color(0.95f, 0.2f, 0.4f), body = new Color(0.5f, 0.12f, 0.25f), trim = new Color(0.4f, 0.8f, 0.3f), sound = "pop", pitch = 1.3f, kick = 0.25f },

        new GunDef { id = ItemId.WoodPistol, mode = GunMode.Hitscan, look = GunLook.Pistol, auto = false,
            damage = 17f, cooldown = 0.26f, heat = 9f, range = 70f, spread = 0.6f,
            color = new Color(1f, 0.85f, 0.55f), body = Wood, trim = Steel, sound = "turret", pitch = 1.2f, kick = 0.45f },

        new GunDef { id = ItemId.SapSprayer, mode = GunMode.Beam, look = GunLook.Sprayer,
            damage = 2.2f, cooldown = 0.08f, heat = 3.2f, range = 10f, element = Element.Slime,
            color = new Color(1f, 0.7f, 0.2f), body = new Color(0.45f, 0.3f, 0.15f), trim = Light, sound = "slime_fire", pitch = 1.5f, volume = 0.3f, kick = 0.05f },

        new GunDef { id = ItemId.SporeGun, mode = GunMode.Projectile, look = GunLook.Launcher,
            damage = 14f, cooldown = 0.7f, heat = 22f, speed = 22f, gravity = -12f, blast = 2.6f, size = 0.22f, element = Element.Poison, structure = 1.2f,
            color = new Color(1f, 0.45f, 0.4f), body = new Color(0.85f, 0.82f, 0.72f), trim = new Color(0.9f, 0.3f, 0.3f), sound = "slime_fire", pitch = 0.7f, kick = 0.8f },

        new GunDef { id = ItemId.MossMusket, mode = GunMode.Hitscan, look = GunLook.Musket, auto = false,
            damage = 48f, cooldown = 1.25f, heat = 30f, range = 110f, structure = 1.5f, force = 4f,
            color = new Color(0.6f, 0.9f, 0.4f), body = Wood, trim = new Color(0.4f, 0.65f, 0.3f), sound = "turret", pitch = 0.6f, volume = 0.8f, kick = 1.3f },

        new GunDef { id = ItemId.PetalPopper, mode = GunMode.Hitscan, look = GunLook.Shotgun, auto = false,
            damage = 7f, pellets = 6, spread = 6f, cooldown = 0.75f, heat = 20f, range = 20f, force = 1.5f,
            color = new Color(1f, 0.62f, 0.85f), body = new Color(0.95f, 0.75f, 0.85f), trim = new Color(0.4f, 0.8f, 0.35f), sound = "pop", pitch = 0.9f, volume = 0.75f, kick = 1f },

        new GunDef { id = ItemId.CoalCannon, mode = GunMode.Projectile, look = GunLook.Launcher, auto = false,
            damage = 34f, cooldown = 1.1f, heat = 30f, speed = 26f, gravity = -10f, blast = 3.2f, size = 0.3f, element = Element.Fire, structure = 2.4f, force = 9f,
            color = new Color(1f, 0.45f, 0.15f), body = new Color(0.12f, 0.12f, 0.13f), trim = new Color(0.8f, 0.6f, 0.25f), sound = "plasma_fire", pitch = 0.7f, kick = 1.4f },

        new GunDef { id = ItemId.IronRifle, mode = GunMode.Hitscan, look = GunLook.Rifle,
            damage = 10f, cooldown = 0.11f, heat = 6f, range = 100f, spread = 1.2f,
            color = new Color(1f, 0.8f, 0.5f), body = new Color(0.45f, 0.3f, 0.25f), trim = new Color(0.85f, 0.52f, 0.4f), sound = "turret", pitch = 1f, kick = 0.3f },

        new GunDef { id = ItemId.GoldRevolver, mode = GunMode.Hitscan, look = GunLook.Revolver, auto = false,
            damage = 30f, cooldown = 0.5f, heat = 12f, range = 90f, force = 2f,
            color = new Color(1f, 0.85f, 0.35f), body = new Color(0.95f, 0.75f, 0.25f), trim = Wood, sound = "turret", pitch = 0.8f, volume = 0.7f, kick = 0.9f },

        new GunDef { id = ItemId.SulfurFlamer, mode = GunMode.Beam, look = GunLook.Sprayer,
            damage = 2.6f, cooldown = 0.08f, heat = 3.5f, range = 9f, element = Element.Fire, structure = 1.5f,
            color = new Color(1f, 0.55f, 0.1f), body = new Color(0.85f, 0.8f, 0.2f), trim = Steel, sound = "toxic_hiss", pitch = 0.8f, volume = 0.35f, kick = 0.05f },

        new GunDef { id = ItemId.GlowstoneLaser, mode = GunMode.Hitscan, look = GunLook.Blaster,
            damage = 7f, cooldown = 0.09f, heat = 5f, range = 130f,
            color = new Color(1f, 0.95f, 0.6f), body = Light, trim = new Color(1f, 0.9f, 0.5f), sound = "laser", pitch = 1.3f, volume = 0.4f, kick = 0.15f },

        new GunDef { id = ItemId.UraniumRailgun, mode = GunMode.Hitscan, look = GunLook.Railgun, auto = false,
            damage = 75f, cooldown = 1.7f, heat = 60f, range = 200f, pierce = true, element = Element.Poison, structure = 2f, force = 6f,
            color = new Color(0.55f, 1f, 0.2f), body = Steel, trim = new Color(0.3f, 0.5f, 0.2f), sound = "plasma_fire", pitch = 1.8f, volume = 0.8f, kick = 1.6f },

        new GunDef { id = ItemId.ObsidianShotgun, mode = GunMode.Hitscan, look = GunLook.Shotgun, auto = false,
            damage = 8f, pellets = 8, spread = 7f, cooldown = 0.95f, heat = 25f, range = 24f, force = 2.5f,
            color = new Color(0.75f, 0.5f, 1f), body = new Color(0.1f, 0.07f, 0.15f), trim = new Color(0.6f, 0.4f, 0.85f), sound = "explode", pitch = 1.6f, volume = 0.45f, kick = 1.3f },

        new GunDef { id = ItemId.CircuitBlaster, mode = GunMode.Hitscan, look = GunLook.Blaster, auto = false,
            damage = 11f, burst = 3, burstGap = 0.08f, cooldown = 0.55f, heat = 14f, range = 80f, element = Element.Shock, lightning = true,
            color = new Color(0.3f, 1f, 0.5f), body = new Color(0.1f, 0.4f, 0.2f), trim = new Color(0.8f, 0.65f, 0.2f), sound = "zap", pitch = 1.25f, kick = 0.35f },

        new GunDef { id = ItemId.CopperCoil, mode = GunMode.Hitscan, look = GunLook.Coil,
            damage = 14f, chain = 3, cooldown = 0.45f, heat = 14f, range = 45f, element = Element.Shock, lightning = true,
            color = new Color(0.5f, 0.8f, 1f), body = Steel, trim = new Color(0.95f, 0.55f, 0.3f), sound = "zap", pitch = 0.7f, volume = 0.7f, kick = 0.5f },

        new GunDef { id = ItemId.BatterySMG, mode = GunMode.Projectile, look = GunLook.SMG,
            damage = 6f, cooldown = 0.07f, heat = 5.5f, speed = 75f, size = 0.1f, spread = 2f, element = Element.Shock,
            color = new Color(0.4f, 0.65f, 1f), body = new Color(0.2f, 0.35f, 0.75f), trim = Light, sound = "laser", pitch = 1.6f, volume = 0.35f, kick = 0.15f },

        new GunDef { id = ItemId.GlassSniper, mode = GunMode.Hitscan, look = GunLook.Sniper, auto = false,
            damage = 60f, cooldown = 1.4f, heat = 40f, range = 220f, force = 3f,
            color = new Color(0.75f, 0.95f, 1f), body = new Color(0.3f, 0.4f, 0.45f), trim = new Color(0.6f, 0.85f, 0.95f), sound = "turret", pitch = 0.5f, volume = 0.85f, kick = 1.2f },

        new GunDef { id = ItemId.PlasticSquirter, mode = GunMode.Projectile, look = GunLook.Pistol,
            damage = 7f, cooldown = 0.16f, heat = 6f, speed = 26f, gravity = -9f, size = 0.14f, element = Element.Slime,
            color = new Color(0.4f, 0.8f, 1f), body = new Color(0.95f, 0.95f, 0.95f), trim = new Color(1f, 0.55f, 0.2f), sound = "slime_fire", pitch = 1.6f, volume = 0.4f, kick = 0.15f },

        new GunDef { id = ItemId.MeteorLauncher, mode = GunMode.Projectile, look = GunLook.Launcher, auto = false,
            damage = 52f, cooldown = 1.5f, heat = 45f, speed = 32f, gravity = -1.5f, blast = 4.2f, size = 0.34f, element = Element.Fire, structure = 3f, force = 12f, life = 4f,
            color = new Color(1f, 0.4f, 0.2f), body = new Color(0.28f, 0.22f, 0.22f), trim = new Color(1f, 0.4f, 0.2f), sound = "plasma_fire", pitch = 0.55f, volume = 0.8f, kick = 1.8f },

        new GunDef { id = ItemId.StarShooter, mode = GunMode.Projectile, look = GunLook.Blaster,
            damage = 13f, pellets = 3, spread = 5f, cooldown = 0.5f, heat = 15f, speed = 45f, size = 0.18f, star = true,
            color = new Color(0.85f, 0.7f, 1f), body = new Color(0.25f, 0.2f, 0.4f), trim = new Color(1f, 0.9f, 0.5f), sound = "laser", pitch = 2f, volume = 0.5f, kick = 0.4f },
    };

    static Dictionary<ItemId, GunDef> byId;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => byId = null;

    // The gun stats for this item (null = not one of the table guns).
    public static GunDef Get(ItemId id)
    {
        if (byId == null)
        {
            byId = new Dictionary<ItemId, GunDef>();
            foreach (GunDef g in All) byId[g.id] = g;
        }
        return byId.TryGetValue(id, out GunDef d) ? d : null;
    }
}
