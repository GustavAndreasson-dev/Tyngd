namespace ArnoldOne.Shared.DTOs;

/// <summary>Request för att spara/uppdatera en enskild övning under ett pågående pass.</summary>
public class UpsertExerciseRequest
{
    public int      PassId     { get; set; }
    public DateOnly Date       { get; set; }
    public int      ExerciseId { get; set; }
    public string   NoteText   { get; set; } = string.Empty;
    public decimal  Weight     { get; set; }
    public List<SaveSessionSetDto> Sets { get; set; } = new();
}
