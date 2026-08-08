using UnityEngine;

public class HitBurstController : MonoBehaviour
{
    [SerializeField] private ParticleSystem hitBurstPrefab;

    [SerializeField] private Color perfectColor;
    [SerializeField] private Color greatColor;
    [SerializeField] private Color goodColor;

    private void OnEnable()
    {
        Note.HitSucceeded += HandleHit;
    }

    private void OnDisable()
    {
        Note.HitSucceeded -= HandleHit;
    }

    private void HandleHit(int laneIndex, HitJudgement judgement, Vector3 worldPosition)
    {
        Color color;
        int particleCount;
        float scale;

        switch (judgement)
        {
            case HitJudgement.Perfect:
                color = perfectColor;
                particleCount = 18;
                scale = 1.2f;
                break;

            case HitJudgement.Great:
                color = greatColor;
                particleCount = 12;
                scale = 1f;
                break;

            case HitJudgement.Good:
                color = goodColor;
                particleCount = 8;
                scale = 0.8f;
                break;

            default:
                return;
        }

        ParticleSystem burst = Instantiate(
            hitBurstPrefab,
            worldPosition,
            Quaternion.identity);

        ParticleSystem.MainModule main = burst.main;
        main.startColor = color;

        burst.transform.localScale = Vector3.one * scale;
        burst.Emit(particleCount);

        Destroy(burst.gameObject, 1f);
    }

}