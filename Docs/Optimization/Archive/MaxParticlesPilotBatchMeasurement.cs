#if UNITY_EDITOR
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MagicTiles.Optimization.Experiments.Editor
{
    public static class MaxParticlesPilotBatchMeasurement
    {
        private const string PrefabPath = "Assets/Optimization/Prefabs/ParticleEffectsOptimized.prefab";
        private const string CandidatePath = "Transition/init";
        private const int ReplayCount = 120;
        private const int BuildAMaxParticles = 1000;
        private const int BuildBMaxParticles = 2;
        private const uint Seed = 424242;

        public static void Run()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            ParticleSystem candidate = prefab == null
                ? null
                : prefab.transform.Find(CandidatePath)?.GetComponent<ParticleSystem>();

            if (candidate == null)
                throw new System.InvalidOperationException("Could not load Transition/init from the optimized prefab.");

            if (candidate.main.maxParticles != BuildAMaxParticles)
                throw new System.InvalidOperationException("Build A no longer has maxParticles = 1000.");

            BuildMeasurement buildA = Measure(candidate, "A", BuildAMaxParticles);
            BuildMeasurement buildB = Measure(candidate, "B", BuildBMaxParticles);
            string result = buildA.Format() + "\n" + buildB.Format();
            string outputPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "MaxParticlesPilotResult.txt");
            File.WriteAllText(outputPath, result);
            Debug.Log(result);
        }

        private static BuildMeasurement Measure(ParticleSystem template, string buildName, int maxParticles)
        {
            ParticleSystem system = Object.Instantiate(template);
            ParticleSystem.MainModule main = system.main;
            main.maxParticles = maxParticles;

            BuildMeasurement measurement = new BuildMeasurement(buildName, maxParticles);
            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[1];
            for (int replayIndex = 0; replayIndex < ReplayCount; replayIndex++)
            {
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                system.Clear(true);
                system.useAutoRandomSeed = false;
                system.randomSeed = Seed;
                system.Simulate(1f / 60f, true, true, true);

                int peak = system.particleCount;
                measurement.AddPeak(peak);
                int particleCount = system.GetParticles(particles);
                measurement.AllReplaysHaveOneParticle &= particleCount == 1;
                measurement.AllParticleStatesMatch &= particleCount == 1 && MatchesReference(measurement, particles[0]);

                system.Simulate(1f, true, false, true);
                measurement.NoStaleParticles &= system.particleCount == 0;
            }

            Object.DestroyImmediate(system.gameObject);
            return measurement;
        }

        private static bool MatchesReference(BuildMeasurement measurement, ParticleSystem.Particle particle)
        {
            if (!measurement.HasReferenceParticle)
            {
                measurement.SetReferenceParticle(particle);
                return true;
            }

            return measurement.ReferenceRandomSeed == particle.randomSeed &&
                   Approximately(measurement.ReferencePosition, particle.position) &&
                   Approximately(measurement.ReferenceVelocity, particle.velocity) &&
                   Mathf.Approximately(measurement.ReferenceStartLifetime, particle.startLifetime);
        }

        private static bool Approximately(Vector3 first, Vector3 second)
        {
            return Mathf.Approximately(first.x, second.x) &&
                   Mathf.Approximately(first.y, second.y) &&
                   Mathf.Approximately(first.z, second.z);
        }

        private sealed class BuildMeasurement
        {
            public BuildMeasurement(string buildName, int maxParticles)
            {
                BuildName = buildName;
                MaxParticles = maxParticles;
                MinimumPeak = int.MaxValue;
                AllReplaysHaveOneParticle = true;
                AllParticleStatesMatch = true;
                NoStaleParticles = true;
            }

            public string BuildName { get; }
            public int MaxParticles { get; }
            public int MinimumPeak { get; private set; }
            public int MaximumPeak { get; private set; }
            public int PeakTotal { get; private set; }
            public int ReplaysAtOrAboveCap { get; private set; }
            public bool AllReplaysHaveOneParticle { get; set; }
            public bool AllParticleStatesMatch { get; set; }
            public bool NoStaleParticles { get; set; }
            public bool HasReferenceParticle { get; private set; }
            public uint ReferenceRandomSeed { get; private set; }
            public Vector3 ReferencePosition { get; private set; }
            public Vector3 ReferenceVelocity { get; private set; }
            public float ReferenceStartLifetime { get; private set; }

            public void AddPeak(int peak)
            {
                MinimumPeak = Mathf.Min(MinimumPeak, peak);
                MaximumPeak = Mathf.Max(MaximumPeak, peak);
                PeakTotal += peak;
                if (peak >= MaxParticles)
                    ReplaysAtOrAboveCap++;
            }

            public void SetReferenceParticle(ParticleSystem.Particle particle)
            {
                HasReferenceParticle = true;
                ReferenceRandomSeed = particle.randomSeed;
                ReferencePosition = particle.position;
                ReferenceVelocity = particle.velocity;
                ReferenceStartLifetime = particle.startLifetime;
            }

            public string Format()
            {
                float average = PeakTotal / (float)ReplayCount;
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "BUILD={0};CAP={1};REPLAYS={2};PEAK_MIN={3};PEAK_MAX={4};PEAK_AVG={5:F3};AT_OR_ABOVE_CAP={6};ONE_PARTICLE={7};STATE_STABLE={8};NO_STALE={9}",
                    BuildName,
                    MaxParticles,
                    ReplayCount,
                    MinimumPeak,
                    MaximumPeak,
                    average,
                    ReplaysAtOrAboveCap,
                    AllReplaysHaveOneParticle,
                    AllParticleStatesMatch,
                    NoStaleParticles);
            }
        }
    }
}
#endif
