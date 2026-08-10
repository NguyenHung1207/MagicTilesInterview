using System.Collections.Generic;
using NUnit.Framework;

public class ChartBuilderTests
{
    [Test]
    public void NarrowPitchRangeUsesAllLanesWhenFeasible()
    {
        ChartBuildResult result = BuildRepeating(new[] { 60, 61, 62, 63 }, 16);

        Assert.That(result.Statistics.LaneCounts, Has.All.GreaterThan(0));
        Assert.That(result.Statistics.LongestConsecutiveLaneRun, Is.LessThan(8));
    }

    [Test]
    public void ClusteredPitchInputDoesNotCollapseToOneLane()
    {
        ChartBuildResult result = BuildRepeating(new[] { 60, 60, 60, 60 }, 32);

        int largestLane = 0;
        foreach (int count in result.Statistics.LaneCounts)
        {
            largestLane = System.Math.Max(largestLane, count);
        }

        Assert.That(largestLane, Is.LessThan(result.Chart.notes.Count * 0.8));
        Assert.That(result.Statistics.LongestConsecutiveLaneRun, Is.LessThan(8));
    }

    [Test]
    public void AscendingPitchesProduceAscendingLaneBias()
    {
        ChartBuildResult result = BuildRepeating(new[] { 48, 50, 52, 54, 56, 58, 60, 62 }, 1);

        double firstHalfAverage = 0d;
        double secondHalfAverage = 0d;
        for (int index = 0; index < result.Chart.notes.Count / 2; index++)
            firstHalfAverage += result.Chart.notes[index].lane;
        for (int index = result.Chart.notes.Count / 2; index < result.Chart.notes.Count; index++)
            secondHalfAverage += result.Chart.notes[index].lane;

        Assert.That(secondHalfAverage, Is.GreaterThan(firstHalfAverage));
    }

    [Test]
    public void RepeatedBuildIsDeterministicAndPreservesTiming()
    {
        List<MidiExtractedNote> source = CreateNotes(new[] { 60, 64, 67, 72 }, 8);
        ChartBuildResult first = new ChartBuilder().Build(source);
        ChartBuildResult second = new ChartBuilder().Build(source);

        Assert.That(second.Chart.notes.Count, Is.EqualTo(first.Chart.notes.Count));
        for (int index = 0; index < first.Chart.notes.Count; index++)
        {
            Assert.That(second.Chart.notes[index].lane, Is.EqualTo(first.Chart.notes[index].lane));
            Assert.That(second.Chart.notes[index].hitTime, Is.EqualTo(first.Chart.notes[index].hitTime));
        }
    }

    private static ChartBuildResult BuildRepeating(int[] pitches, int repetitions)
    {
        return new ChartBuilder().Build(CreateNotes(pitches, repetitions));
    }

    private static List<MidiExtractedNote> CreateNotes(int[] pitches, int repetitions)
    {
        var notes = new List<MidiExtractedNote>(pitches.Length * repetitions);
        long tick = 0;
        double time = 0d;
        for (int repeat = 0; repeat < repetitions; repeat++)
        {
            foreach (int pitch in pitches)
            {
                notes.Add(new MidiExtractedNote
                {
                    Tick = tick,
                    TimeSeconds = time,
                    NoteNumber = pitch,
                    TrackIndex = ChartBuilder.PreferredMelodyTrackIndex
                });
                tick += 120;
                time += 0.25d;
            }
        }

        return notes;
    }
}
