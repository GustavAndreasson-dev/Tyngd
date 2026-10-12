using Tyngd.Client.Services;
using Tyngd.Client.Shared;
using Tyngd.Client.Models;
using Tyngd.Shared.DTOs;
using Tyngd.Shared.TrainingPrograms;
using Tyngd.Shared.Users;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Tyngd.Client.Pages;

public partial class Index : PageComponentBase, IDisposable
{
    [Inject] private DashboardService DashboardService { get; set; } = default!;
    [Inject] private LoginState LoginState { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ILocalStorageService LocalStorage { get; set; } = default!;
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
            activePass = await FindActivePassAsync(dashboard);
            nextPass = activePass is null ? FindNextPass(dashboard) : null;
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
    // Egna namn: WASM har ingen sv-SE-kulturdata, så ToString("dddd") blir engelska.
    private static readonly string[] DayNames = ["söndag", "måndag", "tisdag", "onsdag", "torsdag", "fredag", "lördag"];
    private static readonly string[] MonthNames = ["januari", "februari", "mars", "april", "maj", "juni", "juli", "augusti", "september", "oktober", "november", "december"];

    private static string Today()
    {
        var now = DateTime.Now;
        return $"{DayNames[(int)now.DayOfWeek]} {now.Day} {MonthNames[now.Month - 1]}";
    }

    private static readonly string[] DayLabels =["M", "T", "O", "T", "F", "L", "S"];

    /// <summary>Måndag–söndag för innevarande vecka, med markering för dagar där ett pass slutförts.</summary>
    private IEnumerable<(string Label, bool Done, bool Today)> WeekDays()
    {
        var today = DateTime.Now.Date;
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var done = dashboard!.RecentCompletedAtUtc.Select(d => d.ToLocalTime().Date).ToHashSet();
        return Enumerable.Range(0, 7).Select(i => (DayLabels[i], done.Contains(monday.AddDays(i)), monday.AddDays(i) == today));
    }

    private static string PassUrl(DashboardPass pass, bool startNew = false) =>
        $"/TP/split/{pass.SplitId}/pass/{pass.PassId}{(startNew ? "?new=1" : string.Empty)}";

    private async Task<DashboardPass?> FindActivePassAsync(UserDashboardDto currentDashboard)
    {
        if (Session.IsActive)
        {
            var sessionPass = FindPass(currentDashboard, Session.ActiveSplitId, Session.ActivePassId);
            if (sessionPass is not null)
                return sessionPass;
        }

        var now = DateTimeOffset.UtcNow;
        DashboardPass? latestPass = null;
        DateTimeOffset? latestStartedAt = null;

        foreach (var key in await LocalStorage.KeysAsync())
        {
            if (!TryParsePassKey(key, out var splitId, out var passId))
                continue;

            PassData? data;
            try
            {
                data = await LocalStorage.GetItemAsync<PassData>(key);
            }
            catch
            {
                continue;
            }

            if (!PassResumePolicy.ShouldResume(data, now))
                continue;

            var storedPass = FindPass(currentDashboard, splitId, passId);
            var startedAt = data!.StartedAtUtc!.Value;
            if (storedPass is null || latestStartedAt.HasValue && startedAt <= latestStartedAt.Value)
                continue;

            latestPass = storedPass;
            latestStartedAt = startedAt;
        }

        return latestPass;
    }

    private static DashboardPass? FindNextPass(UserDashboardDto currentDashboard)
    {
        var firstPass = currentDashboard.ActiveSplits
            .SelectMany(split => split.Passes.Select(pass => ToDashboardPass(split, pass)))
            .FirstOrDefault();

        if (currentDashboard.LastCompletedSession is not { } lastSession)
            return firstPass;

        var split = currentDashboard.ActiveSplits.FirstOrDefault(item => item.SplitId == lastSession.SplitId);
        if (split is null || split.Passes.Count == 0)
            return firstPass;

        var lastPassIndex = split.Passes.FindIndex(pass => pass.PassId == lastSession.PassId);
        if (lastPassIndex < 0)
            return firstPass;

        var nextPassIndex = (lastPassIndex + 1) % split.Passes.Count;
        return ToDashboardPass(split, split.Passes[nextPassIndex]);
    }

    private static DashboardPass? FindPass(UserDashboardDto currentDashboard, int splitId, int passId)
    {
        var split = currentDashboard.ActiveSplits.Append(currentDashboard.RecentFreeSessions)
            .FirstOrDefault(item => item?.SplitId == splitId);
        var pass = split?.Passes.FirstOrDefault(item => item.PassId == passId);
        return split is not null && pass is not null ? ToDashboardPass(split, pass) : null;
    }

    private static DashboardPass ToDashboardPass(TrainingSplit split, TrainingPass pass) =>
        new(split.SplitId, split.SplitName, pass.PassId, pass.PassName);

    private static bool TryParsePassKey(string key, out int splitId, out int passId)
    {
        splitId = 0;
        passId = 0;
        var parts = key.Split('-', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 3
            && parts[0] == "pass"
            && int.TryParse(parts[1], out splitId)
            && int.TryParse(parts[2], out passId);
    }

    public void Dispose() => LoginState.OnChange -= OnLoginChanged;

    private sealed record DashboardPass(int SplitId, string SplitName, int PassId, string PassName);
}
