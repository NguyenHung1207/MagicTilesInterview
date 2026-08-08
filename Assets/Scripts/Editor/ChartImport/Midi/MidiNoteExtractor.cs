using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

public class MidiNoteExtractor
{
    public List<MidiExtractedNote> Extract(MidiFile midiFile)
    {
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
                    double timeSeconds = metricTime.TotalSeconds;
                    var extractedNote = new MidiExtractedNote();
                    extractedNote.Tick = note.Time;
                    extractedNote.TimeSeconds = timeSeconds;
                    extractedNote.NoteNumber = (int)note.NoteNumber;
                    extractedNote.TrackIndex = trackIndex;
                    result.Add(extractedNote);
                }

            trackIndex++;
        }

        result.Sort((a, b) => a.Tick.CompareTo(b.Tick));
        return result;
    }
}