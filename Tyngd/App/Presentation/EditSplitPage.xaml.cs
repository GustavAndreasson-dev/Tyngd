using Tyngd.Shared.TrainingPrograms;
using Tyngd.App.Presentation.Dialogs;

namespace Tyngd.App.Presentation;

public sealed partial class EditSplitPage : Page
{
    public EditSplitPage()
    {
        this.InitializeComponent();
        this.KeepFocusedInputVisible();
        this.GoBackOnBackButton();
    }

    private EditSplitViewModel? ViewModel => DataContext as EditSplitViewModel;

    /// <summary>Importdialogen (som ImportSplitDialog.razor): välj en egen split eller en mall att utgå från.</summary>
    private async void OnImportClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } viewModel || await viewModel.LoadImportSourcesAsync() is not { } sources)
            return;

        var dialog = new ImportSplitDialog(sources.MySplits, sources.Templates) { XamlRoot = XamlRoot };
        await dialog.ShowAsync();
        var selected = (object?)dialog.Result?.MySplit ?? dialog.Result?.Template;

        if (selected is TrainingSplit mySplit)
            await viewModel.ImportMySplitAsync(mySplit);
        else if (selected is TrainingSplitTemplate template)
            viewModel.ImportTemplate(template);
    }
}
