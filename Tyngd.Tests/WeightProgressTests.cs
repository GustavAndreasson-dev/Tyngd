using System.Globalization;
using Tyngd.Client.Services;
using Tyngd.Shared.DTOs;

namespace Tyngd.Tests;

public class WeightProgressTests
{
    private static List<ExerciseProgressPointDto> Points(params decimal[] weights) =>
        weights.Select((w, i) => new ExerciseProgressPointDto(new DateOnly(2026, 1, 1).AddDays(i), w)).ToList();

    [Fact]
    public void Spark_points_span_the_box_from_min_to_max() =>
        Assert.Equal([(8.0, 72.0), (150.0, 40.0), (292.0, 8.0)], WeightProgress.SparkPoints(Points(40, 45, 50)));

    [Fact]
    public void Flat_series_stays_on_the_bottom_line() =>
        Assert.All(WeightProgress.SparkPoints(Points(40, 40)), p => Assert.Equal(72.0, p.Y));

    [Fact]
    public void Summary_shows_latest_weight_and_change_with_point_decimals()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("sv-SE");
        try
        {
            Assert.Equal("42.5 kg (+2.5)", WeightProgress.Summary(Points(40, 42.5m)));
            Assert.Equal("38 kg (-2)", WeightProgress.Summary(Points(40, 38)));
            Assert.Equal("40 kg", WeightProgress.Summary(Points(40, 40)));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
