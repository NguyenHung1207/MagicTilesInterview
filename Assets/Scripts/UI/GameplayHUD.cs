using TMPro;
using UnityEngine;
using System.Collections;

public class GameplayHUD : MonoBehaviour
{
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text comboText;

    [SerializeField] private TMP_Text judgementText;
    [SerializeField] private Color perfectColor = new Color(1f, 0.78f, 0.2f);
    [SerializeField] private Color greatColor = new Color(0.2f, 0.75f, 1f);

    [SerializeField] private Color goodColor = new Color(0.35f, 1f, 0.5f);
    [SerializeField] private float judgementPunchDuration = 0.14f;
    [SerializeField] private float judgementDisplayDuration = 0.2f;
    [SerializeField] private float judgementFadeDuration = 0.12f;
    [SerializeField] private float comboPunchDuration = 0.14f;
    [SerializeField] private float scorePunchDuration = 0.1f;

    [SerializeField] private float judgementStartScale = 0.8f;
    [SerializeField] private float judgementPeakScale = 1.15f;

    [SerializeField] private float comboStartScale = 0.85f;
    [SerializeField] private float comboPeakScale = 1.2f;

    [SerializeField] private float scorePeakScale = 1.08f;
    private Coroutine comboCoroutine;
    private Vector3 comboBaseScale;
    private Coroutine scoreCoroutine;
    private Vector3 scoreBaseScale;
    private int displayedScore;
    private Coroutine judgementCoroutine;
    private Vector3 judgementBaseScale;
    
    private void OnEnable()
    {
        scoreManager.ScoreChanged += UpdateScore;
        Note.Judged += ShowJudgement;
    }

    private void OnDisable()
    {
        if (judgementCoroutine != null)
        {
            StopCoroutine(judgementCoroutine);
            judgementCoroutine = null;
        }

        judgementText.transform.localScale = judgementBaseScale;

        Color judgementColor = judgementText.color;
        judgementColor.a = 1f;
        judgementText.color = judgementColor;

        judgementText.gameObject.SetActive(false);

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

        scoreManager.ScoreChanged -= UpdateScore;
        Note.Judged -= ShowJudgement;
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
            target.localScale = Vector3.Lerp(startScale, peakScale, t);

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < settleDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / settleDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            target.localScale = Vector3.Lerp(peakScale, baseScale, t);

            yield return null;
        }

        target.localScale = baseScale;
    }

    private void Awake()
    {
        comboBaseScale = comboText.transform.localScale;
        scoreBaseScale = scoreText.transform.localScale;
        judgementBaseScale = judgementText.transform.localScale;
    }

    private void Start()
    {
        displayedScore = scoreManager.Score;

        UpdateScore(scoreManager.Score, scoreManager.Combo);
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

        if (judgementCoroutine != null)
        {
            StopCoroutine(judgementCoroutine);
        }

        judgementCoroutine = StartCoroutine(AnimateJudgement());
    }

    private IEnumerator AnimateJudgement()
    {
        judgementText.gameObject.SetActive(true);

        Color color = judgementText.color;
        color.a = 1f;
        judgementText.color = color;

        yield return PunchScale(judgementText.transform, judgementBaseScale, judgementStartScale, judgementPeakScale, judgementPunchDuration);

        yield return new WaitForSeconds(judgementDisplayDuration);

        float elapsed = 0f;
        Color startColor = judgementText.color;

        while (elapsed < judgementFadeDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / judgementFadeDuration);

            Color currentColor = startColor;
            currentColor.a = Mathf.Lerp(1f, 0f, t);

            judgementText.color = currentColor;

            yield return null;
        }

        judgementText.gameObject.SetActive(false);

        Color resetColor = judgementText.color;
        resetColor.a = 1f;
        judgementText.color = resetColor;

        judgementText.transform.localScale = judgementBaseScale;

        judgementCoroutine = null;
    }
}
