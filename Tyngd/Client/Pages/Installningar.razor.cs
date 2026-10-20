using Tyngd.Client.Services;
using Tyngd.Shared.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Reflection;

namespace Tyngd.Client.Pages;

/// <summary>
/// Inställningssida där användaren kan konfigurera applikationens utseende och beteende.
/// </summary>
public partial class Installningar : ComponentBase, IDisposable
{
    private static readonly string AppVersion = Tyngd.Shared.AppVersion.Format(
        typeof(Installningar).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    /// <summary>Tjänst som håller temainställningarna globalt.</summary>
    [Inject]
    private ThemeState ThemeState { get; set; } = default!;

    /// <summary>Tjänst som håller vilotimer-inställningarna globalt.</summary>
    [Inject]
    private RestTimerState RestTimer { get; set; } = default!;

    /// <summary>Tjänst för att spara och läsa inställningar från API.</summary>
    [Inject]
    private UserSettingsService UserSettingsService { get; set; } = default!;

    [Inject]
    private PwaService PwaService { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Inject]
    private LoginState LoginState { get; set; } = default!;

    [Inject]
    private AuthService AuthService { get; set; } = default!;

    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    private bool _showDeleteAccount;
    private string _deletePassword = string.Empty;
    private string? _deleteError;
    private bool _deleting;

    private int _restTimerMinutes;
    private string? _notificationPermission;
    private string? successMessage;

    protected override void OnInitialized()
    {
        ThemeState.OnChange += StateHasChanged;
        PwaService.OnChange += OnPwaStateChanged;
        _restTimerMinutes = RestTimer.Seconds / 60;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await PwaService.InitializeAsync();
            _notificationPermission = await JS.InvokeAsync<string>("tyngd.restTimer.permission");
            StateHasChanged();
        }
    }

    private async Task RequestNotificationPermission() =>
        _notificationPermission = await JS.InvokeAsync<string>("tyngd.restTimer.requestPermission");

    /// <summary>Togglar mörkt läge och sparar till API.</summary>
    private async Task OnDarkModeChanged(bool value)
    {
        ThemeState.SetDarkMode(value);
        await PersistSettingsAsync();
    }

    /// <summary>Togglar vilotimern och sparar till API.</summary>
    private async Task OnRestTimerEnabledChanged(bool value)
    {
        RestTimer.SetEnabled(value);
        await PersistSettingsAsync();
    }

    /// <summary>Uppdaterar vilotiden och sparar till API.</summary>
    private async Task OnRestTimerMinutesChanged(int value)
    {
        _restTimerMinutes = value;
        RestTimer.SetSeconds(value * 60);
        await PersistSettingsAsync();
    }

    private async Task PersistSettingsAsync()
    {
        try
        {
            await UserSettingsService.SaveSettingsAsync(new UserSettings
            {
                IsDarkMode         = ThemeState.IsDarkMode,
                IsRestTimerEnabled = RestTimer.IsEnabled,
                RestTimerSeconds   = RestTimer.Seconds,
            });
            successMessage = "Inställningarna har sparats.";
        }
        catch { }
    }

    /// <summary>Avslutar prenumerationen på temaändringar.</summary>
    public void Dispose()
    {
        ThemeState.OnChange -= StateHasChanged;
        PwaService.OnChange -= OnPwaStateChanged;
    }

    private void ClearSuccess() => successMessage = null;

    private async Task DeleteAccount()
    {
        _deleting = true;
        _deleteError = await AuthService.DeleteAccountAsync(_deletePassword);
        _deleting = false;
        if (_deleteError is null)
            Nav.NavigateTo("/?deleted=1");
    }

    private async Task InstallTyngd()
    {
        await PwaService.InstallAsync();
    }

    private void OnPwaStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }
}
