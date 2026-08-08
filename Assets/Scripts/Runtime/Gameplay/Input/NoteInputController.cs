using UnityEngine;
using System;

public class NoteInputController : MonoBehaviour
{
    [SerializeField] private Camera gameplayCamera;

    public static event Action FailedInput;
    private void Awake()
    {
        if (gameplayCamera == null)
        {
            Debug.LogError("NoteInputController requires a gameplay camera.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                if (touch.phase != TouchPhase.Began)
                {
                    continue;
                }

                TryHitAtScreenPosition(touch.position);

                if (!enabled)
                {
                    return;
                }
            }

            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryHitAtScreenPosition(Input.mousePosition);
        }
    }

    private void TryHitAtScreenPosition(Vector2 screenPosition)
    {
        Vector3 worldPosition = gameplayCamera.ScreenToWorldPoint(screenPosition);
        Collider2D hit = Physics2D.OverlapPoint(worldPosition);
        if (hit == null)
        {
            FailedInput?.Invoke();
            return;
        }
        Note note = hit.GetComponent<Note>();

        if (note == null)
        {
            FailedInput?.Invoke();
            return;
        }

        note.TryHit();
    }
}