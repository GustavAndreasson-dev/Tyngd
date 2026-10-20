using Tyngd.Client.Services;

namespace Tyngd.Tests;

public class NumberInputTests
{
    [Theory]
    [InlineData("62,5", 62.5)]
    [InlineData("62.5", 62.5)]
    [InlineData(" 60 ", 60)]
    [InlineData("", 0)]
    [InlineData("6,", 6)]
    public void Parses_comma_and_point_decimals(string text, double expected)
    {
        Assert.True(NumberInput.TryParse(text, out var value));
        Assert.Equal((decimal)expected, value);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-5")]
    [InlineData("1,2,3")]
    public void Rejects_invalid_text(string text) => Assert.False(NumberInput.TryParse(text, out _));

    [Fact]
    public void Formats_without_trailing_decimals()
    {
        Assert.Equal("62.5", NumberInput.Format(62.50m));
        Assert.Equal("60", NumberInput.Format(60m));
    }
}
