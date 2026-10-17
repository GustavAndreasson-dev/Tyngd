using Tyngd.Client.Models;
using Tyngd.Shared.DTOs;

namespace Tyngd.Client.Services;

/// <summary>Håller reda på om det finns lokal passdata som ännu inte har synkats till API:t.</summary>
public class PendingSyncState
{
    private readonly IAppStorage _localStorage;
    private readonly PassLogService _passLogService;
    private readonly HashSet<string> _pendingPassKeys = new(StringComparer.Ordinal);
    private string? _activePassKey;

    public PendingSyncState(IAppStorage localStorage, PassLogService passLogService)
    {
        _localStorage = localStorage;
        _passLogService = passLogService;
    }

    public bool HasPendingSync => _pendingPassKeys.Count > 0;
    public bool HasVisiblePendingSync => _pendingPassKeys.Any(key => key != _activePassKey);

    public event Action? OnChange;

    public void SetActivePassKey(string passKey)
    {
        if (_activePassKey == passKey)
            return;

        _activePassKey = passKey;
        OnChange?.Invoke();
    }

    public void ClearActivePassKey(string passKey)
    {
        if (_activePassKey != passKey)
            return;

        _activePassKey = null;
        OnChange?.Invoke();
    }

    public async Task RefreshAsync()
    {
        var pendingPassKeys = new HashSet<string>(StringComparer.Ordinal);
        var keys = await _localStorage.KeysAsync();

        foreach (var key in keys.Where(k => k.StartsWith("pass-", StringComparison.Ordinal)))
        {
            var data = await _localStorage.GetItemAsync<PassData>(key);
            data?.NormalizeSyncState();
            if (data is not null && !data.IsSynced)
                pendingPassKeys.Add(key);
        }

        if (_pendingPassKeys.SetEquals(pendingPassKeys))
            return;

        _pendingPassKeys.Clear();
        foreach (var key in pendingPassKeys)
            _pendingPassKeys.Add(key);

        OnChange?.Invoke();
    }

    public async Task SyncVisiblePendingAsync()
    {
        var keysToSync = _pendingPassKeys
            .Where(key => key != _activePassKey)
            .ToList();

        foreach (var key in keysToSync)
        {
            if (!TryGetPassId(key, out var passId))
                continue;

            var data = await _localStorage.GetItemAsync<PassData>(key);
            data?.NormalizeSyncState();
            if (data is null || data.IsSynced)
                continue;

            try
            {
                data.NormalizeSessionIdentity(DateTimeOffset.UtcNow);
                await _passLogService.SaveSessionAsync(CreateSaveSessionRequest(passId, data, completeSession: false));
                MarkSaved(data);
                await _localStorage.SetItemAsync(key, data);
            }
            catch (HttpRequestException)
            {
            }
        }

        await RefreshAsync();
    }

    public async Task FinalizeAsync(string passKey)
    {
        if (!TryGetPassId(passKey, out var passId))
            throw new InvalidOperationException("Passnyckeln är ogiltig.");

        var data = await _localStorage.GetItemAsync<PassData>(passKey)
            ?? throw new InvalidOperationException("Det finns ingen lokal passdata att slutföra.");

        data.NormalizeSyncState();
        data.NormalizeSessionIdentity(DateTimeOffset.UtcNow);
        await _passLogService.SaveSessionAsync(CreateSaveSessionRequest(passId, data, completeSession: true));
        MarkSaved(data);
        data.CompletedAtUtc = DateTimeOffset.UtcNow;
        await _localStorage.SetItemAsync(passKey, data);
        await RefreshAsync();
    }

    /// <summary>Hela passet som det ligger lokalt, för synk och för att avsluta passet.</summary>
    public static SaveSessionRequest CreateSaveSessionRequest(int passId, PassData data, bool completeSession)
    {
        var exerciseIds = data.Reps.Keys
            .Concat(data.Notes.Keys)
            .Concat(data.Weights.Keys)
            .Concat(data.CompletedSets.Keys)
            .Distinct()
            .OrderBy(id => id);

        return new SaveSessionRequest
        {
            SessionKey = data.SessionKey,
            PassId = passId,
            Date   = data.LocalDate,
            StartedAtUtc = data.StartedAtUtc!.Value,
            CompleteSession = completeSession,
            Exercises = exerciseIds.Select(exerciseId => CreateExerciseDto(data, exerciseId)).ToList(),
        };
    }

    /// <summary>En övnings anteckning, vikt och set som de ligger lokalt.</summary>
    public static SaveSessionExerciseDto CreateExerciseDto(PassData data, int exerciseId)
    {
        data.Reps.TryGetValue(exerciseId, out var reps);
        data.CompletedSets.TryGetValue(exerciseId, out var completedSets);
        data.Weights.TryGetValue(exerciseId, out var weight);
        data.Notes.TryGetValue(exerciseId, out var note);

        var setCount = Math.Max(reps?.Length ?? 0, completedSets?.Length ?? 0);

        return new SaveSessionExerciseDto
        {
            ExerciseId = exerciseId,
            NoteText   = note ?? string.Empty,
            Weight     = weight,
            Sets       = Enumerable.Range(0, setCount).Select(index => new SaveSessionSetDto
            {
                SetIndex = index,
                Reps     = (int)(reps is not null && index < reps.Length ? reps[index] : 0),
                IsDone   = completedSets is not null && index < completedSets.Length && completedSets[index],
                Weight   = weight,
            }).ToList(),
        };
    }

    private static void MarkSaved(PassData data)
    {
        data.IsSynced = true;
        foreach (var exerciseId in data.Reps.Keys
            .Concat(data.Notes.Keys)
            .Concat(data.Weights.Keys)
            .Concat(data.CompletedSets.Keys)
            .Distinct())
        {
            data.SavedExercises[exerciseId] = true;
        }
    }

    private static bool TryGetPassId(string key, out int passId)
    {
        passId = 0;
        var parts = key.Split('-', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 3 && int.TryParse(parts[2], out passId);
    }
}
