using Tyngd.Shared.TrainingPrograms;

namespace Tyngd.App.Presentation;

/// <summary>Splitmallar som användaren kan aktivera (som /TP/templates i webbappen).</summary>
public partial class TemplatesViewModel : ObservableObject
{
    private readonly SplitTemplateService _splitTemplateService;
    private readonly UserSplitsService _userSplitsService;
    private readonly LoginState _loginState;

    public TemplatesViewModel(SplitTemplateService splitTemplateService, UserSplitsService userSplitsService, LoginState loginState)
    {
        _splitTemplateService = splitTemplateService;
        _userSplitsService = userSplitsService;
        _loginState = loginState;
    }

    [ObservableProperty]
    private IReadOnlyList<TemplateItem> _templates = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    private string UserId => _loginState.CurrentUser?.UserId ?? string.Empty;

    public async Task LoadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var templates = await _splitTemplateService.GetAllAsync();
            var active = await _userSplitsService.GetActiveSplitsAsync(UserId);
            Templates = templates.Select(template => new TemplateItem(template, active.Contains(template.SplitId), ToggleAsync)).ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            ErrorMessage = "Kunde inte ladda splitmallar.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ToggleAsync(TemplateItem item)
    {
        ErrorMessage = null;
        try
        {
            if (item.IsActive)
                await _userSplitsService.DeactivateSplitAsync(UserId, item.SplitId);
            else
                await _userSplitsService.ActivateSplitAsync(UserId, item.SplitId);
            item.IsActive = !item.IsActive;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            ErrorMessage = item.IsActive ? "Kunde inte avaktivera split." : "Kunde inte aktivera split.";
        }
    }
}

public partial class TemplateItem : ObservableObject
{
    private readonly Func<TemplateItem, Task> _toggle;

    public TemplateItem(TrainingSplitTemplate template, bool isActive, Func<TemplateItem, Task> toggle)
    {
        SplitId = template.SplitId;
        SplitName = template.SplitName;
        Passes = template.Passes
            .Select(pass => new TemplatePass(pass.PassName, pass.Exercises.Select(e => new TemplateExercise(e.Exercise.ExerciseName, FormatDefault(e))).ToList()))
            .ToList();
        _isActive = isActive;
        _toggle = toggle;
    }

    public int SplitId { get; }
    public string SplitName { get; }
    public IReadOnlyList<TemplatePass> Passes { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInactive))]
    private bool _isActive;

    public bool IsInactive => !IsActive;

    [RelayCommand]
    private Task ToggleAsync() => _toggle(this);

    /// <summary>Formaterar defaultvärden för en övningsmall som läsbar text.</summary>
    private static string FormatDefault(TrainingExerciseTemplate exercise)
    {
        var reps = exercise.DefaultRepsMin == exercise.DefaultRepsMax
            ? exercise.DefaultRepsMin.ToString()
            : $"{exercise.DefaultRepsMin}-{exercise.DefaultRepsMax}";

        var weight = exercise.DefaultWeight == 0
            ? "kroppsvikt"
            : $"{exercise.DefaultWeight:0.##} kg";

        return $"{exercise.DefaultSets} x {reps} @ {weight}";
    }
}

public record TemplatePass(string PassName, IReadOnlyList<TemplateExercise> Exercises);

public record TemplateExercise(string Name, string Detail);
