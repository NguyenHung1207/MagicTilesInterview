using UnityEngine;
using System;

public class Note : MonoBehaviour
{
    private SongConductor songConductor;
    private Vector3 spawnPosition;
    private Vector3 hitPosition;
    private double spawnTime;
    private double hitTime;

    private const double MissDelay = 0.15;
    private bool isResolved;
    public static event Action<HitJudgement> Judged;
    public void Initialize(SongConductor conductor, Vector3 startPosition, Vector3 targetPosition, double noteSpawnTime, double noteHitTime)
    {
        songConductor = conductor;
        spawnPosition = startPosition;
        hitPosition = targetPosition;
        spawnTime = noteSpawnTime;
        hitTime = noteHitTime;
        transform.position = spawnPosition;
    }

    private void Update()
    {
        double currentSongTime = songConductor.SongTime;
        double progress = (currentSongTime - spawnTime) / (hitTime - spawnTime);

        float t = Mathf.Clamp01((float)progress);

        transform.position = Vector3.Lerp(spawnPosition, hitPosition, t);

        if (!isResolved && currentSongTime > hitTime + MissDelay)
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
        Debug.Log(judgement, this);
        Judged?.Invoke(judgement);
        Destroy(gameObject);
        return judgement;
    }

    public void Cancel()
    {
        if (isResolved)
        {
            return;
        }

        isResolved = true;
        Destroy(gameObject);
    }
}
