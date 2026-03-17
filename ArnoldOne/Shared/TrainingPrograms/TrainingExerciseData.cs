namespace ArnoldOne.Shared.TrainingPrograms;

public class TrainingExerciseData
{
    public int ExerciseId { get; set; }
    public int Sets { get; set; }
    public int RepsMin { get; set; }
    public int RepsMax { get; set; }
    public decimal Weight { get; set; }
    public DateOnly Date { get; set; }
}
