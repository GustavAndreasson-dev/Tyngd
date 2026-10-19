using Tyngd.Client.Models;
using Tyngd.Client.Services;
using Tyngd.Shared.DTOs;
using Tyngd.Shared.TrainingPrograms;

namespace Tyngd.Tests;

public class DashboardPassFinderTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);

    private static UserDashboardDto Dashboard() => new()
    {
        ActiveSplits =
        [
            new TrainingSplit
            {
                SplitId = 1, SplitName = "PPL",
                Passes = [new() { PassId = 10, PassName = "Push" }, new() { PassId = 11, PassName = "Pull" }],
            },
        ],
    };

    [Fact]
    public async Task Picks_the_latest_started_resumable_local_pass()
    {
        var storage = new BlazoredAppStorage(new FakeLocalStorage());
        await storage.SetItemAsync(PassKey.Format(1, 10), new PassData { StartedAtUtc = Now.AddHours(-2) });
        await storage.SetItemAsync(PassKey.Format(1, 11), new PassData { StartedAtUtc = Now.AddHours(-1) });
        await storage.SetItemAsync(PassKey.Format(9, 99), new PassData { StartedAtUtc = Now });
        await storage.SetItemAsync("current-token", "x");

        var active = await DashboardPassFinder.FindActivePassAsync(Dashboard(), new SessionState(), storage, Now);

        Assert.Equal(new DashboardPass(1, "PPL", 11, "Pull"), active);
    }

    [Fact]
    public async Task Ignores_completed_local_pass()
    {
        var storage = new BlazoredAppStorage(new FakeLocalStorage());
        await storage.SetItemAsync(PassKey.Format(1, 10), new PassData { StartedAtUtc = Now.AddHours(-1), CompletedAtUtc = Now });

        Assert.Null(await DashboardPassFinder.FindActivePassAsync(Dashboard(), new SessionState(), storage, Now));
    }

    [Fact]
    public void Next_pass_follows_the_last_completed_and_wraps()
    {
        var dashboard = Dashboard();
        Assert.Equal(10, DashboardPassFinder.FindNextPass(dashboard)!.PassId);

        dashboard.LastCompletedSession = new DashboardSessionDto { SplitId = 1, PassId = 10 };
        Assert.Equal(11, DashboardPassFinder.FindNextPass(dashboard)!.PassId);

        dashboard.LastCompletedSession.PassId = 11;
        Assert.Equal(10, DashboardPassFinder.FindNextPass(dashboard)!.PassId);
    }

    [Fact]
    public void Week_runs_monday_to_sunday_and_marks_done_and_today()
    {
        var wednesday = new DateTime(2026, 7, 15, 9, 0, 0, DateTimeKind.Local);
        var dashboard = Dashboard();
        dashboard.RecentCompletedAtUtc = [new DateTimeOffset(new DateTime(2026, 7, 13, 18, 0, 0, DateTimeKind.Local))];

        var week = DashboardPassFinder.WeekDays(dashboard, wednesday);

        Assert.Equal(["M", "T", "O", "T", "F", "L", "S"], week.Select(d => d.Label));
        Assert.Equal([true, false, false, false, false, false, false], week.Select(d => d.Done));
        Assert.Equal(2, week.ToList().FindIndex(d => d.Today));
    }

    [Theory]
    [InlineData("pass-3-7", true, 3, 7)]
    [InlineData("pass-3", false, 0, 0)]
    [InlineData("other-3-7", false, 3, 0)]
    public void Pass_key_round_trips(string key, bool ok, int splitId, int passId)
    {
        Assert.Equal(ok, PassKey.TryParse(key, out var s, out var p));
        if (ok)
            Assert.Equal((splitId, passId), (s, p));
    }

    [Fact]
    public void Long_date_is_swedish() =>
        Assert.Equal("onsdag 15 juli", SvDate.Long(new DateTime(2026, 7, 15)));
}
