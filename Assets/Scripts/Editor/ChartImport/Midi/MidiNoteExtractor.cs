using System.Collections.Generic;
using System;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

public class MidiNoteExtractor
{
    public List<MidiExtractedNote> Extract(MidiFile midiFile)
    {
        if (midiFile == null)
        {
            throw new ArgumentNullException(nameof(midiFile));
        }

        var result = new List<MidiExtractedNote>();
        var tempoMap = midiFile.GetTempoMap();
        var tracks = midiFile.GetTrackChunks();
        int trackIndex = 0;
        foreach (var track in tracks)
        {
            var notes = track.GetNotes();
            foreach (var note in notes)
            {
                var metricTime = note.TimeAs<MetricTimeSpan>(tempoMap);
                var extractedNote = new MidiExtractedNote();
                extractedNote.Tick = note.Time;
                extractedNote.TimeSeconds = metricTime.TotalSeconds;
                extractedNote.NoteNumber = (int)note.NoteNumber;
                extractedNote.TrackIndex = trackIndex;
                result.Add(extractedNote);
            }

            trackIndex++;
        }

        result.Sort(CompareNotes);
        return result;
    }

    private static int CompareNotes(MidiExtractedNote a, MidiExtractedNote b)
    {
        int tickComparison = a.Tick.CompareTo(b.Tick);
        if (tickComparison != 0)
        {
            return tickComparison;
        }

        int noteComparison = a.NoteNumber.CompareTo(b.NoteNumber);
        if (noteComparison != 0)
        {
            return noteComparison;
        }

        return a.TrackIndex.CompareTo(b.TrackIndex);
    }
}
