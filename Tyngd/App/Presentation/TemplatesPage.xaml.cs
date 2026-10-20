namespace Tyngd.App.Presentation;

public sealed partial class TemplatesPage : Page
{
    public TemplatesPage()
    {
        this.InitializeComponent();
        // Fliksidan behålls; ladda om när den visas så att aktiveringar från Mina splits syns.
        Loaded += (_, _) => Reload();
        DataContextChanged += (_, _) => Reload();
        RegisterPropertyChangedCallback(VisibilityProperty, (_, _) =>
        {
            if (Visibility == Visibility.Visible)
                Reload();
        });
    }

    private void Reload() => _ = (DataContext as TemplatesViewModel)?.LoadAsync();
}
