using Tyngd.Shared.Users;

namespace Tyngd.App.Presentation;

public partial class MainViewModel : ObservableObject
{
    private readonly LoginState _loginState;
    private readonly ThemeState _themeState;
    private readonly UserSettingsService _userSettingsService;
    private UserSettings? _settings;

    public MainViewModel(LoginState loginState, ThemeState themeState, UserSettingsService userSettingsService)
    {
        _loginState = loginState;
        _themeState = themeState;
        _userSettingsService = userSettingsService;
        _isDarkMode = themeState.IsDarkMode;
        _ = LoadSettingsAsync();
    }

    public string UserName => $"Inloggad som {_loginState.CurrentUser?.UserName}";

    public string Greeting => $"Hej {_loginState.CurrentUser?.UserName}!";

    [ObservableProperty]
    private bool _isDarkMode;

    /// <summary>Hämtar användarens sparade tema, som MainLayout gör i webbappen.</summary>
    private async Task LoadSettingsAsync()
    {
        _settings = await _userSettingsService.GetSettingsAsync();
        if (_settings is not null)
            IsDarkMode = _settings.IsDarkMode;
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        _themeState.SetDarkMode(value);
        _ = SaveDarkModeAsync(value);
    }

    private async Task SaveDarkModeAsync(bool value)
    {
        // Spara bara ovanpå hämtade inställningar, annars skrivs vilotimern över med standardvärden.
        if (_settings is null || _settings.IsDarkMode == value)
            return;

        _settings.IsDarkMode = value;
        try
        {
            await _userSettingsService.SaveSettingsAsync(_settings);
        }
        catch (HttpRequestException)
        {
            // Offline: temat gäller ändå lokalt och sparas nästa gång det ändras.
        }
    }

    // App.xaml.cs går tillbaka till inloggningen när LoginState loggas ut.
    [RelayCommand]
    private Task LogoutAsync() => _loginState.LogoutAsync();
}
