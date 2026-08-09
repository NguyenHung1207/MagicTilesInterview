using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;
    private bool isApplying;

    private void OnEnable()
    {
        rectTransform = transform as RectTransform;
        ApplySafeAreaIfChanged();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled || isApplying)
        {
            return;
        }

        ApplySafeAreaIfChanged();
    }

    private void ApplySafeAreaIfChanged()
    {
        int screenWidth = Screen.width;
        int screenHeight = Screen.height;

        if (rectTransform == null || screenWidth <= 0 || screenHeight <= 0)
        {
            return;
        }

        Rect safeArea = Screen.safeArea;
        if (screenWidth == lastScreenWidth
            && screenHeight == lastScreenHeight
            && safeArea.Equals(lastSafeArea))
        {
            return;
        }

        lastScreenWidth = screenWidth;
        lastScreenHeight = screenHeight;
        lastSafeArea = safeArea;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= screenWidth;
        anchorMin.y /= screenHeight;
        anchorMax.x /= screenWidth;
        anchorMax.y /= screenHeight;

        isApplying = true;
        try
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
        finally
        {
            isApplying = false;
        }
    }
}
