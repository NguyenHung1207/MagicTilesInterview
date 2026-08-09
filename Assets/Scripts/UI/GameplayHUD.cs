using TMPro;
using UnityEngine;
using System.Collections;

public class GameplayHUD : MonoBehaviour
{
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text comboText;

    [SerializeField] private TMP_Text judgementText;
    [SerializeField] private float judgementDisplayDuration = 0.45f;
    [SerializeField] private Color perfectColor = new Color(1f, 0.78f, 0.2f);
    [SerializeField] private Color greatColor = new Color(0.2f, 0.75f, 1f);

    [SerializeField] private Color goodColor = new Color(0.35f, 1f, 0.5f);
    [SerializeField] private float judgementPunchDuration = 0.14f;
    [SerializeField] private float comboPunchDuration = 0.14f;
    [SerializeField] private float scorePunchDuration = 0.1f;

    [SerializeField] private float judgementStartScale = 0.8f;
    [SerializeField] private float judgementPeakScale = 1.15f;

    [SerializeField] private float comboStartScale = 0.85f;
    [SerializeField] private float comboPeakScale = 1.2f;

    [SerializeField] private float scorePeakScale = 1.08f;
    [SerializeField] private GameplayController gameplayController;

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverFinalScoreText;

    [SerializeField] private GameObject winPanel;
    [SerializeField] private TMP_Text winFinalScoreText;
    private Coroutine comboCoroutine;
    private Vector3 comboBaseScale;
    private Coroutine scoreCoroutine;
    private Vector3 scoreBaseScale;
    private int displayedScore;

    private Coroutine hideJudgementCoroutine;
    
    private void OnEnable()
    {
        scoreManager.ScoreChanged += UpdateScore;
        Note.Judged += ShowJudgement;
        gameplayController.GameOver += ShowGameOver;
        gameplayController.GameWon += ShowWin;
    }

    private void OnDisable()
    {
        if (hideJudgementCoroutine != null)
        {
            StopCoroutine(hideJudgementCoroutine);
            hideJudgementCoroutine = null;
        }

        if (comboCoroutine != null)
        {
            StopCoroutine(comboCoroutine);
            comboCoroutine = null;
        }

        comboText.transform.localScale = comboBaseScale;

        if (scoreCoroutine != null)
        {
            StopCoroutine(scoreCoroutine);
            scoreCoroutine = null;
        }

        scoreText.transform.localScale = scoreBaseScale;

        judgementText.gameObject.SetActive(false);

        scoreManager.ScoreChanged -= UpdateScore;
        Note.Judged -= ShowJudgement;
        gameplayController.GameOver -= ShowGameOver;
        gameplayController.GameWon -= ShowWin;
    }

    private void UpdateScore(int score, int combo)
    {
        bool scoreIncreased = score > displayedScore;

        scoreText.text = score.ToString();

        if (scoreIncreased)
        {
            PlayScorePunch();
        }

        displayedScore = score;

        bool hasCombo = combo > 0;
        comboText.gameObject.SetActive(hasCombo);

        if (hasCombo)
        {
            comboText.text = $"x{combo}";
            PlayComboPunch();
        }
        else
        {
            if (comboCoroutine != null)
            {
                StopCoroutine(comboCoroutine);
                comboCoroutine = null;
            }

            comboText.transform.localScale = comboBaseScale;
        }
    }

    private void PlayComboPunch()
    {
        if (comboCoroutine != null)
        {
            StopCoroutine(comboCoroutine);
        }

        comboCoroutine = StartCoroutine(ComboPunchRoutine());
    }
    private void PlayScorePunch()
    {
        if (scoreCoroutine != null)
        {
            StopCoroutine(scoreCoroutine);
        }

        scoreCoroutine = StartCoroutine(ScorePunchRoutine());
    }

    private IEnumerator ScorePunchRoutine()
    {
        yield return PunchScale(scoreText.transform, scoreBaseScale, 1f, scorePeakScale, scorePunchDuration);
        scoreCoroutine = null;
    }

    private IEnumerator ComboPunchRoutine()
    {
        yield return PunchScale(comboText.transform, comboBaseScale, comboStartScale, comboPeakScale, comboPunchDuration);

        comboCoroutine = null;
    }

    private IEnumerator PunchScale(Transform target, Vector3 baseScale, float startMultiplier, float peakMultiplier, float duration)
    {
        float growDuration = duration * 0.4f;
        float settleDuration = duration - growDuration;

        Vector3 startScale = baseScale * startMultiplier;
        Vector3 peakScale = baseScale * peakMultiplier;

        target.localScale = startScale;

        float elapsed = 0f;

        while (elapsed < growDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / growDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            target.localScale =
                Vector3.Lerp(startScale, peakScale, t);

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < settleDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / settleDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            target.localScale =
                Vector3.Lerp(peakScale, baseScale, t);

            yield return null;
        }

        target.localScale = baseScale;
    }

    private void Awake()
    {
        comboBaseScale = comboText.transform.localScale;
        scoreBaseScale = scoreText.transform.localScale;
    }

    private void Start()
    {
        displayedScore = scoreManager.Score;

        UpdateScore(scoreManager.Score, scoreManager.Combo);

        gameOverPanel.SetActive(false);
        winPanel.SetActive(false);
    }

    private void ShowJudgement(HitJudgement judgement)
    {
        if (judgement == HitJudgement.None || judgement == HitJudgement.Miss)
        {
            return;
        }

        switch (judgement)
        {
            case HitJudgement.Perfect:
                judgementText.text = "PERFECT";
                judgementText.color = perfectColor;
                break;

            case HitJudgement.Great:
                judgementText.text = "GREAT";
                judgementText.color = greatColor;
                break;

            case HitJudgement.Good:
                judgementText.text = "GOOD";
                judgementText.color = goodColor;
                break;
        }

        judgementText.gameObject.SetActive(true);

        if (hideJudgementCoroutine != null)
        {
            StopCoroutine(hideJudgementCoroutine);
        }

        hideJudgementCoroutine = StartCoroutine(HideJudgementAfterDelay());
    }

    private IEnumerator HideJudgementAfterDelay()
    {
        yield return new WaitForSeconds(judgementDisplayDuration);

        judgementText.gameObject.SetActive(false);
        hideJudgementCoroutine = null;
    }

    private void ShowGameOver()
    {
        gameOverFinalScoreText.text = $"Score: {scoreManager.Score}";
        gameOverPanel.SetActive(true);
    }

    private void ShowWin()
    {
        winFinalScoreText.text = $"Score: {scoreManager.Score}";
        winPanel.SetActive(true);
    }

}
