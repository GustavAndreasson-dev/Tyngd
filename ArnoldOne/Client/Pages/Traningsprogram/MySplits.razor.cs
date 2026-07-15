using ArnoldOne.Client.Services;
using ArnoldOne.Client.Shared;
using ArnoldOne.Shared.DTOs;
using ArnoldOne.Shared.TrainingPrograms;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ArnoldOne.Client.Pages.Traningsprogram;

public partial class MySplits : IDisposable
{
    [Inject]
    private LoginState LoginState { get; set; } = default!;

    [Inject]
    private UserSplitsService UserSplitsService { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "saved")]
    public string? SavedStatus { get; set; }

    private List<TrainingSplit> splits = new();
    private string? successMessage;

    // (splitId, passId) -> övningar
    private Dictionary<(int, int), List<CreateExerciseDto>> passExercises = new();

    protected override async Task OnInitializedAsync()
    {
        LoginState.OnChange += OnLoginStateChanged;
        successMessage = SavedStatus switch
        {
            "created" => "Splitten har skapats.",
            "updated" => "Ändringarna har sparats.",
            _ => null,
        };
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

                foreach (var pass in split.Passes)
                {
                    var exercises = full.Passes
                        .FirstOrDefault(candidate => candidate.PassId == pass.PassId)?
                        .Exercises ?? new();
                    passExercises[(split.SplitId, pass.PassId)] = exercises;
                }
            }
        }, "Kunde inte ladda splits");
    }

    private async Task DeleteSplit(int splitId)
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Arkivera split",
            "Splitten döljs från nya träningspass, men träningshistoriken bevaras. Vill du fortsätta?",
            yesText: "Arkivera",
            cancelText: "Avbryt");

        if (confirmed != true)
            return;

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
