using System.Security.Claims;
using Tyngd.Client.Services;
using Tyngd.Server.Controllers;
using Tyngd.Shared.Catalog;
using Tyngd.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Tyngd.Tests;

public class MuscleLoadTests
{
    [Fact]
    public void Primary_counts_one_secondary_half_and_a_muscle_counts_once_per_exercise()
    {
        var load = MuscleLoad.Sum(
        [
            (["Chest", "Chest"], ["Triceps", "Shoulders"]),
            (["Triceps"], ["Chest"]),
        ]);

        Assert.Equal(1.5, load["Chest"]);
        Assert.Equal(1.5, load["Triceps"]);
        Assert.Equal(0.5, load["Shoulders"]);
    }

    [Fact]
    public void Every_wger_muscle_has_a_picture_mapping() =>
        Assert.Equal(15, Muscles.All.Select(m => m.WgerId).Distinct().Count());

    [Fact]
    public void Unlinked_exercises_are_ignored_in_client_load()
    {
        List<CatalogExerciseDto> catalog = [new() { CatalogExerciseId = 1, PrimaryMuscles = ["Quads"], SecondaryMuscles = ["Glutes"] }];

        var load = CatalogService.MuscleLoadFor(catalog, [1, null, 99]);

        Assert.Equal(2, load.Count);
        Assert.Equal(1, load["Quads"]);
    }

    [Fact]
    public async Task Split_muscles_sum_visible_linked_exercises_of_own_split()
    {
        await using var context = new TyngdContext(new DbContextOptionsBuilder<TyngdContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var userId = Guid.NewGuid();
        var squat = new ExerciseCatalog { WgerUuid = Guid.NewGuid(), NameEn = "Squat", SourceHash = "x", Muscles = "Quads", MusclesSecondary = "Glutes", IsActive = true };
        var split = new Split
        {
            UserId = userId, SplitName = "S", Visability = true,
            Pass =
            [
                new Pass
                {
                    PassName = "A", Visability = 1, Ordning = 1,
                    Exercise =
                    [
                        new Tyngd.Data.Models.Exercise { ExerciseName = "Knäböj", Visability = true, Ordning = 1, CatalogExercise = squat },
                        new Tyngd.Data.Models.Exercise { ExerciseName = "Borttagen", Visability = false, Ordning = 2, CatalogExercise = squat },
                        new Tyngd.Data.Models.Exercise { ExerciseName = "Egen", Visability = true, Ordning = 3 },
                    ],
                },
            ],
        };
        context.Add(split);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var controller = new UserSplitController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Test")),
                },
            },
        };

        var result = Assert.IsType<OkObjectResult>((await controller.GetSplitMuscles(split.SplitId)).Result);
        var load = Assert.IsType<Dictionary<string, double>>(result.Value);

        Assert.Equal(1, load["Quads"]);
        Assert.Equal(0.5, load["Glutes"]);
    }
}
