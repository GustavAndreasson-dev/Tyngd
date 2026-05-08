namespace ArnoldOne.Shared.TrainingPrograms;

/// <summary>
/// Kontrakt för lagring och hämtning av användares träningssplits.
/// In-memory-implementationen används tills databasen är på plats,
/// då byts den mot en EF Core-implementation utan att övrig kod påverkas.
/// </summary>
public interface ITrainingSplitRepository
{
    /// <summary>Alla sparade splits.</summary>
    IReadOnlyList<TrainingSplit> All { get; }

    /// <summary>Hämtar en split via id, eller null om den inte finns.</summary>
    TrainingSplit? GetById(int splitId);

    /// <summary>Lägger till en split i lagret.</summary>
    void Add(TrainingSplit split);

    /// <summary>
    /// Kopierar en splitmall till en användares data och sparar den automatiskt.
    /// </summary>
    TrainingSplit CopyForUser(TrainingSplitTemplate template, string userId);
}
