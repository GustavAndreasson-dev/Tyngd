using Tyngd.Client.Models;
using Tyngd.Shared.DTOs;
using Tyngd.Shared.TrainingPrograms;

namespace Tyngd.App.Presentation;

/// <summary>Översikten (Index.razor i webbappen): nästa eller pågående pass, statistik, veckan och aktiva splits.</summary>
public partial class OverviewViewModel : ObservableObject
{
    // Egna namn, som i webbappen, så att texten inte beror på enhetens kulturinställning.
    private static readonly string[] DayNames = ["söndag", "måndag", "tisdag", "onsdag", "torsdag", "fredag", "lördag"];
    private static readonly string[] MonthNames = ["januari", "februari", "mars", "april", "maj", "juni", "juli", "augusti", "september", "oktober", "november", "december"];
    private static readonly string[] DayLabels = ["M", "T", "O", "T", "F", "L", "S"];

    private readonly DashboardService _dashboardService;
    private readonly SessionState _session;
    private readonly IAppStorage _storage;
    private readonly INavigator _navigator;
    private bool _loading;

    public OverviewViewModel(
        DashboardService dashboardService,
        SessionState session,
        IAppStorage storage,
        INavigator navigator,
        FreeSessionService freeSessions,
        CatalogService catalog,
        FavoritesService favorites)
    {
        _dashboardService = dashboardService;
        _session = session;
        _storage = storage;
        _navigator = navigator;
        FreeSessions = freeSessions;
        Catalog = catalog;
        Favorites = favorites;
    }

    // Tjänsterna som dialogen för löst pass behöver; sidan skapar dialogen.
    public FreeSessionService FreeSessions { get; }
    public CatalogService Catalog { get; }
    public FavoritesService Favorites { get; }

    [ObservableProperty]
    private string _today = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LoadingVisibility))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ContentVisibility), nameof(WeekDays), nameof(Splits), nameof(SplitsVisibility))]
    private UserDashboardDto? _dashboard;

    [ObservableProperty]
    private bool _heroIsActive;

    // Sätts efter Dashboard och HeroIsActive, så att de beroende egenskaperna aviseras med rätt värden.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeroVisibility), nameof(EmptyVisibility), nameof(HeroLabel), nameof(HeroButtonText))]
    private DashboardPass? _hero;

    public Visibility LoadingVisibility => IsLoading && Dashboard is null ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ContentVisibility => Dashboard is null ? Visibility.Collapsed : Visibility.Visible;
    public Visibility HeroVisibility => Hero is null ? Visibility.Collapsed : Visibility.Visible;
    public Visibility EmptyVisibility => Dashboard is not null && Hero is null ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SplitsVisibility => Dashboard?.ActiveSplits.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    public string HeroLabel => HeroIsActive ? "Pågående pass" : "Nästa pass";
    public string HeroButtonText => HeroIsActive ? "Fortsätt" : "Starta";

    /// <summary>Måndag–söndag för innevarande vecka, med markering för dagar där ett pass slutförts.</summary>
    public IReadOnlyList<WeekDay> WeekDays
    {
        get
        {
            if (Dashboard is null)
                return [];
            var today = DateTime.Now.Date;
            var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
            var done = Dashboard.RecentCompletedAtUtc.Select(d => d.ToLocalTime().Date).ToHashSet();
            return Enumerable.Range(0, 7)
                .Select(i => new WeekDay(DayLabels[i], done.Contains(monday.AddDays(i)), monday.AddDays(i) == today))
                .ToList();
        }
    }

    public IReadOnlyList<SplitItem> Splits =>
        Dashboard?.ActiveSplits
            .Select(split => new SplitItem(
                split.SplitName,
                $"{split.Passes.Count} pass",
                split.Passes.Select(pass => new PassChip(split.SplitId, pass.PassId, pass.PassName)).ToList()))
            .ToList() ?? [];

    public async Task LoadAsync()
    {
        // Sidan anropar vid Loaded, DataContext-byte och när fliken syns igen; samma laddning ska inte köras dubbelt.
        if (_loading)
            return;
        _loading = true;
        IsLoading = true;
        ErrorMessage = null;
        var now = DateTime.Now;
        Today = $"{DayNames[(int)now.DayOfWeek]} {now.Day} {MonthNames[now.Month - 1]}";
        try
        {
            var dashboard = await _dashboardService.GetAsync();
            var active = await FindActivePassAsync(dashboard);
            Dashboard = dashboard;
            HeroIsActive = active is not null;
            Hero = active ?? FindNextPass(dashboard);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Kunde inte ladda din träningsöversikt. Kontrollera anslutningen.";
        }
        finally
        {
            IsLoading = false;
            _loading = false;
        }
    }

    [RelayCommand]
    private Task StartHeroAsync() =>
        Hero is null ? Task.CompletedTask : OpenPassAsync(Hero.SplitId, Hero.PassId, startNew: !HeroIsActive);

    [RelayCommand]
    private Task CreateSplitAsync() =>
        _navigator.NavigateViewModelAsync<EditSplitViewModel>(this, data: new EditSplitArgs(null));

    // startNew som webbens ?new=1: Starta och nytt löst pass börjar en ny session i stället för att återuppta.
    public Task OpenPassAsync(int splitId, int passId, bool startNew = false) =>
        _navigator.NavigateViewModelAsync<PassViewModel>(this, data: new PassArgs(splitId, passId, startNew));

    private async Task<DashboardPass?> FindActivePassAsync(UserDashboardDto dashboard)
    {
        if (_session.IsActive && FindPass(dashboard, _session.ActiveSplitId, _session.ActivePassId) is { } sessionPass)
            return sessionPass;

        var now = DateTimeOffset.UtcNow;
        DashboardPass? latestPass = null;
        DateTimeOffset? latestStartedAt = null;

        foreach (var key in await _storage.KeysAsync())
        {
            if (!TryParsePassKey(key, out var splitId, out var passId))
                continue;

            PassData? data;
            try
            {
                data = await _storage.GetItemAsync<PassData>(key);
            }
            catch
            {
                continue;
            }

            if (!PassResumePolicy.ShouldResume(data, now))
                continue;

            var storedPass = FindPass(dashboard, splitId, passId);
            var startedAt = data!.StartedAtUtc!.Value;
            if (storedPass is null || latestStartedAt.HasValue && startedAt <= latestStartedAt.Value)
                continue;

            latestPass = storedPass;
            latestStartedAt = startedAt;
        }

        return latestPass;
    }

    private static DashboardPass? FindNextPass(UserDashboardDto dashboard)
    {
        var firstPass = dashboard.ActiveSplits
            .SelectMany(split => split.Passes.Select(pass => ToDashboardPass(split, pass)))
            .FirstOrDefault();

        if (dashboard.LastCompletedSession is not { } lastSession)
            return firstPass;

        var split = dashboard.ActiveSplits.FirstOrDefault(item => item.SplitId == lastSession.SplitId);
        if (split is null || split.Passes.Count == 0)
            return firstPass;

        var lastPassIndex = split.Passes.FindIndex(pass => pass.PassId == lastSession.PassId);
        if (lastPassIndex < 0)
            return firstPass;

        return ToDashboardPass(split, split.Passes[(lastPassIndex + 1) % split.Passes.Count]);
    }

    private static DashboardPass? FindPass(UserDashboardDto dashboard, int splitId, int passId)
    {
        var split = dashboard.ActiveSplits.Append(dashboard.RecentFreeSessions)
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

    public sealed record DashboardPass(int SplitId, string SplitName, int PassId, string PassName);

    public sealed record WeekDay(string Label, bool Done, bool Today)
    {
        public Visibility DoneVisibility => Done ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TodayVisibility => Today ? Visibility.Visible : Visibility.Collapsed;
        public string AutomationName => Done ? $"{Label}, tränat" : Label;
    }

    public sealed record SplitItem(string SplitName, string PassCount, IReadOnlyList<PassChip> Passes);

    public sealed record PassChip(int SplitId, int PassId, string PassName);
}
