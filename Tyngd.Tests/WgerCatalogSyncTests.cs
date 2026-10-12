using System.Net;
using System.Text;
using System.Text.Json;
using Tyngd.Server.Catalog;
using Tyngd.Shared.Catalog;
using Tyngd.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Tyngd.Tests;

public class WgerCatalogSyncTests
{
    private static readonly Guid Squat = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid Row = Guid.Parse("00000000-0000-0000-0000-000000000002");

    [Fact]
    public async Task New_exercises_get_seed_translation_when_source_matches()
    {
        await using var context = CreateContext();
        var seed = new CatalogSeed([new CatalogSeed.SeedEntry(Squat, CatalogText.SourceHash("Squat", "<p>Go down</p>"), "Knäböj", "<p>Gå ner</p>")]);

        var result = await Sync(context, seed, Exercise(Squat, "Squat", "<p>Go down</p>"), Exercise(Row, "Row", "<p>Pull</p>"));

        Assert.Equal(2, result.Added);
        Assert.Equal(1, result.TranslatedFromSeed);
        var squat = await context.ExerciseCatalog.SingleAsync(row => row.WgerUuid == Squat, TestContext.Current.CancellationToken);
        Assert.Equal("Knäböj", squat.NameSv);
        Assert.Equal((byte)CatalogTranslationStatus.Auto, squat.TranslationStatus);
        Assert.Equal("Quadriceps", squat.Muscles);
        // Texten har översättningens licens (CC0), inte övningens (CC-BY-SA 4); bilden har sin egen.
        Assert.Equal("CC0", squat.License);
        Assert.Equal("author", squat.LicenseAuthor);
        Assert.Equal("CC-BY-SA 3", squat.ImageLicense);
        Assert.Equal("Photographer", squat.ImageAuthor);
        Assert.Equal("Glutes", squat.MusclesSecondary);
        var row = await context.ExerciseCatalog.SingleAsync(item => item.WgerUuid == Row, TestContext.Current.CancellationToken);
        Assert.Null(row.NameSv);
        Assert.Equal((byte)CatalogTranslationStatus.Saknas, row.TranslationStatus);
    }

    [Fact]
    public async Task Changed_source_clears_auto_translation_but_only_flags_reviewed_one()
    {
        await using var context = CreateContext();
        var seed = new CatalogSeed(
        [
            new CatalogSeed.SeedEntry(Squat, CatalogText.SourceHash("Squat", "<p>Go down</p>"), "Knäböj", "<p>Gå ner</p>"),
            new CatalogSeed.SeedEntry(Row, CatalogText.SourceHash("Row", "<p>Pull</p>"), "Rodd", "<p>Dra</p>"),
        ]);
        await Sync(context, seed, Exercise(Squat, "Squat", "<p>Go down</p>"), Exercise(Row, "Row", "<p>Pull</p>"));
        var reviewed = await context.ExerciseCatalog.SingleAsync(item => item.WgerUuid == Row, TestContext.Current.CancellationToken);
        reviewed.NameSv = "Rodd (granskad)";
        reviewed.TranslationStatus = (byte)CatalogTranslationStatus.Granskad;
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Sync(context, seed, Exercise(Squat, "Squat", "<p>Go deeper</p>"), Exercise(Row, "Row", "<p>Pull hard</p>"));

        Assert.Equal(2, result.TextChanged);
        Assert.Equal(1, result.NeedsReview);
        var squat = await context.ExerciseCatalog.SingleAsync(item => item.WgerUuid == Squat, TestContext.Current.CancellationToken);
        Assert.Null(squat.NameSv);
        Assert.Equal((byte)CatalogTranslationStatus.Saknas, squat.TranslationStatus);
        Assert.Equal("<p>Go deeper</p>", squat.DescriptionEn);
        Assert.Equal("Rodd (granskad)", reviewed.NameSv);
        Assert.Equal((byte)CatalogTranslationStatus.BehoverGranskning, reviewed.TranslationStatus);

        var again = await Sync(context, seed, Exercise(Squat, "Squat", "<p>Go deeper</p>"), Exercise(Row, "Row", "<p>Pull hard</p>"));
        Assert.Equal(0, again.TextChanged);
    }

    [Fact]
    public async Task Missing_exercise_is_deactivated_and_comes_back()
    {
        await using var context = CreateContext();
        var seed = new CatalogSeed([]);
        await Sync(context, seed, Exercise(Squat, "Squat", ""), Exercise(Row, "Row", ""));

        var gone = await Sync(context, seed, Exercise(Squat, "Squat", ""));
        Assert.Equal(1, gone.Deactivated);
        Assert.False((await context.ExerciseCatalog.SingleAsync(item => item.WgerUuid == Row, TestContext.Current.CancellationToken)).IsActive);

        var back = await Sync(context, seed, Exercise(Squat, "Squat", ""), Exercise(Row, "Row", ""));
        Assert.Equal(1, back.Reactivated);
    }

    [Fact]
    public async Task Suspiciously_small_response_aborts_without_changes()
    {
        await using var context = CreateContext();
        var seed = new CatalogSeed([]);
        await Sync(context, seed, Exercise(Squat, "Squat", ""), Exercise(Row, "Row", ""), Exercise(Guid.NewGuid(), "Plank", ""));

        await Assert.ThrowsAsync<InvalidOperationException>(() => Sync(context, seed, Exercise(Squat, "Squat", "")));
        Assert.Equal(3, await context.ExerciseCatalog.CountAsync(item => item.IsActive, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Plain_text_keeps_paragraphs_and_lists_and_drops_tags()
    {
        var text = CatalogText.ToPlainText("<p>Stand &amp; brace.</p>\n<ol><li>Down</li><li>Up <script>x</script></li></ol>");

        Assert.Equal("Stand & brace.\n\n• Down\n• Up x", text);
    }

    [Fact]
    public void Embedded_seed_matches_hashes_computed_by_the_app()
    {
        var seed = CatalogSeed.LoadEmbedded();
        // Rå text från wger 2026-10-10. Seed-filen räknades fram i Python; appen måste få samma hash.
        var hash = CatalogText.SourceHash("2 Handed Kettlebell Swing", "<p>Two Handed Russian Style Kettlebell swing</p>\n");

        Assert.Equal(920, seed.Count);
        Assert.Equal("478f24d51a0d0ba5b7f136852242cf558d202f36149a3df792d97123a9d421ba", hash);
        Assert.NotNull(seed.Find(Guid.Parse("1b020b3a-3732-4c7e-92fd-a0cec90ed69b"), hash));
    }

    private static Task<CatalogSyncResultDto> Sync(TyngdContext context, CatalogSeed seed, params object[] exercises)
    {
        var json = JsonSerializer.Serialize(new { count = exercises.Length, next = (string?)null, results = exercises });
        var http = new HttpClient(new StaticHandler(json)) { BaseAddress = new Uri("https://wger.test/") };
        return new WgerCatalogSync(http, context, seed).SyncAsync(TestContext.Current.CancellationToken);
    }

    private static object Exercise(Guid uuid, string name, string description) => new
    {
        uuid,
        category = new { name = "Legs" },
        muscles = new[] { new { name = "Quadriceps femoris", name_en = "Quadriceps" } },
        muscles_secondary = new[] { new { name = "Gluteus maximus", name_en = "Glutes" } },
        equipment = new[] { new { name = "Barbell" } },
        images = new[] { new { image = "https://wger.test/media/a.png", is_main = true, license = 1, license_author = "Photographer" } },
        license = new { short_name = "CC-BY-SA 4" },
        license_author = "wger",
        translations = new[]
        {
            new { name, description, language = 2, license = 3, license_author = "author", aliases = new[] { new { alias = name + " alias" } } },
        },
    };

    private static TyngdContext CreateContext() =>
        new(new DbContextOptionsBuilder<TyngdContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class StaticHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }
}
