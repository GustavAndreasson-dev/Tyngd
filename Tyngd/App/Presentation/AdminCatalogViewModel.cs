using System.Collections.ObjectModel;
using Tyngd.Shared.Catalog;

namespace Tyngd.App.Presentation;

/// <summary>Övningskatalogen i admin (som AdminCatalogPanel.razor): synk från wger och granskning av svenska texter.</summary>
public partial class AdminCatalogViewModel : ObservableObject
{
    private const int PageSize = 50;
    private readonly AdminCatalogService _service;
    private List<AdminCatalogExerciseDto> _all = [];
    private int _shown = PageSize;
    private bool _loaded;

    public AdminCatalogViewModel(AdminCatalogService service)
    {
        _service = service;
        Filters =
        [
            new("Att granska", row => row.TranslationStatus is CatalogTranslationStatus.Auto or CatalogTranslationStatus.BehoverGranskning),
            new("Källan ändrad", row => row.TranslationStatus == CatalogTranslationStatus.BehoverGranskning),
            new("Saknar svenska", row => row.TranslationStatus == CatalogTranslationStatus.Saknas),
            new("Granskade", row => row.TranslationStatus == CatalogTranslationStatus.Granskad),
            new("Alla", _ => true),
        ];
        _filter = Filters[0];
    }

    public List<CatalogFilterOption> Filters { get; }

    [ObservableProperty]
    private CatalogFilterOption? _filter;

    [ObservableProperty]
    private string? _search;

    public ObservableCollection<CatalogRow> Rows { get; } = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private bool _hasRows;

    [ObservableProperty]
    private string? _shownText;

    [ObservableProperty]
    private bool _canShowMore;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SyncText), nameof(CanSync))]
    private bool _isSyncing;

    public string SyncText => IsSyncing ? "Synkar…" : "Synka från wger";
    public bool CanSync => !IsSyncing;

    [ObservableProperty]
    private string? _infoMessage;

    [ObservableProperty]
    private string? _errorMessage;

    partial void OnFilterChanged(CatalogFilterOption? value)
    {
        _shown = PageSize;
        Refresh();
    }

    partial void OnSearchChanged(string? value) => Refresh();

    public async Task LoadOnceAsync()
    {
        if (_loaded)
            return;
        _loaded = true;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            _all = await _service.GetAllAsync();
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            _loaded = false;
            ErrorMessage = AdminViewModel.ErrorText(ex, "Kunde inte ladda katalogen.");
        }
        finally
        {
            IsLoading = false;
        }
        Refresh();
    }

    [RelayCommand]
    private async Task SyncAsync()
    {
        IsSyncing = true;
        InfoMessage = null;
        ErrorMessage = null;
        try
        {
            var (result, error) = await _service.SyncAsync();
            if (result is null)
            {
                ErrorMessage = $"Synken misslyckades: {error}";
                return;
            }
            InfoMessage = $"Hämtade {result.Fetched}. Nya {result.Added} (varav {result.TranslatedFromSeed} med svensk text), " +
                $"ändrade {result.TextChanged}, att granska om {result.NeedsReview}, borttagna {result.Deactivated}, tillbaka {result.Reactivated}.";
            await LoadAsync();
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            ErrorMessage = "Synken misslyckades: ingen kontakt med servern.";
        }
        finally
        {
            IsSyncing = false;
        }
    }

    [RelayCommand]
    private void ShowMore()
    {
        _shown += PageSize;
        Refresh();
    }

    private void Refresh()
    {
        foreach (var option in Filters)
            option.Label = $"{option.Name} ({_all.Count(option.Matches)})";

        var filter = Filter ?? Filters[0];
        var visible = _all.Where(filter.Matches);
        if (!string.IsNullOrWhiteSpace(Search))
        {
            var text = Search.Trim();
            visible = visible.Where(row => row.NameEn.Contains(text, StringComparison.CurrentCultureIgnoreCase)
                || (row.NameSv?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false));
        }
        var list = visible.ToList();

        // Behåll radernas objekt så att en påbörjad redigering inte försvinner när man söker.
        var existing = Rows.ToDictionary(row => row.Exercise.CatalogExerciseId);
        Rows.Clear();
        foreach (var exercise in list.Take(_shown))
            Rows.Add(existing.TryGetValue(exercise.CatalogExerciseId, out var row) && row.Exercise == exercise ? row : new CatalogRow(this, exercise));

        IsEmpty = !IsLoading && _all.Count == 0;
        HasRows = _all.Count > 0;
        ShownText = $"Visar {Math.Min(list.Count, _shown)} av {list.Count}";
        CanShowMore = list.Count > _shown;
    }

    internal void Toggle(CatalogRow row)
    {
        var edit = !row.IsEditing;
        foreach (var other in Rows)
            other.IsEditing = false;
        if (!edit)
            return;
        row.EditName = row.Exercise.NameSv ?? row.Exercise.NameEn;
        row.EditDescription = row.Exercise.DescriptionSv ?? string.Empty;
        row.IsEditing = true;
    }

    internal async Task SaveAsync(CatalogRow row)
    {
        row.IsSaving = true;
        InfoMessage = null;
        ErrorMessage = null;
        try
        {
            await _service.UpdateTranslationAsync(row.Exercise.CatalogExerciseId,
                new UpdateCatalogTranslationRequest { NameSv = row.EditName, DescriptionSv = row.EditDescription });
            row.Exercise.NameSv = row.EditName.Trim();
            row.Exercise.DescriptionSv = row.EditDescription;
            row.Exercise.TranslationStatus = CatalogTranslationStatus.Granskad;
            row.IsEditing = false;
            row.Changed();
            InfoMessage = "Sparad som granskad.";
            Refresh();
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            ErrorMessage = AdminViewModel.ErrorText(ex, "Kunde inte spara. Kontrollera att namnet är 1–50 tecken.");
        }
        finally
        {
            row.IsSaving = false;
        }
    }
}

public partial class CatalogFilterOption(string name, Func<AdminCatalogExerciseDto, bool> matches) : ObservableObject
{
    public string Name { get; } = name;
    public Func<AdminCatalogExerciseDto, bool> Matches { get; } = matches;

    [ObservableProperty]
    private string _label = name;
}

public partial class CatalogRow(AdminCatalogViewModel owner, AdminCatalogExerciseDto exercise) : ObservableObject
{
    public AdminCatalogExerciseDto Exercise { get; } = exercise;

    public string Title => Exercise.NameSv ?? Exercise.NameEn;
    public string Subtitle => $"{Exercise.NameEn} · {Exercise.Category}{(Exercise.IsActive ? "" : " · borttagen hos wger")}";
    public string DescriptionEn => string.IsNullOrWhiteSpace(Exercise.DescriptionEn) ? "–" : Exercise.DescriptionEn;

    public string StatusText => Exercise.TranslationStatus switch
    {
        CatalogTranslationStatus.Auto => "Maskinöversatt",
        CatalogTranslationStatus.Granskad => "Granskad",
        CatalogTranslationStatus.BehoverGranskning => "Källan ändrad",
        _ => "Saknas",
    };

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private bool _isSaving;

    public bool CanSave => !IsSaving;

    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string _editDescription = string.Empty;

    internal void Changed() => OnPropertyChanged(string.Empty);

    [RelayCommand]
    private void Toggle() => owner.Toggle(this);

    [RelayCommand]
    private Task SaveAsync() => owner.SaveAsync(this);

    [RelayCommand]
    private void Cancel() => IsEditing = false;
}
