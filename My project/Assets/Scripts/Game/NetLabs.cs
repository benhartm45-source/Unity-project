using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// ONLINE PVP: breaking each other's labs and shooting each other.
//
// Each player's game is in charge of THEIR OWN lab. Five times a second it tells everyone how that lab is doing
// (health, forcefield, which upgrades it has, whether it's destroyed, what's stored inside).
// When you shoot a friend's lab, your game sends "I hit lab 2 for 40" to that friend; their game applies the
// damage (so their walls, forcefield and starter shield all count) and the new health comes back to you.
// Shooting a friend's scientist works the same way: your hit is sent to them and their game takes the damage.
// The host passes every message on to everyone else.
public partial class NetLabs : MonoBehaviour
{
    static NetLabs I;
    bool registered;
    float stateCd;

    static NetworkManager NM => NetworkManager.Singleton;

    static readonly ItemId[] Upgrades = { ItemId.WallsI, ItemId.WallsII, ItemId.Forcefield, ItemId.Turret, ItemId.SlimeTraps, ItemId.FastBrew };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => I = null;

    void Awake() => I = this;
    void OnDestroy() { if (I == this) I = null; }

    void Update()
    {
        if (!Net.Active || !MLGame.Playing) { registered = false; return; }
        if (!registered && NM != null && NM.CustomMessagingManager != null)
        {
            Register("labstate", OnLabState);
            Register("labhit", OnLabHit);
            Register("pvphit", OnPvpHit);
            Register("ko", OnKnockout);
            registered = true;
        }
        // tell everyone how my lab is doing
        stateCd -= Time.unscaledDeltaTime;
        Player p = Player.Local;
        if (stateCd <= 0f && p != null && p.MyLab != null)
        {
            stateCd = 0.2f;
            SendState(p.MyLab);
            // the host also runs the abandoned rival labs for everyone
            if (Net.IsHost)
                foreach (Laboratory l in Laboratory.All)
                    if (l != null && HostRuns(l.Index)) SendState(l);
        }
    }

    static void Register(string name, CustomMessagingManager.HandleNamedMessageDelegate h)
    {
        NM.CustomMessagingManager.UnregisterNamedMessageHandler(name);
        NM.CustomMessagingManager.RegisterNamedMessageHandler(name, h);
    }

    // ---------------- sending (host: to everyone; friend: to the host, who passes it on) ----------------

    static void Send(string name, FastBufferWriter w, bool reliable, ulong except = ulong.MaxValue)
    {
        try
        {
            if (NM == null || NM.CustomMessagingManager == null) return;
            NetworkDelivery d = reliable ? NetworkDelivery.ReliableSequenced : NetworkDelivery.UnreliableSequenced;
            if (NM.IsServer)
            {
                foreach (ulong id in NM.ConnectedClientsIds)
                    if (id != NM.LocalClientId && id != except) NM.CustomMessagingManager.SendNamedMessage(name, id, w, d);
            }
            else if (NM.IsConnectedClient) NM.CustomMessagingManager.SendNamedMessage(name, NetworkManager.ServerClientId, w, d);
        }
        finally { w.Dispose(); }
    }

    static FastBufferWriter Writer() => new FastBufferWriter(64, Allocator.Temp, 1024);

    // ---------------- lab state ----------------

    static void SendState(Laboratory lab)
    {
        var w = Writer();
        WriteState(w, lab.Index, lab.hp, lab.maxHp, lab.fieldHp, lab.FieldDownLeft, Mask(lab), lab.Destroyed, lab.storage);
        Send("labstate", w, false);
    }

    static ushort Mask(Laboratory lab)
    {
        ushort m = 0;
        for (int i = 0; i < Upgrades.Length; i++) if (lab.Has(Upgrades[i])) m |= (ushort)(1 << i);
        return m;
    }

    static void WriteState(FastBufferWriter w, int index, float hp, float maxHp, float fieldHp, float fieldDown, ushort mask, bool destroyed, int[] storage)
    {
        w.WriteValueSafe((byte)index);
        w.WriteValueSafe(hp); w.WriteValueSafe(maxHp); w.WriteValueSafe(fieldHp); w.WriteValueSafe(fieldDown);
        w.WriteValueSafe(mask);
        w.WriteValueSafe(destroyed);
        w.WriteValueSafe((byte)storage.Length);
        foreach (int s in storage) w.WriteValueSafe((ushort)Mathf.Clamp(s, 0, 65535));
    }

    void OnLabState(ulong sender, FastBufferReader r)
    {
        r.ReadValueSafe(out byte index);
        r.ReadValueSafe(out float hp); r.ReadValueSafe(out float maxHp); r.ReadValueSafe(out float fieldHp); r.ReadValueSafe(out float fieldDown);
        r.ReadValueSafe(out ushort mask);
        r.ReadValueSafe(out bool destroyed);
        r.ReadValueSafe(out byte n);
        var storage = new int[n];
        for (int i = 0; i < n; i++) { r.ReadValueSafe(out ushort s); storage[i] = s; }

        if (NM != null && NM.IsServer)
        {
            var w = Writer();
            WriteState(w, index, hp, maxHp, fieldHp, fieldDown, mask, destroyed, storage);
            Send("labstate", w, false, sender);
        }

        Player me = Player.Local;
        if (me != null && index == me.labIndex) return;           // my own lab: I'm in charge of it
        if (Net.IsHost && HostRuns(index)) return;                // a rival lab: the host is in charge of it
        Laboratory lab = Find(index);
        if (lab == null) return;
        var built = new List<ItemId>();
        for (int i = 0; i < Upgrades.Length; i++) if ((mask & (1 << i)) != 0) built.Add(Upgrades[i]);
        lab.ApplyNetState(hp, maxHp, fieldHp, fieldDown, built, destroyed, storage);
    }

    // Rival labs (no player in them) belong to the host's game.
    static bool HostRuns(int index) => index >= MLGame.OnlinePlayers;

    static Laboratory Find(int index)
    {
        foreach (Laboratory l in Laboratory.All) if (l != null && l.Index == index) return l;
        return null;
    }

    // ---------------- hitting a friend's lab ----------------

    public static void SendLabHit(int index, float damage, bool forcefield)
    {
        if (I == null || !Net.Active) return;
        var w = Writer();
        w.WriteValueSafe((byte)index);
        w.WriteValueSafe(damage);
        w.WriteValueSafe(forcefield);
        Send("labhit", w, true);
    }

    void OnLabHit(ulong sender, FastBufferReader r)
    {
        r.ReadValueSafe(out byte index);
        r.ReadValueSafe(out float damage);
        r.ReadValueSafe(out bool field);
        if (NM != null && NM.IsServer)
        {
            var w = Writer();
            w.WriteValueSafe(index); w.WriteValueSafe(damage); w.WriteValueSafe(field);
            Send("labhit", w, true, sender);
        }
        Player me = Player.Local;
        Laboratory lab;
        if (me != null && me.labIndex == index) lab = me.MyLab;                          // my lab
        else if (Net.IsHost && HostRuns(index)) lab = Find(index);                       // a rival lab (the host runs those)
        else return;                                                                      // only the owner applies it
        if (lab == null || lab.Destroyed) return;
        if (field) lab.ShieldHit(damage, lab.Center + Vector3.up * 4f, false);
        else lab.TakeHit(new Hit { damage = damage, structureMul = 1f, point = lab.Center, dir = Vector3.down, team = Teams.Neutral });
        stateCd = 0f;      // send the new health straight away
    }

    // ---------------- shooting a friend's scientist ----------------

    public static void SendPlayerHit(int slot, Hit h)
    {
        if (I == null || !Net.Active) return;
        var w = Writer();
        w.WriteValueSafe((byte)slot);
        w.WriteValueSafe(h.damage);
        w.WriteValueSafe((byte)h.element);
        w.WriteValueSafe(h.dir.x); w.WriteValueSafe(h.dir.y); w.WriteValueSafe(h.dir.z);
        w.WriteValueSafe(h.force);
        w.WriteValueSafe(h.dot);
        w.WriteValueSafe((byte)Mathf.Clamp(h.team, 0, 255));
        Send("pvphit", w, true);
    }

    void OnPvpHit(ulong sender, FastBufferReader r)
    {
        r.ReadValueSafe(out byte slot);
        r.ReadValueSafe(out float damage);
        r.ReadValueSafe(out byte element);
        r.ReadValueSafe(out float dx); r.ReadValueSafe(out float dy); r.ReadValueSafe(out float dz);
        r.ReadValueSafe(out float force);
        r.ReadValueSafe(out bool dot);
        r.ReadValueSafe(out byte team);
        if (NM != null && NM.IsServer)
        {
            var w = Writer();
            w.WriteValueSafe(slot); w.WriteValueSafe(damage); w.WriteValueSafe(element);
            w.WriteValueSafe(dx); w.WriteValueSafe(dy); w.WriteValueSafe(dz);
            w.WriteValueSafe(force); w.WriteValueSafe(dot); w.WriteValueSafe(team);
            Send("pvphit", w, true, sender);
        }
        Player me = Player.Local;
        if (me == null || me.labIndex != slot || !me.Alive) return;
        Vector3 dir = new Vector3(dx, dy, dz);
        me.TakeHit(new Hit { damage = damage, element = (Element)element, dir = dir, force = force, dot = dot, team = team, point = me.Center - dir * 2f, structureMul = 1f });
    }
}

// ---------------- who knocked out who (free-for-all) ----------------
public partial class NetLabs
{
    static string SlotName(int slot)
    {
        Player me = Player.Local;
        if (me != null && me.labIndex == slot) return "You";
        if (NetLobby.Instance != null && slot >= 0 && slot < NetLobby.Instance.Members.Count) return NetLobby.Instance.Members[slot].name;
        return "Scientist " + (slot + 1);
    }

    // Called by the scientist who got knocked out: shows it here and tells everyone else.
    public static void SendKnockout(int victim, int killer, bool eliminated)
    {
        ShowKnockout(victim, killer, eliminated);
        if (I == null || !Net.Active) return;
        var w = Writer();
        w.WriteValueSafe((sbyte)victim);
        w.WriteValueSafe((sbyte)killer);
        w.WriteValueSafe(eliminated);
        Send("ko", w, true);
    }

    void OnKnockout(ulong sender, FastBufferReader r)
    {
        r.ReadValueSafe(out sbyte victim);
        r.ReadValueSafe(out sbyte killer);
        r.ReadValueSafe(out bool eliminated);
        if (NM != null && NM.IsServer)
        {
            var w = Writer();
            w.WriteValueSafe(victim); w.WriteValueSafe(killer); w.WriteValueSafe(eliminated);
            Send("ko", w, true, sender);
        }
        ShowKnockout(victim, killer, eliminated);
    }

    static void ShowKnockout(int victim, int killer, bool eliminated)
    {
        Player me = Player.Local;
        bool iWasHit = me != null && me.labIndex == victim;
        bool iDidIt = me != null && me.labIndex == killer;
        string v = SlotName(victim), k = killer >= 0 ? SlotName(killer) : null;
        Color red = new Color(1f, 0.4f, 0.35f), green = new Color(0.5f, 1f, 0.45f);
        if (iDidIt)
        {
            GameHUD.HitMarker(true);
            if (eliminated) GameHUD.Banner("ELIMINATED!", "You took " + v + " out of the game!", green);
            else GameHUD.Toast("You knocked out " + v + "!", green);
            return;
        }
        if (iWasHit)
        {
            if (k != null) GameHUD.Toast(k + " knocked you out!", red);
            return;
        }
        // everyone else: a line in the corner
        string line = k != null ? k + " knocked out " + v : v + " got knocked out";
        if (eliminated) line += " - and is ELIMINATED!";
        GameHUD.Toast(line, eliminated ? new Color(1f, 0.75f, 0.3f) : new Color(1f, 1f, 1f, 0.85f));
    }
}

// The hit-box on a friend's scientist (online). Your shots hit this, and the hit is sent to their game.
public class RemotePlayer : MonoBehaviour, IDamageable
{
    public int slot;
    public bool dead;

    public int Team => slot;
    public bool Alive => !dead;
    public Vector3 Center => transform.position + Vector3.up * 1.1f;

    public void TakeHit(Hit h)
    {
        if (dead || !h.fromPlayer || !MLGame.Playing) return;
        NetLabs.SendPlayerHit(slot, h);
        GameHUD.DamageNumber(h.point, h.damage, h.dot ? Items.ElementColor(h.element) : new Color(1f, 0.5f, 0.45f), true);
        if (!h.dot) GameHUD.HitMarker(false);
    }
}
