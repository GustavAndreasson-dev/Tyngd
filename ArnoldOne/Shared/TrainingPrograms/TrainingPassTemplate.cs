namespace ArnoldOne.Shared.TrainingPrograms;

/// <summary>
/// Mall för ett träningspass utan användaranknytning. Lagras som databas-mall och kopieras till <see cref="TrainingPass"/> vid val.
/// </summary>
public class TrainingPassTemplate
{
    /// <summary>Unikt id för passmallen.</summary>
    public int PassId { get; set; }

    /// <summary>FK till splitmallen.</summary>
    public int SplitId { get; set; }

    /// <summary>Passens namn, t.ex. Push eller Pull.</summary>
    public string PassName { get; set; } = string.Empty;

    /// <summary>Övningsmallar som ingår i passet.</summary>
    public List<TrainingExerciseTemplate> Exercises { get; set; } = new();
}
