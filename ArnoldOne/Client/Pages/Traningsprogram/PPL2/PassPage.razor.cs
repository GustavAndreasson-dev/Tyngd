using ArnoldOne.Shared.TrainingPrograms;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace ArnoldOne.Client.Pages.Traningsprogram.PPL2;

/// <summary>
/// Sida för ett enskilt träningspass i PPL2-programmet.
/// Hämtar passmall via <see cref="TrainingSplitTemplateGenerator"/> och sparar användarens vikter/reps i LocalStorage.
/// </summary>
public partial class PassPage : ComponentBase
{
    /// <summary>Id för det valda passet, hämtas från route-parametern.</summary>
    [Parameter]
    public int PassId { get; set; }

    /// <summary>Tjänst för att läsa och skriva användardata i webbläsarens LocalStorage.</summary>
    [Inject]
    private ILocalStorageService LocalStorage { get; set; } = default!;

    // Aktuellt pass hämtat från PPL-mallen
    private TrainingPassTemplate? pass;
    private Transition transition = Transition.None;
    private int activeIndex;

    // Användarens vikter och reps, sparas i LocalStorage
    private PassData data = new();
    private string StorageKey => $"pass-{PassId}";

    /// <summary>
    /// Körs när <see cref="PassId"/> ändras. Hämtar rätt pass från PPL-mallen och
    /// fyller i defaultvärden för vikt och reps om de saknas.
    /// </summary>
    protected override void OnParametersSet()
    {
        // Hämta rätt pass från PPL-mallen baserat på PassId
        var template = new TrainingSplitTemplateGenerator().CreatePpl();
        pass = template.Passes.FirstOrDefault(p => p.PassId == PassId);

        if (pass is not null)
        {
            // Fyll i defaultvärden för vikt och reps om de inte redan finns
            foreach (var ex in pass.Exercises)
            {
                if (!data.Weights.ContainsKey(ex.ExerciseId))
                {
                    data.Weights[ex.ExerciseId] = ex.DefaultWeight;
                }

                if (!data.Reps.ContainsKey(ex.ExerciseId))
                {
                    data.Reps[ex.ExerciseId] = new double[ex.DefaultSets];
                    var defaultRep = (double)ex.DefaultRepsMin;
                    for (int i = 0; i < ex.DefaultSets; i++)
                    {
                        data.Reps[ex.ExerciseId][i] = defaultRep;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Läser sparad passdata från LocalStorage vid första renderingen
    /// och skriver över defaultvärdena om lagrad data finns.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var stored = await LocalStorage.GetItemAsync<PassData>(StorageKey);
            if (stored is not null)
            {
                data = stored;
            }

            StateHasChanged();
        }
    }

    /// <summary>Returnerar sparad vikt för en övning, eller null om ingen finns.</summary>
    private decimal? GetWeight(int exerciseId) =>
        data.Weights.TryGetValue(exerciseId, out var w) ? w : null;

    /// <summary>Sparar vikten för en övning. Nollvärde lagras som 0.</summary>
    private void SetWeight(int exerciseId, decimal? value) =>
        data.Weights[exerciseId] = value ?? 0;

    /// <summary>Returnerar antalet reps för ett specifikt set, eller 1 om index saknas.</summary>
    private double GetReps(int exerciseId, int setIndex)
    {
        if (data.Reps.TryGetValue(exerciseId, out var sets) && setIndex < sets.Length)
        {
            return sets[setIndex];
        }
        return 1;
    }

    /// <summary>Uppdaterar antalet reps för ett specifikt set.</summary>
    private void SetReps(int exerciseId, int setIndex, double value)
    {
        if (data.Reps.TryGetValue(exerciseId, out var sets) && setIndex < sets.Length)
        {
            sets[setIndex] = value;
        }
    }

    /// <summary>Sparar passdata till LocalStorage när användaren lämnar ett viktfält.</summary>
    private async Task OnWeightChanged(FocusEventArgs _)
    {
        await LocalStorage.SetItemAsync(StorageKey, data);
    }

    /// <summary>Formaterar set och reps från en övningsmall som text, t.ex. "3x8-12".</summary>
    private static string FormatSetsReps(TrainingExerciseTemplate exercise)
    {
        var reps = exercise.DefaultRepsMin == exercise.DefaultRepsMax
            ? exercise.DefaultRepsMin.ToString()
            : $"{exercise.DefaultRepsMin}-{exercise.DefaultRepsMax}";

        return $"{exercise.DefaultSets}x{reps}";
    }

    /// <summary>
    /// Användarens sparade data för ett pass: vikter och reps per övning och set.
    /// Serialiseras till och från LocalStorage.
    /// </summary>
    private class PassData
    {
        public Dictionary<int, decimal> Weights { get; set; } = new();
        public Dictionary<int, double[]> Reps { get; set; } = new();
    }
}
