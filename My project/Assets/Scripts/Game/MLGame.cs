using System.Collections.Generic;
using UnityEngine;

// THE MATCH: phases, the shrinking zone, raid drones, winning and losing.
//
//   COLLECT     (3 min) run around gathering materials. Every lab has a gold starter shield.
//   EXPERIMENT  (30 s)  last chance to craft and brew before the shields drop.
//   MADNESS             shields are down - labs can be attacked. After a while the zone starts closing in:
//                       outside it you take damage and labs melt down. Last scientist standing wins.
//
// Playing alone: the other 7 labs are abandoned rival labs (with loot and some defences), and waves of
// raid drones attack your lab during MADNESS. Destroy every rival lab to win.
//
// It needs the arena in the scene: in Unity run Tools > Mad Lab > 2. Create Lab Arena once.
public class MLGame : MonoBehaviour
{
    public static MLGame I { get; private set; }
    public static bool Playing => I != null && I.playing;
    public static bool ShieldsUp => I != null && I.playing && I.phase < Phase.Madness;
    public static bool ArenaPresent => Laboratory.All.Count > 0;

    public enum Phase { Collect, Experiment, Madness, Over }
    public const float CollectTime = 180f, ExperimentTime = 30f, ZoneDelay = 40f, ZoneShrinkTime = 230f, ZoneEndRadius = 18f;

    // Raid drones during MADNESS (solo). Switched off for now - set to true to bring them back.
    public static bool DronesEnabled = false;

    public Phase phase { get; private set; }
    public float phaseLeft { get; private set; }
    public float madnessTime { get; private set; }
    public bool won { get; private set; }
    public float zoneRadius { get; private set; }
    public Vector3 zoneCenter { get; private set; }
    public bool ZoneMoving => phase == Phase.Madness && madnessTime > ZoneDelay && zoneRadius > ZoneEndRadius + 0.1f;
    public float ZoneStartsIn => Mathf.Max(0f, ZoneDelay - madnessTime);
    public float ZoneDamage => 4f + madnessTime * 0.03f;

    // online: how many players are in this match (0 = playing alone). Labs 0..OnlinePlayers-1 belong to players.
    public static int OnlinePlayers { get; private set; }
    public string winnerName;           // online: who won (for the end screen)
    bool endShownHere;                  // this player's end screen is up (online the match can carry on without them)

    // stats for the end screen
    public float matchTime;
    public int dronesKilled, labsDestroyed, wave;

    bool playing;
    float overAt = -1f, nextWave;
    Transform zoneWall;
    Material zoneMat;
    readonly List<GameObject> labBackups = new List<GameObject>();
    readonly List<TargetDummy> dummies = new List<TargetDummy>();
    bool backedUp;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => I = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (I != null) return;
        // keep running when the window isn't focused (online, a frozen game falls behind everyone else)
        Application.runInBackground = true;
        var go = new GameObject("Mad Lab Game");
        I = go.AddComponent<MLGame>();
        go.AddComponent<GameHUD>();
        go.AddComponent<NetPlayers>();
        go.AddComponent<NetNodes>();
        go.AddComponent<NetLabs>();
        go.AddComponent<NetMatch>();
        MakeBigPropsGatherable();
        GameCam.Ensure();
        if (!ArenaPresent && GameCam.I != null)
        {
            // no arena yet: keep the plain dark menu background
            GameCam.I.enabled = false;
        }
    }

    // The big crystal clusters, crystal spires, scrap piles and toxic barrels on the map look just like
    // material spots, so they can be gathered too (more crystals / metal / chemicals each time).
    static void MakeBigPropsGatherable()
    {
        foreach (MeshRenderer r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            GameObject g = r.gameObject;
            if (g.GetComponent<MaterialNode>() != null || r.GetComponentInParent<Laboratory>() != null) continue;
            string n = g.name;
            Mat m; float range;
            if (n.StartsWith("Crystal_Spire")) { m = Mat.Crystal; range = 4f; }
            else if (n.StartsWith("Crystal_Cluster")) { m = Mat.Crystal; range = 3.4f; }
            else if (n.StartsWith("Scrap_Pile")) { m = Mat.Metal; range = 3.6f; }
            else if (n.StartsWith("Barrel_Toxic") || n.StartsWith("Barrel_Group")) { m = Mat.Chemical; range = 2.8f; }
            else continue;
            var node = g.AddComponent<MaterialNode>();
            node.mat = m;
            node.range = range;
        }
    }

    // Middle of the map (the "ZoneCenter" marker if there is one).
    public static Vector3 ArenaCenter
    {
        get
        {
            GameObject zc = GameObject.Find("ZoneCenter");
            if (zc != null) return zc.transform.position;
            if (Laboratory.All.Count == 0) return Vector3.zero;
            Vector3 sum = Vector3.zero;
            foreach (Laboratory l in Laboratory.All) sum += l.transform.position;
            return sum / Laboratory.All.Count;
        }
    }

    public static float ArenaRadius
    {
        get
        {
            Terrain t = Terrain.activeTerrain;
            if (t != null && t.terrainData != null) return Mathf.Min(t.terrainData.size.x, t.terrainData.size.z) / 2f;
            return 100f;
        }
    }

    public bool OutsideZone(Vector3 pos)
    {
        if (!playing || phase != Phase.Madness) return false;
        Vector3 d = pos - zoneCenter;
        d.y = 0f;
        return d.magnitude > zoneRadius;
    }

    // ================================================================
    // Starting and ending
    // ================================================================

    public static void StartMatch(int labSlot)
    {
        if (I == null) Boot();
        I.Begin(labSlot);
    }

    void Begin(int labSlot)
    {
        GraphicsQuality.ApplyScene();
        Cleanup();
        RestoreLabs();
        GameCam.Ensure().enabled = true;

        foreach (MaterialNode n in MaterialNode.All) if (n != null) n.ResetNode();

        // your lab
        Laboratory mine = null;
        foreach (Laboratory l in Laboratory.All) if (l.Index == labSlot) mine = l;
        if (mine == null && Laboratory.All.Count > 0) mine = Laboratory.All[0];
        int myIndex = mine != null ? mine.Index : labSlot;
        // online: the labs that belong to the other players are run by THEIR games (NetLabs.cs) - and can be attacked!
        int players = Net.Active && NetLobby.Instance != null ? NetLobby.Instance.Members.Count : 0;
        OnlinePlayers = players;
        // ...and the abandoned rival labs are run by the HOST's game, so they're the same for everyone
        foreach (Laboratory l in Laboratory.All)
        {
            l.ResetLab();
            l.friendly = false;
            l.remote = l != mine && (l.Index < players || Net.IsClient);
            if (l != mine && !l.remote) l.SetupRival(l.Index * 31 + Random.Range(0, 1000));
        }

        Player.Spawn(mine, myIndex);

        // Halloween / winter map (Holiday.cs) - before the lab extras so the radio picks a fitting song
        Holiday.Begin();

        // posters, lava lamps and the trophy shelf in every lab - LabDecor.cs
        // pets, lab levels, the radio and the security screen - LabExtras.cs
        // and the yard outside every lab - LabOutside.cs
        foreach (Laboratory l in Laboratory.All) { LabDecor.Build(l); LabExtras.Build(l); LabOutside.Build(l); }

        // hint scrolls: new random places (and new hints) every match - RecipeHints.cs
        HintScroll.SpawnForMatch();

        // target dummies (at the "DummySpot" markers)
        dummies.Clear();
        foreach (Transform t in FindMarkers("DummySpot"))
            dummies.Add(TargetDummy.Create(t.position, t.eulerAngles.y));
        if (dummies.Count == 0)
        {
            Vector3 c = ArenaCenter;
            for (int i = 0; i < 4; i++) dummies.Add(TargetDummy.Create(Ground(c + new Vector3(-6f + i * 4f, 0f, 14f)), 180f));
        }

        phase = Phase.Collect;
        phaseLeft = CollectTime;
        madnessTime = matchTime = 0f;
        dronesKilled = labsDestroyed = wave = 0;
        zoneCenter = ArenaCenter;
        zoneRadius = ArenaRadius * 0.98f;
        overAt = -1f;
        won = false;
        winnerName = null;
        endShownHere = false;
        NetMatch.NewMatch();
        playing = true;
        Time.timeScale = 1f;
        MakeZoneWall();
        GameHUD.Banner("COLLECT", "Grab materials! Labs are shielded for now.", new Color(0.45f, 1f, 0.35f));
        Sfx.Play("phase", 0.8f);
    }

    public void EndMatch()
    {
        Cleanup();
        playing = false;
        phase = Phase.Over;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (zoneWall != null) Destroy(zoneWall.gameObject);
        zoneWall = null;
    }

    void Cleanup()
    {
        if (Player.Local != null) Destroy(Player.Local.gameObject);
        foreach (RaidDrone d in RaidDrone.All.ToArray()) if (d != null) Destroy(d.gameObject);
        foreach (MaterialPickup p in MaterialPickup.All.ToArray()) if (p != null) Destroy(p.gameObject);
        foreach (SlimePuddle s in SlimePuddle.All.ToArray()) if (s != null && !s.permanent) Destroy(s.gameObject);
        foreach (TargetDummy d in dummies) if (d != null) Destroy(d.gameObject);
        dummies.Clear();
        foreach (Projectile p in FindObjectsByType<Projectile>(FindObjectsSortMode.None)) Destroy(p.gameObject);
        foreach (ToxicCloud c in FindObjectsByType<ToxicCloud>(FindObjectsSortMode.None)) Destroy(c.gameObject);
        HintScroll.ClearAll();
        Holiday.Cleanup();
        GameHUD.CloseAll();
    }

    // Labs get smashed during a match. We keep a hidden copy of each one so the next match starts fresh.
    void RestoreLabs()
    {
        if (!backedUp)
        {
            foreach (Laboratory l in Laboratory.All.ToArray()) Backup(l);
            backedUp = true;
            return;
        }
        foreach (Laboratory l in Laboratory.All.ToArray()) if (l != null) Destroy(l.gameObject);
        Laboratory.All.Clear();
        var fresh = new List<GameObject>(labBackups);
        labBackups.Clear();
        foreach (GameObject b in fresh)
        {
            if (b == null) continue;
            b.SetActive(true);
            Backup(b.GetComponent<Laboratory>());
        }
    }

    void Backup(Laboratory l)
    {
        if (l == null) return;
        GameObject copy = Instantiate(l.gameObject, l.transform.parent);
        copy.name = l.gameObject.name;
        copy.SetActive(false);
        labBackups.Add(copy);
    }

    static List<Transform> FindMarkers(string prefix)
    {
        var list = new List<Transform>();
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (t.name.StartsWith(prefix) && t.gameObject.activeInHierarchy) list.Add(t);
        return list;
    }

    public static Vector3 Ground(Vector3 p)
    {
        Terrain t = Terrain.activeTerrain;
        if (t != null) p.y = t.SampleHeight(p) + t.transform.position.y;
        return p;
    }

    // ================================================================
    // During the match
    // ================================================================

    void Update()
    {
        if (!playing) return;
        float dt = Time.deltaTime;

        bool ui = GameHUD.UIOpen;
        Cursor.lockState = ui ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = ui;

        if (phase == Phase.Over)
        {
            return;
        }
        matchTime += dt;

        // online, a friend's game never changes phase by itself - it waits for the host's clock (NetMatch.cs)
        bool followHost = Net.IsClient;
        switch (phase)
        {
            case Phase.Collect:
                phaseLeft = Mathf.Max(0f, phaseLeft - dt);
                if (phaseLeft <= 0f && !followHost) EnterPhase(Phase.Experiment);
                break;
            case Phase.Experiment:
                phaseLeft = Mathf.Max(0f, phaseLeft - dt);
                if (phaseLeft <= 0f && !followHost) EnterPhase(Phase.Madness);
                break;
            case Phase.Madness:
                madnessTime += dt;
                UpdateZone(dt);
                if (DronesEnabled)          // raid drones are switched off for now (see DronesEnabled)
                {
                    nextWave -= dt;
                    if (nextWave <= 0f) SpawnWave();
                }
                CheckWin();
                break;
        }
        UpdateZoneWall();

        if (overAt > 0f && Time.time >= overAt) { overAt = -1f; GameOver(false); }
    }

    void EnterPhase(Phase p)
    {
        phase = p;
        if (p == Phase.Experiment)
        {
            phaseLeft = ExperimentTime;
            GameHUD.Banner("EXPERIMENT", "Craft and brew! Shields drop in 30 seconds.", new Color(0.7f, 0.45f, 1f));
            Sfx.Play("phase", 0.8f);
        }
        else if (p == Phase.Madness)
        {
            phaseLeft = 0f;
            nextWave = 20f;
            GameHUD.Banner("MADNESS!", "Shields are DOWN. Defend your lab - destroy the others!", new Color(1f, 0.3f, 0.25f));
            Sfx.Play("madness", 0.9f);
        }
    }

    void UpdateZone(float dt)
    {
        if (madnessTime <= ZoneDelay) { zoneRadius = ArenaRadius * 0.98f; return; }
        float k = Mathf.Clamp01((madnessTime - ZoneDelay) / ZoneShrinkTime);
        zoneRadius = Mathf.Lerp(ArenaRadius * 0.98f, ZoneEndRadius, k);
        if (madnessTime - dt <= ZoneDelay) { GameHUD.Warn("THE ZONE IS CLOSING IN!"); Sfx.Play("zone", 0.8f); }
    }

    // Online (friend's game): the host's clock arrived - follow it.
    public void ApplyNetClock(Phase hostPhase, float hostLeft, float hostMadness, float hostMatchTime)
    {
        if (!playing || phase == Phase.Over || hostPhase == Phase.Over) return;
        while (phase < hostPhase) EnterPhase(phase + 1);          // banners for any phase we skipped
        if (hostPhase < phase) phase = hostPhase;                 // (shouldn't happen, but never get ahead of the host)
        phaseLeft = hostLeft;
        float before = madnessTime;
        madnessTime = hostMadness;
        if (phase == Phase.Madness) UpdateZone(Mathf.Max(0f, madnessTime - before));
        matchTime = hostMatchTime;
    }

    // Online: the host says the match is over. winner = the lab slot of the last player standing (-1 = nobody).
    public void NetMatchOver(int winner)
    {
        if (!playing) return;
        Player p = Player.Local;
        bool iWon = p != null && winner == p.labIndex;
        winnerName = winner >= 0 && NetLobby.Instance != null && winner < NetLobby.Instance.Members.Count ? NetLobby.Instance.Members[winner].name : null;
        if (endShownHere && !iWon) { phase = Phase.Over; return; }   // already showing "you're out"
        endShownHere = false;
        GameOver(iWon, true);
    }

    void SpawnWave()
    {
        wave++;
        nextWave = Mathf.Max(18f, 32f - wave * 2f);
        Player p = Player.Local;
        Laboratory target = p != null ? p.MyLab : null;
        if (target == null || target.Destroyed) target = null;
        int n = Mathf.Min(2 + wave, 9);
        List<Transform> spots = FindMarkers("DroneSpawn");
        Vector3 c = ArenaCenter;
        for (int i = 0; i < n; i++)
        {
            Vector3 pos;
            if (spots.Count > 0) pos = spots[Random.Range(0, spots.Count)].position + Random.insideUnitSphere * 4f;
            else pos = c + Quaternion.Euler(0f, Random.value * 360f, 0f) * Vector3.forward * ArenaRadius * 0.9f;
            pos.y = Ground(pos).y + 10f;
            RaidDrone.Spawn(pos, target);
        }
        GameHUD.Warn("RAID DRONES INCOMING!  (wave " + wave + ")");
        Sfx.Play("drone_alarm", 0.7f);
    }

    void CheckWin()
    {
        if (Net.Active && OnlinePlayers >= 2) return;      // online: the host decides (NetMatch.cs)
        Player p = Player.Local;
        if (p == null || !p.Alive) return;
        foreach (Laboratory l in Laboratory.All)
            if (l != null && !l.Destroyed && l.Index != p.labIndex && !l.friendly) return;
        GameOver(true);
    }

    public void PlayerDied(bool canRespawn)
    {
        if (!canRespawn) overAt = Time.time + 3f;
    }

    public void LabDestroyed(Laboratory l)
    {
        Player p = Player.Local;
        if (p == null || l.Index != p.labIndex) labsDestroyed++;
    }

    public void DroneKilled() => dronesKilled++;

    // Practice helper (pause menu): jump straight to the next phase.
    public void SkipPhase()
    {
        if (Net.IsClient) { GameHUD.Toast("Only the host can skip phases online.", Color.white); return; }
        if (phase == Phase.Collect || phase == Phase.Experiment) phaseLeft = 0.01f;
    }

    void GameOver(bool win, bool final = false)
    {
        if (phase == Phase.Over || endShownHere) return;
        won = win;
        // online, if you're knocked out for good the match carries on for everyone else:
        // you see your end screen, but the host's game keeps running the clock and decides the winner
        if (Net.Active && OnlinePlayers >= 2 && !final) endShownHere = true;
        else phase = Phase.Over;
        Sfx.Play(win ? "victory" : "defeat", 0.9f);
        foreach (RaidDrone d in RaidDrone.All.ToArray()) if (d != null) Destroy(d.gameObject);
        GameHUD.ShowEnd(win);
    }

    // ---------------- the zone wall ----------------

    void MakeZoneWall()
    {
        if (zoneWall != null) Destroy(zoneWall.gameObject);
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        DestroyImmediate(g.GetComponent<Collider>());
        g.name = "Zone";
        zoneWall = g.transform;
        zoneMat = new Material(Shader.Find("Sprites/Default"));
        zoneMat.mainTexture = Gfx.HexTexture;
        zoneMat.renderQueue = 3060;
        g.GetComponent<Renderer>().sharedMaterial = zoneMat;
        g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Gfx.SetLayer(g, 2);
        UpdateZoneWall();
    }

    void UpdateZoneWall()
    {
        if (zoneWall == null) return;
        bool show = phase == Phase.Madness;
        zoneWall.gameObject.SetActive(show);
        if (!show) return;
        zoneWall.position = new Vector3(zoneCenter.x, zoneCenter.y + 10f, zoneCenter.z);
        zoneWall.localScale = new Vector3(zoneRadius * 2f, 60f, zoneRadius * 2f);
        float a = ZoneMoving ? 0.22f : 0.12f;
        zoneMat.color = new Color(0.75f, 0.3f, 1f, a * (0.85f + 0.15f * Mathf.Sin(Time.time * 2f)));
        zoneMat.mainTextureScale = new Vector2(zoneRadius * 0.5f, 12f);
        zoneMat.mainTextureOffset = new Vector2(Time.time * 0.02f, Time.time * 0.05f);
    }
}
