namespace Tyngd.App.Presentation;

public sealed partial class AdminTemplatePage : Page
{
    public AdminTemplatePage()
    {
        this.InitializeComponent();
        this.KeepFocusedInputVisible();
        this.GoBackOnBackButton();
        // Laddas här (UI-tråden) och inte i vymodellens konstruktor, som kan köras på en bakgrundstråd.
        DataContextChanged += (_, _) => _ = (DataContext as AdminTemplateViewModel)?.LoadAsync();
    }
}
