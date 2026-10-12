using System.Net.Http.Json;
using Tyngd.Shared.DTOs;

namespace Tyngd.Client.Services;

public sealed class AdminService
{
    private readonly HttpClient _http;

    public AdminService(HttpClient http) => _http = http;

    public async Task<AdminOverviewDto> GetOverviewAsync() =>
        await _http.GetFromJsonAsync<AdminOverviewDto>("api/admin/overview") ?? new AdminOverviewDto();

    public async Task<PagedResultDto<AdminUserDto>> GetUsersAsync(string? search, int page, int pageSize = 25)
    {
        var query = $"api/admin/users?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            query += $"&search={Uri.EscapeDataString(search.Trim())}";
        return await _http.GetFromJsonAsync<PagedResultDto<AdminUserDto>>(query)
            ?? new PagedResultDto<AdminUserDto> { Page = page, PageSize = pageSize };
    }

    public async Task SetAdminRoleAsync(Guid userId, bool isAdmin)
    {
        var response = await _http.PutAsJsonAsync($"api/admin/users/{userId}/admin-role", new SetUserAdminRequest { IsAdmin = isAdmin });
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<AdminSplitTemplateDto>> GetTemplatesAsync() =>
        await _http.GetFromJsonAsync<List<AdminSplitTemplateDto>>("api/admin/templates?includeArchived=true") ?? new();

    public async Task<AdminSplitTemplateDto?> GetTemplateAsync(int templateId) =>
        await _http.GetFromJsonAsync<AdminSplitTemplateDto>($"api/admin/templates/{templateId}");

    public async Task<int> CreateTemplateAsync(UpsertAdminSplitTemplateRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/admin/templates", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<int>();
    }

    public async Task UpdateTemplateAsync(int templateId, UpsertAdminSplitTemplateRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/admin/templates/{templateId}", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task SetTemplateVisibilityAsync(int templateId, bool isVisible)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"api/admin/templates/{templateId}/visibility")
        {
            Content = JsonContent.Create(new SetVisibilityRequest { IsVisible = isVisible }),
        };
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
