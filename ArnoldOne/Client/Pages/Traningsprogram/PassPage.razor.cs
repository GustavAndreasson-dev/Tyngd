using ArnoldOne.Client.Services;
using ArnoldOne.Client.Shared;
using ArnoldOne.Shared.TrainingPrograms;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace ArnoldOne.Client.Pages.Traningsprogram;

/// <summary>
/// Delad sida för ett enskilt träningspass. Används av både PPL1 (PassId 101–104) och PPL2 (PassId 1–4).
/// Hämtar passmall via <see cref="TrainingSplitTemplateGenerator.FindPass"/> och sparar användarens data i LocalStorage.
/// Vikter lagras globalt per ExerciseId i <c>exercise-weights</c> för synkronisering mellan program.
/// Reps och anteckningar lagras per pass i <c>pass-{PassId}</c>.
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

    /// <summary>Tjänst för att läsa och skriva användardata i webbläsarens LocalStorage.</summary>
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
    private SplitTemplateService SplitTemplateService { get; set; } = default!;

    private TrainingPassTemplate? pass;
    private Transition transition = Transition.None;
    private int activeIndex;

    // Vikter delas globalt mellan program via "exercise-weights"
    private Dictionary<int, decimal> _weights = new();
    private const string WeightsKey = "exercise-weights";

    // Reps och anteckningar sparas per pass via "pass-{SplitId}-{PassId}"
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
        await TryAsync(async () =>
        {
            pass = await SplitTemplateService.GetPassAsync(SplitId, PassId);
        }, "Kunde inte ladda passet");

        if (pass is not null)
        {
            if (_isInitialized && _lastPassId != PassId)
            {
                _lastPassId = PassId;
                activeIndex = FirstIncompleteExerciseIndex();
                var stored = await LocalStorage.GetItemAsync<PassData>(PassKey);
                data = stored ?? new PassData();
            }

            InitializeDefaults();
        }
    }

    /// <summary>
    /// Läser sparad data från LocalStorage vid första renderingen.
    /// Vikter och passdata laddas separat.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var storedWeights = await LocalStorage.GetItemAsync<Dictionary<int, decimal>>(WeightsKey);
            if (storedWeights is not null)
                _weights = storedWeights;

            var storedData = await LocalStorage.GetItemAsync<PassData>(PassKey);
            if (storedData is not null)
                data = storedData;

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

    /// <summary>Fyller i defaultvärden för vikt och reps för övningar som saknar sparad data.</summary>
    private void InitializeDefaults()
    {
        foreach (var ex in pass!.Exercises)
        {
            if (!_weights.ContainsKey(ex.ExerciseId))
                _weights[ex.ExerciseId] = ex.DefaultWeight;

            if (!data.Reps.ContainsKey(ex.ExerciseId))
            {
                data.Reps[ex.ExerciseId] = new double[ex.DefaultSets];
                for (int i = 0; i < ex.DefaultSets; i++)
                    data.Reps[ex.ExerciseId][i] = (double)ex.DefaultRepsMin;
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
    private void SetWeight(int exerciseId, decimal? value) =>
        _weights[exerciseId] = value ?? 0;

    /// <summary>Returnerar antalet reps för ett specifikt set, eller 1 om index saknas.</summary>
    private double GetReps(int exerciseId, int setIndex)
    {
        if (data.Reps.TryGetValue(exerciseId, out var sets) && setIndex < sets.Length)
            return sets[setIndex];
        return 1;
    }

    /// <summary>Justerar reps för ett specifikt set med ett steg upp eller ned (min 1, max 20).</summary>
    private void AdjustReps(int exerciseId, int setIndex, int delta)
    {
        var current = GetReps(exerciseId, setIndex);
        SetReps(exerciseId, setIndex, Math.Clamp(current + delta, 1, 1000));
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

    /// <summary>Avslutar passet: tömmer sessionen, stoppar timer, frigör wake lock och navigerar till startsidan.</summary>
    private void EndPass()
    {
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

    /// <summary>Sparar vikter till LocalStorage när användaren lämnar ett viktfält.</summary>
    private async Task OnWeightChanged(FocusEventArgs _)
    {
        await LocalStorage.SetItemAsync(WeightsKey, _weights);
    }

    /// <summary>Returnerar anteckningen för en övning, eller tom sträng om ingen finns.</summary>
    private string GetNote(int exerciseId) =>
        data.Notes.TryGetValue(exerciseId, out var n) ? n : string.Empty;

    /// <summary>Uppdaterar anteckningen för en övning.</summary>
    private void SetNote(int exerciseId, string value) =>
        data.Notes[exerciseId] = value;

    /// <summary>Sparar passdata till LocalStorage när användaren lämnar anteckningsfältet.</summary>
    private async Task OnNoteChanged(FocusEventArgs _)
    {
        await LocalStorage.SetItemAsync(PassKey, data);
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

    private void NavigateToExercise(int idx)
    {
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
        _ = JSRuntime.InvokeVoidAsync("arnoldOne.releaseWakeLock");
    }

    private class PassData
    {
        public Dictionary<int, double[]> Reps { get; set; } = new();
        public Dictionary<int, string> Notes { get; set; } = new();
        public Dictionary<int, bool[]> CompletedSets { get; set; } = new();
    }
}
