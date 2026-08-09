using UnityEngine;

public sealed class OptimizationBenchmarkRunner : MonoBehaviour
{
    [SerializeField] private ParticleSystem perfectLevel3;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private uint baseRandomSeed = 1337;

    private ParticleSystem[] cachedSystems;
    private int replayRequestedFrame = -1;

    public int CachedParticleCount => cachedSystems?.Length ?? 0;

    private void Awake()
    {
        if (perfectLevel3 == null)
        {
            Debug.LogError("Optimization benchmark requires a PerfectLevel3 ParticleSystem reference.", this);
            enabled = false;
            return;
        }

        cachedSystems = perfectLevel3.GetComponentsInChildren<ParticleSystem>(true);
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
        if (Input.GetKeyDown(KeyCode.R))
        {
            Replay();
        }

        if (replayRequestedFrame >= 0 && Time.frameCount > replayRequestedFrame)
        {
            PlayDeterministically();
            replayRequestedFrame = -1;
        }
    }

    public void Replay()
    {
        if (cachedSystems == null)
        {
            return;
        }

        foreach (ParticleSystem system in cachedSystems)
        {
            system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            system.Clear(false);
        }

        replayRequestedFrame = Time.frameCount;
    }

    private void PlayDeterministically()
    {
        for (int index = 0; index < cachedSystems.Length; index++)
        {
            ParticleSystem system = cachedSystems[index];
            system.useAutoRandomSeed = false;
            system.randomSeed = baseRandomSeed + (uint)index;
        }

        perfectLevel3.Play(true);
    }
}
