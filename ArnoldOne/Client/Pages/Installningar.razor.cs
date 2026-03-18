using ArnoldOne.Client.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace ArnoldOne.Client.Pages;

/// <summary>
/// Inställningssida där användaren kan konfigurera applikationens utseende.
/// </summary>
public partial class Installningar : ComponentBase, IDisposable
{
    private const string DarkModeKey = "darkmode";

    /// <summary>Tjänst som håller temainställningarna globalt.</summary>
    [Inject]
    private ThemeState ThemeState { get; set; } = default!;

    /// <summary>Tjänst för att spara inställningar i webbläsarens LocalStorage.</summary>
    [Inject]
    private ILocalStorageService LocalStorage { get; set; } = default!;

    protected override void OnInitialized()
    {
        // Prenumerera så sidan ritas om när MainLayout sätter systempreferensen
        ThemeState.OnChange += StateHasChanged;
    }

    /// <summary>Togglar mörkt läge och sparar valet i LocalStorage.</summary>
    private async Task OnDarkModeChanged(bool value)
    {
        ThemeState.SetDarkMode(value);
        await LocalStorage.SetItemAsync(DarkModeKey, value);
    }

    /// <summary>Avslutar prenumerationen på temaändringar.</summary>
    public void Dispose()
    {
        ThemeState.OnChange -= StateHasChanged;
    }
}
