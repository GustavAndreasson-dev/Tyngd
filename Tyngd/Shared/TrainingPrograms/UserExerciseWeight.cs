namespace Tyngd.Shared.TrainingPrograms;

/// <summary>
/// En användares aktuella arbetsvikt för en övning.
/// Ersätter "exercise-weights" i LocalStorage.
/// En rad per användare och övning.
/// </summary>
public class UserExerciseWeight
{
    /// <summary>Unikt id för raden.</summary>
    public int Id { get; set; }

    /// <summary>FK till användaren (IdentityUser GUID).</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>FK till övningen.</summary>
    public int ExerciseId { get; set; }

    /// <summary>Navigeringsegenskap till övningen.</summary>
    public Exercise Exercise { get; set; } = new();

    /// <summary>Aktuell arbetsvikt i kg.</summary>
    public decimal WeightKg { get; set; }
}
