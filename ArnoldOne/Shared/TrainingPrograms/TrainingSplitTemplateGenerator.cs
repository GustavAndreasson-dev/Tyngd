namespace ArnoldOne.Shared.TrainingPrograms;

/// <summary>
/// Skapar fördefinierade <see cref="TrainingSplitTemplate"/>-objekt i minnet.
/// Används tills mallar lagras i databas.
/// PPL (SplitId=1) använder PassId 1–4.
/// PPL1 (SplitId=2) använder PassId 5–8 för global unikhet.
/// </summary>
public class TrainingSplitTemplateGenerator
{
    /// <summary>
    /// Exercisekatalog – alla övningar med globalt unika id och namn.
    /// Delas mellan splits för viktsynkronisering via ExerciseId.
    /// </summary>
    private static readonly Dictionary<int, Exercise> Catalog = new()
    {
        [1]   = new() { ExerciseId = 1,   ExerciseName = "Bänkpress" },
        [2]   = new() { ExerciseId = 2,   ExerciseName = "Lutande hantelpress" },
        [3]   = new() { ExerciseId = 3,   ExerciseName = "Cable crossover" },
        [4]   = new() { ExerciseId = 4,   ExerciseName = "Dips" },
        [5]   = new() { ExerciseId = 5,   ExerciseName = "EZ bar skullcrusher" },
        [6]   = new() { ExerciseId = 6,   ExerciseName = "Hantel extension" },
        [7]   = new() { ExerciseId = 7,   ExerciseName = "Cable extension" },
        [8]   = new() { ExerciseId = 8,   ExerciseName = "Marklyft" },
        [9]   = new() { ExerciseId = 9,   ExerciseName = "Stångrodd" },
        [10]  = new() { ExerciseId = 10,  ExerciseName = "Hantelrodd en arm" },
        [11]  = new() { ExerciseId = 11,  ExerciseName = "Latsdrag" },
        [12]  = new() { ExerciseId = 12,  ExerciseName = "Sittande cable row" },
        [13]  = new() { ExerciseId = 13,  ExerciseName = "Z bar bicep curl" },
        [14]  = new() { ExerciseId = 14,  ExerciseName = "Concentration curl" },
        [15]  = new() { ExerciseId = 15,  ExerciseName = "Sittande lutad hantelcurl" },
        [16]  = new() { ExerciseId = 16,  ExerciseName = "Knäböj" },
        [17]  = new() { ExerciseId = 17,  ExerciseName = "Benpress" },
        [18]  = new() { ExerciseId = 18,  ExerciseName = "Benspark" },
        [19]  = new() { ExerciseId = 19,  ExerciseName = "Raka marklyft" },
        [20]  = new() { ExerciseId = 20,  ExerciseName = "Bencurl" },
        [21]  = new() { ExerciseId = 21,  ExerciseName = "Tåhävningar" },
        [22]  = new() { ExerciseId = 22,  ExerciseName = "Militärpress" },
        [23]  = new() { ExerciseId = 23,  ExerciseName = "Sittande Arnold press" },
        [24]  = new() { ExerciseId = 24,  ExerciseName = "Hantellyft sida" },
        [25]  = new() { ExerciseId = 25,  ExerciseName = "Upright row" },
        [26]  = new() { ExerciseId = 26,  ExerciseName = "Hanteshrugs" },
        [27]  = new() { ExerciseId = 27,  ExerciseName = "Handledscurl sittande" },
        [28]  = new() { ExerciseId = 28,  ExerciseName = "Stång static hold - 5 min" },
        [101] = new() { ExerciseId = 101, ExerciseName = "Lutad bänk/hantelpress" },
        [102] = new() { ExerciseId = 102, ExerciseName = "Axelpress" },
        [103] = new() { ExerciseId = 103, ExerciseName = "Triceps extension" },
        [104] = new() { ExerciseId = 104, ExerciseName = "Triceps pushdown" },
        [105] = new() { ExerciseId = 105, ExerciseName = "Stångcurl" },
        [106] = new() { ExerciseId = 106, ExerciseName = "Hammercurls" },
        [107] = new() { ExerciseId = 107, ExerciseName = "Omvända flyes" },
        [108] = new() { ExerciseId = 108, ExerciseName = "Rumänska marklyft" },
        [109] = new() { ExerciseId = 109, ExerciseName = "Lårcurl" },
        [110] = new() { ExerciseId = 110, ExerciseName = "Vadpress" },
        [111] = new() { ExerciseId = 111, ExerciseName = "Crunches" },
        [112] = new() { ExerciseId = 112, ExerciseName = "Smalbänk" },
        [113] = new() { ExerciseId = 113, ExerciseName = "Stångrodd omvänt grepp" },
        [114] = new() { ExerciseId = 114, ExerciseName = "Tricep extension hantel" },
        [115] = new() { ExerciseId = 115, ExerciseName = "Skullcrushers" },
        [116] = new() { ExerciseId = 116, ExerciseName = "Biceps cable stående" },
        [117] = new() { ExerciseId = 117, ExerciseName = "Sittande bicep curl" },
    };

    /// <summary>Returnerar hela exercisekatalogen.</summary>
    public IReadOnlyDictionary<int, Exercise> GetExerciseCatalog() => Catalog;

    /// <summary>Skapar en Push/Pull/Legs-splitmall med fyra pass och tillhörande övningar.</summary>
    public TrainingSplitTemplate CreatePpl()
    {
        return new TrainingSplitTemplate
        {
            SplitId = 1,
            SplitName = "Push, Pull, Legs",
            Passes =
            [
                new() { PassId = 1, SplitId = 1, PassName = "Push",            Exercises = CreatePushExercises(1) },
                new() { PassId = 2, SplitId = 1, PassName = "Pull",            Exercises = CreatePullExercises(2) },
                new() { PassId = 3, SplitId = 1, PassName = "Legs",            Exercises = CreateLegExercises(3) },
                new() { PassId = 4, SplitId = 1, PassName = "Axel/underarmar", Exercises = CreateArmsExercises(4) },
            ]
        };
    }

    /// <summary>
    /// Skapar en PPL1-splitmall med fyra pass och tillhörande övningar.
    /// Delade övningar (samma ExerciseId som PPL) synkroniserar vikt automatiskt.
    /// PPL1 använder PassId 5–8 för global unikhet.
    /// </summary>
    public TrainingSplitTemplate CreatePpl1()
    {
        return new TrainingSplitTemplate
        {
            SplitId = 2,
            SplitName = "PPL1",
            Passes =
            [
                new() { PassId = 5, SplitId = 2, PassName = "Push",            Exercises = CreatePpl1PushExercises(5) },
                new() { PassId = 6, SplitId = 2, PassName = "Pull",            Exercises = CreatePpl1PullExercises(6) },
                new() { PassId = 7, SplitId = 2, PassName = "Legs",            Exercises = CreatePpl1LegExercises(7) },
                new() { PassId = 8, SplitId = 2, PassName = "Axel/underarmar", Exercises = CreatePpl1ArmsExercises(8) },
            ]
        };
    }

    /// <summary>Returnerar alla tillgängliga träningssplitmallar.</summary>
    public IReadOnlyList<TrainingSplitTemplate> GetAllTemplates() =>
        [CreatePpl(), CreatePpl1()];

    /// <summary>
    /// Söker ett pass via split-id och pass-id.
    /// Returnerar null om inget pass hittas.
    /// </summary>
    public TrainingPassTemplate? FindPass(int splitId, int passId) =>
        GetAllTemplates()
            .FirstOrDefault(t => t.SplitId == splitId)
            ?.Passes.FirstOrDefault(p => p.PassId == passId);

    // --- PPL-övningar ---

    private List<TrainingExerciseTemplate> CreatePushExercises(int passId) =>
    [
        Ex(1,  passId, 3, 6,  10, 60m),
        Ex(2,  passId, 3, 8,  12, 20m),
        Ex(3,  passId, 3, 12, 15, 10m),
        Ex(4,  passId, 3, 8,  12, 0m),
        Ex(5,  passId, 3, 8,  12, 25m),
        Ex(6,  passId, 3, 8,  12, 15m),
        Ex(7,  passId, 3, 8,  12, 15m),
    ];

    private List<TrainingExerciseTemplate> CreatePullExercises(int passId) =>
    [
        Ex(8,  passId, 3, 5,  5,  100m),
        Ex(9,  passId, 3, 8,  12, 60m),
        Ex(10, passId, 3, 8,  12, 30m),
        Ex(11, passId, 3, 10, 12, 50m),
        Ex(12, passId, 3, 10, 12, 50m),
        Ex(13, passId, 3, 10, 12, 30m),
        Ex(14, passId, 3, 10, 12, 15m),
        Ex(15, passId, 3, 10, 12, 12m),
    ];

    private List<TrainingExerciseTemplate> CreateLegExercises(int passId) =>
    [
        Ex(16, passId, 3, 6,  10, 80m),
        Ex(17, passId, 3, 12, 15, 150m),
        Ex(18, passId, 3, 8,  12, 40m),
        Ex(19, passId, 3, 8,  12, 70m),
        Ex(20, passId, 3, 10, 12, 35m),
        Ex(21, passId, 3, 10, 15, 60m),
    ];

    private List<TrainingExerciseTemplate> CreateArmsExercises(int passId) =>
    [
        Ex(22, passId, 3, 6,  10, 40m),
        Ex(23, passId, 3, 8,  12, 25m),
        Ex(24, passId, 3, 10, 15, 10m),
        Ex(25, passId, 3, 8,  12, 30m),
        Ex(26, passId, 3, 8,  12, 40m),
        Ex(27, passId, 3, 12, 15, 15m),
        Ex(28, passId, 1, 1,  1,  60m),
    ];

    // --- PPL1-övningar ---

    private List<TrainingExerciseTemplate> CreatePpl1PushExercises(int passId) =>
    [
        Ex(1,   passId, 4, 6,  8,  60m),
        Ex(22,  passId, 4, 6,  8,  40m),
        Ex(101, passId, 3, 10, 10, 0m),
        Ex(102, passId, 3, 10, 10, 0m),
        Ex(103, passId, 3, 10, 10, 0m),
        Ex(104, passId, 3, 10, 10, 0m),
    ];

    private List<TrainingExerciseTemplate> CreatePpl1PullExercises(int passId) =>
    [
        Ex(8,   passId, 4, 5,  5,  100m),
        Ex(9,   passId, 4, 6,  8,  60m),
        Ex(11,  passId, 3, 10, 12, 50m),
        Ex(105, passId, 4, 10, 12, 0m),
        Ex(106, passId, 4, 10, 12, 0m),
        Ex(107, passId, 3, 15, 15, 0m),
    ];

    private List<TrainingExerciseTemplate> CreatePpl1LegExercises(int passId) =>
    [
        Ex(16,  passId, 5, 6,  8,  80m),
        Ex(108, passId, 5, 6,  8,  0m),
        Ex(109, passId, 3, 10, 10, 0m),
        Ex(18,  passId, 3, 10, 10, 40m),
        Ex(110, passId, 4, 10, 12, 0m),
        Ex(111, passId, 4, 10, 12, 0m),
    ];

    private List<TrainingExerciseTemplate> CreatePpl1ArmsExercises(int passId) =>
    [
        Ex(112, passId, 3, 10, 12, 0m),
        Ex(113, passId, 3, 10, 10, 0m),
        Ex(114, passId, 3, 10, 10, 0m),
        Ex(115, passId, 3, 10, 12, 0m),
        Ex(116, passId, 3, 10, 12, 0m),
        Ex(117, passId, 3, 10, 12, 0m),
    ];

    /// <summary>Hjälpmetod för att skapa en övningsmall med defaults och korrekt FK + navigeringsegenskap.</summary>
    private static TrainingExerciseTemplate Ex(int exerciseId, int passId, int sets, int repsMin, int repsMax, decimal weight) =>
        new()
        {
            ExerciseId   = exerciseId,
            Exercise     = Catalog[exerciseId],
            DefaultSets  = sets,
            DefaultRepsMin = repsMin,
            DefaultRepsMax = repsMax,
            DefaultWeight  = weight,
        };
}
