using System.Collections;
using UnityEngine;

public class LaneFlashController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] laneFlashes;
    [SerializeField] private Color perfectColor = new Color(1f, 0.78f, 0.2f, 0.75f);
    [SerializeField] private Color greatColor = new Color(0.2f, 0.75f, 1f, 0.60f);
    [SerializeField] private Color goodColor = new Color(0.35f, 1f, 0.5f, 0.40f);
    [SerializeField] private float perfectDuration = 0.20f;
    [SerializeField] private float greatDuration = 0.16f;
    [SerializeField] private float goodDuration = 0.12f;

    private Coroutine[] flashCoroutines;
    private bool isInitialized;

    private void Awake()
    {
        InitializeFeedback();
    }

    private void InitializeFeedback()
    {
        if (isInitialized)
        {
            return;
        }

        if (laneFlashes == null || laneFlashes.Length == 0)
        {
            Debug.LogError("LaneFlashController requires lane flash renderers.", this);
            enabled = false;
            return;
        }

        flashCoroutines = new Coroutine[laneFlashes.Length];

        foreach (SpriteRenderer flash in laneFlashes)
        {
            if (flash == null)
            {
                Debug.LogError("LaneFlashController has a missing lane flash reference.", this);
                enabled = false;
                return;
            }

            Color color = flash.color;
            color.a = 0f;
            flash.color = color;

            flash.gameObject.SetActive(true);
        }

        isInitialized = true;
    }

    private void OnEnable()
    {
        InitializeFeedback();
        if (!isInitialized)
        {
            return;
        }

        Note.HitSucceeded += HandleHit;
    }

    private void OnDisable()
    {
        Note.HitSucceeded -= HandleHit;

        if (flashCoroutines == null)
        {
            return;
        }

        for (int i = 0; i < flashCoroutines.Length; i++)
        {
            if (flashCoroutines[i] != null)
            {
                StopCoroutine(flashCoroutines[i]);
                flashCoroutines[i] = null;
            }

            if (laneFlashes[i] != null)
            {
                Color color = laneFlashes[i].color;
                color.a = 0f;
                laneFlashes[i].color = color;
            }
        }
    }

    private void HandleHit(int laneIndex, HitJudgement judgement, Vector3 worldPosition)
    {
        InitializeFeedback();
        if (!isInitialized)
        {
            return;
        }

        if (laneIndex < 0 || laneIndex >= laneFlashes.Length)
        {
            return;
        }

        if (flashCoroutines[laneIndex] != null)
        {
            StopCoroutine(flashCoroutines[laneIndex]);
        }

        Color flashColor;
        float duration;

        switch (judgement)
        {
            case HitJudgement.Perfect:
                flashColor = perfectColor;
                duration = perfectDuration;
                break;

            case HitJudgement.Great:
                flashColor = greatColor;
                duration = greatDuration;
                break;

            case HitJudgement.Good:
                flashColor = goodColor;
                duration = goodDuration;
                break;

            default:
                return;
        }

        flashCoroutines[laneIndex] = StartCoroutine(FlashLane(laneIndex, flashColor, duration));
    }

    private IEnumerator FlashLane(int laneIndex, Color flashColor, float duration)
    {
        SpriteRenderer flash = laneFlashes[laneIndex];

        float startAlpha = flashColor.a;
        SetColor(flash, flashColor, startAlpha);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(startAlpha, 0f, t);

            SetColor(flash, flashColor, alpha);

            yield return null;
        }

        SetColor(flash, flashColor, 0f);
        flashCoroutines[laneIndex] = null;
    }

    private void SetColor(SpriteRenderer renderer, Color color, float alpha)
    {
        color.a = alpha;
        renderer.color = color;
    }
}
