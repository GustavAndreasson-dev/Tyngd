using ArnoldOne.Client.Services;
using ArnoldOne.Client.Shared;
using ArnoldOne.Shared.DTOs;
using ArnoldOne.Shared.TrainingPrograms;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ArnoldOne.Client.Pages.Traningsprogram;

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

    protected override async Task OnInitializedAsync()
    {
        if (!LoginState.IsLoggedIn) return;

        if (SplitId.HasValue)
        {
            isLoadingEditSplit = true;
            await TryAsync(() => LoadSplitForEditAsync(SplitId.Value), "Kunde inte ladda splitten");
            isLoadingEditSplit = false;
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
        try
        {
            allTemplates = await SplitTemplateService.GetAllAsync();
            mySplits     = await UserSplitsService.GetMySplitsAsync();
        }
        catch
        {
            allTemplates = new();
            mySplits = new();
        }
    }

    private void AddPass()
    {
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
        passes[passIndex].Exercises.Add(new CreateExerciseDto());
        exerciseExpanded[passIndex].Add(true);
    }

    private void RemoveExercise(int passIndex, int exerciseIndex)
    {
        passes[passIndex].Exercises.RemoveAt(exerciseIndex);
        exerciseExpanded[passIndex].RemoveAt(exerciseIndex);
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

        if (import.MySplit is not null)
            await ImportFromMySplitAsync(import.MySplit);
        else if (import.Template is not null)
            ImportFromTemplate(import.Template);

        isImporting = false;
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
            splitName    = imported.SplitName;
            passes       = imported.Passes;
            ResetExpandedState();
        }, "Failed to fetch split");
    }

    private async Task LoadSplitForEditAsync(int splitId)
    {
        var imported = await UserSplitsService.GetMySplitFullAsync(splitId).WaitAsync(TimeSpan.FromSeconds(12));
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
        if (string.IsNullOrWhiteSpace(splitName) || isSaving || isLoadingEditSplit) return;

        isSaving = true;
        await TryAsync(async () =>
        {
            var request = new CreateSplitRequest
            {
                SplitName = splitName.Trim(),
                Passes    = passes,
            };

            if (SplitId.HasValue)
            {
                await UserSplitsService.UpdateCustomSplitAsync(SplitId.Value, request);
                successMessage = "Ändringarna har sparats.";
            }
            else
            {
                await UserSplitsService.CreateCustomSplitAsync(request);
                successMessage = "Splitten har sparats.";
            }
        }, "Failed to save split");
        isSaving = false;
        StateHasChanged();
    }

    private void ResetExpandedState()
    {
        splitExpanded = true;
        passExpanded = passes.Select(_ => true).ToList();
        exerciseExpanded = passes
            .Select(p => p.Exercises.Select(_ => true).ToList())
            .ToList();
    }

    private void ClearSuccess() => successMessage = null;
}
