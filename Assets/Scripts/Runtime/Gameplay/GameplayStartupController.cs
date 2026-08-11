using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameplayStartupController : MonoBehaviour
{
    private enum StartupState
    {
        PreparingBackground,
        WarmingUp,
        ReadyToStart,
        Playing
    }

    [Header("Gameplay")]
    [SerializeField] private DynamicBackgroundController backgroundController;
    [SerializeField] private SongConductor songConductor;
    [SerializeField] private NoteSpawner noteSpawner;
    [SerializeField] private NoteInputController inputController;
    [SerializeField] private PauseMenuController pauseMenuController;
    [SerializeField] private LaneView startLane;
    [SerializeField] private StartTileController startTile;

    [Header("Loading")]
    [SerializeField] private GameObject loadingPresentation;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField, Min(0f)] private float loadingPulseSpeed = 2.5f;
    [SerializeField, Range(0f, 1f)] private float loadingMinimumAlpha = 0.55f;

    private StartupState state;
    private Color loadingBaseColor;
    private bool backgroundReady;
    private bool warmupFailureReported;

    public bool IsPlaying => state == StartupState.Playing;
    public bool IsReadyToStart => state == StartupState.ReadyToStart;

    private void Awake()
    {
        if (backgroundController == null || songConductor == null || noteSpawner == null ||
            inputController == null || pauseMenuController == null || startLane == null ||
            startTile == null || loadingPresentation == null || loadingText == null)
        {
            Debug.LogError("GameplayStartupController requires all gameplay, lane, tile, and loading references.", this);
            enabled = false;
            return;
        }

        state = StartupState.PreparingBackground;
        noteSpawner.enabled = false;
        inputController.enabled = false;
        pauseMenuController.SetGameplayStarted(false);
        startTile.Hide();
        loadingBaseColor = loadingText.color;
        loadingPresentation.SetActive(true);
    }

    private void OnEnable()
    {
        if (backgroundController != null)
        {
            backgroundController.BackgroundReady += HandleBackgroundReady;
        }
    }

    private void Start()
    {
        backgroundReady = backgroundController.IsReady;

        if (!noteSpawner.Prewarm())
        {
            ReportWarmupFailureOnce("Gameplay note prewarm could not be completed.");
            return;
        }

        StartCoroutine(WaitForRequiredReadiness());

        if (backgroundController.IsReady)
        {
            HandleBackgroundReady();
        }
    }

    private void OnDisable()
    {
        if (backgroundController != null)
        {
            backgroundController.BackgroundReady -= HandleBackgroundReady;
        }
    }

    private void Update()
    {
        if (state == StartupState.ReadyToStart || state == StartupState.Playing)
        {
            return;
        }

        Color color = loadingBaseColor;
        float pulse = (Mathf.Sin(Time.unscaledTime * loadingPulseSpeed) + 1f) * 0.5f;
        color.a = Mathf.Lerp(loadingMinimumAlpha, loadingBaseColor.a, pulse);
        loadingText.color = color;
    }

    private void HandleBackgroundReady()
    {
        if (state != StartupState.PreparingBackground && state != StartupState.WarmingUp)
        {
            return;
        }

        backgroundReady = true;
        state = StartupState.WarmingUp;
    }

    private IEnumerator WaitForRequiredReadiness()
    {
        // Let the loading presentation render before requesting potentially expensive audio data.
        yield return null;

        while (state == StartupState.PreparingBackground || state == StartupState.WarmingUp)
        {
            if (songConductor.HasAudioLoadFailed)
            {
                ReportWarmupFailureOnce("Gameplay music could not be loaded.");
                yield break;
            }

            if (noteSpawner.HasChartFailed)
            {
                ReportWarmupFailureOnce("Gameplay chart initialization failed.");
                yield break;
            }

            if (songConductor.AudioLoadState == AudioDataLoadState.Unloaded)
            {
                songConductor.PrepareAudioData();
            }

            if (backgroundReady && songConductor.IsAudioReady && noteSpawner.IsReady)
            {
                EnterReadyToStart();
                yield break;
            }

            yield return null;
        }
    }

    private void EnterReadyToStart()
    {
        state = StartupState.ReadyToStart;
        loadingPresentation.SetActive(false);
        loadingText.color = loadingBaseColor;
        startTile.Show(startLane.HitPoint.position, StartGameplay);
    }

    private void ReportWarmupFailureOnce(string message)
    {
        if (warmupFailureReported)
        {
            return;
        }

        warmupFailureReported = true;
        Debug.LogError(message, this);
    }

    private void StartGameplay()
    {
        if (state != StartupState.ReadyToStart)
        {
            return;
        }

        if (!songConductor.StartSong())
        {
            Debug.LogError("Gameplay could not start because the configured song was unavailable or already started.", this);
            startTile.Show(startLane.HitPoint.position, StartGameplay);
            return;
        }

        state = StartupState.Playing;
        startTile.Hide();
        SfxController.PlayUIClick();
        noteSpawner.enabled = true;
        pauseMenuController.SetGameplayStarted(true);
        StartCoroutine(EnableGameplayInputNextFrame());
    }

    private IEnumerator EnableGameplayInputNextFrame()
    {
        yield return null;

        if (state == StartupState.Playing)
        {
            inputController.enabled = true;
        }
    }
}
