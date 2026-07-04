using ArnoldOne.Client.Services;
using Microsoft.AspNetCore.Components;

namespace ArnoldOne.Client.Shared;

public partial class ClientErrorBoundary
{
    [Inject] public ClientErrorService ClientErrorService { get; set; } = null!;

    protected override async Task OnErrorAsync(Exception exception)
    {
        await ClientErrorService.ReportAsync(exception);
        await base.OnErrorAsync(exception);
    }
}
