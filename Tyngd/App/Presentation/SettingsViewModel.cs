using Tyngd.Shared.Users;

namespace Tyngd.App.Presentation;

public partial class SettingsViewModel : ObservableObject
{
    private readonly LoginState _loginState;
    private readonly ThemeState _themeState;
    private readonly RestTimerState _restTimer;
    private readonly UserSettingsService _userSettingsService;

    public SettingsViewModel(LoginState loginState, ThemeState themeState, RestTimerState restTimer, UserSettingsService userSettingsService)
    {
        _loginState = loginState;
        _themeState = themeState;
        _restTimer = restTimer;
        _userSettingsService = userSettingsService;
        _isDarkMode = themeState.IsDarkMode;
    }

    public string UserName => $"Inloggad som {_loginState.CurrentUser?.UserName}";

    [ObservableProperty]
    private bool _isDarkMode;

    partial void OnIsDarkModeChanged(bool value)
    {
        _themeState.SetDarkMode(value);
        _ = PersistSettingsAsync();
    }

    /// <summary>Sparar alla inställningar från appens tillstånd, som PersistSettingsAsync i webbappens Inställningar.</summary>
    private async Task PersistSettingsAsync()
    {
        try
        {
            await _userSettingsService.SaveSettingsAsync(new UserSettings
            {
                IsDarkMode         = _themeState.IsDarkMode,
                IsRestTimerEnabled = _restTimer.IsEnabled,
                RestTimerSeconds   = _restTimer.Seconds,
            });
        }
        catch (HttpRequestException)
        {
            // Offline: inställningen gäller ändå lokalt.
        }
    }

    // App.xaml.cs går tillbaka till inloggningen när LoginState loggas ut.
    [RelayCommand]
    private Task LogoutAsync() => _loginState.LogoutAsync();
}
