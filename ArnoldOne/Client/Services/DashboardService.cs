using System.Net.Http.Json;
using ArnoldOne.Shared.DTOs;

namespace ArnoldOne.Client.Services;

public sealed class DashboardService
{
    private readonly HttpClient _http;

    public DashboardService(HttpClient http) => _http = http;

    public async Task<UserDashboardDto> GetAsync() =>
        await _http.GetFromJsonAsync<UserDashboardDto>("api/dashboard") ?? new UserDashboardDto();
}
