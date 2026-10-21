using UnityEngine;
using UnityEngine.InputSystem;

// MAIN MENU: the first screen of the game - PLAY, SETTINGS and CO-OP.
// It builds itself when you press Play in Unity (no setup needed): a dark lab background with
// glowing bubbles floating up, the title, and the three buttons.
//   PLAY      - starts a match in the Lab Arena (make it once in Unity: Tools > Mad Lab > 2. Create Lab Arena)
//   SETTINGS  - volume, music, mouse sensitivity, fullscreen and graphics quality (saved)
//   CO-OP     - host a game and get a code, or type a friend's code to join (see Net.cs)
// Mouse, keyboard (arrows / WASD + Enter, Esc to go back) and controller (stick / d-pad + A, B to go back) all work.
public class MainMenu : MonoBehaviour
{
    public const string GameTitle = "MAD LAB";                 // change the name here
    const string Tagline = "Collect.  Craft.  Survive.  Last scientist standing.";

    enum Screen2 { Main, Settings, Coop }
    Screen2 screen = Screen2.Main;
    int sel;                       // highlighted row (keyboard / controller)
    float[] hover = new float[8];
    float openedAt;
    string toast;
    float toastAt = -10f;
    float repeatAt;
    Vector2Int lastNav;
    bool mouseMoved;            // so hovering only takes over when the mouse actually moves
    string nameDraft, codeDraft = "";
    string nameError;
    string focusRequest;        // a text box to put the cursor in (done in OnGUI)
    string focusedCtrl = "";    // which text box has the cursor (read in OnGUI)

    // background bubbles
    struct Bubble { public float x, y, r, speed, wobble, hue; }
    Bubble[] bubbles;

    static Texture2D white, circle;
    static readonly System.Collections.Generic.Dictionary<int, GUIStyle> styles = new System.Collections.Generic.Dictionary<int, GUIStyle>();

    static readonly Color Green = new Color(0.45f, 1f, 0.35f);
    static readonly Color Purple = new Color(0.7f, 0.4f, 1f);
    static readonly Color Panel = new Color(0.05f, 0.07f, 0.09f, 0.88f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { styles.Clear(); white = circle = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindAnyObjectByType<MainMenu>() != null) return;
        new GameObject("Main Menu").AddComponent<MainMenu>();
    }

    static MainMenu instance;
    public static void Ping() => Click(0.6f);       // something happened in the lobby

    void OnEnable() { instance = this; NetLobby.GameStarted += OnGameStarted; }
    void OnDisable() { NetLobby.GameStarted -= OnGameStarted; }
    // The host pressed START: everyone goes into the arena, each in their own lab (host = lab 1, next = lab 2...).
    void OnGameStarted()
    {
        if (!MLGame.ArenaPresent) { Toast("The host pressed START, but this copy of the game has no arena yet!"); return; }
        int slot = 0;
        if (NetLobby.Instance != null)
        {
            ulong me = Unity.Netcode.NetworkManager.Singleton != null ? Unity.Netcode.NetworkManager.Singleton.LocalClientId : 0;
            int i = NetLobby.Instance.Members.FindIndex(m => m.id == me);
            if (i >= 0) slot = i;
        }
        MLGame.StartMatch(slot);
    }

    void Awake()
    {
        Settings.Apply();
        openedAt = Time.unscaledTime;
        bubbles = new Bubble[40];
        for (int i = 0; i < bubbles.Length; i++) bubbles[i] = NewBubble(Random.value);
        // a plain dark camera background (the menu draws over everything anyway)
        Camera cam = Camera.main;
        if (cam != null && !MLGame.ArenaPresent)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.04f);
        }
    }

    Bubble NewBubble(float y)
    {
        return new Bubble
        {
            x = Random.value,
            y = y,
            r = Random.Range(0.004f, 0.022f),
            speed = Random.Range(0.02f, 0.07f),
            wobble = Random.Range(0f, 10f),
            hue = Random.value < 0.6f ? 0.3f : Random.value < 0.5f ? 0.78f : 0.5f,
        };
    }

    // ---------- Input (keyboard + controller) ----------

    void Update()
    {
        if (MLGame.Playing) { if (GUIUtility.keyboardControl != 0) GUIUtility.keyboardControl = 0; return; }   // hidden while you play
        float dt = Time.unscaledDeltaTime;
        mouseMoved = Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.5f;
        for (int i = 0; i < bubbles.Length; i++)
        {
            bubbles[i].y -= bubbles[i].speed * dt;
            if (bubbles[i].y < -0.05f) bubbles[i] = NewBubble(1.05f);
        }

        // typing in a text box: arrows / letters type, Enter does the button, Esc leaves the box
        if (GUIUtility.keyboardControl != 0)
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            {
                GUIUtility.keyboardControl = 0;
                if (screen == Screen2.Coop && Net.Mode == Net.State.Offline)
                {
                    if (focusedCtrl == "code") ChooseCoop(3);       // Enter in the code box = JOIN
                    else CommitName();
                }
            }
            else if (kb != null && kb.escapeKey.wasPressedThisFrame) GUIUtility.keyboardControl = 0;
            return;
        }
        int rows = RowCount();
        Vector2Int nav = Nav();
        if (nav.y != 0) { sel = (sel - nav.y + rows) % rows; Click(0.35f); }
        if (nav.x != 0 && screen == Screen2.Settings) ChangeSetting(sel, nav.x);
        if (Confirm()) Choose(sel);
        if (Back() && screen != Screen2.Main) GoTo(Screen2.Main);
    }

    int RowCount()
    {
        if (screen == Screen2.Main) return 3;
        if (screen == Screen2.Settings) return 7;
        switch (Net.Mode)
        {
            case Net.State.Offline: return 5;      // name, host, code, join, back
            case Net.State.Working: return 1;      // cancel
            case Net.State.Hosting: return 3;      // start, copy code, leave
            default: return 1;                     // leave
        }
    }

    static bool Confirm()
    {
        var k = Keyboard.current; var g = Gamepad.current;
        return (k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame))
            || (g != null && (g.buttonSouth.wasPressedThisFrame || g.startButton.wasPressedThisFrame));
    }

    static bool Back()
    {
        var k = Keyboard.current; var g = Gamepad.current;
        return (k != null && (k.escapeKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame)) || (g != null && g.buttonEast.wasPressedThisFrame);
    }

    // Arrow keys / WASD / stick / d-pad, with key-repeat when held.
    Vector2Int Nav()
    {
        Vector2 v = Vector2.zero;
        var k = Keyboard.current;
        if (k != null)
        {
            if (k.upArrowKey.isPressed || k.wKey.isPressed) v.y += 1f;
            if (k.downArrowKey.isPressed || k.sKey.isPressed) v.y -= 1f;
            if (k.leftArrowKey.isPressed || k.aKey.isPressed) v.x -= 1f;
            if (k.rightArrowKey.isPressed || k.dKey.isPressed) v.x += 1f;
        }
        var g = Gamepad.current;
        if (g != null)
        {
            Vector2 s = g.leftStick.ReadValue();
            if (s.magnitude > 0.5f) v += s;
            if (g.dpad.up.isPressed) v.y += 1f;
            if (g.dpad.down.isPressed) v.y -= 1f;
            if (g.dpad.left.isPressed) v.x -= 1f;
            if (g.dpad.right.isPressed) v.x += 1f;
        }
        var d = new Vector2Int(Mathf.Abs(v.x) > 0.5f ? (int)Mathf.Sign(v.x) : 0, Mathf.Abs(v.y) > 0.5f ? (int)Mathf.Sign(v.y) : 0);
        if (d.y != 0) d.x = 0;
        float t = Time.unscaledTime;
        if (d == Vector2Int.zero) { lastNav = d; return d; }
        if (d != lastNav) { lastNav = d; repeatAt = t + 0.4f; return d; }
        if (t >= repeatAt) { repeatAt = t + 0.1f; return d; }
        return Vector2Int.zero;
    }

    void Choose(int row)
    {
        switch (screen)
        {
            case Screen2.Main:
                if (row == 0)
                {
                    Click(0.6f);
                    if (!MLGame.ArenaPresent) Toast("No arena yet! In Unity: Tools > Mad Lab > 2. Create Lab Arena");
                    else MLGame.StartMatch(0);
                }
                else if (row == 1) GoTo(Screen2.Settings);
                else GoTo(Screen2.Coop);
                break;
            case Screen2.Settings:
                if (row == 6) GoTo(Screen2.Main);
                else ChangeSetting(row, 1);
                break;
            case Screen2.Coop:
                ChooseCoop(row);
                break;
        }
    }

    void GoTo(Screen2 s)
    {
        screen = s;
        sel = 0;
        openedAt = Time.unscaledTime;
        Click(0.5f);
        if (s == Screen2.Main) Settings.Save();
    }

    void ChangeSetting(int row, int dir)
    {
        switch (row)
        {
            case 0: Settings.Volume = Mathf.Clamp01(Settings.Volume + dir * 0.1f); break;
            case 1: Settings.Music = Mathf.Clamp01(Settings.Music + dir * 0.1f); break;
            case 2: Settings.Sensitivity = Mathf.Clamp(Settings.Sensitivity + dir * 0.25f, 0.25f, 5f); break;
            case 3: Settings.Fullscreen = !Settings.Fullscreen; break;
            case 5: Settings.FirstPerson = !Settings.FirstPerson; break;
            case 4:
                int n = QualitySettings.names.Length;
                Settings.Quality = (Settings.Quality + dir + n) % n;
                break;
            default: return;
        }
        Settings.Apply();
        Click(0.4f);
    }

    void Toast(string s) { toast = s; toastAt = Time.unscaledTime; }

    // A tiny "tick" made in code (there are no sound files yet).
    static AudioClip tick;
    static AudioSource src;
    static void Click(float vol)
    {
        if (tick == null)
        {
            const int n = 1600;
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / 44100f;
                data[i] = Mathf.Sin(2f * Mathf.PI * 1300f * t) * Mathf.Exp(-t * 90f) * 0.5f;
            }
            tick = AudioClip.Create("tick", n, 1, 44100, false);
            tick.SetData(data, 0);
        }
        if (src == null)
        {
            var go = new GameObject("Menu Sound");
            DontDestroyOnLoad(go);
            src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
        }
        src.PlayOneShot(tick, vol);
    }

    // ---------- Drawing ----------

    static float S => UnityEngine.Screen.height / 1080f;
    static float W => UnityEngine.Screen.width / S;
    static Rect R(float x, float y, float w, float h) => new Rect(x * S, y * S, w * S, h * S);

    static Texture2D White
    {
        get
        {
            if (white == null) { white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply(); }
            return white;
        }
    }

    static Texture2D Circle
    {
        get
        {
            if (circle != null) return circle;
            const int N = 64;
            circle = new Texture2D(N, N, TextureFormat.RGBA32, false);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(N / 2f, N / 2f)) / (N / 2f);
                    float a = Mathf.Clamp01((1f - d) * 8f);                 // solid disc
                    float rim = Mathf.Clamp01(1f - Mathf.Abs(d - 0.85f) * 10f);  // brighter edge
                    circle.SetPixel(x, y, new Color(1f, 1f, 1f, a * (0.35f + 0.65f * rim)));
                }
            circle.Apply();
            return circle;
        }
    }

    static void Fill(Rect r, Color c)
    {
        Color old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, White);
        GUI.color = old;
    }

    static GUIStyle Style(int size, bool bold, TextAnchor anchor)
    {
        int px = Mathf.Max(8, Mathf.RoundToInt(size * S));
        int key = px * 100 + (bold ? 50 : 0) + (int)anchor;
        if (!styles.TryGetValue(key, out GUIStyle s))
        {
            s = new GUIStyle(GUI.skin.label) { fontSize = px, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal, alignment = anchor, wordWrap = false, clipping = TextClipping.Overflow };
            s.normal.textColor = Color.white;
            styles[key] = s;
        }
        return s;
    }

    static void Text(Rect r, string t, int size, Color c, TextAnchor a = TextAnchor.MiddleLeft, bool bold = true)
    {
        GUIStyle s = Style(size, bold, a);
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, c.a * 0.6f);
        GUI.Label(new Rect(r.x + 2f * S, r.y + 2f * S, r.width, r.height), t, s);
        GUI.color = c;
        GUI.Label(r, t, s);
        GUI.color = old;
    }

    float Appear(float delay) => Mathf.Clamp01((Time.unscaledTime - openedAt - delay) / 0.35f);

    void OnGUI()
    {
        if (MLGame.Playing) return;
        focusedCtrl = GUI.GetNameOfFocusedControl();
        float sw = UnityEngine.Screen.width, sh = UnityEngine.Screen.height;
        DrawBackground(sw, sh);
        DrawTitle();
        switch (screen)
        {
            case Screen2.Main: DrawMain(); break;
            case Screen2.Settings: DrawSettings(); break;
            case Screen2.Coop: DrawCoop(); break;
        }
        // message (e.g. "No map yet")
        float ta = Time.unscaledTime - toastAt;
        if (ta < 2.6f && !string.IsNullOrEmpty(toast))
        {
            float a = ta < 2.1f ? 1f : 1f - (ta - 2.1f) / 0.5f;
            Rect tr = R(W / 2f - 380f, 960f, 760f, 54f);
            Fill(tr, new Color(0f, 0f, 0f, 0.7f * a));
            Fill(new Rect(tr.x, tr.y, 4f * S, tr.height), new Color(Green.r, Green.g, Green.b, a));
            Text(tr, toast, 24, new Color(1f, 1f, 1f, a), TextAnchor.MiddleCenter);
        }
        Text(R(0f, 1040f, W - 24f, 30f), "v0.1  ·  prototype", 15, new Color(1f, 1f, 1f, 0.3f), TextAnchor.MiddleRight, false);
    }

    void DrawBackground(float sw, float sh)
    {
        // dark lab gradient (see-through when there's an arena behind, so you see the map fly-by)
        float bgA = MLGame.ArenaPresent ? 0.45f : 1f;
        for (int i = 0; i < 12; i++)
        {
            float k = i / 11f;
            Color g = Color.Lerp(new Color(0.03f, 0.06f, 0.07f), new Color(0.01f, 0.015f, 0.02f), k);
            g.a = bgA;
            Fill(new Rect(0f, sh * k, sw, sh / 11f + 1f), g);
        }
        // faint lab-grid lines
        Color line = new Color(0.4f, 1f, 0.6f, 0.035f);
        float step = 80f * S;
        for (float x = (Time.unscaledTime * 6f * S) % step; x < sw; x += step) Fill(new Rect(x, 0f, 1f, sh), line);
        for (float y = 0f; y < sh; y += step) Fill(new Rect(0f, y, sw, 1f), line);
        // glowing bubbles floating up
        foreach (Bubble b in bubbles)
        {
            float x = (b.x + Mathf.Sin(Time.unscaledTime * 0.8f + b.wobble) * 0.008f) * sw;
            float y = b.y * sh;
            float r = b.r * sh;
            Color c = Color.HSVToRGB(b.hue, 0.7f, 1f);
            c.a = 0.08f + 0.18f * (b.r / 0.022f);
            Color old = GUI.color;
            GUI.color = new Color(c.r, c.g, c.b, c.a * 0.35f);
            GUI.DrawTexture(new Rect(x - r * 2f, y - r * 2f, r * 4f, r * 4f), Circle);     // glow
            GUI.color = c;
            GUI.DrawTexture(new Rect(x - r, y - r, r * 2f, r * 2f), Circle);
            GUI.color = old;
        }
        // darker on the left behind the menu
        for (int i = 0; i < 10; i++)
            Fill(new Rect(sw * 0.06f * i, 0f, sw * 0.06f + 1f, sh), new Color(0f, 0f, 0f, (MLGame.ArenaPresent ? 0.55f : 0.4f) * (1f - i / 10f)));
    }

    void DrawTitle()
    {
        float a = Appear(0f);
        float x = 120f - (1f - a) * 30f;
        // pulsing green glow behind the title
        float glow = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2f);
        Text(R(x, 150f, 1200f, 40f), "A  CHAOTIC  CRAFTING  BATTLE", 22, new Color(Purple.r, Purple.g, Purple.b, a));
        Text(R(x - 4f, 185f, 1200f, 170f), GameTitle, 150, new Color(Green.r, Green.g, Green.b, (0.15f + 0.1f * glow) * a));
        Text(R(x, 190f, 1200f, 160f), GameTitle, 144, new Color(1f, 1f, 1f, a));
        Fill(R(x, 355f, 110f, 6f), new Color(Green.r, Green.g, Green.b, a));
        Text(R(x, 372f, 1200f, 40f), Tagline, 24, new Color(1f, 1f, 1f, 0.7f * a), TextAnchor.MiddleLeft, false);
    }

    // A big menu button. Returns true when clicked.
    bool Button(int row, Rect r, string label, string hint, bool primary)
    {
        bool over = r.Contains(Event.current.mousePosition);
        if (over && mouseMoved) sel = row;
        bool on = sel == row;
        if (Event.current.type == EventType.Repaint) hover[row] = Mathf.MoveTowards(hover[row], on ? 1f : 0f, Time.unscaledDeltaTime * 8f);
        float h = hover[row];
        float a = Appear(0.15f + row * 0.08f);
        r.x -= (1f - a) * 40f * S;
        if (primary)
        {
            Fill(r, Color.Lerp(new Color(0.2f, 0.62f, 0.15f, a), new Color(0.35f, 0.85f, 0.25f, a), h));
            Fill(new Rect(r.x, r.yMax - 4f * S, r.width, 4f * S), new Color(0f, 0f, 0f, 0.3f * a));
        }
        else
        {
            Fill(r, new Color(0.04f, 0.06f, 0.08f, (0.75f + 0.15f * h) * a));
            Fill(r, new Color(1f, 1f, 1f, 0.08f * h * a));
            Fill(new Rect(r.x, r.y, (3f + 4f * h) * S, r.height), Color.Lerp(new Color(1f, 1f, 1f, 0.25f * a), new Color(Green.r, Green.g, Green.b, a), h));
        }
        float pad = (26f + 10f * h) * S;
        Text(new Rect(r.x + pad, r.y, r.width - pad, r.height), label, primary ? 44 : 32, new Color(1f, 1f, 1f, a));
        if (!string.IsNullOrEmpty(hint))
            Text(new Rect(r.x, r.y, r.width - 24f * S, r.height), hint, 16, new Color(1f, 1f, 1f, (primary ? 0.85f : 0.3f + 0.5f * h) * a), TextAnchor.MiddleRight);
        if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { sel = row; return true; }
        return false;
    }

    void DrawMain()
    {
        bool pad = Gamepad.current != null;
        float x = 120f, y = 470f;
        if (Button(0, R(x, y, 520f, 92f), "PLAY", pad ? "A" : "ENTER", true)) Choose(0);
        if (Button(1, R(x, y + 112f, 520f, 72f), "SETTINGS", "VOLUME, CONTROLS, GRAPHICS", false)) Choose(1);
        if (Button(2, R(x, y + 196f, 520f, 72f), "CO-OP", Net.Active ? "IN A LOBBY - " + Net.JoinCode : "PLAY ONLINE WITH A CODE", false)) Choose(2);
        string help = pad ? "Stick / D-pad to choose   ·   A to select" : "Arrows / WASD to choose   ·   Enter to select   ·   or use the mouse";
        Text(R(x, y + 300f, 900f, 30f), help, 17, new Color(1f, 1f, 1f, 0.4f * Appear(0.5f)), TextAnchor.MiddleLeft, false);
    }

    void PanelFrame(string title, float height)
    {
        float a = Appear(0f);
        Rect p = R(120f, 450f, 760f, height);
        Fill(p, new Color(Panel.r, Panel.g, Panel.b, Panel.a * a));
        Fill(new Rect(p.x, p.y, p.width, 4f * S), new Color(Green.r, Green.g, Green.b, a));
        Text(R(150f, 462f, 700f, 60f), title, 36, new Color(1f, 1f, 1f, a));
    }

    // One settings row: label on the left, value with < > on the right (and a bar for sliders).
    void SettingRow(int row, string label, string value, float bar)
    {
        Rect r = R(140f, 540f + row * 64f, 720f, 56f);
        bool over = r.Contains(Event.current.mousePosition);
        if (over && mouseMoved) sel = row;
        bool on = sel == row;
        Fill(r, on ? new Color(Green.r, Green.g, Green.b, 0.18f) : new Color(1f, 1f, 1f, 0.04f));
        if (on) Fill(new Rect(r.x, r.y, 5f * S, r.height), Green);
        Text(new Rect(r.x + 22f * S, r.y, 300f * S, r.height), label, 24, Color.white);
        Rect left = new Rect(r.x + 330f * S, r.y + 8f * S, 40f * S, 40f * S);
        Rect right = new Rect(r.xMax - 52f * S, r.y + 8f * S, 40f * S, 40f * S);
        Text(left, "<", 26, new Color(1f, 1f, 1f, on ? 1f : 0.5f), TextAnchor.MiddleCenter);
        Text(right, ">", 26, new Color(1f, 1f, 1f, on ? 1f : 0.5f), TextAnchor.MiddleCenter);
        Rect mid = new Rect(left.xMax + 8f * S, r.y, right.x - left.xMax - 16f * S, r.height);
        if (bar >= 0f)
        {
            Rect track = new Rect(mid.x, mid.center.y - 4f * S, mid.width * 0.62f, 8f * S);
            Fill(track, new Color(1f, 1f, 1f, 0.12f));
            Fill(new Rect(track.x, track.y, track.width * bar, track.height), Green);
            Text(new Rect(track.xMax + 10f * S, r.y, mid.xMax - track.xMax - 10f * S, r.height), value, 22, Color.white, TextAnchor.MiddleCenter);
            // click / drag on the bar to set it
            Event e = Event.current;
            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && new Rect(track.x, r.y, track.width, r.height).Contains(e.mousePosition))
            {
                sel = row;
                SetFromBar(row, Mathf.Clamp01((e.mousePosition.x - track.x) / track.width));
                e.Use();
            }
        }
        else Text(mid, value, 22, Color.white, TextAnchor.MiddleCenter);
        if (GUI.Button(left, GUIContent.none, GUIStyle.none)) { sel = row; ChangeSetting(row, -1); }
        else if (GUI.Button(right, GUIContent.none, GUIStyle.none)) { sel = row; ChangeSetting(row, 1); }
    }

    void SetFromBar(int row, float k)
    {
        if (row == 0) Settings.Volume = Mathf.Round(k * 20f) / 20f;
        else if (row == 1) Settings.Music = Mathf.Round(k * 20f) / 20f;
        else if (row == 2) Settings.Sensitivity = Mathf.Round(Mathf.Lerp(0.25f, 5f, k) * 4f) / 4f;
        Settings.Apply();
    }

    void DrawSettings()
    {
        PanelFrame("SETTINGS", 584f);
        SettingRow(0, "Master Volume", Mathf.RoundToInt(Settings.Volume * 100f) + "%", Settings.Volume);
        SettingRow(1, "Music", Mathf.RoundToInt(Settings.Music * 100f) + "%", Settings.Music);
        SettingRow(2, "Mouse Sensitivity", Settings.Sensitivity.ToString("0.00"), Mathf.InverseLerp(0.25f, 5f, Settings.Sensitivity));
        SettingRow(3, "Fullscreen", Settings.Fullscreen ? "ON" : "OFF", -1f);
        string[] q = QualitySettings.names;
        SettingRow(4, "Graphics", q.Length > 0 ? q[Mathf.Clamp(Settings.Quality, 0, q.Length - 1)].ToUpperInvariant() : "-", -1f);
        SettingRow(5, "Camera", Settings.FirstPerson ? "1ST PERSON" : "3RD PERSON", -1f);
        BackButton(6, R(140f, 540f + 6 * 64f + 8f, 240f, 56f));
    }

    // ---------- Co-op ----------

    void ChooseCoop(int row)
    {
        switch (Net.Mode)
        {
            case Net.State.Offline:
                if (row == 0) focusRequest = "name";
                else if (row == 1) { CommitName(); Click(0.6f); Net.Host(); }
                else if (row == 2) focusRequest = "code";
                else if (row == 3) { CommitName(); Click(0.6f); Net.Join(codeDraft); }
                else GoTo(Screen2.Main);
                break;
            case Net.State.Working:
                Net.Cancel();
                Click(0.5f);
                break;
            case Net.State.Hosting:
                if (row == 0)
                {
                    if (NetLobby.Instance != null && NetLobby.Instance.Members.Count < 2) Toast("Starting on your own - invite a friend with the code!");
                    NetLobby.Instance?.StartGame();
                    Click(0.7f);
                }
                else if (row == 1) { GUIUtility.systemCopyBuffer = Net.JoinCode; Toast("Code copied: " + Net.JoinCode); Click(0.5f); }
                else { Net.Leave(); Click(0.5f); sel = 0; }
                break;
            default:
                Net.Leave();
                Click(0.5f);
                sel = 0;
                break;
        }
    }

    void CommitName()
    {
        if (nameDraft == null) return;
        string n = NameFilter.Tidy(nameDraft).Trim();
        if (n == Net.PlayerName) { nameError = null; return; }
        if (NameFilter.IsAllowed(n, out string why)) { Net.PlayerName = n; nameError = null; }
        else { nameError = why; nameDraft = Net.PlayerName; }
    }

    static GUIStyle fieldStyle;
    GUIStyle FieldStyle(int size)
    {
        int px = Mathf.Max(8, Mathf.RoundToInt(size * S));
        if (fieldStyle == null || fieldStyle.fontSize != px)
        {
            fieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = px, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            fieldStyle.normal.background = fieldStyle.focused.background = fieldStyle.hover.background = White;
            fieldStyle.normal.textColor = fieldStyle.focused.textColor = fieldStyle.hover.textColor = Color.white;
            fieldStyle.padding = new RectOffset(Mathf.RoundToInt(16 * S), 8, 4, 4);
        }
        return fieldStyle;
    }

    // A text box row. Returns the new text.
    string Field(int row, Rect r, string label, string ctrl, string value, int maxLen, bool upper)
    {
        bool over = r.Contains(Event.current.mousePosition);
        if (over && mouseMoved) sel = row;
        bool on = sel == row;
        Fill(r, on ? new Color(Green.r, Green.g, Green.b, 0.18f) : new Color(1f, 1f, 1f, 0.04f));
        if (on) Fill(new Rect(r.x, r.y, 5f * S, r.height), Green);
        Text(new Rect(r.x + 22f * S, r.y, 220f * S, r.height), label, 22, Color.white);
        Rect box = new Rect(r.x + 240f * S, r.y + 7f * S, r.width - 252f * S, r.height - 14f * S);
        Color old = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
        GUI.SetNextControlName(ctrl);
        string v = GUI.TextField(box, value ?? "", maxLen, FieldStyle(24));
        GUI.backgroundColor = old;
        if (focusRequest == ctrl) { GUI.FocusControl(ctrl); focusRequest = null; }
        if (upper) v = CleanCode(v);
        return v;
    }

    static string CleanCode(string v)
    {
        var sb = new System.Text.StringBuilder();
        foreach (char c in v.ToUpperInvariant()) if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')) sb.Append(c);
        return sb.ToString();
    }

    void DrawCoop()
    {
        switch (Net.Mode)
        {
            case Net.State.Offline: DrawCoopStart(); break;
            case Net.State.Working: DrawCoopWorking(); break;
            default: DrawLobby(); break;
        }
    }

    void StatusLine(float y)
    {
        if (string.IsNullOrEmpty(Net.Status)) return;
        Text(R(150f, y, 700f, 32f), Net.Status, 19, Net.StatusIsError ? new Color(1f, 0.5f, 0.45f) : new Color(Green.r, Green.g, Green.b), TextAnchor.MiddleLeft, false);
    }

    void DrawCoopStart()
    {
        PanelFrame("CO-OP", 470f);
        Text(R(150f, 515f, 700f, 30f), "Host a game and send your friends the code, or type a friend's code to join.", 17, new Color(1f, 1f, 1f, 0.55f), TextAnchor.MiddleLeft, false);
        if (nameDraft == null) nameDraft = Net.PlayerName;
        string before = nameDraft;
        nameDraft = Field(0, R(140f, 556f, 720f, 60f), "YOUR NAME", "name", nameDraft, NameFilter.MaxLength, false);
        if (GUI.GetNameOfFocusedControl() != "name" && nameDraft != Net.PlayerName && before == nameDraft) CommitName();
        if (nameError != null) Text(R(390f, 614f, 470f, 24f), nameError, 15, new Color(1f, 0.5f, 0.45f), TextAnchor.MiddleLeft, false);
        if (Button(1, R(140f, 640f, 720f, 62f), "HOST A GAME", "GET A CODE", false)) ChooseCoop(1);
        codeDraft = Field(2, R(140f, 716f, 520f, 60f), "JOIN CODE", "code", codeDraft, 8, true);
        Rect join = R(672f, 716f, 188f, 60f);
        bool on = sel == 3;
        if (join.Contains(Event.current.mousePosition) && mouseMoved) sel = 3;
        Fill(join, on ? Green : new Color(1f, 1f, 1f, 0.1f));
        Text(join, "JOIN", 26, on ? new Color(0.05f, 0.08f, 0.05f) : Color.white, TextAnchor.MiddleCenter);
        if (GUI.Button(join, GUIContent.none, GUIStyle.none)) ChooseCoop(3);
        BackButton(4, R(140f, 792f, 240f, 56f));
        StatusLine(856f);
    }

    void DrawCoopWorking()
    {
        PanelFrame("CO-OP", 300f);
        string dots = new string('.', 1 + (int)(Time.unscaledTime * 3f) % 3);
        Text(R(150f, 540f, 700f, 50f), Net.Status.TrimEnd('.') + dots, 28, Color.white);
        Text(R(150f, 590f, 700f, 30f), "This can take a few seconds.", 17, new Color(1f, 1f, 1f, 0.5f), TextAnchor.MiddleLeft, false);
        Rect cancel = R(140f, 660f, 240f, 56f);
        Fill(cancel, Green);
        Text(cancel, "CANCEL", 24, new Color(0.05f, 0.08f, 0.05f), TextAnchor.MiddleCenter);
        if (GUI.Button(cancel, GUIContent.none, GUIStyle.none)) ChooseCoop(0);
    }

    void DrawLobby()
    {
        bool host = Net.Mode == Net.State.Hosting;
        var members = NetLobby.Instance != null ? NetLobby.Instance.Members : new System.Collections.Generic.List<NetLobby.Member>();
        PanelFrame(host ? "YOUR LOBBY" : "LOBBY", 560f);
        // the code, big
        Text(R(150f, 515f, 400f, 30f), host ? "SEND THIS CODE TO YOUR FRIENDS" : "JOINED GAME", 17, new Color(1f, 1f, 1f, 0.55f));
        Text(R(150f, 540f, 700f, 80f), Net.JoinCode, 64, new Color(Green.r, Green.g, Green.b));
        // who's in
        Text(R(560f, 515f, 290f, 30f), "SCIENTISTS  " + members.Count + " / " + Net.MaxPlayers, 17, new Color(1f, 1f, 1f, 0.55f), TextAnchor.MiddleRight);
        float y = 630f;
        for (int i = 0; i < Net.MaxPlayers; i++)
        {
            Rect r = R(140f + (i % 2) * 364f, y + (i / 2) * 50f, 356f, 44f);
            if (i < members.Count)
            {
                NetLobby.Member mem = members[i];
                Fill(r, new Color(1f, 1f, 1f, 0.07f));
                Fill(new Rect(r.x, r.y, 4f * S, r.height), mem.host ? new Color(1f, 0.82f, 0.3f) : Green);
                Text(new Rect(r.x + 16f * S, r.y, r.width - 100f * S, r.height), mem.name, 21, Color.white);
                if (mem.host) Text(new Rect(r.x, r.y, r.width - 12f * S, r.height), "HOST", 15, new Color(1f, 0.82f, 0.3f), TextAnchor.MiddleRight);
            }
            else
            {
                Fill(r, new Color(1f, 1f, 1f, 0.02f));
                Text(new Rect(r.x + 16f * S, r.y, r.width, r.height), "empty", 17, new Color(1f, 1f, 1f, 0.2f), TextAnchor.MiddleLeft, false);
            }
        }
        float by = 840f;
        if (host)
        {
            if (Button(0, R(140f, by, 300f, 62f), "START", members.Count < 2 ? "SOLO" : members.Count + " PLAYERS", true)) ChooseCoop(0);
            SmallButton(1, R(452f, by + 3f, 200f, 56f), "COPY CODE");
            SmallButton(2, R(664f, by + 3f, 196f, 56f), "LEAVE");
        }
        else
        {
            Text(R(150f, by, 480f, 62f), "Waiting for the host to start...", 22, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleLeft, false);
            SmallButton(0, R(664f, by + 3f, 196f, 56f), "LEAVE");
        }
        StatusLine(910f);
    }

    void SmallButton(int row, Rect r, string label)
    {
        if (r.Contains(Event.current.mousePosition) && mouseMoved) sel = row;
        bool on = sel == row;
        Fill(r, on ? Green : new Color(1f, 1f, 1f, 0.1f));
        Text(r, label, 22, on ? new Color(0.05f, 0.08f, 0.05f) : Color.white, TextAnchor.MiddleCenter);
        if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { sel = row; ChooseCoop(row); }
    }

    void BackButton(int row, Rect r)
    {
        bool over = r.Contains(Event.current.mousePosition);
        if (over && mouseMoved) sel = row;
        bool on = sel == row;
        Fill(r, on ? Green : new Color(1f, 1f, 1f, 0.08f));
        Text(r, "BACK", 24, on ? new Color(0.05f, 0.08f, 0.05f) : Color.white, TextAnchor.MiddleCenter);
        if (GUI.Button(r, GUIContent.none, GUIStyle.none)) GoTo(Screen2.Main);
    }
}

// Player settings, saved between sessions.
public static class Settings
{
    public static float Volume = 0.8f, Music = 0.6f, Sensitivity = 1.5f;
    public static bool Fullscreen = true;
    public static bool FirstPerson = true;      // camera: true = 1st person (default), false = 3rd person over the shoulder
    public static int Quality = -1;
    static bool loaded;

    static void Load()
    {
        if (loaded) return;
        loaded = true;
        Volume = PlayerPrefs.GetFloat("set.volume", 0.8f);
        Music = PlayerPrefs.GetFloat("set.music", 0.6f);
        Sensitivity = PlayerPrefs.GetFloat("set.sens", 1.5f);
        Fullscreen = PlayerPrefs.GetInt("set.fullscreen", 1) == 1;
        FirstPerson = PlayerPrefs.GetInt("set.firstperson", 1) == 1;
        Quality = PlayerPrefs.GetInt("set.quality", QualitySettings.GetQualityLevel());
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => loaded = false;

    public static void Apply()
    {
        Load();
        AudioListener.volume = Volume;
        if (Quality < 0) Quality = QualitySettings.GetQualityLevel();
        int n = QualitySettings.names.Length;
        if (n > 0 && Quality < n && QualitySettings.GetQualityLevel() != Quality) QualitySettings.SetQualityLevel(Quality, true);
        if (!Application.isEditor && UnityEngine.Screen.fullScreen != Fullscreen) UnityEngine.Screen.fullScreen = Fullscreen;
    }

    public static void Save()
    {
        PlayerPrefs.SetFloat("set.volume", Volume);
        PlayerPrefs.SetFloat("set.music", Music);
        PlayerPrefs.SetFloat("set.sens", Sensitivity);
        PlayerPrefs.SetInt("set.fullscreen", Fullscreen ? 1 : 0);
        PlayerPrefs.SetInt("set.firstperson", FirstPerson ? 1 : 0);
        PlayerPrefs.SetInt("set.quality", Quality);
        PlayerPrefs.Save();
    }
}
