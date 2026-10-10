using ArnoldOne.Shared.DTOs;
using DbArnold.DbArnold.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DbExercise = DbArnold.DbArnold.models.Exercise;

namespace ArnoldOne.Server.Controllers;

[Route("api/[controller]")]
public class SessionController : ApiControllerBase
{
    private const int MaxExercises = 100;

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

        if (request.Exercises.Count > MaxExercises
            || request.Exercises.Any(e => ValidateExercise(e.NoteText, e.Weight, e.Sets) is not null))
            return BadRequest("Ogiltiga värden för övning, set, reps, vikt eller anteckning.");

        if (!await UserOwnsPassAsync(request.PassId))
            return Forbid();

        var exerciseIds = request.Exercises.Select(e => e.ExerciseId).ToList();
        if (exerciseIds.Count > 0 && !await AllExercisesBelongToPassAsync(request.PassId, exerciseIds))
            return Forbid();

        var session = await GetOrCreateSessionAsync(request.SessionKey, request.PassId, request.Date, request.StartedAtUtc);
        if (session.PassId != request.PassId)
            return Forbid();

        if (session.CompletedAtUtc is not null)
            return request.CompleteSession ? Ok() : Conflict("Sessionen är redan avslutad och kan inte ändras.");

        var state = await LoadExerciseStateAsync(session, exerciseIds);
        foreach (var ex in request.Exercises)
            ReplaceExerciseLog(session, state, ex.ExerciseId, ex.NoteText, ex.Weight, ex.Sets);

        if (request.CompleteSession)
            session.CompletedAtUtc = DateTimeOffset.UtcNow;

        return await SaveOrConflictAsync();
    }

    /// <summary>Högsta avklarade vikt per avslutat pass för en övning (senaste 12), äldst först.</summary>
    [HttpGet("progress/{exerciseId:int}")]
    public async Task<ActionResult<List<ExerciseProgressPointDto>>> GetProgress(int exerciseId)
    {
        var rows = await _context.ExerciseLog
            .Where(l => l.ExerciseId == exerciseId
                && l.Session.CompletedAtUtc != null
                && l.Session.Pass.Split.UserId == CurrentUserId)
            .OrderByDescending(l => l.Session.StartedAtUtc)
            .Take(12)
            .Select(l => new { l.Session.Date, Weight = l.SetLog.Where(s => s.IsDone).Max(s => (decimal?)s.Weight) })
            .ToListAsync();

        return Ok(rows.Where(r => r.Weight > 0)
            .Select(r => new ExerciseProgressPointDto(r.Date, r.Weight!.Value))
            .Reverse().ToList());
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

        if (ValidateExercise(request.NoteText, request.Weight, request.Sets) is { } invalid)
            return BadRequest(invalid);

        if (!await UserOwnsPassAsync(request.PassId))
            return Forbid();

        if (!await AllExercisesBelongToPassAsync(request.PassId, new[] { request.ExerciseId }))
            return Forbid();

        var session = await GetOrCreateSessionAsync(request.SessionKey, request.PassId, request.Date, request.StartedAtUtc);
        if (session.PassId != request.PassId)
            return Forbid();

        if (session.CompletedAtUtc is not null)
            return Conflict("Sessionen är redan avslutad och kan inte ändras.");

        var state = await LoadExerciseStateAsync(session, new[] { request.ExerciseId });
        ReplaceExerciseLog(session, state, request.ExerciseId, request.NoteText, request.Weight, request.Sets);

        return await SaveOrConflictAsync();
    }

    /// <summary>
    /// Hämtar sessionen eller skapar den. Två samtidiga anrop med samma SessionKey (t.ex. en omförsökt
    /// synk) kan båda försöka skapa den; den som förlorar mot det unika indexet läser om vinnarens rad.
    /// </summary>
    private async Task<Session> GetOrCreateSessionAsync(Guid sessionKey, int passId, DateOnly date, DateTimeOffset startedAtUtc)
    {
        var session = await _context.Session.FirstOrDefaultAsync(s => s.SessionKey == sessionKey);
        if (session is not null)
            return session;

        session = new Session
        {
            SessionKey = sessionKey,
            PassId = passId,
            Date = date,
            StartedAtUtc = startedAtUtc.ToUniversalTime(),
        };
        _context.Session.Add(session);
        try
        {
            await _context.SaveChangesAsync();
            return session;
        }
        catch (DbUpdateException ex) when (DbExceptions.IsUniqueViolation(ex))
        {
            _context.Entry(session).State = EntityState.Detached;
            return await _context.Session.FirstAsync(s => s.SessionKey == sessionKey);
        }
    }

    private sealed record ExerciseState(Dictionary<int, ExerciseLog> Logs, Dictionary<int, DbExercise> Exercises);

    /// <summary>Läser sessionens befintliga övningsloggar (med set) och övningarna i två frågor, oavsett antal övningar.</summary>
    private async Task<ExerciseState> LoadExerciseStateAsync(Session session, IReadOnlyCollection<int> exerciseIds)
    {
        var logs = await _context.ExerciseLog
            .Include(e => e.SetLog)
            .Where(e => e.SessionId == session.SessionId && exerciseIds.Contains(e.ExerciseId))
            .ToDictionaryAsync(e => e.ExerciseId);
        var exercises = await _context.Exercise
            .Where(e => exerciseIds.Contains(e.ExerciseId))
            .ToDictionaryAsync(e => e.ExerciseId);
        return new ExerciseState(logs, exercises);
    }

    /// <summary>Ersätter övningens utförda set och anteckning, och sparar vikten som ny standardvikt.</summary>
    private void ReplaceExerciseLog(
        Session session, ExerciseState state, int exerciseId, string noteText, decimal weight, IEnumerable<SaveSessionSetDto> sets)
    {
        if (!state.Logs.TryGetValue(exerciseId, out var exerciseLog))
        {
            exerciseLog = new ExerciseLog { SessionId = session.SessionId, ExerciseId = exerciseId };
            _context.ExerciseLog.Add(exerciseLog);
            state.Logs[exerciseId] = exerciseLog;
        }

        exerciseLog.NoteText = noteText;
        _context.SetLog.RemoveRange(exerciseLog.SetLog);

        // Genom navigeringen sätts ExerciseLogID när loggen sparas, även för en ny rad.
        foreach (var s in sets.Where(s => s.IsDone))
        {
            exerciseLog.SetLog.Add(new SetLog
            {
                SetIndex = s.SetIndex,
                Reps     = s.Reps,
                IsDone   = true,
                Weight   = s.Weight,
            });
        }

        if (state.Exercises.TryGetValue(exerciseId, out var exercise))
            exercise.DefaultWeight = weight;
    }

    /// <summary>Sparar. Om två anrop skapade samma övningslogg samtidigt vinner ett; det andra får 409 och klienten försöker igen.</summary>
    private async Task<IActionResult> SaveOrConflictAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            return Ok();
        }
        catch (DbUpdateException ex) when (DbExceptions.IsUniqueViolation(ex))
        {
            return Conflict("Samtidig sparning, försök igen.");
        }
    }

    // Gränserna följer kolumnerna: SetLog.Weight är decimal(6,2) och NoteText är VARCHAR(5000).
    private static string? ValidateExercise(string? noteText, decimal weight, IReadOnlyCollection<SaveSessionSetDto> sets)
    {
        if (noteText is { Length: > 5000 })
            return "Anteckningen är för lång.";
        if (weight is < 0 or > 1000)
            return "Ogiltig vikt.";
        if (sets.Count > 100)
            return "För många set.";
        if (sets.Any(s => s.Reps is < 0 or > 500 || s.SetIndex is < 0 or > 100 || s.Weight is < 0 or > 1000))
            return "Ogiltiga värden för set.";
        return null;
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
