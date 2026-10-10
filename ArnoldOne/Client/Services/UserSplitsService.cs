using ArnoldOne.Shared.DTOs;
using ArnoldOne.Shared.TrainingPrograms;
using System.Net.Http.Json;
using System.Diagnostics;

namespace ArnoldOne.Client.Services;

public class UserSplitsService
{
    private readonly HttpClient _http;
    private readonly LoginState _loginState;
    private readonly ILogger<UserSplitsService> _logger;
    private readonly Dictionary<(string UserId, int SplitId), CreateSplitRequest> _fullSplitCache = new();

    public UserSplitsService(HttpClient http, LoginState loginState, ILogger<UserSplitsService> logger)
    {
        _http = http;
        _loginState = loginState;
        _logger = logger;
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

    public async Task<TrainingPassTemplate?> GetPassAsync(int splitId, int passId)
    {
        try
        {
            return await _http.GetFromJsonAsync<TrainingPassTemplate>($"api/UserSplit/{splitId}/pass/{passId}");
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
        var response = await _http.PostAsync($"api/UserSplit/{splitId}", null);
        response.EnsureSuccessStatusCode();
        await NotifySplitsChangedAsync();
    }

    public async Task DeactivateSplitAsync(string userId, int splitId)
    {
        var response = await _http.DeleteAsync($"api/UserSplit/{splitId}");
        response.EnsureSuccessStatusCode();
        await NotifySplitsChangedAsync();
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
        RemoveCachedSplit(splitId);
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
        RemoveCachedSplit(splitId);
        await NotifySplitsChangedAsync();
    }

    public async Task<CreateSplitRequest?> GetMySplitFullAsync(int splitId)
    {
        var cacheKey = CurrentCacheKey(splitId);
        if (_fullSplitCache.TryGetValue(cacheKey, out var cached))
        {
            _logger.LogInformation("Editable split {SplitId} loaded from client cache", splitId);
            return Clone(cached);
        }

        var started = Stopwatch.GetTimestamp();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var response = await _http.GetAsync($"api/UserSplit/my/{splitId}/full", cts.Token);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateSplitRequest>(cancellationToken: cts.Token);
        if (result is null)
            return null;

        _fullSplitCache[cacheKey] = Clone(result);
        _logger.LogInformation(
            "Editable split {SplitId} fetched in {ElapsedMilliseconds} ms; {ServerTiming}",
            splitId,
            Stopwatch.GetElapsedTime(started).TotalMilliseconds,
            response.Headers.TryGetValues("Server-Timing", out var values) ? string.Join(',', values) : "no Server-Timing");
        return Clone(result);
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
                CatalogExerciseId = e.CatalogExerciseId,
                DefaultSets   = e.DefaultSets,
                RepsMin       = e.DefaultRepsMin,
                RepsMax       = e.DefaultRepsMax,
                DefaultWeight = e.DefaultWeight,
            }).ToList(),
        }).ToList(),
    };

    public static CreateSplitRequest AsNewCopy(CreateSplitRequest source) => new()
    {
        SplitName = source.SplitName,
        Passes = source.Passes.Select(pass => new CreatePassDto
        {
            PassName = pass.PassName,
            Exercises = pass.Exercises.Select(exercise => new CreateExerciseDto
            {
                ExerciseName = exercise.ExerciseName,
                CatalogExerciseId = exercise.CatalogExerciseId,
                DefaultSets = exercise.DefaultSets,
                RepsMin = exercise.RepsMin,
                RepsMax = exercise.RepsMax,
                DefaultWeight = exercise.DefaultWeight,
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

    private (string UserId, int SplitId) CurrentCacheKey(int splitId) =>
        (_loginState.CurrentUser?.UserId ?? string.Empty, splitId);

    private void RemoveCachedSplit(int splitId) => _fullSplitCache.Remove(CurrentCacheKey(splitId));

    private static CreateSplitRequest Clone(CreateSplitRequest source) => new()
    {
        SplitName = source.SplitName,
        Passes = source.Passes.Select(pass => new CreatePassDto
        {
            PassId = pass.PassId,
            PassName = pass.PassName,
            Exercises = pass.Exercises.Select(exercise => new CreateExerciseDto
            {
                ExerciseId = exercise.ExerciseId,
                ExerciseName = exercise.ExerciseName,
                CatalogExerciseId = exercise.CatalogExerciseId,
                DefaultSets = exercise.DefaultSets,
                RepsMin = exercise.RepsMin,
                RepsMax = exercise.RepsMax,
                DefaultWeight = exercise.DefaultWeight,
            }).ToList(),
        }).ToList(),
    };
}
