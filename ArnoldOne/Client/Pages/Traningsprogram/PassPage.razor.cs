using ArnoldOne.Client.Services;
using ArnoldOne.Client.Shared;
using ArnoldOne.Client.Models;
using ArnoldOne.Shared.TrainingPrograms;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace ArnoldOne.Client.Pages.Traningsprogram;

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

    private TrainingPassTemplate? pass;
    private Transition transition = Transition.None;
    private int activeIndex;

    private Dictionary<int, decimal> _weights = new();
    private PassData data = new();
    private string PassKey => $"pass-{SplitId}-{PassId}";

    // Används för att detektera PassId-byte vid navigering
    private bool _isInitialized;
    private int _lastPassId = -1;
    private bool _navOpen;

    // Vilotimer
    private CancellationTokenSource? _restTimerCts;
    private bool _restTimerRunning;
    private int _restTimerRemaining;

    /// <summary>
    /// Körs när <see cref="PassId"/> ändras. Hämtar rätt passmall och
    /// laddar om passdata om PassId har bytt sedan senaste inläsning.
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        var passChanged = !_isInitialized || _lastPassId != PassId;
        var isNewSession = NewSession == 1;

        await TryAsync(async () =>
        {
            pass = await UserSplitsService.GetPassAsync(PassId);

            if (pass is not null && passChanged)
            {
                if (isNewSession)
                {
                    data = new PassData();
                    await LoadLastSessionAsync(includeCompletedSets: false);
                }
                else
                {
                    var stored = await LocalStorage.GetItemAsync<PassData>(PassKey);
                    data = stored ?? new PassData();
                    data.NormalizeSyncState();

                    if (stored is null)
                        await LoadLastSessionAsync(includeCompletedSets: true);
                }

                InitializeDefaults();

                if (isNewSession)
                    await LocalStorage.SetItemAsync(PassKey, data);
                else
                    RestoreSessionStateFromLocalData();

                await PendingSync.RefreshAsync();
                PendingSync.SetActivePassKey(PassKey);
                activeIndex = FirstIncompleteExerciseIndex();
                _lastPassId = PassId;
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
            if (NewSession == 1 || !Session.IsActive)
                StartSession();
            else
                activeIndex = FirstIncompleteExerciseIndex();
            StateHasChanged();
        }

        await JSRuntime.InvokeVoidAsync("arnoldOne.attachNoteKeyHandler");
    }

    /// <summary>Läser sparad vikt, reps och anteckningar från senaste sessionen för aktuellt pass.</summary>
    private async Task LoadLastSessionAsync(bool includeCompletedSets)
    {
        var lastSession = await PassLogService.GetLastSessionAsync(PassId);
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

        // Återställ SessionState om sessionen är från idag (t.ex. efter page refresh).
        if (includeCompletedSets && lastSession.Date == DateOnly.FromDateTime(DateTime.Today))
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
    private void StartSession()
    {
        Session.StartPass(SplitId, PassId);
        CancelRestTimer();
        _ = JSRuntime.InvokeVoidAsync("arnoldOne.requestWakeLock");
    }

    /// <summary>Avslutar passet: sparar till DB, tömmer sessionen, stoppar timer, frigör wake lock och navigerar till startsidan.</summary>
    private async Task EndPass()
    {
        if (pass is not null)
        {
            var request = new ArnoldOne.Shared.DTOs.SaveSessionRequest
            {
                PassId = PassId,
                Date   = DateOnly.FromDateTime(DateTime.Today),
                Exercises = pass.Exercises.Select(ex => new ArnoldOne.Shared.DTOs.SaveSessionExerciseDto
                {
                    ExerciseId = ex.ExerciseId,
                    NoteText   = GetNote(ex.ExerciseId),
                    Weight     = GetWeight(ex.ExerciseId) ?? 0,
                    Sets       = Enumerable.Range(0, ex.DefaultSets).Select(i => new ArnoldOne.Shared.DTOs.SaveSessionSetDto
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
                await PassLogService.SaveSessionAsync(request);
                await MarkSessionSavedAsync();
                saved = true;
            }, "Kunde inte spara passet");

            if (!saved) return;
        }

        Session.EndSession();
        CancelRestTimer();
        _ = JSRuntime.InvokeVoidAsync("arnoldOne.releaseWakeLock");
        Nav.NavigateTo("/");
    }

    /// <summary>Startar vilotimern med nedräkning. Avbryter eventuell pågående timer först.</summary>
    private void StartRestTimer()
    {
        CancelRestTimer();
        _restTimerCts = new CancellationTokenSource();
        var token = _restTimerCts.Token;
        _restTimerRemaining = RestTimer.Seconds;
        _restTimerRunning = true;

        _ = Task.Run(async () =>
        {
            try
            {
                while (_restTimerRemaining > 0)
                {
                    await Task.Delay(1000, token);
                    _restTimerRemaining--;
                    await InvokeAsync(StateHasChanged);
                }

                _restTimerRunning = false;
                await InvokeAsync(StateHasChanged);
                await JSRuntime.InvokeVoidAsync("arnoldOne.flashScreen", token);
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    /// <summary>Avbryter pågående vilotimer.</summary>
    private void CancelRestTimer()
    {
        _restTimerCts?.Cancel();
        _restTimerCts?.Dispose();
        _restTimerCts = null;
        _restTimerRunning = false;
    }

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

        var request = new ArnoldOne.Shared.DTOs.UpsertExerciseRequest
        {
            PassId     = PassId,
            Date       = DateOnly.FromDateTime(DateTime.Today),
            ExerciseId = exerciseId,
            NoteText   = GetNote(exerciseId),
            Weight     = GetWeight(exerciseId) ?? 0,
            Sets       = Enumerable.Range(0, ex.DefaultSets).Select(i => new ArnoldOne.Shared.DTOs.SaveSessionSetDto
            {
                SetIndex = i,
                Reps     = (int)GetReps(exerciseId, i),
                IsDone   = IsSetDone(exerciseId, i),
                Weight   = GetWeight(exerciseId) ?? 0,
            }).ToList(),
        };

        await PassLogService.UpsertExerciseAsync(request);
        await MarkExerciseSavedAsync(exerciseId);
    }

    /// <summary>Formaterar set och reps från en övningsmall som text, t.ex. "3x8-12".</summary>
    private static string FormatSetsReps(TrainingExerciseTemplate exercise)
    {
        var reps = exercise.DefaultRepsMin == exercise.DefaultRepsMax
            ? exercise.DefaultRepsMin.ToString()
            : $"{exercise.DefaultRepsMin}-{exercise.DefaultRepsMax}";

        return $"{exercise.DefaultSets}x{reps}";
    }

    private string NavClass => _navOpen ? "pass-nav pass-nav--open" : "pass-nav";

    private string StepClass(int idx) => activeIndex == idx ? "pass-step pass-step--active" : "pass-step";

    private void ToggleNav() => _navOpen = !_navOpen;

    private async Task OnCarouselIndexChanged(int idx)
    {
        if (pass is not null && activeIndex >= 0 && activeIndex < pass.Exercises.Count && idx != activeIndex)
            await TryAsync(() => SaveCurrentExerciseAsync(pass.Exercises[activeIndex].ExerciseId), "Kunde inte spara övningen");
        activeIndex = idx;
    }

    private async Task NavigateToExercise(int idx)
    {
        if (pass is not null && activeIndex >= 0 && activeIndex < pass.Exercises.Count && idx != activeIndex)
            await TryAsync(() => SaveCurrentExerciseAsync(pass.Exercises[activeIndex].ExerciseId), "Kunde inte spara övningen");
        activeIndex = idx;
        _navOpen = false;
    }

    /// <summary>
    /// Användarens sparade data per pass: reps och anteckningar per övning.
    /// Serialiseras till och från LocalStorage.
    /// </summary>
    /// <summary>Avbryter timern och frigör wake lock när komponenten avmonteras.</summary>
    public void Dispose()
    {
        CancelRestTimer();
        PendingSync.ClearActivePassKey(PassKey);
        _ = JSRuntime.InvokeVoidAsync("arnoldOne.releaseWakeLock");
    }
}
