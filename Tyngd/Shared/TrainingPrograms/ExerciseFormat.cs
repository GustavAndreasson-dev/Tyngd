using System.Globalization;
using Tyngd.Shared.DTOs;

namespace Tyngd.Shared.TrainingPrograms;

/// <summary>Gemensam text för en övnings standardvärden, så webb och app visar samma sak.</summary>
public static class ExerciseFormat
{
    /// <summary>T.ex. "3x8-12".</summary>
    public static string SetsReps(int sets, int repsMin, int repsMax) => $"{sets}x{Reps(repsMin, repsMax)}";

    /// <summary>T.ex. "3 x 8-12 @ 22.5 kg" eller "3 x 10 @ kroppsvikt".</summary>
    public static string Summary(int sets, int repsMin, int repsMax, decimal weight)
    {
        // Fast kultur: webben har alltid visat punkt som decimaltecken, appen ska inte visa "22,5".
        var weightText = weight == 0
            ? "kroppsvikt"
            : weight.ToString("0.##", CultureInfo.InvariantCulture) + " kg";

        return $"{sets} x {Reps(repsMin, repsMax)} @ {weightText}";
    }

    public static string SetsReps(TrainingExerciseTemplate e) => SetsReps(e.DefaultSets, e.DefaultRepsMin, e.DefaultRepsMax);

    public static string Summary(TrainingExerciseTemplate e) => Summary(e.DefaultSets, e.DefaultRepsMin, e.DefaultRepsMax, e.DefaultWeight);

    public static string Summary(CreateExerciseDto e) => Summary(e.DefaultSets, e.RepsMin, e.RepsMax, e.DefaultWeight);

    private static string Reps(int min, int max) =>
        min == max ? min.ToString(CultureInfo.InvariantCulture) : $"{min}-{max}";
}
