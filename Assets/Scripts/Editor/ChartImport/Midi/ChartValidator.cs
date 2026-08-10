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
        ValidateLaneBalance(chart.notes, result);

        return result;
    }

    private static void ValidateRuntimeNotes(
        IReadOnlyList<NoteData> notes,
        ChartValidationResult result)
    {
        double previousHitTime = double.NegativeInfinity;
        var hitTimes = new HashSet<double>();

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

            if (!hitTimes.Add(note.hitTime))
            {
                result.Errors.Add(
                    $"More than one gameplay note exists at hitTime={note.hitTime}.");
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

        if (buildResult.Statistics.RepresentativeNoteCount !=
            buildResult.Statistics.TickGroupCount)
        {
            result.Errors.Add("Representative note count does not match unique Tick group count.");
        }

        if (buildResult.Chart.notes.Count != buildResult.Statistics.TickGroupCount)
        {
            result.Errors.Add("Gameplay note count does not match unique Tick group count.");
        }

        var ticks = new HashSet<long>();
        foreach (ChartBuiltNote note in buildResult.BuiltNotes)
        {
            if (!ticks.Add(note.Tick))
            {
                result.Errors.Add(
                    $"More than one gameplay note exists at MIDI Tick {note.Tick}.");
            }
        }
    }

    private static void ValidateLaneBalance(
        IReadOnlyList<NoteData> notes,
        ChartValidationResult result)
    {
        if (notes.Count < 20)
        {
            return;
        }

        int[] laneCounts = new int[ChartBuilder.LaneCount];
        foreach (NoteData note in notes)
        {
            if (note != null && note.lane >= 0 && note.lane < ChartBuilder.LaneCount)
            {
                laneCounts[note.lane]++;
            }
        }

        for (int lane = 0; lane < laneCounts.Length; lane++)
        {
            double percentage = laneCounts[lane] * 100d / notes.Count;
            if (percentage < 15d || percentage > 40d)
            {
                result.Warnings.Add(
                    $"Lane {lane} represents {percentage:F1}% of gameplay notes; " +
                    "review playability for this chart.");
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
