using UnityEngine;

public class NoteInputController : MonoBehaviour
{
    [SerializeField] private Camera gameplayCamera;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 screenPosition = Input.mousePosition;
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
}