using System.Net.Http.Json;
using Tyngd.Shared.Catalog;

namespace Tyngd.Client.Services;

public sealed class AdminCatalogService
{
    private readonly HttpClient _http;

    public AdminCatalogService(HttpClient http) => _http = http;

    public async Task<List<AdminCatalogExerciseDto>> GetAllAsync() =>
        await _http.GetFromJsonAsync<List<AdminCatalogExerciseDto>>("api/admin/catalog") ?? new();

    /// <summary>Kör synk mot wger. Returnerar resultat eller felmeddelande från servern.</summary>
    public async Task<(CatalogSyncResultDto? Result, string? Error)> SyncAsync()
    {
        var response = await _http.PostAsync("api/admin/catalog/sync", null);
        if (response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<CatalogSyncResultDto>(), null);
        return (null, await response.Content.ReadAsStringAsync());
    }

    public async Task UpdateTranslationAsync(int catalogExerciseId, UpdateCatalogTranslationRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/admin/catalog/{catalogExerciseId}", request);
        response.EnsureSuccessStatusCode();
    }
}
