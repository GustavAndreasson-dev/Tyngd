using System.Security.Claims;
using Tyngd.Server.Controllers;
using Tyngd.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Tyngd.Tests;

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

    [Fact]
    public async Task Activation_copies_the_catalog_link()
    {
        await using var context = CreateContext();
        var templateId = await AddTemplateAsync(context, templateVisible: true);
        var catalog = new ExerciseCatalog { WgerUuid = Guid.NewGuid(), NameEn = "Squat", SourceHash = "x", IsActive = true };
        context.ExerciseCatalog.Add(catalog);
        (await context.ExerciseTemplate.SingleAsync(e => e.ExerciseTemplateName == "Synlig övning", TestContext.Current.CancellationToken)).CatalogExercise = catalog;
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Controller(context, Guid.NewGuid()).ActivateSplit(templateId);

        var exercise = await context.Exercise.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(catalog.CatalogExerciseId, exercise.CatalogExerciseId);
    }

    [Fact]
    public async Task Custom_split_with_unknown_catalog_id_is_rejected()
    {
        await using var context = CreateContext();

        var result = await Controller(context, Guid.NewGuid()).CreateCustomSplit(new Tyngd.Shared.DTOs.CreateSplitRequest
        {
            SplitName = "Min split",
            Passes = [new() { PassName = "A", Exercises = [new() { ExerciseName = "Knäböj", CatalogExerciseId = 999 }] }],
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(0, await context.Split.CountAsync(TestContext.Current.CancellationToken));
    }

    private static async Task<int> AddTemplateAsync(TyngdContext context, bool templateVisible)
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

    private static UserSplitController Controller(TyngdContext context, Guid userId) =>
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

    private static TyngdContext CreateContext() =>
        new(new DbContextOptionsBuilder<TyngdContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
