using Tyngd.Shared.DTOs;
using Tyngd.Shared.TrainingPrograms;
using Tyngd.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Tyngd.Server.Controllers;

[Route("api/[controller]")]
public sealed class DashboardController : ApiControllerBase
{
    private readonly TyngdContext _context;

    public DashboardController(TyngdContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<UserDashboardDto>> Get()
    {
        var now = DateTimeOffset.UtcNow;
        var sevenDaysAgo = now.AddDays(-7);
        var thirtyDaysAgo = now.AddDays(-30);
        // Aktivitetsrutnätet visar 16 hela veckor (måndag–söndag) bakåt från innevarande vecka.
        var activityStart = now.AddDays(-7 * 17);

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
            RecentActivity = await completed
                .Where(session => session.CompletedAtUtc >= activityStart)
                .Select(session => new DashboardActivityDto(session.CompletedAtUtc!.Value, session.Pass.Color, session.Pass.PassName))
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

        result.RecentCompletedAtUtc = result.RecentActivity.Select(a => a.CompletedAtUtc).ToList();
        return Ok(result);
    }
}
