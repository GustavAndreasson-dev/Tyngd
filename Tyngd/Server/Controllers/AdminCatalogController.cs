using Tyngd.Server.Catalog;
using Tyngd.Shared.Catalog;
using Tyngd.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Tyngd.Server.Controllers;

[Route("api/admin/catalog")]
public sealed class AdminCatalogController(TyngdContext context) : AdminApiControllerBase
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

    /// <summary>Hela katalogen för granskning. Beskrivningar skickas som ren text, så admin redigerar text och inte HTML.</summary>
    [HttpGet]
    public async Task<ActionResult<List<AdminCatalogExerciseDto>>> GetAll()
    {
        var rows = await context.ExerciseCatalog.AsNoTracking()
            .OrderBy(row => row.Category).ThenBy(row => row.NameEn)
            .ToListAsync();
        return Ok(rows.Select(row => new AdminCatalogExerciseDto
        {
            CatalogExerciseId = row.CatalogExerciseId,
            NameEn = row.NameEn,
            NameSv = row.NameSv,
            DescriptionEn = CatalogText.ToPlainText(row.DescriptionEn),
            DescriptionSv = row.DescriptionSv is null ? null : CatalogText.ToPlainText(row.DescriptionSv),
            TranslationStatus = (CatalogTranslationStatus)row.TranslationStatus,
            Category = CatalogNames.Sv(row.Category),
            IsActive = row.IsActive,
        }).ToList());
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
