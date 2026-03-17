namespace ArnoldOne.Shared.TrainingPrograms;

public class TrainingSplitRepository
{
    private readonly List<TrainingSplit> _splits = new();

    public IReadOnlyList<TrainingSplit> All => _splits;

    public TrainingSplit? GetById(int splitId) =>
        _splits.FirstOrDefault(p => p.SplitId == splitId);

    public void Add(TrainingSplit split) => _splits.Add(split);

    public TrainingSplit CopyForUser(TrainingSplitTemplate template, int userId)
    {
        var copy = new TrainingSplit
        {
            SplitId = template.SplitId,
            SplitName = template.SplitName,
            UserId = userId,
            Passes = template.Passes.Select(p => new TrainingPass
            {
                PassId = p.PassId,
                PassName = p.PassName,
                Exercises = p.Exercises.Select(e => new TrainingExercise
                {
                    ExerciseId = e.ExerciseId,
                    ExerciseName = e.ExerciseName,
                    DefaultSets = e.DefaultSets,
                    DefaultRepsMin = e.DefaultRepsMin,
                    DefaultRepsMax = e.DefaultRepsMax,
                    DefaultWeight = e.DefaultWeight
                }).ToList()
            }).ToList()
        };

        _splits.Add(copy);
        return copy;
    }
}
