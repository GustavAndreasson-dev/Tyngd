namespace ArnoldOne.Shared.TrainingPrograms;

public class TrainingSplitTemplateGenerator
{
    public TrainingSplitTemplate CreatePpl()
    {
        return new TrainingSplitTemplate
        {
            SplitId = 1,
            SplitName = "Push, Pull, Legs",
            Passes = new List<TrainingPassTemplate>
            {
                new() { PassId = 1, PassName = "Push", Exercises = CreatePushExercises() },
                new() { PassId = 2, PassName = "Pull", Exercises = CreatePullExercises() },
                new() { PassId = 3, PassName = "Legs", Exercises = CreateLegExercises() },
                new() { PassId = 4, PassName = "Axel/underarmar", Exercises = CreateArmsExercises() }
            }
        };
    }

    private List<TrainingExerciseTemplate> CreatePushExercises() =>
        new()
        {
            Ex(1, "Bänkpress", 3, 6, 10, 60m),
            Ex(2, "Lutande hantelpress", 3, 8, 12, 20m),
            Ex(3, "Cable crossover", 3, 12, 15, 10m),
            Ex(4, "Dips", 3, 8, 12, 0m),
            Ex(5, "EZ bar skullcrusher", 3, 8, 12, 25m),
            Ex(6, "Hantel extension", 3, 8, 12, 15m),
            Ex(7, "Cable extension", 3, 8, 12, 15m)
        };

    private List<TrainingExerciseTemplate> CreatePullExercises() =>
        new()
        {
            Ex(8, "Marklyft", 3, 5, 5, 100m),
            Ex(9, "Stångrodd", 3, 8, 12, 60m),
            Ex(10, "Hantelrodd en arm", 3, 8, 12, 30m),
            Ex(11, "Latsdrag", 3, 10, 12, 50m),
            Ex(12, "Sittande cable row", 3, 10, 12, 50m),
            Ex(13, "Z bar bicep curl", 3, 10, 12, 30m),
            Ex(14, "Concentration curl", 3, 10, 12, 15m),
            Ex(15, "Sittande lutad hantelcurl", 3, 10, 12, 12m)
        };

    private List<TrainingExerciseTemplate> CreateLegExercises() =>
        new()
        {
            Ex(16, "Knäböj", 3, 6, 10, 80m),
            Ex(17, "Benpress", 3, 12, 15, 150m),
            Ex(18, "Benspark", 3, 8, 12, 40m),
            Ex(19, "Raka marklyft", 3, 8, 12, 70m),
            Ex(20, "Bencurl", 3, 10, 12, 35m),
            Ex(21, "Tåhävningar", 3, 10, 15, 60m)
        };

    private List<TrainingExerciseTemplate> CreateArmsExercises() =>
        new()
        {
            Ex(22, "Militärpress", 3, 6, 10, 40m),
            Ex(23, "Sittande Arnold press", 3, 8, 12, 25m),
            Ex(24, "Hantellyft sida", 3, 10, 15, 10m),
            Ex(25, "Upright row", 3, 8, 12, 30m),
            Ex(26, "Hanteshrugs", 3, 8, 12, 40m),
            Ex(27, "Handledscurl sittande", 3, 12, 15, 15m),
            Ex(28, "Stång static hold - 5 min", 1, 1, 1, 60m)
        };

    private TrainingExerciseTemplate Ex(int id, string name, int sets, int repsMin, int repsMax, decimal weight) =>
        new()
        {
            ExerciseId = id,
            ExerciseName = name,
            DefaultSets = sets,
            DefaultRepsMin = repsMin,
            DefaultRepsMax = repsMax,
            DefaultWeight = weight
        };
}
