using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tyngd.Server.Catalog;

/// <summary>
/// Incheckade svenska översättningar (wger-sv.json), nycklade på wger-uuid. Används när synken
/// möter en övning utan svensk text och källtexten fortfarande matchar den som översattes.
/// </summary>
public sealed class CatalogSeed
{
    private readonly Dictionary<Guid, SeedEntry> _entries;

    public CatalogSeed(IEnumerable<SeedEntry> entries) =>
        _entries = entries.ToDictionary(entry => entry.Uuid);

    public static CatalogSeed LoadEmbedded()
    {
        using var stream = typeof(CatalogSeed).Assembly.GetManifestResourceStream("Tyngd.Server.Catalog.wger-sv.json")
            ?? throw new InvalidOperationException("wger-sv.json saknas som embedded resource.");
        return new CatalogSeed(JsonSerializer.Deserialize<List<SeedEntry>>(stream) ?? []);
    }

    public int Count => _entries.Count;

    /// <summary>Returnerar översättningen bara om den gjordes från exakt samma källtext.</summary>
    public SeedEntry? Find(Guid uuid, string sourceHash) =>
        _entries.TryGetValue(uuid, out var entry) && entry.SourceHash == sourceHash ? entry : null;

    public sealed record SeedEntry(
        [property: JsonPropertyName("uuid")] Guid Uuid,
        [property: JsonPropertyName("sourceHash")] string SourceHash,
        [property: JsonPropertyName("nameSv")] string NameSv,
        [property: JsonPropertyName("descriptionSv")] string DescriptionSv);
}
