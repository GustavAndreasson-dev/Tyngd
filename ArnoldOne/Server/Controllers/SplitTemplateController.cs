using ArnoldOne.Shared.TrainingPrograms;
using DbArnold.DbArnold.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedExercise = ArnoldOne.Shared.TrainingPrograms.Exercise;

namespace ArnoldOne.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SplitTemplateController : ControllerBase
{
    private readonly ArnoldContext _context;

    public SplitTemplateController(ArnoldContext context)
    {
        _context = context;
    }

    /// <summary>Returnerar alla splitmallar med tillhörande pass och övningar.</summary>
    [HttpGet]
    public async Task<ActionResult<List<TrainingSplitTemplate>>> GetAll()
    {
        var splits = await _context.SplitTemplate
            .Where(s => s.Visability)
            .OrderBy(s => s.SplitTemplateId)
            .ToListAsync();

        var passMallar = await _context.PassTemplate
            .Where(p => p.Visability)
            .OrderBy(p => p.Ordning)
            .ToListAsync();

        var exercises = await _context.ExerciseTemplate
            .Where(e => e.Visability)
            .OrderBy(e => e.Ordning)
            .ToListAsync();

        var result = splits.Select(s => new TrainingSplitTemplate
        {
            SplitId   = s.SplitTemplateId,
            SplitName = s.SplitTemplateName,
            Passes    = passMallar
                .Where(p => p.SplitTemplateID == s.SplitTemplateId)
                .Select(p => new TrainingPassTemplate
                {
                    PassId   = p.PassTemplateId,
                    SplitId  = s.SplitTemplateId,
                    PassName = p.PassTemplateName,
                    Exercises = exercises
                        .Where(e => e.PassTemplateId == p.PassTemplateId)
                        .Select(e => new TrainingExerciseTemplate
                        {
                            ExerciseId     = e.ExerciseTemplateId,
                            Exercise       = new SharedExercise { ExerciseId = e.ExerciseTemplateId, ExerciseName = e.ExerciseTemplateName },
                            DefaultSets    = e.DefaultSets    ?? 3,
                            DefaultRepsMin = e.DefaultRepsMin ?? 8,
                            DefaultRepsMax = e.DefaultRepsMax ?? 12,
                            DefaultWeight  = e.DefaultWeight  ?? 0,
                        })
                        .ToList(),
                })
                .ToList(),
        }).ToList();

        return Ok(result);
    }

    /// <summary>Returnerar ett specifikt pass med övningar.</summary>
    [HttpGet("{splitId}/pass/{passId}")]
    public async Task<ActionResult<TrainingPassTemplate>> GetPass(int splitId, int passId)
    {
        var pass = await _context.PassTemplate
            .FirstOrDefaultAsync(p => p.PassTemplateId == passId && p.SplitTemplateID == splitId && p.Visability);

        if (pass is null)
            return NotFound();

        var exercises = await _context.ExerciseTemplate
            .Where(e => e.PassTemplateId == passId && e.Visability)
            .OrderBy(e => e.Ordning)
            .ToListAsync();

        var result = new TrainingPassTemplate
        {
            PassId   = pass.PassTemplateId,
            SplitId  = splitId,
            PassName = pass.PassTemplateName,
            Exercises = exercises.Select(e => new TrainingExerciseTemplate
            {
                ExerciseId     = e.ExerciseTemplateId,
                Exercise       = new SharedExercise { ExerciseId = e.ExerciseTemplateId, ExerciseName = e.ExerciseTemplateName },
                DefaultSets    = e.DefaultSets    ?? 3,
                DefaultRepsMin = e.DefaultRepsMin ?? 8,
                DefaultRepsMax = e.DefaultRepsMax ?? 12,
                DefaultWeight  = e.DefaultWeight  ?? 0,
            }).ToList(),
        };

        return Ok(result);
    }
}
