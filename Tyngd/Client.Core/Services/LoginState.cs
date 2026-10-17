using Tyngd.Shared.Users;

namespace Tyngd.Client.Services;

/// <summary>
/// Singleton-tjänst som håller inloggningsstatus globalt.
/// Persisterar inloggad användare och JWT-token i localStorage.
/// </summary>
public class LoginState
{
    private const string UserKey  = "current-user";
    private const string TokenKey = "current-token";
    private readonly IAppStorage _localStorage;
    private readonly SessionState _sessionState;

    public LoginState(IAppStorage localStorage, SessionState sessionState)
    {
        _localStorage = localStorage;
        _sessionState = sessionState;
    }

    public UserInfo? CurrentUser { get; private set; }
    public string?   Token       { get; private set; }
    public bool      IsLoggedIn  => CurrentUser is not null;

    public event Action? OnChange;

    /// <summary>Läser inloggad användare och token från localStorage vid app-start.</summary>
    public async Task InitializeAsync()
    {
        CurrentUser = await _localStorage.GetItemAsync<UserInfo>(UserKey);
        Token       = await _localStorage.GetItemAsync<string>(TokenKey);
        OnChange?.Invoke();
    }

    /// <summary>Loggar in en användare och sparar i localStorage.</summary>
    public async Task LoginAsync(UserInfo user, string token)
    {
        CurrentUser = user;
        Token       = token;
        await _localStorage.SetItemAsync(UserKey,  user);
        await _localStorage.SetItemAsync(TokenKey, token);
        OnChange?.Invoke();
    }

    /// <summary>
    /// Loggar ut den inloggade användaren och tar bort från localStorage. När token har gått ut
    /// (<paramref name="keepPassData"/>) behålls pågående pass, så osynkade set finns kvar efter ny inloggning.
    /// </summary>
    public async Task LogoutAsync(bool keepPassData = false)
    {
        CurrentUser = null;
        Token       = null;
        await _localStorage.RemoveItemAsync(UserKey);
        await _localStorage.RemoveItemAsync(TokenKey);
        if (!keepPassData)
        {
            _sessionState.EndSession();
            await _localStorage.RemoveItemAsync("exercise-weights");
            await ClearPassDataAsync();
        }
        OnChange?.Invoke();
    }

    /// <summary>Tar bort alla pass-{SplitId}-{PassId}-nycklar från localStorage.</summary>
    private async Task ClearPassDataAsync()
    {
        var keys = await _localStorage.KeysAsync();
        foreach (var key in keys.Where(k => k.StartsWith("pass-")))
            await _localStorage.RemoveItemAsync(key);
    }
}
