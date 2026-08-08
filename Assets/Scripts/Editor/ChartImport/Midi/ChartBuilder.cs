using System;
using System.Collections.Generic;

public class ChartBuilder
{
    public const int LaneCount = 4;
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
            List<MidiExtractedNote> uniqueGroup = pair.Value;
            if (uniqueGroup.Count > 1)
            {
                result.Statistics.ChordGroupCount++;
            }

            result.Statistics.LargestChordSize = Math.Max(
                result.Statistics.LargestChordSize,
                uniqueGroup.Count);

            if (uniqueGroup.Count > LaneCount)
            {
                result.Statistics.GroupsExceedingLaneCount++;
            }

            List<MidiExtractedNote> selectedNotes = SelectRepresentativeNotes(uniqueGroup);
            result.Statistics.NotesDroppedByLaneLimit += uniqueGroup.Count - selectedNotes.Count;
            preparedGroups.Add(new PreparedTickGroup(pair.Key, selectedNotes));

            foreach (MidiExtractedNote note in selectedNotes)
            {
                minimumPitch = Math.Min(minimumPitch, note.NoteNumber);
                maximumPitch = Math.Max(maximumPitch, note.NoteNumber);
            }
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
            var key = (note.Tick, note.NoteNumber);
            if (seen.Add(key))
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

    private static List<MidiExtractedNote> SelectRepresentativeNotes(
        IReadOnlyList<MidiExtractedNote> sortedUniqueNotes)
    {
        if (sortedUniqueNotes.Count <= LaneCount)
        {
            return new List<MidiExtractedNote>(sortedUniqueNotes);
        }

        var result = new List<MidiExtractedNote>(LaneCount);
        for (int i = 0; i < LaneCount; i++)
        {
            double normalizedIndex = i * (sortedUniqueNotes.Count - 1d) / (LaneCount - 1d);
            int sourceIndex = (int)Math.Round(normalizedIndex, MidpointRounding.AwayFromZero);
            result.Add(sortedUniqueNotes[sourceIndex]);
        }

        return result;
    }

    private static void BuildGameplayNotes(
        IReadOnlyList<PreparedTickGroup> groups,
        int minimumPitch,
        int maximumPitch,
        ChartBuildResult result)
    {
        foreach (PreparedTickGroup group in groups)
        {
            for (int i = 0; i < group.Notes.Count; i++)
            {
                MidiExtractedNote sourceNote = group.Notes[i];
                int lane = group.Notes.Count == 1
                    ? MapSinglePitchToLane(sourceNote.NoteNumber, minimumPitch, maximumPitch)
                    : MapChordIndexToLane(i, group.Notes.Count);

                result.Chart.notes.Add(new NoteData
                {
                    lane = lane,
                    hitTime = sourceNote.TimeSeconds
                });

                result.BuiltNotes.Add(new ChartBuiltNote
                {
                    Tick = group.Tick,
                    NoteNumber = sourceNote.NoteNumber,
                    Lane = lane,
                    HitTime = sourceNote.TimeSeconds
                });

                result.Statistics.LaneCounts[lane]++;
            }
        }

        result.Statistics.GameplayNoteCount = result.Chart.notes.Count;
        if (result.Chart.notes.Count > 0)
        {
            result.Statistics.FirstHitTime = result.Chart.notes[0].hitTime;
            result.Statistics.LastHitTime = result.Chart.notes[result.Chart.notes.Count - 1].hitTime;
        }
    }

    private static int MapSinglePitchToLane(int pitch, int minimumPitch, int maximumPitch)
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

    private static int MapChordIndexToLane(int index, int chordSize)
    {
        double normalizedIndex = index / (double)(chordSize - 1);
        return (int)Math.Round(
            normalizedIndex * (LaneCount - 1),
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

            foreach (MidiExtractedNote note in group.Notes)
            {
                sample.SelectedPitches.Add(note.NoteNumber);
            }

            foreach (ChartBuiltNote builtNote in result.BuiltNotes)
            {
                if (builtNote.Tick == group.Tick)
                {
                    sample.OutputNotes.Add(builtNote);
                }
            }

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
        public readonly List<MidiExtractedNote> Notes;

        public PreparedTickGroup(long tick, List<MidiExtractedNote> notes)
        {
            Tick = tick;
            Notes = notes;
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
    public int ChordGroupCount;
    public int LargestChordSize;
    public int GroupsExceedingLaneCount;
    public int NotesDroppedByLaneLimit;
    public int GameplayNoteCount;
    public int[] LaneCounts = new int[ChartBuilder.LaneCount];
    public double FirstHitTime;
    public double LastHitTime;
}

public class ChartBuiltNote
{
    public long Tick;
    public int NoteNumber;
    public int Lane;
    public double HitTime;
}

public class ChartTransformationSample
{
    public long Tick;
    public List<int> RawPitches { get; } = new();
    public List<int> UniquePitches { get; } = new();
    public List<int> SelectedPitches { get; } = new();
    public List<ChartBuiltNote> OutputNotes { get; } = new();
}
