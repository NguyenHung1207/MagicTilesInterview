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
    [SerializeField] private GameplayController gameplayController;

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverFinalScoreText;

    [SerializeField] private GameObject winPanel;
    [SerializeField] private TMP_Text winFinalScoreText;

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

        judgementText.gameObject.SetActive(false);

        scoreManager.ScoreChanged -= UpdateScore;
        Note.Judged -= ShowJudgement;
        gameplayController.GameOver -= ShowGameOver;
        gameplayController.GameWon -= ShowWin;
    }

    private void UpdateScore(int score, int combo)
    {
        scoreText.text = $"Score: {score}";
        comboText.text = $"Combo: {combo}";
    }

    private void Start()
    {
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

        judgementText.text = judgement switch
        {
            HitJudgement.Perfect => "PERFECT",
            HitJudgement.Cool => "COOL",
            HitJudgement.Good => "GOOD",
            _ => string.Empty
        };

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