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

    private string splitName = string.Empty;
    private List<CreatePassDto> passes = new() { new CreatePassDto() };

    private List<TrainingSplitTemplate> allTemplates = new();
    private List<TrainingSplit> mySplits = new();
    private bool isImporting = false;
    private bool splitExpanded = true;
    private List<bool> passExpanded = new() { true };

    protected override async Task OnInitializedAsync()
    {
        if (!LoginState.IsLoggedIn) return;
        await TryAsync(async () =>
        {
            allTemplates = await SplitTemplateService.GetAllAsync();
            mySplits     = await UserSplitsService.GetMySplitsAsync();
        }, "Failed to load splits");
    }

    private void AddPass()
    {
        passes.Add(new CreatePassDto());
        passExpanded.Add(true);
    }

    private void RemovePass(int index)
    {
        passes.RemoveAt(index);
        passExpanded.RemoveAt(index);
    }

    private void AddExercise(int passIndex) =>
        passes[passIndex].Exercises.Add(new CreateExerciseDto());

    private void RemoveExercise(int passIndex, int exerciseIndex) =>
        passes[passIndex].Exercises.RemoveAt(exerciseIndex);

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
        passExpanded = passes.Select(_ => true).ToList();
        splitExpanded = true;
    }

    private async Task ImportFromMySplitAsync(TrainingSplit split)
    {
        await TryAsync(async () =>
        {
            var imported = await UserSplitsService.GetMySplitFullAsync(split.SplitId);
            if (imported is null) return;
            splitName    = imported.SplitName;
            passes       = imported.Passes;
            passExpanded = passes.Select(_ => true).ToList();
            splitExpanded = true;
        }, "Failed to fetch split");
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(splitName)) return;

        await TryAsync(async () =>
        {
            var request = new CreateSplitRequest
            {
                SplitName = splitName.Trim(),
                Passes    = passes,
            };

            await UserSplitsService.CreateCustomSplitAsync(request);
            Nav.NavigateTo("/");
        }, "Failed to save split");
    }
}
