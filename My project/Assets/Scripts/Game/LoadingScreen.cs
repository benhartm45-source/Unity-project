using System.Collections;
using UnityEngine;

// LOADING SCREEN: a big flask that fills up with glowing green liquid, a progress bar, what's being "prepared"
// and a random tip. Shows when the game starts up (while the map, trees and shaders get ready) and every
// time a match starts (PLAY, Play Again, Restart, co-op START).
public class LoadingScreen : MonoBehaviour
{
    static LoadingScreen I;
    public static bool Active => I != null && I.active;

    bool active;
    float alpha, progress, shownProgress, shownAt;
    string title = "", stage = "", tip = "";
    Texture2D white, circle, ring;

    static readonly string[] Tips =
    {
        "You can only craft inside YOUR lab - and its door only opens for you.",
        "Walk into your lab and everything you're carrying gets stored safely.",
        "If your lab is destroyed, getting knocked out means you're OUT.",
        "Plasma Launchers do extra damage to labs. Great for raiding!",
        "A Forcefield blocks shots from outside and recharges after a few seconds.",
        "Bounce Boots give you a double jump - and no fall damage.",
        "Toxic Bombs eat away at lab walls. Throw them over a forcefield... from inside it!",
        "Enchant your gun at the Enchanting Table: Fire burns, Ice slows, Poison stacks up.",
        "Press Q to quickly drink a Healing Potion.",
        "Press V to switch between 1st and 3rd person.",
        "Guns overheat if you hold the trigger. Short bursts!",
        "Every lab has a Herb spot and a Metal spot right outside the door.",
        "Big crystal piles, scrap heaps and toxic barrels can be gathered too.",
        "Stay inside the zone during MADNESS - labs outside it melt down.",
        "Brewing takes time, but you can walk away while the cauldron bubbles.",
        "The Freeze Ray slows things down... then freezes them solid.",
    };

    static readonly string[] Stages =
    {
        "Mixing chemicals...", "Calibrating the reactor...", "Growing mutant trees...", "Polishing crystals...",
        "Charging the zappers...", "Feeding the slime...", "Labelling the potions...", "Waking up the scientists...",
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => I = null;

    // Shown straight away when the game opens.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() => Run("STARTING UP", null, 2.2f);

    static LoadingScreen Get()
    {
        if (I == null)
        {
            var go = new GameObject("Loading Screen");
            DontDestroyOnLoad(go);
            I = go.AddComponent<LoadingScreen>();
        }
        return I;
    }

    // Show the loading screen, do 'work' behind it, then fade out once the game is running smoothly.
    public static void Run(string title, System.Action work, float minSeconds = 1.6f)
    {
        LoadingScreen l = Get();
        l.StopAllCoroutines();
        l.StartCoroutine(l.Load(title, work, minSeconds));
    }

    IEnumerator Load(string t, System.Action work, float minSeconds)
    {
        active = true;
        alpha = 1f;
        title = t;
        progress = shownProgress = 0f;
        shownAt = Time.unscaledTime;
        tip = Tips[Random.Range(0, Tips.Length)];
        stage = Stages[Random.Range(0, Stages.Length)];

        // two frames so the screen is definitely showing before the heavy work starts
        yield return null;
        yield return null;
        progress = 0.25f;
        if (work != null)
        {
            try { work(); }
            catch (System.Exception e) { Debug.LogException(e); }
        }
        progress = 0.55f;

        // wait until the game is drawing smoothly (the first frames of a big map are slow)
        int smooth = 0, frames = 0;
        while (Time.unscaledTime - shownAt < minSeconds || smooth < 8)
        {
            frames++;
            smooth = Time.unscaledDeltaTime < 0.05f ? smooth + 1 : 0;
            float timeK = Mathf.Clamp01((Time.unscaledTime - shownAt) / minSeconds);
            progress = Mathf.Max(progress, Mathf.Lerp(0.55f, 0.95f, timeK * Mathf.Clamp01(smooth / 8f + 0.3f)));
            if (Random.value < Time.unscaledDeltaTime * 1.2f) stage = Stages[Random.Range(0, Stages.Length)];
            if (frames > 600) break;     // never get stuck
            yield return null;
        }
        progress = 1f;
        stage = "Ready!";
        yield return new WaitForSecondsRealtime(0.35f);

        // fade away
        float f = 0f;
        while (f < 1f)
        {
            f += Time.unscaledDeltaTime / 0.4f;
            alpha = 1f - f;
            yield return null;
        }
        alpha = 0f;
        active = false;
    }

    void Update()
    {
        shownProgress = Mathf.MoveTowards(shownProgress, progress, Time.unscaledDeltaTime * 0.9f);
    }

    // ---------------- drawing ----------------

    void MakeTextures()
    {
        if (white != null) return;
        white = new Texture2D(1, 1);
        white.SetPixel(0, 0, Color.white);
        white.Apply();
        const int N = 128;
        circle = new Texture2D(N, N, TextureFormat.RGBA32, false);
        ring = new Texture2D(N, N, TextureFormat.RGBA32, false);
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(N / 2f, N / 2f)) / (N / 2f);
                circle.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01((1f - d) * 30f)));
                ring.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01((1f - d) * 30f) * Mathf.Clamp01((d - 0.9f) * 30f)));
            }
        circle.Apply();
        ring.Apply();
        circle.wrapMode = ring.wrapMode = TextureWrapMode.Clamp;
    }

    void Fill(Rect r, Color c)
    {
        GUI.color = new Color(c.r, c.g, c.b, c.a * alpha);
        GUI.DrawTexture(r, white);
    }

    void Tex(Rect r, Texture2D t, Color c)
    {
        GUI.color = new Color(c.r, c.g, c.b, c.a * alpha);
        GUI.DrawTexture(r, t);
    }

    void Label(Rect r, string text, int size, Color c, TextAnchor a, bool bold = true)
    {
        var st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(8, Mathf.RoundToInt(size * Screen.height / 1080f)), fontStyle = bold ? FontStyle.Bold : FontStyle.Normal, alignment = a, wordWrap = true };
        st.normal.textColor = Color.white;
        GUI.color = new Color(0f, 0f, 0f, 0.6f * alpha * c.a);
        GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), text, st);
        GUI.color = new Color(c.r, c.g, c.b, c.a * alpha);
        GUI.Label(r, text, st);
    }

    void OnGUI()
    {
        if (!active && alpha <= 0f) return;
        MakeTextures();
        GUI.depth = -1000;     // on top of everything
        float sw = Screen.width, sh = Screen.height, s = sh / 1080f;
        Color green = new Color(0.45f, 1f, 0.35f);
        float t = Time.unscaledTime;

        // background: dark lab gradient with a moving grid
        for (int i = 0; i < 12; i++)
            Fill(new Rect(0f, sh * i / 12f, sw, sh / 12f + 1f), Color.Lerp(new Color(0.03f, 0.06f, 0.07f), new Color(0.01f, 0.015f, 0.02f), i / 11f));
        float step = 80f * s;
        for (float x = (t * 10f * s) % step; x < sw; x += step) Fill(new Rect(x, 0f, 1f, sh), new Color(0.4f, 1f, 0.6f, 0.04f));
        for (float y = (t * 6f * s) % step; y < sh; y += step) Fill(new Rect(0f, y, sw, 1f), new Color(0.4f, 1f, 0.6f, 0.04f));

        // the flask
        float cx = sw / 2f, cy = sh * 0.42f, r = 150f * s;
        Rect bulb = new Rect(cx - r, cy - r, r * 2f, r * 2f);
        Tex(new Rect(cx - r * 1.6f, cy - r * 1.6f, r * 3.2f, r * 3.2f), circle, new Color(green.r, green.g, green.b, 0.07f + 0.04f * Mathf.Sin(t * 3f)));   // glow
        Fill(new Rect(cx - r * 0.28f, cy - r * 1.75f, r * 0.56f, r * 0.95f), new Color(0.75f, 0.9f, 1f, 0.12f));    // neck glass
        Fill(new Rect(cx - r * 0.36f, cy - r * 1.95f, r * 0.72f, r * 0.24f), new Color(0.55f, 0.35f, 0.2f, 1f));     // cork
        Tex(bulb, circle, new Color(0.75f, 0.9f, 1f, 0.12f));                                                         // bulb glass
        // liquid: the lower part of the circle, rising with the progress (with a little wobble)
        float level = Mathf.Clamp01(0.08f + shownProgress * 0.85f + Mathf.Sin(t * 4f) * 0.01f);
        GUI.color = new Color(green.r, green.g, green.b, 0.9f * alpha);
        GUI.DrawTextureWithTexCoords(new Rect(bulb.x, bulb.y + bulb.height * (1f - level), bulb.width, bulb.height * level), circle, new Rect(0f, 0f, 1f, level));
        GUI.color = new Color(1f, 1f, 1f, 0.25f * alpha);
        GUI.DrawTextureWithTexCoords(new Rect(bulb.x, bulb.y + bulb.height * (1f - level), bulb.width, bulb.height * 0.03f), circle, new Rect(0f, level - 0.03f, 1f, 0.03f));
        // bubbles rising inside the liquid
        for (int i = 0; i < 9; i++)
        {
            float k = Mathf.Repeat(t * (0.25f + i * 0.04f) + i * 0.37f, 1f);
            float by = cy + r * 0.9f - k * r * 1.8f * level;
            float bx = cx + Mathf.Sin(i * 2.1f + t * 2f) * r * 0.5f * (1f - k * 0.5f);
            float br = (6f + (i % 3) * 4f) * s;
            Tex(new Rect(bx - br, by - br, br * 2f, br * 2f), circle, new Color(0.85f, 1f, 0.8f, 0.6f * (1f - k)));
        }
        Tex(bulb, ring, new Color(0.85f, 0.95f, 1f, 0.9f));                                                           // outline
        Fill(new Rect(cx - r * 0.28f, cy - r * 1.75f, 3f * s, r * 0.85f), new Color(0.85f, 0.95f, 1f, 0.9f));
        Fill(new Rect(cx + r * 0.28f - 3f * s, cy - r * 1.75f, 3f * s, r * 0.85f), new Color(0.85f, 0.95f, 1f, 0.9f));
        Tex(new Rect(cx - r * 0.55f, cy - r * 0.6f, r * 0.3f, r * 0.3f), circle, new Color(1f, 1f, 1f, 0.35f));       // shine

        // title, bar, stage, tip
        Label(new Rect(0f, sh * 0.06f, sw, 110f * s), MainMenu.GameTitle, 92, Color.white, TextAnchor.MiddleCenter);
        Label(new Rect(0f, sh * 0.06f + 100f * s, sw, 40f * s), title, 24, new Color(0.7f, 0.45f, 1f), TextAnchor.MiddleCenter);
        float bw = 640f * s, bh = 16f * s, bx0 = (sw - bw) / 2f, by0 = sh * 0.7f;
        Fill(new Rect(bx0 - 3f * s, by0 - 3f * s, bw + 6f * s, bh + 6f * s), new Color(0f, 0f, 0f, 0.6f));
        Fill(new Rect(bx0, by0, bw * shownProgress, bh), green);
        Fill(new Rect(bx0, by0, bw * shownProgress, bh * 0.35f), new Color(1f, 1f, 1f, 0.25f));
        Label(new Rect(bx0, by0 + bh + 8f * s, bw, 34f * s), stage, 20, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleLeft, false);
        Label(new Rect(bx0, by0 + bh + 8f * s, bw, 34f * s), Mathf.RoundToInt(shownProgress * 100f) + "%", 20, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleRight);
        Label(new Rect(sw * 0.15f, sh * 0.84f, sw * 0.7f, 90f * s), "TIP:  " + tip, 22, new Color(1f, 1f, 1f, 0.75f), TextAnchor.MiddleCenter, false);
        GUI.color = Color.white;
    }
}
