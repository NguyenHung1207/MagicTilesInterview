using System.Collections;
using UnityEngine;

public class LaneFlashController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] laneFlashes;
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private float flashAlpha = 0.55f;

    private Coroutine[] flashCoroutines;

    private void Awake()
    {
        flashCoroutines = new Coroutine[laneFlashes.Length];

        foreach (SpriteRenderer flash in laneFlashes)
        {
            SetAlpha(flash, 0f);
            flash.gameObject.SetActive(true);
        }
    }

    private void OnEnable()
    {
        Note.HitSucceeded += HandleHit;
    }

    private void OnDisable()
    {
        Note.HitSucceeded -= HandleHit;
    }

    private void HandleHit(int laneIndex, HitJudgement judgement)
    {
        if (laneIndex < 0 || laneIndex >= laneFlashes.Length)
        {
            return;
        }

        if (flashCoroutines[laneIndex] != null)
        {
            StopCoroutine(flashCoroutines[laneIndex]);
        }

        flashCoroutines[laneIndex] =
            StartCoroutine(FlashLane(laneIndex));
    }

    private IEnumerator FlashLane(int laneIndex)
    {
        SpriteRenderer flash = laneFlashes[laneIndex];

        SetAlpha(flash, flashAlpha);

        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / flashDuration;
            SetAlpha(flash, Mathf.Lerp(flashAlpha, 0f, t));

            yield return null;
        }

        SetAlpha(flash, 0f);
        flashCoroutines[laneIndex] = null;
    }

    private void SetAlpha(SpriteRenderer renderer, float alpha)
    {
        Color color = renderer.color;
        color.a = alpha;
        renderer.color = color;
    }
}