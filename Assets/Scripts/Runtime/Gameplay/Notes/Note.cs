using UnityEngine;
using System;
using System.Collections;

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
    private float hitFadeDuration = 0.12f;

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

        if (noteRenderer != null)
        {
            StartCoroutine(FadeResolvedVisual());
        }
    }

    private IEnumerator FadeResolvedVisual()
    {
        Color color = noteRenderer.color;
        float startAlpha = color.a;

        float elapsed = 0f;

        while (elapsed < hitFadeDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / hitFadeDuration);

            color.a = Mathf.Lerp(startAlpha, resolvedAlpha,t);

            noteRenderer.color = color;

            yield return null;
        }

        color.a = resolvedAlpha;
        noteRenderer.color = color;
    }

    public void Cancel()
    {
        isResolved = true;
        Destroy(gameObject);
    }
}
