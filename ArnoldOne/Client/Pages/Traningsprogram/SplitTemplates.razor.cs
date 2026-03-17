using ArnoldOne.Shared.TrainingPrograms;
using Microsoft.AspNetCore.Components;

namespace ArnoldOne.Client.Pages.Traningsprogram;

public partial class SplitTemplates : ComponentBase
{
    private readonly TrainingSplit template = new TrainingSplitTemplates().CreatePpl();

    private static string FormatDefault(TrainingExercise exercise)
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
