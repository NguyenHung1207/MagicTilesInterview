using UnityEngine;

public class SongConductor : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private double startDelay = 0.5;
    private double songStartDspTime;

    private bool isSongScheduled;
    private bool isSongStopped;
    private bool isSongPaused;
    private double stoppedSongTime;
    private double pausedSongTime;

    public bool IsPaused => isSongPaused;

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

            if (isSongPaused)
            {
                return pausedSongTime;
            }

            return AudioSettings.dspTime - songStartDspTime;
        }
    }

    public void PauseSong()
    {
        if (!isSongScheduled || isSongStopped || isSongPaused)
        {
            return;
        }

        pausedSongTime = SongTime;
        isSongPaused = true;
        musicSource.Pause();
    }

    public void ResumeSong()
    {
        if (!isSongScheduled || isSongStopped || !isSongPaused)
        {
            return;
        }

        songStartDspTime = AudioSettings.dspTime - pausedSongTime;
        isSongPaused = false;
        musicSource.UnPause();
    }
    private void StartSong()
    {
        if (musicSource.clip == null)
        {
            Debug.LogError("SongConductor requires an AudioClip.", this);
            return;
        }

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
        isSongPaused = false;
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

    public bool IsSongFinished
    {
        get
        {
            if (!isSongScheduled || isSongStopped || isSongPaused || musicSource.clip == null)
            {
                return false;
            }

            return SongTime >= musicSource.clip.length;
        }
    }

    public void SetSong(AudioClip clip)
    {
        musicSource.clip = clip;
    }
}
