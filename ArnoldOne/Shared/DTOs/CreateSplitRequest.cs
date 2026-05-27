namespace ArnoldOne.Shared.DTOs;

public class CreateSplitRequest
{
    public string SplitName { get; set; } = string.Empty;
    public List<CreatePassDto> Passes { get; set; } = new();
}

public class CreatePassDto
{
    public string PassName { get; set; } = string.Empty;
    public List<CreateExerciseDto> Exercises { get; set; } = new();
}

public class CreateExerciseDto
{
    public string ExerciseName  { get; set; } = string.Empty;
    public int    DefaultSets   { get; set; } = 3;
    public int    RepsMin       { get; set; } = 8;
    public int    RepsMax       { get; set; } = 12;
    public decimal DefaultWeight { get; set; } = 0;
}
