using ArnoldOne.Server.Catalog;
using ArnoldOne.Shared.DTOs;
using DbArnold.DbArnold.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DbExercise = DbArnold.DbArnold.models.Exercise;

namespace ArnoldOne.Server.Controllers;

/// <summary>
/// Lösa pass: ett pass utan split, som sparas som ett eget pass i en dold split per användare
/// (<see cref="Split.IsFreeSessions"/>). Sedan fungerar det som vilket pass som helst.
/// </summary>
[Route("api/free-session")]
public sealed class FreeSessionController(ArnoldContext context) : ApiControllerBase
{
    private const int MaxExercises = 30;
    private const string FreeSplitName = "Lösa pass";

    [HttpPost]
    public async Task<ActionResult<StartFreeSessionResponse>> Start(StartFreeSessionRequest request)
    {
        var passName = request.PassName.Trim();
        if (passName.Length is 0 or > 50)
            return BadRequest("Passnamnet måste vara 1–50 tecken.");
        if (request.Exercises.Count is 0 or > MaxExercises)
            return BadRequest($"Ett löst pass måste ha 1–{MaxExercises} övningar.");
        if (ValidateExercises(request.Exercises) is { } error)
            return BadRequest(error);
        if (!await CatalogLinks.AllExistAsync(context, request.Exercises.Select(e => e.CatalogExerciseId)))
            return BadRequest("En eller flera övningar pekar på en okänd katalogövning.");

        var split = await GetOrCreateFreeSplitAsync();
        var order = await context.Pass.Where(p => p.SplitId == split.SplitId).MaxAsync(p => (int?)p.Ordning) ?? 0;
        var weights = await LatestWeightsAsync(request.Exercises.Select(e => e.CatalogExerciseId));

        var pass = new Pass
        {
            SplitId = split.SplitId,
            PassName = passName,
            Visability = 1,
            Ordning = order + 1,
            Exercise = request.Exercises.Select((exercise, index) => NewExercise(exercise, index + 1, weights)).ToList(),
        };
        context.Pass.Add(pass);
        await context.SaveChangesAsync();

        return Ok(new StartFreeSessionResponse { SplitId = split.SplitId, PassId = pass.PassId });
    }

    /// <summary>Lägger till en övning i ett pågående löst pass. Returnerar övningens id.</summary>
    [HttpPost("{passId:int}/exercises")]
    public async Task<ActionResult<int>> AddExercise(int passId, FreeSessionExerciseDto request)
    {
        if (ValidateExercises([request]) is { } error)
            return BadRequest(error);

        var pass = await context.Pass
            .Include(p => p.Exercise)
            .FirstOrDefaultAsync(p => p.PassId == passId && p.Split.UserId == CurrentUserId && p.Split.IsFreeSessions);
        if (pass is null)
            return NotFound();
        if (pass.Exercise.Count(e => e.Visability) >= MaxExercises)
            return BadRequest($"Ett löst pass kan ha högst {MaxExercises} övningar.");
        if (!await CatalogLinks.AllExistAsync(context, [request.CatalogExerciseId]))
            return BadRequest("Okänd katalogövning.");

        var weights = await LatestWeightsAsync([request.CatalogExerciseId]);
        var exercise = NewExercise(request, pass.Exercise.Select(e => e.Ordning).DefaultIfEmpty(0).Max() + 1, weights);
        pass.Exercise.Add(exercise);
        await context.SaveChangesAsync();
        return Ok(exercise.ExerciseId);
    }

    /// <summary>
    /// Sparar ett löst pass som ett riktigt pass genom att flytta det till en egen split (befintlig eller ny).
    /// Passet behåller sina övningar och sessioner, så förra passets vikter följer med nästa gång.
    /// </summary>
    [HttpPost("{passId:int}/keep")]
    public async Task<ActionResult<StartFreeSessionResponse>> Keep(int passId, KeepFreeSessionRequest request)
    {
        var passName = request.PassName.Trim();
        if (passName.Length is 0 or > 50)
            return BadRequest("Passnamnet måste vara 1–50 tecken.");

        var userId = CurrentUserId;
        var pass = await context.Pass
            .FirstOrDefaultAsync(p => p.PassId == passId && p.Visability == 1 && p.Split.UserId == userId && p.Split.IsFreeSessions);
        if (pass is null)
            return NotFound();

        Split target;
        if (request.TargetSplitId is { } targetSplitId)
        {
            var existing = await context.Split.FirstOrDefaultAsync(s => s.SplitId == targetSplitId
                && s.UserId == userId && s.Visability && s.SplitTemplateId == null && !s.IsFreeSessions);
            if (existing is null)
                return BadRequest("Splitten finns inte eller går inte att ändra.");
            target = existing;
        }
        else
        {
            var splitName = request.NewSplitName?.Trim() ?? string.Empty;
            if (splitName.Length is 0 or > 50)
                return BadRequest("Splittens namn måste vara 1–50 tecken.");
            target = new Split { UserId = userId, SplitName = splitName, Visability = true };
            context.Split.Add(target);
            await context.SaveChangesAsync();
        }

        pass.SplitId = target.SplitId;
        pass.PassName = passName;
        pass.Ordning = (await context.Pass.Where(p => p.SplitId == target.SplitId && p.Visability == 1).MaxAsync(p => (int?)p.Ordning) ?? 0) + 1;
        await context.SaveChangesAsync();
        return Ok(new StartFreeSessionResponse { SplitId = target.SplitId, PassId = pass.PassId });
    }

    /// <summary>Slänger ett löst pass: passet döljs, men det som tränats finns kvar i historik och statistik.</summary>
    [HttpPost("{passId:int}/discard")]
    public async Task<IActionResult> Discard(int passId)
    {
        var pass = await context.Pass
            .FirstOrDefaultAsync(p => p.PassId == passId && p.Split.UserId == CurrentUserId && p.Split.IsFreeSessions);
        if (pass is null)
            return NotFound();

        pass.Visability = 0;
        await context.SaveChangesAsync();
        return NoContent();
    }

    private static string? ValidateExercises(IEnumerable<FreeSessionExerciseDto> exercises) =>
        exercises.Any(e => e.Name.Trim().Length is 0 or > 50) ? "Varje övning måste ha ett namn på 1–50 tecken." : null;

    private static DbExercise NewExercise(FreeSessionExerciseDto exercise, int order, IReadOnlyDictionary<int, decimal> weights) => new()
    {
        ExerciseName = exercise.Name.Trim(),
        CatalogExerciseId = exercise.CatalogExerciseId,
        DefaultSets = 3,
        DefautlRepsMin = 8,
        DefaultRepsMax = 12,
        DefaultWeight = exercise.CatalogExerciseId is { } id ? weights.GetValueOrDefault(id) : 0,
        Ordning = order,
        Visability = true,
    };

    /// <summary>Senast använda tyngsta vikt per katalogövning, från alla användarens pass.</summary>
    private async Task<Dictionary<int, decimal>> LatestWeightsAsync(IEnumerable<int?> catalogExerciseIds)
    {
        var ids = catalogExerciseIds.OfType<int>().Distinct().ToList();
        if (ids.Count == 0)
            return new();

        var rows = await context.ExerciseLog
            .Where(l => l.Exercise.CatalogExerciseId != null
                && ids.Contains(l.Exercise.CatalogExerciseId.Value)
                && l.Session.CompletedAtUtc != null
                && l.Session.Pass.Split.UserId == CurrentUserId)
            .Select(l => new
            {
                CatalogExerciseId = l.Exercise.CatalogExerciseId!.Value,
                l.Session.StartedAtUtc,
                Weight = l.SetLog.Where(s => s.IsDone).Max(s => (decimal?)s.Weight),
            })
            .ToListAsync();

        return rows.Where(row => row.Weight is not null)
            .GroupBy(row => row.CatalogExerciseId)
            .ToDictionary(group => group.Key, group => group.MaxBy(row => row.StartedAtUtc)!.Weight!.Value);
    }

    private async Task<Split> GetOrCreateFreeSplitAsync()
    {
        var userId = CurrentUserId;
        var split = await context.Split.FirstOrDefaultAsync(s => s.UserId == userId && s.IsFreeSessions);
        if (split is not null)
            return split;

        split = new Split { UserId = userId, SplitName = FreeSplitName, Visability = true, IsFreeSessions = true };
        context.Split.Add(split);
        try
        {
            await context.SaveChangesAsync();
            return split;
        }
        catch (DbUpdateException ex) when (DbExceptions.IsUniqueViolation(ex))
        {
            // Två lösa pass startades samtidigt: använd den split som hann skapas.
            context.Entry(split).State = EntityState.Detached;
            return await context.Split.FirstAsync(s => s.UserId == userId && s.IsFreeSessions);
        }
    }
}
