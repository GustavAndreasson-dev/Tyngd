namespace ArnoldOne.Shared.TrainingPrograms;

/// <summary>
/// En genomförd träningssession för ett specifikt pass.
/// Ersätter nyckeln "pass-{SplitId}-{PassId}" i LocalStorage.
/// En rad per gång användaren kör ett pass.
/// </summary>
public class WorkoutSession
{
    /// <summary>Unikt id för sessionen.</summary>
    public int Id { get; set; }

    /// <summary>FK till användaren (IdentityUser GUID).</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>FK till splitten.</summary>
    public int SplitId { get; set; }

    /// <summary>FK till passet.</summary>
    public int PassId { get; set; }

    /// <summary>Navigeringsegenskap till passmallen.</summary>
    public TrainingPassTemplate Pass { get; set; } = new();

    /// <summary>Datum då passet genomfördes.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Loggade övningar i sessionen.</summary>
    public List<WorkoutExerciseLog> ExerciseLogs { get; set; } = new();
}
