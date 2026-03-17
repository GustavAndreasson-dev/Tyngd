using System.Collections.Generic;

namespace ArnoldOne.Shared.TrainingPrograms;

public class TrainingSplit
{
    public string SplitId { get; set; } = string.Empty;
    public string SplitName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public List<TrainingPass> Passes { get; set; } = new();
}
