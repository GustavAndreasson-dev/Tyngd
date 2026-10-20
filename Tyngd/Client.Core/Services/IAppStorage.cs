namespace Tyngd.Client.Services;

/// <summary>
/// Lokal nyckel/värde-lagring på enheten: localStorage i webbappen, appens egen lagring i Uno-appen.
/// Värden serialiseras som JSON.
/// </summary>
public interface IAppStorage
{
    ValueTask<T?> GetItemAsync<T>(string key);
    ValueTask SetItemAsync<T>(string key, T value);
    ValueTask RemoveItemAsync(string key);
    ValueTask<IEnumerable<string>> KeysAsync();
}
