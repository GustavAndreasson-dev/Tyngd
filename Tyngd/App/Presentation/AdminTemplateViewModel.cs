using System.Collections.ObjectModel;
using Tyngd.Shared.DTOs;

namespace Tyngd.App.Presentation;

/// <summary>Skapa eller redigera en split-mall (som AdminTemplateEditor.razor).</summary>
public partial class AdminTemplateViewModel : ObservableObject
{
    private readonly AdminService _adminService;
    private readonly INavigator _navigator;
    private bool _loaded;

    public AdminTemplateViewModel(AdminTemplateArgs args, AdminService adminService, INavigator navigator)
    {
        Args = args;
        _adminService = adminService;
        _navigator = navigator;
        if (!IsEditMode)
            AddPass();
    }

    public AdminTemplateArgs Args { get; }
    public bool IsEditMode => Args.TemplateId.HasValue;
    public string Title => IsEditMode ? "Redigera mall" : "Skapa mall";
    public string SaveText => IsSaving ? "Sparar..." : "Spara mall";

    public ObservableCollection<AdminPassItem> Passes { get; } = [];

    [ObservableProperty]
    private string _templateName = string.Empty;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle), nameof(SaveText))]
    private bool _isSaving;

    public bool IsIdle => !IsLoading && !IsSaving;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Laddar mallen som ska redigeras (en gång; sidan anropar när den fått sin DataContext).</summary>
    public async Task LoadAsync()
    {
        if (Args.TemplateId is not int templateId || _loaded)
            return;
        _loaded = true;
        IsLoading = true;
        try
        {
            if (await _adminService.GetTemplateAsync(templateId) is not { } template)
            {
                ErrorMessage = "Mallen hittades inte.";
                return;
            }
            TemplateName = template.SplitTemplateName;
            IsVisible = template.IsVisible;
            foreach (var pass in template.Passes)
                Passes.Add(new AdminPassItem(this, new UpsertAdminPassTemplateRequest
                {
                    PassTemplateId = pass.PassTemplateId,
                    PassTemplateName = pass.PassTemplateName,
                    IsVisible = pass.IsVisible,
                    Exercises = pass.Exercises.Select(exercise => new UpsertAdminExerciseTemplateRequest
                    {
                        ExerciseTemplateId = exercise.ExerciseTemplateId,
                        ExerciseTemplateName = exercise.ExerciseTemplateName,
                        CatalogExerciseId = exercise.CatalogExerciseId,
                        IsVisible = exercise.IsVisible,
                        DefaultSets = exercise.DefaultSets,
                        DefaultRepsMin = exercise.DefaultRepsMin,
                        DefaultRepsMax = exercise.DefaultRepsMax,
                        DefaultWeight = exercise.DefaultWeight,
                    }).ToList(),
                }));
            Renumber();
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            _loaded = false;
            ErrorMessage = AdminViewModel.ErrorText(ex, "Kunde inte ladda mallen.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void AddPass()
    {
        Passes.Add(new AdminPassItem(this, new UpsertAdminPassTemplateRequest
        {
            PassTemplateName = $"Pass {Passes.Count + 1}",
            Exercises = [new UpsertAdminExerciseTemplateRequest()],
        }));
        Renumber();
    }

    internal void RemovePass(AdminPassItem pass)
    {
        Passes.Remove(pass);
        Renumber();
    }

    private void Renumber()
    {
        for (var index = 0; index < Passes.Count; index++)
            Passes[index].Number = index + 1;
    }

    [RelayCommand]
    private Task CancelAsync() => _navigator.NavigateBackAsync(this);

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!IsIdle)
            return;

        var request = new UpsertAdminSplitTemplateRequest
        {
            SplitTemplateName = TemplateName,
            IsVisible = IsVisible,
            Passes = Passes.Select(pass => pass.ToRequest()).ToList(),
        };
        ErrorMessage = Validate(request);
        if (ErrorMessage is not null)
            return;

        IsSaving = true;
        try
        {
            if (Args.TemplateId is int templateId)
                await _adminService.UpdateTemplateAsync(templateId, request);
            else
                await _adminService.CreateTemplateAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            ErrorMessage = AdminViewModel.ErrorText(ex, "Kunde inte spara mallen.");
            return;
        }
        finally
        {
            IsSaving = false;
        }

        await _navigator.NavigateBackAsync(this);
    }

    // Samma kontroller och texter som AdminTemplateEditor.Validate i webbappen.
    private static string? Validate(UpsertAdminSplitTemplateRequest model)
    {
        if (string.IsNullOrWhiteSpace(model.SplitTemplateName)) return "Ange ett mallnamn.";
        if (model.Passes.Count == 0) return "Lägg till minst ett pass.";
        if (model.Passes.Any(pass => string.IsNullOrWhiteSpace(pass.PassTemplateName))) return "Alla pass måste ha ett namn.";
        if (model.Passes.Any(pass => pass.Exercises.Count == 0)) return "Alla pass måste innehålla minst en övning.";
        if (model.Passes.SelectMany(pass => pass.Exercises).Any(exercise => string.IsNullOrWhiteSpace(exercise.ExerciseTemplateName))) return "Alla övningar måste ha ett namn.";
        if (model.Passes.SelectMany(pass => pass.Exercises).Any(exercise => exercise.DefaultRepsMin > exercise.DefaultRepsMax)) return "Reps min får inte vara större än reps max.";
        return null;
    }
}

public partial class AdminPassItem : ObservableObject
{
    private readonly AdminTemplateViewModel _owner;
    private readonly int? _passTemplateId;

    public AdminPassItem(AdminTemplateViewModel owner, UpsertAdminPassTemplateRequest pass)
    {
        _owner = owner;
        _passTemplateId = pass.PassTemplateId;
        _passName = pass.PassTemplateName;
        _isVisible = pass.IsVisible;
        foreach (var exercise in pass.Exercises)
            Exercises.Add(new AdminExerciseItem(this, exercise));
    }

    public ObservableCollection<AdminExerciseItem> Exercises { get; } = [];

    [ObservableProperty]
    private string _passName;

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private int _number;

    public string Title => $"Pass {Number}";

    [RelayCommand]
    private void Remove() => _owner.RemovePass(this);

    [RelayCommand]
    private void AddExercise() => Exercises.Add(new AdminExerciseItem(this, new UpsertAdminExerciseTemplateRequest()));

    internal void RemoveExercise(AdminExerciseItem exercise) => Exercises.Remove(exercise);

    public UpsertAdminPassTemplateRequest ToRequest() => new()
    {
        PassTemplateId = _passTemplateId,
        PassTemplateName = PassName,
        IsVisible = IsVisible,
        Exercises = Exercises.Select(exercise => exercise.ToRequest()).ToList(),
    };
}

public partial class AdminExerciseItem : ObservableObject
{
    private readonly AdminPassItem _owner;
    private readonly int? _exerciseTemplateId;

    public AdminExerciseItem(AdminPassItem owner, UpsertAdminExerciseTemplateRequest exercise)
    {
        _owner = owner;
        _exerciseTemplateId = exercise.ExerciseTemplateId;
        _exerciseName = exercise.ExerciseTemplateName;
        _catalogExerciseId = exercise.CatalogExerciseId;
        _isVisible = exercise.IsVisible;
        _setsText = NumberInput.Format(exercise.DefaultSets);
        _repsMinText = NumberInput.Format(exercise.DefaultRepsMin);
        _repsMaxText = NumberInput.Format(exercise.DefaultRepsMax);
        _weightText = NumberInput.Format(exercise.DefaultWeight);
    }

    [ObservableProperty]
    private string _exerciseName;

    [ObservableProperty]
    private int? _catalogExerciseId;

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private string _setsText;

    [ObservableProperty]
    private string _repsMinText;

    [ObservableProperty]
    private string _repsMaxText;

    [ObservableProperty]
    private string _weightText;

    [RelayCommand]
    private void Remove() => _owner.RemoveExercise(this);

    public UpsertAdminExerciseTemplateRequest ToRequest() => new()
    {
        ExerciseTemplateId = _exerciseTemplateId,
        ExerciseTemplateName = ExerciseName,
        CatalogExerciseId = CatalogExerciseId,
        IsVisible = IsVisible,
        DefaultSets = Read(SetsText, 1, 20),
        DefaultRepsMin = Read(RepsMinText, 1, 100),
        DefaultRepsMax = Read(RepsMaxText, 1, 100),
        DefaultWeight = Read(WeightText, 0, 1000),
    };

    // Begränsas till samma intervall som webbens fält (Min/Max); ogiltig text blir minsta värdet.
    private static int Read(string text, int min, int max) =>
        NumberInput.TryParse(text, out var value) ? (int)Math.Clamp(Math.Round(value), min, max) : min;
}
