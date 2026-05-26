using System.Net.Http.Json;

namespace ArnoldOne.Client.Services;

public class UserSplitsService
{
    private readonly HttpClient _http;

    public UserSplitsService(HttpClient http)
    {
        _http = http;
    }

    public async Task<HashSet<int>> GetActiveSplitsAsync(string userId)
    {
        var result = await _http.GetFromJsonAsync<List<int>>("api/UserSplit");
        return result is not null ? new HashSet<int>(result) : new HashSet<int>();
    }

    public async Task<bool> IsSplitActiveAsync(string userId, int splitId)
    {
        var active = await GetActiveSplitsAsync(userId);
        return active.Contains(splitId);
    }

    public async Task ActivateSplitAsync(string userId, int splitId)
    {
        await _http.PostAsync($"api/UserSplit/{splitId}", null);
    }

    public async Task DeactivateSplitAsync(string userId, int splitId)
    {
        await _http.DeleteAsync($"api/UserSplit/{splitId}");
    }
}

