using System;
using MagicTiles.Optimization;
using UnityEngine;

namespace MagicTiles.OptimizationExperiments
{
    /// <summary>
    /// Isolated PL3 lines overdraw pilot. The serialized lifetime value belongs
    /// to this experiment scene and is applied only to the instantiated prefab
    /// copy at runtime. No production prefab asset is edited.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OverdrawPL3LinesPilot : MonoBehaviour
    {
        private const float RequiredMinimumVertexDistance = 0.4f;

        [SerializeField] private ParticleSystem pl3Root;
        [SerializeField] private float trailLifetimeMultiplier = 0.5f;
        [SerializeField] private bool replayOnStart = true;
        [SerializeField] private uint baseRandomSeed = 1337;

        private ParticleSystem[] systems;
        private ParticleSystem lines;
        private DeterministicRainEmitter rain;
        private bool initialized;

        public float TrailLifetimeMultiplier => trailLifetimeMultiplier;
        public ParticleSystem Lines => lines;

        private void Awake()
        {
            if (pl3Root == null)
            {
                Debug.LogError("PL3 overdraw pilot requires a PL3 root reference.", this);
                enabled = false;
                return;
            }

            lines = pl3Root.transform.Find("lines")?.GetComponent<ParticleSystem>();
            rain = pl3Root.GetComponent<DeterministicRainEmitter>();
            systems = CacheHierarchyOrder(pl3Root.transform);

            if (lines == null || rain == null)
            {
                Debug.LogError("PL3 overdraw pilot could not resolve lines or deterministic rain.", this);
                enabled = false;
                return;
            }

            ParticleSystem.TrailModule trail = lines.trails;
            if (!Mathf.Approximately(trail.minVertexDistance, RequiredMinimumVertexDistance))
            {
                Debug.LogError(
                    $"Pilot requires PL3 lines minVertexDistance {RequiredMinimumVertexDistance}, " +
                    $"found {trail.minVertexDistance}. The pilot will not alter it.",
                    this);
                enabled = false;
                return;
            }

            if (trailLifetimeMultiplier <= 0f)
            {
                Debug.LogError("Pilot Trail Lifetime must be positive.", this);
                enabled = false;
                return;
            }

            // This setter targets the instantiated scene copy only.
            trail.lifetimeMultiplier = trailLifetimeMultiplier;
            initialized = true;
        }

        private void Start()
        {
            if (replayOnStart)
                Replay();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
                Replay();
        }

        public void Replay()
        {
            if (!initialized)
                return;

            StopAndClear(systems);

            uint seed = baseRandomSeed;
            uint rightRainSeed = 0u;
            uint leftRainSeed = 0u;

            for (int index = 0; index < systems.Length; index++)
            {
                ParticleSystem system = systems[index];
                system.useAutoRandomSeed = false;
                system.randomSeed = seed;

                if (system == rain.RainSystem)
                {
                    rightRainSeed = seed++;
                    leftRainSeed = seed++;
                }
                else
                {
                    seed++;
                }
            }

            pl3Root.Play(true);
            rain.EmitDeterministicRain(rightRainSeed, leftRainSeed);
        }

        private static void StopAndClear(ParticleSystem[] cachedSystems)
        {
            for (int index = 0; index < cachedSystems.Length; index++)
            {
                ParticleSystem system = cachedSystems[index];
                system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                system.Clear(false);
            }
        }

        private static ParticleSystem[] CacheHierarchyOrder(Transform root)
        {
            int count = CountParticleSystems(root);
            ParticleSystem[] cached = new ParticleSystem[count];
            int index = 0;
            AddParticleSystems(root, cached, ref index);
            return cached;
        }

        private static int CountParticleSystems(Transform current)
        {
            int count = current.TryGetComponent(out ParticleSystem _) ? 1 : 0;
            for (int childIndex = 0; childIndex < current.childCount; childIndex++)
                count += CountParticleSystems(current.GetChild(childIndex));
            return count;
        }

        private static void AddParticleSystems(
            Transform current,
            ParticleSystem[] cached,
            ref int index)
        {
            if (current.TryGetComponent(out ParticleSystem system))
                cached[index++] = system;

            for (int childIndex = 0; childIndex < current.childCount; childIndex++)
                AddParticleSystems(current.GetChild(childIndex), cached, ref index);
        }
    }
}
