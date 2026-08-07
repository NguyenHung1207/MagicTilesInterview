using UnityEngine;

public class Note : MonoBehaviour
{
    private SongConductor songConductor;
    private Vector3 spawnPosition;
    private Vector3 hitPosition;
    private double spawnTime;
    private double hitTime;

    public void Initialize(SongConductor conductor, Vector3 startPosition, Vector3 targetPosition, double noteSpawnTime, double noteHitTime)
    {
        songConductor = conductor;
        spawnPosition = startPosition;
        hitPosition = targetPosition;
        spawnTime = songConductor.SongTime;
        hitTime = noteHitTime;
        transform.position = spawnPosition;
    }

    private void Update()
    {
        double currentSongTime = songConductor.SongTime;
        double progress = (currentSongTime - spawnTime) / (hitTime - spawnTime);

        float t = Mathf.Clamp01((float)progress);

        transform.position = Vector3.Lerp(spawnPosition, hitPosition, t);
    }
}