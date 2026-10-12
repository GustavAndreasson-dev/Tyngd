using Tyngd.Client.Models;
using Tyngd.Client.Services;

namespace Tyngd.Tests;

public class PassResumePolicyTests
{
    [Fact]
    public void Resumes_at_exactly_twelve_hours()
    {
        var now = new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero);
        var data = ActivePass(now.AddHours(-12));

        Assert.True(PassResumePolicy.ShouldResume(data, now));
    }

    [Fact]
    public void Resumes_older_session_from_same_local_day()
    {
        var now = new DateTimeOffset(2026, 7, 15, 20, 0, 0, TimeSpan.Zero);
        var data = ActivePass(now.AddHours(-14));

        Assert.True(PassResumePolicy.ShouldResume(data, now));
    }

    [Fact]
    public void Does_not_resume_old_session_from_previous_day()
    {
        var now = new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero);
        var data = ActivePass(now.AddHours(-20));

        Assert.False(PassResumePolicy.ShouldResume(data, now));
    }

    [Fact]
    public void Completed_session_never_resumes()
    {
        var now = DateTimeOffset.UtcNow;
        var data = ActivePass(now.AddMinutes(-5));
        data.CompletedAtUtc = now;

        Assert.False(PassResumePolicy.ShouldResume(data, now));
    }

    private static PassData ActivePass(DateTimeOffset startedAt) => new()
    {
        SessionKey = Guid.NewGuid(),
        StartedAtUtc = startedAt,
    };
}
