namespace Tyngd.Shared.TrainingPrograms;

/// <summary>
/// Mall för en träningssplit utan användaranknytning. Lagras i databasen och kopieras till
/// <see cref="TrainingSplit"/> när en användare väljer mallen via <see cref="TrainingSplitRepository.CopyForUser"/>.
/// </summary>
public class TrainingSplitTemplate
{
    /// <summary>Unikt id för splitmallen.</summary>
    public int SplitId { get; set; }

    /// <summary>Splittens namn, t.ex. Push, Pull, Legs.</summary>
    public string SplitName { get; set; } = string.Empty;

    /// <summary>Passmallar som ingår i splitten.</summary>
    public List<TrainingPassTemplate> Passes { get; set; } = new();
}
