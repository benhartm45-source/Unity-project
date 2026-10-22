using UnityEngine;

// WHAT HAPPENS WHEN YOU GET KNOCKED OUT (free-for-all: every scientist is on their own team).
//   - You drop what you're carrying (the materials in your backpack, as before).
//   - You LOSE THE GUN YOU WERE HOLDING (if you weren't holding one, your first gun goes instead).
//     The starter Zapper is never taken, so you always have something to fight back with.
//   - Half of that gun's materials (rounded up) go back into your lab storage.
//   - If your lab is still standing you come back there in a few seconds. If it's destroyed, you're OUT.
//   - Everyone in the game is told who knocked out who (see NetLabs.SendKnockout).
public partial class Player
{
    int lastAttacker = -1;
    float lastAttackerAt = -100f;

    // Remember which other scientist hit us last (so they get the credit).
    void NoteAttacker(Hit h)
    {
        if (h.team >= 0 && h.team < 8 && h.team != labIndex)
        {
            lastAttacker = h.team;
            lastAttackerAt = Time.time;
        }
    }

    void OnKnockedOut(bool canRespawn)
    {
        string lost = LoseGun(canRespawn);
        if (lost != null) GameHUD.Toast(lost, new Color(1f, 0.6f, 0.35f));
        int killer = Time.time - lastAttackerAt < 10f ? lastAttacker : -1;
        lastAttacker = -1;
        NetLabs.SendKnockout(labIndex, killer, !canRespawn);
    }

    // Takes the gun away and gives half its materials back. Returns a message (null = nothing lost).
    string LoseGun(bool canRespawn)
    {
        int slot = -1;
        if (Held != null && Held.R.kind == ItemKind.Weapon && Held.id != ItemId.Zapper) slot = selected;
        else
            for (int i = 0; i < SlotCount; i++)
                if (slots[i] != null && slots[i].R.kind == ItemKind.Weapon && slots[i].id != ItemId.Zapper) { slot = i; break; }
        if (slot < 0) return null;
        Recipe r = slots[slot].R;
        slots[slot] = null;
        // back to a slot that still has something in it
        if (slots[selected] == null)
            for (int i = 0; i < SlotCount; i++) if (slots[i] != null) { selected = i; break; }
        RefreshHeld();

        int back = 0;
        if (canRespawn && MyLab != null && !MyLab.Destroyed)
            for (int m = 0; m < Items.MatCount; m++)
            {
                int n = (r.cost[m] + 1) / 2;
                MyLab.storage[m] += n;
                back += n;
            }
        // (the message never lists WHICH materials - that would give secret recipes away)
        return "You lost your " + r.name + "!" + (back > 0 ? "  " + back + " of its materials went back to your lab." : "");
    }
}
