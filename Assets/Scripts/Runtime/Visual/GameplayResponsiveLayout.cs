using UnityEngine;

[DisallowMultipleComponent]
public class GameplayResponsiveLayout : MonoBehaviour
{
    private const int LaneCount = 4;
    private const int SeparatorCount = 3;
    private const float BackgroundOverscan = 1.01f;

    [Header("Camera Width Lock")]
    [SerializeField] private Camera targetCamera;
    [SerializeField, Min(1f)] private float referenceWidth = 1080f;
    [SerializeField, Min(1f)] private float referenceHeight = 1920f;
    [SerializeField, Min(0.01f)] private float referenceOrthographicSize = 5f;

    [Header("Lane Travel Points")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform[] hitPoints;
    [SerializeField] private float spawnViewportY = 1.05f;
    [SerializeField] private float hitViewportY = 0.10f;

    [Header("Full-Screen World Visuals")]
    [SerializeField] private SpriteRenderer backgroundBaseRenderer;
    [SerializeField] private SpriteRenderer[] laneFlashRenderers;
    [SerializeField] private SpriteRenderer[] separatorRenderers;

    private void Awake()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        float referenceAspect = referenceWidth / referenceHeight;
        float referenceHalfWidth = referenceOrthographicSize * referenceAspect;
        float currentAspect = (float)Screen.width / Screen.height;

        targetCamera.orthographicSize = Mathf.Max(
            referenceOrthographicSize,
            referenceHalfWidth / currentAspect);

        float spawnWorldY = CameraViewportYToWorld(spawnViewportY);
        float hitWorldY = CameraViewportYToWorld(hitViewportY);

        SetWorldY(spawnPoints, spawnWorldY);
        SetWorldY(hitPoints, hitWorldY);

        float visibleHeight = targetCamera.orthographicSize * 2f;
        float visibleWidth = visibleHeight * targetCamera.aspect;

        SetRendererWorldSize(
            backgroundBaseRenderer,
            visibleWidth * BackgroundOverscan,
            visibleHeight * BackgroundOverscan);
        SetRenderersWorldHeight(laneFlashRenderers, visibleHeight);
        SetRenderersWorldHeight(separatorRenderers, visibleHeight);
    }

    private float CameraViewportYToWorld(float viewportY)
    {
        return targetCamera.transform.position.y
            + (viewportY - 0.5f) * 2f * targetCamera.orthographicSize;
    }

    private static void SetWorldY(Transform[] points, float worldY)
    {
        for (int index = 0; index < points.Length; index++)
        {
            Transform point = points[index];
            Vector3 position = point.position;
            position.y = worldY;
            point.position = position;
        }
    }

    private static void SetRenderersWorldHeight(
        SpriteRenderer[] renderers,
        float worldHeight)
    {
        for (int index = 0; index < renderers.Length; index++)
        {
            SpriteRenderer renderer = renderers[index];
            Transform rendererTransform = renderer.transform;
            Vector3 localScale = rendererTransform.localScale;
            float parentScaleY = GetParentWorldScale(rendererTransform, false);

            localScale.y = worldHeight
                / (renderer.sprite.bounds.size.y * parentScaleY);
            rendererTransform.localScale = localScale;
        }
    }

    private static void SetRendererWorldSize(
        SpriteRenderer renderer,
        float worldWidth,
        float worldHeight)
    {
        Transform rendererTransform = renderer.transform;
        Vector3 localScale = rendererTransform.localScale;
        float parentScaleX = GetParentWorldScale(rendererTransform, true);
        float parentScaleY = GetParentWorldScale(rendererTransform, false);
        Vector3 spriteSize = renderer.sprite.bounds.size;

        localScale.x = worldWidth / (spriteSize.x * parentScaleX);
        localScale.y = worldHeight / (spriteSize.y * parentScaleY);
        rendererTransform.localScale = localScale;
    }

    private static float GetParentWorldScale(Transform child, bool horizontal)
    {
        if (child.parent == null)
        {
            return 1f;
        }

        float scale = horizontal
            ? Mathf.Abs(child.parent.lossyScale.x)
            : Mathf.Abs(child.parent.lossyScale.y);
        return Mathf.Max(scale, Mathf.Epsilon);
    }

    private bool ValidateReferences()
    {
        if (targetCamera == null || !targetCamera.orthographic)
        {
            Debug.LogError(
                "GameplayResponsiveLayout requires an orthographic target camera.",
                this);
            return false;
        }

        if (referenceWidth <= 0f
            || referenceHeight <= 0f
            || referenceOrthographicSize <= 0f
            || Screen.height <= 0)
        {
            Debug.LogError(
                "GameplayResponsiveLayout requires positive reference and screen dimensions.",
                this);
            return false;
        }

        if (!ValidateTransforms(spawnPoints, LaneCount)
            || !ValidateTransforms(hitPoints, LaneCount))
        {
            Debug.LogError(
                "GameplayResponsiveLayout requires exactly four non-null spawn and hit points.",
                this);
            return false;
        }

        if (!ValidateRenderer(backgroundBaseRenderer)
            || !ValidateRenderers(laneFlashRenderers, LaneCount)
            || !ValidateRenderers(separatorRenderers, SeparatorCount))
        {
            Debug.LogError(
                "GameplayResponsiveLayout requires a valid background, four lane flashes, and three separators.",
                this);
            return false;
        }

        return true;
    }

    private static bool ValidateTransforms(Transform[] transforms, int count)
    {
        if (transforms == null || transforms.Length != count)
        {
            return false;
        }

        for (int index = 0; index < transforms.Length; index++)
        {
            if (transforms[index] == null)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ValidateRenderers(SpriteRenderer[] renderers, int count)
    {
        if (renderers == null || renderers.Length != count)
        {
            return false;
        }

        for (int index = 0; index < renderers.Length; index++)
        {
            if (!ValidateRenderer(renderers[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ValidateRenderer(SpriteRenderer renderer)
    {
        return renderer != null
            && renderer.sprite != null
            && renderer.sprite.bounds.size.x > Mathf.Epsilon
            && renderer.sprite.bounds.size.y > Mathf.Epsilon;
    }
}
