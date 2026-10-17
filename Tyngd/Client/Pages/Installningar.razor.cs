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
    // "2026.10.17.1605+<hash>" från bygget blir "2026.10.17.1605 · 267123a".
    private static readonly string AppVersion = FormatVersion(
        typeof(Installningar).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    public static string FormatVersion(string? informational)
    {
        if (string.IsNullOrEmpty(informational)) return "okänd";
        var parts = informational.Split('+', 2);
        return parts.Length == 2 ? $"{parts[0]} · {parts[1][..Math.Min(7, parts[1].Length)]}" : parts[0];
    }

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

    private async Task InstallTyngd()
    {
        await PwaService.InstallAsync();
    }

    private void OnPwaStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }
}
