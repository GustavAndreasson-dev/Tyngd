using ArnoldOne.Client.Models;

namespace ArnoldOne.Client.Services;

public static class PassResumePolicy
{
    public static readonly TimeSpan ResumeWindow = TimeSpan.FromHours(12);

    public static bool ShouldResume(PassData? data, DateTimeOffset now)
    {
        if (data?.StartedAtUtc is null || data.CompletedAtUtc is not null)
            return false;

        var started = data.StartedAtUtc.Value;
        var age = now.ToUniversalTime() - started.ToUniversalTime();
        if (age < TimeSpan.FromMinutes(-5))
            return false;

        var sameLocalDate = started.ToLocalTime().Date == now.ToLocalTime().Date;
        return sameLocalDate || age <= ResumeWindow;
    }
}
