namespace ArnoldOne.Shared.TrainingPrograms;

public class TrainingExerciseTemplate
{
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int DefaultSets { get; set; }
    public int DefaultRepsMin { get; set; }
    public int DefaultRepsMax { get; set; }
    public decimal DefaultWeight { get; set; }
}
