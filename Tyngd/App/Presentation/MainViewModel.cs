namespace Tyngd.App.Presentation;

/// <summary>Appens skal (bottenmenyn). Läser användarens inställningar vid start, som MainLayout i webbappen.</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ThemeState _themeState;
    private readonly RestTimerState _restTimer;
    private readonly UserSettingsService _userSettingsService;
    private readonly PendingSyncState _pendingSync;

    public MainViewModel(ThemeState themeState, RestTimerState restTimer, UserSettingsService userSettingsService,
        PendingSyncState pendingSync, IDispatcher dispatcher)
    {
        _themeState = themeState;
        _restTimer = restTimer;
        _userSettingsService = userSettingsService;
        _pendingSync = pendingSync;
        // IDispatcher, inte DispatcherQueue.GetForCurrentThread(): navigeringen skapar ViewModel på en bakgrundstråd.
        _pendingSync.OnChange += () => dispatcher.TryEnqueue(() => OnPropertyChanged(nameof(HasVisiblePendingSync)));
        _ = LoadSettingsAsync();
    }

    public RestTimerState RestTimer => _restTimer;

    /// <summary>Andra pass än det som pågår har lokal data som inte synkats (som "Osynkat" i webbappens appfält).</summary>
    public bool HasVisiblePendingSync => _pendingSync.HasVisiblePendingSync;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SyncPendingCommand))]
    private bool _isSyncing;

    [RelayCommand(CanExecute = nameof(CanSync))]
    private async Task SyncPendingAsync()
    {
        IsSyncing = true;
        try
        {
            await _pendingSync.SyncVisiblePendingAsync();
        }
        finally
        {
            IsSyncing = false;
        }
    }

    private bool CanSync() => !IsSyncing;

    private async Task LoadSettingsAsync()
    {
        await _restTimer.RestoreAsync();
        await _pendingSync.RefreshAsync();

        var settings = await _userSettingsService.GetSettingsAsync();
        if (settings is null)
            return;

        _themeState.SetDarkMode(settings.IsDarkMode);
        _restTimer.SetEnabled(settings.IsRestTimerEnabled);
        _restTimer.SetSeconds(settings.RestTimerSeconds);
    }
}
