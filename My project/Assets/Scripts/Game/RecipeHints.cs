using System.Collections.Generic;
using System.Text;
using UnityEngine;

// HINTS FOR THE SECRET RECIPES.
//
//   Hint scrolls   Every match, a few glowing scrolls are hidden around the map in NEW random places.
//                  Press E on one and it tells you part of a recipe you HAVEN'T found yet (picked at random),
//                  like "Something icy comes from Crystal + Glass + ?". Hints last for this match only
//                  and show up in your Recipe Book (B).
//   "Close!"       When a mix at a station fizzles, Close() checks if it was one material away from a real recipe.
public static class RecipeHints
{
    public class Hint
    {
        public Recipe recipe;
        public string text;
    }

    public const int ScrollsPerMatch = 10;

    static readonly List<Hint> hints = new List<Hint>();
    public static IReadOnlyList<Hint> All => hints;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => hints.Clear();

    public static void NewMatch() => hints.Clear();

    public static Hint For(ItemId id)
    {
        foreach (Hint h in hints) if (h.recipe.id == id) return h;
        return null;
    }

    // A scroll was read: hint a random recipe you haven't found (and haven't had a hint for).
    public static Hint Reveal()
    {
        var pool = new List<Recipe>();
        foreach (Recipe r in Items.All)
        {
            if (r.id == ItemId.Zapper || r.station == Station.Storage) continue;
            if (r.hideRecipe || Discoveries.Knows(r.id) || For(r.id) != null) continue;
            pool.Add(r);
        }
        if (pool.Count == 0) return null;
        Recipe pick = pool[Random.Range(0, pool.Count)];
        var h = new Hint { recipe = pick, text = Describe(pick) };
        hints.Add(h);
        return h;
    }

    // "Something icy comes from Crystal + Glass + ?   (Workbench · 5 materials)"
    static string Describe(Recipe r)
    {
        var mats = new List<int>();
        int total = 0;
        for (int m = 0; m < Items.MatCount; m++) if (r.cost[m] > 0) { mats.Add(m); total += r.cost[m]; }
        // show some of the ingredients, hide the rest (always hide at least one when there are 2+)
        Shuffle(mats);
        int show = mats.Count <= 1 ? mats.Count : mats.Count == 2 ? 1 : mats.Count - 1;
        var shown = mats.GetRange(0, show);
        shown.Sort();
        var sb = new StringBuilder();
        sb.Append("Something ").Append(Adjective(r)).Append(" comes from ");
        for (int i = 0; i < shown.Count; i++)
        {
            if (i > 0) sb.Append(" + ");
            sb.Append(Items.MatNames[shown[i]]);
        }
        for (int i = show; i < mats.Count; i++) sb.Append(" + ?");
        sb.Append("...   (").Append(LabStation.Names[(int)r.station]).Append(" · ").Append(total).Append(" materials in total)");
        return sb.ToString();
    }

    static void Shuffle(List<int> l)
    {
        for (int i = l.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int t = l[i]; l[i] = l[j]; l[j] = t;
        }
    }

    // "icy", "fiery", "explosive", "protective", ...
    static string Adjective(Recipe r)
    {
        Element e = ElementOf(r);
        string el = e == Element.Fire ? "fiery" : e == Element.Ice ? "icy" : e == Element.Poison ? "toxic" : e == Element.Slime ? "slimy" : e == Element.Shock ? "shocking" : null;
        switch (r.kind)
        {
            case ItemKind.Weapon: return (el ?? "dangerous") + " you can shoot";
            case ItemKind.Armor: return (el != null ? el + "-proof" : "tough") + " you can wear";
            case ItemKind.Potion: return (el != null ? el + "-proof" : "bubbly") + " you can drink";
            case ItemKind.Throwable: return (el ?? "explosive") + " you can throw";
            case ItemKind.Enchant: return (el != null ? el + " and magical" : "magical") + " for your gun";
            default: return "useful for your lab";
        }
    }

    public static Element ElementOf(Recipe r)
    {
        switch (r.kind)
        {
            case ItemKind.Weapon:
                {
                    GunDef g = Guns.Get(r.id);
                    return g != null ? g.element : Element.None;
                }
            case ItemKind.Armor:
                {
                    ArmorDef a = Gear.Armor(r.id);
                    return a != null ? a.immune : Element.None;
                }
            case ItemKind.Potion:
            case ItemKind.Throwable:
                {
                    PotionDef p = Gear.Potion(r.id);
                    if (p == null) return Element.None;
                    return p.bomb != null ? p.bomb.element : p.immune;
                }
            case ItemKind.Enchant:
                {
                    EnchantDef d = Gear.Enchant(r.id);
                    return d != null ? d.element : Element.None;
                }
        }
        return Element.None;
    }

    // ---------------- "Close!" ----------------

    // How far a mix is from a recipe: 0 = exact, 1 = one material too many or too few, ...
    public static int Distance(Recipe r, int[] mix)
    {
        int d = 0;
        for (int m = 0; m < Items.MatCount; m++) d += Mathf.Abs(r.cost[m] - mix[m]);
        return d;
    }

    // A message for a mix that fizzled (null = not close to anything).
    public static string Close(Station st, int[] mix)
    {
        Recipe one = null, oneKnown = null;
        bool warm = false;
        foreach (Recipe r in Items.For(st))
        {
            if (r.hideRecipe) continue;
            int d = Distance(r, mix);
            if (d == 1)
            {
                if (Discoveries.Knows(r.id)) { if (oneKnown == null) oneKnown = r; }
                else if (one == null) one = r;
            }
            else if (d == 2 && !Discoveries.Knows(r.id)) warm = true;
        }
        Recipe hit = one ?? oneKnown;
        if (hit != null)
        {
            int have = 0, need = 0;
            for (int m = 0; m < Items.MatCount; m++) { have += mix[m]; need += hit.cost[m]; }
            string how = need > have ? "ONE more material is missing." : "there's ONE material too many.";
            if (one != null) return "SO CLOSE!  Something new is almost happening - " + how;
            return "CLOSE!  That's nearly your " + hit.name + " (you already know it) - " + how;
        }
        if (warm) return "Getting warmer...  This mix is only 2 materials away from a secret recipe.";
        return null;
    }
}

// A glowing scroll lying somewhere on the map. Press E to read it.
public class HintScroll : MonoBehaviour, IInteractable
{
    public static readonly List<HintScroll> All = new List<HintScroll>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => All.Clear();

    static readonly Color Gold = new Color(1f, 0.82f, 0.35f);

    Transform look;
    Light glow;
    float t, baseY;

    public Vector3 InteractPoint => transform.position + Vector3.up * 0.4f;
    public float InteractRange => 2.6f;

    void OnEnable() { All.Add(this); Interactables.All.Add(this); }
    void OnDisable() { All.Remove(this); Interactables.All.Remove(this); }

    public string Prompt(Player p, out bool usable)
    {
        usable = true;
        return "Read the hint scroll";
    }

    public void Use(Player p)
    {
        RecipeHints.Hint h = RecipeHints.Reveal();
        Sfx.Play("upgrade", 0.6f, 1.3f);
        Sfx.At("pickup_crystal", InteractPoint, 0.8f);
        Fx.Burst(InteractPoint, Gold, 24, 4f, 0.08f, 0.8f, -4f);
        if (h == null) GameHUD.Banner("HINT SCROLL", "It's blank... you've already found every recipe!", Gold);
        else
        {
            GameHUD.Banner("HINT SCROLL", h.text, Gold);
            GameHUD.Toast("Hint saved in your Recipe Book (press B).", Gold);
        }
        Destroy(gameObject);
    }

    void Update()
    {
        t += Time.deltaTime;
        Vector3 p = transform.position;
        p.y = baseY + 0.25f + Mathf.Sin(t * 2f) * 0.12f;
        transform.position = p;
        if (look != null) look.localRotation = Quaternion.Euler(0f, t * 60f, 12f);
        if (glow != null) glow.intensity = 1.6f + 0.5f * Mathf.Sin(t * 3f);
        if (Random.value < Time.deltaTime * 2f)
            Fx.Burst(InteractPoint + Random.insideUnitSphere * 0.5f, Gold, 1, 0.6f, 0.05f, 0.9f, 0.5f);
    }

    static HintScroll Create(Vector3 ground)
    {
        var go = new GameObject("Hint Scroll");
        go.transform.position = ground;
        var s = go.AddComponent<HintScroll>();
        s.baseY = ground.y;
        s.t = Random.value * 10f;
        s.look = new GameObject("look").transform;
        s.look.SetParent(go.transform, false);
        // a rolled-up paper with wooden ends and a red wax seal
        Gfx.Part(s.look, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.22f, 0.32f, 0.22f), Gfx.Lit(new Color(0.93f, 0.85f, 0.65f)), new Vector3(0f, 0f, 90f));
        Gfx.Part(s.look, PrimitiveType.Cylinder, new Vector3(0.36f, 0f, 0f), new Vector3(0.08f, 0.05f, 0.08f), Gfx.Lit(new Color(0.45f, 0.28f, 0.15f)), new Vector3(0f, 0f, 90f));
        Gfx.Part(s.look, PrimitiveType.Cylinder, new Vector3(-0.36f, 0f, 0f), new Vector3(0.08f, 0.05f, 0.08f), Gfx.Lit(new Color(0.45f, 0.28f, 0.15f)), new Vector3(0f, 0f, 90f));
        Gfx.Part(s.look, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.235f, 0.03f, 0.235f), Gfx.Glow(Gold, 2.2f), new Vector3(0f, 0f, 90f), false);
        Gfx.Part(s.look, PrimitiveType.Sphere, new Vector3(0f, 0f, -0.11f), new Vector3(0.09f, 0.09f, 0.04f), Gfx.Lit(new Color(0.8f, 0.1f, 0.12f), 0.6f));
        Gfx.Part(s.look, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 1.1f, Gfx.Clear(new Color(Gold.r, Gold.g, Gold.b, 0.12f)), default, false);
        s.glow = Gfx.AddLight(go.transform, Vector3.up * 0.3f, Gold, 6f, 1.6f);
        Gfx.SetLayer(go, 2);
        return s;
    }

    public static void ClearAll()
    {
        foreach (HintScroll s in All.ToArray()) if (s != null) Destroy(s.gameObject);
    }

    // New match: hide scrolls in random new places (near material spots, away from labs).
    public static void SpawnForMatch()
    {
        ClearAll();
        RecipeHints.NewMatch();
        var spots = new List<Vector3>();
        foreach (MaterialNode n in MaterialNode.All) if (n != null) spots.Add(n.transform.position);
        Vector3 center = MLGame.ArenaCenter;
        float radius = MLGame.ArenaRadius * 0.85f;
        var placed = new List<Vector3>();
        for (int tries = 0; tries < 300 && placed.Count < RecipeHints.ScrollsPerMatch; tries++)
        {
            Vector3 basePos;
            if (spots.Count > 0) basePos = spots[Random.Range(0, spots.Count)];
            else { Vector2 c = Random.insideUnitCircle * radius; basePos = center + new Vector3(c.x, 0f, c.y); }
            Vector2 off = Random.insideUnitCircle.normalized * Random.Range(2.5f, 6f);
            Vector3 p = basePos + new Vector3(off.x, 0f, off.y);
            if (!Physics.Raycast(p + Vector3.up * 40f, Vector3.down, out RaycastHit hit, 120f, Combat.NotPlayer, QueryTriggerInteraction.Ignore)) continue;
            if (hit.normal.y < 0.6f) continue;                       // too steep / hit a wall
            p = hit.point;
            Terrain ter = Terrain.activeTerrain;
            if (ter != null && Mathf.Abs(p.y - (ter.SampleHeight(p) + ter.transform.position.y)) > 1.5f) continue;   // landed on a tree / roof
            Vector3 flat = p - center; flat.y = 0f;
            if (flat.magnitude > radius) continue;
            bool ok = true;
            foreach (Laboratory l in Laboratory.All)
                if (l != null && Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(l.Center.x, 0f, l.Center.z)) < 18f) { ok = false; break; }
            foreach (Vector3 q in placed) if (Vector3.Distance(p, q) < 25f) { ok = false; break; }
            if (!ok) continue;
            placed.Add(p);
            Create(p);
        }
    }
}
