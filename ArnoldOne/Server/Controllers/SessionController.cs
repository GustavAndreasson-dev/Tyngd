using ArnoldOne.Shared.DTOs;
using DbArnold.DbArnold.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArnoldOne.Server.Controllers;

[Route("api/[controller]")]
public class SessionController : ApiControllerBase
{
    private readonly ArnoldContext _context;

    public SessionController(ArnoldContext context)
    {
        _context = context;
    }

    /// <summary>Sparar ett avslutat träningspass. Upsert:ar befintlig session för dagen om den finns.</summary>
    [HttpPost]
    public async Task<IActionResult> SaveSession([FromBody] SaveSessionRequest request)
    {
        if (!await UserOwnsPassAsync(request.PassId))
            return Forbid();

        // Återanvänd befintlig session för dagen om den finns (skapad av UpsertExercise)
        var session = await _context.Session
            .FirstOrDefaultAsync(s => s.PassId == request.PassId && s.Date == request.Date);

        if (session is null)
        {
            session = new Session { PassId = request.PassId, Date = request.Date };
            _context.Session.Add(session);
            await _context.SaveChangesAsync();
        }

        var nextExerciseLogId = await _context.ExerciseLog.MaxAsync(e => (int?)e.ExerciseLogId) ?? 0;
        var nextSetLogId      = await _context.SetLog.MaxAsync(s => (int?)s.SetLogIId) ?? 0;

        foreach (var ex in request.Exercises)
        {
            // Hämta eller skapa ExerciseLog
            var exerciseLog = await _context.ExerciseLog
                .Include(e => e.SetLog)
                .FirstOrDefaultAsync(e => e.SessionId == session.SessionId && e.ExerciseId == ex.ExerciseId);

            if (exerciseLog is null)
            {
                nextExerciseLogId++;
                exerciseLog = new ExerciseLog
                {
                    ExerciseLogId = nextExerciseLogId,
                    SessionId     = session.SessionId,
                    ExerciseId    = ex.ExerciseId,
                };
                _context.ExerciseLog.Add(exerciseLog);
                await _context.SaveChangesAsync();
            }

            exerciseLog.NoteText = ex.NoteText;

            _context.SetLog.RemoveRange(exerciseLog.SetLog);

            foreach (var s in ex.Sets.Where(s => s.IsDone))
            {
                nextSetLogId++;
                _context.SetLog.Add(new SetLog
                {
                    SetLogIId     = nextSetLogId,
                    ExerciseLogID = exerciseLog.ExerciseLogId,
                    SetIndex      = s.SetIndex,
                    Reps          = s.Reps,
                    IsDone        = true,
                    Weight        = s.Weight,
                });
            }

            var exercise = await _context.Exercise.FindAsync(ex.ExerciseId);
            if (exercise is not null)
                exercise.DefaultWeight = ex.Weight;
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    /// <summary>Hämtar senaste sparade data för ett pass (vikt, reps, anteckningar).</summary>
    [HttpGet("last/{passId}")]
    public async Task<ActionResult<LastSessionDto>> GetLastSession(int passId)
    {
        if (!await UserOwnsPassAsync(passId))
            return Forbid();

        var lastSession = await _context.Session
            .Where(s => s.PassId == passId)
            .OrderByDescending(s => s.Date)
            .Include(s => s.ExerciseLog)
                .ThenInclude(e => e.SetLog)
            .Include(s => s.ExerciseLog)
                .ThenInclude(e => e.Exercise)
            .FirstOrDefaultAsync();

        if (lastSession is null)
            return Ok(new LastSessionDto());

        var dto = new LastSessionDto
        {
            Date      = lastSession.Date,
            Exercises = lastSession.ExerciseLog.Select(e => new LastSessionExerciseDto
            {
                ExerciseId = e.ExerciseId,
                NoteText   = e.NoteText ?? string.Empty,
                Weight     = e.Exercise.DefaultWeight ?? 0,
                Sets       = e.SetLog.OrderBy(s => s.SetIndex).Select(s => new LastSessionSetDto
                {
                    SetIndex = s.SetIndex,
                    Reps     = s.Reps,
                    Weight   = s.Weight,
                }).ToList(),
            }).ToList(),
        };

        return Ok(dto);
    }

    /// <summary>
    /// Sparar eller uppdaterar en enskild övning för dagens session.
    /// Skapar sessionen om den inte finns ännu för dagens datum.
    /// </summary>
    [HttpPost("exercise")]
    public async Task<IActionResult> UpsertExercise([FromBody] UpsertExerciseRequest request)
    {
        if (!await UserOwnsPassAsync(request.PassId))
            return Forbid();

        // Hämta eller skapa session för dagens datum
        var session = await _context.Session
            .FirstOrDefaultAsync(s => s.PassId == request.PassId && s.Date == request.Date);

        if (session is null)
        {
            session = new Session { PassId = request.PassId, Date = request.Date };
            _context.Session.Add(session);
            await _context.SaveChangesAsync();
        }

        // Hämta eller skapa ExerciseLog för övningen
        var exerciseLog = await _context.ExerciseLog
            .Include(e => e.SetLog)
            .FirstOrDefaultAsync(e => e.SessionId == session.SessionId && e.ExerciseId == request.ExerciseId);

        if (exerciseLog is null)
        {
            var nextId = await _context.ExerciseLog.MaxAsync(e => (int?)e.ExerciseLogId) ?? 0;
            nextId++;
            exerciseLog = new ExerciseLog
            {
                ExerciseLogId = nextId,
                SessionId     = session.SessionId,
                ExerciseId    = request.ExerciseId,
            };
            _context.ExerciseLog.Add(exerciseLog);
            await _context.SaveChangesAsync();
        }

        exerciseLog.NoteText = request.NoteText;

        // Ta bort gamla sets och ersätt med nya (bara utförda)
        _context.SetLog.RemoveRange(exerciseLog.SetLog);

        var nextSetId = await _context.SetLog.MaxAsync(s => (int?)s.SetLogIId) ?? 0;
        foreach (var s in request.Sets.Where(s => s.IsDone))
        {
            nextSetId++;
            _context.SetLog.Add(new SetLog
            {
                SetLogIId     = nextSetId,
                ExerciseLogID = exerciseLog.ExerciseLogId,
                SetIndex      = s.SetIndex,
                Reps          = s.Reps,
                IsDone        = true,
                Weight        = s.Weight,
            });
        }

        // Uppdatera defaultvikt på övningen
        var exercise = await _context.Exercise.FindAsync(request.ExerciseId);
        if (exercise is not null)
            exercise.DefaultWeight = request.Weight;

        await _context.SaveChangesAsync();
        return Ok();
    }

    /// <summary>Returnerar true om det inloggade kontot äger det angivna passet.</summary>
    private async Task<bool> UserOwnsPassAsync(int passId)
    {
        return await _context.Pass
            .AnyAsync(p => p.PassId == passId && p.Split.UserId == CurrentUserId);
    }
}
