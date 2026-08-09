using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FailureFeedbackController : MonoBehaviour
{
    [SerializeField] private Image flashImage;
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.4f;

    private Coroutine flashCoroutine;

    public void PlayFailure()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(Flash());
    }

    private IEnumerator Flash()
    {
        Color color = flashImage.color;
        color.a = maxAlpha;
        flashImage.color = color;

        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / flashDuration);

            color.a = Mathf.Lerp(maxAlpha, 0f, t);
            flashImage.color = color;

            yield return null;
        }

        color.a = 0f;
        flashImage.color = color;

        flashCoroutine = null;
    }

    private void Awake()
    {
        if (flashImage == null)
        {
            return;
        }

        Color color = flashImage.color;
        color.a = 0f;
        flashImage.color = color;
    }

    private void OnEnable()
    {
        Note.Judged += HandleJudgement;
        NoteInputController.FailedInput += PlayFailure;
    }

    private void HandleJudgement(HitJudgement judgement)
    {
        if (judgement == HitJudgement.Miss)
        {
            PlayFailure();
        }
    }

    private void OnDisable()
    {
        Note.Judged -= HandleJudgement;
        NoteInputController.FailedInput -= PlayFailure;
    }
}