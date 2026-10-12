using Tyngd.Client.Services;
using Tyngd.Client.Shared;
using Tyngd.Shared.DTOs;
using Tyngd.Shared.TrainingPrograms;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Diagnostics;

namespace Tyngd.Client.Pages.Traningsprogram;

public partial class CreateSplit : PageComponentBase
{
    [Inject]
    private LoginState LoginState { get; set; } = default!;

    [Inject]
    private UserSplitsService UserSplitsService { get; set; } = default!;

    [Inject]
    private SplitTemplateService SplitTemplateService { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    [Inject]
    private ILogger<CreateSplit> Logger { get; set; } = default!;

    [Parameter]
    public int? SplitId { get; set; }

    private bool IsEditMode => SplitId.HasValue;

    private string splitName = string.Empty;
    private List<CreatePassDto> passes = new() { new CreatePassDto() };

    private List<TrainingSplitTemplate> allTemplates = new();
    private List<TrainingSplit> mySplits = new();
    private bool isImporting = false;
    private bool splitExpanded = true;
    private List<bool> passExpanded = new() { true };
    private List<List<bool>> exerciseExpanded = new() { new List<bool>() };
    private string? successMessage;
    private bool isLoadingEditSplit;
    private bool isSaving;
    private Dictionary<string, string> validationErrors = new();
    private long? editLoadStartedTimestamp;
    private bool measureNextEditRender;
    private bool CanReorder =>
        passes.Count > 1 || passes.Any(pass => pass.Exercises.Count > 1);

    protected override async Task OnInitializedAsync()
    {
        if (!LoginState.IsLoggedIn) return;

        if (SplitId.HasValue)
        {
            editLoadStartedTimestamp = Stopwatch.GetTimestamp();
            isLoadingEditSplit = true;
            await TryAsync(() => LoadSplitForEditAsync(SplitId.Value), "Kunde inte ladda splitten");
            isLoadingEditSplit = false;
            measureNextEditRender = true;
            StateHasChanged();
            return;
        }

        await TryAsync(async () =>
        {
            await LoadImportDataAsync();
        }, "Failed to load splits");
    }

    private async Task LoadImportDataAsync()
    {
        allTemplates = await SplitTemplateService.GetAllAsync();
        mySplits     = await UserSplitsService.GetMySplitsAsync();
    }

    private void AddPass()
    {
        for (var index = 0; index < passExpanded.Count; index++)
            passExpanded[index] = false;
        passes.Add(new CreatePassDto());
        passExpanded.Add(true);
        exerciseExpanded.Add(new List<bool>());
    }

    private void RemovePass(int index)
    {
        passes.RemoveAt(index);
        passExpanded.RemoveAt(index);
        exerciseExpanded.RemoveAt(index);
    }

    private void AddExercise(int passIndex)
    {
        for (var index = 0; index < exerciseExpanded[passIndex].Count; index++)
            exerciseExpanded[passIndex][index] = false;
        passes[passIndex].Exercises.Add(new CreateExerciseDto());
        exerciseExpanded[passIndex].Add(true);
    }

    private void RemoveExercise(int passIndex, int exerciseIndex)
    {
        passes[passIndex].Exercises.RemoveAt(exerciseIndex);
        exerciseExpanded[passIndex].RemoveAt(exerciseIndex);
    }

    private async Task OpenReorderDialogAsync()
    {
        if (!CanReorder)
            return;

        var parameters = new DialogParameters
        {
            { nameof(ReorderSplitDialog.Passes), passes },
        };
        var options = new DialogOptions
        {
            FullWidth = true,
            MaxWidth = MaxWidth.Medium,
            CloseOnEscapeKey = true,
        };

        var dialog = await DialogService.ShowAsync<ReorderSplitDialog>("", parameters, options);
        var result = await dialog.Result;

        if (result is null
            || result.Canceled
            || result.Data is not List<CreatePassDto> reorderedPasses)
            return;

        passes = reorderedPasses;
        validationErrors.Clear();
        ResetExpandedState();
    }

    private IDialogReference? _importDialog;

    private async Task OpenImportDialog()
    {
        var parameters = new DialogParameters
        {
            { nameof(ImportSplitDialog.MySplits),  mySplits     },
            { nameof(ImportSplitDialog.Templates), allTemplates },
            { nameof(ImportSplitDialog.OnSelected), EventCallback.Factory.Create<ImportSplitResult>(this, OnImportSelected) },
        };

        _importDialog = await DialogService.ShowAsync<ImportSplitDialog>("", parameters);
    }

    private async Task OnImportSelected(ImportSplitResult import)
    {
        isImporting = true;
        _importDialog?.Close();
        StateHasChanged();
        await Task.Yield();

        try
        {
            if (import.MySplit is not null)
                await ImportFromMySplitAsync(import.MySplit);
            else if (import.Template is not null)
                ImportFromTemplate(import.Template);
        }
        finally
        {
            isImporting = false;
        }
    }

    private void ImportFromTemplate(TrainingSplitTemplate template)
    {
        var imported = UserSplitsService.FromTemplate(template);
        splitName    = imported.SplitName;
        passes       = imported.Passes;
        ResetExpandedState();
    }

    private async Task ImportFromMySplitAsync(TrainingSplit split)
    {
        await TryAsync(async () =>
        {
            var imported = await UserSplitsService.GetMySplitFullAsync(split.SplitId);
            if (imported is null) return;
            var copy = UserSplitsService.AsNewCopy(imported);
            splitName    = copy.SplitName;
            passes       = copy.Passes;
            ResetExpandedState();
        }, "Failed to fetch split");
    }

    private async Task LoadSplitForEditAsync(int splitId)
    {
        var apiStarted = Stopwatch.GetTimestamp();
        var imported = await UserSplitsService.GetMySplitFullAsync(splitId);
        Logger.LogInformation(
            "Edit split {SplitId}: data loaded in {ElapsedMilliseconds} ms",
            splitId,
            Stopwatch.GetElapsedTime(apiStarted).TotalMilliseconds);
        if (imported is null)
        {
            Nav.NavigateTo("/TP/my-splits");
            return;
        }

        splitName = imported.SplitName;
        passes = imported.Passes;
        ResetExpandedState();
    }

    private async Task SaveAsync()
    {
        if (isSaving || isLoadingEditSplit) return;

        var request = new CreateSplitRequest
        {
            SplitName = splitName.Trim(),
            Passes    = passes,
        };

        validationErrors = SplitRequestValidator.Validate(request)
            .GroupBy(error => error.Key)
            .ToDictionary(group => group.Key, group => group.First().Message);

        if (validationErrors.Count > 0)
        {
            ExpandFirstValidationError();
            StateHasChanged();
            return;
        }

        isSaving = true;
        await TryAsync(async () =>
        {
            if (SplitId.HasValue)
            {
                await UserSplitsService.UpdateCustomSplitAsync(SplitId.Value, request);
                Nav.NavigateTo("/TP/my-splits?saved=updated");
            }
            else
            {
                await UserSplitsService.CreateCustomSplitAsync(request);
                Nav.NavigateTo("/TP/my-splits?saved=created");
            }
        }, "Failed to save split");
        isSaving = false;
        StateHasChanged();
    }

    private void ResetExpandedState()
    {
        splitExpanded = true;
        passExpanded = passes.Select(_ => false).ToList();
        exerciseExpanded = passes
            .Select(p => p.Exercises.Select(_ => false).ToList())
            .ToList();
    }

    private void TogglePass(int passIndex)
    {
        var shouldExpand = !passExpanded[passIndex];
        for (var index = 0; index < passExpanded.Count; index++)
            passExpanded[index] = false;
        passExpanded[passIndex] = shouldExpand;
    }

    private void ToggleExercise(int passIndex, int exerciseIndex)
    {
        var shouldExpand = !exerciseExpanded[passIndex][exerciseIndex];
        for (var index = 0; index < exerciseExpanded[passIndex].Count; index++)
            exerciseExpanded[passIndex][index] = false;
        exerciseExpanded[passIndex][exerciseIndex] = shouldExpand;
    }

    private void ExpandFirstValidationError()
    {
        for (var passIndex = 0; passIndex < passes.Count; passIndex++)
        {
            var passPrefix = $"Passes[{passIndex}]";
            if (!validationErrors.Keys.Any(key => key.StartsWith(passPrefix, StringComparison.Ordinal)))
                continue;

            for (var index = 0; index < passExpanded.Count; index++)
                passExpanded[index] = false;
            passExpanded[passIndex] = true;

            for (var exerciseIndex = 0; exerciseIndex < passes[passIndex].Exercises.Count; exerciseIndex++)
            {
                var exercisePrefix = $"{passPrefix}.Exercises[{exerciseIndex}]";
                if (!validationErrors.Keys.Any(key => key.StartsWith(exercisePrefix, StringComparison.Ordinal)))
                    continue;

                for (var index = 0; index < exerciseExpanded[passIndex].Count; index++)
                    exerciseExpanded[passIndex][index] = false;
                exerciseExpanded[passIndex][exerciseIndex] = true;
                break;
            }

            break;
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (!measureNextEditRender || editLoadStartedTimestamp is not long started)
            return;

        measureNextEditRender = false;
        Logger.LogInformation(
            "Edit split {SplitId}: first usable render completed in {ElapsedMilliseconds} ms",
            SplitId,
            Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }

    private void ClearSuccess() => successMessage = null;

    private string? FieldError(string key) =>
        validationErrors.TryGetValue(key, out var error) ? error : null;
}
