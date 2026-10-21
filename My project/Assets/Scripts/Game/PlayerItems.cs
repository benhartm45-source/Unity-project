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
        if (Model != null) { Model.dead = false; Model.SetArmor(0, false); }
        shownSlot = -1;
        RefreshHeld();
    }

    // Rebuild the model in your hands if the selected thing changed.
    void RefreshHeld()
    {
        HeldItem h = Held;
        Element e = h != null ? h.enchant : Element.None;
        if (shownSlot == selected && shownItem == h && shownEnchant == e) return;
        shownSlot = selected;
        shownItem = h;
        shownEnchant = e;
        Model.SetHeld(h);
    }
    HeldItem shownItem;

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
            float cool = it.overheated ? 45f : 32f;
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
            if (kind == ItemKind.Weapon && firePressed) Firing = FireWeapon(h, fireDown);
            else if (kind == ItemKind.Potion && fireDown) StartDrink(h);
            else if (kind == ItemKind.Throwable && fireDown) StartThrow(h);
        }
        if (!Firing || h == null || h.id != ItemId.FreezeRay) StopBeamSound();

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
        GameHUD.Toast("No Healing Potions - brew some at your Brewing Stand (2 Herb).", Color.white);
    }

    // ---------------- weapons ----------------

    bool FireWeapon(HeldItem h, bool fireDown)
    {
        if (h.overheated)
        {
            if (fireDown) Sfx.Play("overheat_click", 0.5f);
            return false;
        }
        if (h.cooldown > 0f) return h.id == ItemId.FreezeRay;
        Transform muzzle = Model.Muzzle;
        if (muzzle == null) return false;
        Vector3 from = muzzle.position;
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
        Projectile.Fire(Projectile.Kind.Toxic, from, dir * 18f + Vector3.up * 4f, Team, Element.Poison, true);
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
                if (r.id == ItemId.ScrapArmor && armorTier == 2 && armor > 50f) return "You've got better armour on.";
                break;
            case ItemKind.Enchant:
                if (Held == null || Held.R.kind != ItemKind.Weapon) return "Hold a weapon to enchant it.";
                if (Held.enchant == Items.EnchantElement(r.id)) return "Already enchanted with that.";
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
                if (r.id == ItemId.BounceBoots) bounceBoots = true;
                else
                {
                    armorTier = r.id == ItemId.CrystalArmor ? 2 : 1;
                    armor = armorTier == 2 ? 100f : 50f;
                }
                Model.SetArmor(armorTier, bounceBoots);
                break;
            case ItemKind.Enchant:
                if (Held != null && Held.R.kind == ItemKind.Weapon) Held.enchant = Items.EnchantElement(r.id);
                Fx.Burst(Model.Muzzle != null ? Model.Muzzle.position : Center, r.color, 20, 3f, 0.08f, 0.8f, 1f);
                break;
            case ItemKind.Upgrade:
                MyLab.Build(r.id);
                break;
        }
        Sfx.Play("craft_done", 0.8f);
        GameHUD.Toast(r.name + (r.kind == ItemKind.Upgrade ? " built!" : r.kind == ItemKind.Enchant ? " applied!" : " crafted!"), r.color);
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
