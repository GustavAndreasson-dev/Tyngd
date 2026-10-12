using Tyngd.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace Tyngd.Client.Services;

public class ClientErrorService
{
    private readonly HttpClient _http;
    private readonly NavigationManager _navigationManager;

    public ClientErrorService(HttpClient http, NavigationManager navigationManager)
    {
        _http = http;
        _navigationManager = navigationManager;
    }

    public async Task ReportAsync(Exception exception)
    {
        try
        {
            var report = new ClientErrorReportDto
            {
                Exception = SerializableExceptionDto.FromException(exception),
                Uri = _navigationManager.Uri,
                OccurredAt = DateTimeOffset.UtcNow,
            };

            using var response = await _http.PostAsJsonAsync("api/client-errors", report);
        }
        catch
        {
            // Error reporting must never create a second app error.
        }
    }
}
