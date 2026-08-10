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
        }

        BuildGameplayNotes(preparedGroups, result);
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
        ChartBuildResult result)
    {
        int[] pitchThresholds = DerivePitchThresholds(groups);
        int[] laneCounts = new int[LaneCount];
        int previousLane = -1;
        int previousPitch = 0;
        int consecutiveLaneRun = 0;

        foreach (PreparedTickGroup group in groups)
        {
            MidiExtractedNote sourceNote = group.Representative;
            int baseLane = MapPitchToLane(sourceNote.NoteNumber, pitchThresholds);
            int lane = AdjustForPlayability(
                baseLane,
                sourceNote.NoteNumber,
                previousLane,
                previousPitch,
                consecutiveLaneRun,
                laneCounts,
                result.Chart.notes.Count);

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

            laneCounts[lane]++;
            if (lane == previousLane)
            {
                consecutiveLaneRun++;
            }
            else
            {
                consecutiveLaneRun = 1;
            }

            previousLane = lane;
            previousPitch = sourceNote.NoteNumber;
        }

        result.Statistics.LaneCounts = laneCounts;
        result.Statistics.GameplayNoteCount = result.Chart.notes.Count;
        if (result.Chart.notes.Count > 0)
        {
            result.Statistics.FirstHitTime = result.Chart.notes[0].hitTime;
            result.Statistics.LastHitTime = result.Chart.notes[result.Chart.notes.Count - 1].hitTime;
            result.Statistics.MinimumHitInterval = FindMinimumHitInterval(result.Chart.notes);
            result.Statistics.LongestConsecutiveLaneRun = FindLongestLaneRun(result.Chart.notes);
        }
    }

    private static int[] DerivePitchThresholds(IReadOnlyList<PreparedTickGroup> groups)
    {
        var pitches = new List<int>(groups.Count);
        foreach (PreparedTickGroup group in groups)
        {
            pitches.Add(group.Representative.NoteNumber);
        }

        pitches.Sort();
        return new[]
        {
            SelectQuantile(pitches, 0.25),
            SelectQuantile(pitches, 0.50),
            SelectQuantile(pitches, 0.75)
        };
    }

    private static int SelectQuantile(IReadOnlyList<int> sortedValues, double quantile)
    {
        int index = (int)Math.Floor((sortedValues.Count - 1) * quantile);
        return sortedValues[index];
    }

    private static int MapPitchToLane(int pitch, IReadOnlyList<int> thresholds)
    {
        if (pitch <= thresholds[0])
        {
            return 0;
        }

        if (pitch <= thresholds[1])
        {
            return 1;
        }

        if (pitch <= thresholds[2])
        {
            return 2;
        }

        return 3;
    }

    private static int AdjustForPlayability(
        int baseLane,
        int pitch,
        int previousLane,
        int previousPitch,
        int consecutiveLaneRun,
        IReadOnlyList<int> laneCounts,
        int assignedCount)
    {
        if (assignedCount == 0)
        {
            return baseLane;
        }

        int minimumCount = int.MaxValue;
        for (int lane = 0; lane < LaneCount; lane++)
        {
            minimumCount = Math.Min(minimumCount, laneCounts[lane]);
        }

        bool baseOverrepresented = laneCounts[baseLane] > minimumCount + 1;
        bool baseNeedsBreak = consecutiveLaneRun >= 2;
        bool missingLaneNeedsHelp = assignedCount >= LaneCount
            && laneCounts[baseLane] > minimumCount
            && HasEmptyLane(laneCounts);
        if (!baseOverrepresented && !baseNeedsBreak && !missingLaneNeedsHelp)
        {
            return baseLane;
        }

        int pitchDirection = Math.Sign(pitch - previousPitch);
        int bestLane = baseLane;
        int bestScore = int.MaxValue;
        for (int candidate = 0; candidate < LaneCount; candidate++)
        {
            int score = Math.Abs(candidate - baseLane) * 2;
            if (candidate == previousLane)
            {
                score += consecutiveLaneRun >= 2 ? 12 : 3;
            }

            if (laneCounts[candidate] > minimumCount)
            {
                score += laneCounts[candidate] - minimumCount;
            }
            else
            {
                score -= 3;
            }

            if (pitchDirection > 0 && candidate < previousLane)
            {
                score += 2;
            }
            else if (pitchDirection < 0 && candidate > previousLane)
            {
                score += 2;
            }

            if (previousLane >= 0 && Math.Abs(candidate - previousLane) > 1
                && Math.Abs(pitch - previousPitch) <= 2)
            {
                score += 4;
            }

            if (score < bestScore)
            {
                bestScore = score;
                bestLane = candidate;
            }
        }

        return bestLane;
    }

    private static bool HasEmptyLane(IReadOnlyList<int> laneCounts)
    {
        for (int lane = 0; lane < LaneCount; lane++)
        {
            if (laneCounts[lane] == 0)
            {
                return true;
            }
        }

        return false;
    }

    private static double FindMinimumHitInterval(IReadOnlyList<NoteData> notes)
    {
        if (notes.Count < 2)
        {
            return 0d;
        }

        double minimum = double.MaxValue;
        for (int index = 1; index < notes.Count; index++)
        {
            minimum = Math.Min(minimum, notes[index].hitTime - notes[index - 1].hitTime);
        }

        return minimum;
    }

    private static int FindLongestLaneRun(IReadOnlyList<NoteData> notes)
    {
        int longest = 0;
        int current = 0;
        int previousLane = -1;
        foreach (NoteData note in notes)
        {
            current = note.lane == previousLane ? current + 1 : 1;
            longest = Math.Max(longest, current);
            previousLane = note.lane;
        }

        return longest;
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
    public double MinimumHitInterval;
    public int LongestConsecutiveLaneRun;
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
