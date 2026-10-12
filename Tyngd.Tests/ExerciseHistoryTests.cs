using System.Security.Claims;
using Tyngd.Server.Controllers;
using Tyngd.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DbExercise = Tyngd.Data.Models.Exercise;

namespace Tyngd.Tests;

public class ExerciseHistoryTests
{
    [Fact]
    public async Task Catalog_history_combines_passes_and_skips_other_users_and_unfinished_sessions()
    {
        await using var context = new TyngdContext(new DbContextOptionsBuilder<TyngdContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var me = Guid.NewGuid();
        var bench = new ExerciseCatalog { WgerUuid = Guid.NewGuid(), NameEn = "Bench", SourceHash = "x", IsActive = true };
        context.Add(bench);
        var day = new DateOnly(2026, 10, 1);

        Split SplitWith(Guid user, string passName, out DbExercise exercise)
        {
            exercise = new DbExercise { ExerciseName = "Bänk", Visability = true, Ordning = 1, CatalogExercise = bench };
            return new Split { UserId = user, SplitName = "S", Visability = true, Pass = [new Pass { PassName = passName, Visability = 1, Ordning = 1, Exercise = [exercise] }] };
        }
        void Log(DbExercise exercise, int dayOffset, decimal weight, bool completed)
        {
            context.Add(new Session
            {
                Pass = exercise.Pass, SessionKey = Guid.NewGuid(), Date = day.AddDays(dayOffset),
                StartedAtUtc = new DateTimeOffset(day.AddDays(dayOffset).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                CompletedAtUtc = completed ? DateTimeOffset.UtcNow : null,
                ExerciseLog = [new ExerciseLog { Exercise = exercise, SetLog = [new SetLog { SetIndex = 0, Reps = 8, IsDone = true, Weight = weight }, new SetLog { SetIndex = 1, Reps = 6, IsDone = true, Weight = weight + 5 }] }],
            });
        }

        context.AddRange(SplitWith(me, "Pass A", out var inA), SplitWith(me, "Löst pass", out var inFree), SplitWith(Guid.NewGuid(), "Annans", out var others));
        Log(inA, 0, 60, completed: true);
        Log(inFree, 2, 62.5m, completed: true);
        Log(inA, 4, 70, completed: false);
        Log(others, 3, 200, completed: true);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = new SessionController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, me.ToString())], "Test")) },
            },
        };

        var result = Assert.IsType<OkObjectResult>((await controller.GetCatalogHistory(bench.CatalogExerciseId)).Result);
        var history = Assert.IsType<List<Tyngd.Shared.DTOs.ExerciseHistoryEntryDto>>(result.Value);

        Assert.Equal(["Pass A", "Löst pass"], history.Select(entry => entry.PassName));
        Assert.Equal([65m, 67.5m], history.Select(entry => entry.MaxWeight));
        Assert.All(history, entry => Assert.Equal(14, entry.TotalReps));
    }
}
