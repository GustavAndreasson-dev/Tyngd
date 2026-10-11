using ArnoldOne.Shared.DTOs;
using System.Net.Http.Json;

namespace ArnoldOne.Client.Services;

public class PassLogService
{
    private readonly HttpClient _http;

    public PassLogService(HttpClient http)
    {
        _http = http;
    }

    public async Task SaveSessionAsync(SaveSessionRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/Session", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpsertExerciseAsync(UpsertExerciseRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/Session/exercise", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<LastSessionDto?> GetLastSessionAsync(int passId)
    {
        return await _http.GetFromJsonAsync<LastSessionDto>($"api/Session/last/{passId}");
    }

    public async Task<List<ExerciseProgressPointDto>> GetProgressAsync(int exerciseId) =>
        await _http.GetFromJsonAsync<List<ExerciseProgressPointDto>>($"api/Session/progress/{exerciseId}") ?? new();

    public async Task<List<ExerciseHistoryEntryDto>> GetCatalogHistoryAsync(int catalogExerciseId, int take = 12) =>
        await _http.GetFromJsonAsync<List<ExerciseHistoryEntryDto>>($"api/Session/history/{catalogExerciseId}?take={take}") ?? new();

    public async Task CompleteSessionAsync(Guid sessionKey)
    {
        var response = await _http.PostAsync($"api/Session/{sessionKey}/complete", null);
        response.EnsureSuccessStatusCode();
    }
}
