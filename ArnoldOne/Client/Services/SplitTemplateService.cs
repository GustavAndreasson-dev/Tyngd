using ArnoldOne.Shared.TrainingPrograms;
using System.Net.Http.Json;

namespace ArnoldOne.Client.Services;

public class SplitTemplateService
{
    private readonly HttpClient _http;

    public SplitTemplateService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<TrainingSplitTemplate>> GetAllAsync()
    {
        return await _http.GetFromJsonAsync<List<TrainingSplitTemplate>>("api/SplitTemplate")
               ?? new List<TrainingSplitTemplate>();
    }

    public async Task<TrainingPassTemplate?> GetPassAsync(int splitId, int passId)
    {
        return await _http.GetFromJsonAsync<TrainingPassTemplate>($"api/SplitTemplate/{splitId}/pass/{passId}");
    }
}
