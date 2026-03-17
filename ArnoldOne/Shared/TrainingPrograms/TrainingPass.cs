using System.Collections.Generic;

namespace ArnoldOne.Shared.TrainingPrograms;

public class TrainingPass
{
    public string PassId { get; set; } = string.Empty;
    public string PassName { get; set; } = string.Empty;
    public List<TrainingExercise> Exercises { get; set; } = new();
}
