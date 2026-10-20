using Tyngd.Shared.DTOs;
using System.Net.Http.Json;

namespace Tyngd.Client.Services;

public class ClientErrorService
{
    private readonly HttpClient _http;

    public ClientErrorService(HttpClient http)
    {
        _http = http;
    }

    /// <summary>Skickar felet till servern. <paramref name="location"/> är sidans adress i webben.</summary>
    public async Task ReportAsync(Exception exception, string? location)
    {
        try
        {
            var report = new ClientErrorReportDto
            {
                Exception = SerializableExceptionDto.FromException(exception),
                Uri = location ?? string.Empty,
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
