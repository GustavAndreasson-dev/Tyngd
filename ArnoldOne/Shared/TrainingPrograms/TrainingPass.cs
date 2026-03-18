namespace ArnoldOne.Shared.TrainingPrograms;

/// <summary>
/// Ett träningspass kopplat till en användares split. Skapas genom att kopiera en <see cref="TrainingPassTemplate"/>.
/// </summary>
public class TrainingPass
{
    /// <summary>Unikt id för passet.</summary>
    public int PassId { get; set; }

    /// <summary>Passens namn, t.ex. Push eller Pull.</summary>
    public string PassName { get; set; } = string.Empty;

    /// <summary>Övningar som ingår i passet.</summary>
    public List<TrainingExercise> Exercises { get; set; } = new();
}
