using Tyngd.App.Presentation.Dialogs;

namespace Tyngd.App.Presentation;

public sealed partial class PassPage : Page
{
    public PassPage()
    {
        this.InitializeComponent();
        this.KeepFocusedInputVisible();
        this.GoBackOnBackButton();
    }

    private PassViewModel? ViewModel => DataContext as PassViewModel;

    // Skärmen släcks inte under passet (som webbappens wake lock).
    private readonly Windows.System.Display.DisplayRequest _keepScreenOn = new();
    private bool _screenKeptOn;

    private void KeepScreenOn(bool on)
    {
        if (on == _screenKeptOn)
            return;
        try
        {
            if (on)
                _keepScreenOn.RequestActive();
            else
                _keepScreenOn.RequestRelease();
            _screenKeptOn = on;
        }
        catch (Exception)
        {
            // Inte stöd på alla plattformar (t.ex. desktop); passet fungerar ändå.
        }
    }

    // DataContext kan sättas efter Loaded; ladda då när den kommer.
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        KeepScreenOn(true);
        if (ViewModel is { } vm)
            await vm.ActivateAsync();
        else
            DataContextChanged += OnDataContextChanged;
    }

    private async void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (ViewModel is not { } vm)
            return;
        DataContextChanged -= OnDataContextChanged;
        await vm.ActivateAsync();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        KeepScreenOn(false);
        DataContextChanged -= OnDataContextChanged;
        ViewModel?.Deactivate();
    }

    private async void OnAddExerciseClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } vm)
            return;

        var dialog = new AddFreeExerciseDialog(vm.Catalog, vm.Favorites, vm.TakenCatalogIds);
        await dialog.ShowOverAsync(this);
        if (dialog.Picked is { } picked)
            await vm.AddFreeExerciseAsync(picked);
    }

    private async void OnEndPassClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } vm)
            return;

        EndPassButton.IsEnabled = false;
        try
        {
            if (!await vm.EndPassAsync())
                return;

            if (vm.IsFreeSession)
            {
                var dialog = new FinishFreeSessionDialog(vm.FreeSessions, vm.UserSplits, vm.Args.PassId, vm.PassName);
                await dialog.ShowOverAsync(this);
                // Som webbappen: ett sparat pass visas i Mina splits.
                if (dialog.Kept is not null)
                    MainPage.RequestTab(MainPage.SplitsTabIndex);
            }

            await vm.GoBackCommand.ExecuteAsync(null);
        }
        finally
        {
            EndPassButton.IsEnabled = true;
        }
    }
}
