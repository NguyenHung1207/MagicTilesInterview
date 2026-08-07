using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [SerializeField] private SongConductor songConductor;
    [SerializeField] private MidiChartLoader chartLoader;
    [SerializeField] private double travelTime = 2.0;

    [SerializeField] private Note notePrefab;
    [SerializeField] private LaneView[] lanes;

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

            LaneView lane = lanes[nextNote.lane];
            Note spawnedNote = Instantiate(notePrefab, lane.SpawnPoint.position, Quaternion.identity);
            spawnedNote.Initialize(songConductor, lane.SpawnPoint.position, lane.HitPoint.position, spawnTime, nextNote.hitTime);
            
            Debug.Log($"Spawn note {nextNoteIndex} | Lane {nextNote.lane} | Hit {nextNote.hitTime:F3}s", this);
            nextNoteIndex++;
        }
    }
}