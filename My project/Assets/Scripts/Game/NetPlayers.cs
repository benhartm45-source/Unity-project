using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// CO-OP: shows the other scientists in your game.
// 10 times a second everyone sends where they are and what they're doing (running, aiming, what they're holding);
// the host passes it on to everyone else. Each friend appears as their own scientist, in their lab colour,
// with their name above their head. (For now they're just visible - shots don't hit each other online yet.)
public class NetPlayers : MonoBehaviour
{
    class Ghost
    {
        public Scientist model;
        public Transform root;
        public Vector3 pos, vel;
        public float yaw, pitch, last;
        public int held = -1, armor;
        public bool boots;
        public string name;
    }

    readonly Dictionary<int, Ghost> ghosts = new Dictionary<int, Ghost>();
    float sendCd;
    bool registered;

    static NetworkManager NM => NetworkManager.Singleton;

    void Update()
    {
        if (!Net.Active || !MLGame.Playing)
        {
            Clear();
            return;
        }
        if (!registered && NM != null && NM.CustomMessagingManager != null)
        {
            NM.CustomMessagingManager.UnregisterNamedMessageHandler("pose");
            NM.CustomMessagingManager.RegisterNamedMessageHandler("pose", OnPose);
            registered = true;
        }

        sendCd -= Time.unscaledDeltaTime;
        Player p = Player.Local;
        if (sendCd <= 0f && p != null)
        {
            sendCd = 0.1f;
            Send(p);
        }

        // move the ghosts smoothly toward where they were last seen
        var gone = new List<int>();
        foreach (var kv in ghosts)
        {
            Ghost g = kv.Value;
            if (Time.unscaledTime - g.last > 4f) { gone.Add(kv.Key); continue; }
            Vector3 predicted = g.pos + g.vel * Mathf.Min(0.15f, Time.unscaledTime - g.last);
            g.root.position = Vector3.Lerp(g.root.position, predicted, Time.deltaTime * 12f);
            g.model.velocity = g.vel;
            g.model.aimYaw = Mathf.LerpAngle(g.model.aimYaw, g.yaw, Time.deltaTime * 14f);
            g.model.aimPitch = Mathf.Lerp(g.model.aimPitch, g.pitch, Time.deltaTime * 14f);
        }
        foreach (int k in gone) { Destroy(ghosts[k].root.gameObject); ghosts.Remove(k); }
    }

    void Clear()
    {
        foreach (var kv in ghosts) if (kv.Value.root != null) Destroy(kv.Value.root.gameObject);
        ghosts.Clear();
        registered = false;
    }

    void OnDisable() => Clear();

    // ---------------- sending ----------------

    void Send(Player p)
    {
        if (NM == null || NM.CustomMessagingManager == null) return;
        var w = new FastBufferWriter(64, Allocator.Temp, 256);
        try
        {
            Write(w, p.labIndex, p.transform.position, p.Model.velocity, p.Model.aimYaw, p.Model.aimPitch,
                Flags(p.Model.grounded, p.Aiming, p.Sprinting, !p.Alive, p.bounceBoots), p.Held != null ? (int)p.Held.id : -1, p.armorTier);
            CustomMessagingManager cmm = NM.CustomMessagingManager;
            if (NM.IsServer)
            {
                foreach (ulong id in NM.ConnectedClientsIds)
                    if (id != NM.LocalClientId) cmm.SendNamedMessage("pose", id, w, NetworkDelivery.UnreliableSequenced);
            }
            else if (NM.IsConnectedClient) cmm.SendNamedMessage("pose", NetworkManager.ServerClientId, w, NetworkDelivery.UnreliableSequenced);
        }
        finally { w.Dispose(); }
    }

    static byte Flags(bool grounded, bool aiming, bool sprinting, bool dead, bool boots)
        => (byte)((grounded ? 1 : 0) | (aiming ? 2 : 0) | (sprinting ? 4 : 0) | (dead ? 8 : 0) | (boots ? 16 : 0));

    static void Write(FastBufferWriter w, int slot, Vector3 pos, Vector3 vel, float yaw, float pitch, byte flags, int held, int armor)
    {
        w.WriteValueSafe((byte)slot);
        w.WriteValueSafe(pos.x); w.WriteValueSafe(pos.y); w.WriteValueSafe(pos.z);
        w.WriteValueSafe(vel.x); w.WriteValueSafe(vel.y); w.WriteValueSafe(vel.z);
        w.WriteValueSafe(yaw); w.WriteValueSafe(pitch);
        w.WriteValueSafe(flags);
        w.WriteValueSafe((ushort)(held + 1));      // (ushort: there are more than 255 items now)
        w.WriteValueSafe((byte)armor);
    }

    // ---------------- receiving ----------------

    void OnPose(ulong sender, FastBufferReader r)
    {
        r.ReadValueSafe(out byte slot);
        r.ReadValueSafe(out float px); r.ReadValueSafe(out float py); r.ReadValueSafe(out float pz);
        r.ReadValueSafe(out float vx); r.ReadValueSafe(out float vy); r.ReadValueSafe(out float vz);
        r.ReadValueSafe(out float yaw); r.ReadValueSafe(out float pitch);
        r.ReadValueSafe(out byte flags);
        r.ReadValueSafe(out ushort held);
        r.ReadValueSafe(out byte armor);
        Player me = Player.Local;
        if (me != null && slot == me.labIndex) return;

        // host: pass it on to everyone else
        if (NM != null && NM.IsServer)
        {
            var w = new FastBufferWriter(64, Allocator.Temp, 256);
            try
            {
                Write(w, slot, new Vector3(px, py, pz), new Vector3(vx, vy, vz), yaw, pitch, flags, held - 1, armor);
                foreach (ulong id in NM.ConnectedClientsIds)
                    if (id != NM.LocalClientId && id != sender) NM.CustomMessagingManager.SendNamedMessage("pose", id, w, NetworkDelivery.UnreliableSequenced);
            }
            finally { w.Dispose(); }
        }

        if (!ghosts.TryGetValue(slot, out Ghost g))
        {
            g = new Ghost();
            g.root = new GameObject("Friend " + (slot + 1)).transform;
            g.root.position = new Vector3(px, py, pz);
            g.model = Scientist.Create(g.root, Items.LabColors[slot % 8], slot * 7 + 1);
            g.name = NameFor(slot);
            ghosts[slot] = g;
        }
        g.pos = new Vector3(px, py, pz);
        g.vel = new Vector3(vx, vy, vz);
        g.yaw = yaw;
        g.pitch = pitch;
        g.last = Time.unscaledTime;
        g.model.grounded = (flags & 1) != 0;
        g.model.aiming = (flags & 2) != 0;
        g.model.sprinting = (flags & 4) != 0;
        g.model.dead = (flags & 8) != 0;
        bool boots = (flags & 16) != 0;
        int heldId = held - 1;
        if (heldId != g.held)
        {
            g.held = heldId;
            g.model.SetHeld(heldId >= 0 ? new HeldItem { id = (ItemId)heldId } : null);
        }
        if (armor != g.armor || boots != g.boots)
        {
            g.armor = armor;
            g.boots = boots;
            g.model.SetArmor(armor, boots);
        }
    }

    static string NameFor(int slot)
    {
        if (NetLobby.Instance != null && slot < NetLobby.Instance.Members.Count) return NetLobby.Instance.Members[slot].name;
        return "Scientist " + (slot + 1);
    }

    // names above their heads
    void OnGUI()
    {
        if (!MLGame.Playing || GameCam.I == null || GameCam.I.cam == null) return;
        float s = Screen.height / 1080f;
        var style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(18 * s), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        foreach (var kv in ghosts)
        {
            Ghost g = kv.Value;
            Vector3 sp = GameCam.I.cam.WorldToScreenPoint(g.root.position + Vector3.up * 2.2f);
            if (sp.z < 0f || sp.z > 80f) continue;
            Rect r = new Rect(sp.x - 150f * s, Screen.height - sp.y - 15f * s, 300f * s, 30f * s);
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), g.name, style);
            GUI.color = Items.LabColors[kv.Key % 8];
            GUI.Label(r, g.name, style);
            GUI.color = Color.white;
        }
    }
}
