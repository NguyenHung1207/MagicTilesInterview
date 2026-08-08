using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [SerializeField] private SongConductor songConductor;
    [SerializeField] private JsonChartLoader chartLoader;
    [SerializeField] private double travelTime = 2.0;

    [SerializeField] private Note notePrefab;
    [SerializeField] private LaneView[] lanes;
    public int TotalNoteCount => chartLoader.Notes.Count;

    private int nextNoteIndex;

    private void Awake()
    {
        if (songConductor == null || chartLoader == null || notePrefab == null)
        {
            Debug.LogError(
                "NoteSpawner requires SongConductor, JsonChartLoader, and Note prefab references.",
                this);
            enabled = false;
            return;
        }

        if (travelTime <= 0.0)
        {
            Debug.LogError("NoteSpawner travelTime must be greater than zero.", this);
            enabled = false;
            return;
        }

        if (lanes == null || lanes.Length != 4)
        {
            Debug.LogError("NoteSpawner requires exactly four LaneView references.", this);
            enabled = false;
            return;
        }

        foreach (LaneView lane in lanes)
        {
            if (lane != null)
            {
                continue;
            }

            Debug.LogError("NoteSpawner lane references cannot contain null entries.", this);
            enabled = false;
            return;
        }
    }

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
            spawnedNote.Initialize(songConductor, lane.SpawnPoint.position, lane.HitPoint.position, spawnTime, nextNote.hitTime, nextNote.lane);
            
            Debug.Log($"Spawn note {nextNoteIndex} | Lane {nextNote.lane} | Hit {nextNote.hitTime:F3}s", this);
            nextNoteIndex++;
        }
    }
}
