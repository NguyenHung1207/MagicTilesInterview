using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class PauseMenuController : MonoBehaviour
{
    [Header("Gameplay")]
    [SerializeField] private GameplayController gameplayController;
    [SerializeField] private SongConductor songConductor;
    [SerializeField] private NoteSpawner noteSpawner;
    [SerializeField] private NoteInputController inputController;
    [SerializeField] private ResultScreenController resultScreenController;

    [Header("UI")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private CanvasGroup pauseCanvasGroup;
    [SerializeField] private RectTransform pauseCard;

    [Header("Presentation")]
    [SerializeField, Min(0.01f)] private float entranceDuration = 0.24f;
    [SerializeField, Min(0.01f)] private float exitDuration = 0.14f;
    [SerializeField] private float startScale = 0.92f;
    [SerializeField] private float overshootScale = 1.02f;

    private Coroutine presentationCoroutine;
    private bool isPaused;
    private bool terminalStateReached;
    private bool gameplayStarted;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (gameplayController == null || songConductor == null || noteSpawner == null ||
            inputController == null || resultScreenController == null || pauseButton == null ||
            pausePanel == null || pauseCanvasGroup == null || pauseCard == null)
        {
            Debug.LogError("PauseMenuController requires all gameplay and UI references.", this);
            enabled = false;
            return;
        }

        HideImmediately();
        SetGameplayStarted(false);
    }

    private void OnEnable()
    {
        gameplayController.GameOver += HandleTerminalState;
        gameplayController.GameWon += HandleTerminalState;
    }

    private void OnDisable()
    {
        gameplayController.GameOver -= HandleTerminalState;
        gameplayController.GameWon -= HandleTerminalState;

        if (presentationCoroutine != null)
        {
            StopCoroutine(presentationCoroutine);
            presentationCoroutine = null;
        }
    }

    public void PauseGame()
    {
        if (!gameplayStarted || isPaused || terminalStateReached || gameplayController.IsGameOver || gameplayController.IsGameWon)
        {
            return;
        }

        SfxController.PlayUIClick();
        isPaused = true;
        pauseButton.interactable = false;
        inputController.enabled = false;
        noteSpawner.enabled = false;
        songConductor.PauseSong();

        pausePanel.SetActive(true);
        pauseCanvasGroup.alpha = 0f;
        pauseCanvasGroup.interactable = false;
        pauseCanvasGroup.blocksRaycasts = true;
        pauseCard.localScale = Vector3.one * startScale;
        StartPresentation(ShowPauseMenu());
    }

    public void ResumeGame()
    {
        if (!isPaused || terminalStateReached)
        {
            return;
        }

        SfxController.PlayUIClick();
        pauseCanvasGroup.interactable = false;
        StartPresentation(HidePauseMenuAndResume());
    }

    public void RestartGame()
    {
        if (!isPaused || terminalStateReached)
        {
            return;
        }

        PrepareSceneExit();
        resultScreenController.RestartGame();
    }

    public void ReturnHome()
    {
        if (!isPaused || terminalStateReached)
        {
            return;
        }

        PrepareSceneExit();
        resultScreenController.ReturnHome();
    }

    private IEnumerator ShowPauseMenu()
    {
        float elapsed = 0f;

        while (elapsed < entranceDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / entranceDuration);
            pauseCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, progress);
            pauseCard.localScale = Vector3.one * EvaluateCardScale(progress);
            yield return null;
        }

        pauseCanvasGroup.alpha = 1f;
        pauseCard.localScale = Vector3.one;
        pauseCanvasGroup.interactable = true;
        presentationCoroutine = null;
    }

    private IEnumerator HidePauseMenuAndResume()
    {
        float elapsed = 0f;
        float initialAlpha = pauseCanvasGroup.alpha;
        Vector3 initialScale = pauseCard.localScale;

        while (elapsed < exitDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / exitDuration));
            pauseCanvasGroup.alpha = Mathf.Lerp(initialAlpha, 0f, progress);
            pauseCard.localScale = Vector3.Lerp(initialScale, Vector3.one * startScale, progress);
            yield return null;
        }

        HideImmediately();
        songConductor.ResumeSong();
        noteSpawner.enabled = true;
        inputController.enabled = true;
        pauseButton.interactable = true;
        isPaused = false;
        presentationCoroutine = null;
    }

    private void HandleTerminalState()
    {
        terminalStateReached = true;
        SetGameplayStarted(false);
        isPaused = false;
        HideImmediately();
    }

    public void SetGameplayStarted(bool started)
    {
        gameplayStarted = started && !terminalStateReached;
        pauseButton.gameObject.SetActive(gameplayStarted);
        pauseButton.interactable = gameplayStarted && !isPaused;
    }

    private void PrepareSceneExit()
    {
        pauseCanvasGroup.interactable = false;
        pauseCanvasGroup.blocksRaycasts = true;
        pauseButton.interactable = false;
        songConductor.StopSong();
    }

    private void StartPresentation(IEnumerator routine)
    {
        if (presentationCoroutine != null)
        {
            StopCoroutine(presentationCoroutine);
        }

        presentationCoroutine = StartCoroutine(routine);
    }

    private void HideImmediately()
    {
        pauseCanvasGroup.alpha = 0f;
        pauseCanvasGroup.interactable = false;
        pauseCanvasGroup.blocksRaycasts = false;
        pauseCard.localScale = Vector3.one;
        pausePanel.SetActive(false);
    }

    private float EvaluateCardScale(float progress)
    {
        const float OvershootPoint = 0.72f;

        if (progress < OvershootPoint)
        {
            float grow = Mathf.SmoothStep(0f, 1f, progress / OvershootPoint);
            return Mathf.Lerp(startScale, overshootScale, grow);
        }

        float settle = Mathf.SmoothStep(0f, 1f, (progress - OvershootPoint) / (1f - OvershootPoint));
        return Mathf.Lerp(overshootScale, 1f, settle);
    }
}
