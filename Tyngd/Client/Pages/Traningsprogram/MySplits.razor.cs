using Tyngd.Client.Services;
using Tyngd.Client.Shared;
using Tyngd.Shared.DTOs;
using Tyngd.Shared.TrainingPrograms;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Tyngd.Client.Pages.Traningsprogram;

public partial class MySplits : IDisposable
{
    [Inject]
    private LoginState LoginState { get; set; } = default!;

    [Inject]
    private UserSplitsService UserSplitsService { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "saved")]
    public string? SavedStatus { get; set; }

    private List<TrainingSplit> splits = new();
    private string? successMessage;

    // (splitId, passId) -> övningar
    private Dictionary<(int, int), List<CreateExerciseDto>> passExercises = new();
    private HashSet<(int, int)> loadingPasses = new();
    private Dictionary<(int, int), Dictionary<string, double>> passMuscles = new();
    // null = laddas
    private Dictionary<int, Dictionary<string, double>?> splitMuscles = new();

    [Inject]
    private CatalogService CatalogService { get; set; } = default!;

    private async Task ToggleSplitMusclesAsync(int splitId)
    {
        if (splitMuscles.Remove(splitId))
            return;

        splitMuscles[splitId] = null;
        try
        {
            splitMuscles[splitId] = await UserSplitsService.GetSplitMusclesAsync(splitId);
        }
        catch (Exception exception)
        {
            splitMuscles.Remove(splitId);
            Snackbar.Add($"Kunde inte ladda muskler: {exception.Message}", Severity.Error);
        }
    }

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
            passMuscles = new();
            splitMuscles = new();
            return;
        }

        await TryAsync(async () =>
        {
            // Egna splits och aktiverade mallar; lösa pass ingår inte.
            splits = await UserSplitsService.GetAllActiveSplitsAsync();
            passExercises = new();
            passMuscles = new();
            splitMuscles = new();
        }, "Kunde inte ladda splits");
    }

    private async Task OnPassExpandedAsync(int splitId, int passId, bool expanded)
    {
        var key = (splitId, passId);
        if (!expanded || passExercises.ContainsKey(key) || !loadingPasses.Add(key))
            return;

        StateHasChanged();
        try
        {
            var full = await UserSplitsService.GetMySplitFullAsync(splitId);
            if (full is null)
                return;

            var catalog = await CatalogService.GetAllAsync();
            foreach (var pass in full.Passes)
            {
                passExercises[(splitId, pass.PassId ?? 0)] = pass.Exercises;
                passMuscles[(splitId, pass.PassId ?? 0)] = CatalogService.MuscleLoadFor(catalog, pass.Exercises.Select(e => e.CatalogExerciseId));
            }
        }
        catch (Exception exception)
        {
            Snackbar.Add($"Kunde inte ladda övningarna: {exception.Message}", Severity.Error);
        }
        finally
        {
            loadingPasses.Remove(key);
            StateHasChanged();
        }
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

    private async Task DeactivateTemplate(TrainingSplit split)
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Avaktivera mall",
            $"{split.SplitName} försvinner från översikten, men träningshistoriken bevaras. Du kan aktivera mallen igen under Split-mallar.",
            yesText: "Avaktivera",
            cancelText: "Avbryt");

        if (confirmed != true)
            return;

        await TryAsync(async () =>
        {
            await UserSplitsService.DeactivateSplitAsync(LoginState.CurrentUser!.UserId, split.SplitTemplateId!.Value);
            splits.Remove(split);
            successMessage = "Mallen har avaktiverats.";
        }, "Kunde inte avaktivera mallen");
    }

    public void Dispose() => LoginState.OnChange -= OnLoginStateChanged;

    private void ClearSuccess() => successMessage = null;
}
