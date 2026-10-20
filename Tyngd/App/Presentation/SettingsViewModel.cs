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
    private readonly PendingSyncState _pendingSync;
    private readonly INavigator _navigator;

    // Sparar bara ovanpå inställningar som hämtats från servern: annars skulle standardvärden (t.ex. vilotiden)
    // skriva över användarens sparade inställningar om något ändras innan de laddats eller när appen är offline.
    private bool _settingsLoaded;

    public SettingsViewModel(LoginState loginState, ThemeState themeState, RestTimerState restTimer,
        UserSettingsService userSettingsService, PendingSyncState pendingSync, INavigator navigator, IDispatcher dispatcher)
    {
        RestNotifications.PermissionChanged += () => dispatcher.TryEnqueue(RefreshNotificationState);
        _navigator = navigator;
        _loginState = loginState;
        _themeState = themeState;
        _restTimer = restTimer;
        _userSettingsService = userSettingsService;
        _pendingSync = pendingSync;
        _isDarkMode = themeState.IsDarkMode;
        _isRestTimerEnabled = restTimer.IsEnabled;
        _restTimerMinutes = restTimer.Seconds / 60;
        _ = LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        var settings = await _userSettingsService.GetSettingsAsync();
        if (settings is null)
        {
            SavedMessage = "Kunde inte hämta dina inställningar. Ändringar sparas inte förrän appen är online igen.";
            return;
        }

        IsDarkMode = settings.IsDarkMode;
        IsRestTimerEnabled = settings.IsRestTimerEnabled;
        RestTimerMinutes = settings.RestTimerSeconds / 60;
        _settingsLoaded = true;
    }

    // Android-bygget saknar InformationalVersion; då används paketets version (byggtiden, se Tyngd.App.csproj).
    public string AppVersion { get; } = "Version " + Tyngd.Shared.AppVersion.Format(
        typeof(SettingsViewModel).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? PackageVersion());

    private static string? PackageVersion()
    {
        try
        {
            var v = Windows.ApplicationModel.Package.Current.Id.Version;
            return $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
        }
        catch (Exception)
        {
            return null;
        }
    }

    public string UserName => $"Inloggad som {_loginState.CurrentUser?.UserName}";

    public string? Email => _loginState.CurrentUser?.Email;

    public Visibility AdminNoteVisibility =>
        _loginState.CurrentUser?.IsAdmin == true ? Visibility.Visible : Visibility.Collapsed;

    // Notis när vilan är slut (som webbappens inställning); bara där plattformen har notiser.
    public bool NotificationsSupported { get; } = RestNotifications.IsSupported;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEnableNotifications), nameof(ShowNotificationsOn))]
    private bool _notificationsAllowed = RestNotifications.AreAllowed;

    public bool ShowEnableNotifications => NotificationsSupported && !NotificationsAllowed;

    public bool ShowNotificationsOn => NotificationsSupported && NotificationsAllowed;

    private void RefreshNotificationState() => NotificationsAllowed = RestNotifications.AreAllowed;

    [RelayCommand]
    private void EnableNotifications() => RestNotifications.AskOrOpenSettings();

    [RelayCommand]
    private Task OpenAdminAsync() => _navigator.NavigateViewModelAsync<AdminViewModel>(this);

    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private bool _isRestTimerEnabled;

    [ObservableProperty]
    private int _restTimerMinutes;

    /// <summary>Vilotid 1–10 minuter, som webbappen.</summary>
    public IReadOnlyList<int> RestMinuteChoices { get; } = Enumerable.Range(1, 10).ToList();

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

    partial void OnRestTimerMinutesChanged(int value)
    {
        if (value < 1 || value > 10)
            return;

        _restTimer.SetSeconds(value * 60);
        _ = PersistSettingsAsync();
    }

    /// <summary>Sparar alla inställningar från appens tillstånd, som PersistSettingsAsync i webbappens Inställningar.</summary>
    private async Task PersistSettingsAsync()
    {
        if (!_settingsLoaded)
            return;

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

    /// <summary>
    /// Försöker synka osynkade pass innan utloggning (utloggning raderar dem från telefonen).
    /// Returnerar true om det fortfarande finns osynkade pass.
    /// </summary>
    public async Task<bool> HasUnsyncedPassesAfterSyncAsync()
    {
        await _pendingSync.RefreshAsync();
        if (_pendingSync.HasPendingSync)
            await _pendingSync.SyncVisiblePendingAsync();
        return _pendingSync.HasPendingSync;
    }

    // App.xaml.cs går tillbaka till inloggningen när LoginState loggas ut.
    public Task LogoutAsync() => _loginState.LogoutAsync();
}
