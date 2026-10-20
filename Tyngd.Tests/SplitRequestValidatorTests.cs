using Tyngd.Shared.DTOs;

namespace Tyngd.Tests;

public class SplitRequestValidatorTests
{
    [Fact]
    public void Valid_split_has_no_errors()
    {
        var request = ValidRequest();

        var errors = SplitRequestValidator.Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Invalid_nested_values_return_field_keys()
    {
        var request = ValidRequest();
        request.Passes[0].Exercises[0].RepsMin = 12;
        request.Passes[0].Exercises[0].RepsMax = 8;
        request.Passes[0].Exercises[0].DefaultWeight = -1;

        var errors = SplitRequestValidator.Validate(request);

        Assert.Contains(errors, error => error.Key == "Passes[0].Exercises[0].RepsMax");
        Assert.Contains(errors, error => error.Key == "Passes[0].Exercises[0].DefaultWeight");
    }

    [Theory]
    [InlineData("#23CE6B", true)]
    [InlineData(null, true)]
    [InlineData("#000000", false)]
    [InlineData("red;background:url(x)", false)]
    public void Pass_color_must_be_one_of_the_offered_colors(string? color, bool valid)
    {
        var request = ValidRequest();
        request.Passes[0].Color = color;

        var errors = SplitRequestValidator.Validate(request);

        Assert.Equal(valid, !errors.Any(error => error.Key == "Passes[0].Color"));
    }

    [Fact]
    public void Split_requires_at_least_one_pass()
    {
        var request = ValidRequest();
        request.Passes.Clear();

        var errors = SplitRequestValidator.Validate(request);

        Assert.Contains(errors, error => error.Key == "Passes");
    }

    private static CreateSplitRequest ValidRequest() => new()
    {
        SplitName = "Styrka",
        Passes =
        [
            new CreatePassDto
            {
                PassName = "Pass A",
                Exercises =
                [
                    new CreateExerciseDto
                    {
                        ExerciseName = "Bänkpress",
                        DefaultSets = 3,
                        RepsMin = 8,
                        RepsMax = 12,
                        DefaultWeight = 50,
                    },
                ],
            },
        ],
    };
}
