using System.Collections.Generic;
using UnityEngine;

public class JsonChartLoader : MonoBehaviour
{
    private readonly List<NoteData> notes = new();
    public IReadOnlyList<NoteData> Notes => notes;

    public void Load(TextAsset asset)
    {
        if (asset == null)
        {
            Debug.LogError("JsonChartLoader requires a chart asset.", this);
            return;
        }

        SongChartData chartData;
        try
        {
            chartData = JsonUtility.FromJson<SongChartData>(asset.text);
        }
        catch (System.ArgumentException exception)
        {
            Debug.LogError($"Failed to parse chart JSON: {exception.Message}", this);
            return;
        }

        if (chartData == null ||
            chartData.notes == null ||
            chartData.notes.Count == 0)
        {
            Debug.LogError("Chart contains no gameplay notes.", this);
            return;
        }

        notes.Clear();
        notes.AddRange(chartData.notes);
    }
}
