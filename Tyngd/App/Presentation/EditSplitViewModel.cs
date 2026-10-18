namespace Tyngd.App.Presentation;

public partial class EditSplitViewModel : ObservableObject
{
    public EditSplitViewModel(EditSplitArgs args)
    {
        Args = args;
    }

    public EditSplitArgs Args { get; }
}
