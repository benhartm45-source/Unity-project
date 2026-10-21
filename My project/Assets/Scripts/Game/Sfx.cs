using System.Collections.Generic;
using UnityEngine;

// Plays the game's sound effects (the .wav files in Assets/Resources/Audio).
public static class Sfx
{
    static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    static readonly List<AudioSource> pool = new List<AudioSource>();
    static AudioSource ui;
    static GameObject host;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { clips.Clear(); pool.Clear(); ui = null; host = null; }

    public static AudioClip Clip(string name)
    {
        if (!clips.TryGetValue(name, out AudioClip c))
        {
            c = Resources.Load<AudioClip>("Audio/" + name);
            clips[name] = c;
        }
        return c;
    }

    static void Ensure()
    {
        if (host != null) return;
        host = new GameObject("Sounds");
        Object.DontDestroyOnLoad(host);
        ui = host.AddComponent<AudioSource>();
        ui.playOnAwake = false;
        ui.spatialBlend = 0f;
        pool.Clear();
    }

    // Plain (not 3D) sound: UI, your own gun, your footsteps.
    public static void Play(string name, float vol = 1f, float pitch = 1f)
    {
        AudioClip c = Clip(name);
        if (c == null) return;
        Ensure();
        if (Mathf.Abs(pitch - 1f) < 0.01f) { ui.PlayOneShot(c, vol * Settings.Volume); return; }
        AudioSource s = Free();
        s.transform.position = Vector3.zero;
        s.spatialBlend = 0f;
        s.pitch = pitch;
        s.clip = c;
        s.volume = vol * Settings.Volume;
        s.Play();
    }

    // Sound that comes from somewhere in the world.
    public static void At(string name, Vector3 pos, float vol = 1f, float pitch = 1f, float maxDist = 45f)
    {
        AudioClip c = Clip(name);
        if (c == null) return;
        Ensure();
        AudioSource s = Free();
        s.transform.position = pos;
        s.spatialBlend = 1f;
        s.minDistance = 2f;
        s.maxDistance = maxDist;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.pitch = pitch * Random.Range(0.95f, 1.05f);
        s.clip = c;
        s.volume = vol * Settings.Volume;
        s.Play();
    }

    static AudioSource Free()
    {
        foreach (AudioSource s in pool) if (s != null && !s.isPlaying) return s;
        var go = new GameObject("sfx");
        go.transform.SetParent(host.transform, false);
        var a = go.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.dopplerLevel = 0f;
        if (pool.Count < 40) pool.Add(a);
        return a;
    }

    // A looping sound stuck to an object (cauldron bubbling, beams, engines).
    public static AudioSource Loop(string name, Transform parent, float vol, float maxDist = 20f)
    {
        AudioClip c = Clip(name);
        var go = new GameObject("loop " + name);
        go.transform.SetParent(parent, false);
        var a = go.AddComponent<AudioSource>();
        a.clip = c;
        a.loop = true;
        a.spatialBlend = 1f;
        a.minDistance = 1.5f;
        a.maxDistance = maxDist;
        a.rolloffMode = AudioRolloffMode.Linear;
        a.dopplerLevel = 0f;
        a.volume = vol * Settings.Volume;
        a.playOnAwake = false;
        if (c != null) a.Play();
        return a;
    }
}
