using UnityEngine;

public class SongConductor : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private double startDelay = 0.5;
    private double songStartDspTime;

    private bool isSongScheduled;
    private bool isSongStopped;
    private double stoppedSongTime;

    public double SongTime
    {
        get
        {
            if (!isSongScheduled)
            {
                return 0.0;
            }

            if (isSongStopped)
            {
                return stoppedSongTime;
            }

            return AudioSettings.dspTime - songStartDspTime;
        }
    }
    private void StartSong()
    {
        // Schedule slightly ahead so the audio system can prepare playback.
        songStartDspTime = AudioSettings.dspTime + startDelay;
        musicSource.PlayScheduled(songStartDspTime);
        isSongScheduled = true;
    }

    public void StopSong()
    {
        if (!isSongScheduled || isSongStopped)
        {
            return;
        }
        stoppedSongTime = SongTime;
        isSongStopped = true;
        musicSource.Stop();
    }

    void Start()
    {
        if (musicSource == null)
        {
            Debug.LogError("SongConductor requires an AudioSource reference.", this);
            return;
        }
        StartSong();
    }

    void Update()
    {

    }
}
