using Tyngd.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedSettings = Tyngd.Shared.Users.UserSettings;
using DbSettings = Tyngd.Data.Models.UserSettings;

namespace Tyngd.Server.Controllers;

[Route("api/[controller]")]
public class UserSettingsController : ApiControllerBase
{
    private readonly TyngdContext _context;

    public UserSettingsController(TyngdContext context)
    {
        _context = context;
    }

    /// <summary>Hämtar inställningar för inloggad användare. Returnerar defaults om ingen rad finns.</summary>
    [HttpGet]
    public async Task<ActionResult<SharedSettings>> GetSettings()
    {
        var userId = CurrentUserId;

        var settings = await _context.UserSettings.FindAsync(userId);

        return Ok(settings is null ? new SharedSettings
        {
            UserId             = userId.ToString(),
            IsDarkMode         = false,
            IsRestTimerEnabled = true,
            RestTimerSeconds   = 120,
        } : new SharedSettings
        {
            UserId             = settings.UserId.ToString(),
            IsDarkMode         = settings.IsDarkMode,
            IsRestTimerEnabled = settings.IsRestTimerEnabled,
            RestTimerSeconds   = settings.RestTimerSeconds,
        });
    }

    /// <summary>Sparar inställningar för inloggad användare (upsert).</summary>
    [HttpPut]
    public async Task<IActionResult> SaveSettings([FromBody] SharedSettings dto)
    {
        var userId = CurrentUserId;

        var existing = await _context.UserSettings.FindAsync(userId);

        if (existing is null)
        {
            _context.UserSettings.Add(new DbSettings
            {
                UserId             = userId,
                IsDarkMode         = dto.IsDarkMode,
                IsRestTimerEnabled = dto.IsRestTimerEnabled,
                RestTimerSeconds   = dto.RestTimerSeconds,
            });
        }
        else
        {
            existing.IsDarkMode         = dto.IsDarkMode;
            existing.IsRestTimerEnabled = dto.IsRestTimerEnabled;
            existing.RestTimerSeconds   = dto.RestTimerSeconds;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }
}
