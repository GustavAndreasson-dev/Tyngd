using Tyngd.Client.Services;
using Microsoft.AspNetCore.Components;

namespace Tyngd.Client.Shared;

public partial class ClientErrorBoundary
{
    [Inject] public ClientErrorService ClientErrorService { get; set; } = null!;
    [Inject] public NavigationManager Nav { get; set; } = null!;

    protected override async Task OnErrorAsync(Exception exception)
    {
        await ClientErrorService.ReportAsync(exception, Nav.Uri);
        await base.OnErrorAsync(exception);
    }
}
