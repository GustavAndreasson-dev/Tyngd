using ArnoldOne.Client.Services;
using ArnoldOne.Client.Shared;
using ArnoldOne.Shared.TrainingPrograms;
using Microsoft.AspNetCore.Components;

namespace ArnoldOne.Client.Pages.Traningsprogram;

public partial class SplitTemplates : IDisposable
{
    [Inject]
    private LoginState LoginState { get; set; } = default!;

    [Inject]
    private UserSplitsService UserSplitsService { get; set; } = default!;

    [Inject]
    private SplitTemplateService SplitTemplateService { get; set; } = default!;

    private IReadOnlyList<TrainingSplitTemplate> templates = new List<TrainingSplitTemplate>();

    private HashSet<int> activeSplits = new();

    protected override async Task OnInitializedAsync()
    {
        LoginState.OnChange += OnLoginStateChanged;
        await TryAsync(async () =>
        {
            templates = await SplitTemplateService.GetAllAsync();
            await LoadActiveSplitsAsync();
        }, "Kunde inte ladda splitmallar");
    }

    private async void OnLoginStateChanged()
    {
        try
        {
            await LoadActiveSplitsAsync();
            await InvokeAsync(StateHasChanged);
        }
        catch { }
    }

    private async Task LoadActiveSplitsAsync()
    {
        if (!LoginState.IsLoggedIn)
        {
            activeSplits = new HashSet<int>();
            return;
        }
        try
        {
            activeSplits = await UserSplitsService.GetActiveSplitsAsync(LoginState.CurrentUser!.UserId);
        }
        catch { }
    }

    public void Dispose() => LoginState.OnChange -= OnLoginStateChanged;

    private async Task ActivateSplit(int splitId)
    {
        if (!LoginState.IsLoggedIn) return;
        await TryAsync(async () =>
        {
            await UserSplitsService.ActivateSplitAsync(LoginState.CurrentUser!.UserId, splitId);
            activeSplits.Add(splitId);
        }, "Kunde inte aktivera split");
    }

    private async Task DeactivateSplit(int splitId)
    {
        if (!LoginState.IsLoggedIn) return;
        await TryAsync(async () =>
        {
            await UserSplitsService.DeactivateSplitAsync(LoginState.CurrentUser!.UserId, splitId);
            activeSplits.Remove(splitId);
        }, "Kunde inte avaktivera split");
    }

    /// <summary>Formaterar defaultvärden för en övningsmall som läsbar text.</summary>
    private static string FormatDefault(TrainingExerciseTemplate exercise)
    {
        var reps = exercise.DefaultRepsMin == exercise.DefaultRepsMax
            ? exercise.DefaultRepsMin.ToString()
            : $"{exercise.DefaultRepsMin}-{exercise.DefaultRepsMax}";

        var weight = exercise.DefaultWeight == 0
            ? "kroppsvikt"
            : $"{exercise.DefaultWeight:0.#} kg";

        return $"{exercise.DefaultSets} x {reps} @ {weight}";
    }
}
