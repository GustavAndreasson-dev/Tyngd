using System.Globalization;
using Tyngd.Shared.TrainingPrograms;

namespace Tyngd.Tests;

public class ExerciseFormatTests
{
    [Theory]
    [InlineData(3, 8, 12, "3x8-12")]
    [InlineData(4, 10, 10, "4x10")]
    public void SetsReps_formats_range_or_single_value(int sets, int min, int max, string expected) =>
        Assert.Equal(expected, ExerciseFormat.SetsReps(sets, min, max));

    [Fact]
    public void Summary_uses_point_as_decimal_separator_regardless_of_culture()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("sv-SE");
        try
        {
            Assert.Equal("3 x 8-12 @ 22.5 kg", ExerciseFormat.Summary(3, 8, 12, 22.5m));
            Assert.Equal("3 x 8-12 @ 40 kg", ExerciseFormat.Summary(3, 8, 12, 40m));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Summary_shows_bodyweight_when_weight_is_zero() =>
        Assert.Equal("3 x 10 @ kroppsvikt", ExerciseFormat.Summary(3, 10, 10, 0m));
}
