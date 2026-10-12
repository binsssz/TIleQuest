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
    // Title, Pause, and HowToPlay draw over the dimmed village; Update only
    // handles menu input there, so nothing in the world moves behind them.
    public enum GameScreen
    {
        Title,
        HowToPlay,
        Pause,
        Playing,
    }
}
