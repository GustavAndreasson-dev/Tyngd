namespace ArnoldOne.Shared.TrainingPrograms;
public class TrainingExercise
{
    public string ExerciseId { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int DefaultSets { get; set; }
    public int DefaultRepsMin { get; set; }
    public int DefaultRepsMax { get; set; }
    public decimal DefaultWeight { get; set; }
}
