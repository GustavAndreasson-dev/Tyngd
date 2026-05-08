using Blazored.LocalStorage;

namespace ArnoldOne.Client.Services;

/// <summary>
/// Hanterar vilka splits som en inloggad användare har aktiverat.
/// Lagrar i localStorage under nyckeln "user-splits-{userId}".
/// Ersätts med databasanrop mot API när backend finns.
/// </summary>
public class UserSplitsService
{
    private readonly ILocalStorageService _localStorage;

    public UserSplitsService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    /// <summary>
    /// Returnerar alla aktiverade split-ids för en användare.
    /// </summary>
    public async Task<HashSet<int>> GetActiveSplitsAsync(string userId)
    {
        var key = GetKey(userId);
        var stored = await _localStorage.GetItemAsync<List<int>?>(key);
        return stored is not null ? new HashSet<int>(stored) : new HashSet<int>();
    }

    /// <summary>
    /// Kontrollerar om en split är aktiverad för användaren.
    /// </summary>
    public async Task<bool> IsSplitActiveAsync(string userId, int splitId)
    {
        var active = await GetActiveSplitsAsync(userId);
        return active.Contains(splitId);
    }

    /// <summary>
    /// Aktiverar en split för användaren.
    /// </summary>
    public async Task ActivateSplitAsync(string userId, int splitId)
    {
        var active = await GetActiveSplitsAsync(userId);
        active.Add(splitId);
        await _localStorage.SetItemAsync(GetKey(userId), active.ToList());
    }

    /// <summary>
    /// Avaktiverar en split för användaren.
    /// </summary>
    public async Task DeactivateSplitAsync(string userId, int splitId)
    {
        var active = await GetActiveSplitsAsync(userId);
        active.Remove(splitId);
        await _localStorage.SetItemAsync(GetKey(userId), active.ToList());
    }

    private static string GetKey(string userId) => $"user-splits-{userId}";
}
