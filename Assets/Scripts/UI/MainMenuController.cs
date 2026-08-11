using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private SongCatalog songCatalog;
    [SerializeField] private SongCardView songCardPrefab;
    [SerializeField] private Transform songListContent;
    [SerializeField] private string gameplaySceneName = "Gameplay";

    private void Start()
    {
        if (songCatalog == null || songCardPrefab == null || songListContent == null)
        {
            Debug.LogError("MainMenuController requires a SongCatalog, SongCard prefab, and song list Content.", this);
            enabled = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            Debug.LogError("MainMenuController requires a gameplay scene name.", this);
            enabled = false;
            return;
        }

        ClearSongCards();

        foreach (SongDefinition song in songCatalog.Songs)
        {
            if (song == null)
            {
                Debug.LogWarning("SongCatalog contains a null song entry; it was skipped.", this);
                continue;
            }

            SongCardView card = Instantiate(songCardPrefab, songListContent, false);
            card.Bind(song, SelectSong);
        }
    }

    private void ClearSongCards()
    {
        for (int index = songListContent.childCount - 1; index >= 0; index--)
        {
            Destroy(songListContent.GetChild(index).gameObject);
        }
    }

    private void SelectSong(SongDefinition song)
    {
        SfxController.PlayUIClick();
        SelectedSongContext.Select(song);
        SceneManager.LoadScene(gameplaySceneName);
    }
}
