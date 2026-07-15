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
        if (request.SessionKey == Guid.Empty || request.StartedAtUtc == default)
            return BadRequest("SessionKey och StartedAtUtc måste anges.");

        if (!await UserOwnsPassAsync(request.PassId))
            return Forbid();

        var exerciseIds = request.Exercises.Select(e => e.ExerciseId).ToList();
        if (exerciseIds.Count > 0 && !await AllExercisesBelongToPassAsync(request.PassId, exerciseIds))
            return Forbid();

        var session = await _context.Session
            .FirstOrDefaultAsync(s => s.SessionKey == request.SessionKey);

        if (session is not null && session.PassId != request.PassId)
            return Forbid();

        if (session?.CompletedAtUtc is not null)
            return request.CompleteSession ? Ok() : Conflict("Sessionen är redan avslutad och kan inte ändras.");

        if (session is null)
        {
            session = new Session
            {
                SessionKey = request.SessionKey,
                PassId = request.PassId,
                Date = request.Date,
                StartedAtUtc = request.StartedAtUtc.ToUniversalTime(),
            };
            _context.Session.Add(session);
            await _context.SaveChangesAsync();
        }

        foreach (var ex in request.Exercises)
        {
            // Hämta eller skapa ExerciseLog
            var exerciseLog = await _context.ExerciseLog
                .Include(e => e.SetLog)
                .FirstOrDefaultAsync(e => e.SessionId == session.SessionId && e.ExerciseId == ex.ExerciseId);

            if (exerciseLog is null)
            {
                exerciseLog = new ExerciseLog
                {
                    SessionId  = session.SessionId,
                    ExerciseId = ex.ExerciseId,
                };
                _context.ExerciseLog.Add(exerciseLog);
                await _context.SaveChangesAsync();
            }

            exerciseLog.NoteText = ex.NoteText;

            _context.SetLog.RemoveRange(exerciseLog.SetLog);

            foreach (var s in ex.Sets.Where(s => s.IsDone))
            {
                _context.SetLog.Add(new SetLog
                {
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

        if (request.CompleteSession)
            session.CompletedAtUtc = DateTimeOffset.UtcNow;

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
            .OrderByDescending(s => s.StartedAtUtc)
            .Include(s => s.ExerciseLog)
                .ThenInclude(e => e.SetLog)
            .Include(s => s.ExerciseLog)
                .ThenInclude(e => e.Exercise)
            .FirstOrDefaultAsync();

        if (lastSession is null)
            return Ok(new LastSessionDto());

        var dto = new LastSessionDto
        {
            SessionKey = lastSession.SessionKey,
            Date      = lastSession.Date,
            StartedAtUtc = lastSession.StartedAtUtc,
            CompletedAtUtc = lastSession.CompletedAtUtc,
            Exercises = lastSession.ExerciseLog.Select(e => new LastSessionExerciseDto
            {
                ExerciseId = e.ExerciseId,
                NoteText   = e.NoteText ?? string.Empty,
                Weight     = e.SetLog.OrderByDescending(s => s.SetIndex)
                    .Select(s => s.Weight)
                    .FirstOrDefault(e.Exercise.DefaultWeight ?? 0),
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

    [HttpPost("{sessionKey:guid}/complete")]
    public async Task<IActionResult> CompleteSession(Guid sessionKey)
    {
        var session = await _context.Session
            .FirstOrDefaultAsync(s => s.SessionKey == sessionKey && s.Pass.Split.UserId == CurrentUserId);

        if (session is null)
            return NotFound();

        if (session.CompletedAtUtc is null)
        {
            session.CompletedAtUtc = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Ok();
    }

    /// <summary>
    /// Sparar eller uppdaterar en enskild övning för dagens session.
    /// Skapar sessionen om den inte finns ännu för dagens datum.
    /// </summary>
    [HttpPost("exercise")]
    public async Task<IActionResult> UpsertExercise([FromBody] UpsertExerciseRequest request)
    {
        if (request.SessionKey == Guid.Empty || request.StartedAtUtc == default)
            return BadRequest("SessionKey och StartedAtUtc måste anges.");

        if (!await UserOwnsPassAsync(request.PassId))
            return Forbid();

        if (!await AllExercisesBelongToPassAsync(request.PassId, new[] { request.ExerciseId }))
            return Forbid();

        var session = await _context.Session
            .FirstOrDefaultAsync(s => s.SessionKey == request.SessionKey);

        if (session is not null && session.PassId != request.PassId)
            return Forbid();

        if (session?.CompletedAtUtc is not null)
            return Conflict("Sessionen är redan avslutad och kan inte ändras.");

        if (session is null)
        {
            session = new Session
            {
                SessionKey = request.SessionKey,
                PassId = request.PassId,
                Date = request.Date,
                StartedAtUtc = request.StartedAtUtc.ToUniversalTime(),
            };
            _context.Session.Add(session);
            await _context.SaveChangesAsync();
        }

        // Hämta eller skapa ExerciseLog för övningen
        var exerciseLog = await _context.ExerciseLog
            .Include(e => e.SetLog)
            .FirstOrDefaultAsync(e => e.SessionId == session.SessionId && e.ExerciseId == request.ExerciseId);

        if (exerciseLog is null)
        {
            exerciseLog = new ExerciseLog
            {
                SessionId  = session.SessionId,
                ExerciseId = request.ExerciseId,
            };
            _context.ExerciseLog.Add(exerciseLog);
            await _context.SaveChangesAsync();
        }

        exerciseLog.NoteText = request.NoteText;

        // Ta bort gamla sets och ersätt med nya (bara utförda)
        _context.SetLog.RemoveRange(exerciseLog.SetLog);

        foreach (var s in request.Sets.Where(s => s.IsDone))
        {
            _context.SetLog.Add(new SetLog
            {
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
    private async Task<bool> UserOwnsPassAsync(int passId) =>
        await _context.Pass
            .AnyAsync(p => p.PassId == passId && p.Split.UserId == CurrentUserId);

    /// <summary>Returnerar true om alla ExerciseId tillhör det angivna passet.</summary>
    private async Task<bool> AllExercisesBelongToPassAsync(int passId, IEnumerable<int> exerciseIds)
    {
        var validIds = await _context.Exercise
            .Where(e => e.PassId == passId)
            .Select(e => e.ExerciseId)
            .ToListAsync();

        return exerciseIds.All(id => validIds.Contains(id));
    }
}
