using UnityEngine;
using Melanchall.DryWetMidi.Core;
using System.IO;
using Melanchall.DryWetMidi.Interaction;
using System.Collections.Generic;

public class MidiChartLoader : MonoBehaviour
{
    [SerializeField] private TextAsset midiAsset;
    private readonly List<NoteData> noteDataList = new();
    public IReadOnlyList<NoteData> Notes => noteDataList;

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
        noteDataList.Clear();
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
            int noteNumber = (int)note.NoteNumber;
            int lane = GetLaneFromNoteNumber(noteNumber);

            var noteData = new NoteData();
            noteData.lane = lane;
            noteData.hitTime = hitTime;

            noteDataList.Add(noteData);
        }
        Debug.Log($"Created {noteDataList.Count} gameplay notes.", this);
    }

    private int GetLaneFromNoteNumber(int noteNumber)
    {
        return noteNumber % 4;
    }
}