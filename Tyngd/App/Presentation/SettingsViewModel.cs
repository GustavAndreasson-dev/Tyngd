using System.Reflection;
using Microsoft.UI.Xaml;
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
        _isRestTimerEnabled = restTimer.IsEnabled;
        _restTimerMinutes = restTimer.Seconds / 60;
    }

    // "2026.10.17.1605+<hash>" från bygget blir "2026.10.17.1605 · 267123a", som i webbappen.
    public string AppVersion { get; } = "Version " + FormatVersion(
        typeof(SettingsViewModel).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    // Kopia av Installningar.FormatVersion i webbappen (Tyngd.Client refereras inte av appen).
    public static string FormatVersion(string? informational)
    {
        if (string.IsNullOrEmpty(informational)) return "okänd";
        var parts = informational.Split('+', 2);
        return parts.Length == 2 ? $"{parts[0]} · {parts[1][..Math.Min(7, parts[1].Length)]}" : parts[0];
    }

    public string UserName => $"Inloggad som {_loginState.CurrentUser?.UserName}";

    public string? Email => _loginState.CurrentUser?.Email;

    // Admin finns bara i webbappen; visa en hänvisning dit för administratörer.
    public Visibility AdminNoteVisibility =>
        _loginState.CurrentUser?.IsAdmin == true ? Visibility.Visible : Visibility.Collapsed;

    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private bool _isRestTimerEnabled;

    [ObservableProperty]
    private double _restTimerMinutes;

    [ObservableProperty]
    private string? _savedMessage;

    partial void OnIsDarkModeChanged(bool value)
    {
        _themeState.SetDarkMode(value);
        _ = PersistSettingsAsync();
    }

    partial void OnIsRestTimerEnabledChanged(bool value)
    {
        _restTimer.SetEnabled(value);
        _ = PersistSettingsAsync();
    }

    partial void OnRestTimerMinutesChanged(double value)
    {
        // NumberBox ger NaN när fältet töms; spara bara giltiga värden (1–10 som i webbappen).
        if (double.IsNaN(value) || value < 1 || value > 10)
            return;

        _restTimer.SetSeconds((int)value * 60);
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
            SavedMessage = "Inställningarna har sparats.";
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
