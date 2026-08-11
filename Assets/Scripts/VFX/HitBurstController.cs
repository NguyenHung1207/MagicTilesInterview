using UnityEngine;

public class HitBurstController : MonoBehaviour
{
    [SerializeField] private ParticleSystem hitBurstPrefab;
    [SerializeField, Min(1)] private int poolSize = 16;

    [SerializeField] private Color perfectColor = new Color(1f, 0.78f, 0.2f);
    [SerializeField] private Color greatColor = new Color(0.2f, 0.75f, 1f);
    [SerializeField] private Color goodColor = new Color(0.35f, 1f, 0.5f);

    private ParticleSystem[] burstPool;
    private bool[] activeBursts;
    private int nextPoolIndex;

    private void Awake()
    {
        if (hitBurstPrefab == null)
        {
            Debug.LogError("HitBurstController requires a hit burst prefab.", this);
            enabled = false;
            return;
        }

        int count = Mathf.Max(1, poolSize);
        burstPool = new ParticleSystem[count];
        activeBursts = new bool[count];

        for (int i = 0; i < count; i++)
        {
            ParticleSystem burst = Instantiate(hitBurstPrefab, transform);
            burst.name = $"HitBurst_{i}";
            burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            burst.gameObject.SetActive(false);
            burstPool[i] = burst;
        }
    }

    private void OnEnable()
    {
        Note.HitSucceeded += HandleHit;
    }

    private void OnDisable()
    {
        Note.HitSucceeded -= HandleHit;

        if (burstPool == null)
        {
            return;
        }

        for (int i = 0; i < burstPool.Length; i++)
        {
            ResetBurst(i);
        }
    }

    private void Update()
    {
        if (burstPool == null)
        {
            return;
        }

        for (int i = 0; i < burstPool.Length; i++)
        {
            if (activeBursts[i] && !burstPool[i].IsAlive(true))
            {
                ResetBurst(i);
            }
        }
    }

    private void HandleHit(int laneIndex, HitJudgement judgement, Vector3 worldPosition)
    {
        int poolIndex = AcquireBurst();
        ParticleSystem burst = burstPool[poolIndex];

        burst.gameObject.SetActive(true);
        burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        burst.Clear(true);
        burst.transform.SetPositionAndRotation(worldPosition, Quaternion.identity);

        switch (judgement)
        {
            case HitJudgement.Good:
                PlayGood(burst);
                break;

            case HitJudgement.Great:
                PlayGreat(burst);
                break;

            case HitJudgement.Perfect:
                PlayPerfect(burst);
                break;

            default:
                ResetBurst(poolIndex);
                return;
        }

        activeBursts[poolIndex] = true;
    }

    private int AcquireBurst()
    {
        for (int offset = 0; offset < burstPool.Length; offset++)
        {
            int index = (nextPoolIndex + offset) % burstPool.Length;
            if (activeBursts[index])
            {
                continue;
            }

            nextPoolIndex = (index + 1) % burstPool.Length;
            return index;
        }

        int recycledIndex = nextPoolIndex;
        nextPoolIndex = (nextPoolIndex + 1) % burstPool.Length;
        ResetBurst(recycledIndex);
        return recycledIndex;
    }

    private void PlayGood(ParticleSystem burst)
    {
        burst.transform.localScale = Vector3.one * 0.72f;
        ConfigureBurst(burst, goodColor, 0.24f, 2.1f, 0.1f);
        burst.Emit(6);
    }

    private void PlayGreat(ParticleSystem burst)
    {
        burst.transform.localScale = Vector3.one * 0.96f;
        ConfigureBurst(burst, greatColor, 0.34f, 2.8f, 0.13f);
        burst.Emit(11);

        Color violetAccent = Color.Lerp(greatColor, new Color(0.72f, 0.46f, 1f), 0.55f);
        ConfigureBurst(burst, violetAccent, 0.24f, 3.3f, 0.09f);
        burst.Emit(4);
    }

    private void PlayPerfect(ParticleSystem burst)
    {
        burst.transform.localScale = Vector3.one * 1.18f;
        ConfigureBurst(burst, perfectColor, 0.44f, 3.4f, 0.15f);
        burst.Emit(17);

        Color whiteGold = Color.Lerp(perfectColor, Color.white, 0.72f);
        ConfigureBurst(burst, whiteGold, 0.3f, 4f, 0.11f);
        burst.Emit(8);

        ParticleSystem.EmitParams centralFlash = new ParticleSystem.EmitParams
        {
            position = Vector3.zero,
            velocity = Vector3.zero,
            startColor = Color.white,
            startLifetime = 0.18f,
            startSize = 0.62f
        };
        burst.Emit(centralFlash, 1);
    }

    private static void ConfigureBurst(
        ParticleSystem burst,
        Color color,
        float lifetime,
        float speed,
        float size)
    {
        ParticleSystem.MainModule main = burst.main;
        main.startColor = color;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
    }

    private void ResetBurst(int index)
    {
        ParticleSystem burst = burstPool[index];
        if (burst == null)
        {
            return;
        }

        burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        burst.Clear(true);
        burst.transform.localScale = Vector3.one;
        burst.gameObject.SetActive(false);
        activeBursts[index] = false;
    }
}
