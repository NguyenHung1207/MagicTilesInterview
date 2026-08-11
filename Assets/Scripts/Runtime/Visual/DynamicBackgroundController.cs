using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;

[DisallowMultipleComponent]
public class DynamicBackgroundController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer baseRenderer;
    [SerializeField] private SpriteRenderer[] decorativeRenderers;
    [SerializeField] private Transform[] animatedTransforms;
    [SerializeField] private float driftDistance = 0.2f;
    [SerializeField, Min(1f)] private float preparationTimeout = 10f;

    private Vector3[] initialPositions;
    private Quaternion[] initialRotations;
    private Vector3[] initialScales;
    private float[] decorativeAlphas;

    private float driftSpeed;
    private float rotationSpeed;
    private float pulseSpeed;
    private float pulseAmount;
    private float animationTime;
    private bool initialStateCached;
    private bool hasTheme;
    private VideoPlayer videoPlayer;
    private Coroutine preparationTimeoutRoutine;
    private bool readinessResolved;
    private bool failureReported;

    public bool IsReady { get; private set; }
    public event Action BackgroundReady;

    private void Awake()
    {
        EnsureVideoPlayer();
        EnsureInitialState();
    }

    private void OnEnable()
    {
        EnsureVideoPlayer();
        videoPlayer.prepareCompleted += HandlePrepared;
        videoPlayer.frameReady += HandleFrameReady;
        videoPlayer.errorReceived += HandleVideoError;
    }

    private void OnDisable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= HandlePrepared;
            videoPlayer.frameReady -= HandleFrameReady;
            videoPlayer.errorReceived -= HandleVideoError;
        }

        StopPreparationTimeout();
    }

    public void ApplyTheme(BackgroundTheme theme)
    {
        if (theme == null)
        {
            Debug.LogWarning(
                "DynamicBackgroundController received no theme; keeping the fallback background.",
                this);
            EnsureVideoPlayer();
            ResetReadiness();
            videoPlayer.Stop();
            videoPlayer.clip = null;
            ResolveReadiness();
            return;
        }

        if (!EnsureInitialState())
        {
            return;
        }

        ApplyVideo(theme.VideoClip);

        Color baseColor = theme.VideoClip != null
            ? theme.VideoOverlayColor
            : Color.Lerp(Color.black, theme.PrimaryColor, 0.22f);
        baseRenderer.color = baseColor;

        for (int index = 0; index < decorativeRenderers.Length; index++)
        {
            Color color = index % 2 == 0
                ? theme.SecondaryColor
                : theme.AccentColor;
            color.a = decorativeAlphas[index];
            decorativeRenderers[index].color = color;
        }

        driftSpeed = theme.DriftSpeed;
        rotationSpeed = theme.RotationSpeed;
        pulseSpeed = theme.PulseSpeed;
        pulseAmount = theme.PulseAmount;
        animationTime = 0f;
        hasTheme = true;

        ResetTransforms();
    }

    private void ApplyVideo(VideoClip clip)
    {
        EnsureVideoPlayer();
        ResetReadiness();

        if (clip == null)
        {
            videoPlayer.Stop();
            videoPlayer.clip = null;
            ResolveReadiness();
            return;
        }

        if (videoPlayer.clip != clip)
        {
            videoPlayer.Stop();
            videoPlayer.clip = clip;
        }

        videoPlayer.sendFrameReadyEvents = true;
        videoPlayer.Prepare();
        preparationTimeoutRoutine = StartCoroutine(PreparationTimeout());
    }

    private void HandlePrepared(VideoPlayer preparedPlayer)
    {
        if (readinessResolved || preparedPlayer != videoPlayer)
        {
            return;
        }

        preparedPlayer.Play();
    }

    private void HandleFrameReady(VideoPlayer framePlayer, long frameIndex)
    {
        if (readinessResolved || framePlayer != videoPlayer)
        {
            return;
        }

        ResolveReadiness();
    }

    private void HandleVideoError(VideoPlayer failedPlayer, string message)
    {
        if (readinessResolved || failedPlayer != videoPlayer)
        {
            return;
        }

        ReportFailureOnce($"Gameplay background video could not be prepared: {message}");
        UseFallbackBackground();
    }

    private IEnumerator PreparationTimeout()
    {
        float elapsed = 0f;
        while (!readinessResolved && elapsed < preparationTimeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!readinessResolved)
        {
            preparationTimeoutRoutine = null;
            ReportFailureOnce(
                $"Gameplay background video was not visually ready within {preparationTimeout:F0} seconds; using the fallback background.");
            UseFallbackBackground();
        }
    }

    private void UseFallbackBackground()
    {
        videoPlayer.Stop();
        ResolveReadiness();
    }

    private void ResetReadiness()
    {
        StopPreparationTimeout();
        readinessResolved = false;
        failureReported = false;
        IsReady = false;
    }

    private void ResolveReadiness()
    {
        if (readinessResolved)
        {
            return;
        }

        readinessResolved = true;
        IsReady = true;
        StopPreparationTimeout();
        BackgroundReady?.Invoke();
    }

    private void ReportFailureOnce(string message)
    {
        if (failureReported)
        {
            return;
        }

        failureReported = true;
        Debug.LogWarning(message, this);
    }

    private void StopPreparationTimeout()
    {
        if (preparationTimeoutRoutine == null)
        {
            return;
        }

        StopCoroutine(preparationTimeoutRoutine);
        preparationTimeoutRoutine = null;
    }

    private void EnsureVideoPlayer()
    {
        if (videoPlayer != null)
        {
            return;
        }

        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = true;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.sendFrameReadyEvents = true;
        videoPlayer.skipOnDrop = true;
        videoPlayer.renderMode = VideoRenderMode.CameraFarPlane;
        videoPlayer.aspectRatio = VideoAspectRatio.FitOutside;
        videoPlayer.targetCamera = Camera.main;
        videoPlayer.targetCameraAlpha = 1f;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
    }

    private void Update()
    {
        if (!hasTheme)
        {
            return;
        }

        animationTime += Time.deltaTime;

        for (int index = 0; index < animatedTransforms.Length; index++)
        {
            Transform animatedTransform = animatedTransforms[index];
            float phase = index * 1.618f;
            float driftTime = animationTime * driftSpeed + phase;
            float driftScale = 1f + index * 0.08f;

            Vector3 offset = new(
                Mathf.Sin(driftTime) * driftDistance * driftScale,
                Mathf.Cos(driftTime * 0.83f) * driftDistance * driftScale,
                0f);
            animatedTransform.localPosition = initialPositions[index] + offset;

            float direction = index % 2 == 0 ? 1f : -1f;
            animatedTransform.localRotation = initialRotations[index]
                * Quaternion.Euler(0f, 0f, rotationSpeed * animationTime * direction);

            float pulse = 1f
                + Mathf.Sin(animationTime * pulseSpeed + phase) * pulseAmount;
            animatedTransform.localScale = initialScales[index] * pulse;
        }
    }

    private bool EnsureInitialState()
    {
        if (initialStateCached)
        {
            return true;
        }

        if (baseRenderer == null
            || decorativeRenderers == null
            || animatedTransforms == null
            || decorativeRenderers.Length == 0
            || decorativeRenderers.Length != animatedTransforms.Length)
        {
            Debug.LogError(
                "DynamicBackgroundController requires a base renderer and matching decorative renderer/transform arrays.",
                this);
            enabled = false;
            return false;
        }

        int count = animatedTransforms.Length;
        initialPositions = new Vector3[count];
        initialRotations = new Quaternion[count];
        initialScales = new Vector3[count];
        decorativeAlphas = new float[count];

        for (int index = 0; index < count; index++)
        {
            if (decorativeRenderers[index] == null || animatedTransforms[index] == null)
            {
                Debug.LogError(
                    "DynamicBackgroundController arrays cannot contain null entries.",
                    this);
                enabled = false;
                return false;
            }

            Transform animatedTransform = animatedTransforms[index];
            initialPositions[index] = animatedTransform.localPosition;
            initialRotations[index] = animatedTransform.localRotation;
            initialScales[index] = animatedTransform.localScale;
            decorativeAlphas[index] = decorativeRenderers[index].color.a;
        }

        initialStateCached = true;
        return true;
    }

    private void ResetTransforms()
    {
        for (int index = 0; index < animatedTransforms.Length; index++)
        {
            Transform animatedTransform = animatedTransforms[index];
            animatedTransform.localPosition = initialPositions[index];
            animatedTransform.localRotation = initialRotations[index];
            animatedTransform.localScale = initialScales[index];
        }
    }
}
