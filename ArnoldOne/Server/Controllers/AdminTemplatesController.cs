using ArnoldOne.Shared.DTOs;
using DbArnold.DbArnold.models;
using ArnoldOne.Server.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArnoldOne.Server.Controllers;

[Route("api/admin/templates")]
public sealed class AdminTemplatesController : AdminApiControllerBase
{
    private readonly ArnoldContext _context;

    public AdminTemplatesController(ArnoldContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<List<AdminSplitTemplateDto>>> GetAll([FromQuery] bool includeArchived = true)
    {
        var splits = await _context.SplitTemplate
            .Where(template => includeArchived || template.Visability)
            .OrderBy(template => template.SplitTemplateName)
            .AsNoTracking()
            .ToListAsync();
        var splitIds = splits.Select(template => template.SplitTemplateId).ToList();
        var passes = await _context.PassTemplate
            .Where(pass => splitIds.Contains(pass.SplitTemplateID) && (includeArchived || pass.Visability))
            .OrderBy(pass => pass.Ordning)
            .AsNoTracking()
            .ToListAsync();
        var passIds = passes.Select(pass => pass.PassTemplateId).ToList();
        var exercises = await _context.ExerciseTemplate
            .Where(exercise => passIds.Contains(exercise.PassTemplateId) && (includeArchived || exercise.Visability))
            .OrderBy(exercise => exercise.Ordning)
            .AsNoTracking()
            .ToListAsync();

        return Ok(splits.Select(split => MapTemplate(split, passes, exercises)).ToList());
    }

    [HttpGet("{templateId:int}")]
    public async Task<ActionResult<AdminSplitTemplateDto>> Get(int templateId)
    {
        var split = await _context.SplitTemplate.AsNoTracking()
            .SingleOrDefaultAsync(template => template.SplitTemplateId == templateId);
        if (split is null)
            return NotFound();

        var passes = await _context.PassTemplate.AsNoTracking()
            .Where(pass => pass.SplitTemplateID == templateId)
            .OrderBy(pass => pass.Ordning)
            .ToListAsync();
        var passIds = passes.Select(pass => pass.PassTemplateId).ToList();
        var exercises = await _context.ExerciseTemplate.AsNoTracking()
            .Where(exercise => passIds.Contains(exercise.PassTemplateId))
            .OrderBy(exercise => exercise.Ordning)
            .ToListAsync();

        return Ok(MapTemplate(split, passes, exercises));
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create(UpsertAdminSplitTemplateRequest request)
    {
        if ((Validate(request) ?? await ValidateCatalogLinksAsync(request)) is { } validationError)
            return BadRequest(validationError);
        if (request.Passes.Any(pass => pass.PassTemplateId.HasValue || pass.Exercises.Any(exercise => exercise.ExerciseTemplateId.HasValue)))
            return BadRequest("Nya mallar får inte innehålla befintliga id:n.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var split = new SplitTemplate
        {
            SplitTemplateName = request.SplitTemplateName.Trim(),
            Visability = request.IsVisible,
        };
        _context.SplitTemplate.Add(split);
        await _context.SaveChangesAsync();
        await UpsertChildrenAsync(split.SplitTemplateId, request.Passes, archiveMissing: false);
        await transaction.CommitAsync();
        return CreatedAtAction(nameof(Get), new { templateId = split.SplitTemplateId }, split.SplitTemplateId);
    }

    [HttpPut("{templateId:int}")]
    public async Task<IActionResult> Update(int templateId, UpsertAdminSplitTemplateRequest request)
    {
        if ((Validate(request) ?? await ValidateCatalogLinksAsync(request)) is { } validationError)
            return BadRequest(validationError);

        var split = await _context.SplitTemplate.FindAsync(templateId);
        if (split is null)
            return NotFound();

        await using var transaction = await _context.Database.BeginTransactionAsync();
        split.SplitTemplateName = request.SplitTemplateName.Trim();
        split.Visability = request.IsVisible;

        try
        {
            await UpsertChildrenAsync(templateId, request.Passes, archiveMissing: true);
            await transaction.CommitAsync();
        }
        catch (InvalidOperationException exception)
        {
            await transaction.RollbackAsync();
            return BadRequest(exception.Message);
        }

        return NoContent();
    }

    [HttpPatch("{templateId:int}/visibility")]
    public async Task<IActionResult> SetVisibility(int templateId, SetVisibilityRequest request)
    {
        var split = await _context.SplitTemplate.FindAsync(templateId);
        if (split is null)
            return NotFound();

        split.Visability = request.IsVisible;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task UpsertChildrenAsync(
        int splitTemplateId,
        IReadOnlyList<UpsertAdminPassTemplateRequest> incomingPasses,
        bool archiveMissing)
    {
        var existingPasses = await _context.PassTemplate
            .Where(pass => pass.SplitTemplateID == splitTemplateId)
            .ToListAsync();
        var incomingPassIds = incomingPasses.Where(pass => pass.PassTemplateId.HasValue)
            .Select(pass => pass.PassTemplateId!.Value)
            .ToList();
        if (incomingPassIds.Count != incomingPassIds.Distinct().Count())
            throw new InvalidOperationException("Samma pass-id får bara förekomma en gång.");

        if (archiveMissing)
        {
            foreach (var missing in existingPasses.Where(pass => !incomingPassIds.Contains(pass.PassTemplateId)))
                missing.Visability = false;
        }

        for (var passIndex = 0; passIndex < incomingPasses.Count; passIndex++)
        {
            var request = incomingPasses[passIndex];
            PassTemplate pass;
            if (request.PassTemplateId.HasValue)
            {
                pass = existingPasses.SingleOrDefault(item => item.PassTemplateId == request.PassTemplateId.Value)
                    ?? throw new InvalidOperationException("Ett pass-id tillhör inte den valda splitmallen.");
            }
            else
            {
                pass = new PassTemplate { SplitTemplateID = splitTemplateId };
                _context.PassTemplate.Add(pass);
            }

            pass.PassTemplateName = request.PassTemplateName.Trim();
            pass.Visability = request.IsVisible;
            pass.Ordning = passIndex + 1;
            await _context.SaveChangesAsync();

            var existingExercises = await _context.ExerciseTemplate
                .Where(exercise => exercise.PassTemplateId == pass.PassTemplateId)
                .ToListAsync();
            var incomingExerciseIds = request.Exercises.Where(exercise => exercise.ExerciseTemplateId.HasValue)
                .Select(exercise => exercise.ExerciseTemplateId!.Value)
                .ToList();
            if (incomingExerciseIds.Count != incomingExerciseIds.Distinct().Count())
                throw new InvalidOperationException("Samma övnings-id får bara förekomma en gång.");

            if (archiveMissing)
            {
                foreach (var missing in existingExercises.Where(exercise => !incomingExerciseIds.Contains(exercise.ExerciseTemplateId)))
                    missing.Visability = false;
            }

            for (var exerciseIndex = 0; exerciseIndex < request.Exercises.Count; exerciseIndex++)
            {
                var exerciseRequest = request.Exercises[exerciseIndex];
                ExerciseTemplate exercise;
                if (exerciseRequest.ExerciseTemplateId.HasValue)
                {
                    exercise = existingExercises.SingleOrDefault(item => item.ExerciseTemplateId == exerciseRequest.ExerciseTemplateId.Value)
                        ?? throw new InvalidOperationException("Ett övnings-id tillhör inte det valda passet.");
                }
                else
                {
                    exercise = new ExerciseTemplate { PassTemplateId = pass.PassTemplateId };
                    _context.ExerciseTemplate.Add(exercise);
                }

                exercise.ExerciseTemplateName = exerciseRequest.ExerciseTemplateName.Trim();
                exercise.CatalogExerciseId = exerciseRequest.CatalogExerciseId;
                exercise.Visability = exerciseRequest.IsVisible;
                exercise.Ordning = exerciseIndex + 1;
                exercise.DefaultSets = exerciseRequest.DefaultSets;
                exercise.DefaultRepsMin = exerciseRequest.DefaultRepsMin;
                exercise.DefaultRepsMax = exerciseRequest.DefaultRepsMax;
                exercise.DefaultWeight = exerciseRequest.DefaultWeight;
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task<string?> ValidateCatalogLinksAsync(UpsertAdminSplitTemplateRequest request) =>
        await CatalogLinks.AllExistAsync(_context, request.Passes.SelectMany(p => p.Exercises).Select(e => e.CatalogExerciseId))
            ? null
            : "En eller flera övningar pekar på en okänd katalogövning.";

    private static string? Validate(UpsertAdminSplitTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SplitTemplateName) || request.SplitTemplateName.Trim().Length > 50)
            return "Mallnamnet måste vara mellan 1 och 50 tecken.";
        if (request.Passes.Count == 0)
            return "Mallen måste innehålla minst ett pass.";

        foreach (var pass in request.Passes)
        {
            if (string.IsNullOrWhiteSpace(pass.PassTemplateName) || pass.PassTemplateName.Trim().Length > 50)
                return "Alla pass måste ha ett namn på högst 50 tecken.";
            if (pass.Exercises.Count == 0)
                return "Alla pass måste innehålla minst en övning.";
            foreach (var exercise in pass.Exercises)
            {
                if (string.IsNullOrWhiteSpace(exercise.ExerciseTemplateName) || exercise.ExerciseTemplateName.Trim().Length > 50)
                    return "Alla övningar måste ha ett namn på högst 50 tecken.";
                if (exercise.DefaultSets is < 1 or > 20
                    || exercise.DefaultRepsMin is < 1 or > 100
                    || exercise.DefaultRepsMax is < 1 or > 100
                    || exercise.DefaultRepsMin > exercise.DefaultRepsMax
                    || exercise.DefaultWeight is < 0 or > 1000)
                    return "Kontrollera set, repsintervall och vikt i övningarna.";
            }
        }

        return null;
    }

    private static AdminSplitTemplateDto MapTemplate(
        SplitTemplate split,
        IReadOnlyList<PassTemplate> passes,
        IReadOnlyList<ExerciseTemplate> exercises) =>
        new()
        {
            SplitTemplateId = split.SplitTemplateId,
            SplitTemplateName = split.SplitTemplateName,
            IsVisible = split.Visability,
            Passes = passes.Where(pass => pass.SplitTemplateID == split.SplitTemplateId)
                .OrderBy(pass => pass.Ordning)
                .Select(pass => new AdminPassTemplateDto
                {
                    PassTemplateId = pass.PassTemplateId,
                    PassTemplateName = pass.PassTemplateName,
                    IsVisible = pass.Visability,
                    Order = pass.Ordning,
                    Exercises = exercises.Where(exercise => exercise.PassTemplateId == pass.PassTemplateId)
                        .OrderBy(exercise => exercise.Ordning)
                        .Select(exercise => new AdminExerciseTemplateDto
                        {
                            ExerciseTemplateId = exercise.ExerciseTemplateId,
                            ExerciseTemplateName = exercise.ExerciseTemplateName,
                            CatalogExerciseId = exercise.CatalogExerciseId,
                            IsVisible = exercise.Visability,
                            Order = exercise.Ordning,
                            DefaultSets = exercise.DefaultSets ?? 3,
                            DefaultRepsMin = exercise.DefaultRepsMin ?? 8,
                            DefaultRepsMax = exercise.DefaultRepsMax ?? 12,
                            DefaultWeight = exercise.DefaultWeight ?? 0,
                        }).ToList(),
                }).ToList(),
        };
}
