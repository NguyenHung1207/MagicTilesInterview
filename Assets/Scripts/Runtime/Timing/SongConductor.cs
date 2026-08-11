using UnityEngine;

public class SongConductor : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private double startDelay = 0.5;
    private double songStartDspTime;

    private bool isSongScheduled;
    private bool isSongStopped;
    private bool isSongPaused;
    private bool audioLoadFailureReported;
    private double stoppedSongTime;
    private double pausedSongTime;

    public bool IsPaused => isSongPaused;
    public bool HasStarted => isSongScheduled && !isSongStopped;
    public bool IsAudioReady => musicSource != null
        && musicSource.clip != null
        && musicSource.clip.loadState == AudioDataLoadState.Loaded;
    public bool HasAudioLoadFailed => musicSource != null
        && musicSource.clip != null
        && musicSource.clip.loadState == AudioDataLoadState.Failed;
    public AudioDataLoadState AudioLoadState => musicSource != null && musicSource.clip != null
        ? musicSource.clip.loadState
        : AudioDataLoadState.Unloaded;

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

    public bool StartSong()
    {
        if (isSongScheduled)
        {
            return false;
        }

        if (musicSource.clip == null)
        {
            Debug.LogError("SongConductor requires an AudioClip.", this);
            return false;
        }

        if (!IsAudioReady)
        {
            Debug.LogError("SongConductor requires loaded audio data before StartSong.", this);
            return false;
        }

        // Schedule slightly ahead so the audio system can prepare playback.
        songStartDspTime = AudioSettings.dspTime + startDelay;
        musicSource.PlayScheduled(songStartDspTime);
        isSongScheduled = true;
        isSongStopped = false;
        return true;
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

    private void Awake()
    {
        if (musicSource == null)
        {
            Debug.LogError("SongConductor requires an AudioSource reference.", this);
            enabled = false;
        }
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
        audioLoadFailureReported = false;
    }

    public bool PrepareAudioData()
    {
        if (musicSource == null || musicSource.clip == null)
        {
            ReportAudioLoadFailureOnce("SongConductor cannot preload a missing AudioClip.");
            return false;
        }

        switch (musicSource.clip.loadState)
        {
            case AudioDataLoadState.Loaded:
            case AudioDataLoadState.Loading:
                return true;

            case AudioDataLoadState.Failed:
                ReportAudioLoadFailureOnce(
                    $"Audio data failed to load for '{musicSource.clip.name}'.");
                return false;

            case AudioDataLoadState.Unloaded:
                if (musicSource.clip.LoadAudioData())
                {
                    return true;
                }

                ReportAudioLoadFailureOnce(
                    $"Audio data could not be requested for '{musicSource.clip.name}'.");
                return false;

            default:
                return false;
        }
    }

    private void ReportAudioLoadFailureOnce(string message)
    {
        if (audioLoadFailureReported)
        {
            return;
        }

        audioLoadFailureReported = true;
        Debug.LogError(message, this);
    }
}
