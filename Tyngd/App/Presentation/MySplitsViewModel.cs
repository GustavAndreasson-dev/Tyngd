using System.Collections.ObjectModel;
using Tyngd.Shared.DTOs;
using Tyngd.Shared.TrainingPrograms;

namespace Tyngd.App.Presentation;

/// <summary>Användarens egna splits med sina pass (som /TP/my-splits i webbappen).</summary>
public partial class MySplitsViewModel : ObservableObject
{
    private readonly UserSplitsService _userSplitsService;
    private readonly CatalogService _catalogService;
    private readonly INavigator _navigator;

    public MySplitsViewModel(UserSplitsService userSplitsService, CatalogService catalogService, INavigator navigator)
    {
        _userSplitsService = userSplitsService;
        _catalogService = catalogService;
        _navigator = navigator;
    }

    public ObservableCollection<SplitItem> Splits { get; } = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSuccess))]
    private string? _successMessage;

    public bool HasError => ErrorMessage is not null;
    public bool HasSuccess => SuccessMessage is not null;

    public async Task LoadAsync()
    {
        if (IsLoading)
            return;

        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            var splits = await _userSplitsService.GetMySplitsAsync();
            Splits.Clear();
            foreach (var split in splits)
                Splits.Add(new SplitItem(this, split));
        }
        catch (Exception exception) when (IsNetworkError(exception))
        {
            ErrorMessage = "Kunde inte ladda splits. Kontrollera anslutningen.";
        }
        finally
        {
            IsLoading = false;
            IsEmpty = Splits.Count == 0 && ErrorMessage is null;
        }
    }

    public async Task DeleteAsync(SplitItem split)
    {
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            await _userSplitsService.DeleteMySplitAsync(split.SplitId);
            Splits.Remove(split);
            IsEmpty = Splits.Count == 0;
            SuccessMessage = "Splitten har tagits bort.";
        }
        catch (Exception exception) when (IsNetworkError(exception))
        {
            ErrorMessage = "Kunde inte ta bort split. Kontrollera anslutningen.";
        }
    }

    [RelayCommand]
    private Task NewSplitAsync() =>
        _navigator.NavigateViewModelAsync<EditSplitViewModel>(this, data: new EditSplitArgs(null));

    internal Task EditSplitAsync(int splitId) =>
        _navigator.NavigateViewModelAsync<EditSplitViewModel>(this, data: new EditSplitArgs(splitId));

    internal Task StartPassAsync(int splitId, int passId) =>
        _navigator.NavigateViewModelAsync<PassViewModel>(this, data: new PassArgs(splitId, passId));

    /// <summary>Hela splitten hämtas på en gång, så alla pass i den får sina övningar.</summary>
    internal async Task LoadExercisesAsync(SplitItem split, PassItem expandedPass)
    {
        expandedPass.IsLoading = true;
        try
        {
            var full = await _userSplitsService.GetMySplitFullAsync(split.SplitId);
            if (full is null)
                return;

            var catalog = await _catalogService.GetAllAsync();
            foreach (var pass in full.Passes)
            {
                var item = split.Passes.FirstOrDefault(p => p.PassId == pass.PassId);
                if (item is null)
                    continue;
                item.Exercises = pass.Exercises.Select(e => new ExerciseLine(e.ExerciseName, ExerciseFormat.Summary(e))).ToList();
                item.MuscleLoad = CatalogService.MuscleLoadFor(catalog, pass.Exercises.Select(e => e.CatalogExerciseId));
            }
        }
        catch (Exception exception) when (IsNetworkError(exception))
        {
            ErrorMessage = "Kunde inte ladda övningarna. Kontrollera anslutningen.";
        }
        finally
        {
            expandedPass.IsLoading = false;
        }
    }

    internal async Task ToggleSplitMusclesAsync(SplitItem split)
    {
        split.ShowMuscles = !split.ShowMuscles;
        if (!split.ShowMuscles || split.MuscleLoad is not null)
            return;

        try
        {
            split.MuscleLoad = await _userSplitsService.GetSplitMusclesAsync(split.SplitId);
        }
        catch (Exception exception) when (IsNetworkError(exception))
        {
            split.ShowMuscles = false;
            ErrorMessage = "Kunde inte ladda muskler. Kontrollera anslutningen.";
        }
    }

    private static bool IsNetworkError(Exception exception) =>
        exception is HttpRequestException or TaskCanceledException;
}

public partial class SplitItem : ObservableObject
{
    public SplitItem(MySplitsViewModel owner, TrainingSplit split)
    {
        Owner = owner;
        SplitId = split.SplitId;
        Name = split.SplitName;
        Passes = split.Passes.Select(p => new PassItem(this, p.PassId, p.PassName)).ToList();
    }

    public int SplitId { get; }
    public string Name { get; }
    public IReadOnlyList<PassItem> Passes { get; }

    internal MySplitsViewModel Owner { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMusclesLoading))]
    private bool _showMuscles;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMusclesLoading), nameof(HasMuscleLoad))]
    private IReadOnlyDictionary<string, double>? _muscleLoad;

    public bool IsMusclesLoading => ShowMuscles && MuscleLoad is null;
    public bool HasMuscleLoad => MuscleLoad is not null;

    [RelayCommand]
    private Task ToggleMusclesAsync() => Owner.ToggleSplitMusclesAsync(this);

    [RelayCommand]
    private Task EditAsync() => Owner.EditSplitAsync(SplitId);
}

public partial class PassItem : ObservableObject
{
    private readonly SplitItem _split;

    public PassItem(SplitItem split, int passId, string name)
    {
        _split = split;
        PassId = passId;
        Name = name;
    }

    public int PassId { get; }
    public string Name { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChevronAngle))]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private IReadOnlyList<ExerciseLine>? _exercises;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMuscleLoad))]
    private IReadOnlyDictionary<string, double>? _muscleLoad;

    public bool HasMuscleLoad => MuscleLoad is not null;

    public double ChevronAngle => IsExpanded ? 180 : 0;

    [RelayCommand]
    private async Task ToggleAsync()
    {
        IsExpanded = !IsExpanded;
        if (IsExpanded && Exercises is null && !IsLoading)
            await _split.Owner.LoadExercisesAsync(_split, this);
    }

    [RelayCommand]
    private Task StartAsync() => _split.Owner.StartPassAsync(_split.SplitId, PassId);
}

public record ExerciseLine(string Name, string Detail);
