using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameplayStartupController : MonoBehaviour
{
    private enum StartupState
    {
        LoadingBackground,
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

        state = StartupState.LoadingBackground;
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
        if (state != StartupState.LoadingBackground)
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
        if (state != StartupState.LoadingBackground)
        {
            return;
        }

        state = StartupState.ReadyToStart;
        loadingPresentation.SetActive(false);
        loadingText.color = loadingBaseColor;
        startTile.Show(startLane.HitPoint.position, StartGameplay);
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
