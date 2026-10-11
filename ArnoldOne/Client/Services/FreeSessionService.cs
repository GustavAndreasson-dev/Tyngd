using System.Net.Http.Json;
using ArnoldOne.Shared.DTOs;

namespace ArnoldOne.Client.Services;

public sealed class FreeSessionService
{
    private readonly HttpClient _http;

    public FreeSessionService(HttpClient http) => _http = http;

    public async Task<StartFreeSessionResponse> StartAsync(StartFreeSessionRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/free-session", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StartFreeSessionResponse>())!;
    }

    public async Task<int> AddExerciseAsync(int passId, FreeSessionExerciseDto exercise)
    {
        var response = await _http.PostAsJsonAsync($"api/free-session/{passId}/exercises", exercise);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<int>();
    }

    public async Task<StartFreeSessionResponse> KeepAsync(int passId, KeepFreeSessionRequest request)
    {
        var response = await _http.PostAsJsonAsync($"api/free-session/{passId}/keep", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StartFreeSessionResponse>())!;
    }

    public async Task DiscardAsync(int passId)
    {
        var response = await _http.PostAsync($"api/free-session/{passId}/discard", null);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Standardnamn med lokalt datum, t.ex. "Löst pass 11 okt".</summary>
    public static string DefaultPassName(DateTime localNow) => $"Löst pass {SvDate.Short(localNow.Day, localNow.Month)}";
}
