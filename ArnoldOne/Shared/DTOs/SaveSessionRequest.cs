namespace ArnoldOne.Shared.DTOs;

/// <summary>Request för att spara ett avslutat träningspass.</summary>
public class SaveSessionRequest
{
    public int PassId { get; set; }
    public DateOnly Date { get; set; }
    public List<SaveSessionExerciseDto> Exercises { get; set; } = new();
}

public class SaveSessionExerciseDto
{
    public int ExerciseId { get; set; }
    public string NoteText { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public List<SaveSessionSetDto> Sets { get; set; } = new();
}

public class SaveSessionSetDto
{
    public int     SetIndex { get; set; }
    public int     Reps     { get; set; }
    public bool    IsDone   { get; set; }
    public decimal Weight   { get; set; }
}
