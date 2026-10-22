using Tyngd.Server.Controllers;
using Tyngd.Shared.Catalog;
using Tyngd.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Tyngd.Tests;

public class CatalogImageTests
{
    [Fact]
    public async Task Clients_get_the_servers_image_path_and_only_wger_images_are_fetched()
    {
        await using var context = new TyngdContext(new DbContextOptionsBuilder<TyngdContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var wger = new ExerciseCatalog { WgerUuid = Guid.NewGuid(), NameEn = "Bench", SourceHash = "x", IsActive = true, ImageUrl = "https://wger.de/media/a.png" };
        var foreign = new ExerciseCatalog { WgerUuid = Guid.NewGuid(), NameEn = "Row", SourceHash = "x", IsActive = true, ImageUrl = "https://example.com/a.png" };
        context.AddRange(wger, foreign);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var controller = new CatalogController(context) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

        var all = Assert.IsType<List<CatalogExerciseDto>>(Assert.IsType<OkObjectResult>((await controller.GetAll()).Result).Value);
        Assert.Equal($"api/catalog/{wger.CatalogExerciseId}/image", all.Single(e => e.NameEn == "Bench").ImageUrl);

        // Avvisas innan något hämtas, så ingen HTTP-klient behövs.
        Assert.IsType<NotFoundResult>(await controller.Image(foreign.CatalogExerciseId, null!));
        Assert.IsType<NotFoundResult>(await controller.Image(12345, null!));
    }
}
