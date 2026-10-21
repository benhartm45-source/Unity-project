using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// CO-OP: MATERIAL SPOTS ARE SHARED.
// The host is in charge of every material spot on the map. When a friend finishes gathering, their game asks the
// host "can I have this?". If the spot still has some left, the host takes it off, gives the friend the material,
// and tells everyone the spot's new size - so it shrinks (and later grows back) on every screen at the same time.
// If two people grab the last bit at the same moment, only the first one gets it.
//
// Spots are matched up by where they are on the map, so everyone needs the SAME version of the map
// (pull from GitHub before playing together).
public class NetNodes : MonoBehaviour
{
    static NetNodes I;
    readonly List<MaterialNode> byId = new List<MaterialNode>();
    readonly HashSet<int> waiting = new HashSet<int>();          // spots we've asked the host about
    readonly HashSet<int> dirty = new HashSet<int>();            // host: spots that changed and need sending
    bool registered, matchSeen, warnedMismatch;
    float snapshotCd;

    static NetworkManager NM => NetworkManager.Singleton;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => I = null;

    void Awake() => I = this;
    void OnDestroy() { if (I == this) I = null; }

    // Give every spot the same number on every computer: sort them by position.
    void Number()
    {
        byId.Clear();
        foreach (MaterialNode n in MaterialNode.All) if (n != null) byId.Add(n);
        byId.Sort((a, b) =>
        {
            Vector3 pa = a.transform.position, pb = b.transform.position;
            int c = Mathf.RoundToInt(pa.x * 10f).CompareTo(Mathf.RoundToInt(pb.x * 10f));
            if (c != 0) return c;
            c = Mathf.RoundToInt(pa.z * 10f).CompareTo(Mathf.RoundToInt(pb.z * 10f));
            if (c != 0) return c;
            return Mathf.RoundToInt(pa.y * 10f).CompareTo(Mathf.RoundToInt(pb.y * 10f));
        });
        for (int i = 0; i < byId.Count; i++) byId[i].netId = i;
        waiting.Clear();
        dirty.Clear();
        warnedMismatch = false;
    }

    void Update()
    {
        bool online = Net.Active && MLGame.Playing;
        if (!online)
        {
            if (matchSeen) { foreach (MaterialNode n in byId) if (n != null) n.netId = -1; byId.Clear(); }
            matchSeen = false;
            registered = false;
            return;
        }
        if (!matchSeen) { matchSeen = true; Number(); }
        if (!registered && NM != null && NM.CustomMessagingManager != null)
        {
            Register("node_req", OnRequest);
            Register("node_grant", OnGrant);
            Register("node_state", OnState);
            registered = true;
        }
        if (!Net.IsHost) return;

        // host: send changes straight away, and the whole map every few seconds (in case anything got lost)
        snapshotCd -= Time.unscaledDeltaTime;
        if (snapshotCd <= 0f)
        {
            snapshotCd = 4f;
            dirty.Clear();
            for (int i = 0; i < byId.Count; i++) dirty.Add(i);
        }
        if (dirty.Count > 0) SendState();
    }

    static void Register(string name, CustomMessagingManager.HandleNamedMessageDelegate h)
    {
        NM.CustomMessagingManager.UnregisterNamedMessageHandler(name);
        NM.CustomMessagingManager.RegisterNamedMessageHandler(name, h);
    }

    // ---------------- called by MaterialNode ----------------

    // Host: a spot changed (gathered or regrown).
    public static void Changed(MaterialNode n)
    {
        if (I == null || !Net.IsHost || n.netId < 0) return;
        I.dirty.Add(n.netId);
    }

    // Friend: we finished gathering this spot - ask the host for it.
    public static void RequestGather(MaterialNode n)
    {
        if (I == null || NM == null || n.netId < 0) return;
        if (I.waiting.Contains(n.netId)) return;
        I.waiting.Add(n.netId);
        var w = new FastBufferWriter(16, Allocator.Temp);
        try
        {
            w.WriteValueSafe((ushort)n.netId);
            w.WriteValueSafe((ushort)I.byId.Count);
            NM.CustomMessagingManager.SendNamedMessage("node_req", NetworkManager.ServerClientId, w, NetworkDelivery.ReliableSequenced);
        }
        finally { w.Dispose(); }
    }

    // ---------------- messages ----------------

    // Host: a friend wants a spot.
    void OnRequest(ulong sender, FastBufferReader r)
    {
        if (!Net.IsHost) return;
        r.ReadValueSafe(out ushort id);
        r.ReadValueSafe(out ushort count);
        bool ok = false;
        int mat = 0, amount = 0;
        if (count == byId.Count && id < byId.Count && byId[id] != null && byId[id].Available)
        {
            MaterialNode n = byId[id];
            n.TakeCharge();
            dirty.Add(id);
            ok = true;
            mat = (int)n.mat;
            amount = n.amount;
        }
        var w = new FastBufferWriter(16, Allocator.Temp);
        try
        {
            w.WriteValueSafe(id);
            w.WriteValueSafe(ok);
            w.WriteValueSafe((byte)mat);
            w.WriteValueSafe((byte)amount);
            NM.CustomMessagingManager.SendNamedMessage("node_grant", sender, w, NetworkDelivery.ReliableSequenced);
        }
        finally { w.Dispose(); }
    }

    // Friend: the host's answer.
    void OnGrant(ulong sender, FastBufferReader r)
    {
        r.ReadValueSafe(out ushort id);
        r.ReadValueSafe(out bool ok);
        r.ReadValueSafe(out byte mat);
        r.ReadValueSafe(out byte amount);
        waiting.Remove(id);
        Player p = Player.Local;
        if (p == null) return;
        if (ok && id < byId.Count && byId[id] != null)
        {
            byId[id].Give(p);
        }
        else if (!ok)
        {
            GameHUD.Toast("Too slow - someone else got there first!", new Color(1f, 0.7f, 0.4f));
            Sfx.Play("error", 0.4f);
        }
    }

    // Host -> everyone: how the spots look now.
    void SendState()
    {
        if (NM == null || NM.CustomMessagingManager == null) { dirty.Clear(); return; }
        var ids = new List<int>(dirty);
        dirty.Clear();
        // in chunks so a message never gets too big
        for (int start = 0; start < ids.Count; start += 200)
        {
            int n = Mathf.Min(200, ids.Count - start);
            var w = new FastBufferWriter(32 + n * 5, Allocator.Temp);
            try
            {
                w.WriteValueSafe((ushort)byId.Count);
                w.WriteValueSafe((ushort)n);
                for (int k = 0; k < n; k++)
                {
                    int id = ids[start + k];
                    MaterialNode node = byId[id];
                    w.WriteValueSafe((ushort)id);
                    w.WriteValueSafe((byte)(node != null ? node.Left : 0));
                    float rg = node != null ? node.RegrowIn : -1f;
                    w.WriteValueSafe((ushort)(rg < 0f ? 0 : Mathf.Clamp(Mathf.CeilToInt(rg * 10f) + 1, 1, 65535)));
                }
                foreach (ulong cid in NM.ConnectedClientsIds)
                    if (cid != NM.LocalClientId) NM.CustomMessagingManager.SendNamedMessage("node_state", cid, w, NetworkDelivery.ReliableFragmentedSequenced);
            }
            finally { w.Dispose(); }
        }
    }

    // Friend: update our spots to match the host.
    void OnState(ulong sender, FastBufferReader r)
    {
        if (Net.IsHost) return;
        r.ReadValueSafe(out ushort total);
        r.ReadValueSafe(out ushort n);
        if (total != byId.Count)
        {
            if (!warnedMismatch)
            {
                warnedMismatch = true;
                GameHUD.Toast("Your map is different from the host's - pull the latest version from GitHub!", new Color(1f, 0.5f, 0.3f));
                Debug.LogWarning("NetNodes: host has " + total + " material spots, you have " + byId.Count + ". Material spots won't be shared.");
            }
            return;
        }
        for (int k = 0; k < n; k++)
        {
            r.ReadValueSafe(out ushort id);
            r.ReadValueSafe(out byte left);
            r.ReadValueSafe(out ushort regrow);
            if (id < byId.Count && byId[id] != null) byId[id].SetState(left, regrow == 0 ? -1f : (regrow - 1) / 10f);
        }
    }
}
