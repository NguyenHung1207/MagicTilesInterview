using TMPro;
using UnityEngine;

public class GameplayHUD : MonoBehaviour
{
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text comboText;
    
    private void OnEnable()
    {
        scoreManager.ScoreChanged += UpdateScore;
    }

    private void OnDisable()
    {
        scoreManager.ScoreChanged -= UpdateScore;
    }

    private void UpdateScore(int score, int combo)
    {
        scoreText.text = $"Score: {score}";
        comboText.text = $"Combo: {combo}";
    }

    private void Start()
    {
        UpdateScore(scoreManager.Score, scoreManager.Combo);
    }
    
}