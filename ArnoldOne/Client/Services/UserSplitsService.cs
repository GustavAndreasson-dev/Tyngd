using ArnoldOne.Shared.DTOs;
using ArnoldOne.Shared.TrainingPrograms;
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

    public async Task<List<TrainingSplit>> GetAllActiveSplitsAsync()
    {
        return await _http.GetFromJsonAsync<List<TrainingSplit>>("api/UserSplit/active")
               ?? new List<TrainingSplit>();
    }

    public async Task<TrainingPassTemplate?> GetPassAsync(int passId)
    {
        try
        {
            return await _http.GetFromJsonAsync<TrainingPassTemplate>($"api/UserSplit/pass/{passId}");
        }
        catch { return null; }
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

    public async Task<int> CreateCustomSplitAsync(CreateSplitRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/UserSplit/custom", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<int>();
    }

    public async Task<List<TrainingSplit>> GetMySplitsAsync()
    {
        return await _http.GetFromJsonAsync<List<TrainingSplit>>("api/UserSplit/my")
               ?? new List<TrainingSplit>();
    }

    public async Task DeleteMySplitAsync(int splitId)
    {
        var response = await _http.DeleteAsync($"api/UserSplit/my/{splitId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<CreateSplitRequest?> GetMySplitFullAsync(int splitId)
    {
        return await _http.GetFromJsonAsync<CreateSplitRequest>($"api/UserSplit/my/{splitId}/full");
    }

    public static CreateSplitRequest FromTemplate(TrainingSplitTemplate template) => new()
    {
        SplitName = template.SplitName,
        Passes = template.Passes.Select(p => new CreatePassDto
        {
            PassName  = p.PassName,
            Exercises = p.Exercises.Select(e => new CreateExerciseDto
            {
                ExerciseName  = e.Exercise.ExerciseName,
                DefaultSets   = e.DefaultSets,
                RepsMin       = e.DefaultRepsMin,
                RepsMax       = e.DefaultRepsMax,
                DefaultWeight = e.DefaultWeight,
            }).ToList(),
        }).ToList(),
    };
}

