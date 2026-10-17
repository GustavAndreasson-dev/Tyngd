using System.Security.Claims;
using Tyngd.Server.Controllers;
using Tyngd.Shared.DTOs;
using Tyngd.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DbExercise = Tyngd.Data.Models.Exercise;

namespace Tyngd.Tests;

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
    public async Task Editable_split_returns_visible_passes_and_exercises_in_order()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var split = CreateSplit(userId);
        var firstPass = split.Pass.Single();
        firstPass.Ordning = 2;
        firstPass.Exercise.Last().Visability = false;
        split.Pass.Add(new Pass
        {
            PassName = "Pass först",
            Visability = 1,
            Ordning = 1,
            Exercise =
            [
                new DbExercise
                {
                    ExerciseName = "Första övningen",
                    DefaultSets = 3,
                    DefautlRepsMin = 8,
                    DefaultRepsMax = 12,
                    DefaultWeight = 20,
                    Ordning = 1,
                    Visability = true,
                },
            ],
        });
        context.Add(split);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var controller = WithUser(new UserSplitController(context), userId);

        var result = await controller.GetMySplitFull(split.SplitId);
        var dto = Assert.IsType<OkObjectResult>(result.Result).Value as CreateSplitRequest;

        Assert.NotNull(dto);
        Assert.Equal(["Pass först", "Pass A"], dto.Passes.Select(pass => pass.PassName));
        Assert.Single(dto.Passes.Last().Exercises);
        Assert.DoesNotContain(dto.Passes.SelectMany(pass => pass.Exercises), exercise => exercise.ExerciseName == "Flyes");
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
    public async Task Late_sync_of_a_completed_session_succeeds_without_changing_it()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var split = CreateSplit(userId);
        context.Add(split);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var pass = split.Pass.Single();
        var exerciseId = pass.Exercise.First().ExerciseId;
        var controller = WithUser(new SessionController(context), userId);
        SaveSessionRequest Request(bool complete, int reps) => new()
        {
            SessionKey = Guid.Parse("5b0f3c1e-0000-0000-0000-000000000001"), PassId = pass.PassId,
            Date = new DateOnly(2026, 7, 15), StartedAtUtc = new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero),
            CompleteSession = complete,
            Exercises = [new SaveSessionExerciseDto { ExerciseId = exerciseId, Weight = 50,
                Sets = [new SaveSessionSetDto { SetIndex = 0, Reps = reps, IsDone = true, Weight = 50 }] }],
        };

        await controller.SaveSession(Request(complete: true, reps: 8));
        var result = await controller.SaveSession(Request(complete: false, reps: 3));

        Assert.IsType<OkResult>(result);
        Assert.Equal(8, (await context.SetLog.SingleAsync(TestContext.Current.CancellationToken)).Reps);
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

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(10000, 10)]
    [InlineData(50, 501)]
    public async Task Upsert_rejects_out_of_range_weight_or_reps(double weight, int reps)
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var split = CreateSplit(userId);
        context.Add(split);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var pass = split.Pass.Single();
        var controller = WithUser(new SessionController(context), userId);

        var result = await controller.UpsertExercise(new UpsertExerciseRequest
        {
            SessionKey = Guid.NewGuid(), PassId = pass.PassId, Date = new DateOnly(2026, 7, 15),
            StartedAtUtc = DateTimeOffset.UtcNow, ExerciseId = pass.Exercise.First().ExerciseId,
            Weight = (decimal)weight,
            Sets = [new SaveSessionSetDto { SetIndex = 0, Reps = reps, IsDone = true, Weight = (decimal)weight }],
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, await context.Session.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Saving_the_same_exercise_twice_replaces_sets_without_duplicating_logs()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var split = CreateSplit(userId);
        context.Add(split);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var pass = split.Pass.Single();
        var controller = WithUser(new SessionController(context), userId);
        var request = new UpsertExerciseRequest
        {
            SessionKey = Guid.NewGuid(), PassId = pass.PassId, Date = new DateOnly(2026, 7, 15),
            StartedAtUtc = DateTimeOffset.UtcNow, ExerciseId = pass.Exercise.First().ExerciseId, Weight = 60,
            Sets = [new SaveSessionSetDto { SetIndex = 0, Reps = 8, IsDone = true, Weight = 60 }],
        };

        Assert.IsType<OkResult>(await controller.UpsertExercise(request));
        request.Sets = [new SaveSessionSetDto { SetIndex = 0, Reps = 9, IsDone = true, Weight = 62.5m }];
        Assert.IsType<OkResult>(await controller.UpsertExercise(request));

        Assert.Equal(1, await context.Session.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, await context.ExerciseLog.CountAsync(TestContext.Current.CancellationToken));
        var set = await context.SetLog.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(9, set.Reps);
    }

    [Fact]
    public async Task SaveSession_with_several_exercises_replaces_sets_and_updates_default_weight()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var split = CreateSplit(userId);
        context.Add(split);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var pass = split.Pass.Single();
        var ids = pass.Exercise.Select(e => e.ExerciseId).ToList();
        var controller = WithUser(new SessionController(context), userId);
        SaveSessionRequest Request(int sets, decimal weight) => new()
        {
            SessionKey = Guid.Parse("11111111-1111-1111-1111-111111111111"), PassId = pass.PassId,
            Date = new DateOnly(2026, 7, 15), StartedAtUtc = DateTimeOffset.UtcNow,
            Exercises = ids.Select(id => new SaveSessionExerciseDto
            {
                ExerciseId = id, Weight = weight,
                Sets = Enumerable.Range(0, sets).Select(i => new SaveSessionSetDto { SetIndex = i, Reps = 8, IsDone = true, Weight = weight }).ToList(),
            }).ToList(),
        };

        Assert.IsType<OkResult>(await controller.SaveSession(Request(3, 50)));
        Assert.IsType<OkResult>(await controller.SaveSession(Request(2, 55)));

        Assert.Equal(2, await context.ExerciseLog.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(4, await context.SetLog.CountAsync(TestContext.Current.CancellationToken));
        Assert.All(context.Exercise.Where(e => ids.Contains(e.ExerciseId)), e => Assert.Equal(55m, e.DefaultWeight));
    }

    private static TyngdContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TyngdContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TyngdContext(options);
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
