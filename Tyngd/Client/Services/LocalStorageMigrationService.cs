using Blazored.LocalStorage;

namespace Tyngd.Client.Services;

/// <summary>
/// Hanterar engångsmigrering av localStorage-nycklar mellan versioner.
/// Körs vid appstart och är idempotent – en version migreras aldrig mer än en gång.
/// </summary>
public class LocalStorageMigrationService
{
    private const string SchemaVersionKey = "schema-version";
    private const int CurrentVersion = 2;

    private readonly ILocalStorageService _localStorage;

    public LocalStorageMigrationService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    /// <summary>
    /// Kör alla migrationer som inte ännu körts och uppdaterar schema-versionen.
    /// </summary>
    public async Task MigrateAsync()
    {
        var version = await _localStorage.GetItemAsync<int?>(SchemaVersionKey) ?? 1;

        if (version < 2)
            await MigrateV1ToV2Async();

        await _localStorage.SetItemAsync(SchemaVersionKey, CurrentVersion);
    }

    /// <summary>
    /// Version 1 → 2: PPL1 PassId ändrades från 1–4 till 5–8.
    /// Kopierar passdata från gamla nycklar till nya om de inte redan finns.
    /// SplitId för PPL1 är 2.
    /// </summary>
    private async Task MigrateV1ToV2Async()
    {
        var passIdMap = new Dictionary<int, int>
        {
            [1] = 5,
            [2] = 6,
            [3] = 7,
            [4] = 8,
        };

        const int ppl1SplitId = 2;

        foreach (var (oldPassId, newPassId) in passIdMap)
        {
            var oldKey = $"pass-{ppl1SplitId}-{oldPassId}";
            var newKey = $"pass-{ppl1SplitId}-{newPassId}";

            var oldData = await _localStorage.GetItemAsStringAsync(oldKey);

            if (oldData is not null && !await _localStorage.ContainKeyAsync(newKey))
                await _localStorage.SetItemAsStringAsync(newKey, oldData);
        }
    }
}
