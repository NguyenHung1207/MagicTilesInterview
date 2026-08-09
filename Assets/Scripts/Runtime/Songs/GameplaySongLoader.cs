using UnityEngine;

public class GameplaySongLoader : MonoBehaviour
{
    [SerializeField] private SongDefinition defaultSong;
    [SerializeField] private SongConductor songConductor;
    [SerializeField] private JsonChartLoader chartLoader;

    private void Awake()
    {
        if (songConductor == null || chartLoader == null)
        {
            Debug.LogError(
                "GameplaySongLoader requires SongConductor and JsonChartLoader references.",
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
    }
}
