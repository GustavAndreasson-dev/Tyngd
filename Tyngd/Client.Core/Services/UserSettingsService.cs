using Tyngd.Shared.Users;
using System.Net.Http.Json;

namespace Tyngd.Client.Services;

public class UserSettingsService
{
    private readonly HttpClient _http;

    public UserSettingsService(HttpClient http)
    {
        _http = http;
    }

    public async Task<UserSettings?> GetSettingsAsync()
    {
        try
        {
            var response = await _http.GetAsync("api/UserSettings");
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<UserSettings>();
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveSettingsAsync(UserSettings settings)
    {
        var response = await _http.PutAsJsonAsync("api/UserSettings", settings);
        response.EnsureSuccessStatusCode();
    }
}
