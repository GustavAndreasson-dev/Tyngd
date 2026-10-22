using Tyngd.Server.Catalog;
using Tyngd.Shared.Catalog;
using Tyngd.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Tyngd.Server.Controllers;

// Katalogen är öppen data (wger, CC-BY-SA) och visas även för den som inte är inloggad.
[AllowAnonymous]
[Route("api/catalog")]
public sealed class CatalogController(TyngdContext context) : ApiControllerBase
{
    /// <summary>Alla aktiva katalogövningar, utan beskrivning. Klienten söker och filtrerar själv.</summary>
    [HttpGet]
    public async Task<ActionResult<List<CatalogExerciseDto>>> GetAll()
    {
        var rows = await context.ExerciseCatalog.AsNoTracking()
            .Where(row => row.IsActive)
            .Select(row => new { row.CatalogExerciseId, row.NameSv, row.NameEn, row.Aliases, row.Category, row.Muscles, row.MusclesSecondary, row.Equipment, row.ImageUrl })
            .ToListAsync();

        return Ok(rows
            .Select(row => new CatalogExerciseDto
            {
                CatalogExerciseId = row.CatalogExerciseId,
                Name = string.IsNullOrWhiteSpace(row.NameSv) ? row.NameEn : row.NameSv,
                NameEn = row.NameEn,
                Aliases = row.Aliases.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(),
                Category = CatalogNames.Sv(row.Category),
                Muscles = CatalogNames.SvList(row.Muscles + "," + row.MusclesSecondary),
                PrimaryMuscles = Muscles.ParseKeys(row.Muscles),
                SecondaryMuscles = Muscles.ParseKeys(row.MusclesSecondary),
                Equipment = CatalogNames.SvList(row.Equipment),
                ImageUrl = ImagePath(row.CatalogExerciseId, row.ImageUrl),
            })
            .OrderBy(row => row.Name, StringComparer.Create(new System.Globalization.CultureInfo("sv-SE"), true))
            .ToList());
    }

    /// <summary>En katalogövning med beskrivning som ren text, licens och upphovsperson.</summary>
    [HttpGet("{catalogExerciseId:int}")]
    public async Task<ActionResult<CatalogExerciseDetailDto>> Get(int catalogExerciseId)
    {
        var row = await context.ExerciseCatalog.AsNoTracking()
            .SingleOrDefaultAsync(item => item.CatalogExerciseId == catalogExerciseId);
        if (row is null)
            return NotFound();

        var hasSwedish = !string.IsNullOrWhiteSpace(row.NameSv);
        return Ok(new CatalogExerciseDetailDto
        {
            CatalogExerciseId = row.CatalogExerciseId,
            Name = hasSwedish ? row.NameSv! : row.NameEn,
            NameEn = row.NameEn,
            Description = CatalogText.ToPlainText(hasSwedish && !string.IsNullOrWhiteSpace(row.DescriptionSv) ? row.DescriptionSv : row.DescriptionEn),
            Category = CatalogNames.Sv(row.Category),
            Muscles = CatalogNames.SvList(row.Muscles + "," + row.MusclesSecondary),
            PrimaryMuscles = Muscles.ParseKeys(row.Muscles),
            SecondaryMuscles = Muscles.ParseKeys(row.MusclesSecondary),
            Equipment = CatalogNames.SvList(row.Equipment),
            ImageUrl = ImagePath(row.CatalogExerciseId, row.ImageUrl),
            ImageAuthor = row.ImageAuthor,
            ImageLicense = row.ImageLicense,
            License = row.License,
            LicenseAuthor = row.LicenseAuthor,
            WgerUuid = row.WgerUuid,
        });
    }

    /// <summary>
    /// Övningsbilden via den här servern, så att besökarens webbläsare aldrig kontaktar wger.de (och inte lämnar sin
    /// IP-adress där). Bara adresser ur katalogen hämtas, aldrig en adress som klienten skickar.
    /// </summary>
    // shortcut: ingen servercache; varje webbläsare som inte har bilden cachad ger en hämtning från wger. Lägg bilderna i blob/disk vid synk om trafiken växer.
    [HttpGet("{catalogExerciseId:int}/image")]
    public async Task<IActionResult> Image(int catalogExerciseId, [FromServices] IHttpClientFactory httpClientFactory)
    {
        var url = await context.ExerciseCatalog.AsNoTracking()
            .Where(row => row.CatalogExerciseId == catalogExerciseId)
            .Select(row => row.ImageUrl)
            .SingleOrDefaultAsync();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || uri.Host != "wger.de")
            return NotFound();

        using var response = await httpClientFactory.CreateClient().GetAsync(uri, HttpContext.RequestAborted);
        var contentType = response.Content.Headers.ContentType?.MediaType;
        if (!response.IsSuccessStatusCode || contentType?.StartsWith("image/") != true)
            return NotFound();

        // Bara lyckade svar cachas, så att en bild som läggs till vid nästa synk syns direkt.
        Response.Headers.CacheControl = "public,max-age=604800";
        return File(await response.Content.ReadAsByteArrayAsync(HttpContext.RequestAborted), contentType);
    }

    private static string? ImagePath(int catalogExerciseId, string? imageUrl) =>
        imageUrl is null ? null : $"api/catalog/{catalogExerciseId}/image";
}
