namespace TileQuest
{
    // Day: explore, gather and craft. Night: the hearth is lit and a queued
    // wave of goblins marches on it from the village edge.
    public enum GameState
    {
        Day,
        Night,
    }

    // Which full-screen layer the game is on. Kept separate from GameState
    // (Day/Night) so the in-game day/night checks never see the menus.
    // Title, Pause, HowToPlay, and Ending draw over the dimmed world; Update
    // only handles menu input there, so nothing in the world moves behind them.
    public enum GameScreen
    {
        Title,
        HowToPlay,
        Pause,
        Playing,
        Ending,
    }

    // Which ending the Ending screen shows. Victory: all five nights survived.
    // Defeat: the hearth was destroyed or the player fell during a night.
    public enum EndingKind
    {
        Victory,
        Defeat,
    }
}