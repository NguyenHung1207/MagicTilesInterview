using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public int Score { get; private set; }
    public int Combo { get; private set; }

    public void ApplyJudgement(HitJudgement judgement)
    {
        if (judgement == HitJudgement.Miss)
        {
            Combo = 0;
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

            case HitJudgement.Cool:
                points = 75;
                Combo = 0;
                break;

            case HitJudgement.Good:
                points = 50;
                Combo = 0;
                break;
        }

        Score += points;
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