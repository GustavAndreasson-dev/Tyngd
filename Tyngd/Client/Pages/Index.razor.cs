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

    private static readonly string[] DayLabels = ["M", "T", "O", "T", "F", "L", "S"];

    private const int ActivityWeeks = 16;

    /// <summary>Måndagen i den äldsta veckan i aktivitetsrutnätet; sista kolumnen är innevarande vecka.</summary>
    private static DateTime ActivityStart()
    {
        var today = DateTime.Now.Date;
        return today.AddDays(-(((int)today.DayOfWeek + 6) % 7) - 7 * (ActivityWeeks - 1));
    }

    private static string PassInitial(string passName) =>
        string.IsNullOrWhiteSpace(passName) ? "" : passName.Trim()[..1].ToUpperInvariant();

    /// <summary>Tränade dagar (lokalt datum) med dagens senaste pass, vars färg och namn visas i rutan.</summary>
    private Dictionary<DateTime, DashboardActivityDto> ActivityDays() =>
        dashboard!.RecentActivity
            .OrderBy(a => a.CompletedAtUtc)
            .GroupBy(a => a.CompletedAtUtc.ToLocalTime().Date)
            .ToDictionary(g => g.Key, g => g.Last());

    /// <summary>Månadens kortnamn ovanför första veckan i en ny månad, som GitHubs bidragsgraf.</summary>
    private static string MonthLabel(DateTime monday, int week)
    {
        var starts = week == 0 ? monday.AddDays(7).Month == monday.Month : monday.AddDays(-7).Month != monday.Month;
        return starts ? SvDate.Month(monday.Month) : "";
    }

    private static string PassUrl(DashboardPass pass, bool startNew = false) =>
        $"/TP/split/{pass.SplitId}/pass/{pass.PassId}{(startNew ? "?new=1" : string.Empty)}";

    public void Dispose() => LoginState.OnChange -= OnLoginChanged;
}
