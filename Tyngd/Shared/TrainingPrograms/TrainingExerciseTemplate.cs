namespace Tyngd.Shared.TrainingPrograms;

/// <summary>
/// Kopplingstabell mellan ett pass och en övning i mallen.
/// Lagrar defaults (set/reps/vikt) specifika för just det passet.
/// </summary>
public class TrainingExerciseTemplate
{
    /// <summary>FK till övningen i exercisekatalogen.</summary>
    public int ExerciseId { get; set; }

    /// <summary>Navigeringsegenskap till övningen.</summary>
    public Exercise Exercise { get; set; } = new();

    /// <summary>Koppling till övningskatalogen (wger). Null för egen övning.</summary>
    public int? CatalogExerciseId { get; set; }

    /// <summary>Rekommenderat antal set.</summary>
    public int DefaultSets { get; set; }

    /// <summary>Lägsta rekommenderade antal reps per set.</summary>
    public int DefaultRepsMin { get; set; }

    /// <summary>Högsta rekommenderade antal reps per set.</summary>
    public int DefaultRepsMax { get; set; }

    /// <summary>Rekommenderad startvikt i kg.</summary>
    public decimal DefaultWeight { get; set; }
}
