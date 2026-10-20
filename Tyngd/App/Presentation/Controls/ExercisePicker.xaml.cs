using Tyngd.Shared.Catalog;

namespace Tyngd.App.Presentation.Controls;

/// <summary>Väljer en övning ur katalogen (som ExercisePicker.razor). Bind <see cref="ExerciseName"/> och <see cref="CatalogExerciseId"/> TwoWay.</summary>
public sealed partial class ExercisePicker : UserControl
{
    private const int MaxResults = 25;

    public static readonly DependencyProperty ExerciseNameProperty = DependencyProperty.Register(
        nameof(ExerciseName), typeof(string), typeof(ExercisePicker), new PropertyMetadata(string.Empty, (d, _) => ((ExercisePicker)d).SyncFromProperties()));

    public static readonly DependencyProperty CatalogExerciseIdProperty = DependencyProperty.Register(
        nameof(CatalogExerciseId), typeof(int?), typeof(ExercisePicker), new PropertyMetadata(null, (d, _) => ((ExercisePicker)d).SyncFromProperties()));

    public static readonly DependencyProperty ErrorTextProperty = DependencyProperty.Register(
        nameof(ErrorText), typeof(string), typeof(ExercisePicker), new PropertyMetadata(null, (d, e) => SetLabel(((ExercisePicker)d).ErrorLabel, e.NewValue as string)));

    public static readonly DependencyProperty HelperTextProperty = DependencyProperty.Register(
        nameof(HelperText), typeof(string), typeof(ExercisePicker), new PropertyMetadata(null, (d, e) => SetLabel(((ExercisePicker)d).HelperLabel, e.NewValue as string)));

    private CatalogService? _catalogService;
    private FavoritesService? _favoritesService;

    public ExercisePicker()
    {
        this.InitializeComponent();
    }

    public string ExerciseName
    {
        get => (string)GetValue(ExerciseNameProperty);
        set => SetValue(ExerciseNameProperty, value);
    }

    /// <summary>Null för egen övning som inte finns i katalogen.</summary>
    public int? CatalogExerciseId
    {
        get => (int?)GetValue(CatalogExerciseIdProperty);
        set => SetValue(CatalogExerciseIdProperty, value);
    }

    public string? ErrorText
    {
        get => (string?)GetValue(ErrorTextProperty);
        set => SetValue(ErrorTextProperty, value);
    }

    public string? HelperText
    {
        get => (string?)GetValue(HelperTextProperty);
        set => SetValue(HelperTextProperty, value);
    }

    private void SyncFromProperties()
    {
        // Jämför trimmat så att ett mellanslag under skrivning inte skrivs bort.
        if (!string.Equals(Box.Text.Trim(), ExerciseName, StringComparison.CurrentCultureIgnoreCase))
            Box.Text = ExerciseName;
        CustomLabel.Visibility = CatalogExerciseId is null && !string.IsNullOrWhiteSpace(ExerciseName) ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void SetLabel(TextBlock label, string? text)
    {
        label.Text = text ?? string.Empty;
        label.Visibility = string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
            return;

        // Fritext blir egen övning direkt, utom när den exakt matchar en katalogövning (nedan).
        var text = sender.Text.Trim();
        ExerciseName = text;
        CatalogExerciseId = null;

        var matches = await SearchAsync(text);
        if (sender.Text.Trim() != text)
            return; // Användaren har hunnit skriva mer.

        sender.ItemsSource = matches;
        if (matches.FirstOrDefault(item => string.Equals(item.Name, text, StringComparison.CurrentCultureIgnoreCase)) is { } exact)
        {
            ExerciseName = exact.Name;
            CatalogExerciseId = exact.Exercise.CatalogExerciseId;
        }
    }

    private void OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is not PickerItem item)
            return;
        ExerciseName = item.Name;
        CatalogExerciseId = item.Exercise.CatalogExerciseId;
    }

    private async void OnGotFocus(object sender, RoutedEventArgs e)
    {
        // Tomt fält visar favoriterna.
        if (Box.Text.Trim().Length > 0)
            return;
        var matches = await SearchAsync(string.Empty);
        Box.ItemsSource = matches;
        Box.IsSuggestionListOpen = matches.Count > 0;
    }

    private async Task<List<PickerItem>> SearchAsync(string text)
    {
        if (_catalogService is null)
        {
            var services = this.FindServiceProvider();
            _catalogService = services?.GetService<CatalogService>();
            _favoritesService = services?.GetService<FavoritesService>();
        }
        if (_catalogService is null || _favoritesService is null)
            return [];

        try
        {
            var all = await _catalogService.GetAllAsync();
            var favorites = await _favoritesService.GetAsync();
            var found = text.Length == 0
                ? all.Where(exercise => favorites.Contains(exercise.CatalogExerciseId))
                : CatalogService.Search(all, text).OrderByDescending(exercise => favorites.Contains(exercise.CatalogExerciseId));
            return found
                .Take(MaxResults)
                .Select(exercise => new PickerItem(exercise, favorites.Contains(exercise.CatalogExerciseId)))
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            // Utan katalog går det fortfarande att skriva en egen övning.
            return [];
        }
    }

    public sealed record PickerItem(CatalogExerciseDto Exercise, bool IsFavorite)
    {
        public string Name => Exercise.Name;
        public string Title => IsFavorite ? $"★ {Exercise.Name}" : Exercise.Name;

        public string Subtitle
        {
            get
            {
                var parts = new List<string>();
                if (!string.Equals(Exercise.Name, Exercise.NameEn, StringComparison.OrdinalIgnoreCase))
                    parts.Add(Exercise.NameEn);
                parts.Add(Exercise.Category);
                if (Exercise.Equipment.Count > 0)
                    parts.Add(string.Join(", ", Exercise.Equipment));
                return string.Join(" · ", parts);
            }
        }
    }
}
