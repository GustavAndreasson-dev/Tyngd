using Microsoft.AspNetCore.Identity;
using Tyngd.Shared.TrainingPrograms;
using Tyngd.Shared.Users;

namespace Tyngd.Server.Models;

/// <summary>
/// Applikationens användarentitet. Ärver från <see cref="IdentityUser"/> som ger
/// inbyggt stöd för lösenordshashning, roller, claims och token-hantering.
/// Lägg till applikationsspecifika fält här.
/// Id (PK) ärvs från IdentityUser som en GUID-sträng.
/// </summary>
public class AppUser : IdentityUser
{
    /// <summary>Datum och tid när kontot skapades (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Användarens genomförda träningssessioner.</summary>
    public List<WorkoutSession> WorkoutSessions { get; set; } = new();

    /// <summary>Användarens aktuella arbetsvikter per övning.</summary>
    public List<UserExerciseWeight> ExerciseWeights { get; set; } = new();

    /// <summary>Splits som användaren har aktiverat.</summary>
    public List<UserSplit> UserSplits { get; set; } = new();
}
