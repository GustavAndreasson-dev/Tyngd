namespace ArnoldOne.Shared.TrainingPrograms;

/// <summary>
/// Skapar fördefinierade <see cref="TrainingSplitTemplate"/>-objekt i minnet.
/// Används tills mallar lagras i databas.
/// </summary>
public class TrainingSplitTemplateGenerator
{
    /// <summary>
    /// Skapar en Push/Pull/Legs-splitmall med fyra pass och tillhörande övningar.
    /// </summary>
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

    /// <summary>Skapar övningsmallar för Push-passet (bröst, triceps).</summary>
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

    /// <summary>Skapar övningsmallar för Pull-passet (rygg, biceps).</summary>
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

    /// <summary>Skapar övningsmallar för Legs-passet (quadriceps, hamstrings, vader).</summary>
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

    /// <summary>Skapar övningsmallar för Axel/underarmar-passet (axlar, underarmar).</summary>
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

    /// <summary>
    /// Skapar en PPL1-splitmall med fyra pass och tillhörande övningar.
    /// Övningar som delas med PPL2 återanvänder samma ExerciseId för viktsynkronisering.
    /// PPL1 använder PassId 101–104 och ExerciseId 101–117 för unika övningar.
    /// </summary>
    public TrainingSplitTemplate CreatePpl1()
    {
        return new TrainingSplitTemplate
        {
            SplitId = 2,
            SplitName = "PPL1",
            Passes = new List<TrainingPassTemplate>
            {
                new() { PassId = 101, PassName = "Push", Exercises = CreatePpl1PushExercises() },
                new() { PassId = 102, PassName = "Pull", Exercises = CreatePpl1PullExercises() },
                new() { PassId = 103, PassName = "Legs", Exercises = CreatePpl1LegExercises() },
                new() { PassId = 104, PassName = "Axel/underarmar", Exercises = CreatePpl1ArmsExercises() }
            }
        };
    }

    /// <summary>
    /// Returnerar alla tillgängliga träningssplitmallar.
    /// </summary>
    public IReadOnlyList<TrainingSplitTemplate> GetAllTemplates() =>
        new[] { CreatePpl(), CreatePpl1() };

    /// <summary>
    /// Söker ett pass
    /// Returnerar null om inget pass hittas.
    /// </summary>
    public TrainingPassTemplate? FindPass(int passId) =>
        new[] { CreatePpl(), CreatePpl1() }
            .SelectMany(t => t.Passes)
            .FirstOrDefault(p => p.PassId == passId);

    /// <summary>Skapar övningsmallar för PPL1 Push-passet (bröst, axlar, triceps).</summary>
    private List<TrainingExerciseTemplate> CreatePpl1PushExercises() =>
        new()
        {
            Ex(1,   "Bänkpress",              4, 6,  8,  60m),
            Ex(22,  "Militärpress",           4, 6,  8,  40m),
            Ex(101, "Lutad bänk/hantelpress", 3, 10, 10, 0m),
            Ex(102, "Axelpress",              3, 10, 10, 0m),
            Ex(103, "Triceps extension",      3, 10, 10, 0m),
            Ex(104, "Triceps pushdown",       3, 10, 10, 0m)
        };

    /// <summary>Skapar övningsmallar för PPL1 Pull-passet (rygg, biceps).</summary>
    private List<TrainingExerciseTemplate> CreatePpl1PullExercises() =>
        new()
        {
            Ex(8,   "Marklyft",        4, 5,  5,  100m),
            Ex(9,   "Stångrodd",       4, 6,  8,  60m),
            Ex(11,  "Latsdrag",        3, 10, 12, 50m),
            Ex(105, "Stångcurl",       4, 10, 12, 0m),
            Ex(106, "Hammercurls",     4, 10, 12, 0m),
            Ex(107, "Omvända flyes",   3, 15, 15, 0m)
        };

    /// <summary>Skapar övningsmallar för PPL1 Legs-passet (quadriceps, hamstrings, vader).</summary>
    private List<TrainingExerciseTemplate> CreatePpl1LegExercises() =>
        new()
        {
            Ex(16,  "Knäböj",              5, 6,  8,  80m),
            Ex(108, "Rumänska marklyft",   5, 6,  8,  0m),
            Ex(109, "Lårcurl",             3, 10, 10, 0m),
            Ex(18,  "Benspark",            3, 10, 10, 40m),
            Ex(110, "Vadpress",            4, 10, 12, 0m),
            Ex(111, "Crunches",            4, 10, 12, 0m)
        };

    /// <summary>Skapar övningsmallar för PPL1 Axel/underarmar-passet.</summary>
    private List<TrainingExerciseTemplate> CreatePpl1ArmsExercises() =>
        new()
        {
            Ex(112, "Smalbänk",                    3, 10, 12, 0m),
            Ex(113, "Stångrodd omvänt grepp",      3, 10, 10, 0m),
            Ex(114, "Tricep extension hantel",     3, 10, 10, 0m),
            Ex(115, "Skullcrushers",               3, 10, 12, 0m),
            Ex(116, "Biceps cable stående",        3, 10, 12, 0m),
            Ex(117, "Sittande bicep curl",         3, 10, 12, 0m)
        };

    /// <summary>Hjälpmetod för att skapa en övningsmall med defaultvärden.</summary>
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
