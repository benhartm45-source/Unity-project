using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

// ONLINE PLAY OVER A CODE (same setup as Arena Survival's co-op):
// the host makes a game and gets a short join code, friends type the code in and land in the same lobby.
// It uses Unity's free Relay service, so it works across the internet with no router setup.
// (The Unity project has to be linked to Unity Cloud once: Edit > Project Settings > Services.)
// Up to 8 scientists per game. The lobby shows who's in, and when the host presses START everyone
// goes into the arena in their own lab (host = lab 1, the next player = lab 2, ...).
// Other players' positions are shared (see NetPlayers.cs) - fighting each other online comes later.
public static class Net
{
    public const int MaxPlayers = 8;
    public enum State { Offline, Working, Hosting, Joined }

    public static State Mode { get; private set; }
    public static string JoinCode { get; private set; } = "";
    public static string Status { get; private set; } = "";
    public static bool StatusIsError { get; private set; }

    static ISession session;

    // This project may skip the domain reload when pressing Play, so reset everything by hand.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Mode = State.Offline;
        JoinCode = "";
        Status = "";
        StatusIsError = false;
        session = null;
    }

    static NetworkManager NM => NetworkManager.Singleton;

    public static bool Active => NM != null && NM.IsListening && (Mode == State.Hosting || Mode == State.Joined);
    public static bool IsHost => Active && NM.IsServer;
    public static bool IsClient => Active && !NM.IsServer;
    public static bool Connected => Active && (NM.IsServer || NM.IsConnectedClient);

    // Your name in games (saved). Only clean names are ever kept.
    public static string PlayerName
    {
        get
        {
            string saved = PlayerPrefs.GetString("player.name", "");
            if (string.IsNullOrEmpty(saved))
            {
                saved = NameFilter.DefaultName + UnityEngine.Random.Range(10, 100);      // first time: Scientist42
                PlayerPrefs.SetString("player.name", saved);
            }
            return NameFilter.Safe(saved, NameFilter.DefaultName);
        }
        set { if (NameFilter.IsAllowed(value)) { PlayerPrefs.SetString("player.name", NameFilter.Tidy(value).Trim()); PlayerPrefs.Save(); } }
    }

    public static void EnsureNetworkManager()
    {
        if (NM != null) return;
        var go = new GameObject("NetworkManager");
        go.SetActive(false);                       // set everything up before it starts
        var transport = go.AddComponent<UnityTransport>();
        var nm = go.AddComponent<NetworkManager>();
        nm.NetworkConfig = new NetworkConfig
        {
            NetworkTransport = transport,
            EnableSceneManagement = false,         // the game loads its own scenes
            ConnectionApproval = false,
            PlayerPrefab = null,                   // players are built by the game, not spawned by Netcode
        };
        go.AddComponent<NetLobby>();
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.SetActive(true);
    }

    static async Task SignIn()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            // A fresh anonymous player each launch, so two copies on one PC can test together.
            var options = new InitializationOptions();
            options.SetProfile("p" + UnityEngine.Random.Range(0, 99999999));
            await UnityServices.InitializeAsync(options);
        }
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public static async void Host()
    {
        if (Mode != State.Offline) return;
        SetStatus("Creating your game...", false);
        Mode = State.Working;
        try
        {
            EnsureNetworkManager();
            await SignIn();
            var options = new SessionOptions
            {
                MaxPlayers = MaxPlayers,
                Name = PlayerName,
                IsPrivate = true,                  // only joinable with the code
                SessionProperties = new Dictionary<string, SessionProperty>
                {
                    ["v"] = new SessionProperty(Application.version),
                },
            }.WithRelayNetwork();
            session = await MultiplayerService.Instance.CreateSessionAsync(options);
            if (Mode != State.Working) { CancelLate(); return; }   // cancelled meanwhile
            JoinCode = session.Code;
            Mode = State.Hosting;
            SetStatus("Send this code to your friends!", false);
            NetLobby.Instance?.HostStarted();
        }
        catch (Exception e) { Fail(e); }
    }

    public static async void Join(string code)
    {
        if (Mode != State.Offline) return;
        code = (code ?? "").Trim().ToUpperInvariant();
        if (code.Length < 4) { SetStatus("Type the code your friend sent you.", true); return; }
        SetStatus("Joining " + code + "...", false);
        Mode = State.Working;
        try
        {
            EnsureNetworkManager();
            await SignIn();
            session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
            if (Mode != State.Working) { CancelLate(); return; }
            JoinCode = code;
            Mode = State.Joined;
            SetStatus("Connected! Waiting for the host to start...", false);
        }
        catch (Exception e) { Fail(e); }
    }

    // Stop hosting / leave the game. 'reason' is shown on the menu afterwards.
    public static async void Leave(string reason = null)
    {
        bool wasError = reason != null;
        Mode = State.Offline;
        JoinCode = "";
        SetStatus(reason ?? "", wasError);
        NetLobby.Instance?.Clear();
        if (NM != null && NM.IsListening) NM.Shutdown();
        await LeaveSession();
    }

    // Cancel while still connecting.
    public static void Cancel()
    {
        if (Mode != State.Working) return;
        Mode = State.Offline;
        SetStatus("", false);
    }

    static async void CancelLate()
    {
        if (NM != null && NM.IsListening) NM.Shutdown();
        await LeaveSession();
    }

    static async Task LeaveSession()
    {
        ISession s = session;
        session = null;
        if (s == null) return;
        try { await s.LeaveAsync(); }
        catch (Exception) { /* already gone */ }
    }

    static void Fail(Exception e)
    {
        Debug.LogException(e);
        string msg = e.Message ?? "unknown error";
        string lower = msg.ToLowerInvariant();
        if (lower.Contains("project") || lower.Contains("cloud") || lower.Contains("environment"))
            msg = "Online play isn't set up yet (Edit > Project Settings > Services).";
        else if (lower.Contains("not found") || lower.Contains("invalid") || lower.Contains("join code"))
            msg = "No game with that code. Check it and try again.";
        else if (lower.Contains("full"))
            msg = "That game is already full.";
        else if (msg.Length > 90)
            msg = msg.Substring(0, 90) + "...";
        Leave("Couldn't connect: " + msg);
    }

    public static void SetStatus(string text, bool error)
    {
        Status = text;
        StatusIsError = error;
    }
}

// Lives on the NetworkManager. Keeps the lobby in sync: everyone sends the host their name,
// the host sends everyone the list of who's in, and the host's START reaches everyone.
public class NetLobby : MonoBehaviour
{
    public static NetLobby Instance { get; private set; }

    public struct Member { public ulong id; public string name; public bool host; }
    public readonly List<Member> Members = new List<Member>();

    // Set when the host presses START (the menu shows it; the arena will use it later).
    public static event Action GameStarted;

    NetworkManager NM => NetworkManager.Singleton;
    bool helloSent;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; GameStarted = null; }

    void Awake()
    {
        Instance = this;
        NetworkManager nm = GetComponent<NetworkManager>();
        nm.OnClientStarted += RegisterHandlers;
        nm.OnServerStarted += RegisterHandlers;
        nm.OnClientConnectedCallback += OnClientConnected;
        nm.OnClientDisconnectCallback += OnClientDisconnected;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    public void Clear()
    {
        Members.Clear();
        helloSent = false;
    }

    // Host: put yourself in the list.
    public void HostStarted()
    {
        Members.Clear();
        Members.Add(new Member { id = NM != null ? NM.LocalClientId : 0, name = Net.PlayerName, host = true });
    }

    void Update()
    {
        // Friend: once connected, tell the host your name (once).
        if (Net.IsClient && NM.IsConnectedClient && !helloSent)
        {
            helloSent = true;
            var w = Writer();
            w.WriteValueSafe(Net.PlayerName);
            Send("hello", w);
        }
        // Host: if the list was emptied somehow, put yourself back.
        if (Net.IsHost && Members.Count == 0) HostStarted();
    }

    // ---------- Host actions ----------

    public void StartGame()
    {
        if (!Net.IsHost) return;
        var w = Writer();
        w.WriteValueSafe((byte)1);
        Send("start", w);
        GameStarted?.Invoke();
    }

    // ---------- Messages ----------

    static FastBufferWriter Writer() => new FastBufferWriter(64, Allocator.Temp, 4096);

    // Host: to everyone else. Friend: to the host.
    void Send(string name, FastBufferWriter w)
    {
        try
        {
            if (!Net.Active || NM.CustomMessagingManager == null) return;
            if (!NM.IsServer && !NM.IsConnectedClient) return;
            CustomMessagingManager cmm = NM.CustomMessagingManager;
            if (NM.IsServer)
            {
                foreach (ulong id in NM.ConnectedClientsIds)
                    if (id != NM.LocalClientId) cmm.SendNamedMessage(name, id, w, NetworkDelivery.ReliableSequenced);
            }
            else cmm.SendNamedMessage(name, NetworkManager.ServerClientId, w, NetworkDelivery.ReliableSequenced);
        }
        finally { w.Dispose(); }
    }

    void RegisterHandlers()
    {
        CustomMessagingManager cmm = NM != null ? NM.CustomMessagingManager : null;
        if (cmm == null) return;
        Register(cmm, "hello", OnHello);
        Register(cmm, "roster", OnRoster);
        Register(cmm, "start", OnStart);
    }

    static void Register(CustomMessagingManager cmm, string name, CustomMessagingManager.HandleNamedMessageDelegate handler)
    {
        cmm.UnregisterNamedMessageHandler(name);
        cmm.RegisterNamedMessageHandler(name, handler);
    }

    // Host: someone told us their name.
    void OnHello(ulong sender, FastBufferReader r)
    {
        if (!Net.IsHost) return;
        r.ReadValueSafe(out string name);
        name = NameFilter.Safe(name, "Scientist");
        Members.RemoveAll(m => m.id == sender);
        Members.Add(new Member { id = sender, name = name, host = false });
        SendRoster();
        Net.SetStatus(name + " joined!", false);
        MainMenu.Ping();
    }

    void SendRoster()
    {
        var w = Writer();
        w.WriteValueSafe((byte)Members.Count);
        foreach (Member m in Members)
        {
            w.WriteValueSafe(m.id);
            w.WriteValueSafe(m.name);
            w.WriteValueSafe(m.host);
        }
        Send("roster", w);
    }

    // Friend: the host sent the list of who's in.
    void OnRoster(ulong sender, FastBufferReader r)
    {
        if (!Net.IsClient) return;
        r.ReadValueSafe(out byte n);
        Members.Clear();
        for (int i = 0; i < n; i++)
        {
            r.ReadValueSafe(out ulong id);
            r.ReadValueSafe(out string name);
            r.ReadValueSafe(out bool host);
            Members.Add(new Member { id = id, name = NameFilter.Safe(name, "Scientist"), host = host });
        }
        MainMenu.Ping();
    }

    void OnStart(ulong sender, FastBufferReader r)
    {
        if (!Net.IsClient) return;
        GameStarted?.Invoke();
    }

    void OnClientConnected(ulong id)
    {
        if (NM == null || !NM.IsServer || id == NM.LocalClientId) return;
        SendRoster();      // they'll get the full list (and again once their name arrives)
    }

    void OnClientDisconnected(ulong id)
    {
        if (NM == null) return;
        if (NM.IsServer)
        {
            int i = Members.FindIndex(m => m.id == id);
            if (i >= 0)
            {
                Net.SetStatus(Members[i].name + " left.", false);
                Members.RemoveAt(i);
                SendRoster();
            }
            return;
        }
        // We're the friend and the host is gone (or we got dropped).
        if (Net.Mode == Net.State.Joined || Net.Mode == Net.State.Working)
            Net.Leave("The host closed the game.");
    }
}
