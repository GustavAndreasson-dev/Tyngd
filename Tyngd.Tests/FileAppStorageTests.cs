using Tyngd.Client.Models;
using Tyngd.Client.Services;

namespace Tyngd.Tests;

public sealed class FileAppStorageTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "tyngd-storage-" + Guid.NewGuid());

    [Fact]
    public async Task Stores_reads_lists_and_removes_items()
    {
        var storage = new FileAppStorage(_folder);
        await storage.SetItemAsync("pass-1-2", new PassData { IsSynced = false, Reps = { [7] = [10, 8] } });
        await storage.SetItemAsync("current-token", "abc");

        var pass = await storage.GetItemAsync<PassData>("pass-1-2");
        Assert.Equal([10, 8], pass!.Reps[7]);
        Assert.Equal("abc", await storage.GetItemAsync<string>("current-token"));
        Assert.Equal(["current-token", "pass-1-2"], (await storage.KeysAsync()).Order());

        await storage.RemoveItemAsync("pass-1-2");
        Assert.Null(await storage.GetItemAsync<PassData>("pass-1-2"));
        Assert.Equal(["current-token"], await storage.KeysAsync());
    }

    [Fact]
    public async Task Missing_key_returns_default_and_bad_keys_are_rejected()
    {
        var storage = new FileAppStorage(_folder);

        Assert.Null(await storage.GetItemAsync<int?>("schema-version"));
        await Assert.ThrowsAsync<ArgumentException>(async () => await storage.SetItemAsync("../utanför", 1));
    }

    [Fact]
    public async Task Concurrent_saves_of_the_same_key_do_not_collide()
    {
        var storage = new FileAppStorage(_folder);

        await Task.WhenAll(Enumerable.Range(0, 20).Select(i => Task.Run(async () => await storage.SetItemAsync("pass-1-2", i))));

        Assert.InRange(await storage.GetItemAsync<int>("pass-1-2"), 0, 19);
        Assert.Equal(["pass-1-2"], await storage.KeysAsync());
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }
}
