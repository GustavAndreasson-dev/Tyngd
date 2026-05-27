using ArnoldOne.Shared.TrainingPrograms;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ArnoldOne.Client.Pages.Traningsprogram;

public partial class ImportSplitDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public List<TrainingSplit> MySplits { get; set; } = new();

    [Parameter]
    public List<TrainingSplitTemplate> Templates { get; set; } = new();

    [Parameter]
    public EventCallback<ImportSplitResult> OnSelected { get; set; }

    private async Task SelectMySplit(TrainingSplit split)
    {
        await OnSelected.InvokeAsync(new ImportSplitResult(split, null));
    }

    private async Task SelectTemplate(TrainingSplitTemplate template)
    {
        await OnSelected.InvokeAsync(new ImportSplitResult(null, template));
    }

    private void Cancel() =>
        MudDialog.Cancel();
}

public record ImportSplitResult(TrainingSplit? MySplit, TrainingSplitTemplate? Template);
