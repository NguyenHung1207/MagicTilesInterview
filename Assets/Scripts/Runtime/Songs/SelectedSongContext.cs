public static class SelectedSongContext
{
    public static SongDefinition SelectedSong { get; private set; }

    public static void Select(SongDefinition song)
    {
        SelectedSong = song;
    }

    public static void Clear()
    {
        SelectedSong = null;
    }
}