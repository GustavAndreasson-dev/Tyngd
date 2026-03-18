namespace ArnoldOne.Shared.TrainingPrograms;

/// <summary>
/// Mall för en övning utan användaranknytning. Lagras som databas-mall och kopieras till <see cref="TrainingExercise"/> vid val.
/// </summary>
public class TrainingExerciseTemplate
{
    /// <summary>Unikt id för övningsmallen.</summary>
    public int ExerciseId { get; set; }

    /// <summary>Övningens namn.</summary>
    public string ExerciseName { get; set; } = string.Empty;

    /// <summary>Rekommenderat antal set.</summary>
    public int DefaultSets { get; set; }

    /// <summary>Lägsta rekommenderade antal reps per set.</summary>
    public int DefaultRepsMin { get; set; }

    /// <summary>Högsta rekommenderade antal reps per set.</summary>
    public int DefaultRepsMax { get; set; }

    /// <summary>Rekommenderad startvikt i kg.</summary>
    public decimal DefaultWeight { get; set; }
}
