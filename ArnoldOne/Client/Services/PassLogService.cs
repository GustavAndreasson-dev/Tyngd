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
}
