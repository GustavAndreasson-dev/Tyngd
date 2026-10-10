using ArnoldOne.Server.Catalog;
using ArnoldOne.Shared.Catalog;
using DbArnold.DbArnold.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArnoldOne.Server.Controllers;

[Route("api/admin/catalog")]
public sealed class AdminCatalogController(ArnoldContext context) : AdminApiControllerBase
{
    [HttpPost("sync")]
    public async Task<ActionResult<CatalogSyncResultDto>> Sync([FromServices] WgerCatalogSync sync)
    {
        try
        {
            return Ok(await sync.SyncAsync(HttpContext.RequestAborted));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }
        catch (HttpRequestException exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, $"Kunde inte hämta från wger: {exception.Message}");
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminCatalogExerciseDto>>> GetAll([FromQuery] CatalogTranslationStatus? status = null)
    {
        var query = context.ExerciseCatalog.AsNoTracking();
        if (status is { } wanted)
            query = query.Where(row => row.TranslationStatus == (byte)wanted);

        var rows = await query
            .OrderBy(row => row.Category).ThenBy(row => row.NameEn)
            .Select(row => new AdminCatalogExerciseDto
            {
                CatalogExerciseId = row.CatalogExerciseId,
                NameEn = row.NameEn,
                NameSv = row.NameSv,
                DescriptionEn = row.DescriptionEn,
                DescriptionSv = row.DescriptionSv,
                TranslationStatus = (CatalogTranslationStatus)row.TranslationStatus,
                Category = row.Category,
                IsActive = row.IsActive,
            })
            .ToListAsync();
        return Ok(rows);
    }

    /// <summary>Sparar svensk text och markerar den som granskad mot nuvarande engelska text.</summary>
    [HttpPut("{catalogExerciseId:int}")]
    public async Task<IActionResult> UpdateTranslation(int catalogExerciseId, UpdateCatalogTranslationRequest request)
    {
        var nameSv = request.NameSv.Trim();
        if (nameSv.Length is 0 or > 50)
            return BadRequest("Det svenska namnet måste vara 1–50 tecken.");
        if (request.DescriptionSv.Length > 10_000)
            return BadRequest("Beskrivningen är för lång.");

        var row = await context.ExerciseCatalog.FindAsync(catalogExerciseId);
        if (row is null)
            return NotFound();

        row.NameSv = nameSv;
        row.DescriptionSv = request.DescriptionSv;
        row.TranslationStatus = (byte)CatalogTranslationStatus.Granskad;
        row.SourceHash = CatalogText.SourceHash(row.NameEn, row.DescriptionEn);
        await context.SaveChangesAsync();
        return NoContent();
    }
}
