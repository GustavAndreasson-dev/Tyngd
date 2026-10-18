namespace Tyngd.App.Presentation;

/// <summary>Appens skal (bottenmenyn). Läser användarens inställningar vid start, som MainLayout i webbappen.</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ThemeState _themeState;
    private readonly RestTimerState _restTimer;
    private readonly UserSettingsService _userSettingsService;

    public MainViewModel(ThemeState themeState, RestTimerState restTimer, UserSettingsService userSettingsService)
    {
        _themeState = themeState;
        _restTimer = restTimer;
        _userSettingsService = userSettingsService;
        _ = LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        await _restTimer.RestoreAsync();

        var settings = await _userSettingsService.GetSettingsAsync();
        if (settings is null)
            return;

        _themeState.SetDarkMode(settings.IsDarkMode);
        _restTimer.SetEnabled(settings.IsRestTimerEnabled);
        _restTimer.SetSeconds(settings.RestTimerSeconds);
    }
}
