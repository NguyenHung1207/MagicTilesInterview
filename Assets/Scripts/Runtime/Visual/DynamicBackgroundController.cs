using UnityEngine;
using UnityEngine.Video;

[DisallowMultipleComponent]
public class DynamicBackgroundController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer baseRenderer;
    [SerializeField] private SpriteRenderer[] decorativeRenderers;
    [SerializeField] private Transform[] animatedTransforms;
    [SerializeField] private float driftDistance = 0.2f;

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

    private void Awake()
    {
        EnsureVideoPlayer();
        EnsureInitialState();
    }

    public void ApplyTheme(BackgroundTheme theme)
    {
        if (theme == null)
        {
            Debug.LogWarning(
                "DynamicBackgroundController received no theme; keeping the fallback background.",
                this);
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

        if (clip == null)
        {
            videoPlayer.Stop();
            videoPlayer.clip = null;
            return;
        }

        if (videoPlayer.clip != clip)
        {
            videoPlayer.Stop();
            videoPlayer.clip = clip;
        }

        videoPlayer.Play();
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
