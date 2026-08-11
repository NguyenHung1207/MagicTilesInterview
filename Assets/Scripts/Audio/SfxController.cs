using System.Collections;
using UnityEngine;

public sealed class SfxController : MonoBehaviour
{
    public static SfxController Instance { get; private set; }

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip missClip;
    [SerializeField] private AudioClip uiClickClip;
    [SerializeField] private AudioClip victoryClip;
    [SerializeField] private AudioClip gameOverClip;

    [SerializeField, Range(0f, 1f)] private float missVolume = 0.35f;
    [SerializeField, Range(0f, 1f)] private float uiClickVolume = 0.25f;
    [SerializeField, Range(0f, 1f)] private float victoryVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float gameOverVolume = 0.45f;
    [SerializeField, Min(0f)] private float gameOverDelay = 0.16f;

    private Coroutine delayedGameOverCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null || missClip == null || uiClickClip == null || victoryClip == null || gameOverClip == null)
        {
            Debug.LogError("SfxController requires an AudioSource and all four production SFX clips.", this);
            enabled = false;
            return;
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static void PlayMiss()
    {
        if (TryGetActiveInstance(out SfxController instance))
        {
            instance.audioSource.PlayOneShot(instance.missClip, instance.missVolume);
        }
    }

    public static void PlayUIClick()
    {
        if (TryGetActiveInstance(out SfxController instance))
        {
            instance.audioSource.PlayOneShot(instance.uiClickClip, instance.uiClickVolume);
        }
    }

    public static void PlayVictory()
    {
        if (TryGetActiveInstance(out SfxController instance))
        {
            instance.audioSource.PlayOneShot(instance.victoryClip, instance.victoryVolume);
        }
    }

    public static void PlayGameOver()
    {
        if (!TryGetActiveInstance(out SfxController instance) || instance.delayedGameOverCoroutine != null)
        {
            return;
        }

        instance.delayedGameOverCoroutine = instance.StartCoroutine(instance.PlayGameOverDelayed());
    }

    private IEnumerator PlayGameOverDelayed()
    {
        if (gameOverDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(gameOverDelay);
        }

        audioSource.PlayOneShot(gameOverClip, gameOverVolume);
        delayedGameOverCoroutine = null;
    }

    private static bool TryGetActiveInstance(out SfxController instance)
    {
        instance = Instance;

        if (instance != null && instance.enabled)
        {
            return true;
        }

        Debug.LogError("No active SfxController is available for playback.");
        return false;
    }
}
