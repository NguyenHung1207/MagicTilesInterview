using UnityEngine;

public class GameplaySongLoader : MonoBehaviour
{
    [SerializeField] private SongDefinition defaultSong;
    [SerializeField] private SongConductor songConductor;
    [SerializeField] private JsonChartLoader chartLoader;
    [SerializeField] private DynamicBackgroundController backgroundController;

    private void Awake()
    {
        if (songConductor == null || chartLoader == null || backgroundController == null)
        {
            Debug.LogError(
                "GameplaySongLoader requires SongConductor, JsonChartLoader, and DynamicBackgroundController references.",
                this);
            enabled = false;
            return;
        }

        SongDefinition song = SelectedSongContext.SelectedSong != null
            ? SelectedSongContext.SelectedSong
            : defaultSong;

        if (song == null)
        {
            Debug.LogError("GameplaySongLoader requires a song.", this);

            enabled = false;
            return;
        }

        songConductor.SetSong(song.AudioClip);
        chartLoader.Load(song.ChartAsset);
        backgroundController.ApplyTheme(song.BackgroundTheme);
    }
}
