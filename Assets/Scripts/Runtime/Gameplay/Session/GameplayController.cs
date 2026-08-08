using UnityEngine;

public class GameplayController : MonoBehaviour
{
    [SerializeField] private SongConductor songConductor;
    [SerializeField] private NoteSpawner noteSpawner;
    [SerializeField] private NoteInputController inputController;

    public bool IsGameOver { get; private set; }

    private void Awake()
    {
        if (songConductor != null && noteSpawner != null && inputController != null)
        {
            return;
        }

        Debug.LogError(
            "GameplayController requires SongConductor, NoteSpawner, and NoteInputController references.",
            this);
        enabled = false;
    }

    private void OnEnable()
    {
        Note.Judged += HandleJudgement;
        NoteInputController.FailedInput += HandleFailedInput;
    }

    private void OnDisable()
    {
        Note.Judged -= HandleJudgement;
        NoteInputController.FailedInput -= HandleFailedInput;
    }

    private void HandleJudgement(HitJudgement judgement)
    {
        if (judgement == HitJudgement.Miss)
        {
            EndGame();
        }
    }

    private void HandleFailedInput()
    {
        EndGame();
    }

    private void EndGame()
    {
        if (IsGameOver)
        {
            return;
        }

        IsGameOver = true;
        inputController.enabled = false;
        noteSpawner.enabled = false;

        songConductor.StopSong();
        CancelActiveNotes();
        Debug.Log("Game Over", this);
    }

    private static void CancelActiveNotes()
    {
        Note[] activeNotes = FindObjectsByType<Note>(FindObjectsSortMode.None);
        foreach (Note note in activeNotes)
        {
            note.Cancel();
        }
    }
}
