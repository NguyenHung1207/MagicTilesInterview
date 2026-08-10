using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Melanchall.DryWetMidi.Core;

public static class MidiChartImporter
{
    [MenuItem("Tools/Magic Tiles/Build Demo Song Chart")]
    public static void BuildDemoSongChart()
    {
        BuildChart(
            "Assets/GameData/Source/Midi/DemoSong_Source.mid.bytes",
            "Assets/GameData/Generated/Charts/DemoSong_Chart.json",
            "Assets/GameData/Reports/DemoSong_ChartImportReport.txt");
    }

    [MenuItem("Tools/Magic Tiles/Build All Music Charts")]
    public static void BuildAllMusicCharts()
    {
        BuildDemoSongChart();
        BuildChart(
            "Assets/GameData/Source/Midi/Sonata01_Source.mid.bytes",
            "Assets/GameData/Generated/Charts/Sonata01_Chart.json",
            "Assets/GameData/Reports/Sonata01_ChartImportReport.txt");
        BuildChart(
            "Assets/GameData/Source/Midi/Sonata02_Source.mid.bytes",
            "Assets/GameData/Generated/Charts/Sonata02_Chart.json",
            "Assets/GameData/Reports/Sonata02_ChartImportReport.txt");
        BuildChart(
            "Assets/GameData/Source/Midi/Sakura_Source.mid.bytes",
            "Assets/GameData/Generated/Charts/Sakura_Chart.json",
            "Assets/GameData/Reports/Sakura_ChartImportReport.txt");
    }

    private static void BuildChart(
        string midiAssetPath,
        string chartOutputPath,
        string reportOutputPath)
    {
        try
        {
            TextAsset midiAsset = LoadMidiAsset(midiAssetPath);
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
                midiAssetPath,
                trackCount,
                buildResult,
                validationResult);
            WriteProjectFile(reportOutputPath, report);

            if (!validationResult.IsValid)
            {
                AssetDatabase.Refresh();
                Debug.LogError($"Chart validation failed. See {reportOutputPath} for details.");
                return;
            }

            string json = JsonUtility.ToJson(buildResult.Chart, true) + Environment.NewLine;
            WriteProjectFile(chartOutputPath, json);
            AssetDatabase.Refresh();

            ChartBuildStatistics statistics = buildResult.Statistics;
            Debug.Log(
                $"Built {Path.GetFileNameWithoutExtension(chartOutputPath)} chart: " +
                $"{statistics.RawNoteCount} raw -> {statistics.GameplayNoteCount} gameplay notes, " +
                $"{statistics.ExactDuplicatesRemoved} duplicates removed, " +
                $"{statistics.RepresentativeNoteCount} representatives selected. " +
                $"Validation PASS. Output: {chartOutputPath}");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/Magic Tiles/Diagnostics/Preview Raw MIDI Notes")]
    private static void PreviewRawMidiNotes()
    {
        TextAsset midiAsset = LoadMidiAsset(
            "Assets/GameData/Source/Midi/DemoSong_Source.mid.bytes");
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
                $"[{i}] Tick={note.Tick}, Time={note.TimeSeconds:F3}s, " +
                $"Note={note.NoteNumber}, Track={note.TrackIndex}");
        }
    }

    private static TextAsset LoadMidiAsset(string midiAssetPath)
    {
        TextAsset midiAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(midiAssetPath);
        if (midiAsset == null)
        {
            Debug.LogError($"MIDI asset not found at {midiAssetPath}.");
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
