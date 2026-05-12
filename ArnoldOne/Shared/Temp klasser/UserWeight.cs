namespace ArnoldOne.Shared.Temp_klasser;

/// <summary>
/// En användares aktuella arbetsvikt för en övning.
/// Ersätter "exercise-weights" i LocalStorage.
/// En rad per användare och övning.
/// </summary>
public class UserWeight
{
    /// <summary>Unikt id för raden.</summary>
    public int WeightId { get; set; }

    /// <summary>FK till användaren (IdentityUser GUID).</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>FK till övningen.</summary>
    public int ExerciseId { get; set; }

    /// <summary>Aktuell arbetsvikt i kg.</summary>
    public decimal Weight { get; set; }
}