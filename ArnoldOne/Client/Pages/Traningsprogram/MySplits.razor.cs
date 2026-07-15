using ArnoldOne.Client.Services;
using ArnoldOne.Client.Shared;
using ArnoldOne.Shared.DTOs;
using ArnoldOne.Shared.TrainingPrograms;
using Microsoft.AspNetCore.Components;

namespace ArnoldOne.Client.Pages.Traningsprogram;

public partial class MySplits : IDisposable
{
    [Inject]
    private LoginState LoginState { get; set; } = default!;

    [Inject]
    private UserSplitsService UserSplitsService { get; set; } = default!;

    private List<TrainingSplit> splits = new();
    private string? successMessage;

    // (splitId, passId) -> övningar
    private Dictionary<(int, int), List<CreateExerciseDto>> passExercises = new();

    protected override async Task OnInitializedAsync()
    {
        LoginState.OnChange += OnLoginStateChanged;
        await LoadAsync();
    }

    private async void OnLoginStateChanged()
    {
        try
        {
            await LoadAsync();
            await InvokeAsync(StateHasChanged);
        }
        catch { }
    }

    private async Task LoadAsync()
    {
        if (!LoginState.IsLoggedIn)
        {
            splits = new();
            passExercises = new();
            return;
        }

        await TryAsync(async () =>
        {
            splits = await UserSplitsService.GetMySplitsAsync();
            passExercises = new();

            foreach (var split in splits)
            {
                var full = await UserSplitsService.GetMySplitFullAsync(split.SplitId);
                if (full is null) continue;

                for (int i = 0; i < split.Passes.Count; i++)
                {
                    var pass = split.Passes[i];
                    var exercises = full.Passes.ElementAtOrDefault(i)?.Exercises ?? new();
                    passExercises[(split.SplitId, pass.PassId)] = exercises;
                }
            }
        }, "Kunde inte ladda splits");
    }

    private async Task DeleteSplit(int splitId)
    {
        await TryAsync(async () =>
        {
            await UserSplitsService.DeleteMySplitAsync(splitId);
            splits.RemoveAll(s => s.SplitId == splitId);
            foreach (var key in passExercises.Keys.Where(k => k.Item1 == splitId).ToList())
                passExercises.Remove(key);

            successMessage = "Splitten har tagits bort.";
        }, "Kunde inte ta bort split");
    }

    private static string FormatDefault(CreateExerciseDto exercise)
    {
        var reps = exercise.RepsMin == exercise.RepsMax
            ? exercise.RepsMin.ToString()
            : $"{exercise.RepsMin}-{exercise.RepsMax}";

        var weight = exercise.DefaultWeight == 0
            ? "kroppsvikt"
            : $"{exercise.DefaultWeight:0.##} kg";

        return $"{exercise.DefaultSets} x {reps} @ {weight}";
    }

    public void Dispose() => LoginState.OnChange -= OnLoginStateChanged;

    private void ClearSuccess() => successMessage = null;
}
