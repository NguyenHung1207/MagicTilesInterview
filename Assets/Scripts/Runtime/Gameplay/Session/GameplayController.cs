using UnityEngine;

public class GameplayController : MonoBehaviour
{
    [SerializeField] private SongConductor songConductor;
    [SerializeField] private NoteSpawner noteSpawner;
    [SerializeField] private NoteInputController inputController;
    private int successfulHitCount;
    public bool IsGameWon { get; private set; }

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

    private void Update()
    {
        if (IsGameOver || IsGameWon)
        {
            return;
        }

        if (successfulHitCount < noteSpawner.TotalNoteCount)
        {
            return;
        }

        if (!songConductor.IsSongFinished)
        {
            return;
        }

        WinGame();
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
            return;
        }

        if (judgement == HitJudgement.Perfect || judgement == HitJudgement.Cool || judgement == HitJudgement.Good)
        {
            successfulHitCount++;
        }
    }

    private void HandleFailedInput()
    {
        EndGame();
    }

    private void EndGame()
    {
        if (IsGameOver || IsGameWon)
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

    private void WinGame()
    {
        if (IsGameOver || IsGameWon)
        {
            return;
        }

        IsGameWon = true;
        inputController.enabled = false;
        noteSpawner.enabled = false;
        songConductor.StopSong();
    }
}
