namespace Tyngd.Shared.TrainingPrograms;

/// <summary>
/// Loggad data för en övning inom en <see cref="WorkoutSession"/>.
/// Ersätter Notes-dictionaryn i PassData (LocalStorage).
/// En rad per övning per session.
///
/// Designbeslut: När en ny session startas ska <see cref="NoteText"/> förifyllas med
/// värdet från den senaste <see cref="WorkoutSession"/> där samma <see cref="ExerciseId"/>
/// förekom. Anteckningen följer alltså med mellan pass men skrivs över om användaren
/// ändrar den under passet.
/// </summary>
public class WorkoutExerciseLog
{
    /// <summary>Unikt id för raden.</summary>
    public int Id { get; set; }

    /// <summary>FK till sessionen.</summary>
    public int WorkoutSessionId { get; set; }

    /// <summary>FK till övningen.</summary>
    public int ExerciseId { get; set; }

    /// <summary>Navigeringsegenskap till övningen.</summary>
    public Exercise Exercise { get; set; } = new();

    /// <summary>Anteckning för övningen under detta pass.</summary>
    public string NoteText { get; set; } = string.Empty;

    /// <summary>Loggade set för övningen.</summary>
    public List<WorkoutSetLog> Sets { get; set; } = new();
}
