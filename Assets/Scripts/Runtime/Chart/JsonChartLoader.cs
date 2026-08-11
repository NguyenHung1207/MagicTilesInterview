using System.Collections.Generic;
using UnityEngine;

public class JsonChartLoader : MonoBehaviour
{
    private readonly List<NoteData> notes = new();
    public IReadOnlyList<NoteData> Notes => notes;
    public bool IsReady { get; private set; }
    public bool HasFailed { get; private set; }

    public void Load(TextAsset asset)
    {
        IsReady = false;
        HasFailed = false;
        notes.Clear();

        if (asset == null)
        {
            HasFailed = true;
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
            HasFailed = true;
            Debug.LogError($"Failed to parse chart JSON: {exception.Message}", this);
            return;
        }

        if (chartData == null ||
            chartData.notes == null ||
            chartData.notes.Count == 0)
        {
            HasFailed = true;
            Debug.LogError("Chart contains no gameplay notes.", this);
            return;
        }

        notes.AddRange(chartData.notes);
        IsReady = true;
    }
}
