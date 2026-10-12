using Tyngd.Shared.DTOs;
using System.Net.Http.Json;

namespace Tyngd.Client.Services;

public class PassTemplateService
{
    private readonly HttpClient _http;

    public PassTemplateService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<PassTemplateExerciseCountDto>> GetExerciseCountsAsync()
    {
        return await _http.GetFromJsonAsync<List<PassTemplateExerciseCountDto>>("api/PassTemplate/exercisecount")
               ?? new List<PassTemplateExerciseCountDto>();
    }
}
