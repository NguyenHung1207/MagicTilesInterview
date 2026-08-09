using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FailureFeedbackController : MonoBehaviour
{
    [SerializeField] private Image flashImage;
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.4f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float shakeDuration = 0.15f;
    [SerializeField] private float shakeStrength = 0.08f;

    private Coroutine shakeCoroutine;
    private Vector3 cameraBasePosition;
    private Coroutine flashCoroutine;

    public void PlayFailure()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);

            if (cameraTransform != null)
            {
                cameraTransform.position = cameraBasePosition;
            }
        }

        flashCoroutine = StartCoroutine(Flash());

        if (cameraTransform != null)
        {
            shakeCoroutine = StartCoroutine(ShakeCamera());
        }
    }

    private IEnumerator ShakeCamera()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsed / shakeDuration);

            float strength =
                Mathf.Lerp(shakeStrength, 0f, progress);

            Vector2 offset =
                UnityEngine.Random.insideUnitCircle * strength;

            cameraTransform.position =
                cameraBasePosition +
                new Vector3(offset.x, offset.y, 0f);

            yield return null;
        }

        cameraTransform.position = cameraBasePosition;
        shakeCoroutine = null;
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
        if (cameraTransform != null)
        {
            cameraBasePosition = cameraTransform.position;
        }

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
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        if (cameraTransform != null)
        {
            cameraTransform.position = cameraBasePosition;
        }

        Note.Judged -= HandleJudgement;
        NoteInputController.FailedInput -= PlayFailure;
    }
}