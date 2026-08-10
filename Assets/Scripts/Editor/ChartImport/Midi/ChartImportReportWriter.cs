using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class ChartImportReportWriter
{
    public static string Create(
        string sourcePath,
        int trackCount,
        ChartBuildResult buildResult,
        ChartValidationResult validationResult)
    {
        ChartBuildStatistics statistics = buildResult.Statistics;
        var report = new StringBuilder();

        report.AppendLine("MAGIC TILES MIDI CHART IMPORT REPORT");
        report.AppendLine("====================================");
        report.AppendLine();

        report.AppendLine("SOURCE");
        report.AppendLine($"MIDI path: {sourcePath}");
        report.AppendLine();

        report.AppendLine("RAW DATA");
        report.AppendLine($"Total track count: {trackCount}");
        report.AppendLine($"Total raw note count: {statistics.RawNoteCount}");
        report.AppendLine();

        report.AppendLine("NORMALIZATION");
        report.AppendLine($"Exact duplicates removed: {statistics.ExactDuplicatesRemoved}");
        report.AppendLine($"Unique MIDI note count: {statistics.UniqueMidiNoteCount}");
        report.AppendLine();

        report.AppendLine("SOURCE GROUP ANALYSIS");
        report.AppendLine($"Unique Tick groups: {statistics.TickGroupCount}");
        report.AppendLine($"Multi-pitch source groups: {statistics.MultiPitchGroupCount}");
        report.AppendLine($"Largest source group size: {statistics.LargestSourceGroupSize}");
        report.AppendLine();

        report.AppendLine("REPRESENTATIVE SELECTION");
        report.AppendLine(
            $"Policy: prefer Track {ChartBuilder.PreferredMelodyTrackIndex}; " +
            "otherwise select the highest pitch.");
        report.AppendLine($"Representative notes selected: {statistics.RepresentativeNoteCount}");
        report.AppendLine(
            $"Preferred Track {ChartBuilder.PreferredMelodyTrackIndex} selections: " +
            statistics.PreferredTrackSelectionCount);
        report.AppendLine($"Highest-pitch fallback selections: {statistics.FallbackSelectionCount}");
        report.AppendLine();

        report.AppendLine("OUTPUT");
        report.AppendLine($"Total gameplay notes: {statistics.GameplayNoteCount}");
        for (int lane = 0; lane < statistics.LaneCounts.Length; lane++)
        {
            report.AppendLine($"Lane {lane} count: {statistics.LaneCounts[lane]}");
        }

        report.AppendLine($"First hit time: {FormatSeconds(statistics.FirstHitTime)}");
        report.AppendLine($"Last hit time: {FormatSeconds(statistics.LastHitTime)}");
        report.AppendLine($"Minimum hit interval: {FormatSeconds(statistics.MinimumHitInterval)}");
        report.AppendLine($"Longest consecutive lane run: {statistics.LongestConsecutiveLaneRun}");
        report.AppendLine();

        report.AppendLine("REPRESENTATIVE TRANSFORMATIONS");
        foreach (ChartTransformationSample sample in buildResult.TransformationSamples)
        {
            report.AppendLine($"Tick={sample.Tick}");
            report.AppendLine($"  RAW: pitches=[{JoinIntegers(sample.RawPitches)}]");
            report.AppendLine($"  AFTER DEDUP: pitches=[{JoinIntegers(sample.UniquePitches)}]");
            report.AppendLine(
                $"  REPRESENTATIVE: pitch={sample.RepresentativePitch}, " +
                $"track={sample.RepresentativeTrackIndex}");
            report.AppendLine($"  OUTPUT: {FormatOutputNote(sample.OutputNote)}");
        }

        report.AppendLine();
        report.AppendLine("VALIDATION");
        report.AppendLine($"Result: {(validationResult.IsValid ? "PASS" : "FAIL")}");
        AppendMessages(report, "Errors", validationResult.Errors);
        AppendMessages(report, "Warnings", validationResult.Warnings);

        return report.ToString();
    }

    private static string FormatSeconds(double seconds)
    {
        return seconds.ToString("F6", CultureInfo.InvariantCulture) + "s";
    }

    private static string JoinIntegers(IReadOnlyList<int> values)
    {
        var text = new StringBuilder();
        for (int i = 0; i < values.Count; i++)
        {
            if (i > 0)
            {
                text.Append(", ");
            }

            text.Append(values[i]);
        }

        return text.ToString();
    }

    private static string FormatOutputNote(ChartBuiltNote note)
    {
        return $"pitch {note.NoteNumber} -> lane {note.Lane}";
    }

    private static void AppendMessages(
        StringBuilder report,
        string label,
        IReadOnlyList<string> messages)
    {
        if (messages.Count == 0)
        {
            report.AppendLine($"{label}: None");
            return;
        }

        report.AppendLine($"{label}:");
        foreach (string message in messages)
        {
            report.AppendLine($"- {message}");
        }
    }
}
