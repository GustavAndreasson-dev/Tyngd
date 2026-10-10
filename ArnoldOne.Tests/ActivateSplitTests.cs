using System.Security.Claims;
using ArnoldOne.Server.Controllers;
using DbArnold.DbArnold.models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArnoldOne.Tests;

public class ActivateSplitTests
{
    [Fact]
    public async Task Activation_copies_only_visible_passes_and_exercises()
    {
        await using var context = CreateContext();
        var templateId = await AddTemplateAsync(context, templateVisible: true);
        var userId = Guid.NewGuid();

        Assert.IsType<OkResult>(await Controller(context, userId).ActivateSplit(templateId));

        var split = await context.Split.Include(s => s.Pass).ThenInclude(p => p.Exercise)
            .SingleAsync(TestContext.Current.CancellationToken);
        var pass = Assert.Single(split.Pass);
        Assert.Equal("Synligt pass", pass.PassName);
        Assert.Equal(["Synlig övning"], pass.Exercise.Select(e => e.ExerciseName));
    }

    [Fact]
    public async Task Hidden_template_cannot_be_activated()
    {
        await using var context = CreateContext();
        var templateId = await AddTemplateAsync(context, templateVisible: false);

        var result = await Controller(context, Guid.NewGuid()).ActivateSplit(templateId);

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(0, await context.Split.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Activating_twice_keeps_one_split_and_reactivates_a_deactivated_one()
    {
        await using var context = CreateContext();
        var templateId = await AddTemplateAsync(context, templateVisible: true);
        var controller = Controller(context, Guid.NewGuid());

        await controller.ActivateSplit(templateId);
        await controller.ActivateSplit(templateId);
        await controller.DeactivateSplit(templateId);
        await controller.ActivateSplit(templateId);

        var split = await context.Split.SingleAsync(TestContext.Current.CancellationToken);
        Assert.True(split.Visability);
    }

    private static async Task<int> AddTemplateAsync(ArnoldContext context, bool templateVisible)
    {
        var template = new SplitTemplate
        {
            SplitTemplateName = "Mall",
            Visability = templateVisible,
            PassTemplate =
            [
                new PassTemplate { PassTemplateName = "Synligt pass", Visability = true, Ordning = 1 },
                new PassTemplate { PassTemplateName = "Dolt pass", Visability = false, Ordning = 2 },
            ],
        };
        context.SplitTemplate.Add(template);
        await context.SaveChangesAsync();

        var visiblePass = template.PassTemplate.First(p => p.Visability).PassTemplateId;
        var hiddenPass = template.PassTemplate.First(p => !p.Visability).PassTemplateId;
        context.ExerciseTemplate.AddRange(
            new ExerciseTemplate { PassTemplateId = visiblePass, ExerciseTemplateName = "Synlig övning", Visability = true, Ordning = 1 },
            new ExerciseTemplate { PassTemplateId = visiblePass, ExerciseTemplateName = "Dold övning", Visability = false, Ordning = 2 },
            new ExerciseTemplate { PassTemplateId = hiddenPass, ExerciseTemplateName = "Övning i dolt pass", Visability = true, Ordning = 1 });
        await context.SaveChangesAsync();
        return template.SplitTemplateId;
    }

    private static UserSplitController Controller(ArnoldContext context, Guid userId) =>
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
