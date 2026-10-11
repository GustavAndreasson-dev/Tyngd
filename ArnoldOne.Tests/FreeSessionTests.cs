using System.Security.Claims;
using ArnoldOne.Server.Controllers;
using ArnoldOne.Shared.DTOs;
using ArnoldOne.Shared.TrainingPrograms;
using DbArnold.DbArnold.models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DbExercise = DbArnold.DbArnold.models.Exercise;

namespace ArnoldOne.Tests;

public class FreeSessionTests
{
    [Fact]
    public async Task Free_sessions_share_one_hidden_split_that_never_shows_in_split_lists()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var free = With(new FreeSessionController(context), userId);

        var first = Ok(await free.Start(Request("Löst pass 1", ("Knäböj", null))));
        var second = Ok(await free.Start(Request("Löst pass 2", ("Rodd", null))));

        Assert.Equal(first.SplitId, second.SplitId);
        Assert.NotEqual(first.PassId, second.PassId);
        Assert.Equal(1, await context.Split.CountAsync(s => s.IsFreeSessions, TestContext.Current.CancellationToken));

        var splits = With(new UserSplitController(context), userId);
        var active = Assert.IsType<OkObjectResult>((await splits.GetActiveSplits()).Result);
        Assert.Empty(Assert.IsType<List<TrainingSplit>>(active.Value));
        var mine = Assert.IsType<OkObjectResult>((await splits.GetMySplits()).Result);
        Assert.Empty(Assert.IsType<List<TrainingSplit>>(mine.Value));
        Assert.IsType<NotFoundResult>(await splits.DeleteMySplit(first.SplitId));

        var pass = Assert.IsType<OkObjectResult>((await splits.GetPass(first.SplitId, first.PassId)).Result);
        Assert.True(Assert.IsType<TrainingPassTemplate>(pass.Value).IsFreeSession);
    }

    [Fact]
    public async Task Catalog_exercise_starts_from_latest_logged_weight()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var bench = new ExerciseCatalog { WgerUuid = Guid.NewGuid(), NameEn = "Bench", SourceHash = "x", IsActive = true };
        var exercise = new DbExercise { ExerciseName = "Bänkpress", Visability = true, Ordning = 1, CatalogExercise = bench };
        var pass = new Pass { PassName = "A", Visability = 1, Ordning = 1, Exercise = [exercise] };
        context.Add(new Split { UserId = userId, SplitName = "S", Visability = true, Pass = [pass] });
        context.Add(new Session
        {
            Pass = pass, SessionKey = Guid.NewGuid(), Date = new DateOnly(2026, 10, 1), StartedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            CompletedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ExerciseLog = [new ExerciseLog { Exercise = exercise, SetLog = [new SetLog { SetIndex = 0, Reps = 5, IsDone = true, Weight = 82.5m }] }],
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var started = Ok(await With(new FreeSessionController(context), userId).Start(Request("Löst", ("Bänkpress", bench.CatalogExerciseId))));

        var created = await context.Exercise.SingleAsync(e => e.PassId == started.PassId, TestContext.Current.CancellationToken);
        Assert.Equal(82.5m, created.DefaultWeight);
        Assert.Equal(bench.CatalogExerciseId, created.CatalogExerciseId);
    }

    [Fact]
    public async Task Exercises_can_be_added_only_to_own_free_pass()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var started = Ok(await With(new FreeSessionController(context), userId).Start(Request("Löst", ("Knäböj", null))));
        var normal = new Split { UserId = userId, SplitName = "S", Visability = true, Pass = [new Pass { PassName = "A", Visability = 1, Ordning = 1 }] };
        context.Add(normal);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var added = await With(new FreeSessionController(context), userId).AddExercise(started.PassId, new FreeSessionExerciseDto { Name = "Dips" });
        Assert.IsType<OkObjectResult>(added.Result);
        Assert.Equal(2, await context.Exercise.CountAsync(e => e.PassId == started.PassId, TestContext.Current.CancellationToken));

        Assert.IsType<NotFoundResult>((await With(new FreeSessionController(context), userId).AddExercise(normal.Pass.Single().PassId, new FreeSessionExerciseDto { Name = "Dips" })).Result);
        Assert.IsType<NotFoundResult>((await With(new FreeSessionController(context), Guid.NewGuid()).AddExercise(started.PassId, new FreeSessionExerciseDto { Name = "Dips" })).Result);
    }

    [Fact]
    public async Task Keeping_moves_the_pass_into_a_new_or_own_split_and_discard_hides_it()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var free = With(new FreeSessionController(context), userId);
        var started = Ok(await free.Start(Request("Löst", ("Knäböj", null))));

        var kept = Ok(await free.Keep(started.PassId, new KeepFreeSessionRequest { PassName = "Benpass", NewSplitName = "Mina pass" }));

        Assert.NotEqual(started.SplitId, kept.SplitId);
        Assert.Equal(started.PassId, kept.PassId);
        var mine = Assert.IsType<OkObjectResult>((await With(new UserSplitController(context), userId).GetMySplits()).Result);
        var split = Assert.Single(Assert.IsType<List<TrainingSplit>>(mine.Value));
        Assert.Equal("Mina pass", split.SplitName);
        Assert.Equal("Benpass", Assert.Single(split.Passes).PassName);

        var second = Ok(await free.Start(Request("Löst 2", ("Rodd", null))));
        Assert.IsType<NoContentResult>(await free.Discard(second.PassId));
        Assert.Equal(0, (await context.Pass.SingleAsync(p => p.PassId == second.PassId, TestContext.Current.CancellationToken)).Visability);
    }

    [Fact]
    public async Task Cannot_keep_into_someone_elses_split()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var foreign = new Split { UserId = Guid.NewGuid(), SplitName = "Annans", Visability = true };
        context.Add(foreign);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var free = With(new FreeSessionController(context), userId);
        var started = Ok(await free.Start(Request("Löst", ("Knäböj", null))));

        var result = await free.Keep(started.PassId, new KeepFreeSessionRequest { PassName = "X", TargetSplitId = foreign.SplitId });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Empty_free_session_is_rejected()
    {
        await using var context = CreateContext();
        var result = await With(new FreeSessionController(context), Guid.NewGuid()).Start(new StartFreeSessionRequest { PassName = "Löst" });
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    private static StartFreeSessionRequest Request(string passName, params (string Name, int? CatalogId)[] exercises) => new()
    {
        PassName = passName,
        Exercises = exercises.Select(e => new FreeSessionExerciseDto { Name = e.Name, CatalogExerciseId = e.CatalogId }).ToList(),
    };

    private static StartFreeSessionResponse Ok(ActionResult<StartFreeSessionResponse> result) =>
        Assert.IsType<StartFreeSessionResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);

    private static T With<T>(T controller, Guid userId) where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Test")) },
        };
        return controller;
    }

    private static ArnoldContext CreateContext() =>
        new(new DbContextOptionsBuilder<ArnoldContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
