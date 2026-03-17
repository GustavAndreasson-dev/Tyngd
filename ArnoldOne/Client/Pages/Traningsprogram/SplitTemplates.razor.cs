using ArnoldOne.Shared.TrainingPrograms;
using Microsoft.AspNetCore.Components;

namespace ArnoldOne.Client.Pages.Traningsprogram;

/// <summary>
/// Sida som visar alla tillgängliga träningssplitmallar med övningar och defaultvärden.
/// </summary>
public partial class SplitTemplates : ComponentBase
{
    // PPL-mallen hämtas från generatorn och används enbart för visning
    private readonly TrainingSplitTemplate template = new TrainingSplitTemplateGenerator().CreatePpl();

    /// <summary>Formaterar defaultvärden för en övningsmall som läsbar text.</summary>
    private static string FormatDefault(TrainingExerciseTemplate exercise)
    {
        var reps = exercise.DefaultRepsMin == exercise.DefaultRepsMax
            ? exercise.DefaultRepsMin.ToString()
            : $"{exercise.DefaultRepsMin}-{exercise.DefaultRepsMax}";

        var weight = exercise.DefaultWeight == 0
            ? "kroppsvikt"
            : $"{exercise.DefaultWeight:0.#} kg";

        return $"{exercise.DefaultSets} x {reps} @ {weight}";
    }
}
