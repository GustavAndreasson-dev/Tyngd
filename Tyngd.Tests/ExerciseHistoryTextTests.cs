using Tyngd.Client.Services;
using Tyngd.Shared.DTOs;

namespace Tyngd.Tests;

public class ExerciseHistoryTextTests
{
    private static readonly List<ExerciseHistoryEntryDto> History =
        Enumerable.Range(1, 6)
            .Select(day => new ExerciseHistoryEntryDto { Date = new DateOnly(2026, 10, day), PassName = $"Pass {day}", MaxWeight = 40 + day / 2m, Sets = 3, TotalReps = 30 })
            .ToList();

    [Fact]
    public void Summary_shows_best_weight_and_count() =>
        Assert.Equal("Bästa vikt: 43 kg · 6 senaste tillfällen från alla pass", ExerciseHistoryText.Summary(History));

    [Fact]
    public void Rows_are_the_five_newest_first()
    {
        var rows = ExerciseHistoryText.Rows(History);

        Assert.Equal(5, rows.Count);
        Assert.Equal(("6 okt · Pass 6", "43 kg · 3 set · 30 reps"), rows[0]);
        Assert.Equal(("2 okt · Pass 2", "41 kg · 3 set · 30 reps"), rows[^1]);
        Assert.Equal("42.5 kg · 3 set · 30 reps", rows[1].What);
    }
}
