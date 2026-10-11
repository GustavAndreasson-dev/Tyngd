using ArnoldOne.Shared.DTOs;
using ArnoldOne.Shared.TrainingPrograms;
using DbArnold.DbArnold.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArnoldOne.Server.Controllers;

[Route("api/[controller]")]
public sealed class DashboardController : ApiControllerBase
{
    private readonly ArnoldContext _context;

    public DashboardController(ArnoldContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<UserDashboardDto>> Get()
    {
        var now = DateTimeOffset.UtcNow;
        var sevenDaysAgo = now.AddDays(-7);
        var thirtyDaysAgo = now.AddDays(-30);

        var splits = await _context.Split
            .Where(split => split.UserId == CurrentUserId && split.Visability && !split.IsFreeSessions)
            .Include(split => split.Pass.Where(pass => pass.Visability == 1).OrderBy(pass => pass.Ordning))
            .OrderBy(split => split.SplitId)
            .ToListAsync();

        var freeSplit = await _context.Split
            .Where(split => split.UserId == CurrentUserId && split.IsFreeSessions)
            .Include(split => split.Pass.Where(pass => pass.Visability == 1).OrderByDescending(pass => pass.Ordning).Take(5))
            .FirstOrDefaultAsync();

        var completed = _context.Session
            .Where(session => session.Pass.Split.UserId == CurrentUserId && session.CompletedAtUtc != null);

        var result = new UserDashboardDto
        {
            ActiveSplits = splits.Select(split => split.ToTrainingSplit()).ToList(),
            RecentFreeSessions = freeSplit?.ToTrainingSplit(),
            CompletedSessionsLast7Days = await completed.CountAsync(session => session.CompletedAtUtc >= sevenDaysAgo),
            CompletedSessionsLast30Days = await completed.CountAsync(session => session.CompletedAtUtc >= thirtyDaysAgo),
            TotalCompletedSessions = await completed.CountAsync(),
            RecentCompletedAtUtc = await completed
                .Where(session => session.CompletedAtUtc >= sevenDaysAgo)
                .Select(session => session.CompletedAtUtc!.Value)
                .ToListAsync(),
            LastCompletedSession = await completed
                .OrderByDescending(session => session.CompletedAtUtc)
                .Select(session => new DashboardSessionDto
                {
                    SessionKey = session.SessionKey,
                    SplitId = session.Pass.SplitId,
                    SplitName = session.Pass.Split.SplitName,
                    PassId = session.PassId,
                    PassName = session.Pass.PassName,
                    StartedAtUtc = session.StartedAtUtc,
                    CompletedAtUtc = session.CompletedAtUtc!.Value,
                })
                .FirstOrDefaultAsync(),
        };

        return Ok(result);
    }
}
