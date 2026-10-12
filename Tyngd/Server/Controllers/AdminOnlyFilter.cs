using System.Security.Claims;
using Tyngd.Shared.Users;
using Tyngd.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Tyngd.Server.Controllers;

/// <summary>
/// Kontrollerar mot databasen att inloggad användare fortfarande är admin. Rollen i JWT-token kan
/// vara upp till ExpiryMinutes gammal, så en nedgraderad admin stoppas här.
/// </summary>
public sealed class AdminOnlyFilter(TyngdContext context) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext executing, ActionExecutionDelegate next)
    {
        var user = executing.HttpContext.User;
        var userId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"), out var id)
            ? id
            : Guid.Empty;

        var roles = await context.UserInfo
            .Where(info => info.UserId == userId)
            .Select(info => info.Roles)
            .SingleOrDefaultAsync();

        if (!AppRoles.Parse(roles).Contains(AppRoles.Admin, StringComparer.OrdinalIgnoreCase))
        {
            executing.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
