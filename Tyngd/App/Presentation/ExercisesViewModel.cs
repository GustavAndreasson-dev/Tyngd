using Tyngd.Shared.Catalog;

namespace Tyngd.App.Presentation;

/// <summary>Övningskatalogen med sökning, filter och favoriter (som Ovningar.razor i webbappen). Appen kräver inloggning, så webbens utloggade läge behövs inte.</summary>
public partial class ExercisesViewModel : ObservableObject
{
    private readonly CatalogService _catalogService;
    private readonly FavoritesService _favoritesService;
    private readonly INavigator _navigator;
    private List<CatalogExerciseDto>? _all;
    private HashSet<int> _favorites = new();

    public ExercisesViewModel(CatalogService catalogService, FavoritesService favoritesService, INavigator navigator)
    {
        _catalogService = catalogService;
        _favoritesService = favoritesService;
        _navigator = navigator;
    }

    public IReadOnlyList<FilterOption> MuscleOptions { get; } =
        [new(null, "Alla muskler"), .. Muscles.All.OrderBy(m => m.NameSv).Select(m => new FilterOption(m.Key, m.NameSv))];

    [ObservableProperty]
    private IReadOnlyList<FilterOption> _equipmentOptions = [new(null, "All utrustning")];

    [ObservableProperty]
    private IReadOnlyList<string> _categories = [];

    [ObservableProperty]
    private IReadOnlyList<ExerciseItem> _visible = [];

    [ObservableProperty]
    private string? _search;

    [ObservableProperty]
    private string? _category;

    [ObservableProperty]
    private FilterOption? _muscle;

    [ObservableProperty]
    private FilterOption? _equipment;

    [ObservableProperty]
    private bool _onlyFavorites;

    [ObservableProperty]
    private string _countText = string.Empty;

    [ObservableProperty]
    private string _favoritesText = "★ Favoriter";

    [ObservableProperty]
    private string? _emptyText;

    [ObservableProperty]
    private string? _error;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LoadingVisibility))]
    private bool _isLoading;

    public Visibility LoadingVisibility => IsLoading ? Visibility.Visible : Visibility.Collapsed;

    partial void OnSearchChanged(string? value) => Filter();
    partial void OnCategoryChanged(string? value) => Filter();
    partial void OnMuscleChanged(FilterOption? value) => Filter();
    partial void OnEquipmentChanged(FilterOption? value) => Filter();
    partial void OnOnlyFavoritesChanged(bool value) => Filter();

    /// <summary>Hämtar katalogen första gången (CatalogService cachar listan) och favoriterna.</summary>
    public async Task LoadAsync()
    {
        if (_all is not null || IsLoading)
            return;

        IsLoading = true;
        Error = null;
        try
        {
            var all = await _catalogService.GetAllAsync();
            _favorites = await _favoritesService.GetAsync();
            _all = all;
            Categories = all.Select(e => e.Category).Distinct().Order().ToList();
            EquipmentOptions = [EquipmentOptions[0], .. all.SelectMany(e => e.Equipment).Distinct().Order().Select(e => new FilterOption(e, e))];
            CountText = all.Count > 0 ? $"{all.Count} övningar" : string.Empty;
            Filter();
        }
        catch (HttpRequestException)
        {
            Error = "Kunde inte hämta övningarna. Kontrollera anslutningen.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Lyssnar på favoritändringar medan sidan visas (sidan anropar Attach/Detach vid Loaded/Unloaded).</summary>
    public void Attach()
    {
        Detach();
        _favoritesService.OnChange += SyncFavorites;
    }

    public void Detach() => _favoritesService.OnChange -= SyncFavorites;

    /// <summary>Uppdaterar stjärnorna utan att sortera om, så att listan inte hoppar när man trycker.</summary>
    public void SyncFavorites()
    {
        foreach (var item in Visible)
            item.IsFavorite = _favorites.Contains(item.Exercise.CatalogExerciseId);
        FavoritesText = _favorites.Count > 0 ? $"★ Favoriter ({_favorites.Count})" : "★ Favoriter";
    }

    private void Filter()
    {
        if (_all is null)
            return;

        IEnumerable<CatalogExerciseDto> rows = _all;
        if (OnlyFavorites)
            rows = rows.Where(e => _favorites.Contains(e.CatalogExerciseId));
        if (Category is not null)
            rows = rows.Where(e => e.Category == Category);
        if (Muscle?.Value is { } muscle)
            rows = rows.Where(e => e.PrimaryMuscles.Contains(muscle) || e.SecondaryMuscles.Contains(muscle));
        if (Equipment?.Value is { } equipment)
            rows = rows.Where(e => e.Equipment.Contains(equipment));
        rows = CatalogService.Search(rows, Search);

        // Favoriter först, annars sökordningen.
        Visible = rows
            .OrderByDescending(e => _favorites.Contains(e.CatalogExerciseId))
            .Select(e => new ExerciseItem(e, ToggleFavoriteCommand))
            .ToList();
        SyncFavorites();
        EmptyText = Visible.Count > 0 ? null
            : OnlyFavorites && _favorites.Count == 0 ? "Du har inga favoriter än. Tryck på stjärnan vid en övning."
            : "Inga övningar matchar.";
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(ExerciseItem item)
    {
        Error = null;
        try
        {
            // FavoritesService.OnChange anropar SyncFavorites.
            await _favoritesService.ToggleAsync(item.Exercise.CatalogExerciseId);
        }
        catch (HttpRequestException)
        {
            Error = "Kunde inte spara favoriten.";
        }
    }

    [RelayCommand]
    private Task OpenAsync(ExerciseItem item) =>
        _navigator.NavigateViewModelAsync<ExerciseInfoViewModel>(this, data: new ExerciseInfoArgs(item.Exercise.CatalogExerciseId));
}

/// <summary>Val i en filterlista; Value null betyder alla.</summary>
public sealed record FilterOption(string? Value, string Label)
{
    public override string ToString() => Label;
}

public partial class ExerciseItem : ObservableObject
{
    public ExerciseItem(CatalogExerciseDto exercise, IAsyncRelayCommand<ExerciseItem> toggleFavoriteCommand)
    {
        Exercise = exercise;
        ToggleFavoriteCommand = toggleFavoriteCommand;
    }

    public CatalogExerciseDto Exercise { get; }

    public IAsyncRelayCommand<ExerciseItem> ToggleFavoriteCommand { get; }

    public string Name => Exercise.Name;

    public string? ImageUrl => Exercise.ImageUrl;

    public Visibility PlaceholderVisibility => Exercise.ImageUrl is null ? Visibility.Visible : Visibility.Collapsed;

    public string Meta
    {
        get
        {
            var parts = new List<string> { Exercise.Category };
            if (Exercise.Equipment.Count > 0)
                parts.Add(string.Join(", ", Exercise.Equipment));
            if (!string.Equals(Exercise.Name, Exercise.NameEn, StringComparison.OrdinalIgnoreCase))
                parts.Add(Exercise.NameEn);
            return string.Join(" · ", parts);
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteVisibility), nameof(NotFavoriteVisibility), nameof(StarLabel))]
    private bool _isFavorite;

    public Visibility FavoriteVisibility => IsFavorite ? Visibility.Visible : Visibility.Collapsed;

    public Visibility NotFavoriteVisibility => IsFavorite ? Visibility.Collapsed : Visibility.Visible;

    public string StarLabel => IsFavorite ? $"Ta bort {Name} från favoriter" : $"Spara {Name} som favorit";
}
