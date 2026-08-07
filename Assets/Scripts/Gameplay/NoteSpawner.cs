using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [SerializeField] private SongConductor songConductor;
    [SerializeField] private MidiChartLoader chartLoader;
    [SerializeField] private double travelTime = 2.0;

    [SerializeField] private GameObject notePrefab;
    [SerializeField] private Transform[] laneSpawnPoints;

    private int nextNoteIndex;

    private void Update()
    {
        double currentSongTime = songConductor.SongTime;

        while (nextNoteIndex < chartLoader.Notes.Count)
        {
            NoteData nextNote = chartLoader.Notes[nextNoteIndex];
            double spawnTime = nextNote.hitTime - travelTime;

            if (currentSongTime < spawnTime)
            {
                break;
            }

            Debug.Log($"Spawn note {nextNoteIndex} | Lane {nextNote.lane} | Hit {nextNote.hitTime:F3}s", this);
            nextNoteIndex++;
        }
    }
}