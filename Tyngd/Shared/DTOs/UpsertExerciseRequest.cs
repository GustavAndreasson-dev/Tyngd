namespace Tyngd.Shared.DTOs;

/// <summary>Request för att spara/uppdatera en enskild övning under ett pågående pass.</summary>
public class UpsertExerciseRequest
{
    public Guid     SessionKey { get; set; }
    public int      PassId     { get; set; }
    public DateOnly Date       { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public int      ExerciseId { get; set; }
    public string   NoteText   { get; set; } = string.Empty;
    public decimal  Weight     { get; set; }
    public List<SaveSessionSetDto> Sets { get; set; } = new();
}
