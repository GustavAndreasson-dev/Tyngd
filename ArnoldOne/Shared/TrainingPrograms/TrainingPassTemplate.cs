namespace ArnoldOne.Shared.TrainingPrograms;

public class TrainingPassTemplate
{
    public int PassId { get; set; }
    public string PassName { get; set; } = string.Empty;
    public List<TrainingExerciseTemplate> Exercises { get; set; } = new();
}
