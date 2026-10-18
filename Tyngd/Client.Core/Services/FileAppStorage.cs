using System.Text.Json;

namespace Tyngd.Client.Services;

/// <summary>
/// <see cref="IAppStorage"/> som sparar varje nyckel som en JSON-fil i en mapp, t.ex. appens privata lagring på telefonen.
/// </summary>
// shortcut: JWT-token ligger i appens privata mapp, inte i Keystore/Keychain – byt till säker lagring före release.
public class FileAppStorage : IAppStorage
{
    private const string Extension = ".json";
    private readonly string _folder;
    // En filoperation i taget: samtidiga sparningar av samma nyckel krockar annars i filsystemet.
    private readonly SemaphoreSlim _lock = new(1, 1);

    public FileAppStorage(string folder)
    {
        _folder = folder;
        Directory.CreateDirectory(folder);
    }

    public async ValueTask<T?> GetItemAsync<T>(string key)
    {
        var path = PathFor(key);
        await _lock.WaitAsync();
        try
        {
            if (!File.Exists(path))
                return default;

            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask SetItemAsync<T>(string key, T value)
    {
        // Skriv till en temporär fil och byt sedan, så att en krasch mitt i inte lämnar trasig passdata.
        var path = PathFor(key);
        var temp = path + ".tmp";
        await _lock.WaitAsync();
        try
        {
            await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(value));
            File.Move(temp, path, overwrite: true);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask RemoveItemAsync(string key)
    {
        var path = PathFor(key);
        await _lock.WaitAsync();
        try
        {
            File.Delete(path);
        }
        finally
        {
            _lock.Release();
        }
    }

    public ValueTask<IEnumerable<string>> KeysAsync() =>
        ValueTask.FromResult(Directory.EnumerateFiles(_folder, "*" + Extension)
            .Select(path => Path.GetFileNameWithoutExtension(path)!)
            .ToList()
            .AsEnumerable());

    private string PathFor(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || key.Contains(".."))
            throw new ArgumentException($"Ogiltig lagringsnyckel: '{key}'.", nameof(key));

        return Path.Combine(_folder, key + Extension);
    }
}
