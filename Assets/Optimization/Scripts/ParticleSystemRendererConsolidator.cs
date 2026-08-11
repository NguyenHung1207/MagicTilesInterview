using UnityEngine;

namespace MagicTiles.Optimization
{
    /// <summary>
    /// Copies newly emitted particles from a renderer-disabled source into an
    /// otherwise identical target system. Both systems keep their original
    /// deterministic simulation, while only the target submits render geometry.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ParticleSystemRendererConsolidator : MonoBehaviour
    {
        [SerializeField] private ParticleSystem target;
        [SerializeField] private ParticleSystem source;

        private ParticleSystem.Particle[] targetParticles;
        private ParticleSystem.Particle[] sourceParticles;
        private uint[] transferredSourceSeeds;
        private int transferredSeedCount;
        private bool initialized;
        private bool transferComplete;

        private void Awake()
        {
            if (target == null || source == null || target == source)
            {
                Debug.LogError("Particle renderer consolidation requires two distinct ParticleSystems.", this);
                enabled = false;
                return;
            }

            int targetCapacity = target.main.maxParticles + source.main.maxParticles;
            targetParticles = new ParticleSystem.Particle[targetCapacity];
            sourceParticles = new ParticleSystem.Particle[source.main.maxParticles];
            transferredSourceSeeds = new uint[source.main.maxParticles];
            initialized = true;
        }

        private void LateUpdate()
        {
            if (!initialized)
                return;

            if (source.isStopped)
            {
                transferredSeedCount = 0;
                transferComplete = false;
                return;
            }

            if (transferComplete)
                return;

            int sourceCount = source.GetParticles(sourceParticles);
            if (sourceCount == 0)
                return;

            int targetCount = target.GetParticles(targetParticles);
            bool changed = false;

            for (int sourceIndex = 0; sourceIndex < sourceCount; sourceIndex++)
            {
                ParticleSystem.Particle particle = sourceParticles[sourceIndex];
                if (WasTransferred(particle.randomSeed))
                    continue;

                if (targetCount >= targetParticles.Length || transferredSeedCount >= transferredSourceSeeds.Length)
                {
                    Debug.LogError("Particle renderer consolidation cache capacity was exceeded.", this);
                    enabled = false;
                    return;
                }

                ConvertSimulationSpace(ref particle);
                targetParticles[targetCount] = particle;
                targetCount++;
                transferredSourceSeeds[transferredSeedCount] = particle.randomSeed;
                transferredSeedCount++;
                changed = true;
            }

            if (changed)
            {
                target.SetParticles(targetParticles, targetCount);
                transferComplete = true;
            }
        }

        private bool WasTransferred(uint randomSeed)
        {
            for (int index = 0; index < transferredSeedCount; index++)
            {
                if (transferredSourceSeeds[index] == randomSeed)
                    return true;
            }

            return false;
        }

        private void ConvertSimulationSpace(ref ParticleSystem.Particle particle)
        {
            Transform sourceSpace = GetSimulationTransform(source);
            Transform targetSpace = GetSimulationTransform(target);

            Vector3 worldPosition = sourceSpace == null
                ? particle.position
                : sourceSpace.TransformPoint(particle.position);
            Vector3 worldVelocity = sourceSpace == null
                ? particle.velocity
                : sourceSpace.TransformVector(particle.velocity);
            Vector3 worldAxis = sourceSpace == null
                ? particle.axisOfRotation
                : sourceSpace.TransformDirection(particle.axisOfRotation);

            particle.position = targetSpace == null
                ? worldPosition
                : targetSpace.InverseTransformPoint(worldPosition);
            particle.velocity = targetSpace == null
                ? worldVelocity
                : targetSpace.InverseTransformVector(worldVelocity);
            particle.axisOfRotation = targetSpace == null
                ? worldAxis
                : targetSpace.InverseTransformDirection(worldAxis);
        }

        private static Transform GetSimulationTransform(ParticleSystem system)
        {
            ParticleSystem.MainModule main = system.main;
            switch (main.simulationSpace)
            {
                case ParticleSystemSimulationSpace.Local:
                    return system.transform;
                case ParticleSystemSimulationSpace.Custom:
                    return main.customSimulationSpace;
                default:
                    return null;
            }
        }
    }
}
