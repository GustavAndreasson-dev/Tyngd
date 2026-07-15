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

    public event Func<Task>? SplitsChanged;

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
        var splitId = await response.Content.ReadFromJsonAsync<int>();
        await NotifySplitsChangedAsync();
        return splitId;
    }

    public async Task UpdateCustomSplitAsync(int splitId, CreateSplitRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/UserSplit/my/{splitId}", request);
        response.EnsureSuccessStatusCode();
        await NotifySplitsChangedAsync();
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
        await NotifySplitsChangedAsync();
    }

    public async Task<CreateSplitRequest?> GetMySplitFullAsync(int splitId)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var response = await _http.GetAsync($"api/UserSplit/my/{splitId}/full", cts.Token);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CreateSplitRequest>(cancellationToken: cts.Token);
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

    private async Task NotifySplitsChangedAsync()
    {
        if (SplitsChanged is null)
            return;

        foreach (Func<Task> handler in SplitsChanged.GetInvocationList())
            await handler();
    }
}
