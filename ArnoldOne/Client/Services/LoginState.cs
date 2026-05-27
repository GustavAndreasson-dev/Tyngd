using ArnoldOne.Shared.Users;
using Blazored.LocalStorage;
using Microsoft.Extensions.DependencyInjection;

namespace ArnoldOne.Client.Services;

/// <summary>
/// Singleton-tjänst som håller inloggningsstatus globalt.
/// Persisterar inloggad användare och JWT-token i localStorage.
/// </summary>
public class LoginState
{
    private const string UserKey  = "current-user";
    private const string TokenKey = "current-token";
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SessionState _sessionState;

    public LoginState(IServiceScopeFactory scopeFactory, SessionState sessionState)
    {
        _scopeFactory = scopeFactory;
        _sessionState = sessionState;
    }

    public UserInfo? CurrentUser { get; private set; }
    public string?   Token       { get; private set; }
    public bool      IsLoggedIn  => CurrentUser is not null;

    public event Action? OnChange;

    /// <summary>Läser inloggad användare och token från localStorage vid app-start.</summary>
    public async Task InitializeAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var localStorage = scope.ServiceProvider.GetRequiredService<ILocalStorageService>();
        CurrentUser = await localStorage.GetItemAsync<UserInfo>(UserKey);
        Token       = await localStorage.GetItemAsync<string>(TokenKey);
        OnChange?.Invoke();
    }

    /// <summary>Loggar in en användare och sparar i localStorage.</summary>
    public async Task LoginAsync(UserInfo user, string token)
    {
        CurrentUser = user;
        Token       = token;
        using var scope = _scopeFactory.CreateScope();
        var localStorage = scope.ServiceProvider.GetRequiredService<ILocalStorageService>();
        await localStorage.SetItemAsync(UserKey,  user);
        await localStorage.SetItemAsync(TokenKey, token);
        OnChange?.Invoke();
    }

    /// <summary>Loggar ut den inloggade användaren och tar bort från localStorage.</summary>
    public async Task LogoutAsync()
    {
        CurrentUser = null;
        Token       = null;
        _sessionState.EndSession();
        using var scope = _scopeFactory.CreateScope();
        var localStorage = scope.ServiceProvider.GetRequiredService<ILocalStorageService>();
        await localStorage.RemoveItemAsync(UserKey);
        await localStorage.RemoveItemAsync(TokenKey);
        await localStorage.RemoveItemAsync("exercise-weights");
        await ClearPassDataAsync(localStorage);
        OnChange?.Invoke();
    }

    /// <summary>Tar bort alla pass-{SplitId}-{PassId}-nycklar från localStorage.</summary>
    private static async Task ClearPassDataAsync(ILocalStorageService localStorage)
    {
        var keys = await localStorage.KeysAsync();
        foreach (var key in keys.Where(k => k.StartsWith("pass-")))
            await localStorage.RemoveItemAsync(key);
    }
}
