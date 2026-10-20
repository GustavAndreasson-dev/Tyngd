using Tyngd.Client.Services;

namespace Tyngd.Tests;

public sealed class RestTimerStateTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "tyngd-rest-" + Guid.NewGuid());

    [Fact]
    public async Task A_running_rest_survives_a_restart_and_remembers_its_pass()
    {
        var storage = new FileAppStorage(_folder);
        var timer = new RestTimerState(storage);
        timer.SetSeconds(90);
        await timer.StartAsync(splitId: 5, passId: 14);

        var restarted = new RestTimerState(new FileAppStorage(_folder));
        await restarted.RestoreAsync();

        Assert.True(restarted.IsResting);
        Assert.Equal((5, 14), (restarted.SplitId, restarted.PassId));
        Assert.InRange(restarted.RemainingSeconds, 85, 90);
        await timer.CancelAsync();
        await restarted.CancelAsync();
    }

    [Fact]
    public async Task Cancelling_removes_the_stored_rest_without_finishing()
    {
        var storage = new FileAppStorage(_folder);
        var timer = new RestTimerState(storage);
        var finished = false;
        var cancelled = false;
        timer.Finished += () => finished = true;
        timer.Cancelled += () => cancelled = true;

        await timer.StartAsync(1, 2);
        await timer.CancelAsync();

        Assert.False(timer.IsResting);
        Assert.True(cancelled);
        Assert.False(finished);
        Assert.Empty(await storage.KeysAsync());
    }

    [Fact]
    public async Task Finished_fires_when_the_rest_runs_out()
    {
        var timer = new RestTimerState(new FileAppStorage(_folder));
        var finished = new TaskCompletionSource();
        timer.Finished += () => finished.TrySetResult();
        timer.SetSeconds(1);

        await timer.StartAsync(1, 2);

        Assert.Same(finished.Task, await Task.WhenAny(finished.Task, Task.Delay(5000)));
        Assert.False(timer.IsResting);
    }

    [Fact]
    public async Task An_expired_stored_rest_is_dropped_on_restore()
    {
        var storage = new FileAppStorage(_folder);
        await storage.SetItemAsync("rest-timer", new RestTimerState.StoredRest(DateTimeOffset.UtcNow.AddMinutes(-1), 120, 1, 2));

        var timer = new RestTimerState(storage);
        await timer.RestoreAsync();

        Assert.False(timer.IsResting);
        Assert.Empty(await storage.KeysAsync());
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }
}
