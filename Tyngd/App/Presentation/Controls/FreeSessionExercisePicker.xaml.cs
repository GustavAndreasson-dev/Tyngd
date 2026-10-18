using Microsoft.UI.Xaml.Automation;
using Tyngd.Shared.Catalog;
using Tyngd.Shared.DTOs;

namespace Tyngd.App.Presentation.Controls;

/// <summary>
/// Väljer övningar till ett löst pass (FreeSessionExercisePicker i webbappen). Anropa <see cref="LoadAsync"/> en gång;
/// varje val skickas med <see cref="ExercisePicked"/> och sökfältet töms.
/// </summary>
public sealed partial class FreeSessionExercisePicker : UserControl
{
    private const int MaxResults = 25;

    private List<CatalogExerciseDto> _catalog = [];
    private HashSet<int> _favoriteIds = [];
    private IReadOnlySet<int> _taken = new HashSet<int>();

    public FreeSessionExercisePicker()
    {
        this.InitializeComponent();
    }

    /// <summary>Anropas för varje vald övning.</summary>
    public event EventHandler<FreeSessionExerciseDto>? ExercisePicked;

    /// <summary>Katalog-id:n som redan är valda; deras favoritknappar gråas ut.</summary>
    public IReadOnlySet<int> Taken
    {
        get => _taken;
        set
        {
            _taken = value;
            RenderFavorites();
        }
    }

    public async Task LoadAsync(CatalogService catalogService, FavoritesService favoritesService)
    {
        // Utan katalog (offline) går det ändå att lägga till en egen övning.
        try
        {
            _catalog = await catalogService.GetAllAsync();
            _favoriteIds = await favoritesService.GetAsync();
        }
        catch (HttpRequestException) { }
        RenderFavorites();
    }

    private void RenderFavorites()
    {
        FavoritesPanel.Children.Clear();
        foreach (var exercise in _catalog.Where(e => _favoriteIds.Contains(e.CatalogExerciseId)))
        {
            var button = new Button
            {
                Content = $"+ {exercise.Name}",
                IsEnabled = !Taken.Contains(exercise.CatalogExerciseId),
                Style = (Style)Resources["FavoriteChipStyle"],
            };
            AutomationProperties.SetName(button, $"Lägg till {exercise.Name}");
            button.Click += (_, _) => Pick(exercise.CatalogExerciseId, exercise.Name);
            FavoritesPanel.Children.Add(button);
        }
        FavoritesHeader.Visibility = FavoritesPanel.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            sender.ItemsSource = Suggestions(sender.Text);
    }

    private void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var text = args.QueryText.Trim();
        // Enter utan vald rad: exakt katalogträff, annars egen övning (sist i listan).
        var item = args.ChosenSuggestion as PickerItem
            ?? (text.Length == 0 ? null : Suggestions(text).LastOrDefault(i => i.CatalogExerciseId is null || IsExact(i, text)));
        if (item is null)
            return;
        sender.Text = string.Empty;
        sender.ItemsSource = null;
        Pick(item.CatalogExerciseId, item.Name);
    }

    private List<PickerItem> Suggestions(string? text)
    {
        var trimmed = (text ?? string.Empty).Trim();
        // Tomt fält visar favoriterna; vid sökning kommer favoriter först.
        var found = trimmed.Length == 0
            ? _catalog.Where(e => _favoriteIds.Contains(e.CatalogExerciseId))
            : CatalogService.Search(_catalog, trimmed).OrderByDescending(e => _favoriteIds.Contains(e.CatalogExerciseId));
        var items = found
            .Take(MaxResults)
            .Select(e => new PickerItem(
                (_favoriteIds.Contains(e.CatalogExerciseId) ? "★ " : string.Empty) + e.Name,
                Subtitle(e),
                e.CatalogExerciseId,
                e.Name))
            .ToList();

        if (trimmed.Length > 0 && !items.Any(i => IsExact(i, trimmed)))
        {
            var name = Shorten(trimmed);
            items.Add(new PickerItem($"Lägg till egen övning: \"{name}\"", "Egen övning (finns inte i katalogen)", null, name));
        }
        return items;
    }

    private void Pick(int? catalogExerciseId, string name) =>
        ExercisePicked?.Invoke(this, new FreeSessionExerciseDto { CatalogExerciseId = catalogExerciseId, Name = Shorten(name) });

    private static bool IsExact(PickerItem item, string text) =>
        string.Equals(item.Name, text, StringComparison.CurrentCultureIgnoreCase);

    private static string Subtitle(CatalogExerciseDto exercise)
    {
        var parts = new List<string>();
        if (!string.Equals(exercise.Name, exercise.NameEn, StringComparison.OrdinalIgnoreCase))
            parts.Add(exercise.NameEn);
        parts.Add(exercise.Category);
        if (exercise.Equipment.Count > 0)
            parts.Add(string.Join(", ", exercise.Equipment));
        return string.Join(" · ", parts);
    }

    private static string Shorten(string name) => name.Length > 50 ? name[..50] : name;

    public sealed record PickerItem(string Title, string Subtitle, int? CatalogExerciseId, string Name);
}
