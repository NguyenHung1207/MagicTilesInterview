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

        report.AppendLine("CHORD ANALYSIS");
        report.AppendLine($"Unique Tick groups: {statistics.TickGroupCount}");
        report.AppendLine($"Chord groups (>1 pitch): {statistics.ChordGroupCount}");
        report.AppendLine($"Largest chord size: {statistics.LargestChordSize}");
        report.AppendLine($"Groups exceeding 4 pitches: {statistics.GroupsExceedingLaneCount}");
        report.AppendLine($"Notes dropped by 4-lane limit: {statistics.NotesDroppedByLaneLimit}");
        report.AppendLine();

        report.AppendLine("OUTPUT");
        report.AppendLine($"Total gameplay notes: {statistics.GameplayNoteCount}");
        for (int lane = 0; lane < statistics.LaneCounts.Length; lane++)
        {
            report.AppendLine($"Lane {lane} count: {statistics.LaneCounts[lane]}");
        }

        report.AppendLine($"First hit time: {FormatSeconds(statistics.FirstHitTime)}");
        report.AppendLine($"Last hit time: {FormatSeconds(statistics.LastHitTime)}");
        report.AppendLine();

        report.AppendLine("REPRESENTATIVE TRANSFORMATIONS");
        foreach (ChartTransformationSample sample in buildResult.TransformationSamples)
        {
            report.AppendLine($"Tick={sample.Tick}");
            report.AppendLine($"  RAW: pitches=[{JoinIntegers(sample.RawPitches)}]");
            report.AppendLine($"  AFTER DEDUP: pitches=[{JoinIntegers(sample.UniquePitches)}]");
            report.AppendLine($"  AFTER 4-LANE LIMIT: pitches=[{JoinIntegers(sample.SelectedPitches)}]");
            report.AppendLine($"  OUTPUT: {FormatOutputNotes(sample.OutputNotes)}");
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

    private static string FormatOutputNotes(IReadOnlyList<ChartBuiltNote> notes)
    {
        var text = new StringBuilder();
        for (int i = 0; i < notes.Count; i++)
        {
            if (i > 0)
            {
                text.Append(", ");
            }

            text.Append($"pitch {notes[i].NoteNumber} -> lane {notes[i].Lane}");
        }

        return text.ToString();
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
