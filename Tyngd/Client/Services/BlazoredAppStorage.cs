using Blazored.LocalStorage;

namespace Tyngd.Client.Services;

/// <summary><see cref="IAppStorage"/> ovanpå webbläsarens localStorage.</summary>
public class BlazoredAppStorage(ILocalStorageService localStorage) : IAppStorage
{
    public ValueTask<T?> GetItemAsync<T>(string key) => localStorage.GetItemAsync<T>(key);
    public ValueTask SetItemAsync<T>(string key, T value) => localStorage.SetItemAsync(key, value);
    public ValueTask RemoveItemAsync(string key) => localStorage.RemoveItemAsync(key);
    public ValueTask<IEnumerable<string>> KeysAsync() => localStorage.KeysAsync();
}
