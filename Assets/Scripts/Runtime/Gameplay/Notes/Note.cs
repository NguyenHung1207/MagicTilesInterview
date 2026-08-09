using UnityEngine;
using System;

public class Note : MonoBehaviour
{
    private SongConductor songConductor;
    private Vector3 spawnPosition;
    private Vector3 hitPosition;
    private double spawnTime;
    private double hitTime;
    private int laneIndex;
    public static event Action<int, HitJudgement, Vector3> HitSucceeded;
    private const double MissDelay = 0.15;
    private bool isResolved;

    [SerializeField] private SpriteRenderer noteRenderer;
    [SerializeField] private Collider2D hitCollider;

    [SerializeField, Range(0f, 1f)]
    private float resolvedAlpha = 0.25f;

    [SerializeField]
    private float resolvedCleanupProgress = 1.2f;
    public static event Action<HitJudgement> Judged;
    public void Initialize(SongConductor conductor, Vector3 startPosition, Vector3 targetPosition, double noteSpawnTime, double noteHitTime, int laneIndex)
    {
        songConductor = conductor;
        spawnPosition = startPosition;
        hitPosition = targetPosition;
        spawnTime = noteSpawnTime;
        hitTime = noteHitTime;
        transform.position = spawnPosition;
        this.laneIndex = laneIndex;
    }

    private void Update()
    {
        double currentSongTime = songConductor.SongTime;
        double progress = (currentSongTime - spawnTime) / (hitTime - spawnTime);

        transform.position = Vector3.LerpUnclamped(spawnPosition, hitPosition, (float)progress);

        if (isResolved)
        {
            if (progress >= resolvedCleanupProgress)
            {
                Destroy(gameObject);
            }

            return;
        }

        if (currentSongTime > hitTime + MissDelay)
        {
            isResolved = true;
            Debug.Log("Miss", this);
            Judged?.Invoke(HitJudgement.Miss);
            Destroy(gameObject);
        }
    }

    public HitJudgement TryHit()
    {
        if (isResolved)
        {
            return HitJudgement.None;
        }
        HitJudgement judgement = HitPositionJudge.Evaluate(transform.position.y, hitPosition.y);
        isResolved = true;

        SetResolvedVisual();

        Debug.Log(judgement, this);
        Judged?.Invoke(judgement);
        HitSucceeded?.Invoke(laneIndex, judgement, transform.position);

        return judgement;
    }

    private void SetResolvedVisual()
    {
        if (hitCollider != null)
        {
            hitCollider.enabled = false;
        }

        if (noteRenderer == null)
        {
            return;
        }

        Color color = noteRenderer.color;
        color.a = resolvedAlpha;
        noteRenderer.color = color;
    }

    public void Cancel()
    {
        isResolved = true;
        Destroy(gameObject);
    }
}
