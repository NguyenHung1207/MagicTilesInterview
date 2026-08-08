using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Melanchall.DryWetMidi.Core;

public static class MidiChartImporter
{
    private const string MidiAssetPath =
        "Assets/GameData/Source/Midi/DemoSong_Source.mid.bytes";
    private const string ChartOutputPath =
        "Assets/GameData/Generated/Charts/DemoSong_Chart.json";
    private const string ReportOutputPath =
        "Assets/GameData/Reports/DemoSong_ChartImportReport.txt";

    [MenuItem("Tools/Magic Tiles/Build Demo Song Chart")]
    public static void BuildDemoSongChart()
    {
        try
        {
            TextAsset midiAsset = LoadMidiAsset();
            if (midiAsset == null)
            {
                return;
            }

            using var stream = new MemoryStream(midiAsset.bytes);
            MidiFile midiFile = MidiFile.Read(stream);
            int trackCount = CountTracks(midiFile);

            var extractor = new MidiNoteExtractor();
            var builder = new ChartBuilder();
            var validator = new ChartValidator();

            var rawNotes = extractor.Extract(midiFile);
            ChartBuildResult buildResult = builder.Build(rawNotes);
            ChartValidationResult validationResult = validator.Validate(buildResult);

            string report = ChartImportReportWriter.Create(
                MidiAssetPath,
                trackCount,
                buildResult,
                validationResult);
            WriteProjectFile(ReportOutputPath, report);

            if (!validationResult.IsValid)
            {
                AssetDatabase.Refresh();
                Debug.LogError(
                    $"Chart validation failed. See {ReportOutputPath} for details.");
                return;
            }

            string json = JsonUtility.ToJson(buildResult.Chart, true) + Environment.NewLine;
            WriteProjectFile(ChartOutputPath, json);
            AssetDatabase.Refresh();

            ChartBuildStatistics statistics = buildResult.Statistics;
            Debug.Log(
                $"Built Demo Song chart: {statistics.RawNoteCount} raw -> " +
                $"{statistics.GameplayNoteCount} gameplay notes, " +
                $"{statistics.ExactDuplicatesRemoved} duplicates removed, " +
                $"{statistics.RepresentativeNoteCount} representatives selected. " +
                $"Validation PASS. Output: {ChartOutputPath}");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/Magic Tiles/Diagnostics/Preview Raw MIDI Notes")]
    private static void PreviewRawMidiNotes()
    {
        TextAsset midiAsset = LoadMidiAsset();

        if (midiAsset == null)
        {
            return;
        }

        using var stream = new MemoryStream(midiAsset.bytes);
        MidiFile midiFile = MidiFile.Read(stream);
        var extractor = new MidiNoteExtractor();
        var extractedNotes = extractor.Extract(midiFile);
        Debug.Log($"Extracted {extractedNotes.Count} raw MIDI notes.");
        int previewCount = Math.Min(10, extractedNotes.Count);
        for (int i = 0; i < previewCount; i++)
        {
            MidiExtractedNote note = extractedNotes[i];
            Debug.Log(
                $"[{i}] Tick={note.Tick}, " +
                $"Time={note.TimeSeconds:F3}s, " +
                $"Note={note.NoteNumber}, " +
                $"Track={note.TrackIndex}");
        }
    }

    private static TextAsset LoadMidiAsset()
    {
        TextAsset midiAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(MidiAssetPath);
        if (midiAsset == null)
        {
            Debug.LogError($"MIDI asset not found at {MidiAssetPath}.");
        }

        return midiAsset;
    }

    private static int CountTracks(MidiFile midiFile)
    {
        int count = 0;
        foreach (TrackChunk unused in midiFile.GetTrackChunks())
        {
            count++;
        }

        return count;
    }

    private static void WriteProjectFile(string assetPath, string contents)
    {
        string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
        string directory = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(absolutePath, contents);
    }
}
