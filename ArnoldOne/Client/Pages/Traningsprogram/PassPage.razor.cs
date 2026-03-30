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
public partial class PassPage : ComponentBase
{
    /// <summary>Id för den valda splitten, hämtas från route-parametern.</summary>
    [Parameter]
    public int SplitId { get; set; }

    /// <summary>Id för det valda passet, hämtas från route-parametern.</summary>
    [Parameter]
    public int PassId { get; set; }

    /// <summary>Tjänst för att läsa och skriva användardata i webbläsarens LocalStorage.</summary>
    [Inject]
    private ILocalStorageService LocalStorage { get; set; } = default!;

    /// <summary>Används för att koppla on keydown-lyssnaren på anteckningsfältet.</summary>
    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

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

    /// <summary>
    /// Körs när <see cref="PassId"/> ändras. Hämtar rätt passmall och
    /// laddar om passdata om PassId har bytt sedan senaste inläsning.
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        pass = new TrainingSplitTemplateGenerator().FindPass(SplitId, PassId);

        if (pass is not null)
        {
            if (_isInitialized && _lastPassId != PassId)
            {
                _lastPassId = PassId;
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
        SetReps(exerciseId, setIndex, Math.Clamp(current + delta, 1, 20));
    }

    /// <summary>Uppdaterar antalet reps för ett specifikt set.</summary>
    private void SetReps(int exerciseId, int setIndex, double value)
    {
        if (data.Reps.TryGetValue(exerciseId, out var sets) && setIndex < sets.Length)
            sets[setIndex] = value;
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
    private class PassData
    {
        public Dictionary<int, double[]> Reps { get; set; } = new();
        public Dictionary<int, string> Notes { get; set; } = new();
    }
}
