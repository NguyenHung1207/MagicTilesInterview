using UnityEngine;
using System;

public class ScoreManager : MonoBehaviour
{
    public int Score { get; private set; }
    public int Combo { get; private set; }

    public event Action<int, int> ScoreChanged;
    public void ApplyJudgement(HitJudgement judgement)
    {
        if (judgement == HitJudgement.Miss)
        {
            Combo = 0;
            ScoreChanged?.Invoke(Score, Combo);
            return;
        }

        if (judgement == HitJudgement.None)
        {
            return;
        }

        int points = 0;

        switch (judgement)
        {
            case HitJudgement.Perfect:
                points = 100;
                Combo++;
                break;

            case HitJudgement.Great:
                points = 75;
                Combo++;
                break;

            case HitJudgement.Good:
                points = 50;
                Combo++;
                break;

        }

        Score += points;
        ScoreChanged?.Invoke(Score, Combo);
    }

    private void OnEnable()
    {
        Note.Judged += ApplyJudgement;
    }

    private void OnDisable()
    {
        Note.Judged -= ApplyJudgement;
    }
}
