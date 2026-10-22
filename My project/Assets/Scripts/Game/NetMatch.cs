using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// ONLINE: ONE MATCH FOR EVERYONE.
// The host's game is the referee:
//   - it runs THE match clock (phase, time left, the zone) and sends it 4 times a second - everyone else's
//     clock follows it, so all players are always in the same phase at the same time.
//   - it decides who wins: a player is OUT when their lab is destroyed AND they've been knocked out
//     (or they left the game). When only one player is left, the host tells everyone who won.
// (The abandoned rival labs are also run by the host - see NetLabs.cs.)
public class NetMatch : MonoBehaviour
{
    static NetMatch I;
    bool registered;
    float sendCd;
    bool decided;

    static NetworkManager NM => NetworkManager.Singleton;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => I = null;

    void Awake() => I = this;
    void OnDestroy() { if (I == this) I = null; }

    public static void NewMatch() { if (I != null) { I.decided = false; I.sendCd = 0f; } }

    void Update()
    {
        if (!Net.Active || !MLGame.Playing) { registered = false; return; }
        if (!registered && NM != null && NM.CustomMessagingManager != null)
        {
            NM.CustomMessagingManager.UnregisterNamedMessageHandler("clock");
            NM.CustomMessagingManager.RegisterNamedMessageHandler("clock", OnClock);
            NM.CustomMessagingManager.UnregisterNamedMessageHandler("matchover");
            NM.CustomMessagingManager.RegisterNamedMessageHandler("matchover", OnMatchOver);
            registered = true;
        }
        if (!Net.IsHost) return;

        MLGame gm = MLGame.I;
        sendCd -= Time.unscaledDeltaTime;
        if (sendCd <= 0f)
        {
            sendCd = 0.25f;
            var w = new FastBufferWriter(32, Allocator.Temp);
            w.WriteValueSafe((byte)gm.phase);
            w.WriteValueSafe(gm.phaseLeft);
            w.WriteValueSafe(gm.madnessTime);
            w.WriteValueSafe(gm.matchTime);
            w.WriteValueSafe((byte)Holiday.Current);
            SendAll("clock", w, NetworkDelivery.UnreliableSequenced);
        }
        if (!decided && gm.phase == MLGame.Phase.Madness) CheckWinner(gm);
    }

    // ---------------- host: who's still in? ----------------

    void CheckWinner(MLGame gm)
    {
        int players = MLGame.OnlinePlayers;
        if (players < 2) return;                    // alone online: the normal solo rules
        int left = 0, last = -1;
        for (int slot = 0; slot < players; slot++)
        {
            if (StillIn(slot, gm)) { left++; last = slot; }
        }
        if (left > 1) return;
        decided = true;
        var w = new FastBufferWriter(8, Allocator.Temp);
        w.WriteValueSafe((sbyte)last);
        SendAll("matchover", w, NetworkDelivery.ReliableSequenced);
        gm.NetMatchOver(last);
    }

    static bool StillIn(int slot, MLGame gm)
    {
        Laboratory lab = null;
        foreach (Laboratory l in Laboratory.All) if (l != null && l.Index == slot) lab = l;
        bool labUp = lab != null && !lab.Destroyed;
        Player me = Player.Local;
        if (me != null && me.labIndex == slot) return labUp || me.Alive;
        // a friend: is their scientist still around (and standing)?
        bool seen = NetPlayers.Seen(slot, out bool dead);
        if (!seen) return gm.matchTime < 20f && labUp;      // left the game (or never arrived)
        return labUp || !dead;
    }

    // ---------------- sending / receiving ----------------

    static void SendAll(string name, FastBufferWriter w, NetworkDelivery d)
    {
        try
        {
            if (NM == null || NM.CustomMessagingManager == null || !NM.IsServer) return;
            foreach (ulong id in NM.ConnectedClientsIds)
                if (id != NM.LocalClientId) NM.CustomMessagingManager.SendNamedMessage(name, id, w, d);
        }
        finally { w.Dispose(); }
    }

    void OnClock(ulong sender, FastBufferReader r)
    {
        if (Net.IsHost || MLGame.I == null) return;
        r.ReadValueSafe(out byte phase);
        r.ReadValueSafe(out float phaseLeft);
        r.ReadValueSafe(out float madnessTime);
        r.ReadValueSafe(out float matchTime);
        r.ReadValueSafe(out byte theme);
        Holiday.FollowHost((Holiday.Theme)theme);
        MLGame.I.ApplyNetClock((MLGame.Phase)phase, phaseLeft, madnessTime, matchTime);
    }

    void OnMatchOver(ulong sender, FastBufferReader r)
    {
        if (Net.IsHost || MLGame.I == null) return;
        r.ReadValueSafe(out sbyte winner);
        MLGame.I.NetMatchOver(winner);
    }
}
