
using ArnoldOne.Client.Services;
using ArnoldOne.Client.Shared;
using ArnoldOne.Shared.Users;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

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

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private LoginState LoginState { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private LocalStorageMigrationService MigrationService { get; set; } = default!;

    /// <summary>Styr om navigeringslådan är öppen eller stängd.</summary>
    bool _drawerOpen = true;
    MudMenu _menu = default!;

    protected override async Task OnInitializedAsync()
    {
        await MigrationService.MigrateAsync();
        await LoginState.InitializeAsync();

        ThemeState.OnChange += StateHasChanged;
        LoginState.OnChange += StateHasChanged;

        var stored = await LocalStorage.GetItemAsync<bool?>(DarkModeKey);

        if (stored.HasValue)
        {
            ThemeState.SetDarkMode(stored.Value);
        }
        else
        {
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

    /// <summary>Öppnar inloggningsdialogen och loggar in om lyckad.</summary>
    private async Task OpenLoginDialog()
    {
        var dialog = await DialogService.ShowAsync<LoginDialog>("Logga in");
        var result = await dialog.Result;

        await _menu.CloseMenuAsync();

        if (!result.Canceled && result.Data is UserInfo user)
            StateHasChanged();
    }

    /// <summary>Öppnar registreringsdialogen och loggar in direkt om lyckad.</summary>
    private async Task OpenRegisterDialog()
    {
        var dialog = await DialogService.ShowAsync<RegisterDialog>("Skapa konto");
        var result = await dialog.Result;

        await _menu.CloseMenuAsync();

        if (!result.Canceled && result.Data is UserInfo user)
            StateHasChanged();
    }

    /// <summary>Loggar ut den inloggade användaren.</summary>
    private async Task Logout()
    {
        await _menu.CloseMenuAsync();
        await LoginState.LogoutAsync();
    }

    /// <summary>Växlar navigeringslådans öppna/stängda tillstånd.</summary>
    void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }

    /// <summary>Avslutar prenumerationerna.</summary>
    public void Dispose()
    {
        ThemeState.OnChange -= StateHasChanged;
        LoginState.OnChange -= StateHasChanged;
    }
}
