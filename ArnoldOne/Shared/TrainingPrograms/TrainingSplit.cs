namespace ArnoldOne.Shared.TrainingPrograms;

public class TrainingSplit
{
    public int SplitId { get; set; }
    public string SplitName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public List<TrainingPass> Passes { get; set; } = new();
}
