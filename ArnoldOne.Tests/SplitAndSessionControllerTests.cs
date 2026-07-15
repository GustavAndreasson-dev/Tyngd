using System.Security.Claims;
using ArnoldOne.Server.Controllers;
using ArnoldOne.Shared.DTOs;
using DbArnold.DbArnold.models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DbExercise = DbArnold.DbArnold.models.Exercise;

namespace ArnoldOne.Tests;

public class SplitAndSessionControllerTests
{
    [Fact]
    public async Task Editing_preserves_ids_and_archives_removed_exercise()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var split = CreateSplit(userId);
        var kept = split.Pass.Single().Exercise.First();
        var removed = split.Pass.Single().Exercise.Last();
        context.Add(split);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = WithUser(new UserSplitController(context), userId);
        var request = new CreateSplitRequest
        {
            SplitName = "Uppdaterad",
            Passes =
            [
                new CreatePassDto
                {
                    PassId = split.Pass.Single().PassId,
                    PassName = "Pass A",
                    Exercises =
                    [
                        new CreateExerciseDto
                        {
                            ExerciseId = kept.ExerciseId,
                            ExerciseName = kept.ExerciseName,
                            DefaultSets = 4,
                            RepsMin = 6,
                            RepsMax = 8,
                            DefaultWeight = 62.5m,
                        },
                        new CreateExerciseDto
                        {
                            ExerciseName = "Lutande bänkpress",
                            DefaultSets = 3,
                            RepsMin = 8,
                            RepsMax = 10,
                            DefaultWeight = 40,
                        },
                    ],
                },
            ],
        };

        var result = await controller.UpdateMySplit(split.SplitId, request);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(4, kept.DefaultSets);
        Assert.True(kept.Visability);
        Assert.False(removed.Visability);
        var added = await context.Exercise.SingleAsync(
            exercise => exercise.ExerciseName == "Lutande bänkpress",
            TestContext.Current.CancellationToken);
        Assert.NotEqual(kept.ExerciseId, added.ExerciseId);
        Assert.NotEqual(removed.ExerciseId, added.ExerciseId);
    }

    [Fact]
    public async Task User_cannot_edit_another_users_split()
    {
        await using var context = CreateContext();
        var split = CreateSplit(Guid.NewGuid());
        context.Add(split);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var controller = WithUser(new UserSplitController(context), Guid.NewGuid());

        var result = await controller.UpdateMySplit(split.SplitId, new CreateSplitRequest
        {
            SplitName = "Försök",
            Passes =
            [
                new CreatePassDto
                {
                    PassName = "Pass",
                    Exercises = [new CreateExerciseDto { ExerciseName = "Övning" }],
                },
            ],
        });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Pass_route_requires_matching_split()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var split = CreateSplit(userId);
        context.Add(split);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var controller = WithUser(new UserSplitController(context), userId);

        var result = await controller.GetPass(split.SplitId + 100, split.Pass.Single().PassId);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Completed_session_rejects_normal_upsert()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var split = CreateSplit(userId);
        context.Add(split);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var pass = split.Pass.Single();
        var exercise = pass.Exercise.First();
        var session = new Session
        {
            PassId = pass.PassId,
            SessionKey = Guid.NewGuid(),
            Date = new DateOnly(2026, 7, 15),
            StartedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
            CompletedAtUtc = DateTimeOffset.UtcNow,
        };
        context.Add(session);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var controller = WithUser(new SessionController(context), userId);

        var result = await controller.UpsertExercise(new UpsertExerciseRequest
        {
            SessionKey = session.SessionKey,
            PassId = pass.PassId,
            Date = session.Date,
            StartedAtUtc = session.StartedAtUtc,
            ExerciseId = exercise.ExerciseId,
        });

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Separate_session_keys_allow_multiple_sessions_same_day()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var split = CreateSplit(userId);
        context.Add(split);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var controller = WithUser(new SessionController(context), userId);
        var passId = split.Pass.Single().PassId;
        var date = new DateOnly(2026, 7, 15);

        await controller.SaveSession(new SaveSessionRequest
        {
            SessionKey = Guid.NewGuid(), PassId = passId, Date = date,
            StartedAtUtc = DateTimeOffset.UtcNow.AddHours(-2), CompleteSession = true,
        });
        await controller.SaveSession(new SaveSessionRequest
        {
            SessionKey = Guid.NewGuid(), PassId = passId, Date = date,
            StartedAtUtc = DateTimeOffset.UtcNow, CompleteSession = true,
        });

        Assert.Equal(2, await context.Session.CountAsync(TestContext.Current.CancellationToken));
    }

    private static ArnoldContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ArnoldContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ArnoldContext(options);
    }

    private static T WithUser<T>(T controller, Guid userId) where T : ControllerBase
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "Test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };
        return controller;
    }

    private static Split CreateSplit(Guid userId)
    {
        var pass = new Pass
        {
            PassName = "Pass A",
            Visability = 1,
            Ordning = 1,
            Exercise =
            [
                new DbExercise
                {
                    ExerciseName = "Bänkpress", DefaultSets = 3, DefautlRepsMin = 8,
                    DefaultRepsMax = 12, DefaultWeight = 50, Ordning = 1, Visability = true,
                },
                new DbExercise
                {
                    ExerciseName = "Flyes", DefaultSets = 3, DefautlRepsMin = 10,
                    DefaultRepsMax = 12, DefaultWeight = 15, Ordning = 2, Visability = true,
                },
            ],
        };

        return new Split
        {
            SplitName = "Styrka",
            UserId = userId,
            Visability = true,
            Pass = [pass],
        };
    }
}
