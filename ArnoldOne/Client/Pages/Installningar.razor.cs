using ArnoldOne.Client.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace ArnoldOne.Client.Pages;

/// <summary>
/// Inställningssida där användaren kan konfigurera applikationens utseende och beteende.
/// </summary>
public partial class Installningar : ComponentBase, IDisposable
{
    private const string DarkModeKey = "darkmode";
    private const string RestTimerEnabledKey = "resttimer-enabled";
    private const string RestTimerSecondsKey = "resttimer-seconds";

    /// <summary>Tjänst som håller temainställningarna globalt.</summary>
    [Inject]
    private ThemeState ThemeState { get; set; } = default!;

    /// <summary>Tjänst som håller vilotimer-inställningarna globalt.</summary>
    [Inject]
    private RestTimerState RestTimer { get; set; } = default!;

    /// <summary>Tjänst för att spara inställningar i webbläsarens LocalStorage.</summary>
    [Inject]
    private ILocalStorageService LocalStorage { get; set; } = default!;

    private int _restTimerMinutes;

    protected override async Task OnInitializedAsync()
    {
        ThemeState.OnChange += StateHasChanged;

        var storedEnabled = await LocalStorage.GetItemAsync<bool?>(RestTimerEnabledKey);
        if (storedEnabled.HasValue)
            RestTimer.SetEnabled(storedEnabled.Value);

        var storedSeconds = await LocalStorage.GetItemAsync<int?>(RestTimerSecondsKey);
        RestTimer.SetSeconds(storedSeconds ?? 120);

        _restTimerMinutes = RestTimer.Seconds / 60;
    }

    /// <summary>Togglar mörkt läge och sparar valet i LocalStorage.</summary>
    private async Task OnDarkModeChanged(bool value)
    {
        ThemeState.SetDarkMode(value);
        await LocalStorage.SetItemAsync(DarkModeKey, value);
    }

    /// <summary>Togglar vilotimern och sparar valet i LocalStorage.</summary>
    private async Task OnRestTimerEnabledChanged(bool value)
    {
        RestTimer.SetEnabled(value);
        await LocalStorage.SetItemAsync(RestTimerEnabledKey, value);
    }

    /// <summary>Uppdaterar vilotiden och sparar till LocalStorage.</summary>
    private async Task OnRestTimerMinutesChanged(int value)
    {
        _restTimerMinutes = value;
        RestTimer.SetSeconds(value * 60);
        await LocalStorage.SetItemAsync(RestTimerSecondsKey, value * 60);
    }

    /// <summary>Avslutar prenumerationen på temaändringar.</summary>
    public void Dispose()
    {
        ThemeState.OnChange -= StateHasChanged;
    }
}
