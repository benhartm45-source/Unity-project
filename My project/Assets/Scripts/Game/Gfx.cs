using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Materials, simple shapes and little effects (sparks, smoke, flashes, beams) made in code.
public static class Gfx
{
    static readonly Dictionary<(Color, float, float), Material> lit = new Dictionary<(Color, float, float), Material>();
    static readonly Dictionary<(Color, float), Material> glow = new Dictionary<(Color, float), Material>();
    static readonly Dictionary<Color, Material> clear = new Dictionary<Color, Material>();
    static Shader litShader;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { lit.Clear(); glow.Clear(); clear.Clear(); litShader = null; }

    static Shader LitShader
    {
        get
        {
            if (litShader == null) litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");
            return litShader;
        }
    }

    // Normal solid surface.
    public static Material Lit(Color c, float smooth = 0.3f, float metal = 0f)
    {
        var key = (c, smooth, metal);
        if (lit.TryGetValue(key, out Material m) && m != null) return m;
        m = new Material(LitShader);
        m.SetColor("_BaseColor", c);
        m.color = c;
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metal);
        m.enableInstancing = true;
        lit[key] = m;
        return m;
    }

    // Glows in the dark (emission).
    public static Material Glow(Color c, float intensity = 2f)
    {
        var key = (c, intensity);
        if (glow.TryGetValue(key, out Material m) && m != null) return m;
        m = new Material(LitShader);
        m.SetColor("_BaseColor", c);
        m.color = c;
        m.SetFloat("_Smoothness", 0.6f);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", c * intensity);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        glow[key] = m;
        return m;
    }

    // See-through, not lit (forcefields, smoke, beams, sparks). Alpha in the colour = how see-through.
    public static Material Clear(Color c)
    {
        if (clear.TryGetValue(c, out Material m) && m != null) return m;
        Shader sh = Shader.Find("Sprites/Default");
        m = new Material(sh);
        m.color = c;
        m.renderQueue = 3100;
        clear[c] = m;
        return m;
    }

    public static Transform Pivot(Transform parent, Vector3 pos, string name = "Pivot")
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        return t;
    }

    // A primitive shape with no collider.
    public static GameObject Part(Transform parent, PrimitiveType shape, Vector3 pos, Vector3 size, Material m, Vector3 euler = default(Vector3), bool shadows = true)
    {
        GameObject g = GameObject.CreatePrimitive(shape);
        Collider c = g.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localRotation = Quaternion.Euler(euler);
        g.transform.localScale = size;
        var r = g.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        return g;
    }

    public static Light AddLight(Transform parent, Vector3 pos, Color c, float range, float intensity)
    {
        var go = new GameObject("Light");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = c;
        l.range = range;
        l.intensity = intensity;
        l.shadows = LightShadows.None;
        return l;
    }

    public static void SetLayer(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform) SetLayer(t.gameObject, layer);
    }

    // Hexagon pattern for forcefields (made once).
    static Texture2D hex;
    public static Texture2D HexTexture
    {
        get
        {
            if (hex != null) return hex;
            const int N = 128;
            hex = new Texture2D(N, N, TextureFormat.RGBA32, true);
            float s = N / 4f;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    // distance to the nearest hexagon edge
                    float px = x / s, py = y / s * 1.1547f;
                    float q = px - py * 0.5f, r = py;
                    float fq = q - Mathf.Floor(q), fr = r - Mathf.Floor(r);
                    float e = Mathf.Min(Mathf.Min(fq, 1f - fq), Mathf.Min(fr, 1f - fr));
                    float fs = Mathf.Repeat(q + r, 1f);
                    e = Mathf.Min(e, Mathf.Min(fs, 1f - fs));
                    float a = Mathf.Clamp01(1f - e * 9f);
                    hex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.25f + 0.75f * a));
                }
            hex.Apply();
            hex.wrapMode = TextureWrapMode.Repeat;
            return hex;
        }
    }
}

// Lightweight particles: little glowing cubes and puffs of smoke that fly about and shrink away.
public class Fx : MonoBehaviour
{
    struct P
    {
        public Transform t;
        public Vector3 v;
        public float life, max, size, grav, drag, grow;
    }

    static Fx I;
    readonly List<P> live = new List<P>();
    readonly Stack<Transform> pool = new Stack<Transform>();
    readonly List<Light> lights = new List<Light>();
    readonly List<float> lightEnd = new List<float>();
    readonly List<float> lightMax = new List<float>();
    Mesh cube, ball;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => I = null;

    static Fx Get()
    {
        if (I == null)
        {
            var go = new GameObject("FX");
            I = go.AddComponent<Fx>();
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            I.cube = c.GetComponent<MeshFilter>().sharedMesh;
            Destroy(c);
            GameObject b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            I.ball = b.GetComponent<MeshFilter>().sharedMesh;
            Destroy(b);
        }
        return I;
    }

    Transform Take(Mesh mesh, Material m)
    {
        Transform t;
        if (pool.Count > 0) { t = pool.Pop(); t.gameObject.SetActive(true); }
        else
        {
            var go = new GameObject("p");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            t = go.transform;
        }
        t.GetComponent<MeshFilter>().sharedMesh = mesh;
        t.GetComponent<MeshRenderer>().sharedMaterial = m;
        return t;
    }

    void Add(Mesh mesh, Material m, Vector3 pos, Vector3 vel, float size, float life, float grav, float drag, float grow)
    {
        if (live.Count > 700) return;
        Transform t = Take(mesh, m);
        t.position = pos;
        t.rotation = Random.rotation;
        t.localScale = Vector3.one * size;
        live.Add(new P { t = t, v = vel, life = life, max = life, size = size, grav = grav, drag = drag, grow = grow });
    }

    // Sparks / bits flying out.
    public static void Burst(Vector3 pos, Color c, int n, float speed, float size = 0.1f, float life = 0.6f, float grav = -12f)
    {
        Fx f = Get();
        Material m = Gfx.Clear(c);
        for (int i = 0; i < n; i++)
            f.Add(f.cube, m, pos, Random.onUnitSphere * speed * Random.Range(0.3f, 1f) + Vector3.up * speed * 0.3f,
                size * Random.Range(0.6f, 1.3f), life * Random.Range(0.6f, 1.2f), grav, 1.5f, 0f);
    }

    // Soft round puffs (smoke, slime, steam).
    public static void Puff(Vector3 pos, Color c, int n, float spread, float size, float life, Vector3 drift)
    {
        Fx f = Get();
        Material m = Gfx.Clear(c);
        for (int i = 0; i < n; i++)
            f.Add(f.ball, m, pos + Random.insideUnitSphere * spread, drift + Random.insideUnitSphere * spread,
                size * Random.Range(0.6f, 1.2f), life * Random.Range(0.7f, 1.2f), 0f, 1f, 1.2f);
    }

    // Short bright light.
    public static void Flash(Vector3 pos, Color c, float range, float intensity, float time)
    {
        Fx f = Get();
        Light l = null;
        for (int i = 0; i < f.lights.Count; i++)
            if (!f.lights[i].enabled) { l = f.lights[i]; f.lightEnd[i] = Time.time + time; f.lightMax[i] = intensity; break; }
        if (l == null)
        {
            if (f.lights.Count >= 10) return;
            l = Gfx.AddLight(f.transform, Vector3.zero, c, range, intensity);
            f.lights.Add(l); f.lightEnd.Add(Time.time + time); f.lightMax.Add(intensity);
        }
        l.transform.position = pos;
        l.color = c;
        l.range = range;
        l.intensity = intensity;
        l.enabled = true;
    }

    // A line between two points that fades (zaps, lasers, beams). Zig-zag for lightning.
    public static void Beam(Vector3 a, Vector3 b, Color c, float width, float time, bool zigzag)
    {
        Fx f = Get();
        var go = new GameObject("beam");
        go.transform.SetParent(f.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.material = Gfx.Clear(c);
        lr.useWorldSpace = true;
        lr.startWidth = width;
        lr.endWidth = width * 0.6f;
        lr.startColor = c;
        lr.endColor = c;
        lr.numCapVertices = 2;
        int n = zigzag ? Mathf.Clamp(Mathf.RoundToInt(Vector3.Distance(a, b) / 1.2f), 3, 30) : 2;
        lr.positionCount = n;
        for (int i = 0; i < n; i++)
        {
            float k = i / (float)(n - 1);
            Vector3 p = Vector3.Lerp(a, b, k);
            if (zigzag && i > 0 && i < n - 1) p += Random.insideUnitSphere * 0.25f;
            lr.SetPosition(i, p);
        }
        f.StartCoroutine(FadeLine(lr, time, width));
    }

    static System.Collections.IEnumerator FadeLine(LineRenderer lr, float time, float width)
    {
        float t = 0f;
        while (t < time && lr != null)
        {
            t += Time.deltaTime;
            float k = 1f - t / time;
            lr.startWidth = width * k;
            lr.endWidth = width * 0.6f * k;
            yield return null;
        }
        if (lr != null) Destroy(lr.gameObject);
    }

    // Ring of bits on the ground (landing, explosions).
    public static void Ring(Vector3 pos, Color c, int n, float speed, float size)
    {
        Fx f = Get();
        Material m = Gfx.Clear(c);
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            Vector3 d = new Vector3(Mathf.Cos(a), 0.15f, Mathf.Sin(a));
            f.Add(f.ball, m, pos + d * 0.3f, d * speed, size, 0.5f, 0f, 3f, 0.8f);
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        for (int i = live.Count - 1; i >= 0; i--)
        {
            P p = live[i];
            p.life -= dt;
            if (p.life <= 0f || p.t == null)
            {
                if (p.t != null) { p.t.gameObject.SetActive(false); pool.Push(p.t); }
                live.RemoveAt(i);
                continue;
            }
            p.v.y += p.grav * dt;
            p.v *= Mathf.Max(0f, 1f - p.drag * dt);
            p.t.position += p.v * dt;
            float k = p.life / p.max;
            float s = p.size * (p.grow > 0f ? (1f + (1f - k) * p.grow) * Mathf.Min(1f, k * 3f) : k);
            p.t.localScale = Vector3.one * s;
            live[i] = p;
        }
        for (int i = 0; i < lights.Count; i++)
        {
            if (!lights[i].enabled) continue;
            float left = lightEnd[i] - Time.time;
            if (left <= 0f) lights[i].enabled = false;
            else lights[i].intensity = lightMax[i] * Mathf.Clamp01(left * 8f);
        }
    }
}
