using System;

namespace ArnoldOne.Shared.TrainingPrograms;

public class TrainingExerciseData
{
    public string ExerciseId { get; set; } = string.Empty;
    public int Sets { get; set; }
    public int RepsMin { get; set; }
    public int RepsMax { get; set; }
    public decimal Weight { get; set; }
    public DateOnly Date { get; set; }
}
