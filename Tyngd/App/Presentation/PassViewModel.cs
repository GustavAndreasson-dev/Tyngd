namespace Tyngd.App.Presentation;

public partial class PassViewModel : ObservableObject
{
    public PassViewModel(PassArgs args)
    {
        Args = args;
    }

    public PassArgs Args { get; }
}
