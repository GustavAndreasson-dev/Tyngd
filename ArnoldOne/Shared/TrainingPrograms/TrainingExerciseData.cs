namespace ArnoldOne.Shared.TrainingPrograms;

/// <summary>
/// Loggad träningsdata för en övning vid ett specifikt tillfälle.
/// </summary>
public class TrainingExerciseData
{
    /// <summary>Id för övningen som datan tillhör.</summary>
    public int ExerciseId { get; set; }

    /// <summary>Antal utförda set.</summary>
    public int Sets { get; set; }

    /// <summary>Lägsta antal reps utförda i ett set.</summary>
    public int RepsMin { get; set; }

    /// <summary>Högsta antal reps utförda i ett set.</summary>
    public int RepsMax { get; set; }

    /// <summary>Använd vikt i kg.</summary>
    public decimal Weight { get; set; }

    /// <summary>Datum då träningen utfördes.</summary>
    public DateOnly Date { get; set; }
}
