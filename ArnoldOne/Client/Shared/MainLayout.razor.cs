
using ArnoldOne.Client.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ArnoldOne.Shared;

/// <summary>
/// Applikationens huvudlayout med navigeringslåda (drawer).
/// </summary>
public partial class MainLayout : IDisposable
{
    private const string DarkModeKey = "darkmode";
    private const string RestTimerEnabledKey = "resttimer-enabled";
    private const string RestTimerSecondsKey = "resttimer-seconds";

    [Inject]
    private ThemeState ThemeState { get; set; } = default!;

    [Inject]
    private RestTimerState RestTimer { get; set; } = default!;

    [Inject]
    private ILocalStorageService LocalStorage { get; set; } = default!;

    /// <summary>Används för att läsa webbläsarens systempreferens via JS.</summary>
    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>Styr om navigeringslådan är öppen eller stängd.</summary>
    bool _drawerOpen = true;

    protected override async Task OnInitializedAsync()
    {
        // Prenumerera på temaändringar så layouten ritas om vid byte
        ThemeState.OnChange += StateHasChanged;

        var stored = await LocalStorage.GetItemAsync<bool?>(DarkModeKey);

        if (stored.HasValue)
        {
            // Använd användarens sparade val
            ThemeState.SetDarkMode(stored.Value);
        }
        else
        {
            // Inget sparat val – läs webbläsarens/OS:ets preferens via JS
            var systemDark = await JSRuntime.InvokeAsync<bool>(
                "eval", "window.matchMedia('(prefers-color-scheme: dark)').matches");
            ThemeState.SetDarkMode(systemDark);
        }

        var storedRestEnabled = await LocalStorage.GetItemAsync<bool?>(RestTimerEnabledKey);
        if (storedRestEnabled.HasValue)
            RestTimer.SetEnabled(storedRestEnabled.Value);

        var storedRestSeconds = await LocalStorage.GetItemAsync<int?>(RestTimerSecondsKey);
        if (storedRestSeconds.HasValue)
            RestTimer.SetSeconds(storedRestSeconds.Value);
    }

    /// <summary>Växlar navigeringslådans öppna/stängda tillstånd.</summary>
    void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }

    /// <summary>Avslutar prenumerationen på temaändringar.</summary>
    public void Dispose()
    {
        ThemeState.OnChange -= StateHasChanged;
    }
}
