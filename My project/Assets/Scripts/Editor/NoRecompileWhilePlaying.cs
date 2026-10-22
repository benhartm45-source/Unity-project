using UnityEditor;

// Unity normally recompiles your scripts the moment you save them - even in the middle of playing.
// That breaks a running match (everything the game was keeping track of gets wiped) and causes
// NullReferenceExceptions. This switches Unity to "Recompile After Finished Playing" (the same as
// Edit > Preferences > General > Script Changes While Playing), so changes wait until you press Stop.
[InitializeOnLoad]
static class NoRecompileWhilePlaying
{
    const string Key = "ScriptCompilationDuringPlay";   // 0 = recompile and continue, 1 = after finished playing, 2 = stop playing

    static NoRecompileWhilePlaying()
    {
        if (EditorPrefs.GetInt(Key, 0) == 0) EditorPrefs.SetInt(Key, 1);
    }
}
