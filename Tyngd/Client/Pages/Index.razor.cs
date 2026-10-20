using Tyngd.Client.Services;
using Tyngd.Client.Shared;
using Tyngd.Client.Models;
using Tyngd.Shared.DTOs;
using Tyngd.Shared.TrainingPrograms;
using Tyngd.Shared.Users;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Tyngd.Client.Pages;

public partial class Index : PageComponentBase, IDisposable
{
    [Inject] private DashboardService DashboardService { get; set; } = default!;
    [Inject] private LoginState LoginState { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private IAppStorage Storage { get; set; } = default!;
    [Inject] private SessionState Session { get; set; } = default!;

    private UserDashboardDto? dashboard;
    private DashboardPass? activePass;
    private DashboardPass? nextPass;

    protected override async Task OnInitializedAsync()
    {
        LoginState.OnChange += OnLoginChanged;
        await LoadDashboardAsync();
    }

    private async Task LoadDashboardAsync()
    {
        dashboard = null;
        activePass = null;
        nextPass = null;
        if (!LoginState.IsLoggedIn)
            return;

        await TryAsync(async () =>
        {
            dashboard = await DashboardService.GetAsync();
            activePass = await DashboardPassFinder.FindActivePassAsync(dashboard, Session, Storage, DateTimeOffset.UtcNow);
            nextPass = activePass is null ? DashboardPassFinder.FindNextPass(dashboard) : null;
        }, "Kunde inte ladda din träningsöversikt");
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

    private Task StartFreeSession() =>
        DialogService.ShowAsync<StartFreeSessionDialog>(null, new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true });
    private static string PassUrl(DashboardPass pass, bool startNew = false) =>
        $"/TP/split/{pass.SplitId}/pass/{pass.PassId}{(startNew ? "?new=1" : string.Empty)}";

    public void Dispose() => LoginState.OnChange -= OnLoginChanged;
}
