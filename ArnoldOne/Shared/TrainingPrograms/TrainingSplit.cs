namespace ArnoldOne.Shared.TrainingPrograms;

/// <summary>
/// En träningssplit kopplad till en användare. Skapas genom att kopiera en <see cref="TrainingSplitTemplate"/> via <see cref="TrainingSplitRepository.CopyForUser"/>.
/// </summary>
public class TrainingSplit
{
    /// <summary>Unikt id för splitten.</summary>
    public int SplitId { get; set; }

    /// <summary>Splittens namn, t.ex. Push, Pull, Legs.</summary>
    public string SplitName { get; set; } = string.Empty;

    /// <summary>Id för användaren som äger splitten.</summary>
    public int UserId { get; set; }

    /// <summary>Pass som ingår i splitten.</summary>
    public List<TrainingPass> Passes { get; set; } = new();
}
