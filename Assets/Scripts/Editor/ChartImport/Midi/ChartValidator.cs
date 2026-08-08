using System.Collections.Generic;

public class ChartValidator
{
    public ChartValidationResult Validate(ChartBuildResult buildResult)
    {
        var result = new ChartValidationResult();
        if (buildResult == null)
        {
            result.Errors.Add("Build result is null.");
            return result;
        }

        SongChartData chart = buildResult.Chart;
        if (chart == null)
        {
            result.Errors.Add("Generated chart is null.");
            return result;
        }

        if (chart.notes == null)
        {
            result.Errors.Add("Generated chart notes list is null.");
            return result;
        }

        if (chart.notes.Count == 0)
        {
            result.Errors.Add("Generated chart is empty.");
        }

        ValidateRuntimeNotes(chart.notes, result);
        ValidateBuildContext(buildResult, result);

        if (buildResult.Statistics.NotesDroppedByLaneLimit > 0)
        {
            result.Warnings.Add(
                $"Dropped {buildResult.Statistics.NotesDroppedByLaneLimit} MIDI notes " +
                "from chords exceeding the four-lane limit.");
        }

        return result;
    }

    private static void ValidateRuntimeNotes(
        IReadOnlyList<NoteData> notes,
        ChartValidationResult result)
    {
        double previousHitTime = double.NegativeInfinity;
        var hitTimeLanePairs = new HashSet<(double HitTime, int Lane)>();

        for (int i = 0; i < notes.Count; i++)
        {
            NoteData note = notes[i];
            if (note == null)
            {
                result.Errors.Add($"Note at index {i} is null.");
                continue;
            }

            if (note.lane < 0 || note.lane >= ChartBuilder.LaneCount)
            {
                result.Errors.Add($"Note at index {i} has invalid lane {note.lane}.");
            }

            if (double.IsNaN(note.hitTime) || double.IsInfinity(note.hitTime))
            {
                result.Errors.Add($"Note at index {i} has a non-finite hit time.");
            }
            else
            {
                if (note.hitTime < 0d)
                {
                    result.Errors.Add($"Note at index {i} has negative hit time {note.hitTime}.");
                }

                if (note.hitTime < previousHitTime)
                {
                    result.Errors.Add($"Notes are not sorted at index {i}.");
                }

                previousHitTime = note.hitTime;
            }

            if (!hitTimeLanePairs.Add((note.hitTime, note.lane)))
            {
                result.Errors.Add(
                    $"Duplicate gameplay note at hitTime={note.hitTime}, lane={note.lane}.");
            }
        }
    }

    private static void ValidateBuildContext(
        ChartBuildResult buildResult,
        ChartValidationResult result)
    {
        if (buildResult.BuiltNotes.Count != buildResult.Chart.notes.Count)
        {
            result.Errors.Add("Build context count does not match generated chart note count.");
        }

        var tickLanePairs = new HashSet<(long Tick, int Lane)>();
        foreach (ChartBuiltNote note in buildResult.BuiltNotes)
        {
            if (!tickLanePairs.Add((note.Tick, note.Lane)))
            {
                result.Errors.Add(
                    $"Duplicate gameplay lane {note.Lane} at MIDI tick {note.Tick}.");
            }
        }
    }
}

public class ChartValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
}
