using Tyngd.Shared.Catalog;

namespace Tyngd.App.Presentation;

/// <summary>Detaljer, muskelfigur och historik för en katalogövning (som ExerciseInfoDialog.razor i webbappen).</summary>
public partial class ExerciseInfoViewModel : ObservableObject
{
    private readonly CatalogService _catalogService;
    private readonly FavoritesService _favoritesService;
    private readonly PassLogService _passLogService;
    private readonly INavigator _navigator;

    public ExerciseInfoViewModel(ExerciseInfoArgs args, CatalogService catalogService, FavoritesService favoritesService, PassLogService passLogService, INavigator navigator)
    {
        Args = args;
        _catalogService = catalogService;
        _favoritesService = favoritesService;
        _passLogService = passLogService;
        _navigator = navigator;
        _ = LoadAsync();
    }

    public ExerciseInfoArgs Args { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ContentVisibility))]
    private CatalogExerciseDetailDto? _exercise;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LoadingVisibility))]
    private bool _isLoading = true;

    [ObservableProperty]
    private string? _error;

    [ObservableProperty]
    private Dictionary<string, double>? _load;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteVisibility), nameof(NotFavoriteVisibility), nameof(StarLabel))]
    private bool _isFavorite;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HistoryVisibility))]
    private string? _historySummary;

    [ObservableProperty]
    private IReadOnlyList<HistoryRow> _historyRows = [];

    public Visibility ContentVisibility => Exercise is null ? Visibility.Collapsed : Visibility.Visible;
    public Visibility LoadingVisibility => IsLoading ? Visibility.Visible : Visibility.Collapsed;
    public Visibility HistoryVisibility => HistorySummary is null ? Visibility.Collapsed : Visibility.Visible;
    public Visibility FavoriteVisibility => IsFavorite ? Visibility.Visible : Visibility.Collapsed;
    public Visibility NotFavoriteVisibility => IsFavorite ? Visibility.Collapsed : Visibility.Visible;
    public Visibility ImageVisibility => Exercise?.ImageUrl is null ? Visibility.Collapsed : Visibility.Visible;
    public string StarLabel => IsFavorite ? "Ta bort från favoriter" : "Spara som favorit";

    public string Title => Exercise?.Name ?? "Övning";

    public string? NameEn => Exercise is { } e && !string.Equals(e.Name, e.NameEn, StringComparison.OrdinalIgnoreCase) ? e.NameEn : null;

    /// <summary>Kategori (om den inte redan är en muskel), muskler och utrustning.</summary>
    public IReadOnlyList<string> Tags => Exercise is not { } e ? []
        : [.. (e.Muscles.Contains(e.Category) ? Array.Empty<string>() : new[] { e.Category }), .. e.Muscles, .. e.Equipment];

    public string Description => string.IsNullOrWhiteSpace(Exercise?.Description) ? "Ingen beskrivning finns." : Exercise.Description;

    // Licenstexten krävs av wgers licens och visas som i webbappen.
    public string LicenseText => Exercise is not { } e ? string.Empty
        : string.Concat(new[] { e.License, e.LicenseAuthor }.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => " · " + part));

    public string? ImageCredit => Exercise is { ImageUrl: not null } e && (!string.IsNullOrWhiteSpace(e.ImageAuthor) || !string.IsNullOrWhiteSpace(e.ImageLicense))
        ? "Bild: " + string.Join(", ", new[] { e.ImageAuthor, e.ImageLicense }.Where(part => !string.IsNullOrWhiteSpace(part)))
        : null;

    partial void OnExerciseChanged(CatalogExerciseDetailDto? value)
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(NameEn));
        OnPropertyChanged(nameof(Tags));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(LicenseText));
        OnPropertyChanged(nameof(ImageCredit));
        OnPropertyChanged(nameof(ImageVisibility));
    }

    private async Task LoadAsync()
    {
        try
        {
            var exercise = await _catalogService.GetAsync(Args.CatalogExerciseId);
            if (exercise is null)
            {
                Error = "Övningen hittades inte.";
                return;
            }

            Load = MuscleLoad.Sum([(exercise.PrimaryMuscles, exercise.SecondaryMuscles)]);
            Exercise = exercise;
            IsFavorite = (await _favoritesService.GetAsync()).Contains(Args.CatalogExerciseId);

            var history = await _passLogService.GetCatalogHistoryAsync(Args.CatalogExerciseId);
            if (history.Count > 0)
            {
                HistorySummary = $"Bästa vikt: {history.Max(entry => entry.MaxWeight):0.##} kg · {history.Count} senaste tillfällen från alla pass";
                HistoryRows = Enumerable.Reverse(history).Take(5)
                    .Select(entry => new HistoryRow(
                        $"{SvDate.Short(entry.Date)} · {entry.PassName}",
                        $"{entry.MaxWeight:0.##} kg · {entry.Sets} set · {entry.TotalReps} reps"))
                    .ToList();
            }
        }
        catch (HttpRequestException)
        {
            Error = "Kunde inte hämta övningen. Kontrollera anslutningen.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        try { IsFavorite = await _favoritesService.ToggleAsync(Args.CatalogExerciseId); }
        catch (HttpRequestException) { }
    }

    [RelayCommand]
    private Task BackAsync() => _navigator.NavigateBackAsync(this);
}

public sealed record HistoryRow(string Left, string Right);
