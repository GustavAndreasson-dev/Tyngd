using Tyngd.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Tyngd.Server.Controllers;

/// <summary>Inloggad användares favoritövningar (katalog-id:n).</summary>
[Route("api/favorites")]
public sealed class FavoritesController(TyngdContext context) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<int>>> GetAll() =>
        await context.UserFavoriteExercise
            .Where(favorite => favorite.UserId == CurrentUserId)
            .OrderByDescending(favorite => favorite.CreatedAtUtc)
            .Select(favorite => favorite.CatalogExerciseId)
            .ToListAsync();

    /// <summary>Lägger till en favorit. Att lägga till en befintlig favorit är ok.</summary>
    [HttpPut("{catalogExerciseId:int}")]
    public async Task<IActionResult> Add(int catalogExerciseId)
    {
        var userId = CurrentUserId;
        if (await context.UserFavoriteExercise.AnyAsync(f => f.UserId == userId && f.CatalogExerciseId == catalogExerciseId))
            return NoContent();
        if (!await context.ExerciseCatalog.AnyAsync(row => row.CatalogExerciseId == catalogExerciseId))
            return NotFound();

        context.UserFavoriteExercise.Add(new UserFavoriteExercise
        {
            UserId = userId,
            CatalogExerciseId = catalogExerciseId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbExceptions.IsUniqueViolation(ex))
        {
            // Dubbeltryck: favoriten finns redan.
        }
        return NoContent();
    }

    [HttpDelete("{catalogExerciseId:int}")]
    public async Task<IActionResult> Remove(int catalogExerciseId)
    {
        var favorite = await context.UserFavoriteExercise.FindAsync(CurrentUserId, catalogExerciseId);
        if (favorite is not null)
        {
            context.UserFavoriteExercise.Remove(favorite);
            await context.SaveChangesAsync();
        }
        return NoContent();
    }
}
