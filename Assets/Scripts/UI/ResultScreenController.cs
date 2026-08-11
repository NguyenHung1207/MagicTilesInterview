using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ResultScreenController : MonoBehaviour
{
    [Header("Session")]
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private GameplayController gameplayController;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private CanvasGroup gameOverCanvasGroup;
    [SerializeField] private RectTransform gameOverCard;
    [SerializeField] private TMP_Text gameOverFinalScoreText;

    [Header("Victory")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private CanvasGroup winCanvasGroup;
    [SerializeField] private RectTransform winCard;
    [SerializeField] private TMP_Text winFinalScoreText;

    [Header("Presentation")]
    [SerializeField, Min(0.01f)] private float entranceDuration = 0.34f;
    [SerializeField, Min(0.01f)] private float scoreCountDuration = 0.45f;
    [SerializeField] private float startScale = 0.88f;
    [SerializeField] private float overshootScale = 1.03f;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private Coroutine presentationCoroutine;
    private bool resultVisible;

    private void Awake()
    {
        HidePanel(gameOverPanel, gameOverCanvasGroup, gameOverCard);
        HidePanel(winPanel, winCanvasGroup, winCard);
    }

    private void OnEnable()
    {
        gameplayController.GameOver += ShowGameOver;
        gameplayController.GameWon += ShowWin;
    }

    private void OnDisable()
    {
        gameplayController.GameOver -= ShowGameOver;
        gameplayController.GameWon -= ShowWin;

        if (presentationCoroutine != null)
        {
            StopCoroutine(presentationCoroutine);
            presentationCoroutine = null;
        }
    }

    public void ReturnHome()
    {
        SetInteraction(gameOverCanvasGroup, false);
        SetInteraction(winCanvasGroup, false);
        SfxController.PlayUIClick();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void RestartGame()
    {
        SetInteraction(gameOverCanvasGroup, false);
        SetInteraction(winCanvasGroup, false);
        SfxController.PlayUIClick();
        gameplayController.RestartGame();
    }

    private void ShowGameOver()
    {
        if (ShowResult(gameOverPanel, gameOverCanvasGroup, gameOverCard, gameOverFinalScoreText))
        {
            SfxController.PlayGameOver();
        }
    }

    private void ShowWin()
    {
        if (ShowResult(winPanel, winCanvasGroup, winCard, winFinalScoreText))
        {
            SfxController.PlayVictory();
        }
    }

    private bool ShowResult(GameObject panel, CanvasGroup canvasGroup, RectTransform card, TMP_Text scoreText)
    {
        if (resultVisible)
        {
            return false;
        }

        resultVisible = true;
        panel.SetActive(true);
        canvasGroup.alpha = 0f;
        SetInteraction(canvasGroup, false);
        canvasGroup.blocksRaycasts = true;
        card.localScale = Vector3.one * startScale;
        scoreText.text = "0";

        presentationCoroutine = StartCoroutine(
            PresentResult(canvasGroup, card, scoreText, scoreManager.Score));
        return true;
    }

    private IEnumerator PresentResult(
        CanvasGroup canvasGroup,
        RectTransform card,
        TMP_Text scoreText,
        int finalScore)
    {
        float elapsed = 0f;
        float totalDuration = Mathf.Max(entranceDuration, scoreCountDuration);

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float entranceT = Mathf.Clamp01(elapsed / entranceDuration);
            canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, entranceT);
            card.localScale = Vector3.one * EvaluateCardScale(entranceT);

            float scoreT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / scoreCountDuration));
            scoreText.text = Mathf.RoundToInt(Mathf.Lerp(0f, finalScore, scoreT)).ToString();

            if (elapsed >= entranceDuration && !canvasGroup.interactable)
            {
                canvasGroup.interactable = true;
            }

            yield return null;
        }

        canvasGroup.alpha = 1f;
        card.localScale = Vector3.one;
        scoreText.text = finalScore.ToString();
        SetInteraction(canvasGroup, true);
        presentationCoroutine = null;
    }

    private float EvaluateCardScale(float t)
    {
        const float OvershootPoint = 0.72f;

        if (t < OvershootPoint)
        {
            float growT = Mathf.SmoothStep(0f, 1f, t / OvershootPoint);
            return Mathf.Lerp(startScale, overshootScale, growT);
        }

        float settleT = Mathf.SmoothStep(0f, 1f, (t - OvershootPoint) / (1f - OvershootPoint));
        return Mathf.Lerp(overshootScale, 1f, settleT);
    }

    private static void HidePanel(GameObject panel, CanvasGroup canvasGroup, RectTransform card)
    {
        canvasGroup.alpha = 0f;
        SetInteraction(canvasGroup, false);
        card.localScale = Vector3.one;
        panel.SetActive(false);
    }

    private static void SetInteraction(CanvasGroup canvasGroup, bool enabled)
    {
        canvasGroup.interactable = enabled;
        canvasGroup.blocksRaycasts = enabled;
    }
}
