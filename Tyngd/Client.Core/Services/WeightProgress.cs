using Tyngd.Shared.DTOs;

namespace Tyngd.Client.Services;

/// <summary>Viktutvecklingen på passidan: liten graf och texten bredvid.</summary>
public static class WeightProgress
{
    /// <summary>Punkter i en 300x80-yta; vikterna skalas mellan min och max.</summary>
    public static IReadOnlyList<(double X, double Y)> SparkPoints(IReadOnlyList<ExerciseProgressPointDto> points)
    {
        var min = points.Min(p => p.Weight);
        var range = Math.Max(points.Max(p => p.Weight) - min, 1m);
        return points
            .Select((p, i) => (8 + i * 284.0 / Math.Max(points.Count - 1, 1), 72 - (double)((p.Weight - min) / range) * 64))
            .ToList();
    }

    /// <summary>T.ex. "42.5 kg (+2.5)": senaste vikten och skillnaden mot första.</summary>
    public static string Summary(IReadOnlyList<ExerciseProgressPointDto> points)
    {
        var delta = points[^1].Weight - points[0].Weight;
        // Fast kultur, som övriga vikter (se ExerciseFormat).
        return FormattableString.Invariant($"{points[^1].Weight:0.##} kg")
            + (delta == 0 ? "" : FormattableString.Invariant($" ({(delta > 0 ? "+" : "")}{delta:0.##})"));
    }
}
