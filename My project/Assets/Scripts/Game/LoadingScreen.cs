// The old in-game loading screen has been removed (the game now uses Unity's startup splash screen
// with the Mad Lab logo instead - see Tools > Mad Lab > Build Game, BuildGameMenu.cs).
// This tiny class is kept so the places that start a match still work: it just runs the work straight away.
public static class LoadingScreen
{
    public static bool Active => false;

    public static void Run(string title, System.Action work, float minSeconds = 0f)
    {
        work?.Invoke();
    }
}
