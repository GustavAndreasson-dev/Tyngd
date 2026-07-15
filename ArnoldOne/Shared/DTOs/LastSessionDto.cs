namespace ArnoldOne.Shared.DTOs;

/// <summary>Senaste sparade data per övning för ett pass – används för att förfylla vikt, anteckningar och återställa sets.</summary>
public class LastSessionDto
{
    public Guid?                       SessionKey { get; set; }
    public DateOnly?                    Date      { get; set; }
    public DateTimeOffset?              StartedAtUtc { get; set; }
    public DateTimeOffset?              CompletedAtUtc { get; set; }
    public List<LastSessionExerciseDto> Exercises { get; set; } = new();
}

public class LastSessionExerciseDto
{
    public int ExerciseId { get; set; }
    public string NoteText { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public List<LastSessionSetDto> Sets { get; set; } = new();
}

public class LastSessionSetDto
{
    public int     SetIndex { get; set; }
    public int     Reps     { get; set; }
    public decimal Weight   { get; set; }
}
