using System.Text.Json;
using Tyngd.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Tyngd.Server.Controllers;

[Route("api/account")]
public sealed class AccountController(TyngdContext context) : ApiControllerBase
{
    private static readonly JsonSerializerOptions ExportJson = new(JsonSerializerOptions.Web) { WriteIndented = true };

    /// <summary>
    /// Allt som sparats om användaren som en JSON-fil (rätt till dataportabilitet), även arkiverade splits,
    /// pass och övningar och de lösa passen.
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        var userId = CurrentUserId;
        var account = await context.UserInfo.AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new { u.UserName, u.Email, u.CreatedAt })
            .SingleAsync();
        var settings = await context.UserSettings.AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => new { s.IsDarkMode, s.IsRestTimerEnabled, s.RestTimerSeconds })
            .SingleOrDefaultAsync();
        var favorites = await context.UserFavoriteExercise.AsNoTracking()
            .Where(f => f.UserId == userId)
            .Select(f => f.CatalogExercise!.NameSv ?? f.CatalogExercise.NameEn)
            .ToListAsync();
        var splits = await context.Split.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.SplitId)
            .Select(s => new
            {
                Name = s.SplitName,
                Archived = !s.Visability,
                FromTemplate = s.SplitTemplateId != null,
                FreeSessions = s.IsFreeSessions,
                Passes = s.Pass.OrderBy(p => p.Ordning).Select(p => new
                {
                    Name = p.PassName,
                    Archived = p.Visability != 1,
                    p.Color,
                    Exercises = p.Exercise.OrderBy(e => e.Ordning).Select(e => new
                    {
                        Name = e.ExerciseName,
                        Archived = !e.Visability,
                        Sets = e.DefaultSets,
                        RepsMin = e.DefautlRepsMin,
                        RepsMax = e.DefaultRepsMax,
                        Weight = e.DefaultWeight,
                    }).ToList(),
                    Sessions = p.Session.OrderBy(se => se.StartedAtUtc).Select(se => new
                    {
                        se.Date,
                        se.StartedAtUtc,
                        se.CompletedAtUtc,
                        Exercises = se.ExerciseLog.Select(l => new
                        {
                            Name = l.Exercise.ExerciseName,
                            Note = l.NoteText,
                            Sets = l.SetLog.OrderBy(sl => sl.SetIndex).Select(sl => new { Set = sl.SetIndex + 1, sl.Reps, sl.Weight, Done = sl.IsDone }).ToList(),
                        }).ToList(),
                    }).ToList(),
                }).ToList(),
            })
            .AsSplitQuery()
            .ToListAsync();

        var export = new { ExportedAtUtc = DateTimeOffset.UtcNow, Account = account, Settings = settings, Favorites = favorites, Splits = splits };
        return File(JsonSerializer.SerializeToUtf8Bytes(export, ExportJson), "application/json",
            $"tyngd-data-{DateTime.UtcNow:yyyy-MM-dd}.json");
    }
}
