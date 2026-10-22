using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// EVERYTHING ON SCREEN DURING A MATCH:
// phase + timer, your lab's health, your health / armour / effects, the hotbar, materials, crosshair,
// "Press E" prompts, the station menus (brewing, workbench, enchanting, storage, lab terminal),
// the pause menu (with the recipes you've discovered), and the win / lose screen.
// Stations don't show recipes: you put materials into the MIX and press COMBINE to find out what they make.
public partial class GameHUD : MonoBehaviour
{
    static GameHUD I;

    // ---------- things other scripts ask for ----------
    struct Toastie { public string text; public Color color; public float at; }
    struct Number { public Vector3 pos; public float value; public Color color; public float at; public Vector2 drift; }
    struct Popup { public int mat, n; public float at; }

    readonly List<Toastie> toasts = new List<Toastie>();
    readonly List<Number> numbers = new List<Number>();
    readonly List<Popup> popups = new List<Popup>();
    string warn, bannerTitle, bannerSub;
    Color bannerColor;
    float warnAt = -10f, bannerAt = -10f, hitAt = -10f, killAt = -10f, dmgAt = -10f;
    float dmgAngle;

    LabStation station;
    int stationSel, knownScroll;
    readonly int[] mix = new int[Items.MatCount];      // what's in the station's mix right now
    const int MatCols = 10;
    bool paused, endShown, endWin, howTo;
    int pauseSel;
    float openedAt;

    float closedAt = -10f;
    // True for a moment after a station closes (so the same E press doesn't open it straight back up).
    public static bool JustClosed => I != null && Time.unscaledTime - I.closedAt < 0.25f;

    public static bool UIOpen => I != null && (I.station != null || I.paused || I.endShown || I.book);
    public static bool Paused => I != null && I.paused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { I = null; styles.Clear(); white = null; }

    void Awake() => I = this;

    public static void Toast(string text, Color color)
    {
        if (I == null) return;
        I.toasts.Add(new Toastie { text = text, color = color, at = Time.unscaledTime });
        if (I.toasts.Count > 4) I.toasts.RemoveAt(0);
    }

    public static void Warn(string text) { if (I != null) { I.warn = text; I.warnAt = Time.unscaledTime; } }

    public static void Banner(string title, string sub, Color c)
    {
        if (I == null) return;
        I.bannerTitle = title; I.bannerSub = sub; I.bannerColor = c; I.bannerAt = Time.unscaledTime;
    }

    public static void DamageNumber(Vector3 pos, float value, Color c, bool fromPlayer)
    {
        if (I == null || !fromPlayer || value < 0.5f) return;
        I.numbers.Add(new Number { pos = pos, value = value, color = c, at = Time.unscaledTime, drift = new Vector2(Random.Range(-30f, 30f), 0f) });
        if (I.numbers.Count > 40) I.numbers.RemoveAt(0);
    }

    public static void HitMarker(bool kill)
    {
        if (I == null) return;
        I.hitAt = Time.unscaledTime;
        if (kill) { I.killAt = Time.unscaledTime; Sfx.Play("kill", 0.6f); }
        else Sfx.Play("hitmarker", 0.25f, Random.Range(0.95f, 1.1f));
    }

    public static void DamageFrom(Vector3 worldPos)
    {
        if (I == null || GameCam.I == null) return;
        Vector3 to = worldPos - GameCam.I.transform.position;
        I.dmgAngle = Vector3.SignedAngle(new Vector3(GameCam.I.transform.forward.x, 0f, GameCam.I.transform.forward.z), new Vector3(to.x, 0f, to.z), Vector3.up);
        I.dmgAt = Time.unscaledTime;
    }

    public static void MaterialPopup(int m, int n)
    {
        if (I == null) return;
        I.popups.Add(new Popup { mat = m, n = n, at = Time.unscaledTime });
        if (I.popups.Count > 5) I.popups.RemoveAt(0);
    }

    public static void OpenStation(LabStation s)
    {
        if (I == null) return;
        I.station = s;
        I.stationSel = 0;
        I.knownScroll = 0;
        I.openedAt = Time.unscaledTime;
        System.Array.Clear(I.mix, 0, I.mix.Length);
    }

    public static void CloseAll()
    {
        if (I == null) return;
        I.station = null;
        I.paused = false;
        I.endShown = false;
        I.howTo = false;
        I.book = false;
        Time.timeScale = 1f;
    }

    public static void ShowEnd(bool win)
    {
        if (I == null) return;
        I.station = null;
        I.paused = false;
        I.book = false;
        I.endShown = true;
        I.endWin = win;
        I.pauseSel = 0;
        I.openedAt = Time.unscaledTime;
    }

    // ================================================================
    // Keys
    // ================================================================

    void Update()
    {
        if (!MLGame.Playing) return;
        var k = Keyboard.current;
        var g = Gamepad.current;
        bool esc = (k != null && k.escapeKey.wasPressedThisFrame) || (g != null && g.startButton.wasPressedThisFrame);
        bool back = (k != null && k.tabKey.wasPressedThisFrame) || (g != null && g.buttonEast.wasPressedThisFrame);

        if (endShown) { MenuKeys(2, ref pauseSel, ChooseEnd); return; }
        if (BookUpdate()) return;          // the recipe book (B) - GameHUD.Book.cs

        if (station != null)
        {
            Player p = Player.Local;
            if (esc || back || p == null || !p.Alive || Vector3.Distance(p.transform.position, station.transform.position) > 5f || station.lab == null || station.lab.Destroyed)
            {
                station = null;
                Sfx.Play("station_close", 0.4f);
                return;
            }
            if (Time.unscaledTime - openedAt < 0.15f) return;
            // E (the same key that opened it) closes it again - no need for Esc, so the mouse stays locked
            if (k != null && k.eKey.wasPressedThisFrame)
            {
                station = null;
                closedAt = Time.unscaledTime;
                Sfx.Play("station_close", 0.4f);
                return;
            }
            if (station.type == Station.Storage) return;
            List<int> mats = StationMats(p);
            List<Recipe> known = Discoveries.Known(station.type);
            int cells = mats.Count + known.Count;
            if (cells > 0)
            {
                stationSel = Mathf.Clamp(stationSel, 0, cells - 1);
                int was = stationSel;
                int cols = stationSel < mats.Count ? MatCols : KnownCols(known.Count);
                if (NavRight()) stationSel = Mathf.Min(cells - 1, stationSel + 1);
                if (NavLeft()) stationSel = Mathf.Max(0, stationSel - 1);
                if (NavDown()) stationSel = Mathf.Min(cells - 1, stationSel + cols);
                if (NavUp()) stationSel = Mathf.Max(0, stationSel - cols);
                if (stationSel != was) Sfx.Play("tick", 0.3f);
                bool confirm = (k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)) || (g != null && g.buttonSouth.wasPressedThisFrame);
                bool remove = (k != null && (k.backspaceKey.wasPressedThisFrame || k.deleteKey.wasPressedThisFrame)) || (g != null && g.buttonWest.wasPressedThisFrame);
                if (confirm)
                {
                    if (stationSel < mats.Count) AddToMix(p, mats[stationSel]);
                    else p.Craft(known[stationSel - mats.Count]);
                }
                if (remove && stationSel < mats.Count) TakeFromMix(mats[stationSel]);
            }
            bool combine = (k != null && (k.cKey.wasPressedThisFrame || k.fKey.wasPressedThisFrame)) || (g != null && g.buttonNorth.wasPressedThisFrame);
            bool clear = (k != null && k.rKey.wasPressedThisFrame) || (g != null && g.leftShoulder.wasPressedThisFrame);
            if (combine) Combine(p);
            if (clear) ClearMix();
            return;
        }

        if (paused)
        {
            if (howTo) { if (esc || back) { howTo = false; Sfx.Play("tick", 0.4f); } return; }
            if (esc) { SetPause(false); return; }
            MenuKeys(7, ref pauseSel, ChoosePause);
            return;
        }
        if (esc) SetPause(true);
    }

    void SetPause(bool on)
    {
        paused = on;
        pauseSel = 0;
        howTo = false;
        Time.timeScale = on && !Net.Active ? 0f : 1f;      // online the game keeps going while you're paused
        Sfx.Play("tick", 0.5f);
    }

    static bool NavDown()
    {
        var k = Keyboard.current; var g = Gamepad.current;
        return (k != null && (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame)) || (g != null && g.dpad.down.wasPressedThisFrame);
    }

    static bool NavRight()
    {
        var k = Keyboard.current; var g = Gamepad.current;
        return (k != null && (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame)) || (g != null && g.dpad.right.wasPressedThisFrame);
    }

    static bool NavLeft()
    {
        var k = Keyboard.current; var g = Gamepad.current;
        return (k != null && (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame)) || (g != null && g.dpad.left.wasPressedThisFrame);
    }

    static bool NavUp()
    {
        var k = Keyboard.current; var g = Gamepad.current;
        return (k != null && (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame)) || (g != null && g.dpad.up.wasPressedThisFrame);
    }

    void MenuKeys(int count, ref int sel, System.Action<int> choose)
    {
        if (NavDown()) { sel = (sel + 1) % count; Sfx.Play("tick", 0.3f); }
        if (NavUp()) { sel = (sel - 1 + count) % count; Sfx.Play("tick", 0.3f); }
        var k = Keyboard.current; var g = Gamepad.current;
        if ((k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)) || (g != null && g.buttonSouth.wasPressedThisFrame)) choose(sel);
    }

    void ChoosePause(int row)
    {
        Sfx.Play("tick", 0.5f);
        switch (row)
        {
            case 0: SetPause(false); break;
            case 1: howTo = true; break;
            case 2: Settings.FirstPerson = !Settings.FirstPerson; Settings.Save(); break;
            case 3: SkipPhase(); SetPause(false); break;
            case 4: GiveTestMaterials(); break;
            case 5:
                {
                    if (Net.IsClient) { Toast("Only the host can restart the match online.", Color.white); break; }
                    SetPause(false);
                    if (Net.IsHost && NetLobby.Instance != null) { NetLobby.Instance.StartGame(); break; }      // restarts it for everyone
                    int lab = Player.Local != null ? Player.Local.labIndex : 0;
                    LoadingScreen.Run("RESTARTING THE EXPERIMENT", () => MLGame.StartMatch(lab));
                    break;
                }
            case 6: SetPause(false); MLGame.I.EndMatch(); break;
        }
    }

    void ChooseEnd(int row)
    {
        Sfx.Play("tick", 0.5f);
        int lab = Player.Local != null ? Player.Local.labIndex : 0;
        if (row == 0 && Net.IsClient) { Toast("Waiting for the host to start the next match...", Color.white); return; }
        endShown = false;
        if (row == 0 && Net.IsHost && NetLobby.Instance != null) NetLobby.Instance.StartGame();      // everyone plays again together
        else if (row == 0) LoadingScreen.Run("NEW EXPERIMENT", () => MLGame.StartMatch(lab));
        else MLGame.I.EndMatch();
    }

    static void SkipPhase() => MLGame.I?.SkipPhase();

    // Practice helper: 10 of every material straight into your lab (solo only).
    static void GiveTestMaterials()
    {
        Player p = Player.Local;
        if (p == null || Net.Active) { Toast("Only in solo practice.", Color.white); return; }
        if (p.MyLab == null || p.MyLab.Destroyed) return;
        for (int m = 0; m < Items.MatCount; m++) p.MyLab.storage[m] += 10;
        Toast("Practice: +10 of every material in your lab storage.", new Color(0.6f, 1f, 0.6f));
    }

    // ================================================================
    // Drawing helpers (everything is laid out on a 1920 x 1080 screen and scaled)
    // ================================================================

    static readonly Dictionary<int, GUIStyle> styles = new Dictionary<int, GUIStyle>();
    static Texture2D white;
    static float S => Screen.height / 1080f;
    static float W => Screen.width / S;
    static Rect R(float x, float y, float w, float h) => new Rect(x * S, y * S, w * S, h * S);

    static Texture2D White
    {
        get
        {
            if (white == null) { white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply(); }
            return white;
        }
    }

    static void Fill(Rect r, Color c)
    {
        Color old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, White);
        GUI.color = old;
    }

    static void Box(float x, float y, float w, float h, Color c) => Fill(R(x, y, w, h), c);

    static GUIStyle Style(int size, bool bold, TextAnchor anchor, bool wrap = false)
    {
        int px = Mathf.Max(8, Mathf.RoundToInt(size * S));
        int key = px * 1000 + (bold ? 500 : 0) + (int)anchor * 2 + (wrap ? 1 : 0);
        if (!styles.TryGetValue(key, out GUIStyle s))
        {
            s = new GUIStyle(GUI.skin.label) { fontSize = px, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal, alignment = anchor, wordWrap = wrap, clipping = TextClipping.Overflow };
            s.normal.textColor = Color.white;
            styles[key] = s;
        }
        return s;
    }

    static void Text(float x, float y, float w, float h, string t, int size, Color c, TextAnchor a = TextAnchor.MiddleLeft, bool bold = true, bool wrap = false)
    {
        Rect r = R(x, y, w, h);
        GUIStyle s = Style(size, bold, a, wrap);
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, c.a * 0.65f);
        GUI.Label(new Rect(r.x + 2f * S, r.y + 2f * S, r.width, r.height), t, s);
        GUI.color = c;
        GUI.Label(r, t, s);
        GUI.color = old;
    }

    static void Bar(float x, float y, float w, float h, float k, Color fg, Color bg)
    {
        Box(x, y, w, h, bg);
        Box(x, y, w * Mathf.Clamp01(k), h, fg);
        Box(x, y, w * Mathf.Clamp01(k), h * 0.35f, new Color(1f, 1f, 1f, 0.18f));
    }

    static void Icon(float x, float y, float size, Texture2D tex, Color fallback)
    {
        if (tex != null)
        {
            Color old = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(R(x, y, size, size), tex, ScaleMode.ScaleToFit);
            GUI.color = old;
        }
        else
        {
            Box(x + size * 0.15f, y + size * 0.15f, size * 0.7f, size * 0.7f, fallback);
        }
    }

    static string Clock(float s)
    {
        s = Mathf.Max(0f, s);
        int m = Mathf.FloorToInt(s / 60f), sec = Mathf.FloorToInt(s % 60f);
        return m + ":" + sec.ToString("00");
    }

    static bool Clicked(Rect virtualRect)
    {
        Event e = Event.current;
        return e.type == EventType.MouseDown && e.button == 0 && R(virtualRect.x, virtualRect.y, virtualRect.width, virtualRect.height).Contains(e.mousePosition);
    }

    static bool Hover(float x, float y, float w, float h) => R(x, y, w, h).Contains(Event.current.mousePosition);

    static readonly Color Panel = new Color(0.04f, 0.06f, 0.08f, 0.86f);
    static readonly Color Dim = new Color(1f, 1f, 1f, 0.55f);

    // ================================================================
    // Drawing
    // ================================================================

    void OnGUI()
    {
        if (!MLGame.Playing) return;
        GUI.depth = 10;
        Player p = Player.Local;
        MLGame gm = MLGame.I;
        if (p == null || gm == null) return;

        DrawWorldMarkers(p);
        DrawDamageNumbers();
        DrawVignette(p);

        if (!endShown)
        {
            DrawPhase(gm);
            DrawLabCard(p);
            DrawHealth(p);
            DrawHotbar(p);
            DrawMaterials(p);
            if (showInfo && station == null && !paused) DrawWeaponInfo(p);
            if (station == null && !paused && !book)
            {
                DrawCrosshair(p);
                DrawPrompt(p);
            }
            DrawToasts();
            DrawWarn();
            DrawBanner();
            DrawDead(p);
        }

        if (station != null) DrawStation(p);
        if (paused) { if (howTo) DrawHowTo(); else DrawPause(); }
        if (book && !endShown && !paused) DrawBook();
        if (endShown) DrawEnd(gm);
    }

    // ---------------- top: phase and timer ----------------

    void DrawPhase(MLGame gm)
    {
        float w = 520f, x = (W - w) / 2f, y = 18f;
        Color c; string name, sub; float k;
        switch (gm.phase)
        {
            case MLGame.Phase.Collect:
                c = new Color(0.45f, 1f, 0.35f); name = "COLLECT"; k = gm.phaseLeft / MLGame.CollectTime;
                sub = "Gather materials  ·  labs are shielded";
                break;
            case MLGame.Phase.Experiment:
                c = new Color(0.72f, 0.45f, 1f); name = "EXPERIMENT"; k = gm.phaseLeft / MLGame.ExperimentTime;
                sub = "Craft and brew!  ·  shields drop soon";
                break;
            default:
                c = new Color(1f, 0.32f, 0.26f); name = "MADNESS"; k = 1f;
                int rivals = 0;
                foreach (Laboratory l in Laboratory.All) if (l != null && !l.Destroyed && !l.friendly && Player.Local != null && l.Index != Player.Local.labIndex) rivals++;
                sub = (gm.ZoneStartsIn > 0f ? "Zone closes in " + Clock(gm.ZoneStartsIn) : gm.ZoneMoving ? "THE ZONE IS CLOSING" : "Zone closed") + "  ·  rival labs left: " + rivals;
                break;
        }
        Box(x, y, w, 70f, Panel);
        Box(x, y, w, 4f, c);
        Text(x + 20f, y + 4f, w, 50f, name, 34, c);
        if (gm.phase < MLGame.Phase.Madness) Text(x, y + 4f, w - 20f, 50f, Clock(gm.phaseLeft), 34, Color.white, TextAnchor.MiddleRight);
        else Text(x, y + 4f, w - 20f, 50f, Clock(gm.matchTime), 26, Dim, TextAnchor.MiddleRight);
        Bar(x, y + 62f, w, 8f, k, c, new Color(0f, 0f, 0f, 0.5f));
        Text(x, y + 74f, w, 30f, sub, 18, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter, false);
        if (gm.wave > 0 && gm.phase == MLGame.Phase.Madness)
            Text(x, y + 100f, w, 26f, "Drone wave " + gm.wave + "  ·  drones downed: " + gm.dronesKilled, 16, Dim, TextAnchor.MiddleCenter, false);
    }

    // ---------------- top-left: your lab ----------------

    void DrawLabCard(Player p)
    {
        Laboratory lab = p.MyLab;
        if (lab == null) return;
        float x = 24f, y = 24f, w = 400f;
        int lines = 2 + (lab.Has(ItemId.Forcefield) ? 1 : 0) + lab.brews.Count;
        float h = 74f + (lab.Has(ItemId.Forcefield) ? 30f : 0f) + lab.brews.Count * 28f;
        Box(x, y, w, h, Panel);
        Box(x, y, 6f, h, lab.LabColor);
        Text(x + 18f, y + 6f, w, 28f, "YOUR LAB  ·  " + lab.ColorName, 20, lab.LabColor);
        if (lab.Destroyed)
        {
            Text(x + 18f, y + 36f, w, 30f, "DESTROYED", 26, new Color(1f, 0.3f, 0.25f));
            return;
        }
        if (lab.StarterShield) Text(x, y + 6f, w - 14f, 28f, "SHIELDED", 16, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleRight);
        else if (p.InsideOwnLab) Text(x, y + 6f, w - 14f, 28f, "YOU'RE INSIDE", 16, new Color(0.6f, 1f, 0.6f), TextAnchor.MiddleRight);
        float k = lab.hp / lab.maxHp;
        Color hc = k > 0.5f ? new Color(0.4f, 0.95f, 0.4f) : k > 0.25f ? new Color(1f, 0.8f, 0.2f) : new Color(1f, 0.3f, 0.25f);
        Bar(x + 18f, y + 40f, w - 36f, 22f, k, hc, new Color(0f, 0f, 0f, 0.6f));
        Text(x + 18f, y + 40f, w - 36f, 22f, Mathf.CeilToInt(lab.hp) + " / " + Mathf.CeilToInt(lab.maxHp), 15, Color.white, TextAnchor.MiddleCenter);
        float yy = y + 70f;
        if (lab.Has(ItemId.Forcefield))
        {
            bool down = lab.fieldHp <= 0f;
            Bar(x + 18f, yy + 4f, w - 36f, 14f, lab.fieldHp / lab.fieldMax, new Color(0.4f, 0.85f, 1f), new Color(0f, 0f, 0f, 0.6f));
            Text(x + 18f, yy, w - 36f, 22f, down ? "FORCEFIELD DOWN" : "FORCEFIELD", 13, Color.white, TextAnchor.MiddleCenter);
            yy += 30f;
        }
        foreach (Laboratory.Brew b in lab.brews)
        {
            Icon(x + 18f, yy, 24f, Items.Icon(b.r.icon), b.r.color);
            Text(x + 48f, yy, 200f, 24f, b.r.name, 15, Color.white, TextAnchor.MiddleLeft, false);
            if (b.done) Text(x, yy, w - 18f, 24f, "READY (hotbar full!)", 14, new Color(1f, 0.8f, 0.3f), TextAnchor.MiddleRight);
            else
            {
                Bar(x + 230f, yy + 8f, 110f, 8f, 1f - b.left / b.total, b.r.color, new Color(0f, 0f, 0f, 0.6f));
                Text(x, yy, w - 18f, 24f, Mathf.CeilToInt(b.left) + "s", 14, Dim, TextAnchor.MiddleRight);
            }
            yy += 28f;
        }
    }

    // ---------------- bottom-left: health, armour, effects ----------------

    void DrawHealth(Player p)
    {
        float x = 30f, y = 1080f - 120f, w = 440f;
        // effects
        var chips = new List<(string, float, Color)>();
        if (p.speedLeft > 0f) chips.Add(("SPEED", p.speedLeft, new Color(1f, 0.85f, 0.2f)));
        if (p.leapLeft > 0f) chips.Add(("LEAP", p.leapLeft, new Color(0.45f, 1f, 0.6f)));
        if (p.ironLeft > 0f) chips.Add(("IRON SKIN", p.ironLeft, new Color(0.75f, 0.8f, 0.9f)));
        if (p.status.burn > 0f) chips.Add(("BURNING", p.status.burn, new Color(1f, 0.5f, 0.1f)));
        if (p.status.poison > 0f) chips.Add(("POISONED", p.status.poison, new Color(0.55f, 1f, 0.2f)));
        if (p.status.freeze > 0f) chips.Add(("FROZEN", p.status.freeze, new Color(0.6f, 0.95f, 1f)));
        else if (p.status.slow > 0f) chips.Add(("SLOWED", p.status.slow, new Color(0.6f, 0.95f, 1f)));
        if (p.DoubleJump) chips.Add(("DOUBLE JUMP", -1f, new Color(0.4f, 1f, 0.5f)));
        p.BuffChips(chips);
        float cx = x, cy = y - 74f;
        foreach (var c in chips)
        {
            string t = c.Item2 >= 0f ? c.Item1 + "  " + Mathf.CeilToInt(c.Item2) : c.Item1;
            float cw = 22f + t.Length * 10f;
            if (cx + cw > x + 640f) { cx = x; cy -= 32f; }       // next row up
            Box(cx, cy, cw, 28f, new Color(0f, 0f, 0f, 0.6f));
            Box(cx, cy, 4f, 28f, c.Item3);
            Text(cx + 10f, cy, cw, 28f, t, 15, c.Item3);
            cx += cw + 6f;
        }
        // armour
        if (p.armor > 0f)
        {
            string an = p.suit != null ? Items.Get(p.suit.id).name.ToUpperInvariant() : "SHIELD";
            Text(x, y - 42f, 300f, 20f, an + "  " + Mathf.CeilToInt(p.armor), 14, new Color(0.6f, 0.9f, 1f));
            Bar(x, y - 20f, w, 12f, p.armor / Mathf.Max(1f, p.armorMax), p.suit != null ? Color.Lerp(p.suit.color, new Color(0.4f, 0.8f, 1f), 0.4f) : new Color(0.4f, 0.8f, 1f), new Color(0f, 0f, 0f, 0.55f));
        }
        // health
        float k = p.health / Player.MaxHealth;
        Color hc = k > 0.5f ? new Color(0.35f, 0.95f, 0.4f) : k > 0.25f ? new Color(1f, 0.8f, 0.2f) : new Color(1f, 0.25f, 0.2f);
        Box(x - 6f, y - 4f, w + 12f, 58f, Panel);
        Bar(x, y + 22f, w, 24f, k, hc, new Color(0.2f, 0f, 0f, 0.6f));
        Text(x, y - 2f, w, 24f, "HEALTH", 15, Dim);
        Text(x, y - 2f, w, 24f, Mathf.CeilToInt(p.health).ToString(), 22, Color.white, TextAnchor.MiddleRight);
    }

    // ---------------- bottom-middle: hotbar ----------------

    void DrawHotbar(Player p)
    {
        const float size = 84f, gap = 8f;
        float total = Player.SlotCount * size + (Player.SlotCount - 1) * gap;
        float x0 = (W - total) / 2f, y = 1080f - size - 22f;
        Color lab = Items.LabColors[p.labIndex % 8];
        for (int i = 0; i < Player.SlotCount; i++)
        {
            float x = x0 + i * (size + gap);
            HeldItem it = p.slots[i];
            bool sel = i == p.selected;
            Box(x, y, size, size, sel ? new Color(0.12f, 0.16f, 0.18f, 0.92f) : new Color(0.03f, 0.04f, 0.05f, 0.72f));
            if (sel) { Box(x, y, size, 4f, lab); Box(x, y + size - 4f, size, 4f, lab); Box(x, y, 4f, size, lab); Box(x + size - 4f, y, 4f, size, lab); }
            Text(x + 6f, y + 2f, 30f, 22f, (i + 1).ToString(), 14, Dim);
            if (it == null) continue;
            Recipe r = it.R;
            Icon(x + 12f, y + 10f, size - 24f, Items.Icon(r.icon), r.color);
            if (it.Enchanted)
            {
                Color ec = it.EnchantColor;
                Box(x + 4f, y + size - 10f, size - 8f, 5f, ec);
            }
            GunDef sg = r.kind == ItemKind.Weapon ? Guns.Get(it.id) : null;
            if (r.kind != ItemKind.Weapon) Text(x, y + size - 30f, size - 8f, 26f, "x" + it.count, 18, Color.white, TextAnchor.MiddleRight);
            else if (sg != null && sg.mag > 0)
            {
                // magazine guns: bullets left, or the reload bar
                if (it.reloadLeft > 0f)
                {
                    Box(x + 6f, y + size - 18f, size - 12f, 5f, new Color(0f, 0f, 0f, 0.6f));
                    Box(x + 6f, y + size - 18f, (size - 12f) * (1f - it.reloadLeft / Mathf.Max(0.01f, sg.reload)), 5f, new Color(1f, 0.85f, 0.3f));
                }
                else Text(x, y + size - 30f, size - 8f, 26f, (it.ammo < 0 ? sg.mag : it.ammo).ToString(), 16, it.ammo == 0 ? new Color(1f, 0.4f, 0.3f) : Color.white, TextAnchor.MiddleRight);
            }
            else if (it.heat > 1f)
            {
                Box(x + 6f, y + size - 18f, (size - 12f), 5f, new Color(0f, 0f, 0f, 0.6f));
                Box(x + 6f, y + size - 18f, (size - 12f) * it.heat / 100f, 5f, it.overheated ? new Color(1f, 0.25f, 0.2f) : new Color(1f, 0.6f, 0.2f));
            }
            if (Clicked(new Rect(x, y, size, size)) && !UIOpen) p.selected = i;
        }
        // name of what you're holding
        HeldItem h = p.Held;
        if (h != null)
        {
            string n = h.R.name + (h.Enchanted ? "  ·  " + h.EnchantName : "");
            Text(x0, y - 36f, total, 30f, n, 20, h.Enchanted ? h.EnchantColor : Color.white, TextAnchor.MiddleCenter);
            string hint = h.R.kind == ItemKind.Weapon ? QuickStats(h, p) : h.R.kind == ItemKind.Throwable ? "click to throw" : "click to drink";
            if (hint.Length > 0) Text(x0, y - 60f, total, 24f, hint, 15, Dim, TextAnchor.MiddleCenter, false);
        }
    }

    // ---------------- weapon stats (I) ----------------

    static bool showInfo;
    public static void ToggleWeaponInfo()
    {
        showInfo = !showInfo;
        Sfx.Play("tick", 0.4f);
    }

    // One short line under the weapon's name.
    static string QuickStats(HeldItem h, Player p)
    {
        GunDef g0 = Guns.Get(h.id);
        if (g0 == null) return "";
        GunDef g = Guns.Effective(g0, h.enchantDef, p);
        int shots = Mathf.Max(1, g.pellets) * Mathf.Max(1, g.burst);
        string s = "DMG " + g.damage.ToString("0.#") + (shots > 1 ? " x" + shots : "");
        s += g.mode == GunMode.Beam ? "  ·  beam" : "  ·  " + (1f / Mathf.Max(0.01f, g.cooldown)).ToString("0.0") + "/s";
        if (g.mag > 0) s += "  ·  mag " + g.mag;
        return s + "  ·  [I] stats";
    }

    static float RowH((string, string) row) => row.Item1 == "SPECIAL" ? 14f + 17f * Mathf.CeilToInt(row.Item2.Length / 40f) : 30f;

    void DrawWeaponInfo(Player p)
    {
        HeldItem h = p.Held;
        if (h == null) return;
        Recipe r = h.R;
        float w = 470f, x = W - w - 24f, y = 150f;
        GunDef g0 = r.kind == ItemKind.Weapon ? Guns.Get(h.id) : null;
        List<(string, string)> rows = g0 != null ? Guns.Stats(Guns.Effective(g0, h.enchantDef, p)) : new List<(string, string)>();
        float h2 = 92f + 60f;
        foreach (var row in rows) h2 += RowH(row);
        Box(x, y, w, h2, Panel);
        Box(x, y, w, 4f, h.Enchanted ? h.EnchantColor : r.color);
        Icon(x + 14f, y + 14f, 56f, Items.Icon(r.icon), r.color);
        Text(x + 80f, y + 12f, w - 90f, 30f, r.name, 22, Color.white);
        Text(x + 80f, y + 40f, w - 90f, 40f, r.desc, 13, Dim, TextAnchor.UpperLeft, false, true);
        float yy = y + 92f;
        foreach (var row in rows)
        {
            Text(x + 16f, yy, 140f, 28f, row.Item1, 13, new Color(0.7f, 0.45f, 1f));
            Text(x + 150f, yy + (row.Item1 == "SPECIAL" ? 6f : 0f), w - 166f, RowH(row), row.Item2, row.Item1 == "SPECIAL" ? 13 : 15, Color.white, row.Item1 == "SPECIAL" ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft, row.Item1 != "SPECIAL", true);
            yy += RowH(row);
        }
        if (h.Enchanted) { Text(x + 16f, yy, w - 32f, 26f, "Enchant: " + h.EnchantName + " (included above)", 13, h.EnchantColor, TextAnchor.MiddleLeft, false); yy += 26f; }
        Text(x + 16f, yy + 4f, w - 32f, 24f, "I to hide  ·  R reload (magazine guns)", 12, Dim, TextAnchor.MiddleLeft, false);
    }

    // ---------------- bottom-right: materials ----------------

    void DrawMaterials(Player p)
    {
        // only what you've actually got (there are lots of materials)
        Laboratory lab = p.MyLab;
        bool labOk = lab != null && !lab.Destroyed;
        var have = new List<int>();
        for (int m = 0; m < Items.MatCount; m++) if (p.backpack[m] > 0 || (labOk && lab.storage[m] > 0)) have.Add(m);
        const int maxRows = 14;
        int rows = Mathf.Min(have.Count, maxRows);
        float w = 290f, x = W - w - 24f, rowH = 30f;
        float h = 34f + Mathf.Max(1, rows) * rowH + (have.Count > maxRows ? 26f : 0f) + 6f;
        float y = 1080f - 24f - h;
        Box(x, y, w, h, Panel);
        Text(x + 14f, y + 4f, w, 26f, "MATERIALS", 15, Dim);
        Text(x, y + 4f, w - 14f, 26f, "carried  ·  in lab", 13, new Color(1f, 1f, 1f, 0.4f), TextAnchor.MiddleRight, false);
        if (have.Count == 0) Text(x + 14f, y + 32f, w, rowH, "nothing yet - go gather!", 15, Dim, TextAnchor.MiddleLeft, false);
        for (int i = 0; i < rows; i++)
        {
            int m = have[i];
            float yy = y + 32f + i * rowH;
            Icon(x + 10f, yy + 2f, 26f, Items.MatIcon(m), Items.MatColors[m]);
            Text(x + 42f, yy, 150f, rowH, Items.MatNames[m], 15, Items.MatColors[m], TextAnchor.MiddleLeft, false);
            Text(x, yy, w - 76f, rowH, p.backpack[m].ToString(), 17, p.backpack[m] > 0 ? Color.white : Dim, TextAnchor.MiddleRight);
            int st = labOk ? lab.storage[m] : 0;
            Text(x, yy, w - 14f, rowH, st.ToString(), 15, st > 0 ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 1f, 1f, 0.3f), TextAnchor.MiddleRight);
        }
        if (have.Count > maxRows) Text(x + 14f, y + 32f + rows * rowH, w, 24f, "+" + (have.Count - maxRows) + " more (see Storage in your lab)", 13, Dim, TextAnchor.MiddleLeft, false);
        // "+2 Herb" popups
        for (int i = popups.Count - 1; i >= 0; i--)
        {
            float age = Time.unscaledTime - popups[i].at;
            if (age > 1.6f) { popups.RemoveAt(i); continue; }
            float a = age < 1.2f ? 1f : 1f - (age - 1.2f) / 0.4f;
            int m = popups[i].mat;
            Text(x - 230f, y - 10f - (popups.Count - 1 - i) * 30f - age * 20f, 220f, 30f, "+" + popups[i].n + " " + Items.MatNames[m], 22, new Color(Items.MatColors[m].r, Items.MatColors[m].g, Items.MatColors[m].b, a), TextAnchor.MiddleRight);
        }
    }

    // ---------------- middle: crosshair, prompts ----------------

    void DrawCrosshair(Player p)
    {
        if (!p.Alive) return;
        float cx = W / 2f, cy = 540f;
        HeldItem h = p.Held;
        bool weapon = h != null && h.R.kind == ItemKind.Weapon;
        float spread = p.Aiming ? 6f : p.Sprinting ? 20f : 11f;
        Color c = new Color(1f, 1f, 1f, 0.9f);
        if (weapon && h.overheated) c = new Color(1f, 0.35f, 0.3f);
        Box(cx - 2f, cy - 2f, 4f, 4f, c);
        if (weapon)
        {
            Box(cx - spread - 10f, cy - 1f, 10f, 2f, c);
            Box(cx + spread, cy - 1f, 10f, 2f, c);
            Box(cx - 1f, cy - spread - 10f, 2f, 10f, c);
            Box(cx - 1f, cy + spread, 2f, 10f, c);
            GunDef cg = Guns.Get(h.id);
            if (cg != null && cg.charge > 0f && h.chargeT > 0f)
            {
                float ck = h.chargeT / cg.charge;
                Bar(cx - 50f, cy + 30f, 100f, 7f, ck, ck >= 1f ? new Color(1f, 1f, 0.5f) : cg.color, new Color(0f, 0f, 0f, 0.5f));
                Text(cx - 100f, cy + 40f, 200f, 22f, ck >= 1f ? "FULL CHARGE - let go!" : "CHARGING", 14, Color.white, TextAnchor.MiddleCenter);
            }
            if (cg != null && cg.mag > 0)
            {
                if (h.reloadLeft > 0f)
                {
                    Bar(cx - 40f, cy + 30f, 80f, 5f, 1f - h.reloadLeft / Mathf.Max(0.01f, cg.reload), new Color(1f, 0.85f, 0.3f), new Color(0f, 0f, 0f, 0.5f));
                    Text(cx - 100f, cy + 38f, 200f, 24f, "RELOADING", 15, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
                }
                else
                {
                    int am = h.ammo < 0 ? cg.mag : h.ammo;
                    Text(cx + 22f, cy + 14f, 120f, 22f, am + " / " + cg.mag, 14, am <= cg.mag / 4 ? new Color(1f, 0.45f, 0.35f) : new Color(1f, 1f, 1f, 0.75f), TextAnchor.MiddleLeft);
                    if (am == 0) Text(cx - 100f, cy + 38f, 200f, 24f, "PRESS R TO RELOAD", 14, new Color(1f, 0.45f, 0.35f), TextAnchor.MiddleCenter);
                }
            }
            if (h.heat > 1f)
            {
                Bar(cx - 40f, cy + 30f, 80f, 5f, h.heat / 100f, h.overheated ? new Color(1f, 0.3f, 0.25f) : new Color(1f, 0.65f, 0.2f), new Color(0f, 0f, 0f, 0.5f));
                if (h.overheated) Text(cx - 100f, cy + 38f, 200f, 24f, "OVERHEATED", 16, new Color(1f, 0.35f, 0.3f), TextAnchor.MiddleCenter);
            }
        }
        // hit marker
        float ha = 1f - (Time.unscaledTime - hitAt) / 0.25f;
        if (ha > 0f)
        {
            bool kill = Time.unscaledTime - killAt < 0.4f;
            Color mc = kill ? new Color(1f, 0.25f, 0.2f, ha) : new Color(1f, 1f, 1f, ha);
            float s = kill ? 16f : 11f;
            for (int i = 0; i < 4; i++)
            {
                float sx = (i % 2 == 0 ? -1f : 1f), sy = (i < 2 ? -1f : 1f);
                for (int j = 0; j < 4; j++)
                {
                    float d = s - j * 2.5f;
                    Box(cx + sx * d - 1.5f, cy + sy * d - 1.5f, 3f, 3f, mc);
                }
            }
        }
    }

    void DrawPrompt(Player p)
    {
        float cx = W / 2f, y = 640f;
        if (p.GatherProgress >= 0f)
        {
            Bar(cx - 120f, y, 240f, 10f, p.GatherProgress, new Color(0.5f, 1f, 0.5f), new Color(0f, 0f, 0f, 0.6f));
            Text(cx - 120f, y + 12f, 240f, 26f, "Gathering...", 17, Color.white, TextAnchor.MiddleCenter);
            return;
        }
        if (p.WorkProgress >= 0f)
        {
            Bar(cx - 140f, y, 280f, 12f, p.WorkProgress, new Color(1f, 0.8f, 0.3f), new Color(0f, 0f, 0f, 0.6f));
            Text(cx - 140f, y + 14f, 280f, 26f, "Working...", 17, Color.white, TextAnchor.MiddleCenter);
            return;
        }
        if (string.IsNullOrEmpty(p.PromptText)) return;
        bool pad = Gamepad.current != null;
        float w = 30f + p.PromptText.Length * 11f + (p.PromptUsable ? 50f : 0f);
        Box(cx - w / 2f, y, w, 44f, new Color(0f, 0f, 0f, 0.65f));
        float tx = cx - w / 2f + 12f;
        if (p.PromptUsable)
        {
            Box(tx, y + 7f, 32f, 30f, Color.white);
            Text(tx, y + 7f, 32f, 30f, pad ? "X" : "E", 20, Color.black, TextAnchor.MiddleCenter);
            tx += 44f;
        }
        Text(tx, y, w, 44f, p.PromptText, 19, p.PromptUsable ? Color.white : new Color(1f, 0.7f, 0.5f));
    }

    // ---------------- markers over the world (your lab, rival labs, materials) ----------------

    void DrawWorldMarkers(Player p)
    {
        Camera cam = GameCam.I != null ? GameCam.I.cam : null;
        if (cam == null || endShown) return;
        if (p.MyLab != null && !p.MyLab.Destroyed && !p.InsideOwnLab)
            Marker(cam, p.MyLab.transform.position + Vector3.up * 7f, "YOUR LAB", p.MyLab.LabColor, true);
        foreach (Laboratory l in Laboratory.All)
        {
            if (l == null || l == p.MyLab || l.Destroyed || MLGame.ShieldsUp) continue;
            if (Vector3.Distance(l.transform.position, p.transform.position) < 100f)
                Marker(cam, l.transform.position + Vector3.up * 7f, l.ColorName + " LAB  " + Mathf.CeilToInt(l.hp), l.LabColor * 0.9f, false);
        }
    }

    void Marker(Camera cam, Vector3 world, string label, Color c, bool clampToEdge)
    {
        Vector3 sp = cam.WorldToScreenPoint(world);
        bool behind = sp.z < 0f;
        float x = sp.x / S, y = (Screen.height - sp.y) / S;
        if (behind) { x = W - x; y = 1080f - 60f; }
        bool off = behind || x < 40f || x > W - 40f || y < 40f || y > 1040f;
        if (off && !clampToEdge) return;
        x = Mathf.Clamp(x, 60f, W - 60f);
        y = Mathf.Clamp(y, 140f, 1080f - 160f);
        float dist = Vector3.Distance(world, cam.transform.position);
        Box(x - 7f, y - 7f, 14f, 14f, c);
        Box(x - 4f, y - 4f, 8f, 8f, Color.black);
        Text(x - 150f, y + 8f, 300f, 22f, label + "  " + Mathf.RoundToInt(dist) + "m", 15, c, TextAnchor.MiddleCenter);
    }

    void DrawDamageNumbers()
    {
        Camera cam = GameCam.I != null ? GameCam.I.cam : null;
        if (cam == null) return;
        for (int i = numbers.Count - 1; i >= 0; i--)
        {
            Number n = numbers[i];
            float age = Time.unscaledTime - n.at;
            if (age > 0.9f) { numbers.RemoveAt(i); continue; }
            Vector3 sp = cam.WorldToScreenPoint(n.pos);
            if (sp.z < 0f) continue;
            float x = sp.x / S + n.drift.x * age, y = (Screen.height - sp.y) / S - 40f * age;
            float a = age < 0.6f ? 1f : 1f - (age - 0.6f) / 0.3f;
            int size = n.value >= 30f ? 28 : 20;
            Text(x - 60f, y - 15f, 120f, 30f, Mathf.RoundToInt(n.value).ToString(), size, new Color(n.color.r, n.color.g, n.color.b, a), TextAnchor.MiddleCenter);
        }
    }

    void DrawVignette(Player p)
    {
        float hurt = p.HurtFlash * 0.35f + (p.health < 30f && p.Alive ? (0.12f + 0.08f * Mathf.Sin(Time.unscaledTime * 6f)) : 0f);
        if (hurt > 0.01f)
        {
            Color c = new Color(0.8f, 0f, 0f, hurt);
            for (int i = 0; i < 6; i++)
            {
                float t = i * 18f;
                Color cc = new Color(c.r, c.g, c.b, c.a * (1f - i / 6f));
                Box(0f, t, W, 18f, cc);
                Box(0f, 1080f - t - 18f, W, 18f, cc);
                Box(t, 0f, 18f, 1080f, cc);
                Box(W - t - 18f, 0f, 18f, 1080f, cc);
            }
        }
        // which way the hit came from
        float da = 1f - (Time.unscaledTime - dmgAt) / 0.9f;
        if (da > 0f)
        {
            float rad = dmgAngle * Mathf.Deg2Rad;
            float cx = W / 2f + Mathf.Sin(rad) * 230f, cy = 540f - Mathf.Cos(rad) * 230f;
            for (int i = 0; i < 5; i++) Box(cx - 12f + i * 2f, cy - 12f + i * 2f, 24f - i * 4f, 24f - i * 4f, new Color(1f, 0.15f, 0.1f, da * 0.35f));
        }
        if (MLGame.I != null && MLGame.I.OutsideZone(p.transform.position) && p.Alive)
        {
            Box(0f, 0f, W, 1080f, new Color(0.5f, 0.1f, 0.7f, 0.15f + 0.05f * Mathf.Sin(Time.unscaledTime * 5f)));
            Text(0f, 200f, W, 50f, "OUTSIDE THE ZONE - GET BACK IN!", 34, new Color(0.9f, 0.5f, 1f), TextAnchor.MiddleCenter);
        }
    }

    void DrawToasts()
    {
        float y = 150f;
        for (int i = toasts.Count - 1; i >= 0; i--)
        {
            Toastie t = toasts[i];
            float age = Time.unscaledTime - t.at;
            if (age > 4.5f) { toasts.RemoveAt(i); continue; }
            float a = age < 3.8f ? 1f : 1f - (age - 3.8f) / 0.7f;
            float w = Mathf.Min(1100f, 60f + t.text.Length * 11.5f);
            Box((W - w) / 2f, y, w, 40f, new Color(0f, 0f, 0f, 0.6f * a));
            Box((W - w) / 2f, y, 4f, 40f, new Color(t.color.r, t.color.g, t.color.b, a));
            Text((W - w) / 2f, y, w, 40f, t.text, 19, new Color(1f, 1f, 1f, a), TextAnchor.MiddleCenter, false);
            y += 46f;
        }
    }

    void DrawWarn()
    {
        float age = Time.unscaledTime - warnAt;
        if (age > 2.8f || string.IsNullOrEmpty(warn)) return;
        float a = (age < 2.2f ? 1f : 1f - (age - 2.2f) / 0.6f) * (0.7f + 0.3f * Mathf.Sin(age * 14f));
        Text(0f, 360f, W, 60f, warn, 40, new Color(1f, 0.3f, 0.25f, a), TextAnchor.MiddleCenter);
    }

    void DrawBanner()
    {
        float age = Time.unscaledTime - bannerAt;
        if (age > 3.6f || string.IsNullOrEmpty(bannerTitle)) return;
        float a = age < 0.25f ? age / 0.25f : age < 3f ? 1f : 1f - (age - 3f) / 0.6f;
        float scale = 1f + Mathf.Max(0f, 0.25f - age) * 1.5f;
        Box(0f, 400f, W, 170f, new Color(0f, 0f, 0f, 0.45f * a));
        Box(0f, 400f, W, 4f, new Color(bannerColor.r, bannerColor.g, bannerColor.b, a));
        Box(0f, 566f, W, 4f, new Color(bannerColor.r, bannerColor.g, bannerColor.b, a));
        Text(0f, 405f, W, 110f, bannerTitle, Mathf.RoundToInt(96 * scale), new Color(bannerColor.r, bannerColor.g, bannerColor.b, a), TextAnchor.MiddleCenter);
        Text(0f, 510f, W, 50f, bannerSub, 26, new Color(1f, 1f, 1f, a), TextAnchor.MiddleCenter, false);
    }

    void DrawDead(Player p)
    {
        if (p.Alive) return;
        Box(0f, 0f, W, 1080f, new Color(0.15f, 0f, 0f, 0.35f));
        if (p.RespawnAt > 0f)
        {
            Text(0f, 430f, W, 80f, "KNOCKED OUT", 64, new Color(1f, 0.4f, 0.35f), TextAnchor.MiddleCenter);
            Text(0f, 510f, W, 40f, "Back at your lab in " + Mathf.CeilToInt(p.RespawnAt - Time.time) + "...", 26, Color.white, TextAnchor.MiddleCenter, false);
        }
        else
        {
            Text(0f, 430f, W, 80f, "ELIMINATED", 64, new Color(1f, 0.3f, 0.25f), TextAnchor.MiddleCenter);
        }
    }

    // ================================================================
    // Station menus
    // ================================================================

    static int KnownCols(int n) => 2;

    // Materials you can put in the mix (anything you have, carried or stored).
    static List<int> StationMats(Player p)
    {
        var list = new List<int>();
        for (int m = 0; m < Items.MatCount; m++) if (p.Have(m) > 0) list.Add(m);
        return list;
    }

    static bool Shift => Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);

    // (hold SHIFT to add / take out 10 at a time)
    void AddToMix(Player p, int m)
    {
        if (mix[m] >= p.Have(m)) { Sfx.Play("error", 0.35f); return; }
        mix[m] = Mathf.Min(p.Have(m), mix[m] + (Shift ? 10 : 1));
        Sfx.Play("tick", 0.4f, 1f + mix[m] * 0.05f);
    }

    void TakeFromMix(int m)
    {
        if (mix[m] <= 0) return;
        mix[m] = Mathf.Max(0, mix[m] - (Shift ? 10 : 1));
        Sfx.Play("tick", 0.3f, 0.85f);
    }

    void ClearMix()
    {
        System.Array.Clear(mix, 0, mix.Length);
        Sfx.Play("tick", 0.3f, 0.7f);
    }

    void Combine(Player p)
    {
        if (station == null) return;
        if (p.Experiment(station.type, mix)) System.Array.Clear(mix, 0, mix.Length);
    }

    static bool RightClicked(Rect virtualRect)
    {
        Event e = Event.current;
        return e.type == EventType.MouseDown && e.button == 1 && R(virtualRect.x, virtualRect.y, virtualRect.width, virtualRect.height).Contains(e.mousePosition);
    }

    void DrawStation(Player p)
    {
        LabStation st = station;
        Laboratory lab = st.lab;
        float x = 60f, y = 60f, w = 1120f, h = 960f;
        Box(0f, 0f, W, 1080f, new Color(0f, 0f, 0f, 0.35f));
        Box(x, y, w, h, new Color(0.03f, 0.05f, 0.07f, 0.94f));
        Box(x, y, w, 6f, lab.LabColor);
        Text(x + 30f, y + 18f, w, 50f, st.Name.ToUpperInvariant(), 40, Color.white);
        string sub;
        switch (st.type)
        {
            case Station.Brew: sub = "Things that bubble and fizz. Put materials in the mix and see what brews."; break;
            case Station.Workbench: sub = "Things you can hold or wear. Mix materials and see what you can build."; break;
            case Station.Enchant: sub = "Magic for the weapon you're HOLDING. Pick it in your hotbar first, then experiment."; break;
            case Station.Terminal: sub = "Upgrades for your lab itself. Feed it materials and see what it builds."; break;
            default: sub = "Materials you carry are stored here automatically when you walk in."; break;
        }
        Text(x + 30f, y + 68f, w - 60f, 30f, sub, 17, Dim, TextAnchor.MiddleLeft, false);
        bool pad = Gamepad.current != null;
        Text(x, y + 18f, w - 30f, 50f, pad ? "B  close" : "E  close", 16, Dim, TextAnchor.MiddleRight);
        float yy = y + 110f;

        if (st.type == Station.Storage)
        {
            const int cols = 5;
            float tw = (w - 60f - (cols - 1) * 10f) / cols, th = 104f;
            for (int m = 0; m < Items.MatCount; m++)
            {
                float tx = x + 30f + (m % cols) * (tw + 10f), ty = yy + (m / cols) * (th + 10f);
                int n = lab.storage[m];
                Box(tx, ty, tw, th, new Color(1f, 1f, 1f, n > 0 ? 0.07f : 0.025f));
                Icon(tx + 10f, ty + 12f, 48f, Items.MatIcon(m), n > 0 ? Items.MatColors[m] : Items.MatColors[m] * 0.4f);
                Text(tx + 66f, ty + 6f, tw - 70f, 30f, Items.MatNames[m], 17, n > 0 ? Items.MatColors[m] : new Color(1f, 1f, 1f, 0.3f), TextAnchor.MiddleLeft, true);
                Text(tx + 66f, ty + 40f, tw - 80f, 56f, n.ToString(), 34, n > 0 ? Color.white : new Color(1f, 1f, 1f, 0.25f), TextAnchor.MiddleLeft);
            }
            Text(x + 30f, yy + 5 * (th + 10f) + 10f, w - 60f, 90f, "If your lab gets destroyed, everything stored here spills out for anyone to take. If YOU get knocked out, you drop what you're carrying (not what's stored).", 18, Dim, TextAnchor.UpperLeft, false, true);
            return;
        }

        if (st.type == Station.Terminal)
        {
            Box(x + 30f, yy, w - 60f, 70f, new Color(1f, 1f, 1f, 0.04f));
            Text(x + 44f, yy + 4f, 400f, 30f, "LAB HEALTH  " + Mathf.CeilToInt(lab.hp) + " / " + Mathf.CeilToInt(lab.maxHp), 18, Color.white);
            Bar(x + 44f, yy + 38f, w - 88f, 14f, lab.hp / lab.maxHp, new Color(0.4f, 0.95f, 0.4f), new Color(0f, 0f, 0f, 0.6f));
            string have = (lab.Has(ItemId.WallsII) ? "Fortified walls" : lab.Has(ItemId.WallsI) ? "Reinforced walls" : "Basic walls")
                + (lab.Has(ItemId.Forcefield) ? " · Forcefield" : "") + (lab.Has(ItemId.Turret) ? " · Turret" : "") + (lab.Has(ItemId.SlimeTraps) ? " · Slime traps" : "") + (lab.Has(ItemId.FastBrew) ? " · 2 cauldrons" : "");
            Text(x, yy + 4f, w - 44f, 30f, have, 15, Dim, TextAnchor.MiddleRight, false);
            yy += 82f;
        }
        if (st.type == Station.Brew)
        {
            Text(x + 30f, yy, w, 30f, "CAULDRON" + (lab.BrewSlots > 1 ? "S" : "") + ":  " + (lab.brews.Count == 0 ? "empty" : ""), 17, Dim);
            float bx = x + 190f;
            foreach (Laboratory.Brew b in lab.brews)
            {
                Icon(bx, yy, 28f, Items.Icon(b.r.icon), b.r.color);
                Bar(bx + 34f, yy + 11f, 120f, 8f, b.done ? 1f : 1f - b.left / b.total, b.r.color, new Color(0f, 0f, 0f, 0.6f));
                bx += 180f;
            }
            yy += 40f;
        }

        // ---- your materials (click to put one in the mix) ----
        List<int> mats = StationMats(p);
        List<Recipe> known = Discoveries.Known(st.type);
        Text(x + 30f, yy, w, 28f, "YOUR MATERIALS", 17, new Color(0.7f, 0.45f, 1f));
        Text(x, yy, w - 30f, 28f, pad ? "A  add one   ·   X  take one out" : "click to add one   ·   right-click to take one out   ·   hold SHIFT for 10", 14, Dim, TextAnchor.MiddleRight, false);
        yy += 32f;
        float tile = (w - 60f - (MatCols - 1) * 6f) / MatCols, tileH = 92f;
        if (mats.Count == 0)
        {
            Text(x + 30f, yy, w - 60f, 40f, "You don't have any materials. Go out and gather some!", 18, Dim, TextAnchor.MiddleLeft, false);
            yy += 50f;
        }
        for (int i = 0; i < mats.Count; i++)
        {
            int m = mats[i];
            float tx = x + 30f + (i % MatCols) * (tile + 6f), ty = yy + (i / MatCols) * (tileH + 6f);
            bool over = Hover(tx, ty, tile, tileH);
            if (over && Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.5f) stationSel = i;
            bool sel = stationSel == i;
            int free = p.Have(m) - mix[m];
            Box(tx, ty, tile, tileH, sel ? new Color(1f, 1f, 1f, 0.13f) : new Color(1f, 1f, 1f, 0.045f));
            if (sel) Box(tx, ty + tileH - 4f, tile, 4f, lab.LabColor);
            Icon(tx + (tile - 44f) / 2f, ty + 6f, 44f, Items.MatIcon(m), Items.MatColors[m]);
            Text(tx, ty + 50f, tile, 20f, Items.MatNames[m], 12, Items.MatColors[m], TextAnchor.MiddleCenter, false);
            Text(tx, ty + 68f, tile, 22f, free.ToString(), 17, free > 0 ? Color.white : new Color(1f, 0.4f, 0.35f), TextAnchor.MiddleCenter);
            if (Clicked(new Rect(tx, ty, tile, tileH))) { stationSel = i; AddToMix(p, m); Event.current.Use(); }
            else if (RightClicked(new Rect(tx, ty, tile, tileH))) { stationSel = i; TakeFromMix(m); Event.current.Use(); }
        }
        if (mats.Count > 0) yy += Mathf.CeilToInt(mats.Count / (float)MatCols) * (tileH + 6f);
        yy += 8f;

        // ---- the mix ----
        Text(x + 30f, yy, w, 28f, "THE MIX", 17, new Color(0.7f, 0.45f, 1f));
        yy += 32f;
        Box(x + 30f, yy, w - 60f, 96f, new Color(0f, 0f, 0f, 0.35f));
        float mx = x + 46f;
        bool any = false;
        for (int m = 0; m < Items.MatCount; m++)
        {
            if (mix[m] <= 0) continue;
            any = true;
            if (mx > x + w - 400f) { Text(mx, yy, 60f, 96f, "...", 24, Dim); break; }
            Box(mx, yy + 12f, 82f, 72f, new Color(1f, 1f, 1f, 0.06f));
            Icon(mx + 6f, yy + 18f, 40f, Items.MatIcon(m), Items.MatColors[m]);
            Text(mx + 46f, yy + 18f, 36f, 40f, "x" + mix[m], 18, Color.white);
            Text(mx, yy + 58f, 82f, 22f, Items.MatNames[m], 11, Items.MatColors[m], TextAnchor.MiddleCenter, false);
            if (Clicked(new Rect(mx, yy + 12f, 82f, 72f))) { TakeFromMix(m); Event.current.Use(); }
            mx += 90f;
        }
        if (!any) Text(x + 46f, yy, 600f, 96f, "empty - add materials from above", 18, Dim, TextAnchor.MiddleLeft, false);
        // buttons
        float bx2 = x + w - 340f;
        bool canMix = any;
        bool overC = Hover(bx2, yy + 14f, 200f, 68f);
        Box(bx2, yy + 14f, 200f, 68f, canMix ? (overC ? new Color(0.45f, 0.95f, 0.35f) : new Color(0.28f, 0.68f, 0.22f)) : new Color(1f, 1f, 1f, 0.08f));
        Text(bx2, yy + 14f, 200f, 46f, "COMBINE", 26, canMix ? Color.white : Dim, TextAnchor.MiddleCenter);
        Text(bx2, yy + 52f, 200f, 24f, pad ? "Y" : "C", 14, canMix ? Color.white : Dim, TextAnchor.MiddleCenter, false);
        if (Clicked(new Rect(bx2, yy + 14f, 200f, 68f))) { Combine(p); Event.current.Use(); }
        float cbx = x + w - 130f;
        Box(cbx, yy + 14f, 84f, 68f, Hover(cbx, yy + 14f, 84f, 68f) ? new Color(1f, 1f, 1f, 0.16f) : new Color(1f, 1f, 1f, 0.07f));
        Text(cbx, yy + 14f, 84f, 46f, "CLEAR", 17, Color.white, TextAnchor.MiddleCenter);
        Text(cbx, yy + 52f, 84f, 24f, pad ? "LB" : "R", 14, Dim, TextAnchor.MiddleCenter, false);
        if (Clicked(new Rect(cbx, yy + 14f, 84f, 68f))) { ClearMix(); Event.current.Use(); }
        yy += 108f;

        // ---- recipes you've discovered here ----
        int total = Items.For(st.type).Count;
        Text(x + 30f, yy, w, 28f, "DISCOVERED HERE   " + known.Count + " / " + total, 17, new Color(0.7f, 0.45f, 1f));
        Text(x, yy, w - 30f, 28f, "click one to make it again   ·   B  recipe book & hints", 14, Dim, TextAnchor.MiddleRight, false);
        float headerY = yy;
        yy += 32f;
        if (known.Count == 0)
        {
            Text(x + 30f, yy, w - 60f, 60f, "Nothing yet. Every recipe here is a secret - try different mixes and press COMBINE. Wrong mixes don't cost you anything.", 17, Dim, TextAnchor.UpperLeft, false, true);
            return;
        }
        int kc = KnownCols(known.Count);
        int rowsNeeded = Mathf.CeilToInt(known.Count / (float)kc);
        float avail = y + h - 16f - yy;
        float ch = Mathf.Clamp(avail / rowsNeeded, 56f, 72f);
        int visRows = Mathf.Max(1, Mathf.FloorToInt(avail / ch));
        // scroll: mouse wheel over the list, or follow the keyboard / controller selection
        int maxScroll = Mathf.Max(0, rowsNeeded - visRows);
        if (Event.current.type == EventType.ScrollWheel && R(x, yy, w, avail).Contains(Event.current.mousePosition))
        {
            knownScroll += Event.current.delta.y > 0f ? 1 : -1;
            Event.current.Use();
        }
        int selRow = stationSel - mats.Count >= 0 ? (stationSel - mats.Count) / kc : -1;
        if (selRow >= 0 && Event.current.type == EventType.Repaint && (Mouse.current == null || Mouse.current.delta.ReadValue().sqrMagnitude < 0.5f))
        {
            if (selRow < knownScroll) knownScroll = selRow;
            if (selRow >= knownScroll + visRows) knownScroll = selRow - visRows + 1;
        }
        knownScroll = Mathf.Clamp(knownScroll, 0, maxScroll);
        if (maxScroll > 0)
        {
            Text(x + 330f, headerY - 32f, 300f, 28f, "scroll for more  (rows " + (knownScroll + 1) + "-" + Mathf.Min(rowsNeeded, knownScroll + visRows) + " of " + rowsNeeded + ")", 13, Dim, TextAnchor.MiddleLeft, false);
            Bar(x + w - 22f, yy, 6f, visRows * ch, 1f, new Color(1f, 1f, 1f, 0.06f), new Color(0f, 0f, 0f, 0f));
            Box(x + w - 22f, yy + visRows * ch * knownScroll / rowsNeeded, 6f, visRows * ch * visRows / rowsNeeded, new Color(1f, 1f, 1f, 0.35f));
        }
        float colW = (w - 60f - (kc - 1) * 10f - (maxScroll > 0 ? 14f : 0f)) / kc;
        for (int i = 0; i < known.Count; i++)
        {
            Recipe r = known[i];
            int cell = mats.Count + i;
            int row = i / kc - knownScroll;
            if (row < 0 || row >= visRows) continue;
            float rx = x + 30f + (i % kc) * (colW + 10f), ry = yy + row * ch;
            string problem = p.CraftProblem(r);
            bool ok = problem == null;
            bool over = Hover(rx, ry, colW, ch - 6f);
            if (over && Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.5f) stationSel = cell;
            bool sel = stationSel == cell;
            float rh = ch - 6f;
            Box(rx, ry, colW, rh, sel ? new Color(1f, 1f, 1f, 0.11f) : new Color(1f, 1f, 1f, 0.04f));
            Box(rx, ry, 4f, rh, sel ? lab.LabColor : new Color(r.color.r, r.color.g, r.color.b, ok ? 0.8f : 0.3f));
            // the recipe on the right: material icon + how many (red = you don't have enough)
            int kinds = 0;
            for (int m = 0; m < Items.MatCount; m++) if (r.cost[m] > 0) kinds++;
            if (r.hideRecipe) kinds = 2;
            const float step = 46f;
            float costW = kinds * step;
            float cx = rx + colW - 8f - costW;
            if (r.hideRecipe) Text(cx, ry, costW, rh, "? ? ?", 18, new Color(1f, 0.3f, 0.9f), TextAnchor.MiddleCenter);
            else for (int m = 0; m < Items.MatCount; m++)
            {
                int need = r.cost[m];
                if (need <= 0) continue;
                bool enough = p.Have(m) >= need;
                Icon(cx, ry + (rh - 24f) / 2f, 24f, Items.MatIcon(m), Items.MatColors[m]);
                Text(cx + 24f, ry, 22f, rh, need.ToString(), 15, enough ? Color.white : new Color(1f, 0.35f, 0.3f), TextAnchor.MiddleLeft);
                cx += step;
            }
            // picture and name on the left (the name shrinks to fit before the recipe)
            float isz = Mathf.Min(40f, rh - 10f);
            Icon(rx + 12f, ry + (rh - isz) / 2f, isz, Items.Icon(r.icon), ok ? r.color : r.color * 0.6f);
            float nx = rx + 20f + isz, nw = colW - (nx - rx) - costW - 14f;
            int fs = 16;
            while (fs > 11 && r.name.Length * fs * 0.56f > nw) fs--;
            string label = r.name;
            int maxChars = Mathf.Max(4, Mathf.FloorToInt(nw / (fs * 0.56f)));
            if (label.Length > maxChars) label = label.Substring(0, maxChars - 2) + "..";
            if (ok) Text(nx, ry, nw, rh, label, fs, Color.white, TextAnchor.MiddleLeft);
            else
            {
                Text(nx, ry + 2f, nw, rh * 0.55f, label, fs, new Color(1f, 1f, 1f, 0.5f), TextAnchor.MiddleLeft);
                Text(nx, ry + rh * 0.5f, nw, rh * 0.48f, problem, 12, new Color(1f, 0.55f, 0.45f, sel || over ? 1f : 0.7f), TextAnchor.MiddleLeft, false);
            }
            if (Clicked(new Rect(rx, ry, colW, rh)))
            {
                stationSel = cell;
                p.Craft(r);
                Event.current.Use();
            }
        }
    }

    // ================================================================
    // Pause, recipes, end screen
    // ================================================================

    bool MenuButton(int row, ref int sel, float x, float y, float w, float h, string label, bool primary)
    {
        bool over = Hover(x, y, w, h);
        if (over && Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.5f) sel = row;
        bool on = sel == row;
        Box(x, y, w, h, primary ? (on ? new Color(0.35f, 0.85f, 0.25f) : new Color(0.22f, 0.6f, 0.16f)) : (on ? new Color(1f, 1f, 1f, 0.14f) : new Color(1f, 1f, 1f, 0.05f)));
        if (on && !primary) Box(x, y, 5f, h, new Color(0.45f, 1f, 0.35f));
        Text(x + 26f, y, w, h, label, 28, Color.white);
        if (Clicked(new Rect(x, y, w, h))) { sel = row; Event.current.Use(); return true; }
        return false;
    }

    void DrawPause()
    {
        Box(0f, 0f, W, 1080f, new Color(0f, 0f, 0f, 0.6f));
        float x = 120f, y = 260f;
        Text(x, y - 120f, 800f, 100f, "PAUSED", 80, Color.white);
        string[] rows = { "RESUME", "HOW TO PLAY & DISCOVERIES", "CAMERA:  " + (Settings.FirstPerson ? "1ST PERSON" : "3RD PERSON") + "   (V)", MLGame.I.phase < MLGame.Phase.Madness ? "SKIP TO NEXT PHASE (practice)" : "SKIP PHASE (already MADNESS)", "+10 OF EVERY MATERIAL (practice)", "RESTART MATCH", "QUIT TO MENU" };
        for (int i = 0; i < rows.Length; i++)
            if (MenuButton(i, ref pauseSel, x, y + i * 80f, 620f, 68f, rows[i], i == 0)) ChoosePause(i);
        // quick settings
        float sx = 900f, sy = 260f;
        Box(sx, sy, 520f, 270f, Panel);
        Text(sx + 24f, sy + 10f, 400f, 30f, "QUICK SETTINGS", 18, Dim);
        // graphics: click to switch LOW / MEDIUM / HIGH
        int gq = Mathf.Clamp(Settings.Quality, 0, 2);
        bool gOver = Hover(sx + 24f, sy + 200f, 470f, 50f);
        Box(sx + 24f, sy + 200f, 470f, 50f, gOver ? new Color(1f, 1f, 1f, 0.14f) : new Color(1f, 1f, 1f, 0.06f));
        Text(sx + 40f, sy + 200f, 300f, 50f, "Graphics  (click to change)", 17, Color.white, TextAnchor.MiddleLeft, false);
        Text(sx + 24f, sy + 200f, 455f, 50f, GraphicsQuality.Names[gq], 20, gq == 0 ? new Color(1f, 0.8f, 0.3f) : new Color(0.45f, 1f, 0.35f), TextAnchor.MiddleRight);
        if (Clicked(new Rect(sx + 24f, sy + 200f, 470f, 50f)))
        {
            Settings.Quality = (gq + 1) % 3;
            Settings.Apply();
            Settings.Save();
            Sfx.Play("tick", 0.5f);
            Event.current.Use();
        }
        Slider(sx + 24f, sy + 50f, "Mouse sensitivity", ref Settings.Sensitivity, 0.25f, 5f);
        Slider(sx + 24f, sy + 120f, "Volume", ref Settings.Volume, 0f, 1f);
        Text(x, 1000f, 1200f, 30f, "Esc to resume  ·  arrows / mouse to choose", 17, Dim, TextAnchor.MiddleLeft, false);
    }

    void Slider(float x, float y, string label, ref float value, float min, float max)
    {
        Text(x, y, 300f, 26f, label, 17, Color.white, TextAnchor.MiddleLeft, false);
        float k = Mathf.InverseLerp(min, max, value);
        Bar(x, y + 32f, 470f, 14f, k, new Color(0.45f, 1f, 0.35f), new Color(0f, 0f, 0f, 0.6f));
        Rect r = R(x, y + 24f, 470f, 30f);
        Event e = Event.current;
        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && r.Contains(e.mousePosition))
        {
            value = Mathf.Lerp(min, max, Mathf.Clamp01((e.mousePosition.x - r.x) / r.width));
            Settings.Save();
            e.Use();
        }
        Text(x, y, 470f, 26f, value.ToString("0.00"), 16, Dim, TextAnchor.MiddleRight);
    }

    void DrawHowTo()
    {
        Box(0f, 0f, W, 1080f, new Color(0.01f, 0.02f, 0.03f, 0.95f));
        Text(80f, 30f, 1200f, 70f, "HOW TO PLAY", 50, new Color(0.45f, 1f, 0.35f));
        string how =
            "1. COLLECT (3 min): explore and press E on glowing material spots. Forests, caves, the old office building and the meteor crater all hide different materials.\n" +
            "2. Walk into YOUR lab - what you carry goes into storage. At a station, put materials in the MIX and press COMBINE. If it's a secret recipe, you make it!\n" +
            "3. EXPERIMENT (30s): last chance before the shields drop.\n" +
            "4. MADNESS: labs can be attacked. Defend yours, smash the others. The zone closes in.\n" +
            "If you're knocked out you come back at your lab - unless it's destroyed. Then you're out!";
        Text(80f, 100f, W - 160f, 170f, how, 18, Color.white, TextAnchor.UpperLeft, false, true);
        Text(80f, 270f, 1600f, 30f, "Move WASD · Sprint Shift · Jump Space · Fire / use: left click · Aim: right click · E: gather / use · 1-8 or wheel: pick item · Q: quick-heal · B: recipe book", 16, Dim, TextAnchor.MiddleLeft, false);
        // what you've discovered so far
        Station[] order = { Station.Brew, Station.Workbench, Station.Enchant, Station.Terminal };
        float colW = (W - 160f) / 4f;
        for (int c = 0; c < order.Length; c++)
        {
            float x = 80f, y = 320f;
            x += c * colW;
            List<Recipe> all = Items.For(order[c]);
            List<Recipe> found = Discoveries.Known(order[c]);
            Text(x, y, colW, 34f, LabStation.Names[(int)order[c]].ToUpperInvariant() + "   " + found.Count + "/" + all.Count, 22, new Color(0.7f, 0.45f, 1f));
            y += 40f;
            float step = Mathf.Clamp(640f / Mathf.Max(1, found.Count), 24f, 56f);
            bool compact = step < 50f;          // lots found: one line each
            int shown = 0;
            foreach (Recipe r in found)
            {
                if (y > 990f) { Text(x, y, colW - 20f, 24f, "+ " + (found.Count - shown) + " more (see the station)", 14, Dim, TextAnchor.MiddleLeft, false); y += 30f; break; }
                shown++;
                if (compact)
                {
                    Icon(x, y, 22f, Items.Icon(r.icon), r.color);
                    Text(x + 28f, y - 2f, 190f, 24f, r.name, 14, Color.white);
                    float cx2 = x + 210f;
                    for (int m = 0; m < Items.MatCount; m++)
                    {
                        if (r.cost[m] <= 0) continue;
                        Icon(cx2, y + 2f, 18f, Items.MatIcon(m), Items.MatColors[m]);
                        Text(cx2 + 18f, y - 2f, 22f, 24f, r.cost[m].ToString(), 13, Color.white);
                        cx2 += 40f;
                    }
                    y += step;
                    continue;
                }
                Icon(x, y, 40f, Items.Icon(r.icon), r.color);
                Text(x + 48f, y - 2f, colW - 60f, 24f, r.name, 17, Color.white);
                float cx = x + 48f;
                for (int m = 0; m < Items.MatCount; m++)
                {
                    if (r.cost[m] <= 0) continue;
                    Icon(cx, y + 22f, 20f, Items.MatIcon(m), Items.MatColors[m]);
                    Text(cx + 21f, y + 20f, 30f, 24f, r.cost[m].ToString(), 15, Color.white);
                    cx += 48f;
                }
                y += step;
            }
            int secret = all.Count - found.Count;
            if (secret > 0) Text(x, y, colW - 20f, 30f, "+ " + secret + " secret recipe" + (secret == 1 ? "" : "s") + " (???)", 16, Dim, TextAnchor.MiddleLeft, false);
        }
        Text(80f, 1030f, 1200f, 30f, "Esc / Tab to go back", 17, Dim, TextAnchor.MiddleLeft, false);
    }

    void DrawEnd(MLGame gm)
    {
        float a = Mathf.Clamp01((Time.unscaledTime - openedAt) / 0.6f);
        Box(0f, 0f, W, 1080f, new Color(0f, 0f, 0f, 0.7f * a));
        Color c = endWin ? new Color(0.45f, 1f, 0.35f, a) : new Color(1f, 0.32f, 0.26f, a);
        Text(0f, 180f, W, 140f, endWin ? "LAST SCIENTIST STANDING!" : "YOUR EXPERIMENT IS OVER", endWin ? 84 : 72, c, TextAnchor.MiddleCenter);
        string sub = endWin ? (Net.Active && MLGame.OnlinePlayers >= 2 ? "Every other scientist is out. Science wins." : "Every rival lab is rubble. Science wins.")
            : !string.IsNullOrEmpty(gm.winnerName) ? gm.winnerName + " is the last scientist standing."
            : Net.Active && MLGame.OnlinePlayers >= 2 && gm.phase != MLGame.Phase.Over ? "You're out - the others are still fighting..." : "Better luck in the next experiment.";
        Text(0f, 320f, W, 40f, sub, 26, new Color(1f, 1f, 1f, a), TextAnchor.MiddleCenter, false);
        string stats = "Time  " + Clock(gm.matchTime) + "      Labs destroyed  " + gm.labsDestroyed + "      Drones downed  " + gm.dronesKilled;
        Text(0f, 390f, W, 40f, stats, 22, new Color(1f, 1f, 1f, 0.8f * a), TextAnchor.MiddleCenter, false);
        float x = (W - 520f) / 2f;
        if (MenuButton(0, ref pauseSel, x, 520f, 520f, 76f, Net.IsClient ? "PLAY AGAIN (host starts it)" : "PLAY AGAIN", true)) ChooseEnd(0);
        if (MenuButton(1, ref pauseSel, x, 610f, 520f, 66f, "MAIN MENU", false)) ChooseEnd(1);
    }
}
