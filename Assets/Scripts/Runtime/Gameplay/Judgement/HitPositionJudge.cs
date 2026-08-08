using UnityEngine;

public static class HitPositionJudge
{
    public const float PerfectDistance = 0.35f;
    public const float GreatDistance = 0.9f;

    public static HitJudgement Evaluate(float noteY, float hitY)
    {
        float distance = Mathf.Abs(noteY - hitY);
        if (distance <= PerfectDistance)
        {
            return HitJudgement.Perfect;
        }
        if (distance <= GreatDistance)
        {
            return HitJudgement.Great;
        }
        return HitJudgement.Good;
    }
}
