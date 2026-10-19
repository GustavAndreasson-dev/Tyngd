using System.Collections.ObjectModel;
using Tyngd.Shared.DTOs;
using Tyngd.Shared.TrainingPrograms;

namespace Tyngd.App.Presentation;

/// <summary>Skapa en ny split eller redigera en befintlig (som /create-split och /edit-split/{SplitId}).</summary>
public partial class EditSplitViewModel : ObservableObject
{
    private readonly UserSplitsService _userSplitsService;
    private readonly SplitTemplateService _splitTemplateService;
    private readonly INavigator _navigator;

    public EditSplitViewModel(EditSplitArgs args, UserSplitsService userSplitsService, SplitTemplateService splitTemplateService, INavigator navigator)
    {
        Args = args;
        _userSplitsService = userSplitsService;
        _splitTemplateService = splitTemplateService;
        _navigator = navigator;

        if (IsEditMode)
            _ = LoadForEditAsync(args.SplitId!.Value);
        else
            AddPass();
    }

    public EditSplitArgs Args { get; }
    public bool IsEditMode => Args.SplitId.HasValue;
    public bool IsCreateMode => !IsEditMode;
    public string Title => IsEditMode ? "Redigera split" : "Skapa split";
    public string SaveText => IsSaving ? "Sparar..." : IsEditMode ? "Spara ändringar" : "Spara split";

    public ObservableCollection<EditPassItem> Passes { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSplitNameError))]
    private string? _splitNameError;

    public bool HasSplitNameError => SplitNameError is not null;

    [ObservableProperty]
    private string _splitName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusy), nameof(IsIdle))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusy), nameof(IsIdle), nameof(SaveText))]
    private bool _isSaving;

    public bool IsBusy => IsLoading || IsSaving;
    public bool IsIdle => !IsBusy;

    [ObservableProperty]
    private string? _errorMessage;

    private async Task LoadForEditAsync(int splitId)
    {
        IsLoading = true;
        try
        {
            var split = await _userSplitsService.GetMySplitFullAsync(splitId);
            if (split is null)
            {
                await _navigator.NavigateBackAsync(this);
                return;
            }
            Fill(split);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            ErrorMessage = "Kunde inte ladda splitten.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Hämtar egna splits och mallar för importdialogen; null om det inte gick.</summary>
    public async Task<(List<TrainingSplit> MySplits, List<TrainingSplitTemplate> Templates)?> LoadImportSourcesAsync()
    {
        ErrorMessage = null;
        IsLoading = true;
        try
        {
            return (await _userSplitsService.GetMySplitsAsync(), await _splitTemplateService.GetAllAsync());
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            ErrorMessage = "Kunde inte ladda splits.";
            return null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ImportTemplate(TrainingSplitTemplate template) => Fill(UserSplitsService.FromTemplate(template));

    public async Task ImportMySplitAsync(TrainingSplit split)
    {
        IsLoading = true;
        try
        {
            if (await _userSplitsService.GetMySplitFullAsync(split.SplitId) is { } full)
                Fill(UserSplitsService.AsNewCopy(full));
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            ErrorMessage = "Kunde inte hämta splitten.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Fill(CreateSplitRequest split)
    {
        SplitName = split.SplitName;
        Passes.Clear();
        foreach (var pass in split.Passes)
            Passes.Add(new EditPassItem(this, pass));
        ClearErrors();
        Renumber();
    }

    [RelayCommand]
    private void AddPass()
    {
        foreach (var pass in Passes)
            pass.IsExpanded = false;
        Passes.Add(new EditPassItem(this, new CreatePassDto()) { IsExpanded = true });
        Renumber();
    }

    internal void RemovePass(EditPassItem pass)
    {
        Passes.Remove(pass);
        Renumber();
    }

    internal void MovePass(EditPassItem pass, int delta)
    {
        var index = Passes.IndexOf(pass);
        if (index + delta < 0 || index + delta >= Passes.Count) return;
        Passes.Move(index, index + delta);
        Renumber();
    }

    internal void TogglePass(EditPassItem pass)
    {
        var expand = !pass.IsExpanded;
        foreach (var other in Passes)
            other.IsExpanded = false;
        pass.IsExpanded = expand;
    }

    private void Renumber()
    {
        for (var index = 0; index < Passes.Count; index++)
        {
            Passes[index].Number = index + 1;
            Passes[index].Renumber();
        }
        RevalidateIfShown();
    }

    [RelayCommand]
    private Task CancelAsync() => _navigator.NavigateBackAsync(this);

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy) return;

        if (!ShowValidation(out var request))
        {
            ExpandFirstError();
            return;
        }

        IsSaving = true;
        try
        {
            if (Args.SplitId is int splitId)
                await _userSplitsService.UpdateCustomSplitAsync(splitId, request);
            else
                await _userSplitsService.CreateCustomSplitAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            // Redigeringarna ligger kvar så att användaren kan försöka igen.
            ErrorMessage = "Kunde inte spara splitten. Kontrollera anslutningen och försök igen.";
            return;
        }
        finally
        {
            IsSaving = false;
        }

        await _navigator.NavigateBackAsync(this);
    }

    private void ClearErrors() => ApplyErrors(new Dictionary<string, string>());

    // Efter ett misslyckat sparförsök valideras om när pass och övningar läggs till eller tas bort,
    // så att t.ex. "minst en övning" försvinner när övningen läggs till (annars låg felet kvar till nästa sparning).
    private bool _errorsShown;

    private bool ShowValidation(out CreateSplitRequest request)
    {
        request = new CreateSplitRequest
        {
            SplitName = SplitName.Trim(),
            Passes = Passes.Select(pass => pass.ToDto()).ToList(),
        };

        var errors = SplitRequestValidator.Validate(request)
            .GroupBy(error => error.Key)
            .ToDictionary(group => group.Key, group => group.First().Message);
        ApplyErrors(errors);
        _errorsShown = errors.Count > 0;
        ErrorMessage = errors.Count == 0 ? null
            : errors.TryGetValue("Passes", out var passesError) ? passesError
            : "Kontrollera de markerade fälten innan du sparar.";
        return errors.Count == 0;
    }

    internal void RevalidateIfShown()
    {
        if (_errorsShown)
            ShowValidation(out _);
    }

    private void ApplyErrors(Dictionary<string, string> errors)
    {
        SplitNameError = errors.GetValueOrDefault("SplitName");
        for (var passIndex = 0; passIndex < Passes.Count; passIndex++)
        {
            var pass = Passes[passIndex];
            var passKey = $"Passes[{passIndex}]";
            pass.NameError = errors.GetValueOrDefault($"{passKey}.PassName");
            pass.ExercisesError = errors.GetValueOrDefault($"{passKey}.Exercises");
            for (var exerciseIndex = 0; exerciseIndex < pass.Exercises.Count; exerciseIndex++)
            {
                var exercise = pass.Exercises[exerciseIndex];
                var exerciseKey = $"{passKey}.Exercises[{exerciseIndex}]";
                exercise.NameError = errors.GetValueOrDefault($"{exerciseKey}.ExerciseName");
                var valueErrors = new[] { "DefaultSets", "RepsMin", "RepsMax", "DefaultWeight" }
                    .Select(field => errors.GetValueOrDefault($"{exerciseKey}.{field}"))
                    .OfType<string>()
                    .ToList();
                exercise.ValuesError = valueErrors.Count > 0 ? string.Join("\n", valueErrors) : null;
            }
        }
    }

    private void ExpandFirstError()
    {
        if (Passes.FirstOrDefault(pass => pass.HasErrors) is not { } pass)
            return;
        foreach (var other in Passes)
            other.IsExpanded = other == pass;
        if (pass.Exercises.FirstOrDefault(exercise => exercise.HasErrors) is { } exercise)
            foreach (var other in pass.Exercises)
                other.IsExpanded = other == exercise;
    }
}

public partial class EditPassItem : ObservableObject
{
    private readonly EditSplitViewModel _owner;
    private readonly int? _passId;

    public EditPassItem(EditSplitViewModel owner, CreatePassDto pass)
    {
        _owner = owner;
        _passId = pass.PassId;
        _passName = pass.PassName;
        foreach (var exercise in pass.Exercises)
            Exercises.Add(new EditExerciseItem(this, exercise));
    }

    public ObservableCollection<EditExerciseItem> Exercises { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private string _passName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private int _number;

    public string Title => string.IsNullOrWhiteSpace(PassName) ? $"Pass {Number}" : PassName;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNameError))]
    private string? _nameError;

    public bool HasNameError => NameError is not null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasExercisesError))]
    private string? _exercisesError;

    public bool HasExercisesError => ExercisesError is not null;

    public bool HasErrors => HasNameError || HasExercisesError || Exercises.Any(exercise => exercise.HasErrors);

    [RelayCommand]
    private void Toggle() => _owner.TogglePass(this);

    [RelayCommand]
    private void Remove() => _owner.RemovePass(this);

    [RelayCommand]
    private void MoveUp() => _owner.MovePass(this, -1);

    [RelayCommand]
    private void MoveDown() => _owner.MovePass(this, 1);

    [RelayCommand]
    private void AddExercise()
    {
        foreach (var exercise in Exercises)
            exercise.IsExpanded = false;
        Exercises.Add(new EditExerciseItem(this, new CreateExerciseDto()) { IsExpanded = true });
        Renumber();
        _owner.RevalidateIfShown();
    }

    internal void RemoveExercise(EditExerciseItem exercise)
    {
        Exercises.Remove(exercise);
        Renumber();
        _owner.RevalidateIfShown();
    }

    internal void MoveExercise(EditExerciseItem exercise, int delta)
    {
        var index = Exercises.IndexOf(exercise);
        if (index + delta < 0 || index + delta >= Exercises.Count) return;
        Exercises.Move(index, index + delta);
        Renumber();
    }

    internal void ToggleExercise(EditExerciseItem exercise)
    {
        var expand = !exercise.IsExpanded;
        foreach (var other in Exercises)
            other.IsExpanded = false;
        exercise.IsExpanded = expand;
    }

    internal void Renumber()
    {
        for (var index = 0; index < Exercises.Count; index++)
            Exercises[index].Number = index + 1;
    }

    public CreatePassDto ToDto() => new()
    {
        PassId = _passId,
        PassName = PassName,
        Exercises = Exercises.Select(exercise => exercise.ToDto()).ToList(),
    };
}

public partial class EditExerciseItem : ObservableObject
{
    private readonly EditPassItem _owner;
    private readonly int? _exerciseId;

    public EditExerciseItem(EditPassItem owner, CreateExerciseDto exercise)
    {
        _owner = owner;
        _exerciseId = exercise.ExerciseId;
        _exerciseName = exercise.ExerciseName;
        _catalogExerciseId = exercise.CatalogExerciseId;
        _defaultSets = exercise.DefaultSets.ToString();
        _repsMin = exercise.RepsMin.ToString();
        _repsMax = exercise.RepsMax.ToString();
        _defaultWeight = NumberInput.Format(exercise.DefaultWeight);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private string _exerciseName;

    [ObservableProperty]
    private int? _catalogExerciseId;

    // Text (inte NumberBox) så att värdet finns i ViewModel vid varje tecken, även om man trycker Spara direkt.
    [ObservableProperty]
    private string _defaultSets;

    [ObservableProperty]
    private string _repsMin;

    [ObservableProperty]
    private string _repsMax;

    [ObservableProperty]
    private string _defaultWeight;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private int _number;

    public string Title => string.IsNullOrWhiteSpace(ExerciseName) ? $"Övning {Number}" : ExerciseName;

    /// <summary>Ett byte av övning på en sparad rad behåller dess historik.</summary>
    public string? HelperText => _exerciseId is null ? null : "Byte behåller samma historik. Ta bort och lägg till för en ny statistikserie.";

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private string? _nameError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValuesError))]
    private string? _valuesError;

    public bool HasValuesError => ValuesError is not null;

    public bool HasErrors => NameError is not null || HasValuesError;

    [RelayCommand]
    private void Toggle() => _owner.ToggleExercise(this);

    [RelayCommand]
    private void Remove() => _owner.RemoveExercise(this);

    [RelayCommand]
    private void MoveUp() => _owner.MoveExercise(this, -1);

    [RelayCommand]
    private void MoveDown() => _owner.MoveExercise(this, 1);

    public CreateExerciseDto ToDto() => new()
    {
        ExerciseId = _exerciseId,
        ExerciseName = ExerciseName,
        CatalogExerciseId = CatalogExerciseId,
        DefaultSets = ToInt(DefaultSets),
        RepsMin = ToInt(RepsMin),
        RepsMax = ToInt(RepsMax),
        DefaultWeight = NumberInput.TryParse(DefaultWeight, out var weight) && weight <= 100_000 ? Math.Round(weight, 2) : -1,
    };

    // Ogiltiga värden blir 0/-1 så att SplitRequestValidator visar felet i stället för att det skickas.
    private static int ToInt(string text) =>
        NumberInput.TryParse(text, out var value) && value <= 100_000 ? (int)Math.Round(value) : 0;
}
