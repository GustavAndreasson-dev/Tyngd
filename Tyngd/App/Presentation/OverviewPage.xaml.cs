using Tyngd.App.Presentation.Dialogs;

namespace Tyngd.App.Presentation;

public sealed partial class OverviewPage : Page
{
    public OverviewPage()
    {
        this.InitializeComponent();
        // Fliksidan behålls mellan flikbyten: ladda om när den syns igen, t.ex. efter ett pass.
        Loaded += (_, _) => Reload();
        DataContextChanged += (_, _) => Reload();
        RegisterPropertyChangedCallback(VisibilityProperty, (_, _) =>
        {
            if (Visibility == Visibility.Visible)
                Reload();
        });
    }

    private OverviewViewModel? ViewModel => DataContext as OverviewViewModel;

    private void Reload()
    {
        if (IsLoaded && Visibility == Visibility.Visible)
            _ = ViewModel?.LoadAsync();
    }

    private async void OnStartFreeSession(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } vm)
            return;
        var dialog = new StartFreeSessionDialog(vm.FreeSessions, vm.Catalog, vm.Favorites);
        await dialog.ShowOverAsync(this);
        if (dialog.Started is { } started)
            await vm.OpenPassAsync(started.SplitId, started.PassId, startNew: true);
    }

    private async void OnPassChipClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is { } vm && ((FrameworkElement)sender).DataContext is OverviewViewModel.PassChip chip)
            await vm.OpenPassAsync(chip.SplitId, chip.PassId);
    }
}
