namespace ArnoldOne.Shared.Temp_klasser;

public class ExerciseTemplate
{
    /// <summary>FK till övningen i exercisekatalogen.</summary>
    public int ExerciseTemplateId { get; set; }

    /// <summary>Övningens namn.</summary>
    public string ExerciseTemplateName { get; set; } = string.Empty;

    /// <summary>Rekommenderat antal set.</summary>
    public int? DefaultSets { get; set; }

    /// <summary>Lägsta rekommenderade antal reps per set.</summary>
    public int? DefaultRepsMin { get; set; }

    /// <summary>Högsta rekommenderade antal reps per set.</summary>
    public int? DefaultRepsMax { get; set; }

    /// <summary>Rekommenderad startvikt i kg.</summary>
    public decimal? DefaultWeight { get; set; }

    /// <summary>Vilket pass övningen hör till.</summary>
    public int PassTemplateId { get; set; }
}
