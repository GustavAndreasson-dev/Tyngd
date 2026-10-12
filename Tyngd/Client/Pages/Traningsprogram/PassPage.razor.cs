using Tyngd.Client.Services;
using Tyngd.Client.Shared;
using Tyngd.Client.Models;
using Tyngd.Shared.TrainingPrograms;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Tyngd.Client.Pages.Traningsprogram;

/// <summary>
/// Delad sida för ett enskilt träningspass. Används av både PPL1 (PassId 101–104) och PPL2 (PassId 1–4).
/// Hämtar passmall via <see cref="TrainingSplitTemplateGenerator.FindPass"/> och sparar pågående data i LocalStorage.
/// LocalStorage-datan synkas till API när användaren lämnar en övning eller avslutar passet.
/// </summary>
public partial class PassPage : IDisposable
{
    /// <summary>Id för den valda splitten, hämtas från route-parametern.</summary>
    [Parameter]
    public int SplitId { get; set; }

    /// <summary>Id för det valda passet, hämtas från route-parametern.</summary>
    [Parameter]
    public int PassId { get; set; }

    /// <summary>Sätts till 1 när användaren navigerar från startsidan, vilket startar en ny session.</summary>
    [SupplyParameterFromQuery(Name = "new")]
    public int NewSession { get; set; }

    /// <summary>Tjänst för att läsa och skriva pågående passdata i webbläsarens LocalStorage.</summary>
    [Inject]
    private ILocalStorageService LocalStorage { get; set; } = default!;

    /// <summary>Används för att koppla on keydown-lyssnaren på anteckningsfältet.</summary>
    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>Tjänst som håller vilotimer-inställningarna globalt.</summary>
    [Inject]
    private RestTimerState RestTimer { get; set; } = default!;

    /// <summary>Tjänst som håller pågående pass-session globalt.</summary>
    [Inject]
    private SessionState Session { get; set; } = default!;

    /// <summary>Används för att navigera tillbaka till startsidan efter avslutat pass.</summary>
    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    [Inject]
    private UserSplitsService UserSplitsService { get; set; } = default!;

    [Inject]
    private PassLogService PassLogService { get; set; } = default!;

    [Inject]
    private PendingSyncState PendingSync { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private CatalogService CatalogService { get; set; } = default!;

    [Inject]
    private FreeSessionService FreeSessionService { get; set; } = default!;

    private bool addingExercise;

    /// <summary>Löst pass: välj en övning, lägg till den på servern och ladda om passet med den nya övningen aktiv.</summary>
    private async Task AddFreeExercise()
    {
        if (pass is null)
            return;

        var taken = pass.Exercises.Select(e => e.CatalogExerciseId).OfType<int>().ToHashSet();
        var dialog = await DialogService.ShowAsync<AddFreeExerciseDialog>(null,
            new DialogParameters<AddFreeExerciseDialog> { { d => d.Taken, taken } },
            new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true });
        if (await dialog.Result is not { Canceled: false, Data: Tyngd.Shared.DTOs.FreeSessionExerciseDto picked })
            return;

        addingExercise = true;
        await TryAsync(async () =>
        {
            var exerciseId = await FreeSessionService.AddExerciseAsync(PassId, picked);
            pass = await UserSplitsService.GetPassAsync(SplitId, PassId) ?? pass;
            InitializeDefaults();
            await LocalStorage.SetItemAsync(PassKey, data);
            passMuscleLoad = null;
            showMuscles = false;
            var index = pass.Exercises.FindIndex(e => e.ExerciseId == exerciseId);
            if (index >= 0)
                await NavigateToExercise(index);
        }, "Kunde inte lägga till övningen");
        addingExercise = false;
    }

    private bool showMuscles;
    private Dictionary<string, double>? passMuscleLoad;

    private async Task ToggleMuscles()
    {
        showMuscles = !showMuscles;
        if (!showMuscles || passMuscleLoad is not null || pass is null)
            return;

        try
        {
            passMuscleLoad = await CatalogService.MuscleLoadForAsync(pass.Exercises.Select(exercise => exercise.CatalogExerciseId));
        }
        catch (HttpRequestException)
        {
            passMuscleLoad = new();
        }
    }

    private Task ShowExerciseInfo(int catalogExerciseId) =>
        DialogService.ShowAsync<ExerciseInfoDialog>(null,
            new DialogParameters<ExerciseInfoDialog> { { dialog => dialog.CatalogExerciseId, catalogExerciseId } },
            new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true });

    private TrainingPassTemplate? pass;
    private int activeIndex;
    private int _handlerIndex = -1;
    private readonly Dictionary<int, List<Tyngd.Shared.DTOs.ExerciseProgressPointDto>> _progress = new();

    private Dictionary<int, decimal> _weights = new();
    private PassData data = new();
    private string PassKey => $"pass-{SplitId}-{PassId}";

    // Används för att detektera PassId-byte vid navigering
    private bool _isInitialized;
    private int _lastPassId = -1;
    private int _lastSplitId = -1;
    private bool _isNewPassData;
    private string? saveStatusMessage;
    private Severity saveStatusSeverity = Severity.Info;

    // Vilotimer

    /// <summary>
    /// Körs när <see cref="PassId"/> ändras. Hämtar rätt passmall och
    /// laddar om passdata om PassId har bytt sedan senaste inläsning.
    /// </summary>
    protected override void OnInitialized() => RestTimer.OnTick += OnRestTick;

    protected override async Task OnParametersSetAsync()
    {
        var passChanged = !_isInitialized || _lastPassId != PassId || _lastSplitId != SplitId;
        var isNewSession = NewSession == 1;

        await TryAsync(async () =>
        {
            // Senaste sessionen behövs nästan alltid när ett pass öppnas; hämta den samtidigt som passet i stället för efteråt.
            var lastSessionTask = passChanged ? PassLogService.GetLastSessionAsync(PassId) : null;
            pass = await UserSplitsService.GetPassAsync(SplitId, PassId);

            if (pass is not null && lastSessionTask is not null)
            {
                await InitializePassDataAsync(isNewSession, lastSessionTask);
                InitializeDefaults();
                await LocalStorage.SetItemAsync(PassKey, data);
                RestoreSessionStateFromLocalData();
                if (_isNewPassData)
                    Session.StartNewPass(SplitId, PassId);
                else if (Session.ActiveSplitId != SplitId || Session.ActivePassId != PassId)
                    Session.StartPass(SplitId, PassId);

                await PendingSync.RefreshAsync();
                PendingSync.SetActivePassKey(PassKey);
                activeIndex = FirstIncompleteExerciseIndex();
                _lastPassId = PassId;
                _lastSplitId = SplitId;
                _isInitialized = true;
            }
        }, "Kunde inte ladda passet");
    }

    /// <summary>
    /// Startar eller återställer sessionen efter första renderingen.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _isInitialized = true;
            _lastPassId = PassId;
            _lastSplitId = SplitId;
            if (_isNewPassData)
                StartSession(reset: true);
            else if (!Session.IsActive || Session.ActiveSplitId != SplitId || Session.ActivePassId != PassId)
                StartSession();
            else
                activeIndex = FirstIncompleteExerciseIndex();
            StateHasChanged();
        }

        // Vilotimern renderar om varje sekund; koppla bara när övningen byts.
        if (_handlerIndex != activeIndex)
        {
            _handlerIndex = activeIndex;
            await JSRuntime.InvokeVoidAsync("tyngd.attachNoteKeyHandler");
            await JSRuntime.InvokeVoidAsync("tyngd.scrollActiveChip");
        }
        await EnsureProgressLoadedAsync();
    }

    /// <summary>Hämtar viktutvecklingen för aktiv övning en gång per övning; fel ger bara ingen graf.</summary>
    private async Task EnsureProgressLoadedAsync()
    {
        if (pass is null || activeIndex < 0 || activeIndex >= pass.Exercises.Count)
            return;

        var exercise = pass.Exercises[activeIndex];
        var id = exercise.ExerciseId;
        if (_progress.ContainsKey(id))
            return;

        _progress[id] = new();
        try
        {
            // Kopplad övning: historik från alla pass och lösa pass. Annars bara den här passövningens.
            _progress[id] = exercise.CatalogExerciseId is { } catalogExerciseId
                ? (await PassLogService.GetCatalogHistoryAsync(catalogExerciseId))
                    .Where(entry => entry.MaxWeight > 0)
                    .Select(entry => new Tyngd.Shared.DTOs.ExerciseProgressPointDto(entry.Date, entry.MaxWeight))
                    .ToList()
                : await PassLogService.GetProgressAsync(id);
        }
        catch { }
        StateHasChanged();
    }

    private List<Tyngd.Shared.DTOs.ExerciseProgressPointDto> ActiveProgress =>
        pass is not null && activeIndex >= 0 && activeIndex < pass.Exercises.Count
        && _progress.TryGetValue(pass.Exercises[activeIndex].ExerciseId, out var p) ? p : new();

    /// <summary>Punkter för en 300x80 SVG-polyline; vikter skalas mellan min och max.</summary>
    private static string SparkPoints(List<Tyngd.Shared.DTOs.ExerciseProgressPointDto> pts)
    {
        var min = pts.Min(p => p.Weight);
        var range = Math.Max(pts.Max(p => p.Weight) - min, 1m);
        return string.Join(" ", pts.Select((p, i) => FormattableString.Invariant(
            $"{8 + i * 284.0 / Math.Max(pts.Count - 1, 1):0.#},{72 - (double)((p.Weight - min) / range) * 64:0.#}")));
    }


    private int TotalSets => pass?.Exercises.Sum(e => e.DefaultSets) ?? 0;
    private int DoneSets => pass is null ? 0 : pass.Exercises.Sum(e => Enumerable.Range(0, e.DefaultSets).Count(i => IsSetDone(e.ExerciseId, i)));
    private bool ExerciseDone(int idx) => pass is not null && Enumerable.Range(0, pass.Exercises[idx].DefaultSets).All(i => IsSetDone(pass.Exercises[idx].ExerciseId, i));

    private async Task InitializePassDataAsync(bool forceNewSession, Task<Tyngd.Shared.DTOs.LastSessionDto?> lastSessionTask)
    {
        var now = DateTimeOffset.UtcNow;
        var stored = await LocalStorage.GetItemAsync<PassData>(PassKey);
        if (stored is not null)
        {
            stored.NormalizeSyncState();
            stored.NormalizeSessionIdentity(now);
            data = stored;

            if (!forceNewSession && PassResumePolicy.ShouldResume(stored, now))
            {
                _isNewPassData = false;
                return;
            }

            if (stored.CompletedAtUtc is null)
                await PendingSync.FinalizeAsync(PassKey);

            data = CreateNewPassData(now);
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
                data = serverData;
                _isNewPassData = false;
                ApplyLastSession(lastSession, includeCompletedSets: true);
                return;
            }

            await PassLogService.CompleteSessionAsync(lastSession.SessionKey.Value);
        }

        data = CreateNewPassData(now);
        _isNewPassData = true;
        ApplyLastSession(lastSession, includeCompletedSets: false);
    }

    private void ApplyLastSession(Tyngd.Shared.DTOs.LastSessionDto? lastSession, bool includeCompletedSets)
    {
        if (lastSession is null || pass is null)
            return;

        foreach (var ex in lastSession.Exercises)
        {
            var template = pass.Exercises.FirstOrDefault(e => e.ExerciseId == ex.ExerciseId);
            if (template is null)
                continue;

            _weights[ex.ExerciseId] = ex.Weight;
            data.Weights[ex.ExerciseId] = ex.Weight;

            if (ex.Sets.Count > 0)
            {
                var reps = new double[template.DefaultSets];
                for (int i = 0; i < reps.Length; i++)
                    reps[i] = template.DefaultRepsMin;

                foreach (var set in ex.Sets)
                    if (set.SetIndex >= 0 && set.SetIndex < reps.Length)
                        reps[set.SetIndex] = set.Reps;

                data.Reps[ex.ExerciseId] = reps;
            }

            if (!string.IsNullOrEmpty(ex.NoteText))
                data.Notes[ex.ExerciseId] = ex.NoteText;

            data.SavedExercises[ex.ExerciseId] = true;
        }

        if (includeCompletedSets)
        {
            Session.StartPass(SplitId, PassId);
            foreach (var ex in lastSession.Exercises)
            {
                var template = pass.Exercises.FirstOrDefault(e => e.ExerciseId == ex.ExerciseId);
                if (template is null)
                    continue;

                var doneSets = new bool[template.DefaultSets];
                foreach (var set in ex.Sets)
                    if (set.SetIndex >= 0 && set.SetIndex < doneSets.Length)
                        doneSets[set.SetIndex] = true;

                data.CompletedSets[ex.ExerciseId] = doneSets;
                Session.RestoreSets(ex.ExerciseId, doneSets);
            }
        }

        data.IsSynced = true;
    }

    private static PassData CreateNewPassData(DateTimeOffset now) => new()
    {
        SessionKey = Guid.NewGuid(),
        StartedAtUtc = now.ToUniversalTime(),
        IsSynced = true,
    };

    private static PassData CreatePassDataFromSession(Tyngd.Shared.DTOs.LastSessionDto session) => new()
    {
        SessionKey = session.SessionKey ?? Guid.NewGuid(),
        StartedAtUtc = session.StartedAtUtc ?? DateTimeOffset.UtcNow,
        CompletedAtUtc = session.CompletedAtUtc,
        IsSynced = true,
    };

    /// <summary>Fyller i defaultvärden för vikt och reps för övningar som saknar sparad data.</summary>
    private void InitializeDefaults()
    {
        foreach (var ex in pass!.Exercises)
        {
            if (data.Weights.TryGetValue(ex.ExerciseId, out var storedWeight))
                _weights[ex.ExerciseId] = storedWeight;
            else if (!_weights.ContainsKey(ex.ExerciseId))
            {
                _weights[ex.ExerciseId] = ex.DefaultWeight;
                data.Weights[ex.ExerciseId] = ex.DefaultWeight;
            }

            if (!data.Reps.ContainsKey(ex.ExerciseId))
            {
                data.Reps[ex.ExerciseId] = new double[ex.DefaultSets];
                for (int i = 0; i < ex.DefaultSets; i++)
                    data.Reps[ex.ExerciseId][i] = (double)ex.DefaultRepsMin;
            }

            data.SavedExercises.TryAdd(ex.ExerciseId, true);
        }
    }

    private void RestoreSessionStateFromLocalData()
    {
        if (pass is null || data.CompletedSets.Count == 0)
            return;

        Session.StartPass(SplitId, PassId);
        foreach (var ex in pass.Exercises)
        {
            if (data.CompletedSets.TryGetValue(ex.ExerciseId, out var storedSets))
            {
                var doneSets = new bool[ex.DefaultSets];
                for (int i = 0; i < doneSets.Length && i < storedSets.Length; i++)
                    doneSets[i] = storedSets[i];

                Session.RestoreSets(ex.ExerciseId, doneSets);
            }
        }
    }

    /// <summary>
    /// Returnerar index för den första övningen som har minst ett oikryssat set,
    /// förutsatt att passet är aktivt i sessionen. Annars returneras 0.
    /// </summary>
    private int FirstIncompleteExerciseIndex()
    {
        if (pass is null || Session.ActiveSplitId != SplitId || Session.ActivePassId != PassId)
            return 0;

        for (int i = 0; i < pass.Exercises.Count; i++)
        {
            var exerciseId = pass.Exercises[i].ExerciseId;
            var sets = pass.Exercises[i].DefaultSets;

            if (!Session.CompletedSets.TryGetValue(exerciseId, out var done))
                return i;

            if (done.Take(sets).Any(s => !s))
                return i;
        }

        return 0;
    }

    /// <summary>Returnerar sparad vikt för en övning, eller null om ingen finns.</summary>
    private decimal? GetWeight(int exerciseId) =>
        _weights.TryGetValue(exerciseId, out var w) ? w : null;

    /// <summary>Sparar vikten för en övning. Nollvärde lagras som 0.</summary>
    private async Task SetWeight(int exerciseId, decimal? value)
    {
        _weights[exerciseId] = value ?? 0;
        data.Weights[exerciseId] = value ?? 0;
        await SaveLocalDataAsync(exerciseId);
    }

    /// <summary>Returnerar antalet reps för ett specifikt set, eller 1 om index saknas.</summary>
    private double GetReps(int exerciseId, int setIndex)
    {
        if (data.Reps.TryGetValue(exerciseId, out var sets) && setIndex < sets.Length)
            return sets[setIndex];
        return 1;
    }

    /// <summary>Justerar reps för ett specifikt set med ett steg upp eller ned (min 1, max 20).</summary>
    private async Task AdjustReps(int exerciseId, int setIndex, int delta)
    {
        var current = GetReps(exerciseId, setIndex);
        SetReps(exerciseId, setIndex, Math.Clamp(current + delta, 1, 1000));
        await SaveLocalDataAsync(exerciseId);
    }

    /// <summary>Uppdaterar antalet reps för ett specifikt set.</summary>
    private void SetReps(int exerciseId, int setIndex, double value)
    {
        if (data.Reps.TryGetValue(exerciseId, out var sets) && setIndex < sets.Length)
            sets[setIndex] = value;
    }

    /// <summary>Returnerar om ett specifikt set är markerat som klart.</summary>
    private bool IsSetDone(int exerciseId, int setIndex) => Session.IsSetDone(exerciseId, setIndex);

    /// <summary>Togglar om ett set är klart. Startar vilotimer om aktiverad, men inte efter sista setet i passet.</summary>
    private async Task ToggleSet(int exerciseId, int setIndex)
    {
        var count = pass!.Exercises.First(e => e.ExerciseId == exerciseId).DefaultSets;
        var isDone = Session.ToggleSet(exerciseId, count, setIndex);

        if (isDone && RestTimer.IsEnabled && !AllSetsDone())
            StartRestTimer();
        else
            CancelRestTimer();

        if (Session.CompletedSets.TryGetValue(exerciseId, out var doneSets))
            data.CompletedSets[exerciseId] = doneSets.ToArray();

        await SaveLocalDataAsync(exerciseId);
    }

    /// <summary>Returnerar sant om alla sets för alla övningar är markerade som klara.</summary>
    private bool AllSetsDone()
    {
        if (pass is null) return false;
        foreach (var ex in pass.Exercises)
        {
            if (!Session.CompletedSets.TryGetValue(ex.ExerciseId, out var sets))
                return false;
            if (sets.Any(s => !s))
                return false;
        }
        return true;
    }

    /// <summary>Startar ett nytt pass via SessionState och aktiverar wake lock.</summary>
    private void StartSession(bool reset = false)
    {
        // En vila från samma pass får fortsätta när man kommer tillbaka; bara ett nytt pass nollställer den.
        if (reset)
        {
            Session.StartNewPass(SplitId, PassId);
            CancelRestTimer();
        }
        else
            Session.StartPass(SplitId, PassId);
        _ = JSRuntime.InvokeVoidAsync("tyngd.requestWakeLock");
    }

    /// <summary>Avslutar passet: sparar till DB, tömmer sessionen, stoppar timer, frigör wake lock och navigerar till startsidan.</summary>
    private async Task EndPass()
    {
        if (pass is not null)
        {
            var request = new Tyngd.Shared.DTOs.SaveSessionRequest
            {
                SessionKey = data.SessionKey,
                PassId = PassId,
                Date   = DateOnly.FromDateTime(data.StartedAtUtc!.Value.ToLocalTime().DateTime),
                StartedAtUtc = data.StartedAtUtc.Value,
                CompleteSession = true,
                Exercises = pass.Exercises.Select(ex => new Tyngd.Shared.DTOs.SaveSessionExerciseDto
                {
                    ExerciseId = ex.ExerciseId,
                    NoteText   = GetNote(ex.ExerciseId),
                    Weight     = GetWeight(ex.ExerciseId) ?? 0,
                    Sets       = Enumerable.Range(0, ex.DefaultSets).Select(i => new Tyngd.Shared.DTOs.SaveSessionSetDto
                    {
                        SetIndex = i,
                        Reps     = (int)GetReps(ex.ExerciseId, i),
                        IsDone   = IsSetDone(ex.ExerciseId, i),
                        Weight   = GetWeight(ex.ExerciseId) ?? 0,
                    }).ToList(),
                }).ToList(),
            };

            var saved = false;
            await TryAsync(async () =>
            {
                SetSaveStatus("Sparar passet...", Severity.Info);
                await PassLogService.SaveSessionAsync(request);
                data.CompletedAtUtc = DateTimeOffset.UtcNow;
                await MarkSessionSavedAsync();
                SetSaveStatus("Passet har sparats.", Severity.Success);
                saved = true;
            }, "Kunde inte spara passet");

            if (!saved)
            {
                SetSaveStatus("Passet kunde inte sparas.", Severity.Error);
                return;
            }

            await Task.Delay(700);
        }

        Session.EndSession();
        CancelRestTimer();
        _ = JSRuntime.InvokeVoidAsync("tyngd.releaseWakeLock");

        if (pass?.IsFreeSession == true)
        {
            var dialog = await DialogService.ShowAsync<FinishFreeSessionDialog>(null,
                new DialogParameters<FinishFreeSessionDialog> { { d => d.PassId, PassId }, { d => d.PassName, pass.PassName } },
                new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, BackdropClick = false, CloseOnEscapeKey = false });
            if (await dialog.Result is { Canceled: false, Data: Tyngd.Shared.DTOs.StartFreeSessionResponse })
            {
                Nav.NavigateTo("/TP/my-splits");
                return;
            }
        }

        Nav.NavigateTo("/");
    }

    /// <summary>Startar vilan. Den fortsätter även om man lämnar passidan (se RestTimerState).</summary>
    private void StartRestTimer() => _ = RestTimer.StartAsync($"TP/split/{SplitId}/pass/{PassId}");

    /// <summary>Avbryter pågående vila.</summary>
    private void CancelRestTimer() => _ = RestTimer.CancelAsync();

    private void OnRestTick() => InvokeAsync(StateHasChanged);

    /// <summary>Tom – vikter sparas nu till DB vid övningsbyte och avslutat pass.</summary>
    private Task OnWeightChanged(FocusEventArgs _) => Task.CompletedTask;

    /// <summary>Returnerar anteckningen för en övning, eller tom sträng om ingen finns.</summary>
    private string GetNote(int exerciseId) =>
        data.Notes.TryGetValue(exerciseId, out var n) ? n : string.Empty;

    /// <summary>Uppdaterar anteckningen för en övning.</summary>
    private async Task SetNote(int exerciseId, string value)
    {
        data.Notes[exerciseId] = value;
        await SaveLocalDataAsync(exerciseId);
    }

    /// <summary>Tom – anteckningar sparas nu till DB vid övningsbyte och avslutat pass.</summary>
    private Task OnNoteChanged(FocusEventArgs _) => Task.CompletedTask;

    private async Task SaveLocalDataAsync(int exerciseId)
    {
        data.IsSynced = false;
        data.SavedExercises[exerciseId] = false;
        await LocalStorage.SetItemAsync(PassKey, data);
        await PendingSync.RefreshAsync();
    }

    private async Task MarkExerciseSavedAsync(int exerciseId)
    {
        data.SavedExercises[exerciseId] = true;
        data.IsSynced = pass?.Exercises.All(ex =>
            data.SavedExercises.TryGetValue(ex.ExerciseId, out var isSaved) && isSaved) ?? true;

        await LocalStorage.SetItemAsync(PassKey, data);
        await PendingSync.RefreshAsync();
    }

    private async Task MarkSessionSavedAsync()
    {
        if (pass is not null)
        {
            foreach (var ex in pass.Exercises)
                data.SavedExercises[ex.ExerciseId] = true;
        }

        data.IsSynced = true;
        await LocalStorage.SetItemAsync(PassKey, data);
        await PendingSync.RefreshAsync();
    }

    /// <summary>Sparar en enskild övnings data till DB (anropas vid övningsbyte och vid avslutat pass).</summary>
    private async Task SaveCurrentExerciseAsync(int exerciseId)
    {
        if (pass is null) return;
        var ex = pass.Exercises.FirstOrDefault(e => e.ExerciseId == exerciseId);
        if (ex is null) return;

        var request = new Tyngd.Shared.DTOs.UpsertExerciseRequest
        {
            SessionKey = data.SessionKey,
            PassId     = PassId,
            Date       = DateOnly.FromDateTime(data.StartedAtUtc!.Value.ToLocalTime().DateTime),
            StartedAtUtc = data.StartedAtUtc.Value,
            ExerciseId = exerciseId,
            NoteText   = GetNote(exerciseId),
            Weight     = GetWeight(exerciseId) ?? 0,
            Sets       = Enumerable.Range(0, ex.DefaultSets).Select(i => new Tyngd.Shared.DTOs.SaveSessionSetDto
            {
                SetIndex = i,
                Reps     = (int)GetReps(exerciseId, i),
                IsDone   = IsSetDone(exerciseId, i),
                Weight   = GetWeight(exerciseId) ?? 0,
            }).ToList(),
        };

        SetSaveStatus("Sparar övningen...", Severity.Info);

        try
        {
            await PassLogService.UpsertExerciseAsync(request);
            await MarkExerciseSavedAsync(exerciseId);
            SetSaveStatus("Övningen har sparats.", Severity.Success);
        }
        catch
        {
            SetSaveStatus("Övningen kunde inte sparas.", Severity.Error);
            throw;
        }
    }

    /// <summary>Formaterar set och reps från en övningsmall som text, t.ex. "3x8-12".</summary>
    private static string FormatSetsReps(TrainingExerciseTemplate exercise)
    {
        var reps = exercise.DefaultRepsMin == exercise.DefaultRepsMax
            ? exercise.DefaultRepsMin.ToString()
            : $"{exercise.DefaultRepsMin}-{exercise.DefaultRepsMax}";

        return $"{exercise.DefaultSets}x{reps}";
    }

    private string ChipClass(int idx) =>
        "pass-chip" + (activeIndex == idx ? " pass-chip--active" : "") + (ExerciseDone(idx) ? " pass-chip--done" : "");

    private async Task NavigateToExercise(int idx)
    {
        if (pass is not null && activeIndex >= 0 && activeIndex < pass.Exercises.Count && idx != activeIndex)
            await TryAsync(() => SaveCurrentExerciseAsync(pass.Exercises[activeIndex].ExerciseId), "Kunde inte spara övningen");
        activeIndex = idx;
    }

    private void SetSaveStatus(string message, Severity severity)
    {
        saveStatusMessage = message;
        saveStatusSeverity = severity;
        StateHasChanged();
    }

    private void ClearSaveStatus() => saveStatusMessage = null;

    /// <summary>
    /// Användarens sparade data per pass: reps och anteckningar per övning.
    /// Serialiseras till och från LocalStorage.
    /// </summary>
    /// <summary>Avbryter timern och frigör wake lock när komponenten avmonteras.</summary>
    public void Dispose()
    {
        RestTimer.OnTick -= OnRestTick;
        PendingSync.ClearActivePassKey(PassKey);
        _ = JSRuntime.InvokeVoidAsync("tyngd.releaseWakeLock");
    }
}
