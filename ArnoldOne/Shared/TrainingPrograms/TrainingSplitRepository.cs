namespace ArnoldOne.Shared.TrainingPrograms;

/// <summary>
/// In-memory lagring av användares träningssplits.
/// Ersätts med databasanrop när backend finns på plats.
/// </summary>
public class TrainingSplitRepository
{
    private readonly List<TrainingSplit> _splits = new();

    /// <summary>Alla sparade splits.</summary>
    public IReadOnlyList<TrainingSplit> All => _splits;

    /// <summary>Hämtar en split via id, eller null om den inte finns.</summary>
    public TrainingSplit? GetById(int splitId) =>
        _splits.FirstOrDefault(p => p.SplitId == splitId);

    /// <summary>Lägger till en split i lagret.</summary>
    public void Add(TrainingSplit split) => _splits.Add(split);

    /// <summary>
    /// Kopierar en splitmall till en användares data genom att skapa en ny
    /// <see cref="TrainingSplit"/> med tillhörande pass och övningar, kopplade till <paramref name="userId"/>.
    /// Den nya splitten sparas automatiskt i lagret.
    /// </summary>
    public TrainingSplit CopyForUser(TrainingSplitTemplate template, int userId)
    {
        // Kopiera hela trädstukturen: split -> pass -> övningar
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
