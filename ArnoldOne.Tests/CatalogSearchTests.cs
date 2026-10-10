using ArnoldOne.Client.Services;
using ArnoldOne.Shared.Catalog;

namespace ArnoldOne.Tests;

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
    public void Names_starting_with_the_text_come_first() =>
        Assert.Equal(1, CatalogService.Search(All, "bänkpress").First().CatalogExerciseId);
}
