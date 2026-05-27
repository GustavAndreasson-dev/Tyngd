
using ArnoldOne.Client.Services;
using ArnoldOne.Client.Shared;
using ArnoldOne.Shared.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace ArnoldOne.Shared;

/// <summary>
/// Applikationens huvudlayout med navigeringslåda (drawer).
/// </summary>
public partial class MainLayout : IDisposable
{
    [Inject]
    private ThemeState ThemeState { get; set; } = default!;

    [Inject]
    private RestTimerState RestTimer { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private LoginState LoginState { get; set; } = default!;

    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private LocalStorageMigrationService MigrationService { get; set; } = default!;

    [Inject]
    private UserSettingsService UserSettingsService { get; set; } = default!;

    /// <summary>Styr om navigeringslådan är öppen eller stängd.</summary>
    bool _drawerOpen = true;
    bool _ready = false;
    MudMenu _menu = default!;

    protected override async Task OnInitializedAsync()
    {
        await MigrationService.MigrateAsync();
        await LoginState.InitializeAsync();

        ThemeState.OnChange += StateHasChanged;
        LoginState.OnChange += OnLoginChanged;

        if (LoginState.IsLoggedIn)
        {
            try
            {
                var settings = await UserSettingsService.GetSettingsAsync();
                if (settings is not null)
                {
                    ThemeState.SetDarkMode(settings.IsDarkMode);
                    RestTimer.SetEnabled(settings.IsRestTimerEnabled);
                    RestTimer.SetSeconds(settings.RestTimerSeconds);
                    _ready = true;
                    return;
                }
            }
            catch { }
        }

        // Fallback: systemets mörkt-läge-inställning
        var systemDark = await JSRuntime.InvokeAsync<bool>(
            "eval", "window.matchMedia('(prefers-color-scheme: dark)').matches");
        ThemeState.SetDarkMode(systemDark);
        _ready = true;
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
        Nav.NavigateTo("/", forceLoad: false);
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
        LoginState.OnChange -= OnLoginChanged;
    }

    private void OnLoginChanged()
    {
        StateHasChanged();
        if (LoginState.IsLoggedIn)
            _ = LoadUserSettingsAsync();
    }

    private async Task LoadUserSettingsAsync()
    {
        try
        {
            var settings = await UserSettingsService.GetSettingsAsync();
            if (settings is not null)
            {
                ThemeState.SetDarkMode(settings.IsDarkMode);
                RestTimer.SetEnabled(settings.IsRestTimerEnabled);
                RestTimer.SetSeconds(settings.RestTimerSeconds);
            }
        }
        catch { }
    }
}
