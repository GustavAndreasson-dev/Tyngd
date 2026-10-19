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

    // DataContext kan sättas efter Loaded; ladda då när den kommer.
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
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
                await new FinishFreeSessionDialog(vm.FreeSessions, vm.UserSplits, vm.Args.PassId, vm.PassName) .ShowOverAsync(this);

            await vm.GoBackCommand.ExecuteAsync(null);
        }
        finally
        {
            EndPassButton.IsEnabled = true;
        }
    }
}
