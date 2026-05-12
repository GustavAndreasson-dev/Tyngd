namespace ArnoldOne.Shared.Temp_klasser;

public class Exercise
{
    public int PassId { get; set; }

    /// <summary>FK till övningen i exercisekatalogen.</summary>
    public int ExerciseId { get; set; }

    ///<summary>Namn på övningen.</summary>
    public string ExerciseName { get; set; } = string.Empty;

    /// <summary>Rekommenderat antal set.</summary>
    public int? DefaultSets { get; set; }

    /// <summary>Lägsta rekommenderade antal reps per set.</summary>
    public int? DefaultRepsMin { get; set; }

    /// <summary>Högsta rekommenderade antal reps per set.</summary>
    public int? DefaultRepsMax { get; set; }

    /// <summary>Rekommenderad startvikt i kg.</summary>
    public decimal? DefaultWeight { get; set; }
}
