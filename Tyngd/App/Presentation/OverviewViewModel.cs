using Tyngd.Client.Models;
using Tyngd.Shared.DTOs;
using Tyngd.Shared.TrainingPrograms;

namespace Tyngd.App.Presentation;

/// <summary>Översikten (Index.razor i webbappen): nästa eller pågående pass, statistik, veckan och aktiva splits.</summary>
public partial class OverviewViewModel : ObservableObject
{
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
            return DashboardPassFinder.WeekDays(Dashboard, DateTime.Now)
                .Select(d => new WeekDay(d.Label, d.Done, d.Today))
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
        Today = SvDate.Long(DateTime.Now);
        try
        {
            var dashboard = await _dashboardService.GetAsync();
            var active = await DashboardPassFinder.FindActivePassAsync(dashboard, _session, _storage, DateTimeOffset.UtcNow);
            Dashboard = dashboard;
            HeroIsActive = active is not null;
            Hero = active ?? DashboardPassFinder.FindNextPass(dashboard);
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

    public sealed record WeekDay(string Label, bool Done, bool Today)
    {
        public Visibility DoneVisibility => Done ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TodayVisibility => Today ? Visibility.Visible : Visibility.Collapsed;
        public string AutomationName => Done ? $"{Label}, tränat" : Label;
    }

    public sealed record SplitItem(string SplitName, string PassCount, IReadOnlyList<PassChip> Passes);

    public sealed record PassChip(int SplitId, int PassId, string PassName);
}
