using Tyngd.Client.Models;
using Tyngd.Shared.DTOs;
using Tyngd.Shared.TrainingPrograms;

namespace Tyngd.Client.Services;

public sealed record DashboardPass(int SplitId, string SplitName, int PassId, string PassName);

/// <summary>Översiktens logik: pågående pass, nästa pass och veckans träningsdagar.</summary>
public static class DashboardPassFinder
{
    private static readonly string[] DayLabels = ["M", "T", "O", "T", "F", "L", "S"];

    /// <summary>Passet i minnet om det finns, annars det senast startade lokala passet som får återupptas.</summary>
    public static async Task<DashboardPass?> FindActivePassAsync(
        UserDashboardDto dashboard, SessionState session, IAppStorage storage, DateTimeOffset nowUtc)
    {
        if (session.IsActive && FindPass(dashboard, session.ActiveSplitId, session.ActivePassId) is { } sessionPass)
            return sessionPass;

        DashboardPass? latestPass = null;
        DateTimeOffset? latestStartedAt = null;

        foreach (var key in await storage.KeysAsync())
        {
            if (!PassKey.TryParse(key, out var splitId, out var passId))
                continue;

            PassData? data;
            try
            {
                data = await storage.GetItemAsync<PassData>(key);
            }
            catch
            {
                // Trasig eller gammal data ska inte stoppa översikten.
                continue;
            }

            if (!PassResumePolicy.ShouldResume(data, nowUtc))
                continue;

            var storedPass = FindPass(dashboard, splitId, passId);
            var startedAt = data!.StartedAtUtc!.Value;
            if (storedPass is null || latestStartedAt.HasValue && startedAt <= latestStartedAt.Value)
                continue;

            latestPass = storedPass;
            latestStartedAt = startedAt;
        }

        return latestPass;
    }

    /// <summary>Passet efter det senast slutförda i samma split, annars första passet.</summary>
    public static DashboardPass? FindNextPass(UserDashboardDto dashboard)
    {
        var firstPass = dashboard.ActiveSplits
            .SelectMany(split => split.Passes.Select(pass => ToDashboardPass(split, pass)))
            .FirstOrDefault();

        if (dashboard.LastCompletedSession is not { } lastSession)
            return firstPass;

        var split = dashboard.ActiveSplits.FirstOrDefault(item => item.SplitId == lastSession.SplitId);
        if (split is null || split.Passes.Count == 0)
            return firstPass;

        var lastPassIndex = split.Passes.FindIndex(pass => pass.PassId == lastSession.PassId);
        if (lastPassIndex < 0)
            return firstPass;

        return ToDashboardPass(split, split.Passes[(lastPassIndex + 1) % split.Passes.Count]);
    }

    /// <summary>Måndag–söndag för veckan som innehåller <paramref name="today"/>, med markering för dagar där ett pass slutförts.</summary>
    public static IReadOnlyList<(string Label, bool Done, bool Today)> WeekDays(UserDashboardDto dashboard, DateTime today)
    {
        today = today.Date;
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var done = dashboard.RecentCompletedAtUtc.Select(d => d.ToLocalTime().Date).ToHashSet();
        return Enumerable.Range(0, 7)
            .Select(i => (DayLabels[i], done.Contains(monday.AddDays(i)), monday.AddDays(i) == today))
            .ToList();
    }

    private static DashboardPass? FindPass(UserDashboardDto dashboard, int splitId, int passId)
    {
        var split = dashboard.ActiveSplits.Append(dashboard.RecentFreeSessions)
            .FirstOrDefault(item => item?.SplitId == splitId);
        var pass = split?.Passes.FirstOrDefault(item => item.PassId == passId);
        return split is not null && pass is not null ? ToDashboardPass(split, pass) : null;
    }

    private static DashboardPass ToDashboardPass(TrainingSplit split, TrainingPass pass) =>
        new(split.SplitId, split.SplitName, pass.PassId, pass.PassName);
}
