namespace Tyngd.App.Presentation;

public partial class MainViewModel : ObservableObject
{

    public MainViewModel(
        IOptions<AppConfig> appInfo,
        INavigator navigator)
    {
        Title = "Main";
        Title += $" - {appInfo?.Value?.Environment}";
    }
    public string? Title { get; }


}
