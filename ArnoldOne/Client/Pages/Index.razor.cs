using ArnoldOne.Client.Services;
using ArnoldOne.Client.Shared;
using ArnoldOne.Shared.DTOs;
using Microsoft.AspNetCore.Components;

namespace ArnoldOne.Client.Pages;

public partial class Index
{
    [Inject]
    private PassTemplateService PassTemplateService { get; set; } = default!;

    private List<PassTemplateExerciseCountDto> exerciseCounts = new();

    protected override async Task OnInitializedAsync()
    {
        await TryAsync(async () =>
        {
            exerciseCounts = await PassTemplateService.GetExerciseCountsAsync();
        }, "Kunde inte hämta övningsdata");
    }
}
