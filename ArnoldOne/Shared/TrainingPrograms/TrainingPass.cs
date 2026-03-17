namespace ArnoldOne.Shared.TrainingPrograms;

public class TrainingPass
{
    public int PassId { get; set; }
    public string PassName { get; set; } = string.Empty;
    public List<TrainingExercise> Exercises { get; set; } = new();
}
