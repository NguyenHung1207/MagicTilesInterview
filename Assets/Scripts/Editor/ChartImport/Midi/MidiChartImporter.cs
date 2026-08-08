using System.IO;
using UnityEditor;
using UnityEngine;
using Melanchall.DryWetMidi.Core;

public static class MidiChartImporter
{
    [MenuItem("Tools/Magic Tiles/Test MIDI Extraction")]
    private static void TestMidiExtraction()
    {
        const string midiAssetPath = "Assets/GameData/Source/Midi/DemoSong_Source.mid.bytes";
        TextAsset midiAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(midiAssetPath);

        if (midiAsset == null)
        {
            Debug.LogError($"MIDI asset not found at {midiAssetPath}.");
            return;
        }

        using var stream = new MemoryStream(midiAsset.bytes);
        MidiFile midiFile = MidiFile.Read(stream);
        var extractor = new MidiNoteExtractor();
        var extractedNotes = extractor.Extract(midiFile);
        Debug.Log($"Extracted {extractedNotes.Count} raw MIDI notes.");
        int previewCount = Mathf.Min(10, extractedNotes.Count);
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
}