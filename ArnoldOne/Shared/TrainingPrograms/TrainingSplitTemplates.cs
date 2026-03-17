using System.Collections.Generic;

namespace ArnoldOne.Shared.TrainingPrograms;

public class TrainingSplitTemplates
{
    public string PplSplitName { get; } = "Push, Pull, Legs";
    public string DefaultUserId { get; } = "Pytten";

    public TrainingSplit CreateBlank(string splitId, string splitName, string userId) =>
        new() { SplitId = splitId, SplitName = splitName, UserId = userId };

    public TrainingSplit CreatePpl(string userId)
    {
        return new TrainingSplit
        {
            SplitId = "ppl",
            SplitName = PplSplitName,
            UserId = userId,
            Passes = new List<TrainingPass>
            {
                new() { PassId = "push", PassName = "Push", Exercises = CreatePushExercises(userId) },
                new() { PassId = "pull", PassName = "Pull", Exercises = CreatePullExercises(userId) },
                new() { PassId = "legs", PassName = "Legs", Exercises = CreateLegExercises(userId) },
                new() { PassId = "axel-underarmar", PassName = "Axel/underarmar", Exercises = CreateArmsExercises(userId) }
            }
        };
    }

    public TrainingSplit CreatePpl() => CreatePpl(DefaultUserId);

    private List<TrainingExercise> CreatePushExercises(string userId) =>
        new()
        {
            Ex("bankpress", "Bänkpress", userId, 3, 6, 10, 60m),
            Ex("lutande-hantelpress", "Lutande hantelpress", userId, 3, 8, 12, 20m),
            Ex("cable-crossover", "Cable crossover", userId, 3, 12, 15, 10m),
            Ex("dips", "Dips", userId, 3, 8, 12, 0m),
            Ex("ez-bar-skullcrusher", "EZ bar skullcrusher", userId, 3, 8, 12, 25m),
            Ex("hantel-extension", "Hantel extension", userId, 3, 8, 12, 15m),
            Ex("cable-extension", "Cable extension", userId, 3, 8, 12, 15m)
        };

    private List<TrainingExercise> CreatePullExercises(string userId) =>
        new()
        {
            Ex("marklyft", "Marklyft", userId, 3, 5, 5, 100m),
            Ex("stangrodd", "Stångrodd", userId, 3, 8, 12, 60m),
            Ex("hantelrodd-en-arm", "Hantelrodd en arm", userId, 3, 8, 12, 30m),
            Ex("latsdrag", "Latsdrag", userId, 3, 10, 12, 50m),
            Ex("sittande-cable-row", "Sittande cable row", userId, 3, 10, 12, 50m),
            Ex("z-bar-bicep-curl", "Z bar bicep curl", userId, 3, 10, 12, 30m),
            Ex("concentration-curl", "Concentration curl", userId, 3, 10, 12, 15m),
            Ex("sittande-lutad-hantelcurl", "Sittande lutad hantelcurl", userId, 3, 10, 12, 12m)
        };

    private List<TrainingExercise> CreateLegExercises(string userId) =>
        new()
        {
            Ex("knaboj", "Knäböj", userId, 3, 6, 10, 80m),
            Ex("benpress", "Benpress", userId, 3, 12, 15, 150m),
            Ex("benspark", "Benspark", userId, 3, 8, 12, 40m),
            Ex("raka-marklyft", "Raka marklyft", userId, 3, 8, 12, 70m),
            Ex("bencurl", "Bencurl", userId, 3, 10, 12, 35m),
            Ex("tahavningar", "Tåhävningar", userId, 3, 10, 15, 60m)
        };

    private List<TrainingExercise> CreateArmsExercises(string userId) =>
        new()
        {
            Ex("militarpress", "Militärpress", userId, 3, 6, 10, 40m),
            Ex("sittande-arnold-press", "Sittande Arnold press", userId, 3, 8, 12, 25m),
            Ex("hantellyft-sida", "Hantellyft sida", userId, 3, 10, 15, 10m),
            Ex("upright-row", "Upright row", userId, 3, 8, 12, 30m),
            Ex("hanteshrugs", "Hanteshrugs", userId, 3, 8, 12, 40m),
            Ex("handledscurl-sittande", "Handledscurl sittande", userId, 3, 12, 15, 15m),
            Ex("stang-static-hold", "Stång static hold - 5 min", userId, 1, 1, 1, 60m)
        };

    private TrainingExercise Ex(string id, string name, string userId, int sets, int repsMin, int repsMax, decimal weight) =>
        new()
        {
            ExerciseId = id,
            ExerciseName = name,
            UserId = userId,
            DefaultSets = sets,
            DefaultRepsMin = repsMin,
            DefaultRepsMax = repsMax,
            DefaultWeight = weight
        };
}
