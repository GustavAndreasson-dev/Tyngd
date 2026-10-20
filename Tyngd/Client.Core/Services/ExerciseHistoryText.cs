using Tyngd.Shared.DTOs;

namespace Tyngd.Client.Services;

/// <summary>Texterna för "Din historik" i övningsinfon. Vikter med fast kultur, som övriga vikter.</summary>
public static class ExerciseHistoryText
{
    /// <summary>T.ex. "Bästa vikt: 42.5 kg · 7 senaste tillfällen från alla pass".</summary>
    public static string Summary(IReadOnlyList<ExerciseHistoryEntryDto> history) =>
        FormattableString.Invariant($"Bästa vikt: {history.Max(entry => entry.MaxWeight):0.##} kg · {history.Count} senaste tillfällen från alla pass");

    /// <summary>De fem senaste tillfällena, nyast först (historiken kommer äldst först).</summary>
    public static IReadOnlyList<(string When, string What)> Rows(IReadOnlyList<ExerciseHistoryEntryDto> history) =>
        Enumerable.Reverse(history).Take(5)
            .Select(entry => (
                $"{SvDate.Short(entry.Date)} · {entry.PassName}",
                FormattableString.Invariant($"{entry.MaxWeight:0.##} kg · {entry.Sets} set · {entry.TotalReps} reps")))
            .ToList();
}
