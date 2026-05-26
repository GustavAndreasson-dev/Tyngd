using ArnoldOne.Shared.Users;
using Blazored.LocalStorage;

namespace ArnoldOne.Client.Services;

/// <summary>
/// Singleton-tjänst som håller inloggningsstatus globalt.
/// Persisterar inloggad användare och JWT-token i localStorage.
/// </summary>
public class LoginState
{
    private const string UserKey  = "current-user";
    private const string TokenKey = "current-token";
    private readonly ILocalStorageService _localStorage;

    public LoginState(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
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

    /// <summary>Loggar ut den inloggade användaren och tar bort från localStorage.</summary>
    public async Task LogoutAsync()
    {
        CurrentUser = null;
        Token       = null;
        await _localStorage.RemoveItemAsync(UserKey);
        await _localStorage.RemoveItemAsync(TokenKey);
        OnChange?.Invoke();
    }
}
