using Tyngd.Shared.DTOs;
using Tyngd.Shared.Users;
using Tyngd.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Tyngd.Server.Controllers;

[Route("api/admin")]
public sealed class AdminController : AdminApiControllerBase
{
    private readonly TyngdContext _context;

    public AdminController(TyngdContext context) => _context = context;

    [HttpGet("overview")]
    public async Task<ActionResult<AdminOverviewDto>> GetOverview()
    {
        var now = DateTimeOffset.UtcNow;
        var sevenDaysAgo = now.AddDays(-7);
        var thirtyDaysAgo = now.AddDays(-30);
        var completed = _context.Session.Where(session => session.CompletedAtUtc != null);

        var recentUsers = await _context.UserInfo
            .OrderByDescending(user => user.CreatedAt)
            .Take(10)
            .Select(user => new
            {
                user.UserId,
                user.UserName,
                user.Email,
                user.CreatedAt,
                user.Roles,
            })
            .ToListAsync();

        var recentSessionRows = await completed
            .OrderByDescending(session => session.CompletedAtUtc)
            .Take(10)
            .Select(session => new
            {
                session.SessionKey,
                session.Pass.Split.User.UserName,
                session.Pass.Split.SplitName,
                session.Pass.PassName,
                session.CompletedAtUtc,
                session.StartedAtUtc,
            })
            .ToListAsync();
        var recentSessions = recentSessionRows.Select(session => new AdminSessionDto
        {
            SessionKey = session.SessionKey,
            UserName = session.UserName,
            SplitName = session.SplitName,
            PassName = session.PassName,
            CompletedAtUtc = session.CompletedAtUtc!.Value,
            DurationMinutes = (int)Math.Max(0, (session.CompletedAtUtc.Value - session.StartedAtUtc).TotalMinutes),
        }).ToList();

        return Ok(new AdminOverviewDto
        {
            TotalUsers = await _context.UserInfo.CountAsync(),
            NewUsersLast30Days = await _context.UserInfo.CountAsync(user => user.CreatedAt >= thirtyDaysAgo.UtcDateTime),
            ActiveUsersLast30Days = await completed
                .Where(session => session.CompletedAtUtc >= thirtyDaysAgo)
                .Select(session => session.Pass.Split.UserId)
                .Distinct()
                .CountAsync(),
            CompletedSessionsLast7Days = await completed.CountAsync(session => session.CompletedAtUtc >= sevenDaysAgo),
            CompletedSessionsLast30Days = await completed.CountAsync(session => session.CompletedAtUtc >= thirtyDaysAgo),
            VisibleTemplates = await _context.SplitTemplate.CountAsync(template => template.Visability),
            RecentUsers = recentUsers.Select(user => new AdminUserDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                Roles = AppRoles.Parse(user.Roles),
            }).ToList(),
            RecentSessions = recentSessions,
        });
    }

    [HttpGet("users")]
    public async Task<ActionResult<PagedResultDto<AdminUserDto>>> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _context.UserInfo.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(user => user.UserName.Contains(term) || user.Email.Contains(term));
        }

        var totalCount = await query.CountAsync();
        var rows = await query
            .OrderBy(user => user.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new
            {
                user.UserId,
                user.UserName,
                user.Email,
                user.CreatedAt,
                user.Roles,
            })
            .ToListAsync();

        return Ok(new PagedResultDto<AdminUserDto>
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            Items = rows.Select(user => new AdminUserDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                Roles = AppRoles.Parse(user.Roles),
            }).ToList(),
        });
    }

    [HttpPut("users/{userId:guid}/admin-role")]
    public async Task<IActionResult> SetAdminRole(Guid userId, SetUserAdminRequest request)
    {
        var user = await _context.UserInfo.FindAsync(userId);
        if (user is null)
            return NotFound();

        var roles = AppRoles.Parse(user.Roles);
        if (request.IsAdmin)
        {
            if (!roles.Contains(AppRoles.Admin, StringComparer.OrdinalIgnoreCase))
                roles.Add(AppRoles.Admin);
        }
        else
        {
            if (userId == CurrentUserId)
                return Conflict("Du kan inte ta bort din egen adminroll.");

            roles.RemoveAll(role => role.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase));
        }

        if (!roles.Contains(AppRoles.User, StringComparer.OrdinalIgnoreCase))
            roles.Insert(0, AppRoles.User);

        user.Roles = string.Join(',', roles.Distinct(StringComparer.OrdinalIgnoreCase));
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
