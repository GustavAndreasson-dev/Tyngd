using Tyngd.Shared.TrainingPrograms;

namespace Tyngd.App.Presentation.Dialogs;

/// <summary>Väljer en egen split eller en splitmall att importera, som ImportSplitDialog i webbappen. Läs <see cref="Result"/> efter ShowAsync.</summary>
public sealed partial class ImportSplitDialog : ContentDialog
{
    public ImportSplitDialog(IReadOnlyList<TrainingSplit> mySplits, IReadOnlyList<TrainingSplitTemplate> templates)
    {
        this.InitializeComponent();
        MySplitsList.ItemsSource = mySplits;
        MySplitsSection.Visibility = mySplits.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        TemplatesList.ItemsSource = templates;
    }

    /// <summary>Valet, eller null om dialogen avbröts.</summary>
    public ImportSplitResult? Result { get; private set; }

    private void OnSplitClick(object sender, RoutedEventArgs e)
    {
        Result = (sender as FrameworkElement)?.DataContext switch
        {
            TrainingSplit split => new ImportSplitResult(split, null),
            TrainingSplitTemplate template => new ImportSplitResult(null, template),
            _ => null,
        };
        Hide();
    }
}

public record ImportSplitResult(TrainingSplit? MySplit, TrainingSplitTemplate? Template);
