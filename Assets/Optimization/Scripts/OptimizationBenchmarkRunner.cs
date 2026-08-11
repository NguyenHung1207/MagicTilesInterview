using UnityEngine;

public enum BenchmarkVariant
{
    Transition,
    PerfectLevel1,
    PerfectLevel2,
    PerfectLevel3
}

[DisallowMultipleComponent]
public sealed class OptimizationBenchmarkRunner : MonoBehaviour
{
    [SerializeField] private BenchmarkVariant selectedVariant = BenchmarkVariant.PerfectLevel3;
    [SerializeField] private ParticleSystem transition;
    [SerializeField] private ParticleSystem perfectLevel1;
    [SerializeField] private ParticleSystem perfectLevel2;
    [SerializeField] private ParticleSystem perfectLevel3;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private uint baseRandomSeed = 1337;

    private ParticleSystem[] transitionSystems;
    private ParticleSystem[] perfectLevel1Systems;
    private ParticleSystem[] perfectLevel2Systems;
    private ParticleSystem[] perfectLevel3Systems;
    private int replayRequestedFrame = -1;
    private bool initialized;

    public BenchmarkVariant SelectedVariant => selectedVariant;

    public int CachedParticleCount =>
        GetLength(transitionSystems) +
        GetLength(perfectLevel1Systems) +
        GetLength(perfectLevel2Systems) +
        GetLength(perfectLevel3Systems);

    private void Awake()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        // Cache depth-first by sibling index so seed assignment does not depend on
        // random runtime state or on repeated component queries during replay.
        transitionSystems = CacheHierarchyOrder(transition);
        perfectLevel1Systems = CacheHierarchyOrder(perfectLevel1);
        perfectLevel2Systems = CacheHierarchyOrder(perfectLevel2);
        perfectLevel3Systems = CacheHierarchyOrder(perfectLevel3);
        initialized = true;
    }

    private void Start()
    {
        if (playOnStart)
        {
            Replay();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectAndReplay(BenchmarkVariant.Transition);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectAndReplay(BenchmarkVariant.PerfectLevel1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SelectAndReplay(BenchmarkVariant.PerfectLevel2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SelectAndReplay(BenchmarkVariant.PerfectLevel3);
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            Replay();
        }

        if (replayRequestedFrame >= 0 && Time.frameCount > replayRequestedFrame)
        {
            PlayDeterministically();
            replayRequestedFrame = -1;
        }
    }

    public void SelectAndReplay(BenchmarkVariant variant)
    {
        bool selectionChanged = selectedVariant != variant;
        selectedVariant = variant;

#if UNITY_EDITOR
        if (selectionChanged)
        {
            Debug.Log($"[OptimizationBenchmark] Variant: {selectedVariant}", this);
        }
#endif

        Replay();
    }

    public void Replay()
    {
        if (!initialized)
        {
            return;
        }

        StopAndClear(transitionSystems);
        StopAndClear(perfectLevel1Systems);
        StopAndClear(perfectLevel2Systems);
        StopAndClear(perfectLevel3Systems);

        replayRequestedFrame = Time.frameCount;
    }

    private void PlayDeterministically()
    {
        uint seed = baseRandomSeed;
        AssignSeeds(transitionSystems, ref seed);
        AssignSeeds(perfectLevel1Systems, ref seed);
        AssignSeeds(perfectLevel2Systems, ref seed);
        AssignSeeds(perfectLevel3Systems, ref seed);

        GetSelectedRoot().Play(true);
    }

    private ParticleSystem GetSelectedRoot()
    {
        switch (selectedVariant)
        {
            case BenchmarkVariant.Transition:
                return transition;
            case BenchmarkVariant.PerfectLevel1:
                return perfectLevel1;
            case BenchmarkVariant.PerfectLevel2:
                return perfectLevel2;
            case BenchmarkVariant.PerfectLevel3:
                return perfectLevel3;
            default:
                return perfectLevel3;
        }
    }

    private bool ValidateReferences()
    {
        if (transition == null || perfectLevel1 == null || perfectLevel2 == null || perfectLevel3 == null)
        {
            Debug.LogError("Optimization benchmark requires all four variant ParticleSystem references.", this);
            return false;
        }

        if (transition == perfectLevel1 || transition == perfectLevel2 || transition == perfectLevel3 ||
            perfectLevel1 == perfectLevel2 || perfectLevel1 == perfectLevel3 ||
            perfectLevel2 == perfectLevel3)
        {
            Debug.LogError("Optimization benchmark variant references must be unique.", this);
            return false;
        }

        return true;
    }

    private static void StopAndClear(ParticleSystem[] systems)
    {
        for (int index = 0; index < systems.Length; index++)
        {
            ParticleSystem system = systems[index];
            system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            system.Clear(false);
        }
    }

    private static void AssignSeeds(ParticleSystem[] systems, ref uint seed)
    {
        for (int index = 0; index < systems.Length; index++)
        {
            ParticleSystem system = systems[index];
            system.useAutoRandomSeed = false;
            system.randomSeed = seed;
            seed++;
        }
    }

    private static int GetLength(ParticleSystem[] systems)
    {
        return systems == null ? 0 : systems.Length;
    }

    private static ParticleSystem[] CacheHierarchyOrder(ParticleSystem root)
    {
        int count = CountParticleSystems(root.transform);
        ParticleSystem[] systems = new ParticleSystem[count];
        int index = 0;
        AddParticleSystems(root.transform, systems, ref index);
        return systems;
    }

    private static int CountParticleSystems(Transform current)
    {
        int count = current.TryGetComponent(out ParticleSystem _) ? 1 : 0;

        for (int childIndex = 0; childIndex < current.childCount; childIndex++)
        {
            count += CountParticleSystems(current.GetChild(childIndex));
        }

        return count;
    }

    private static void AddParticleSystems(Transform current, ParticleSystem[] systems, ref int index)
    {
        if (current.TryGetComponent(out ParticleSystem system))
        {
            systems[index] = system;
            index++;
        }

        for (int childIndex = 0; childIndex < current.childCount; childIndex++)
        {
            AddParticleSystems(current.GetChild(childIndex), systems, ref index);
        }
    }
}
