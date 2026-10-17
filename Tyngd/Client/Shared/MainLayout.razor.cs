
using Tyngd.Client.Services;
using Tyngd.Client.Shared;
using Tyngd.Shared.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Tyngd.Shared;

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

    [Inject]
    private PendingSyncState PendingSync { get; set; } = default!;

    [Inject]
    private PwaService PwaService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    /// <summary>Styr om navigeringslådan är öppen eller stängd.</summary>
    bool _drawerOpen = true;
    bool _ready = false;
    bool _syncingPendingData = false;
    bool _updateSnackbarShown = false;
    MudMenu _menu = default!;

    protected override async Task OnInitializedAsync()
    {
        await MigrationService.MigrateAsync();
        await LoginState.InitializeAsync();

        ThemeState.OnChange += OnThemeChanged;
        Nav.LocationChanged += OnLocationChanged;
        LoginState.OnChange += OnLoginChanged;
        PendingSync.OnChange += StateHasChanged;
        await PendingSync.RefreshAsync();
        await RestTimer.RestoreAsync();

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        PwaService.OnChange += OnPwaStateChanged;
        await PwaService.InitializeAsync();
        ShowUpdateSnackbarIfNeeded();
    }

    /// <summary>Öppnar inloggningsdialogen och loggar in om lyckad.</summary>
    private async Task OpenLoginDialog()
    {
        var dialog = await DialogService.ShowAsync<LoginDialog>("Logga in");
        var result = await dialog.Result;

        await _menu.CloseMenuAsync();

        if (result is { Canceled: false, Data: UserInfo })
            StateHasChanged();
    }

    /// <summary>Öppnar registreringsdialogen och loggar in direkt om lyckad.</summary>
    private async Task OpenRegisterDialog()
    {
        var dialog = await DialogService.ShowAsync<RegisterDialog>("Skapa konto");
        var result = await dialog.Result;

        await _menu.CloseMenuAsync();

        if (result is { Canceled: false, Data: UserInfo })
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

    private async Task SyncPendingData()
    {
        if (_syncingPendingData)
            return;

        _syncingPendingData = true;
        try
        {
            await PendingSync.SyncVisiblePendingAsync();
        }
        finally
        {
            _syncingPendingData = false;
        }
    }

    /// <summary>Avslutar prenumerationerna.</summary>
    public void Dispose()
    {
        ThemeState.OnChange -= OnThemeChanged;
        Nav.LocationChanged -= OnLocationChanged;
        LoginState.OnChange -= OnLoginChanged;
        PendingSync.OnChange -= StateHasChanged;
        PwaService.OnChange -= OnPwaStateChanged;
    }

    private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e) =>
        InvokeAsync(StateHasChanged);

    private void OnThemeChanged()
    {
        StateHasChanged();
        _ = JSRuntime.InvokeVoidAsync("tyngd.rememberTheme", ThemeState.IsDarkMode).AsTask();
    }

    /// <summary>Går tillbaka i historiken, eller till startsidan om sidan öppnades direkt.</summary>
    private async Task GoBack()
    {
        if (!await JSRuntime.InvokeAsync<bool>("tyngd.goBack"))
            Nav.NavigateTo("/");
    }

    private void OnLoginChanged()
    {
        StateHasChanged();
        _ = PendingSync.RefreshAsync();
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

    private void OnPwaStateChanged()
    {
        _ = InvokeAsync(ShowUpdateSnackbarIfNeeded);
    }

    private void ShowUpdateSnackbarIfNeeded()
    {
        if (!PwaService.IsUpdateAvailable || _updateSnackbarShown)
            return;

        _updateSnackbarShown = true;
        Snackbar.Add(
            "En ny version finns",
            Severity.Info,
            options =>
            {
                options.Action = "Uppdatera";
                options.ActionColor = Color.Primary;
                options.RequireInteraction = true;
                options.ShowCloseIcon = false;
                options.OnClick = _ => PwaService.ReloadAsync().AsTask();
            },
            key: "pwa-update");
    }
}
