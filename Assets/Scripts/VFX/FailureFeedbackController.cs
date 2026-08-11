using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FailureFeedbackController : MonoBehaviour
{
    [SerializeField] private Image flashImage;
    [SerializeField] private Color missFlashColor = new Color(0.95f, 0.08f, 0.28f, 1f);
    [SerializeField] private Color failedInputFlashColor = new Color(0.82f, 0.1f, 0.58f, 1f);
    [SerializeField] private float missFlashDuration = 0.18f;
    [SerializeField] private float failedInputFlashDuration = 0.14f;
    [SerializeField, Range(0f, 1f)] private float missMaxAlpha = 0.34f;
    [SerializeField, Range(0f, 1f)] private float failedInputMaxAlpha = 0.42f;

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float shakeDuration = 0.14f;
    [SerializeField] private float missShakeStrength = 0.045f;
    [SerializeField] private float failedInputShakeStrength = 0.065f;

    private Coroutine shakeCoroutine;
    private Vector3 cameraBasePosition;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        if (flashImage == null)
        {
            Debug.LogError("FailureFeedbackController requires a flash image.", this);
            enabled = false;
            return;
        }

        if (cameraTransform != null)
        {
            cameraBasePosition = cameraTransform.position;
        }

        SetFlashAlpha(0f);
    }

    private void OnEnable()
    {
        Note.Judged += HandleJudgement;
        NoteInputController.FailedInput += PlayFailedInput;
    }

    private void OnDisable()
    {
        StopActiveFeedback();
        Note.Judged -= HandleJudgement;
        NoteInputController.FailedInput -= PlayFailedInput;
    }

    private void HandleJudgement(HitJudgement judgement)
    {
        if (judgement == HitJudgement.Miss)
        {
            PlayFailure(missFlashColor, missFlashDuration, missMaxAlpha, missShakeStrength);
        }
    }

    private void PlayFailedInput()
    {
        PlayFailure(
            failedInputFlashColor,
            failedInputFlashDuration,
            failedInputMaxAlpha,
            failedInputShakeStrength);
    }

    private void PlayFailure(Color color, float flashDuration, float maxAlpha, float shakeStrength)
    {
        SfxController.PlayMiss();
        StopActiveFeedback();
        flashCoroutine = StartCoroutine(Flash(color, flashDuration, maxAlpha));

        if (cameraTransform != null)
        {
            shakeCoroutine = StartCoroutine(ShakeCamera(shakeStrength));
        }
    }

    private IEnumerator ShakeCamera(float maximumStrength)
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / shakeDuration);
            float strength = Mathf.Lerp(maximumStrength, 0f, progress);
            Vector2 offset = Random.insideUnitCircle * strength;

            cameraTransform.position = cameraBasePosition + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        cameraTransform.position = cameraBasePosition;
        shakeCoroutine = null;
    }

    private IEnumerator Flash(Color color, float duration, float maxAlpha)
    {
        color.a = maxAlpha;
        flashImage.color = color;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            color.a = Mathf.Lerp(maxAlpha, 0f, progress);
            flashImage.color = color;
            yield return null;
        }

        SetFlashAlpha(0f);
        flashCoroutine = null;
    }

    private void StopActiveFeedback()
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

        if (flashImage != null)
        {
            SetFlashAlpha(0f);
        }
    }

    private void SetFlashAlpha(float alpha)
    {
        Color color = flashImage.color;
        color.a = alpha;
        flashImage.color = color;
    }
}
