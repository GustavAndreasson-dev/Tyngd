using Tyngd.Client.Services;
using Tyngd.Shared.Catalog;

namespace Tyngd.Tests;

public class CatalogSearchTests
{
    private static readonly List<CatalogExerciseDto> All =
    [
        new() { CatalogExerciseId = 1, Name = "Bänkpress med skivstång", NameEn = "Bench Press", Aliases = ["Barbell bench"] },
        new() { CatalogExerciseId = 2, Name = "Lutande bänkpress", NameEn = "Incline Bench Press" },
        new() { CatalogExerciseId = 3, Name = "Knäböj", NameEn = "Squat" },
        new() { CatalogExerciseId = 4, Name = "Arnoldpress", NameEn = "Arnold Shoulder Press", Aliases = ["Schwarzenegger Press"] },
    ];

    [Theory]
    [InlineData("bänk", new[] { 1, 2 })]
    [InlineData("BENCH", new[] { 1, 2 })]
    [InlineData("squat", new[] { 3 })]
    [InlineData("schwarzenegger", new[] { 4 })]
    [InlineData("press lutande", new[] { 2 })]
    [InlineData("marklyft", new int[0])]
    public void Search_matches_swedish_english_and_aliases(string text, int[] expected) =>
        Assert.Equal(expected.Order(), CatalogService.Search(All, text).Select(e => e.CatalogExerciseId).Order());

    [Fact]
    public void Filter_combines_filters_and_puts_favorites_first()
    {
        List<CatalogExerciseDto> all =
        [
            new() { CatalogExerciseId = 1, Name = "Bänkpress", NameEn = "Bench Press", Category = "Bröst", PrimaryMuscles = ["chest"], Equipment = ["Skivstång"] },
            new() { CatalogExerciseId = 2, Name = "Hantelpress", NameEn = "Dumbbell Press", Category = "Bröst", SecondaryMuscles = ["chest"], Equipment = ["Hantlar"] },
            new() { CatalogExerciseId = 3, Name = "Knäböj", NameEn = "Squat", Category = "Ben", PrimaryMuscles = ["quads"], Equipment = ["Skivstång"] },
        ];
        var favorites = new HashSet<int> { 2 };

        Assert.Equal([2, 1, 3], CatalogService.Filter(all, favorites, false, null, null, null, null).Select(e => e.CatalogExerciseId));
        Assert.Equal([2, 1], CatalogService.Filter(all, favorites, false, null, "chest", null, null).Select(e => e.CatalogExerciseId));
        Assert.Equal([1, 3], CatalogService.Filter(all, favorites, false, null, null, "Skivstång", null).Select(e => e.CatalogExerciseId));
        Assert.Equal([3], CatalogService.Filter(all, favorites, false, "Ben", null, null, null).Select(e => e.CatalogExerciseId));
        Assert.Equal([2], CatalogService.Filter(all, favorites, true, null, null, null, "press").Select(e => e.CatalogExerciseId));
    }

    [Fact]
    public void Meta_skips_empty_equipment_and_same_english_name()
    {
        Assert.Equal("Bröst · Skivstång, Bänk · Bench Press",
            CatalogService.Meta(new() { Name = "Bänkpress", NameEn = "Bench Press", Category = "Bröst", Equipment = ["Skivstång", "Bänk"] }));
        Assert.Equal("Core", CatalogService.Meta(new() { Name = "Plank", NameEn = "plank", Category = "Core" }));
    }

    [Fact]
    public void Names_starting_with_the_text_come_first() =>
        Assert.Equal(1, CatalogService.Search(All, "bänkpress").First().CatalogExerciseId);
}
