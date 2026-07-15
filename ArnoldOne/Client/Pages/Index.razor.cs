using ArnoldOne.Client.Services;
using ArnoldOne.Client.Shared;
using ArnoldOne.Shared.DTOs;
using ArnoldOne.Shared.Users;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ArnoldOne.Client.Pages;

public partial class Index : PageComponentBase, IDisposable
{
    [Inject] private DashboardService DashboardService { get; set; } = default!;
    [Inject] private LoginState LoginState { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    private UserDashboardDto? dashboard;

    protected override async Task OnInitializedAsync()
    {
        LoginState.OnChange += OnLoginChanged;
        await LoadDashboardAsync();
    }

    private async Task LoadDashboardAsync()
    {
        dashboard = null;
        if (!LoginState.IsLoggedIn)
            return;

        await TryAsync(async () => dashboard = await DashboardService.GetAsync(), "Kunde inte ladda din träningsöversikt");
    }

    private async void OnLoginChanged()
    {
        try
        {
            await LoadDashboardAsync();
            await InvokeAsync(StateHasChanged);
        }
        catch { }
    }

    private async Task OpenLoginDialog() => await DialogService.ShowAsync<LoginDialog>("Logga in");
    private async Task OpenRegisterDialog() => await DialogService.ShowAsync<RegisterDialog>("Skapa konto");
    private static string PassUrl(DashboardSessionDto session) => $"/TP/split/{session.SplitId}/pass/{session.PassId}";
    private static string FormatDate(DateTimeOffset value) => value.ToLocalTime().ToString("d MMM yyyy, HH:mm");

    public void Dispose() => LoginState.OnChange -= OnLoginChanged;
}
