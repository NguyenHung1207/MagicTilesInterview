using System;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class StartTileController : MonoBehaviour
{
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Collider2D tapCollider;
    [SerializeField] private Transform visualRoot;
    [SerializeField, Min(0f)] private float pulseAmount = 0.035f;
    [SerializeField, Min(0f)] private float pulseSpeed = 3.5f;

    private Action pressed;
    private Vector3 baseScale;
    private bool acceptingInput;

    public bool IsAcceptingInput => acceptingInput;

    private void Awake()
    {
        if (gameplayCamera == null || tapCollider == null || visualRoot == null)
        {
            Debug.LogError("StartTileController requires a gameplay camera, collider, and visual root.", this);
            enabled = false;
            return;
        }

        baseScale = visualRoot.localScale;
    }

    private void Update()
    {
        if (!acceptingInput)
        {
            return;
        }

        float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
        visualRoot.localScale = baseScale * pulse;

        if (Input.touchCount > 0)
        {
            for (int index = 0; index < Input.touchCount; index++)
            {
                Touch touch = Input.GetTouch(index);
                if (touch.phase != TouchPhase.Began || IsTouchOverUi(touch.fingerId))
                {
                    continue;
                }

                TryPress(touch.position);
                if (!acceptingInput)
                {
                    return;
                }
            }

            return;
        }

        if (Input.GetMouseButtonDown(0) && !IsTouchOverUi())
        {
            TryPress(Input.mousePosition);
        }
    }

    public void Show(Vector3 worldPosition, Action pressedCallback)
    {
        pressed = pressedCallback;
        transform.position = worldPosition;
        gameObject.SetActive(true);
        visualRoot.localScale = baseScale;
        tapCollider.enabled = true;
        acceptingInput = true;
    }

    public void Hide()
    {
        acceptingInput = false;
        pressed = null;
        if (tapCollider != null)
        {
            tapCollider.enabled = false;
        }

        if (visualRoot != null && baseScale != Vector3.zero)
        {
            visualRoot.localScale = baseScale;
        }

        gameObject.SetActive(false);
    }

    private void TryPress(Vector2 screenPosition)
    {
        Vector3 worldPosition = gameplayCamera.ScreenToWorldPoint(screenPosition);
        if (!tapCollider.OverlapPoint(worldPosition))
        {
            return;
        }

        acceptingInput = false;
        tapCollider.enabled = false;
        Action callback = pressed;
        pressed = null;
        callback?.Invoke();
    }

    private static bool IsTouchOverUi(int fingerId = -1)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return fingerId >= 0
            ? EventSystem.current.IsPointerOverGameObject(fingerId)
            : EventSystem.current.IsPointerOverGameObject();
    }
}
