using ArnoldOne.Shared.Users;

namespace ArnoldOne.Client.Services;

/// <summary>
/// Singleton-tjänst som håller inloggningsstatus globalt.
/// Ersätts med riktiga API-anrop när autentisering är implementerat.
/// </summary>
public class LoginState
{
    /// <summary>Inloggad användare, eller null om ingen är inloggad.</summary>
    public UserInfo? CurrentUser { get; private set; }

    /// <summary>Anger om en användare är inloggad.</summary>
    public bool IsLoggedIn => CurrentUser is not null;

    /// <summary>Utlöses när inloggningsstatus ändras.</summary>
    public event Action? OnChange;

    /// <summary>Loggar in en användare och meddelar prenumeranter.</summary>
    public void Login(UserInfo user)
    {
        CurrentUser = user;
        OnChange?.Invoke();
    }

    /// <summary>Loggar ut den inloggade användaren och meddelar prenumeranter.</summary>
    public void Logout()
    {
        CurrentUser = null;
        OnChange?.Invoke();
    }
}
