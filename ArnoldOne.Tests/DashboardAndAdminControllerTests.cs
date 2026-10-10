using System.Security.Claims;
using ArnoldOne.Server.Controllers;
using ArnoldOne.Shared.DTOs;
using ArnoldOne.Shared.Users;
using DbArnold.DbArnold.models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using DbUserInfo = DbArnold.DbArnold.models.UserInfo;

namespace ArnoldOne.Tests;

public class DashboardAndAdminControllerTests
{
    [Fact]
    public async Task Dashboard_only_counts_current_users_completed_sessions()
    {
        await using var context = CreateContext();
        var user = NewUser(AppRoles.User);
        var otherUser = NewUser(AppRoles.User);
        var split = AddSplit(user, "Min split");
        var otherSplit = AddSplit(otherUser, "Annan split");
        context.AddRange(user, otherUser);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.Session.AddRange(
            NewSession(split.Pass.Single(), DateTimeOffset.UtcNow.AddDays(-2), completed: true),
            NewSession(split.Pass.Single(), DateTimeOffset.UtcNow.AddDays(-20), completed: true),
            NewSession(split.Pass.Single(), DateTimeOffset.UtcNow, completed: false),
            NewSession(otherSplit.Pass.Single(), DateTimeOffset.UtcNow, completed: true));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await WithUser(new DashboardController(context), user.UserId).Get();
        var dto = Assert.IsType<OkObjectResult>(result.Result).Value as UserDashboardDto;

        Assert.NotNull(dto);
        Assert.Equal(1, dto.CompletedSessionsLast7Days);
        Assert.Equal(2, dto.CompletedSessionsLast30Days);
        Assert.Equal(2, dto.TotalCompletedSessions);
        Assert.Single(dto.ActiveSplits);
        Assert.Equal("Min split", dto.LastCompletedSession!.SplitName);
    }

    [Theory]
    [InlineData(AppRoles.User, false)]
    [InlineData($"{AppRoles.User},{AppRoles.Admin}", true)]
    public async Task Admin_filter_rechecks_role_from_database_even_if_token_says_admin(string dbRoles, bool allowed)
    {
        await using var context = CreateContext();
        var user = NewUser(dbRoles);
        context.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var controller = WithUser(new AdminController(context), user.UserId, AppRoles.Admin);
        var executing = new ActionExecutingContext(
            new ActionContext(controller.HttpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()),
            [], new Dictionary<string, object?>(), controller);
        var nextCalled = false;

        await new AdminOnlyFilter(context).OnActionExecutionAsync(executing, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(executing, [], controller));
        });

        Assert.Equal(allowed, nextCalled);
        Assert.Equal(allowed, executing.Result is null);
        if (!allowed)
            Assert.IsType<ForbidResult>(executing.Result);
    }

    [Theory]
    [InlineData(typeof(AdminController))]
    [InlineData(typeof(AdminTemplatesController))]
    public void Admin_controllers_are_covered_by_the_admin_filter(Type controller) =>
        Assert.Contains(
            controller.GetCustomAttributes(inherit: true).OfType<TypeFilterAttribute>(),
            filter => filter.ImplementationType == typeof(AdminOnlyFilter));

    [Fact]
    public async Task Admin_cannot_remove_own_admin_role()
    {
        await using var context = CreateContext();
        var admin = NewUser($"{AppRoles.User},{AppRoles.Admin}");
        context.Add(admin);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var controller = WithUser(new AdminController(context), admin.UserId, AppRoles.Admin);

        var result = await controller.SetAdminRole(admin.UserId, new SetUserAdminRequest { IsAdmin = false });

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains(AppRoles.Admin, admin.Roles);
    }

    [Fact]
    public async Task Only_admin_can_demote_a_regular_user_as_a_harmless_no_op()
    {
        await using var context = CreateContext();
        var admin = NewUser($"{AppRoles.User},{AppRoles.Admin}");
        var other = NewUser(AppRoles.User);
        context.AddRange(admin, other);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var controller = WithUser(new AdminController(context), admin.UserId, AppRoles.Admin);

        var result = await controller.SetAdminRole(other.UserId, new SetUserAdminRequest { IsAdmin = false });

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Admin_can_demote_another_admin_but_keeps_own_role()
    {
        await using var context = CreateContext();
        var admin = NewUser($"{AppRoles.User},{AppRoles.Admin}");
        var other = NewUser($"{AppRoles.User},{AppRoles.Admin}");
        context.AddRange(admin, other);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var controller = WithUser(new AdminController(context), admin.UserId, AppRoles.Admin);

        var result = await controller.SetAdminRole(other.UserId, new SetUserAdminRequest { IsAdmin = false });

        Assert.IsType<NoContentResult>(result);
        Assert.DoesNotContain(AppRoles.Admin, other.Roles);
        Assert.Contains(AppRoles.Admin, admin.Roles);
    }

    [Fact]
    public async Task Template_update_preserves_ids_and_archives_removed_exercise()
    {
        await using var context = CreateContext();
        var admin = NewUser($"{AppRoles.User},{AppRoles.Admin}");
        var split = new SplitTemplate
        {
            SplitTemplateName = "Styrka",
            Visability = true,
            PassTemplate =
            [
                new PassTemplate
                {
                    PassTemplateName = "Pass A",
                    Visability = true,
                    Ordning = 1,
                },
            ],
        };
        context.AddRange(admin, split);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var pass = split.PassTemplate.Single();
        var kept = NewExerciseTemplate(pass.PassTemplateId, "Bänkpress", 1);
        var removed = NewExerciseTemplate(pass.PassTemplateId, "Flyes", 2);
        context.AddRange(kept, removed);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var controller = WithUser(new AdminTemplatesController(context), admin.UserId, AppRoles.Admin);

        var result = await controller.Update(split.SplitTemplateId, new UpsertAdminSplitTemplateRequest
        {
            SplitTemplateName = "Ny styrka",
            Passes =
            [
                new UpsertAdminPassTemplateRequest
                {
                    PassTemplateId = pass.PassTemplateId,
                    PassTemplateName = "Pass A uppdaterat",
                    Exercises =
                    [
                        new UpsertAdminExerciseTemplateRequest
                        {
                            ExerciseTemplateId = kept.ExerciseTemplateId,
                            ExerciseTemplateName = "Bänkpress uppdaterad",
                        },
                        new UpsertAdminExerciseTemplateRequest { ExerciseTemplateName = "Lutande press" },
                    ],
                },
            ],
        });

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("Bänkpress uppdaterad", kept.ExerciseTemplateName);
        Assert.True(kept.Visability);
        Assert.False(removed.Visability);
        var added = await context.ExerciseTemplate.SingleAsync(
            exercise => exercise.ExerciseTemplateName == "Lutande press",
            TestContext.Current.CancellationToken);
        Assert.NotEqual(kept.ExerciseTemplateId, added.ExerciseTemplateId);
    }

    private static ArnoldContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ArnoldContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            // The in-memory provider has no transactions; let the real transaction calls through.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ArnoldContext(options);
    }

    private static DbUserInfo NewUser(string roles) => new()
    {
        UserId = Guid.NewGuid(),
        UserName = $"user-{Guid.NewGuid():N}"[..20],
        Email = $"{Guid.NewGuid():N}@example.com",
        CreatedAt = DateTime.UtcNow,
        Roles = roles,
        PasswordHash = "test",
    };

    private static Split AddSplit(DbUserInfo user, string name)
    {
        var split = new Split
        {
            SplitName = name,
            UserId = user.UserId,
            Visability = true,
            Pass = [new Pass { PassName = "Pass", Visability = 1, Ordning = 1 }],
        };
        user.Split.Add(split);
        return split;
    }

    private static Session NewSession(Pass pass, DateTimeOffset started, bool completed) => new()
    {
        PassId = pass.PassId,
        SessionKey = Guid.NewGuid(),
        Date = DateOnly.FromDateTime(started.UtcDateTime),
        StartedAtUtc = started,
        CompletedAtUtc = completed ? started.AddHours(1) : null,
    };

    private static ExerciseTemplate NewExerciseTemplate(int passId, string name, int order) => new()
    {
        PassTemplateId = passId,
        ExerciseTemplateName = name,
        Visability = true,
        Ordning = order,
        DefaultSets = 3,
        DefaultRepsMin = 8,
        DefaultRepsMax = 12,
        DefaultWeight = 0,
    };

    private static T WithUser<T>(T controller, Guid userId, params string[] roles) where T : ControllerBase
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")),
            },
        };
        return controller;
    }
}
