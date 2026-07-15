namespace ArnoldOne.Shared.DTOs;

/// <summary>Request för att spara ett avslutat träningspass.</summary>
public class SaveSessionRequest
{
    public Guid SessionKey { get; set; }
    public int PassId { get; set; }
    public DateOnly Date { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public bool CompleteSession { get; set; }
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
