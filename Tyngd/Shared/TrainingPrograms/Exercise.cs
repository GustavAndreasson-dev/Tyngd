namespace Tyngd.Shared.TrainingPrograms;

/// <summary>
/// En övning i exercisekatalogen. Lagrar enbart namn och id.
/// Defaults (set/reps/vikt) hanteras i <see cref="TrainingExerciseTemplate"/> per pass.
/// </summary>
public class Exercise
{
    /// <summary>Unikt id för övningen.</summary>
    public int ExerciseId { get; set; }

    /// <summary>Övningens namn.</summary>
    public string ExerciseName { get; set; } = string.Empty;
}
