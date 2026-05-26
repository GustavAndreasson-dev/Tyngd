using ArnoldOne.Shared.TrainingPrograms;
using DbArnold.DbArnold.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DbExercise = DbArnold.DbArnold.models.Exercise;

namespace ArnoldOne.Server.Controllers;

[Route("api/[controller]")]
public class UserSplitController : ApiControllerBase
{
    private readonly ArnoldContext _context;

    public UserSplitController(ArnoldContext context)
    {
        _context = context;
    }

    /// <summary>Returnerar SplitTemplateId för alla aktiva splits för inloggad användare.</summary>
    [HttpGet]
    public async Task<ActionResult<List<int>>> GetActiveSplits()
    {
        var userId = CurrentUserId;
        var splitIds = await _context.Split
            .Where(s => s.UserId == userId && s.Visability)
            .Select(s => s.SplitId)
            .ToListAsync();

        return Ok(splitIds);
    }

    /// <summary>Aktiverar en split för inloggad användare genom att kopiera från SplitTemplate.</summary>
    [HttpPost("{splitTemplateId}")]
    public async Task<IActionResult> ActivateSplit(int splitTemplateId)
    {
        var userId = CurrentUserId;

        // Kolla om redan aktiverad
        var exists = await _context.Split
            .AnyAsync(s => s.UserId == userId && s.SplitId == splitTemplateId);

        if (exists)
            return Ok();

        // Hämta mallen med pass och övningar
        var template = await _context.SplitTemplate
            .Include(s => s.PassTemplate)
            .FirstOrDefaultAsync(s => s.SplitTemplateId == splitTemplateId);

        if (template is null)
            return NotFound();

        var exercises = await _context.ExerciseTemplate
            .Where(e => template.PassTemplate.Select(p => p.PassTemplateId).Contains(e.PassTemplateId))
            .ToListAsync();

        // Skapa Split för användaren
        var split = new Split
        {
            SplitId   = splitTemplateId,
            SplitName = template.SplitTemplateName,
            UserId    = userId,
            Visability = true,
            Pass = template.PassTemplate.OrderBy(p => p.Ordning).Select(p => new Pass
            {
                PassName   = p.PassTemplateName,
                SplitId    = splitTemplateId,
                Visability = 1,
                Ordning    = p.Ordning,
                Exercise   = exercises
                    .Where(e => e.PassTemplateId == p.PassTemplateId)
                    .OrderBy(e => e.Ordning)
                    .Select(e => new DbExercise
                    {
                        ExerciseName   = e.ExerciseTemplateName,
                        PassId         = 0, // sätts av EF
                        DefaultSets    = e.DefaultSets,
                        DefautlRepsMin = e.DefaultRepsMin,
                        DefaultRepsMax = e.DefaultRepsMax,
                        DefaultWeight  = e.DefaultWeight,
                        Ordning        = e.Ordning,
                    }).ToList(),
            }).ToList(),
        };

        _context.Split.Add(split);
        await _context.SaveChangesAsync();

        return Ok();
    }

    /// <summary>Avaktiverar en split för inloggad användare.</summary>
    [HttpDelete("{splitTemplateId}")]
    public async Task<IActionResult> DeactivateSplit(int splitTemplateId)
    {
        var userId = CurrentUserId;

        var split = await _context.Split
            .Include(s => s.Pass)
                .ThenInclude(p => p.Exercise)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SplitId == splitTemplateId);

        if (split is null)
            return NotFound();

        _context.Split.Remove(split);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
