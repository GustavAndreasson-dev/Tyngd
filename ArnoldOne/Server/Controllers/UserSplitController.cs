using ArnoldOne.Shared.DTOs;
using ArnoldOne.Shared.TrainingPrograms;
using DbArnold.DbArnold.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DbExercise = DbArnold.DbArnold.models.Exercise;
using SharedExercise = ArnoldOne.Shared.TrainingPrograms.Exercise;

namespace ArnoldOne.Server.Controllers;

[Route("api/[controller]")]
public class UserSplitController : ApiControllerBase
{
    private readonly ArnoldContext _context;

    public UserSplitController(ArnoldContext context)
    {
        _context = context;
    }

    /// <summary>Returnerar alla aktiva splits för inloggad användare med riktiga Pass.PassId.</summary>
    [HttpGet("active")]
    public async Task<ActionResult<List<TrainingSplit>>> GetActiveSplits()
    {
        var userId = CurrentUserId;

        var splits = await _context.Split
            .Where(s => s.UserId == userId && s.Visability)
            .Include(s => s.Pass.Where(p => p.Visability == 1).OrderBy(p => p.Ordning))
            .ToListAsync();

        var result = splits.Select(s => new TrainingSplit
        {
            SplitId   = s.SplitId,
            SplitName = s.SplitName,
            UserId    = s.UserId.ToString(),
            Passes    = s.Pass.Select(p => new TrainingPass
            {
                PassId   = p.PassId,
                SplitId  = s.SplitId,
                PassName = p.PassName,
            }).ToList(),
        }).ToList();

        return Ok(result);
    }

    /// <summary>Returnerar pass-data med övningar för inloggad användare.</summary>
    [HttpGet("{splitId:int}/pass/{passId:int}")]
    public async Task<ActionResult<TrainingPassTemplate>> GetPass(int splitId, int passId)
    {
        var userId = CurrentUserId;

        var pass = await _context.Pass
            .Where(p => p.PassId == passId
                && p.SplitId == splitId
                && p.Visability == 1
                && p.Split.Visability
                && p.Split.UserId == userId)
            .Include(p => p.Exercise.Where(e => e.Visability).OrderBy(e => e.Ordning))
            .FirstOrDefaultAsync();

        if (pass is null)
            return NotFound();

        var result = new TrainingPassTemplate
        {
            PassId   = pass.PassId,
            SplitId  = pass.SplitId,
            PassName = pass.PassName,
            Exercises = pass.Exercise.Select(e => new TrainingExerciseTemplate
            {
                ExerciseId     = e.ExerciseId,
                Exercise       = new SharedExercise { ExerciseId = e.ExerciseId, ExerciseName = e.ExerciseName },
                DefaultSets    = e.DefaultSets    ?? 3,
                DefaultRepsMin = e.DefautlRepsMin ?? 8,
                DefaultRepsMax = e.DefaultRepsMax ?? 12,
                DefaultWeight  = e.DefaultWeight  ?? 0,
            }).ToList(),
        };

        return Ok(result);
    }

    /// <summary>Returnerar SplitTemplateId för alla aktiva splits för inloggad användare.</summary>
    [HttpGet]
    public async Task<ActionResult<List<int>>> GetActiveSplitTemplateIds()
    {
        var userId = CurrentUserId;
        var templateIds = await _context.Split
            .Where(s => s.UserId == userId && s.Visability && s.SplitTemplateId != null)
            .Select(s => s.SplitTemplateId!.Value)
            .ToListAsync();

        return Ok(templateIds);
    }

    /// <summary>Aktiverar en split för inloggad användare. Återaktiverar om den tidigare avaktiverats.</summary>
    [HttpPost("{splitTemplateId}")]
    public async Task<IActionResult> ActivateSplit(int splitTemplateId)
    {
        var userId = CurrentUserId;

        // Kolla om splitten redan finns (även avaktiverade)
        var existing = await _context.Split
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SplitTemplateId == splitTemplateId);

        if (existing is not null)
        {
            if (existing.Visability)
                return Ok();

            existing.Visability = true;
            await _context.SaveChangesAsync();
            return Ok();
        }

        // Hämta mallen med pass och övningar
        var template = await _context.SplitTemplate
            .Include(s => s.PassTemplate)
            .FirstOrDefaultAsync(s => s.SplitTemplateId == splitTemplateId);

        if (template is null)
            return NotFound();

        var passTemplateIds = template.PassTemplate.Select(p => p.PassTemplateId).ToList();
        var exercises = await _context.ExerciseTemplate
            .Where(e => passTemplateIds.Contains(e.PassTemplateId))
            .ToListAsync();

        // Skapa Split – låt IDENTITY generera SplitId
        var split = new Split
        {
            SplitTemplateId = splitTemplateId,
            SplitName       = template.SplitTemplateName,
            UserId          = userId,
            Visability      = true,
            Pass = template.PassTemplate.OrderBy(p => p.Ordning).Select(p => new Pass
            {
                PassTemplateId = p.PassTemplateId,
                PassName       = p.PassTemplateName,
                Visability     = 1,
                Ordning        = p.Ordning,
                Exercise = exercises
                    .Where(e => e.PassTemplateId == p.PassTemplateId)
                    .OrderBy(e => e.Ordning)
                    .Select(e => new DbExercise
                    {
                        ExerciseName   = e.ExerciseTemplateName,
                        DefaultSets    = e.DefaultSets,
                        DefautlRepsMin = e.DefaultRepsMin,
                        DefaultRepsMax = e.DefaultRepsMax,
                        DefaultWeight  = e.DefaultWeight,
                        Ordning        = e.Ordning,
                        Visability     = true,
                    }).ToList(),
            }).ToList(),
        };

        _context.Split.Add(split);
        await _context.SaveChangesAsync();

        return Ok();
    }

    /// <summary>Avaktiverar en split för inloggad användare genom att sätta Visability = false.</summary>
    [HttpDelete("{splitTemplateId}")]
    public async Task<IActionResult> DeactivateSplit(int splitTemplateId)
    {
        var userId = CurrentUserId;

        var split = await _context.Split
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SplitTemplateId == splitTemplateId && s.Visability);

        if (split is null)
            return NotFound();

        split.Visability = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Skapar en egen split med pass och övningar för inloggad användare.</summary>
    [HttpPost("custom")]
    public async Task<ActionResult<int>> CreateCustomSplit([FromBody] CreateSplitRequest request)
    {
        var validationResult = ValidateRequest(request);
        if (validationResult is not null)
            return validationResult;

        var userId = CurrentUserId;

        var split = new Split
        {
            SplitTemplateId = null,
            SplitName       = request.SplitName.Trim(),
            UserId          = userId,
            Visability      = true,
            Pass = request.Passes.Select((p, pi) => new Pass
            {
                PassTemplateId = null,
                PassName       = p.PassName.Trim(),
                Visability     = 1,
                Ordning        = pi + 1,
                Exercise = p.Exercises.Select((e, ei) => new DbExercise
                {
                    ExerciseName   = e.ExerciseName.Trim(),
                    DefaultSets    = e.DefaultSets,
                    DefautlRepsMin = e.RepsMin,
                    DefaultRepsMax = e.RepsMax,
                    DefaultWeight  = e.DefaultWeight,
                    Ordning        = ei + 1,
                    Visability     = true,
                }).ToList(),
            }).ToList(),
        };

        _context.Split.Add(split);
        await _context.SaveChangesAsync();

        return Ok(split.SplitId);
    }

    /// <summary>Uppdaterar en egen split med nya pass och övningar för inloggad användare.</summary>
    [HttpPut("my/{splitId:int}")]
    public async Task<IActionResult> UpdateMySplit(int splitId, [FromBody] CreateSplitRequest request)
    {
        var validationResult = ValidateRequest(request);
        if (validationResult is not null)
            return validationResult;

        var userId = CurrentUserId;

        var split = await _context.Split
            .Where(s => s.SplitId == splitId
                && s.UserId == userId
                && s.Visability
                && s.SplitTemplateId == null)
            .Include(s => s.Pass)
            .ThenInclude(p => p.Exercise)
            .FirstOrDefaultAsync();

        if (split is null)
            return NotFound();

        var activePasses = split.Pass.Where(p => p.Visability == 1).ToDictionary(p => p.PassId);
        var incomingPassIds = request.Passes.Where(p => p.PassId.HasValue).Select(p => p.PassId!.Value).ToList();

        if (incomingPassIds.Count != incomingPassIds.Distinct().Count()
            || incomingPassIds.Any(id => !activePasses.ContainsKey(id)))
            return BadRequest("Ett eller flera pass-ID:n är ogiltiga för splitten.");

        split.SplitName = request.SplitName.Trim();

        foreach (var (passRequest, passIndex) in request.Passes.Select((pass, index) => (pass, index)))
        {
            Pass pass;
            if (passRequest.PassId.HasValue)
            {
                pass = activePasses[passRequest.PassId.Value];
            }
            else
            {
                pass = new Pass
                {
                    SplitId = split.SplitId,
                    PassTemplateId = null,
                    Visability = 1,
                };
                _context.Pass.Add(pass);
            }

            pass.PassName = passRequest.PassName.Trim();
            pass.Ordning = passIndex + 1;

            var activeExercises = pass.Exercise.Where(e => e.Visability).ToDictionary(e => e.ExerciseId);
            var incomingExerciseIds = passRequest.Exercises
                .Where(e => e.ExerciseId.HasValue)
                .Select(e => e.ExerciseId!.Value)
                .ToList();

            if (incomingExerciseIds.Count != incomingExerciseIds.Distinct().Count()
                || incomingExerciseIds.Any(id => !activeExercises.ContainsKey(id)))
                return BadRequest("En eller flera övningar tillhör inte det angivna passet.");

            foreach (var (exerciseRequest, exerciseIndex) in passRequest.Exercises.Select((exercise, index) => (exercise, index)))
            {
                DbExercise exercise;
                if (exerciseRequest.ExerciseId.HasValue)
                {
                    exercise = activeExercises[exerciseRequest.ExerciseId.Value];
                }
                else
                {
                    exercise = new DbExercise
                    {
                        Pass = pass,
                        Visability = true,
                    };
                    _context.Exercise.Add(exercise);
                }

                exercise.ExerciseName = exerciseRequest.ExerciseName.Trim();
                exercise.DefaultSets = exerciseRequest.DefaultSets;
                exercise.DefautlRepsMin = exerciseRequest.RepsMin;
                exercise.DefaultRepsMax = exerciseRequest.RepsMax;
                exercise.DefaultWeight = exerciseRequest.DefaultWeight;
                exercise.Ordning = exerciseIndex + 1;
            }

            foreach (var removedExercise in activeExercises.Values
                         .Where(e => !incomingExerciseIds.Contains(e.ExerciseId)))
                removedExercise.Visability = false;
        }

        foreach (var removedPass in activePasses.Values.Where(p => !incomingPassIds.Contains(p.PassId)))
        {
            removedPass.Visability = 0;
            foreach (var exercise in removedPass.Exercise.Where(e => e.Visability))
                exercise.Visability = false;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Returnerar en enskild egen split med alla pass och övningar – redo att redigera.</summary>
    [HttpGet("my/{splitId:int}/full")]
    public async Task<ActionResult<CreateSplitRequest>> GetMySplitFull(int splitId)
    {
        var userId = CurrentUserId;

        var split = await _context.Split
            .AsNoTracking()
            .Where(s => s.SplitId == splitId
                && s.UserId == userId
                && s.Visability
                && s.SplitTemplateId == null)
            .FirstOrDefaultAsync();

        if (split is null)
            return NotFound();

        var passes = await _context.Pass
            .AsNoTracking()
            .Where(p => p.SplitId == splitId && p.Visability == 1)
            .OrderBy(p => p.Ordning)
            .ToListAsync();

        var passIds = passes.Select(p => p.PassId).ToList();
        var exercises = await _context.Exercise
            .AsNoTracking()
            .Where(e => passIds.Contains(e.PassId) && e.Visability)
            .OrderBy(e => e.Ordning)
            .ToListAsync();

        var result = new CreateSplitRequest
        {
            SplitName = split.SplitName,
            Passes = passes.Select(p => new CreatePassDto
            {
                PassId = p.PassId,
                PassName  = p.PassName,
                Exercises = exercises.Where(e => e.PassId == p.PassId).Select(e => new CreateExerciseDto
                {
                    ExerciseId     = e.ExerciseId,
                    ExerciseName   = e.ExerciseName,
                    DefaultSets    = e.DefaultSets    ?? 3,
                    RepsMin        = e.DefautlRepsMin ?? 8,
                    RepsMax        = e.DefaultRepsMax ?? 12,
                    DefaultWeight  = e.DefaultWeight  ?? 0,
                }).ToList(),
            }).ToList(),
        };

        return Ok(result);
    }

    /// <summary>Returnerar inloggad användares egna splits (utan mall) med pass.</summary>
    [HttpGet("my")]
    public async Task<ActionResult<List<TrainingSplit>>> GetMySplits()
    {
        var userId = CurrentUserId;

        var splits = await _context.Split
            .Where(s => s.UserId == userId && s.Visability && s.SplitTemplateId == null)
            .Include(s => s.Pass.Where(p => p.Visability == 1).OrderBy(p => p.Ordning))
            .ToListAsync();

        var result = splits.Select(s => new TrainingSplit
        {
            SplitId   = s.SplitId,
            SplitName = s.SplitName,
            UserId    = s.UserId.ToString(),
            Passes    = s.Pass.Select(p => new TrainingPass
            {
                PassId   = p.PassId,
                SplitId  = s.SplitId,
                PassName = p.PassName,
            }).ToList(),
        }).ToList();

        return Ok(result);
    }

    /// <summary>Tar bort en egen split (Visability = false) för inloggad användare.</summary>
    [HttpDelete("my/{splitId:int}")]
    public async Task<IActionResult> DeleteMySplit(int splitId)
    {
        var userId = CurrentUserId;

        var split = await _context.Split
            .FirstOrDefaultAsync(s => s.SplitId == splitId && s.UserId == userId && s.SplitTemplateId == null);

        if (split is null)
            return NotFound();

        split.Visability = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private ActionResult? ValidateRequest(CreateSplitRequest request)
    {
        var errors = SplitRequestValidator.Validate(request);
        if (errors.Count == 0)
            return null;

        var validationErrors = errors
            .GroupBy(error => error.Key)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Message).Distinct().ToArray());

        return ValidationProblem(new ValidationProblemDetails(validationErrors));
    }
}
