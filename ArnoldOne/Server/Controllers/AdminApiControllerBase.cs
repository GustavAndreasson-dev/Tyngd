using ArnoldOne.Shared.Users;
using DbArnold.DbArnold.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ArnoldOne.Server.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public abstract class AdminApiControllerBase : ApiControllerBase
{
    protected async Task<bool> IsCurrentAdminAsync(ArnoldContext context)
    {
        var roles = await context.UserInfo
            .Where(user => user.UserId == CurrentUserId)
            .Select(user => user.Roles)
            .SingleOrDefaultAsync();

        return ParseRoles(roles).Contains(AppRoles.Admin, StringComparer.OrdinalIgnoreCase);
    }

    protected static List<string> ParseRoles(string? roles) =>
        roles?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
        ?? new List<string>();
}
