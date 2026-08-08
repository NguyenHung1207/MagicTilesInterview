using System.Collections.Generic;
using UnityEngine;

public class JsonChartLoader : MonoBehaviour
{
    [SerializeField] private TextAsset chartAsset;
    private readonly List<NoteData> notes = new();
    public IReadOnlyList<NoteData> Notes => notes;

    private void Awake()
    {
        if (chartAsset == null)
        {
            Debug.LogError("JsonChartLoader requires a chart asset.", this);
            return;
        }

        SongChartData chartData = JsonUtility.FromJson<SongChartData>(chartAsset.text);

        if (chartData == null || chartData.notes == null)
        {
            Debug.LogError("Failed to load chart data.", this);
            return;
        }

        notes.Clear();
        notes.AddRange(chartData.notes);
        Debug.Log($"Loaded {notes.Count} gameplay notes from JSON.", this);
    }
}