using System.Collections.ObjectModel;
using Microsoft.UI.Dispatching;
using Tyngd.Client.Models;
using Tyngd.Shared.DTOs;
using Tyngd.Shared.TrainingPrograms;
using Windows.Foundation;

namespace Tyngd.App.Presentation;

/// <summary>
/// Ett träningspass (PassPage i webbappen). Pågående data sparas lokalt i <see cref="IAppStorage"/> under "pass-{SplitId}-{PassId}"
/// efter varje ändring och synkas till API:t när användaren lämnar en övning eller avslutar passet.
/// </summary>
public partial class PassViewModel : ObservableObject
{
    private readonly IAppStorage _storage;
    private readonly RestTimerState _restTimer;
    private readonly SessionState _session;
    private readonly UserSplitsService _userSplits;
    private readonly PassLogService _passLog;
    private readonly PendingSyncState _pendingSync;
    private readonly FreeSessionService _freeSessions;
    private readonly INavigator _navigator;
    private DispatcherQueue? _dispatcher;

    private TrainingPassTemplate? _pass;
    private PassData _data = new();
    private readonly Dictionary<int, decimal> _weights = new();
    private readonly Dictionary<int, List<ExerciseProgressPointDto>> _progress = new();
    private bool _isNewPassData;
    private bool _loadStarted;
    private int _activeIndex;
    private int _shownExerciseId = -1;
    // Sant medan vikt/anteckning fylls i från koden, så att det inte räknas som en ändring.
    private bool _showing;

    public PassViewModel(PassArgs args, IAppStorage storage, RestTimerState restTimer, SessionState session,
        UserSplitsService userSplits, PassLogService passLog, PendingSyncState pendingSync, FreeSessionService freeSessions,
        CatalogService catalog, FavoritesService favorites, INavigator navigator)
    {
        Args = args;
        _storage = storage;
        _restTimer = restTimer;
        _session = session;
        _userSplits = userSplits;
        _passLog = passLog;
        _pendingSync = pendingSync;
        _freeSessions = freeSessions;
        Catalog = catalog;
        Favorites = favorites;
        _navigator = navigator;
    }

    public PassArgs Args { get; }

    // Dialogerna (som sidan öppnar) behöver dessa tjänster.
    internal CatalogService Catalog { get; }
    internal FavoritesService Favorites { get; }
    internal FreeSessionService FreeSessions => _freeSessions;
    internal UserSplitsService UserSplits => _userSplits;

    private string PassKey => $"pass-{Args.SplitId}-{Args.PassId}";

    public ObservableCollection<PassChip> Chips { get; } = new();
    public ObservableCollection<PassSet> Sets { get; } = new();

    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private bool _notFound;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _isStatusOpen;
    [ObservableProperty] private InfoBarSeverity _statusSeverity = InfoBarSeverity.Informational;
    [ObservableProperty] private string _passName = string.Empty;
    [ObservableProperty] private bool _isFreeSession;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanAddExercise))] private bool _isAddingExercise;
    [ObservableProperty] private string _exerciseLabel = string.Empty;
    [ObservableProperty] private string _setsSummary = string.Empty;
    [ObservableProperty] private string _doneText = string.Empty;
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private bool _hasActive;
    [ObservableProperty] private string _exerciseName = string.Empty;
    [ObservableProperty] private string _setsReps = string.Empty;
    [ObservableProperty] private bool _hasCatalogInfo;
    [ObservableProperty] private double _weight;
    [ObservableProperty] private string _note = string.Empty;
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(PreviousCommand))] private bool _canGoPrevious;
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(NextCommand))] private bool _canGoNext;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsNotAllDone))] private bool _allDone;
    [ObservableProperty] private bool _hasProgress;
    [ObservableProperty] private string _progressSummary = string.Empty;
    [ObservableProperty] private PointCollection _progressPoints = new();
    [ObservableProperty] private bool _showMuscles;
    [ObservableProperty] private bool _isLoadingMuscles;
    [ObservableProperty] private IReadOnlyDictionary<string, double>? _muscleLoad;
    [ObservableProperty] private bool _isResting;
    [ObservableProperty] private string _restTime = string.Empty;
    [ObservableProperty] private double _restPercent;

    public bool CanAddExercise => !IsAddingExercise;
    public bool IsNotAllDone => !AllDone;
    public bool HasError => ErrorMessage is not null;

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

    /// <summary>Katalog-id:n som redan finns i passet.</summary>
    public IReadOnlySet<int> TakenCatalogIds =>
        _pass?.Exercises.Select(e => e.CatalogExerciseId).OfType<int>().ToHashSet() ?? new HashSet<int>();

    private TrainingExerciseTemplate? ActiveExercise =>
        _pass is not null && _activeIndex >= 0 && _activeIndex < _pass.Exercises.Count ? _pass.Exercises[_activeIndex] : null;

    /// <summary>Sidan visas (även efter tillbaka från en annan sida). Laddar passet första gången.</summary>
    public async Task ActivateAsync()
    {
        _dispatcher ??= DispatcherQueue.GetForCurrentThread();
        _restTimer.OnTick += OnRestTick;
        _restTimer.Finished += OnRestFinished;
        UpdateRest();

        if (_loadStarted)
        {
            if (IsLoaded)
                _pendingSync.SetActivePassKey(PassKey);
            return;
        }

        _loadStarted = true;
        await LoadAsync();
    }

    /// <summary>Sidan lämnas: släpp singleton-händelserna, som Dispose i webbappen.</summary>
    public void Deactivate()
    {
        _restTimer.OnTick -= OnRestTick;
        _restTimer.Finished -= OnRestFinished;
        _pendingSync.ClearActivePassKey(PassKey);
    }

    private async Task LoadAsync()
    {
        await RunAsync(async () =>
        {
            // Senaste sessionen behövs nästan alltid; hämta den samtidigt som passet.
            var lastSessionTask = _passLog.GetLastSessionAsync(Args.PassId);
            _pass = await _userSplits.GetPassAsync(Args.SplitId, Args.PassId);
            if (_pass is null)
            {
                NotFound = true;
                return;
            }

            await InitializePassDataAsync(ConsumeStartNew(), lastSessionTask);
            InitializeDefaults();
            await _storage.SetItemAsync(PassKey, _data);
            RestoreSessionStateFromLocalData();

            // En vila från samma pass får fortsätta; bara ett nytt pass nollställer den.
            if (_isNewPassData)
            {
                _session.StartNewPass(Args.SplitId, Args.PassId);
                _ = _restTimer.CancelAsync();
            }
            else
                _session.StartPass(Args.SplitId, Args.PassId);

            await _pendingSync.RefreshAsync();
            _pendingSync.SetActivePassKey(PassKey);
            _activeIndex = FirstIncompleteExerciseIndex();
            PassName = _pass.PassName;
            IsFreeSession = _pass.IsFreeSession;
            BuildChips();
            IsLoaded = true;
            Refresh();
        }, "Kunde inte ladda passet");
        IsLoading = false;
    }

    // Samma args-instans kan laddas igen om sidan återskapas vid tillbakanavigering; då får den inte avsluta passet som just startats.
    private static WeakReference<PassArgs>? s_consumedStartNew;

    /// <summary>Webbens ?new=1: starta ett nytt pass i stället för att återuppta. Gäller bara första laddningen med dessa args.</summary>
    private bool ConsumeStartNew()
    {
        if (!Args.StartNew || (s_consumedStartNew?.TryGetTarget(out var consumed) == true && ReferenceEquals(consumed, Args)))
            return false;
        s_consumedStartNew = new WeakReference<PassArgs>(Args);
        return true;
    }

    private async Task InitializePassDataAsync(bool forceNewSession, Task<LastSessionDto?> lastSessionTask)
    {
        var now = DateTimeOffset.UtcNow;
        var stored = await _storage.GetItemAsync<PassData>(PassKey);
        if (stored is not null)
        {
            stored.NormalizeSyncState();
            stored.NormalizeSessionIdentity(now);
            _data = stored;

            if (!forceNewSession && PassResumePolicy.ShouldResume(stored, now))
            {
                _isNewPassData = false;
                return;
            }

            if (stored.CompletedAtUtc is null)
                await _pendingSync.FinalizeAsync(PassKey);

            _data = CreateNewPassData(now);
            _isNewPassData = true;
            ApplyLastSession(await lastSessionTask, includeCompletedSets: false);
            return;
        }

        var lastSession = await lastSessionTask;
        if (lastSession?.SessionKey is not null
            && lastSession.StartedAtUtc is not null
            && lastSession.CompletedAtUtc is null)
        {
            var serverData = CreatePassDataFromSession(lastSession);
            if (!forceNewSession && PassResumePolicy.ShouldResume(serverData, now))
            {
                _data = serverData;
                _isNewPassData = false;
                ApplyLastSession(lastSession, includeCompletedSets: true);
                return;
            }

            await _passLog.CompleteSessionAsync(lastSession.SessionKey.Value);
        }

        _data = CreateNewPassData(now);
        _isNewPassData = true;
        ApplyLastSession(lastSession, includeCompletedSets: false);
    }

    private void ApplyLastSession(LastSessionDto? lastSession, bool includeCompletedSets)
    {
        if (lastSession is null || _pass is null)
            return;

        foreach (var ex in lastSession.Exercises)
        {
            var template = _pass.Exercises.FirstOrDefault(e => e.ExerciseId == ex.ExerciseId);
            if (template is null)
                continue;

            _weights[ex.ExerciseId] = ex.Weight;
            _data.Weights[ex.ExerciseId] = ex.Weight;

            if (ex.Sets.Count > 0)
            {
                var reps = new double[template.DefaultSets];
                for (int i = 0; i < reps.Length; i++)
                    reps[i] = template.DefaultRepsMin;

                foreach (var set in ex.Sets)
                    if (set.SetIndex >= 0 && set.SetIndex < reps.Length)
                        reps[set.SetIndex] = set.Reps;

                _data.Reps[ex.ExerciseId] = reps;
            }

            if (!string.IsNullOrEmpty(ex.NoteText))
                _data.Notes[ex.ExerciseId] = ex.NoteText;

            _data.SavedExercises[ex.ExerciseId] = true;
        }

        if (includeCompletedSets)
        {
            _session.StartPass(Args.SplitId, Args.PassId);
            foreach (var ex in lastSession.Exercises)
            {
                var template = _pass.Exercises.FirstOrDefault(e => e.ExerciseId == ex.ExerciseId);
                if (template is null)
                    continue;

                var doneSets = new bool[template.DefaultSets];
                foreach (var set in ex.Sets)
                    if (set.SetIndex >= 0 && set.SetIndex < doneSets.Length)
                        doneSets[set.SetIndex] = true;

                _data.CompletedSets[ex.ExerciseId] = doneSets;
                _session.RestoreSets(ex.ExerciseId, doneSets);
            }
        }

        _data.IsSynced = true;
    }

    private static PassData CreateNewPassData(DateTimeOffset now) => new()
    {
        SessionKey = Guid.NewGuid(),
        StartedAtUtc = now.ToUniversalTime(),
        IsSynced = true,
    };

    private static PassData CreatePassDataFromSession(LastSessionDto session) => new()
    {
        SessionKey = session.SessionKey ?? Guid.NewGuid(),
        StartedAtUtc = session.StartedAtUtc ?? DateTimeOffset.UtcNow,
        CompletedAtUtc = session.CompletedAtUtc,
        IsSynced = true,
    };

    /// <summary>Fyller i defaultvärden för vikt och reps för övningar som saknar sparad data.</summary>
    private void InitializeDefaults()
    {
        foreach (var ex in _pass!.Exercises)
        {
            if (_data.Weights.TryGetValue(ex.ExerciseId, out var storedWeight))
                _weights[ex.ExerciseId] = storedWeight;
            else if (!_weights.ContainsKey(ex.ExerciseId))
            {
                _weights[ex.ExerciseId] = ex.DefaultWeight;
                _data.Weights[ex.ExerciseId] = ex.DefaultWeight;
            }

            if (!_data.Reps.ContainsKey(ex.ExerciseId))
            {
                _data.Reps[ex.ExerciseId] = new double[ex.DefaultSets];
                for (int i = 0; i < ex.DefaultSets; i++)
                    _data.Reps[ex.ExerciseId][i] = ex.DefaultRepsMin;
            }

            _data.SavedExercises.TryAdd(ex.ExerciseId, true);
        }
    }

    private void RestoreSessionStateFromLocalData()
    {
        if (_pass is null || _data.CompletedSets.Count == 0)
            return;

        _session.StartPass(Args.SplitId, Args.PassId);
        foreach (var ex in _pass.Exercises)
        {
            if (_data.CompletedSets.TryGetValue(ex.ExerciseId, out var storedSets))
            {
                var doneSets = new bool[ex.DefaultSets];
                for (int i = 0; i < doneSets.Length && i < storedSets.Length; i++)
                    doneSets[i] = storedSets[i];

                _session.RestoreSets(ex.ExerciseId, doneSets);
            }
        }
    }

    /// <summary>Första övningen med minst ett oikryssat set, om passet är aktivt i sessionen. Annars 0.</summary>
    private int FirstIncompleteExerciseIndex()
    {
        if (_pass is null || _session.ActiveSplitId != Args.SplitId || _session.ActivePassId != Args.PassId)
            return 0;

        for (int i = 0; i < _pass.Exercises.Count; i++)
        {
            var exerciseId = _pass.Exercises[i].ExerciseId;
            if (!_session.CompletedSets.TryGetValue(exerciseId, out var done))
                return i;
            if (done.Take(_pass.Exercises[i].DefaultSets).Any(s => !s))
                return i;
        }

        return 0;
    }

    private bool IsSetDone(int exerciseId, int setIndex) => _session.IsSetDone(exerciseId, setIndex);

    private bool ExerciseDone(TrainingExerciseTemplate e) =>
        Enumerable.Range(0, e.DefaultSets).All(i => IsSetDone(e.ExerciseId, i));

    private bool AllSetsDone()
    {
        if (_pass is null)
            return false;
        foreach (var ex in _pass.Exercises)
        {
            if (!_session.CompletedSets.TryGetValue(ex.ExerciseId, out var sets) || sets.Any(s => !s))
                return false;
        }
        return true;
    }

    private double GetReps(int exerciseId, int setIndex) =>
        _data.Reps.TryGetValue(exerciseId, out var sets) && setIndex < sets.Length ? sets[setIndex] : 1;

    internal void AdjustReps(PassSet set, int delta)
    {
        if (ActiveExercise is not { } exercise)
            return;

        var value = Math.Clamp(GetReps(exercise.ExerciseId, set.Index) + delta, 1, 1000);
        if (_data.Reps.TryGetValue(exercise.ExerciseId, out var reps) && set.Index < reps.Length)
            reps[set.Index] = value;
        set.Reps = (int)value;
        SaveLocalData(exercise.ExerciseId);
    }

    /// <summary>Kryssar ett set. Startar vilan om den är på, men inte efter sista setet i passet.</summary>
    internal void ToggleSet(PassSet set)
    {
        if (ActiveExercise is not { } exercise)
            return;

        var isDone = _session.ToggleSet(exercise.ExerciseId, exercise.DefaultSets, set.Index);
        if (isDone && _restTimer.IsEnabled && !AllSetsDone())
        {
            IsStatusOpen = false;
            // Vägen tillbaka från vilan ska återuppta passet, inte starta om det.
            _ = _restTimer.StartAsync(Args with { StartNew = false });
        }
        else
            _ = _restTimer.CancelAsync();

        if (_session.CompletedSets.TryGetValue(exercise.ExerciseId, out var doneSets))
            _data.CompletedSets[exercise.ExerciseId] = doneSets.ToArray();

        set.IsDone = isDone;
        SaveLocalData(exercise.ExerciseId);
        Refresh();
    }

    partial void OnWeightChanged(double value)
    {
        if (_showing || ActiveExercise is not { } exercise)
            return;

        // Tomt fält (NaN) sparas som 0, som i webbappen.
        var weight = double.IsNaN(value) ? 0m : (decimal)Math.Clamp(value, 0, 400);
        _weights[exercise.ExerciseId] = weight;
        _data.Weights[exercise.ExerciseId] = weight;
        SaveLocalData(exercise.ExerciseId);
    }

    partial void OnNoteChanged(string value)
    {
        if (_showing || ActiveExercise is not { } exercise)
            return;

        _data.Notes[exercise.ExerciseId] = value ?? string.Empty;
        SaveLocalData(exercise.ExerciseId);
    }

    /// <summary>Ändringen ligger redan i <see cref="_data"/>; spara den lokalt. Sparningarna köas i lagringen och den sista vinner.</summary>
    private void SaveLocalData(int exerciseId)
    {
        _data.IsSynced = false;
        _data.SavedExercises[exerciseId] = false;
        _ = RunAsync(async () =>
        {
            await _storage.SetItemAsync(PassKey, _data);
            await _pendingSync.RefreshAsync();
        }, "Kunde inte spara lokalt");
    }

    private async Task MarkExerciseSavedAsync(int exerciseId)
    {
        _data.SavedExercises[exerciseId] = true;
        _data.IsSynced = _pass?.Exercises.All(ex =>
            _data.SavedExercises.TryGetValue(ex.ExerciseId, out var isSaved) && isSaved) ?? true;

        await _storage.SetItemAsync(PassKey, _data);
        await _pendingSync.RefreshAsync();
    }

    /// <summary>Sparar en övning till API:t (vid övningsbyte). Misslyckas det ligger den kvar osynkad lokalt.</summary>
    private async Task SaveExerciseAsync(int exerciseId)
    {
        if (_pass?.Exercises.Any(e => e.ExerciseId == exerciseId) != true)
            return;

        var exercise = PendingSyncState.CreateExerciseDto(_data, exerciseId);
        var request = new UpsertExerciseRequest
        {
            SessionKey = _data.SessionKey,
            PassId = Args.PassId,
            Date = _data.LocalDate,
            StartedAtUtc = _data.StartedAtUtc!.Value,
            ExerciseId = exerciseId,
            NoteText = exercise.NoteText,
            Weight = exercise.Weight,
            Sets = exercise.Sets,
        };

        SetStatus("Sparar övningen...", InfoBarSeverity.Informational);
        try
        {
            await _passLog.UpsertExerciseAsync(request);
            await MarkExerciseSavedAsync(exerciseId);
            SetStatus("Övningen har sparats.", InfoBarSeverity.Success);
        }
        catch
        {
            SetStatus("Övningen kunde inte sparas.", InfoBarSeverity.Error);
            throw;
        }
    }

    internal async Task NavigateToExerciseAsync(int index)
    {
        if (_pass is null || index < 0 || index >= _pass.Exercises.Count)
            return;
        if (ActiveExercise is { } current && index != _activeIndex)
            await RunAsync(() => SaveExerciseAsync(current.ExerciseId), "Kunde inte spara övningen");
        _activeIndex = index;
        Refresh();
    }

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private Task PreviousAsync() => NavigateToExerciseAsync(_activeIndex - 1);

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private Task NextAsync() => NavigateToExerciseAsync(_activeIndex + 1);

    [RelayCommand]
    private async Task ToggleMusclesAsync()
    {
        ShowMuscles = !ShowMuscles;
        if (!ShowMuscles || MuscleLoad is not null || _pass is null)
            return;

        IsLoadingMuscles = true;
        try
        {
            MuscleLoad = await Catalog.MuscleLoadForAsync(_pass.Exercises.Select(e => e.CatalogExerciseId));
        }
        catch (HttpRequestException)
        {
            MuscleLoad = new Dictionary<string, double>();
        }
        IsLoadingMuscles = false;
    }

    [RelayCommand]
    private Task CancelRestAsync() => _restTimer.CancelAsync();

    [RelayCommand]
    private Task GoBackAsync() => _navigator.NavigateBackAsync(this);

    [RelayCommand]
    private async Task ShowExerciseInfoAsync()
    {
        if (ActiveExercise?.CatalogExerciseId is { } catalogExerciseId)
            await _navigator.NavigateViewModelAsync<ExerciseInfoViewModel>(this, data: new ExerciseInfoArgs(catalogExerciseId));
    }

    /// <summary>
    /// Avslutar passet: sparar allt från lokal data till API:t och tömmer sessionen.
    /// Returnerar false om det inte gick att spara; då ligger passet kvar som pågående.
    /// </summary>
    public async Task<bool> EndPassAsync()
    {
        if (_pass is null)
            return false;

        var saved = false;
        await RunAsync(async () =>
        {
            SetStatus("Sparar passet...", InfoBarSeverity.Informational);
            // Allt som visas ligger redan lokalt, så passet sparas därifrån som vid synk.
            await _pendingSync.FinalizeAsync(PassKey);
            _data.IsSynced = true;
            _data.CompletedAtUtc = DateTimeOffset.UtcNow;
            SetStatus("Passet har sparats.", InfoBarSeverity.Success);
            saved = true;
        }, "Kunde inte spara passet");

        if (!saved)
        {
            SetStatus("Passet kunde inte sparas.", InfoBarSeverity.Error);
            return false;
        }

        await Task.Delay(700);
        _session.EndSession();
        await _restTimer.CancelAsync();
        return true;
    }

    /// <summary>Löst pass: lägg till övningen på servern, ladda om passet och gå till den nya övningen.</summary>
    public async Task AddFreeExerciseAsync(FreeSessionExerciseDto picked)
    {
        if (_pass is null)
            return;

        IsAddingExercise = true;
        await RunAsync(async () =>
        {
            var exerciseId = await _freeSessions.AddExerciseAsync(Args.PassId, picked);
            _pass = await _userSplits.GetPassAsync(Args.SplitId, Args.PassId) ?? _pass;
            InitializeDefaults();
            await _storage.SetItemAsync(PassKey, _data);
            MuscleLoad = null;
            ShowMuscles = false;
            BuildChips();
            var index = _pass.Exercises.FindIndex(e => e.ExerciseId == exerciseId);
            if (index >= 0)
                await NavigateToExerciseAsync(index);
            else
                Refresh();
        }, "Kunde inte lägga till övningen");
        IsAddingExercise = false;
    }

    private void BuildChips()
    {
        Chips.Clear();
        if (_pass is null)
            return;
        for (int i = 0; i < _pass.Exercises.Count; i++)
            Chips.Add(new PassChip(this, i, $"{i + 1}  {_pass.Exercises[i].Exercise.ExerciseName}"));
    }

    /// <summary>Uppdaterar allt som visas utifrån passet, sessionen och lokal data.</summary>
    private void Refresh()
    {
        if (_pass is null)
            return;

        var count = _pass.Exercises.Count;
        var total = _pass.Exercises.Sum(e => e.DefaultSets);
        var done = _pass.Exercises.Sum(e => Enumerable.Range(0, e.DefaultSets).Count(i => IsSetDone(e.ExerciseId, i)));

        ExerciseLabel = $"Övning {_activeIndex + 1} av {count}";
        SetsSummary = $"{done} / {total} set";
        DoneText = $"{done} av {total} set klara";
        ProgressPercent = total == 0 ? 0 : done * 100.0 / total;
        CanGoPrevious = _activeIndex > 0;
        CanGoNext = _activeIndex < count - 1;
        AllDone = AllSetsDone();

        foreach (var chip in Chips)
        {
            chip.IsActive = chip.Index == _activeIndex;
            chip.IsDone = chip.Index < count && ExerciseDone(_pass.Exercises[chip.Index]);
        }

        var exercise = ActiveExercise;
        HasActive = exercise is not null;
        if (exercise is null || exercise.ExerciseId == _shownExerciseId)
            return;

        _shownExerciseId = exercise.ExerciseId;
        ExerciseName = exercise.Exercise.ExerciseName;
        HasCatalogInfo = exercise.CatalogExerciseId is not null;
        SetsReps = FormatSetsReps(exercise);
        _showing = true;
        Weight = (double)(_weights.TryGetValue(exercise.ExerciseId, out var w) ? w : 0);
        Note = _data.Notes.TryGetValue(exercise.ExerciseId, out var n) ? n : string.Empty;
        _showing = false;

        Sets.Clear();
        for (int i = 0; i < exercise.DefaultSets; i++)
            Sets.Add(new PassSet(this, i, (int)GetReps(exercise.ExerciseId, i), IsSetDone(exercise.ExerciseId, i)));

        ShowProgress();
        _ = EnsureProgressLoadedAsync(exercise);
    }

    /// <summary>Hämtar viktutvecklingen en gång per övning; fel ger bara ingen graf.</summary>
    private async Task EnsureProgressLoadedAsync(TrainingExerciseTemplate exercise)
    {
        var id = exercise.ExerciseId;
        if (_progress.ContainsKey(id))
            return;

        _progress[id] = new();
        try
        {
            // Kopplad övning: historik från alla pass och lösa pass. Annars bara den här passövningens.
            _progress[id] = exercise.CatalogExerciseId is { } catalogExerciseId
                ? (await _passLog.GetCatalogHistoryAsync(catalogExerciseId))
                    .Where(entry => entry.MaxWeight > 0)
                    .Select(entry => new ExerciseProgressPointDto(entry.Date, entry.MaxWeight))
                    .ToList()
                : await _passLog.GetProgressAsync(id);
        }
        catch { }

        if (ActiveExercise?.ExerciseId == id)
            ShowProgress();
    }

    private void ShowProgress()
    {
        var points = ActiveExercise is { } exercise && _progress.TryGetValue(exercise.ExerciseId, out var p) ? p : new();
        HasProgress = points.Count >= 2;
        if (!HasProgress)
            return;

        var delta = points[^1].Weight - points[0].Weight;
        ProgressSummary = $"{points[^1].Weight:0.##} kg" + (delta == 0 ? "" : $" ({(delta > 0 ? "+" : "")}{delta:0.##})");

        // Punkter i en 300x80-yta, vikterna skalade mellan min och max (som SparkPoints i webbappen).
        var min = points.Min(x => x.Weight);
        var range = Math.Max(points.Max(x => x.Weight) - min, 1m);
        var collection = new PointCollection();
        for (int i = 0; i < points.Count; i++)
            collection.Add(new Point(8 + i * 284.0 / Math.Max(points.Count - 1, 1), 72 - (double)((points[i].Weight - min) / range) * 64));
        ProgressPoints = collection;
    }

    private static string FormatSetsReps(TrainingExerciseTemplate exercise)
    {
        var reps = exercise.DefaultRepsMin == exercise.DefaultRepsMax
            ? exercise.DefaultRepsMin.ToString()
            : $"{exercise.DefaultRepsMin}-{exercise.DefaultRepsMax}";
        return $"{exercise.DefaultSets}x{reps}";
    }

    private void OnRestTick() => _dispatcher?.TryEnqueue(UpdateRest);

    private void OnRestFinished() => _dispatcher?.TryEnqueue(() => SetStatus("Vilan är slut – dags för nästa set.", InfoBarSeverity.Success));

    private void UpdateRest()
    {
        IsResting = _restTimer.IsResting;
        var remaining = _restTimer.RemainingSeconds;
        RestTime = $"{remaining / 60}:{remaining % 60:00}";
        RestPercent = _restTimer.PercentLeft;
    }

    private void SetStatus(string message, InfoBarSeverity severity)
    {
        StatusSeverity = severity;
        StatusMessage = message;
        IsStatusOpen = true;
    }

    /// <summary>Som TryAsync i webbens PageComponentBase: fel visas som meddelande i stället för att kastas.</summary>
    private async Task RunAsync(Func<Task> action, string errorPrefix)
    {
        ErrorMessage = null;
        try
        {
            await action();
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = $"{errorPrefix}: {ex.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => "Du är inte inloggad.",
                System.Net.HttpStatusCode.Forbidden => "Du har inte behörighet.",
                System.Net.HttpStatusCode.NotFound => "Resursen hittades inte.",
                System.Net.HttpStatusCode.ServiceUnavailable => "Servern är inte tillgänglig.",
                _ => ex.Message
            }}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"{errorPrefix}: {ex.Message}";
        }
    }
}

/// <summary>En övning i raden av övningsknappar.</summary>
public partial class PassChip : ObservableObject
{
    private readonly PassViewModel _owner;

    public PassChip(PassViewModel owner, int index, string label)
    {
        _owner = owner;
        Index = index;
        Label = label;
    }

    public int Index { get; }
    public string Label { get; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsInactive))] private bool _isActive;
    [ObservableProperty] private bool _isDone;

    public bool IsInactive => !IsActive;

    [RelayCommand]
    private void Select() => _ = _owner.NavigateToExerciseAsync(Index);
}

/// <summary>Ett set i aktiv övning.</summary>
public partial class PassSet : ObservableObject
{
    private readonly PassViewModel _owner;

    public PassSet(PassViewModel owner, int index, int reps, bool isDone)
    {
        _owner = owner;
        Index = index;
        _reps = reps;
        _isDone = isDone;
    }

    public int Index { get; }
    public string Label => $"Set {Index + 1}";

    [ObservableProperty] private int _reps;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsNotDone))] private bool _isDone;

    public bool IsNotDone => !IsDone;

    // Synkrona kommandon: snabba tryck ska inte spärras medan föregående sparning pågår.
    [RelayCommand]
    private void Increase() => _owner.AdjustReps(this, 1);

    [RelayCommand]
    private void Decrease() => _owner.AdjustReps(this, -1);

    [RelayCommand]
    private void Toggle() => _owner.ToggleSet(this);
}
