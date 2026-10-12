namespace Tyngd.Shared.TrainingPrograms;

/// <summary>
/// En övning kopplad till ett användarpass. Skapas genom att kopiera en <see cref="TrainingExerciseTemplate"/>.
/// </summary>
public class TrainingExercise
{
    /// <summary>FK till passet som övningen tillhör.</summary>
    public int PassId { get; set; }

    /// <summary>FK till övningen i exercisekatalogen.</summary>
    public int ExerciseId { get; set; }

    /// <summary>Navigeringsegenskap till övningen.</summary>
    public Exercise Exercise { get; set; } = new();

    /// <summary>Rekommenderat antal set.</summary>
    public int DefaultSets { get; set; }

    /// <summary>Lägsta rekommenderade antal reps per set.</summary>
    public int DefaultRepsMin { get; set; }

    /// <summary>Högsta rekommenderade antal reps per set.</summary>
    public int DefaultRepsMax { get; set; }

    /// <summary>Rekommenderad startvikt i kg.</summary>
    public decimal DefaultWeight { get; set; }
}
