using System.Security.Claims;
using ArnoldOne.Server.Controllers;
using DbArnold.DbArnold.models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArnoldOne.Tests;

public class FavoritesControllerTests
{
    [Fact]
    public async Task Add_is_idempotent_and_favorites_are_per_user()
    {
        await using var context = CreateContext();
        var catalog = new ExerciseCatalog { WgerUuid = Guid.NewGuid(), NameEn = "Squat", SourceHash = "x", IsActive = true };
        context.Add(catalog);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var me = Controller(context, Guid.NewGuid());
        var other = Controller(context, Guid.NewGuid());

        Assert.IsType<NoContentResult>(await me.Add(catalog.CatalogExerciseId));
        Assert.IsType<NoContentResult>(await me.Add(catalog.CatalogExerciseId));

        Assert.Equal([catalog.CatalogExerciseId], (await me.GetAll()).Value);
        Assert.Empty((await other.GetAll()).Value!);
        Assert.Equal(1, await context.UserFavoriteExercise.CountAsync(TestContext.Current.CancellationToken));

        Assert.IsType<NoContentResult>(await me.Remove(catalog.CatalogExerciseId));
        Assert.Empty((await me.GetAll()).Value!);
    }

    [Fact]
    public async Task Unknown_catalog_exercise_is_not_found()
    {
        await using var context = CreateContext();
        Assert.IsType<NotFoundResult>(await Controller(context, Guid.NewGuid()).Add(999));
    }

    private static FavoritesController Controller(ArnoldContext context, Guid userId) =>
        new(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Test")),
                },
            },
        };

    private static ArnoldContext CreateContext() =>
        new(new DbContextOptionsBuilder<ArnoldContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
