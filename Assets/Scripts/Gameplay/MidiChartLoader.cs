using UnityEngine;
using Melanchall.DryWetMidi.Core;
using System.IO;
using Melanchall.DryWetMidi.Interaction;
using System.Linq;

public class MidiChartLoader : MonoBehaviour
{
    [SerializeField] private TextAsset midiAsset;

    private void Start()
    {
        if (midiAsset == null)
        {
            Debug.LogError("MidiChartLoader requires a MIDI asset.", this);
            return;
        }

        LoadMidi();
    }

    private void LoadMidi()
    {
        using var stream = new MemoryStream(midiAsset.bytes);
        var midiFile = MidiFile.Read(stream);
        var notes = midiFile.GetNotes();
        var tempoMap = midiFile.GetTempoMap();

        if (notes.Count == 0)
        {
            Debug.LogWarning("MIDI file contains no notes.", this);
            return;
        }

        foreach (var note in notes)
        {
            var metricTime = note.TimeAs<MetricTimeSpan>(tempoMap);
            double hitTime = metricTime.TotalSeconds;
        }

    }
}