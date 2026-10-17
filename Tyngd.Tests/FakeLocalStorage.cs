using System.Text.Json;
using Blazored.LocalStorage;

namespace Tyngd.Tests;

/// <summary>localStorage i minnet. Värden lagras som JSON, som i webbläsaren.</summary>
public sealed class FakeLocalStorage : ILocalStorageService
{
    public Dictionary<string, string> Items { get; } = new();

    public event EventHandler<ChangingEventArgs>? Changing { add { } remove { } }
    public event EventHandler<ChangedEventArgs>? Changed { add { } remove { } }

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        Items.Clear();
        return ValueTask.CompletedTask;
    }

    public ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Items.TryGetValue(key, out var json) ? JsonSerializer.Deserialize<T>(json) : default);

    public ValueTask<string?> GetItemAsStringAsync(string key, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Items.GetValueOrDefault(key));

    public ValueTask<string?> KeyAsync(int index, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<string?>(Items.Keys.ElementAtOrDefault(index));

    public ValueTask<IEnumerable<string>> KeysAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<IEnumerable<string>>(Items.Keys.ToList());

    public ValueTask<bool> ContainKeyAsync(string key, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Items.ContainsKey(key));

    public ValueTask<int> LengthAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Items.Count);

    public ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
    {
        Items.Remove(key);
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveItemsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        foreach (var key in keys)
            Items.Remove(key);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default)
    {
        Items[key] = JsonSerializer.Serialize(data);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default)
    {
        Items[key] = data;
        return ValueTask.CompletedTask;
    }
}
