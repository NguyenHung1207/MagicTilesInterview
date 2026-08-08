using UnityEngine;

public class NoteInputController : MonoBehaviour
{
    [SerializeField] private Camera gameplayCamera;

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
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                TryHitAtScreenPosition(touch.position);
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
            return;
        }
        Note note = hit.GetComponent<Note>();

        if (note == null)
        {
            return;
        }

        note.TryHit();
    }
}