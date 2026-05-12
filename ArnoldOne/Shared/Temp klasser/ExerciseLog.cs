namespace ArnoldOne.Shared.Temp_klasser;

public class ExerciseLog
{
    /// <summary>Unikt id för raden.</summary>
    public int ExerciseLogId { get; set; }

    /// <summary>FK till sessionen.</summary>
    public int SessionId { get; set; }

    /// <summary>FK till övningen.</summary>
    public int ExerciseId { get; set; }

    /// <summary>Anteckning för övningen under detta pass.</summary>
    public string? NoteText { get; set; }
}
