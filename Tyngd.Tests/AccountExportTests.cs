using System.Security.Claims;
using System.Text.Json;
using Tyngd.Server.Controllers;
using Tyngd.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Tyngd.Tests;

public class AccountExportTests
{
    [Fact]
    public async Task Export_contains_the_users_own_data_and_nobody_elses()
    {
        await using var context = new TyngdContext(new DbContextOptionsBuilder<TyngdContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var me = Guid.NewGuid();
        var other = Guid.NewGuid();
        context.Add(new UserInfo { UserId = me, UserName = "bob", Email = "bob@example.com", Roles = "User", PasswordHash = "x" });
        context.Add(new UserInfo { UserId = other, UserName = "eve", Email = "eve@example.com", Roles = "User", PasswordHash = "x" });
        context.Add(SplitWithSession(me, "Min split", 82.5m));
        context.Add(SplitWithSession(other, "Annans split", 140m));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var controller = new AccountController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", me.ToString())], "test")) },
            },
        };

        var file = Assert.IsType<FileContentResult>(await controller.Export());
        var json = System.Text.Encoding.UTF8.GetString(file.FileContents);

        Assert.Equal("application/json", file.ContentType);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("bob", doc.RootElement.GetProperty("account").GetProperty("userName").GetString());
        Assert.Contains("Min split", json);
        Assert.Contains("82.5", json);
        Assert.DoesNotContain("eve", json);
        Assert.DoesNotContain("Annans split", json);
    }

    private static Split SplitWithSession(Guid userId, string name, decimal weight)
    {
        var exercise = new Exercise { ExerciseName = "Knäböj", Visability = true, Ordning = 1 };
        var pass = new Pass { PassName = "A", Visability = 1, Ordning = 1, Exercise = [exercise] };
        pass.Session.Add(new Session
        {
            SessionKey = Guid.NewGuid(), Date = new DateOnly(2026, 10, 20), StartedAtUtc = DateTimeOffset.UtcNow,
            ExerciseLog = [new ExerciseLog { Exercise = exercise, NoteText = "", SetLog = [new SetLog { SetIndex = 0, Reps = 5, IsDone = true, Weight = weight }] }],
        });
        return new Split { UserId = userId, SplitName = name, Visability = true, Pass = [pass] };
    }
}
