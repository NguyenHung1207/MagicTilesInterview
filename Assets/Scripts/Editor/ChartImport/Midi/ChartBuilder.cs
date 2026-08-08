using System;
using System.Collections.Generic;

public class ChartBuilder
{
    public const int LaneCount = 4;
    public const int PreferredMelodyTrackIndex = 1;
    private const int TransformationSampleCount = 3;

    public ChartBuildResult Build(IReadOnlyList<MidiExtractedNote> rawNotes)
    {
        if (rawNotes == null)
        {
            throw new ArgumentNullException(nameof(rawNotes));
        }

        var result = new ChartBuildResult();
        result.Statistics.RawNoteCount = rawNotes.Count;

        List<MidiExtractedNote> sortedRawNotes = CopyAndSort(rawNotes);
        List<MidiExtractedNote> uniqueNotes = RemoveExactDuplicates(sortedRawNotes);
        result.Statistics.ExactDuplicatesRemoved = sortedRawNotes.Count - uniqueNotes.Count;
        result.Statistics.UniqueMidiNoteCount = uniqueNotes.Count;

        SortedDictionary<long, List<MidiExtractedNote>> notesByTick = GroupByTick(uniqueNotes);
        result.Statistics.TickGroupCount = notesByTick.Count;

        var preparedGroups = new List<PreparedTickGroup>(notesByTick.Count);
        int minimumPitch = int.MaxValue;
        int maximumPitch = int.MinValue;

        foreach (KeyValuePair<long, List<MidiExtractedNote>> pair in notesByTick)
        {
            List<MidiExtractedNote> group = pair.Value;
            if (group.Count > 1)
            {
                result.Statistics.MultiPitchGroupCount++;
            }

            result.Statistics.LargestSourceGroupSize = Math.Max(
                result.Statistics.LargestSourceGroupSize,
                group.Count);

            MidiExtractedNote representative = SelectRepresentativeNote(group);
            if (representative.TrackIndex == PreferredMelodyTrackIndex)
            {
                result.Statistics.PreferredTrackSelectionCount++;
            }
            else
            {
                result.Statistics.FallbackSelectionCount++;
            }

            result.Statistics.RepresentativeNoteCount++;
            preparedGroups.Add(new PreparedTickGroup(pair.Key, representative));
            minimumPitch = Math.Min(minimumPitch, representative.NoteNumber);
            maximumPitch = Math.Max(maximumPitch, representative.NoteNumber);
        }

        BuildGameplayNotes(preparedGroups, minimumPitch, maximumPitch, result);
        BuildTransformationSamples(sortedRawNotes, notesByTick, preparedGroups, result);
        return result;
    }

    private static List<MidiExtractedNote> CopyAndSort(IReadOnlyList<MidiExtractedNote> rawNotes)
    {
        var result = new List<MidiExtractedNote>(rawNotes.Count);
        for (int i = 0; i < rawNotes.Count; i++)
        {
            MidiExtractedNote note = rawNotes[i];
            if (note == null)
            {
                throw new ArgumentException("Raw MIDI notes cannot contain null entries.", nameof(rawNotes));
            }

            result.Add(note);
        }

        result.Sort(CompareNotes);
        return result;
    }

    private static List<MidiExtractedNote> RemoveExactDuplicates(
        IReadOnlyList<MidiExtractedNote> sortedNotes)
    {
        var result = new List<MidiExtractedNote>(sortedNotes.Count);
        var seen = new HashSet<(long Tick, int NoteNumber)>();
        foreach (MidiExtractedNote note in sortedNotes)
        {
            if (seen.Add((note.Tick, note.NoteNumber)))
            {
                result.Add(note);
            }
        }

        return result;
    }

    private static SortedDictionary<long, List<MidiExtractedNote>> GroupByTick(
        IReadOnlyList<MidiExtractedNote> notes)
    {
        var result = new SortedDictionary<long, List<MidiExtractedNote>>();
        foreach (MidiExtractedNote note in notes)
        {
            if (!result.TryGetValue(note.Tick, out List<MidiExtractedNote> group))
            {
                group = new List<MidiExtractedNote>();
                result.Add(note.Tick, group);
            }

            group.Add(note);
        }

        return result;
    }

    private static MidiExtractedNote SelectRepresentativeNote(
        IReadOnlyList<MidiExtractedNote> sortedUniqueNotes)
    {
        MidiExtractedNote preferredTrackNote = null;
        foreach (MidiExtractedNote note in sortedUniqueNotes)
        {
            if (note.TrackIndex == PreferredMelodyTrackIndex)
            {
                // Keep the highest note if a future source makes the preferred track polyphonic.
                preferredTrackNote = note;
            }
        }

        if (preferredTrackNote != null)
        {
            return preferredTrackNote;
        }

        // The top voice is the simplest deterministic melody proxy when Track 1 is silent.
        return sortedUniqueNotes[sortedUniqueNotes.Count - 1];
    }

    private static void BuildGameplayNotes(
        IReadOnlyList<PreparedTickGroup> groups,
        int minimumPitch,
        int maximumPitch,
        ChartBuildResult result)
    {
        foreach (PreparedTickGroup group in groups)
        {
            MidiExtractedNote sourceNote = group.Representative;
            int lane = MapPitchToLane(sourceNote.NoteNumber, minimumPitch, maximumPitch);

            result.Chart.notes.Add(new NoteData
            {
                lane = lane,
                hitTime = sourceNote.TimeSeconds
            });

            result.BuiltNotes.Add(new ChartBuiltNote
            {
                Tick = group.Tick,
                NoteNumber = sourceNote.NoteNumber,
                TrackIndex = sourceNote.TrackIndex,
                Lane = lane,
                HitTime = sourceNote.TimeSeconds
            });

            result.Statistics.LaneCounts[lane]++;
        }

        result.Statistics.GameplayNoteCount = result.Chart.notes.Count;
        if (result.Chart.notes.Count > 0)
        {
            result.Statistics.FirstHitTime = result.Chart.notes[0].hitTime;
            result.Statistics.LastHitTime = result.Chart.notes[result.Chart.notes.Count - 1].hitTime;
        }
    }

    private static int MapPitchToLane(int pitch, int minimumPitch, int maximumPitch)
    {
        if (minimumPitch == maximumPitch)
        {
            return 0;
        }

        double normalizedPitch = (pitch - minimumPitch) / (double)(maximumPitch - minimumPitch);
        return (int)Math.Round(
            normalizedPitch * (LaneCount - 1),
            MidpointRounding.AwayFromZero);
    }

    private static void BuildTransformationSamples(
        IReadOnlyList<MidiExtractedNote> sortedRawNotes,
        IReadOnlyDictionary<long, List<MidiExtractedNote>> uniqueNotesByTick,
        IReadOnlyList<PreparedTickGroup> preparedGroups,
        ChartBuildResult result)
    {
        var rawPitchesByTick = new SortedDictionary<long, List<int>>();
        foreach (MidiExtractedNote note in sortedRawNotes)
        {
            if (!rawPitchesByTick.TryGetValue(note.Tick, out List<int> pitches))
            {
                pitches = new List<int>();
                rawPitchesByTick.Add(note.Tick, pitches);
            }

            pitches.Add(note.NoteNumber);
        }

        int sampleCount = Math.Min(TransformationSampleCount, preparedGroups.Count);
        for (int i = 0; i < sampleCount; i++)
        {
            PreparedTickGroup group = preparedGroups[i];
            var sample = new ChartTransformationSample { Tick = group.Tick };
            sample.RawPitches.AddRange(rawPitchesByTick[group.Tick]);

            foreach (MidiExtractedNote note in uniqueNotesByTick[group.Tick])
            {
                sample.UniquePitches.Add(note.NoteNumber);
            }

            sample.RepresentativePitch = group.Representative.NoteNumber;
            sample.RepresentativeTrackIndex = group.Representative.TrackIndex;
            sample.OutputNote = result.BuiltNotes[i];
            result.TransformationSamples.Add(sample);
        }
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

    private class PreparedTickGroup
    {
        public readonly long Tick;
        public readonly MidiExtractedNote Representative;

        public PreparedTickGroup(long tick, MidiExtractedNote representative)
        {
            Tick = tick;
            Representative = representative;
        }
    }
}

public class ChartBuildResult
{
    public SongChartData Chart { get; } = new();
    public ChartBuildStatistics Statistics { get; } = new();
    public List<ChartBuiltNote> BuiltNotes { get; } = new();
    public List<ChartTransformationSample> TransformationSamples { get; } = new();
}

public class ChartBuildStatistics
{
    public int RawNoteCount;
    public int ExactDuplicatesRemoved;
    public int UniqueMidiNoteCount;
    public int TickGroupCount;
    public int MultiPitchGroupCount;
    public int LargestSourceGroupSize;
    public int RepresentativeNoteCount;
    public int PreferredTrackSelectionCount;
    public int FallbackSelectionCount;
    public int GameplayNoteCount;
    public int[] LaneCounts = new int[ChartBuilder.LaneCount];
    public double FirstHitTime;
    public double LastHitTime;
}

public class ChartBuiltNote
{
    public long Tick;
    public int NoteNumber;
    public int TrackIndex;
    public int Lane;
    public double HitTime;
}

public class ChartTransformationSample
{
    public long Tick;
    public List<int> RawPitches { get; } = new();
    public List<int> UniquePitches { get; } = new();
    public int RepresentativePitch;
    public int RepresentativeTrackIndex;
    public ChartBuiltNote OutputNote;
}
