namespace ArnoldOne.Shared.TrainingPrograms;

public class TrainingSplitTemplate
{
    public int SplitId { get; set; }
    public string SplitName { get; set; } = string.Empty;
    public List<TrainingPassTemplate> Passes { get; set; } = new();
}
